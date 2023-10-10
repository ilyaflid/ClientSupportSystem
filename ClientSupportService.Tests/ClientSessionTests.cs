using ClientSupportService.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientSupportService.Tests
{
    public class ClientSessionTests
    {
        private IDateTimeService _dateTimeService1;
        private IDateTimeService _dateTimeService2;
        private IDateTimeService _dateTimeService3;
        public ClientSessionTests()
        {
            var dateTimeService1 = new Mock<IDateTimeService>();
            dateTimeService1.Setup(p => p.Now).Returns(new DateTime(2023, 10, 10, 10, 0, 0));

            _dateTimeService1 = dateTimeService1.Object;

            var dateTimeService2 = new Mock<IDateTimeService>();
            dateTimeService2.Setup(p => p.Now).Returns(new DateTime(2023, 10, 10, 10, 1, 0));

            _dateTimeService2 = dateTimeService2.Object;

            var dateTimeService3 = new Mock<IDateTimeService>();
            dateTimeService3.Setup(p => p.Now).Returns(new DateTime(2023, 10, 10, 10, 4, 0));

            _dateTimeService3 = dateTimeService3.Object;
        }

        [Fact]
        public void SimpleClientSession_Tests()
        {
            var session = new ClientSession(Guid.NewGuid(), _dateTimeService1.Now);
            
            Assert.True(session.IsExpired(TimeSpan.FromSeconds(30), _dateTimeService2.Now));
            Assert.False(session.IsExpired(TimeSpan.FromMinutes(2), _dateTimeService2.Now));

            session.Prolongate(_dateTimeService2.Now);
            Assert.False(session.IsExpired(TimeSpan.FromSeconds(30), _dateTimeService2.Now));
            Assert.False(session.IsExpired(TimeSpan.FromMinutes(2), _dateTimeService2.Now));

            Assert.True(session.IsExpired(TimeSpan.FromSeconds(30), _dateTimeService3.Now));
            Assert.True(session.IsExpired(TimeSpan.FromMinutes(2), _dateTimeService3.Now));
        }
    }
}
