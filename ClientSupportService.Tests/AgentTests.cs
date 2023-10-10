using ClientSupportService.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientSupportService.Tests
{
    public class AgentTests
    {
        private IDateTimeService _dateTimeService;
        public AgentTests()
        {
            var dateTimeService = new Mock<IDateTimeService>();
            dateTimeService.Setup(p => p.Now).Returns(new DateTime(2023, 10, 10, 10, 0, 0));

            _dateTimeService = dateTimeService.Object;
        }

        [Fact]
        public void RegularAgents_Tests()
        {
            // Shift time 9:00 - 17:00
            var agent1 = new RegularAgent(1000, TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), TimeSpan.FromHours(8), AgentLevel.Junior, "Team 1");
            // Shift time 12:00 - 20:00
            var agent2 = new RegularAgent(1001, TimeOnly.FromTimeSpan(TimeSpan.FromHours(12)), TimeSpan.FromHours(8), AgentLevel.Junior, "Team 1");

            Assert.True(agent1.IsWorkingNow(_dateTimeService));
            Assert.True(agent1.IsAvailableToChat(_dateTimeService));
            
            Assert.False(agent2.IsWorkingNow(_dateTimeService));
            Assert.False(agent2.IsAvailableToChat(_dateTimeService));
        }

        [Fact]
        public void AdditionalAgents_NonAllocatedToChat_Tests()
        {
            // Shift time 9:00 - 17:00
            var agent1 = new AdditionalAgent(1000, TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), TimeOnly.FromTimeSpan(TimeSpan.FromHours(17)));
            // Shift time 12:00 - 20:00
            var agent2 = new AdditionalAgent(1001, TimeOnly.FromTimeSpan(TimeSpan.FromHours(12)), TimeOnly.FromTimeSpan(TimeSpan.FromHours(20)));

            Assert.True(agent1.IsWorkingNow(_dateTimeService));
            Assert.False(agent1.IsAvailableToChat(_dateTimeService));

            Assert.False(agent2.IsWorkingNow(_dateTimeService));
            Assert.False(agent2.IsAvailableToChat(_dateTimeService));
        }

        [Fact]
        public void AdditionalAgents_AllocatedToChat_Tests()
        {
            // Shift time 9:00 - 17:00
            var agent1 = new AdditionalAgent(1000, TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), TimeOnly.FromTimeSpan(TimeSpan.FromHours(17)));
            // Shift time 12:00 - 20:00
            var agent2 = new AdditionalAgent(1001, TimeOnly.FromTimeSpan(TimeSpan.FromHours(12)), TimeOnly.FromTimeSpan(TimeSpan.FromHours(20)));
            agent1.MakeOpenToChat();
            agent2.MakeOpenToChat();

            Assert.True(agent1.IsWorkingNow(_dateTimeService));
            Assert.True(agent1.IsAvailableToChat(_dateTimeService));

            Assert.False(agent2.IsWorkingNow(_dateTimeService));
            Assert.False(agent2.IsAvailableToChat(_dateTimeService));
        }
    }
}
