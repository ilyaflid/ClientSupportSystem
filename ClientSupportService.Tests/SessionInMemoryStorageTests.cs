using ClientSupportService.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientSupportService.Tests
{
    public class SessionInMemoryStorageTests
    {
        private IDateTimeService _dateTimeService;
        public SessionInMemoryStorageTests()
        {
            var dateTimeService = new Mock<IDateTimeService>();
            dateTimeService.Setup(p => p.Now).Returns(new DateTime(2023, 10, 10, 10, 0, 0));

            _dateTimeService = dateTimeService.Object;
        }

        [Fact]
        public async Task SessionInMemoryStorage_CreateOperations_Tests()
        {
            var sessionStorage = new SessionInMemoryStorage(_dateTimeService);
            sessionStorage.SetMaximumQueueSize(3);

            var session1 = await sessionStorage.CreateSessionAsync();
            var session2 = await sessionStorage.CreateSessionAsync();
            var session3 = await sessionStorage.CreateSessionAsync();
            var session4 = await sessionStorage.CreateSessionAsync();

            Assert.NotNull(session1);
            Assert.NotNull(session2);
            Assert.NotNull(session3);
            Assert.Null(session4);
            Assert.Equal(3, sessionStorage.GetQueueSize());
        }

        [Fact]
        public async Task SessionInMemoryStorage_PopOperations_Tests()
        {
            var sessionStorage = new SessionInMemoryStorage(_dateTimeService);
            sessionStorage.SetMaximumQueueSize(3);

            var session1 = await sessionStorage.CreateSessionAsync();
            var session2 = await sessionStorage.CreateSessionAsync();
            var session3 = await sessionStorage.CreateSessionAsync();

            var popSession1 = await sessionStorage.PopFirstSessionInQueueAsync();
            var popSession2 = await sessionStorage.PopFirstSessionInQueueAsync();
            var popSession3 = await sessionStorage.PopFirstSessionInQueueAsync();
            var popSession4 = await sessionStorage.PopFirstSessionInQueueAsync();
            Assert.NotNull(popSession1);
            Assert.Equal(session1?.SessionId, popSession1.SessionId);
            Assert.NotNull(popSession2);
            Assert.Equal(session2?.SessionId, popSession2.SessionId);
            Assert.NotNull(popSession3);
            Assert.Equal(session3?.SessionId, popSession3.SessionId);
            Assert.Null(popSession4);
        }

        [Fact]
        public async Task SessionInMemoryStorage_ProlongateOperations_Tests()
        {
            var sessionStorage = new SessionInMemoryStorage(_dateTimeService);
            sessionStorage.SetMaximumQueueSize(3);

            var session1 = await sessionStorage.CreateSessionAsync();

            var result1 = await sessionStorage.ProlongateSessionAsync(session1.SessionId);
            var result2 = await sessionStorage.ProlongateSessionAsync(Guid.NewGuid());

            Assert.True(result1);
            Assert.False(result2);
            Assert.Equal(1, sessionStorage.GetQueueSize());
        }

        [Fact]
        public async Task SessionInMemoryStorage_RemoveOperations_Tests()
        {
            var sessionStorage = new SessionInMemoryStorage(_dateTimeService);
            sessionStorage.SetMaximumQueueSize(3);

            var session1 = await sessionStorage.CreateSessionAsync();

            var result1 = await sessionStorage.RemoveSessionAsync(session1.SessionId);
            var result2 = await sessionStorage.RemoveSessionAsync(Guid.NewGuid());

            Assert.True(result1);
            Assert.False(result2);
            Assert.Equal(0, sessionStorage.GetQueueSize());
        }

        [Fact]
        public async Task SessionInMemoryStorage_RemoveExpiredSessionsOperations_Tests()
        {
            var sessionStorage = new SessionInMemoryStorage(_dateTimeService);
            sessionStorage.SetMaximumQueueSize(3);

            await sessionStorage.CreateSessionAsync();
            await sessionStorage.CreateSessionAsync();
            await sessionStorage.CreateSessionAsync();

            var result1 = await sessionStorage.RemoveExpiredSessionsAsync(TimeSpan.FromMinutes(1));
            Assert.False(result1);
            Assert.Equal(3, sessionStorage.GetQueueSize());

            var result2 = await sessionStorage.RemoveExpiredSessionsAsync(TimeSpan.FromSeconds(-1));

            Assert.True(result2);
            Assert.Equal(0, sessionStorage.GetQueueSize());
        }
    }
}
