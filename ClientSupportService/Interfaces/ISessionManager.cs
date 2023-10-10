using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientSupportService
{
    public interface ISessionManager
    {
        public Task<Guid?> CreateSessionAsync();
        public Task<bool> ProlongateSessionAsync(Guid sessionId);
    }
}
