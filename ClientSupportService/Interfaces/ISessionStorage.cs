using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientSupportService
{
    public interface ISessionStorage
    {
        Action OnSessionCreated { set; }
        Action OnSessionRemoved { set; }
        Task<ClientSession?> CreateSessionAsync();
        Task<bool> ProlongateSessionAsync(Guid sessionId);
        Task<ClientSession?> PopFirstSessionInQueueAsync();
        Task<bool> RemoveExpiredSessionsAsync(TimeSpan expirationTimeout);
        Task<bool> RemoveSessionAsync(Guid sessionId);
        void SetMaximumQueueSize(int maximumQueueSize);
        int GetQueueSize();
    }
}
