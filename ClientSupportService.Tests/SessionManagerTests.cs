using ClientSupportService.Interfaces;
using Moq;
using Serilog;

namespace ClientSupportService.Tests
{
    public class SessionManagerTests
    {
        private IDateTimeService _dateTimeService;
        private IDateTimeService _dateTimeService_SecondShift;
        private IDateTimeService _dateTimeService_ThirdShift;
        private ISessionStorage _sessionStorage;
        private ISessionStorage _sessionStorageFailedToCreate;
        private ISessionStorage _sessionStorageFailedToProlongate;
        private ILogger _logger;
        private IClientSupportServiceConfiguration _configuration;
        public SessionManagerTests()
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

            _logger = new Mock<ILogger>().Object;

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

            var sessionStorage = new Mock<ISessionStorage>();
            sessionStorage.Setup(p => p.CreateSessionAsync())
                .Returns(Task.FromResult<ClientSession?>(new ClientSession(Guid.NewGuid(), _dateTimeService.Now)));
            sessionStorage.Setup(p => p.ProlongateSessionAsync(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _sessionStorage = sessionStorage.Object;

            var sessionStorageFailedToCreateSession = new Mock<ISessionStorage>();
            sessionStorageFailedToCreateSession.Setup(p => p.CreateSessionAsync())
                .Returns(Task.FromResult<ClientSession?>(null));
            _sessionStorageFailedToCreate = sessionStorageFailedToCreateSession.Object;

            var sessionStorageFailedToProlongateSession = new Mock<ISessionStorage>();
            sessionStorageFailedToProlongateSession.Setup(p => p.ProlongateSessionAsync(Guid.NewGuid()))
                .Returns(Task.FromResult(false));
            _sessionStorageFailedToProlongate = sessionStorageFailedToProlongateSession.Object;
        }

        [Fact]
        public void SessionManager_CapacityOnFirstShift_Tests()
        {
            var sessionManager = new SessionManager(_sessionStorage, _dateTimeService, _logger, _configuration);
            Assert.Equal(12, sessionManager.Capacity);
            Assert.Equal(18, sessionManager.MaximumQueueSize);
        }

        [Fact]
        public void SessionManager_CapacityOnSecondShift_Tests()
        {
            var sessionManager = new SessionManager(_sessionStorage, _dateTimeService_SecondShift, _logger, _configuration);
            Assert.Equal(9, sessionManager.Capacity);
            Assert.Equal(13, sessionManager.MaximumQueueSize);
        }

        [Fact]
        public void SessionManager_CapacityOnThirdShift_Tests()
        {
            var sessionManager = new SessionManager(_sessionStorage, _dateTimeService_ThirdShift, _logger, _configuration);
            Assert.Equal(12, sessionManager.Capacity);
            Assert.Equal(18, sessionManager.MaximumQueueSize);
        }

        [Fact]
        public async Task SessionManager_CreateSession_Tests()
        {
            var sessionManager = new SessionManager(_sessionStorage, _dateTimeService, _logger, _configuration);

            var result = await sessionManager.CreateSessionAsync();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task SessionManager_FailedToCreateSession_Tests()
        {
            var sessionManager = new SessionManager(_sessionStorageFailedToCreate, _dateTimeService, _logger, _configuration);

            var result = await sessionManager.CreateSessionAsync();
            Assert.Null(result);
        }

        [Fact]
        public async Task SessionManager_FailedToProlongateStorageSessionNoClientSessions_Tests()
        {
            var sessionManager = new SessionManager(_sessionStorageFailedToProlongate, _dateTimeService, _logger, _configuration);
            
            var result = await sessionManager.ProlongateSessionAsync(Guid.NewGuid());
            Assert.False(result);
        }

        [Fact]
        public async Task SessionManager_FailedToProlongateStorageSessionExistingClientSessions_Tests()
        {
            var sessionManager = new SessionManager(_sessionStorageFailedToProlongate, _dateTimeService, _logger, _configuration);
            var session = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);
            sessionManager.AllocateSessionToAgent(session);

            var result = await sessionManager.ProlongateSessionAsync(session.SessionId);
            Assert.True(result);
        }

        [Fact]
        public async Task SessionManager_SucceedToProlongateStorageSession_Tests()
        {
            var sessionManager = new SessionManager(_sessionStorage, _dateTimeService, _logger, _configuration);
            
            var result = await sessionManager.ProlongateSessionAsync(Guid.NewGuid());
            Assert.True(result);
        }

        [Fact]
        public void SessionManager_DestroyAllocatedSessions_Tests()
        {
            var sessionManager = new SessionManager(_sessionStorage, _dateTimeService, _logger, _configuration);
            var session = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);
            sessionManager.AllocateSessionToAgent(session);

            var allocatedAgent = sessionManager.FindSessionAgent(session.SessionId);
            Assert.NotNull(allocatedAgent);
        
            sessionManager.DestroySession(session);
            allocatedAgent = sessionManager.FindSessionAgent(session.SessionId);
            Assert.Null(allocatedAgent);
        }

        [Fact]
        public void SessionManager_AllocationsFirstShift_Tests()
        {
            var sessionManager = new SessionManager(_sessionStorage, _dateTimeService, _logger, _configuration);
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
        }


        [Fact]
        public void SessionManager_AllocationsSecondShift_Tests()
        {
            var sessionManager = new SessionManager(_sessionStorage, _dateTimeService_SecondShift, _logger, _configuration);
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
        }

        [Fact]
        public void SessionManager_AllocationsThirdShift_Tests()
        {
            var sessionManager = new SessionManager(_sessionStorage, _dateTimeService_ThirdShift, _logger, _configuration);
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
        }
    }
}