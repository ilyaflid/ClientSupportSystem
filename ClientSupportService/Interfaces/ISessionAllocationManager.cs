using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientSupportService.Interfaces
{
    public interface ISessionAllocationManager
    {
        int Capacity { get; }
        bool HasOpenSpots();
        bool DestroySession(Guid sessionId);
        bool AllocateSessionToAgent(ClientSession session);
        void KickAdditionalAgent();
        void RemoveAdditionalAgents();
        Agent? FindSessionAgent(Guid sessionId);
        ISessionAllocationManager SetOnShiftChangedAction(Action onShiftChanged);
    }
}
