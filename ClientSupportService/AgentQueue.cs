using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace ClientSupportService
{
    public class AgentQueue
    {
        private readonly int _maxCapacity;
        public Agent Agent { get; private set; }
        private Dictionary<Guid, ClientSession> _sessions = new Dictionary<Guid, ClientSession>();

        public AgentQueue(Agent agent, int maximumConcurrencyPerAgent)
        {
            Agent = agent;
            _maxCapacity = (int)Math.Floor(maximumConcurrencyPerAgent * agent.Efficiency);
        }
        public int QueueSize {
            get => _sessions.Count;
        }
        public int NumberOfOpenSpots
        {
            get => _maxCapacity - _sessions.Count;
        }

        public bool HasOpenSpots {
            get => _maxCapacity > _sessions.Count;
        }
        public void AddSession(ClientSession session)
        {
            if (_sessions.ContainsKey(session.SessionId))
                return;

            if (_sessions.Count >= _maxCapacity)
                return;

            _sessions.Add(session.SessionId, session);
        }

        public void RemoveSession(ClientSession session)
        {
            RemoveSession(session.SessionId);
        }
        public void RemoveSession(Guid sessionId)
        {
            if (!_sessions.ContainsKey(sessionId))
                return;

            _sessions.Remove(sessionId);
        }
    }
}
