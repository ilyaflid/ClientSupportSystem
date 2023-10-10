using ClientSupportService.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientSupportService
{
    public class ClientSession
    {
        public Guid SessionId { get; private set; }
        private DateTime CreatedDate { get; set; }
        private DateTime UpdatedDate { get; set; }

        public ClientSession(Guid sessionId, DateTime currentTime)
        {
            SessionId = sessionId;
            CreatedDate = currentTime;
            UpdatedDate = currentTime;
        }
        public void Prolongate(DateTime currentTime) => UpdatedDate = currentTime;
        public bool IsExpired(TimeSpan expirationTimeout, DateTime currentTime) => UpdatedDate.Add(expirationTimeout) <= currentTime;

        public override string ToString() => $"{SessionId}";
        public override int GetHashCode() => SessionId.GetHashCode();
    }
}
