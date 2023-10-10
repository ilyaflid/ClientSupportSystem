using ClientSupportService.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientSupportService.Tests
{
    public class AgentQueueTests
    {
        private IDateTimeService _dateTimeService;
        public AgentQueueTests()
        {
            var dateTimeService = new Mock<IDateTimeService>();
            dateTimeService.Setup(p => p.Now).Returns(new DateTime(2023, 10, 10, 10, 0, 0));

            _dateTimeService = dateTimeService.Object;
        }

        [Fact]
        public void SimpleAgentQueue_Tests()
        {
            var agentJunior = new RegularAgent(1000, TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)), TimeSpan.FromHours(8), AgentLevel.Junior, "Team 1");
            var agentQueue_junior = new AgentQueue(agentJunior, 10);
            var agentMiddle = new RegularAgent(1000, TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)), TimeSpan.FromHours(8), AgentLevel.Middle, "Team 1");
            var agentQueue_middle = new AgentQueue(agentMiddle, 10);
            var agentSenior = new RegularAgent(1000, TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)), TimeSpan.FromHours(8), AgentLevel.Senior, "Team 1");
            var agentQueue_senior = new AgentQueue(agentSenior, 10);
            var agentLead = new RegularAgent(1000, TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)), TimeSpan.FromHours(8), AgentLevel.TeamLead, "Team 1");
            var agentQueue_lead = new AgentQueue(agentLead, 10);

            Assert.Equal(0, agentQueue_junior.QueueSize);
            Assert.True(agentQueue_junior.HasOpenSpots);
            Assert.Equal(4, agentQueue_junior.NumberOfOpenSpots);

            Assert.Equal(0, agentQueue_middle.QueueSize);
            Assert.True(agentQueue_middle.HasOpenSpots);
            Assert.Equal(6, agentQueue_middle.NumberOfOpenSpots);

            Assert.Equal(0, agentQueue_senior.QueueSize);
            Assert.True(agentQueue_senior.HasOpenSpots);
            Assert.Equal(8, agentQueue_senior.NumberOfOpenSpots);

            Assert.Equal(0, agentQueue_lead.QueueSize);
            Assert.True(agentQueue_lead.HasOpenSpots);
            Assert.Equal(5, agentQueue_lead.NumberOfOpenSpots);
        }

        [Fact]
        public void AgentQueueAdd_Tests()
        {
            var agentJunior = new RegularAgent(1000, TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)), TimeSpan.FromHours(8), AgentLevel.Junior, "Team 1");
            var agentQueue_junior = new AgentQueue(agentJunior, 10);

            var session1Id = Guid.NewGuid();
            var session1 = new ClientSession(session1Id, _dateTimeService.Now);
            agentQueue_junior.AddSession(session1);

            var session2Id = Guid.NewGuid();
            var session2 = new ClientSession(session2Id, _dateTimeService.Now);
            agentQueue_junior.AddSession(session2);

            Assert.Equal(2, agentQueue_junior.QueueSize);
            Assert.True(agentQueue_junior.HasOpenSpots);
            Assert.Equal(2, agentQueue_junior.NumberOfOpenSpots);
        }

        [Fact]
        public void AgentQueueAddExistingSession_Tests()
        {
            var agentJunior = new RegularAgent(1000, TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)), TimeSpan.FromHours(8), AgentLevel.Junior, "Team 1");
            var agentQueue_junior = new AgentQueue(agentJunior, 10);

            var session1Id = Guid.NewGuid();
            var session1 = new ClientSession(session1Id, _dateTimeService.Now);
            agentQueue_junior.AddSession(session1);
            agentQueue_junior.AddSession(session1);

            Assert.Equal(1, agentQueue_junior.QueueSize);
            Assert.True(agentQueue_junior.HasOpenSpots);
            Assert.Equal(3, agentQueue_junior.NumberOfOpenSpots);
        }

        [Fact]
        public void AgentQueueAddTooManySessions_Tests()
        {
            var agentJunior = new RegularAgent(1000, TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)), TimeSpan.FromHours(8), AgentLevel.Junior, "Team 1");
            var agentQueue_junior = new AgentQueue(agentJunior, 10);

            var session1Id = Guid.NewGuid();
            var session1 = new ClientSession(session1Id, _dateTimeService.Now);
            agentQueue_junior.AddSession(session1);

            var session2Id = Guid.NewGuid();
            var session2 = new ClientSession(session2Id, _dateTimeService.Now);
            agentQueue_junior.AddSession(session2);

            var session3Id = Guid.NewGuid();
            var session3 = new ClientSession(session3Id, _dateTimeService.Now);
            agentQueue_junior.AddSession(session3);

            var session4Id = Guid.NewGuid();
            var session4 = new ClientSession(session4Id, _dateTimeService.Now);
            agentQueue_junior.AddSession(session4);

            var session5Id = Guid.NewGuid();
            var session5 = new ClientSession(session5Id, _dateTimeService.Now);
            agentQueue_junior.AddSession(session5);

            Assert.Equal(4, agentQueue_junior.QueueSize);
            Assert.False(agentQueue_junior.HasOpenSpots);
            Assert.Equal(0, agentQueue_junior.NumberOfOpenSpots);
        }

        [Fact]
        public void AgentQueueRemoveSession_Tests()
        {
            var agentJunior = new RegularAgent(1000, TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)), TimeSpan.FromHours(8), AgentLevel.Junior, "Team 1");
            var agentQueue_junior = new AgentQueue(agentJunior, 10);

            var session1Id = Guid.NewGuid();
            var session1 = new ClientSession(session1Id, _dateTimeService.Now);
            agentQueue_junior.AddSession(session1);

            agentQueue_junior.RemoveSession(session1);

            Assert.Equal(0, agentQueue_junior.QueueSize);
            Assert.True(agentQueue_junior.HasOpenSpots);
            Assert.Equal(4, agentQueue_junior.NumberOfOpenSpots);
        }

        [Fact]
        public void AgentQueueRemoveNonExistingSession_Tests()
        {
            var agentJunior = new RegularAgent(1000, TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)), TimeSpan.FromHours(8), AgentLevel.Junior, "Team 1");
            var agentQueue_junior = new AgentQueue(agentJunior, 10);

            var session1Id = Guid.NewGuid();
            var session1 = new ClientSession(session1Id, _dateTimeService.Now);
            agentQueue_junior.AddSession(session1);

            var session2Id = Guid.NewGuid();
            var session2 = new ClientSession(session2Id, _dateTimeService.Now);

            agentQueue_junior.RemoveSession(session2);

            Assert.Equal(1, agentQueue_junior.QueueSize);
            Assert.True(agentQueue_junior.HasOpenSpots);
            Assert.Equal(3, agentQueue_junior.NumberOfOpenSpots);
        }
    }
}
