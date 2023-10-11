using ClientSupportService.Interfaces;
using Serilog;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace ClientSupportService
{
    public class SessionManager : ISessionManager
    {
        private readonly ILogger _logger;       
        private readonly TimeSpan _sessionTimeout;
        private readonly ISessionStorage _sessionStorage;
        private readonly IDateTimeService _dateTimeService;
        private readonly Timer _timer;
        private readonly ISessionAllocationManager _allocationManager;

        private const int _sessionCheckPeriodInSeconds = 1;

        // Stores all active sessions (not in queue)
        private Dictionary<Guid, ClientSession> _activeClientSessions;
        public int Capacity
        {
            get => _allocationManager.Capacity;
        }
        public int MaximumQueueSize
        {
            get => (int)Math.Floor(Capacity * 1.5);
        }
        public SessionManager(ISessionStorage storage, ISessionAllocationManager allocationManager, IDateTimeService dateTimeService, ILogger logger, IClientSupportServiceConfiguration configuration)
        {
            _logger = logger;
            _sessionTimeout = configuration.SessionTimeout;
            _sessionStorage = storage;
            _dateTimeService = dateTimeService;
            _allocationManager = allocationManager
                .SetOnShiftChangedAction(async () => await OnShiftChanged());

            _activeClientSessions = new Dictionary<Guid, ClientSession>();
            InitializeSessionStorage();

            _timer = new Timer(o => CheckSessionExpiration(), new { }, TimeSpan.Zero, TimeSpan.FromSeconds(_sessionCheckPeriodInSeconds));
        }

        private void InitializeSessionStorage()
        {
            _sessionStorage.SetMaximumQueueSize(MaximumQueueSize);
            _sessionStorage.OnSessionCreated = async () => await OnSessionCreated();
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
            var sessionInQueueProlongated = await _sessionStorage.ProlongateSessionAsync(sessionId);
            if (sessionInQueueProlongated)
            {
                _logger.Information($"The session {sessionId} prolongated");
                return true;
            }

            if (!_activeClientSessions.ContainsKey(sessionId))
                return false;

            _activeClientSessions[sessionId].Prolongate(_dateTimeService.Now);
            _logger.Information($"The session {sessionId} prolongated");

            return true;
        }

        public async Task<bool> DestroySessionAsync(ClientSession session)
        {
            var sessionInQueueRemoved = await _sessionStorage.RemoveSessionAsync(session.SessionId);
            if (sessionInQueueRemoved)
            {
                _logger.Information($"Session {session} has been deleted");
                return true;
            }

            if (!_activeClientSessions.ContainsKey(session.SessionId))
                return false;

            _activeClientSessions.Remove(session.SessionId);  
            var allocatedSessionRemoved = _allocationManager.DestroySession(session.SessionId);
            if (allocatedSessionRemoved)
                _logger.Information($"Session {session} has been deleted");

            return allocatedSessionRemoved;
        }

        public void AllocateSessionToAgent(ClientSession session)
        {
            _activeClientSessions.Add(session.SessionId, session);
            _allocationManager.AllocateSessionToAgent(session);
        }

        private async Task TryToAllocateSessionsFromQueue()
        {
            while (_allocationManager.HasOpenSpots())
            {
                var session =  
                    await _sessionStorage.PopFirstSessionInQueueAsync();
                if (session == null)
                    break;

                AllocateSessionToAgent(session);
            }
        }

        private void KickAdditionalAgent()
        {
            _allocationManager.KickAdditionalAgent();
        }

        private void RemoveAdditionalAgents()
        {
            _allocationManager.RemoveAdditionalAgents();
        }

        private void CheckSessionExpiration()
        {
            // Removing expired sessions in the queue
            _sessionStorage.RemoveExpiredSessionsAsync(_sessionTimeout).Wait();
            
            // Removing expired sessions allocated to agents
            var expierdAllocatedSessions = _activeClientSessions.Values.Where(p => p.IsExpired(_sessionTimeout, _dateTimeService.Now)).ToList();
            foreach (var session in expierdAllocatedSessions)
                DestroySessionAsync(session).Wait();

            TryToAllocateSessionsFromQueue().Wait();
        }

        private async Task OnSessionCreated()
        {
            if (_sessionStorage.GetQueueSize() >= MaximumQueueSize)
                KickAdditionalAgent();

            await TryToAllocateSessionsFromQueue();
        }

        private void OnSessionRemoved()
        {
            if (_sessionStorage.GetQueueSize() < Capacity)
                RemoveAdditionalAgents();
        }

        private async Task OnShiftChanged()
        {
            var queueSize = _sessionStorage.GetQueueSize();
            if (queueSize >= MaximumQueueSize)
                KickAdditionalAgent();
            else if (queueSize < Capacity)
                RemoveAdditionalAgents();

            _sessionStorage.SetMaximumQueueSize(MaximumQueueSize);
            await TryToAllocateSessionsFromQueue();
        }
    }
}