using ClientSupportService.Interfaces;
using Serilog;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace ClientSupportService
{
    public class SessionManager : ISessionManager, IDisposable
    {
        private readonly ILogger _logger;
        private readonly List<RegularAgent> _regularAgentsPool = new List<RegularAgent>();
        private readonly List<AdditionalAgent> _additionalAgentsPool = new List<AdditionalAgent>();
        private readonly List<AgentQueue> _agentQueues = new List<AgentQueue>();
        private readonly int _maximumConcurrencyPerAgent;
        private readonly TimeSpan _sessionTimeout;
        private readonly ISessionStorage _sessionStorage;
        private readonly IDateTimeService _dateTimeService;
        private readonly Timer _timer;
        private readonly ShiftAlert _shiftAlert;

        private const int _sessionCheckPeriodInSeconds = 1;

        // Stores all allocations of sessions to agents
        private Dictionary<Guid, AgentQueue> _sessionToAgentQueueDict;
        // Stores all active sessions (not in queue)
        private Dictionary<Guid, ClientSession> _activeClientSessions;

        public int Capacity
        {
            get => (int)Math.Floor(_regularAgentsPool
                    .Where(p => p.IsWorkingNow(_dateTimeService))
                    .Sum(p => p.Efficiency * _maximumConcurrencyPerAgent));
        }
        public int MaximumQueueSize
        {
            get => (int)Math.Floor(Capacity * 1.5);
        }
        public SessionManager(ISessionStorage storage, IDateTimeService dateTimeService, ILogger logger, IClientSupportServiceConfiguration configuration)
        {
            _logger = logger;
            _regularAgentsPool = configuration.RegularAgents;
            _additionalAgentsPool = configuration.AdditionalAgents;
            _maximumConcurrencyPerAgent = configuration.MaximumConcurrencyPerAgent;
            _sessionTimeout = configuration.SessionTimeout;
            _sessionStorage = storage;
            _dateTimeService = dateTimeService;

            _sessionToAgentQueueDict = new Dictionary<Guid, AgentQueue>();
            _activeClientSessions = new Dictionary<Guid, ClientSession>();

            InitializeAgentsQueues(configuration.MaximumConcurrencyPerAgent);
            InitializeSessionStorage();

            _shiftAlert = new ShiftAlert(OnShiftChanged, configuration.RegularAgents.Select(t => t.ShiftStartTime).Union(configuration.RegularAgents.Select(t => t.ShiftEndTime)));
            _timer = new Timer(o => CheckSessionExpiration(), new { }, TimeSpan.Zero, TimeSpan.FromSeconds(_sessionCheckPeriodInSeconds));
        }

        private void InitializeAgentsQueues(int maximumConcurrencyPerAgent)
        {
            foreach (var agent in _regularAgentsPool)
                _agentQueues.Add(new AgentQueue(agent, maximumConcurrencyPerAgent));
            foreach (var agent in _additionalAgentsPool)
                _agentQueues.Add(new AgentQueue(agent, maximumConcurrencyPerAgent));
        }

        private void InitializeSessionStorage()
        {
            _sessionStorage.SetMaximumQueueSize(MaximumQueueSize);
            _sessionStorage.OnSessionCreated = OnSessionCreated;
            _sessionStorage.OnSessionRemoved = OnSessionRemoved;
        }

        public async Task<Guid?> CreateSessionAsync()
        {
            _logger.Information("Requested creating a new session");
            var session = await _sessionStorage.CreateSessionAsync();

            if (session == null)
                return null;

            _logger.Information($"The new session {session.SessionId} has been created");

            return session.SessionId;
        }

        public async Task<bool> ProlongateSessionAsync(Guid sessionId)
        {
            _logger.Information($"Requested prolongating the session {sessionId}");
            var sessionProlongated = await _sessionStorage.ProlongateSessionAsync(sessionId);
            if (!sessionProlongated)
            {
                if (!_activeClientSessions.ContainsKey(sessionId))
                    return false;

                _activeClientSessions[sessionId].Prolongate(_dateTimeService.Now);
            }

            _logger.Information($"The session {sessionId} prolongated");
            return true;
        }

        public void DestroySession(ClientSession session)
        {
            if (_sessionToAgentQueueDict.ContainsKey(session.SessionId))
            {
                var agent = _sessionToAgentQueueDict[session.SessionId];
                agent.RemoveSession(session);
                _sessionToAgentQueueDict.Remove(session.SessionId);
                _activeClientSessions.Remove(session.SessionId);
                _logger.Information($"Session {session} has been removed from agent {agent.Agent}");
            }
            else
                _sessionStorage.RemoveSessionAsync(session.SessionId);

            _logger.Information($"Session {session} has been deleted");
        }

        public void AllocateSessionToAgent(ClientSession session)
        {
            var agent = _agentQueues
                .Where(p => p.Agent.IsAvailableToChat(_dateTimeService) && p.HasOpenSpots)
                .OrderBy(p => p.Agent.Priority)
                .ThenBy(p => p.QueueSize)
                .FirstOrDefault();

            if (agent == null)
                return;
           
            agent.AddSession(session);
            _sessionToAgentQueueDict.Add(session.SessionId, agent);
            _activeClientSessions.Add(session.SessionId, session);
            _logger.Information($"Session {session} was allocated to agent {agent.Agent}");
        }

        private void TryToAllocateSessionsFromQueue()
        {
            while (_agentQueues
                .Where(p => p.Agent.IsAvailableToChat(_dateTimeService) && p.HasOpenSpots)
                .Sum(p => p.NumberOfOpenSpots) > 0)
            {
                var session =  
                    _sessionStorage.PopFirstSessionInQueueAsync().GetAwaiter().GetResult();
                if (session == null)
                    break;

                AllocateSessionToAgent(session);
            }
        }

        private void KickAdditionalAgent()
        {
            var additionalAgent = _additionalAgentsPool
                .FirstOrDefault(p => p.IsWorkingNow(_dateTimeService) && !p.IsAvailableToChat(_dateTimeService));
            if (additionalAgent != null)
                additionalAgent.MakeOpenToChat();
        }

        private void RemoveAdditionalAgents()
        {
            foreach (var agent in _additionalAgentsPool)
                agent.MakeCloseToChat();
        }

        private void CheckSessionExpiration()
        {
            // Removing expired sessions in the queue
            _sessionStorage.RemoveExpiredSessionsAsync(_sessionTimeout).GetAwaiter().GetResult();
            
            // Removing expired sessions allocated to agents
            var expierdAllocatedSessions = _activeClientSessions.Values.Where(p => p.IsExpired(_sessionTimeout, _dateTimeService.Now)).ToList();
            foreach (var session in expierdAllocatedSessions)
                DestroySession(session);

            TryToAllocateSessionsFromQueue();
        }

        private void OnSessionCreated()
        {
            if (_sessionStorage.GetQueueSize() >= MaximumQueueSize)
                KickAdditionalAgent();

            TryToAllocateSessionsFromQueue();
        }

        private void OnSessionRemoved()
        {
            if (_sessionStorage.GetQueueSize() < Capacity)
                RemoveAdditionalAgents();
        }

        private void OnShiftChanged()
        {
            var queueSize = _sessionStorage.GetQueueSize();
            if (queueSize >= MaximumQueueSize)
                KickAdditionalAgent();
            else if (queueSize < Capacity)
                RemoveAdditionalAgents();

            _sessionStorage.SetMaximumQueueSize(MaximumQueueSize);
            TryToAllocateSessionsFromQueue();
        }

        public Agent? FindSessionAgent(Guid sessionId)
        {
            if (!_sessionToAgentQueueDict.ContainsKey(sessionId))
                return null;

            return _sessionToAgentQueueDict[sessionId].Agent;          
        }
        public void Dispose()
        {
            _shiftAlert.Dispose();
        }
    }
}