using ClientSupportService.Interfaces;
using Moq;
using Serilog;
using Xunit;

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
            sessionStorage.Setup(p => p.RemoveSessionAsync(It.IsAny<Guid>())).Returns(Task.FromResult(true));
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
        public void SessionManager_Capacity_Tests()
        {
            var allocationManager = new Mock<ISessionAllocationManager>();
            allocationManager.Setup(p => p.Capacity).Returns(12);
            allocationManager.Setup(p => p.SetOnShiftChangedAction(It.IsAny<Action>())).Returns(allocationManager.Object);

            var allocationManager2 = new Mock<ISessionAllocationManager>();
            allocationManager2.Setup(p => p.Capacity).Returns(11);
            allocationManager2.Setup(p => p.SetOnShiftChangedAction(It.IsAny<Action>())).Returns(allocationManager2.Object);

            var sessionManager = new SessionManager(_sessionStorage, allocationManager.Object, _dateTimeService, _logger, _configuration);
            var sessionManager2 = new SessionManager(_sessionStorage, allocationManager2.Object, _dateTimeService, _logger, _configuration);
            
            Assert.Equal(12, sessionManager.Capacity);
            Assert.Equal(18, sessionManager.MaximumQueueSize);

            Assert.Equal(11, sessionManager2.Capacity);
            Assert.Equal(16, sessionManager2.MaximumQueueSize);
        }

        [Fact]
        public async Task SessionManager_CreateSession_Tests()
        {
            var allocationManager = new Mock<ISessionAllocationManager>();
            allocationManager.Setup(p => p.Capacity).Returns(12);
            allocationManager.Setup(p => p.SetOnShiftChangedAction(It.IsAny<Action>())).Returns(allocationManager.Object);

            var sessionManager = new SessionManager(_sessionStorage, allocationManager.Object, _dateTimeService, _logger, _configuration);

            var result = await sessionManager.CreateSessionAsync();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task SessionManager_FailedToCreateSession_Tests()
        {
            var allocationManager = new Mock<ISessionAllocationManager>();
            allocationManager.Setup(p => p.Capacity).Returns(12);
            allocationManager.Setup(p => p.SetOnShiftChangedAction(It.IsAny<Action>())).Returns(allocationManager.Object);

            var sessionManager = new SessionManager(_sessionStorageFailedToCreate, allocationManager.Object, _dateTimeService, _logger, _configuration);

            var result = await sessionManager.CreateSessionAsync();
            Assert.Null(result);
        }

        [Fact]
        public async Task SessionManager_FailedToProlongateStorageSessionNoClientSessions_Tests()
        {
            var allocationManager = new Mock<ISessionAllocationManager>();
            allocationManager.Setup(p => p.Capacity).Returns(12);
            allocationManager.Setup(p => p.SetOnShiftChangedAction(It.IsAny<Action>())).Returns(allocationManager.Object);

            var sessionManager = new SessionManager(_sessionStorageFailedToProlongate, allocationManager.Object, _dateTimeService, _logger, _configuration);
            
            var result = await sessionManager.ProlongateSessionAsync(Guid.NewGuid());
            Assert.False(result);
        }

        [Fact]
        public async Task SessionManager_FailedToProlongateStorageSessionExistingClientSessions_Tests()
        {
            var allocationManager = new Mock<ISessionAllocationManager>();
            allocationManager.Setup(p => p.Capacity).Returns(12);
            allocationManager.Setup(p => p.SetOnShiftChangedAction(It.IsAny<Action>())).Returns(allocationManager.Object);

            var sessionManager = new SessionManager(_sessionStorageFailedToProlongate, allocationManager.Object, _dateTimeService, _logger, _configuration);
            var session = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);
            sessionManager.AllocateSessionToAgent(session);

            var result = await sessionManager.ProlongateSessionAsync(session.SessionId);
            Assert.True(result);
        }

        [Fact]
        public async Task SessionManager_SucceedToProlongateStorageSession_Tests()
        {
            var allocationManager = new Mock<ISessionAllocationManager>();
            allocationManager.Setup(p => p.Capacity).Returns(12);
            allocationManager.Setup(p => p.SetOnShiftChangedAction(It.IsAny<Action>())).Returns(allocationManager.Object);

            var sessionManager = new SessionManager(_sessionStorage, allocationManager.Object, _dateTimeService,  _logger, _configuration);
            
            var result = await sessionManager.ProlongateSessionAsync(Guid.NewGuid());
            Assert.True(result);
        }

        [Fact]
        public async Task SessionManager_SucceedToRemoveSessionFromStorage_Tests()
        {
            var allocationManager = new Mock<ISessionAllocationManager>();
            allocationManager.Setup(p => p.Capacity).Returns(12);
            allocationManager.Setup(p => p.SetOnShiftChangedAction(It.IsAny<Action>())).Returns(allocationManager.Object);

            var sessionManager = new SessionManager(_sessionStorage, allocationManager.Object, _dateTimeService, _logger, _configuration);
            var session = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);
            sessionManager.AllocateSessionToAgent(session);

            var result = await sessionManager.DestroySessionAsync(session);
            Assert.True(result);
        }

        [Fact]
        public async Task SessionManager_SucceedToRemoveAllocatedSession_Tests()
        {
            var sessionStorageFailedToRemoveSession = new Mock<ISessionStorage>();
            sessionStorageFailedToRemoveSession.Setup(p => p.RemoveSessionAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult(true));

            var allocationManager = new Mock<ISessionAllocationManager>();
            allocationManager.Setup(p => p.Capacity).Returns(12);
            allocationManager.Setup(p => p.SetOnShiftChangedAction(It.IsAny<Action>())).Returns(allocationManager.Object);

            var sessionManager = new SessionManager(sessionStorageFailedToRemoveSession.Object, allocationManager.Object, _dateTimeService, _logger, _configuration);
            var session = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);
            sessionManager.AllocateSessionToAgent(session);

            var result = await sessionManager.DestroySessionAsync(session);
            Assert.True(result);
        }

        [Fact]
        public async Task SessionManager_FailedToRemoveAllocatedSession_Tests()
        {
            var sessionStorageFailedToRemoveSession = new Mock<ISessionStorage>();
            sessionStorageFailedToRemoveSession.Setup(p => p.RemoveSessionAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult(false));

            var allocationManager = new Mock<ISessionAllocationManager>();
            allocationManager.Setup(p => p.Capacity).Returns(12);
            allocationManager.Setup(p => p.SetOnShiftChangedAction(It.IsAny<Action>())).Returns(allocationManager.Object);

            var sessionManager = new SessionManager(sessionStorageFailedToRemoveSession.Object, allocationManager.Object, _dateTimeService, _logger, _configuration);
            var session = new ClientSession(Guid.NewGuid(), _dateTimeService.Now);

            var result = await sessionManager.DestroySessionAsync(session);
            Assert.False(result);
        }
    }
}