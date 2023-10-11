using ClientSupportService.Interfaces;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientSupportService
{
    public class SessionAllocationManager : ISessionAllocationManager, IDisposable
    {
        private readonly ILogger _logger;
        private readonly List<RegularAgent> _regularAgentsPool;
        private readonly List<AdditionalAgent> _additionalAgentsPool;
        private readonly List<AgentQueue> _agentQueues;
        private readonly int _maximumConcurrencyPerAgent;
        private readonly IDateTimeService _dateTimeService;
        private readonly ShiftAlert _shiftAlert;
        private Action? _onShiftChanged;

        // Stores all allocations of sessions to agents
        private Dictionary<Guid, AgentQueue> _sessionToAgentQueueDict;
        public int Capacity
        {
            get => (int)Math.Floor(_regularAgentsPool
                    .Where(p => p.IsWorkingNow(_dateTimeService))
                    .Sum(p => p.Efficiency * _maximumConcurrencyPerAgent));
        }
        public SessionAllocationManager(IDateTimeService dateTimeService, IClientSupportServiceConfiguration configuration, ILogger logger)
        {
            _sessionToAgentQueueDict = new Dictionary<Guid, AgentQueue>();
            _dateTimeService = dateTimeService;

            _regularAgentsPool = configuration.RegularAgents;
            _additionalAgentsPool = configuration.AdditionalAgents;
            _maximumConcurrencyPerAgent = configuration.MaximumConcurrencyPerAgent;
            _agentQueues = new List<AgentQueue>();

            InitializeAgentsQueues(configuration.MaximumConcurrencyPerAgent);

            _shiftAlert = new ShiftAlert(OnShiftChanged,
                configuration.RegularAgents.Select(t => t.ShiftStartTime).Union(configuration.RegularAgents.Select(t => t.ShiftEndTime)),
                _dateTimeService);

            _logger = logger;
        }
        private void InitializeAgentsQueues(int maximumConcurrencyPerAgent)
        {
            foreach (var agent in _regularAgentsPool)
                _agentQueues.Add(new AgentQueue(agent, maximumConcurrencyPerAgent));
            foreach (var agent in _additionalAgentsPool)
                _agentQueues.Add(new AgentQueue(agent, maximumConcurrencyPerAgent));
        }

        public bool DestroySession(Guid sessionId)
        {
            if (!_sessionToAgentQueueDict.ContainsKey(sessionId))
                return false;

            var agentQueue = _sessionToAgentQueueDict[sessionId];
            agentQueue.RemoveSession(sessionId);
            _sessionToAgentQueueDict.Remove(sessionId);
            
            _logger.Information($"Session {sessionId} has been removed from agent {agentQueue.Agent}");
            return true;
        }

        public bool AllocateSessionToAgent(ClientSession session)
        {
            var agentQueue = _agentQueues
                .Where(p => p.Agent.IsAvailableToChat(_dateTimeService) && p.HasOpenSpots)
                .OrderBy(p => p.Agent.Priority)
                .ThenBy(p => p.QueueSize)
                .FirstOrDefault();

            if (agentQueue == null)
                return false;

            agentQueue.AddSession(session);
            _sessionToAgentQueueDict.Add(session.SessionId, agentQueue);

            _logger.Information($"Session {session} was allocated to agent {agentQueue.Agent}");

            return true;
        }

        public bool HasOpenSpots()
        {
            return _agentQueues
                .Where(p => p.Agent.IsAvailableToChat(_dateTimeService) && p.HasOpenSpots)
                .Sum(p => p.NumberOfOpenSpots) > 0;
        }

        public void KickAdditionalAgent()
        {
            var additionalAgent = _additionalAgentsPool
                .FirstOrDefault(p => p.IsWorkingNow(_dateTimeService) && !p.IsAvailableToChat(_dateTimeService));
            if (additionalAgent != null)
                additionalAgent.MakeOpenToChat();
        }

        public void RemoveAdditionalAgents()
        {
            foreach (var agent in _additionalAgentsPool)
                agent.MakeCloseToChat();
        }
        public ISessionAllocationManager SetOnShiftChangedAction(Action onShiftChanged)
        {
            _onShiftChanged = onShiftChanged;
            return this;
        }

        public Agent? FindSessionAgent(Guid sessionId)
        {
            if (!_sessionToAgentQueueDict.ContainsKey(sessionId))
                return null;
        
            return _sessionToAgentQueueDict[sessionId].Agent;          
        }
        private void OnShiftChanged()
        {
            if (_onShiftChanged != null)
                _onShiftChanged();
        }

        public void Dispose()
        {
            _shiftAlert.Dispose();
        }
    }
}
