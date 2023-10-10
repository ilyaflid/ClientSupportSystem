using ClientSupportService.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ClientSupportService
{
    public abstract class Agent
    {
        protected int _id;
        protected AgentLevel _level;
        protected TimeOnly _shiftStartTime;
        protected TimeOnly _shiftEndTime;
        public Agent(int id, TimeOnly shiftStartTime, TimeOnly shiftEndTime, AgentLevel level)
        {
            _id = id;
            _level = level;
            _shiftStartTime = shiftStartTime;
            _shiftEndTime = shiftEndTime;
        }

        public int Id { get => _id; }
        public double Efficiency {
            get {
                return _level switch
                {
                    AgentLevel.Middle => .6,
                    AgentLevel.Senior => .8,
                    AgentLevel.TeamLead => .5,
                    _ => .4
                };
            }
        }

        public int Priority { get =>  (int)_level; }
        public TimeOnly ShiftStartTime { get => _shiftStartTime; }
        public TimeOnly ShiftEndTime { get => _shiftEndTime; }
        public abstract bool IsAvailableToChat(IDateTimeService dateTimeService);
        public string GetWorkingHours() => $"{_shiftStartTime}-{_shiftEndTime}";
        public bool IsWorkingNow(IDateTimeService dateTimeService)
        {
            return TimeOnly.FromDateTime(dateTimeService.Now)
                .IsBetween(_shiftStartTime, _shiftEndTime);
        }
    }

    public class RegularAgent : Agent
    {
        private string _team;

        public RegularAgent(int id, TimeOnly shiftStartTime, TimeSpan shiftTime, AgentLevel level, string team)
            : base(id, shiftStartTime, shiftStartTime.Add(shiftTime), level)
        {
            _team = team;
        }
        public override string ToString() => $"Agent {_id} (level {_level}, team {_team})";
        public override bool IsAvailableToChat(IDateTimeService dateTimeService)
        {
            return IsWorkingNow(dateTimeService);
        }
    }
    public class AdditionalAgent : Agent
    {
        private bool _openToChat = false;
        public override bool IsAvailableToChat(IDateTimeService dateTimeService)
        {
            return IsWorkingNow(dateTimeService) && _openToChat;
        }
        public AdditionalAgent(int id, TimeOnly shiftStartTime, TimeOnly shiftEndTime) 
            : base(id, shiftStartTime, shiftEndTime, AgentLevel.Junior) { }
        public override string ToString() => $"Additional agent {_id}";

        public void MakeOpenToChat() => _openToChat = true;
        public void MakeCloseToChat() => _openToChat = false;
    }

    public enum AgentLevel
    {
        Junior,
        Middle,
        Senior,
        TeamLead
    }
}
