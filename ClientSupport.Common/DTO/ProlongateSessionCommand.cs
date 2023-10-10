using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientSupport.Common.DTO
{
    public class ProlongateSessionCommand : Command
    {
        public Guid SessionId { get; set; }
    }
}
