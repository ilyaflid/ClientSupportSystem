using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientSupport.Common.ClientModels
{
    public class CreateSessionResponse
    {
        public string SessionId { get; set; } = string.Empty;
        public CreateSessionResponseCode Code { get; set; }
        public CreateSessionResponse() { }
        public CreateSessionResponse(Guid sessionId)
        {
            SessionId = sessionId.ToString();
            Code = CreateSessionResponseCode.Created;
        }

        public CreateSessionResponse(CreateSessionResponseCode code)
        {
            Code = code;
        }
    }

    public enum CreateSessionResponseCode
    {
        Created = 0,
        TooBusy = 1,
        Failed = 2
    }
}
