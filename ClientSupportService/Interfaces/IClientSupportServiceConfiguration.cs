using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientSupportService
{
    public interface IClientSupportServiceConfiguration
    {
        TimeOnly WorkingHoursStartTime { get; }
        TimeOnly WorkingHoursEndTime { get; }
        int MaximumConcurrencyPerAgent { get; }
        TimeSpan SessionTimeout { get; }
        List<RegularAgent> RegularAgents { get; }
        List<AdditionalAgent> AdditionalAgents { get; }
    }
}
