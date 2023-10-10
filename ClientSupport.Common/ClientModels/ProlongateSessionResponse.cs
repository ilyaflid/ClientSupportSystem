using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientSupport.Common.ClientModels
{
    public class ProlongateSessionResponse
    {
        public ProlongateSessionResponseCode Code { get; set; }
        public ProlongateSessionResponse() { }
        public ProlongateSessionResponse(ProlongateSessionResponseCode code) {
            Code = code;
        }
    }

    public enum ProlongateSessionResponseCode
    {
        Success = 0,
        Failed = 1
    }
}
