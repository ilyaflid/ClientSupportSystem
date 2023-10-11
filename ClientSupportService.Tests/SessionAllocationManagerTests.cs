using Castle.Core.Configuration;
using ClientSupportService.Interfaces;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientSupportService.Tests
{
    public class SessionAllocationManagerTests
    {
        private IDateTimeService _dateTimeService;
        private IDateTimeService _dateTimeService_SecondShift;
        private IDateTimeService _dateTimeService_ThirdShift;
        private ILogger _logger;
        private IClientSupportServiceConfiguration _configuration;
        public SessionAllocationManagerTests()
        {
            var dateTimeService = new Mock<IDateTimeService>();
            dateTimeService.Setup(p => p.Now).Returns(new DateTime(2023, 10, 10, 10, 0, 0));

            _dateTimeService = dateTimeService.Object;

            var dateTimeServiceSecondShift = new Mock<IDateTimeService>();
            dateTimeServiceSecondShift.Setup(p => p.Now).Returns(new DateTime(2023, 10, 10, 18, 0, 0));

            _dateTimeService_SecondShift = dateTimeServiceSecondShift.Object;

            var dateTimeServiceThirdShift = new Mock<IDateTimeService>();
            dateTimeServiceThirdShift.Setup(p => p.Now).Returns(new DateTime(2023, 10, 10, 0, 0, 0));

            _dateTimeService_ThirdShift = dateTimeServiceThirdShift.Object;


            var configuration = new Mock<IClientSupportServiceConfiguration>();
            configuration.Setup(p => p.WorkingHoursStartTime).Returns(new TimeOnly(9, 0));
            configuration.Setup(p => p.WorkingHoursEndTime).Returns(new TimeOnly(17, 0));
            configuration.Setup(p => p.SessionTimeout).Returns(TimeSpan.FromSeconds(3));
            configuration.Setup(p => p.MaximumConcurrencyPerAgent).Returns(10);
            configuration.Setup(p => p.RegularAgents).Returns(new List<RegularAgent>() {
                new RegularAgent(10, new TimeOnly(7, 0), TimeSpan.FromHours(8), AgentLevel.Junior, "Team 1"),
                new RegularAgent(11, new TimeOnly(7, 0), TimeSpan.FromHours(8), AgentLevel.Senior, "Team 1"),
                new RegularAgent(12, new TimeOnly(15, 0), TimeSpan.FromHours(8), AgentLevel.Junior, "Team 2"),
                new RegularAgent(13, new TimeOnly(15, 0), TimeSpan.FromHours(8), AgentLevel.TeamLead, "Team 2"),
                new RegularAgent(14, new TimeOnly(23, 0), TimeSpan.FromHours(8), AgentLevel.Middle, "Team 3"),
                new RegularAgent(15, new TimeOnly(23, 0), TimeSpan.FromHours(8), AgentLevel.Middle, "Team 3"),
            });
            configuration.Setup(p => p.AdditionalAgents).Returns(new List<AdditionalAgent>() {
                new AdditionalAgent(100, new TimeOnly(9, 0), new TimeOnly(17, 0)),
                new AdditionalAgent(101, new TimeOnly(9, 0), new TimeOnly(17, 0)),
                new AdditionalAgent(102, new TimeOnly(9, 0), new TimeOnly(17, 0)),
                new AdditionalAgent(103, new TimeOnly(9, 0), new TimeOnly(17, 0))
            });
            _configuration = configuration.Object;

            _logger = new Mock<ILogger>().Object;
        }

        [Fact]
        public void SessionAllocationManager_CapacityOnFirstShift_Tests()
        {
            var sessionManager = new SessionAllocationManager(_dateTimeService, _configuration, _logger);
            Assert.Equal(12, sessionManager.Capacity);
        }

        [Fact]
        public void SessionAllocationManager_CapacityOnSecondShift_Tests()
        {
            var sessionManager = new SessionAllocationManager(_dateTimeService_SecondShift, _configuration, _logger);
            Assert.Equal(9, sessionManager.Capacity);
        }

        [Fact]
        public void SessionAllocationManager_CapacityOnThirdShift_Tests()
        {
            var sessionManager = new SessionAllocationManager(_dateTimeService_ThirdShift, _configuration, _logger);
            Assert.Equal(12, sessionManager.Capacity);
        }

        [Fact]
        public void SessionManager_DestroyAllocatedSessions_Tests()
        {
            var sessionManager = new SessionAllocationManager(_dateTimeService, _configuration, _logger);
            var session = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);
            sessionManager.AllocateSessionToAgent(session);

            var allocatedAgent = sessionManager.FindSessionAgent(session.SessionId);
            Assert.NotNull(allocatedAgent);

            sessionManager.DestroySession(session.SessionId);
            allocatedAgent = sessionManager.FindSessionAgent(session.SessionId);
            Assert.Null(allocatedAgent);
        }

        [Fact]
        public void SessionManager_AllocationsFirstShift_Tests()
        {
            var sessionManager = new SessionAllocationManager(_dateTimeService, _configuration, _logger);
            var session1 = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);
            var session2 = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);
            var session3 = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);
            var session4 = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);
            var session5 = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);
            var session6 = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);

            sessionManager.AllocateSessionToAgent(session1);
            sessionManager.AllocateSessionToAgent(session2);
            sessionManager.AllocateSessionToAgent(session3);
            sessionManager.AllocateSessionToAgent(session4);
            sessionManager.AllocateSessionToAgent(session5);
            sessionManager.AllocateSessionToAgent(session6);

            var allocatedAgent1 = sessionManager.FindSessionAgent(session1.SessionId);
            var allocatedAgent2 = sessionManager.FindSessionAgent(session2.SessionId);
            var allocatedAgent3 = sessionManager.FindSessionAgent(session3.SessionId);
            var allocatedAgent4 = sessionManager.FindSessionAgent(session4.SessionId);
            var allocatedAgent5 = sessionManager.FindSessionAgent(session5.SessionId);
            var allocatedAgent6 = sessionManager.FindSessionAgent(session6.SessionId);

            Assert.NotNull(allocatedAgent1);
            Assert.NotNull(allocatedAgent2);
            Assert.NotNull(allocatedAgent3);
            Assert.NotNull(allocatedAgent4);
            Assert.NotNull(allocatedAgent5);
            Assert.NotNull(allocatedAgent6);
            Assert.Equal(10, allocatedAgent1.Id);
            Assert.Equal(10, allocatedAgent2.Id);
            Assert.Equal(10, allocatedAgent3.Id);
            Assert.Equal(10, allocatedAgent4.Id);
            Assert.Equal(11, allocatedAgent5.Id);
            Assert.Equal(11, allocatedAgent6.Id);

            Assert.True(sessionManager.HasOpenSpots());
        }


        [Fact]
        public void SessionManager_AllocationsSecondShift_Tests()
        {
            var sessionManager = new SessionAllocationManager(_dateTimeService_SecondShift, _configuration, _logger);
            var session1 = new ClientSession(Guid.NewGuid(), _dateTimeService_SecondShift.Now);
            var session2 = new ClientSession(Guid.NewGuid(), _dateTimeService_SecondShift.Now);
            var session3 = new ClientSession(Guid.NewGuid(), _dateTimeService_SecondShift.Now);
            var session4 = new ClientSession(Guid.NewGuid(), _dateTimeService_SecondShift.Now);
            var session5 = new ClientSession(Guid.NewGuid(), _dateTimeService_SecondShift.Now);
            var session6 = new ClientSession(Guid.NewGuid(), _dateTimeService_SecondShift.Now);

            sessionManager.AllocateSessionToAgent(session1);
            sessionManager.AllocateSessionToAgent(session2);
            sessionManager.AllocateSessionToAgent(session3);
            sessionManager.AllocateSessionToAgent(session4);
            sessionManager.AllocateSessionToAgent(session5);
            sessionManager.AllocateSessionToAgent(session6);

            var allocatedAgent1 = sessionManager.FindSessionAgent(session1.SessionId);
            var allocatedAgent2 = sessionManager.FindSessionAgent(session2.SessionId);
            var allocatedAgent3 = sessionManager.FindSessionAgent(session3.SessionId);
            var allocatedAgent4 = sessionManager.FindSessionAgent(session4.SessionId);
            var allocatedAgent5 = sessionManager.FindSessionAgent(session5.SessionId);
            var allocatedAgent6 = sessionManager.FindSessionAgent(session6.SessionId);

            Assert.NotNull(allocatedAgent1);
            Assert.NotNull(allocatedAgent2);
            Assert.NotNull(allocatedAgent3);
            Assert.NotNull(allocatedAgent4);
            Assert.NotNull(allocatedAgent5);
            Assert.NotNull(allocatedAgent6);
            Assert.Equal(12, allocatedAgent1.Id);
            Assert.Equal(12, allocatedAgent2.Id);
            Assert.Equal(12, allocatedAgent3.Id);
            Assert.Equal(12, allocatedAgent4.Id);
            Assert.Equal(13, allocatedAgent5.Id);
            Assert.Equal(13, allocatedAgent6.Id);

            Assert.True(sessionManager.HasOpenSpots());
        }

        [Fact]
        public void SessionManager_AllocationsThirdShift_Tests()
        {
            var sessionManager = new SessionAllocationManager(_dateTimeService_ThirdShift, _configuration, _logger);
            var session1 = new ClientSession(Guid.NewGuid(), _dateTimeService_ThirdShift.Now);
            var session2 = new ClientSession(Guid.NewGuid(), _dateTimeService_ThirdShift.Now);
            var session3 = new ClientSession(Guid.NewGuid(), _dateTimeService_ThirdShift.Now);
            var session4 = new ClientSession(Guid.NewGuid(), _dateTimeService_ThirdShift.Now);
            var session5 = new ClientSession(Guid.NewGuid(), _dateTimeService_ThirdShift.Now);
            var session6 = new ClientSession(Guid.NewGuid(), _dateTimeService_ThirdShift.Now);

            sessionManager.AllocateSessionToAgent(session1);
            sessionManager.AllocateSessionToAgent(session2);
            sessionManager.AllocateSessionToAgent(session3);
            sessionManager.AllocateSessionToAgent(session4);
            sessionManager.AllocateSessionToAgent(session5);
            sessionManager.AllocateSessionToAgent(session6);

            var allocatedAgent1 = sessionManager.FindSessionAgent(session1.SessionId);
            var allocatedAgent2 = sessionManager.FindSessionAgent(session2.SessionId);
            var allocatedAgent3 = sessionManager.FindSessionAgent(session3.SessionId);
            var allocatedAgent4 = sessionManager.FindSessionAgent(session4.SessionId);
            var allocatedAgent5 = sessionManager.FindSessionAgent(session5.SessionId);
            var allocatedAgent6 = sessionManager.FindSessionAgent(session6.SessionId);

            Assert.NotNull(allocatedAgent1);
            Assert.NotNull(allocatedAgent2);
            Assert.NotNull(allocatedAgent3);
            Assert.NotNull(allocatedAgent4);
            Assert.NotNull(allocatedAgent5);
            Assert.NotNull(allocatedAgent6);
            Assert.Equal(14, allocatedAgent1.Id);
            Assert.Equal(15, allocatedAgent2.Id);
            Assert.Equal(14, allocatedAgent3.Id);
            Assert.Equal(15, allocatedAgent4.Id);
            Assert.Equal(14, allocatedAgent5.Id);
            Assert.Equal(15, allocatedAgent6.Id);

            Assert.True(sessionManager.HasOpenSpots());
        }

        [Fact]
        public void SessionManager_AllocationsFirstShift_Overflow_Tests()
        {
            var sessionManager = new SessionAllocationManager(_dateTimeService, _configuration, _logger);
            var session1 = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);
            var session2 = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);
            var session3 = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);
            var session4 = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);
            var session5 = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);
            var session6 = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);
            var session7 = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);
            var session8 = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);
            var session9 = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);
            var session10 = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);
            var session11 = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);
            var session12 = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);

            sessionManager.AllocateSessionToAgent(session1);
            sessionManager.AllocateSessionToAgent(session2);
            sessionManager.AllocateSessionToAgent(session3);
            sessionManager.AllocateSessionToAgent(session4);
            sessionManager.AllocateSessionToAgent(session5);
            sessionManager.AllocateSessionToAgent(session6);
            sessionManager.AllocateSessionToAgent(session7);
            sessionManager.AllocateSessionToAgent(session8);
            sessionManager.AllocateSessionToAgent(session9);
            sessionManager.AllocateSessionToAgent(session10);
            sessionManager.AllocateSessionToAgent(session11);
            sessionManager.AllocateSessionToAgent(session12);

            Assert.False(sessionManager.HasOpenSpots());

            sessionManager.KickAdditionalAgent();
            Assert.True(sessionManager.HasOpenSpots());

            var session13 = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);
            var session14 = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);

            sessionManager.AllocateSessionToAgent(session13);
            sessionManager.AllocateSessionToAgent(session14);
            
            sessionManager.RemoveAdditionalAgents();
            Assert.False(sessionManager.HasOpenSpots());
        }

        [Fact]
        public async Task SessionManager_OnShiftChanged_Tests()
        {
            var dateTimeService = new Mock<IDateTimeService>();
            dateTimeService.Setup(p => p.Now).Returns(new DateTime(2023, 10, 10, 14, 59, 59));

            var sessionManager = new SessionAllocationManager(dateTimeService.Object, _configuration, _logger);
            sessionManager.SetOnShiftChangedAction(() => {
                dateTimeService.Setup(p => p.Now).Returns(new DateTime(2023, 10, 10, 15, 00, 01));
            });

            var capacityBeforeUpdate = sessionManager.Capacity;
            await Task.Delay(TimeSpan.FromSeconds(5));
            var capacityAfterUpdate = sessionManager.Capacity;

            Assert.Equal(12, capacityBeforeUpdate);
            Assert.Equal(9, capacityAfterUpdate);
        }
    }
}
