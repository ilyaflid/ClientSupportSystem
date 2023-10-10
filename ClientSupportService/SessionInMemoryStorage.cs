using ClientSupportService.Interfaces;

namespace ClientSupportService
{
    public class SessionInMemoryStorage : ISessionStorage
    {
        private Action? _onSessionCreated;
        private Action? _onSessionRemoved;
        private int _maximumQueueSize;
        private IDateTimeService _dateTimeService;

        // Stores all active sessions not currently allocated to agents
        private List<ClientSession> _sessionsQueue;
        // Stores all active sessions
        private Dictionary<Guid, ClientSession> _sessionsDict;

        private object _lock = new object();

        public SessionInMemoryStorage(IDateTimeService dateTimeService) {
            _sessionsQueue = new List<ClientSession>();
            _sessionsDict = new Dictionary<Guid, ClientSession>();
            _dateTimeService = dateTimeService;
        }

        public Action OnSessionCreated
        { 
            set => _onSessionCreated = value;
        }

        public Action OnSessionRemoved
        {
            set => _onSessionRemoved = value;
        }

        public async Task<ClientSession?> CreateSessionAsync()
        {
            if (_maximumQueueSize <= _sessionsDict.Count)
                return await Task.FromResult<ClientSession?>(null);

            var sessionId = Guid.NewGuid();
            var session = new ClientSession(sessionId, _dateTimeService.Now);
            
            lock (_lock)
            {
                _sessionsQueue.Add(session);
                _sessionsDict.Add(sessionId, session);
            }

            if (_onSessionCreated != null)
                _onSessionCreated();

            return await Task.FromResult(session);
        }

        public async Task<ClientSession?> PopFirstSessionInQueueAsync()
        {
            ClientSession? session = null;
            lock (_lock)
                session = _sessionsQueue.FirstOrDefault();

            if (session != null)
                await RemoveSessionAsync(session.SessionId);

            return session;
        }
        public async Task<bool> ProlongateSessionAsync(Guid sessionId)
        {
            ClientSession? session = null;

            lock (_lock)
                session = _sessionsDict.GetValueOrDefault(sessionId);

            if (session == null)
                return await Task.FromResult(false);

            session.Prolongate(_dateTimeService.Now);
            return await Task.FromResult(true);
        }

        public async Task<bool> RemoveSessionAsync(Guid sessionId)
        {
            ClientSession? session = null;
            lock (_lock)
            {
                session = _sessionsDict.GetValueOrDefault(sessionId);
                if (session != null)
                {
                    _sessionsQueue.Remove(session);
                    _sessionsDict.Remove(sessionId);
                }
            }

            if (_onSessionRemoved != null)
                _onSessionRemoved();

            return await Task.FromResult(session != null);
        }

        public async Task<bool> RemoveExpiredSessionsAsync(TimeSpan expirationTimeout)
        {
            List<ClientSession> expiredSessions;
            lock (_lock)
            {
                expiredSessions = _sessionsQueue.Where(p => p.IsExpired(expirationTimeout, _dateTimeService.Now)).ToList();
                foreach (var session in expiredSessions)
                {
                    _sessionsQueue.Remove(session);
                    _sessionsDict.Remove(session.SessionId);
                }
            }

            if (expiredSessions.Any() && _onSessionRemoved != null)
                _onSessionRemoved();

            return await Task.FromResult(expiredSessions.Any());
        }
        public void SetMaximumQueueSize(int maximumQueueSize)
        {
            _maximumQueueSize = maximumQueueSize;
        }

        public int GetQueueSize()
        {
            return _sessionsQueue.Count;
        }


    }
}
