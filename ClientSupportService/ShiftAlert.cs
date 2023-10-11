using ClientSupportService.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientSupportService
{
    public class ShiftAlert : IDisposable
    {
        private List<TimeOnly> _notificationTimes;
        private Action _callback;
        private CancellationTokenSource _cancellationToken;
        Task RunningTask { get; set; }
        public ShiftAlert(Action callback, IEnumerable<TimeOnly> notificationTimes, IDateTimeService dateTimeService)
        { 
            _callback = callback;
            _notificationTimes = notificationTimes.Distinct().ToList();
            _notificationTimes.Sort();
            _cancellationToken = new CancellationTokenSource();

            RunningTask = Task.Run(async () => {
                while (true)
                {
                    foreach (var notificationTime in notificationTimes)
                    {
                        if (dateTimeService.Now.Date.Add(notificationTime.ToTimeSpan()) < dateTimeService.Now)
                            continue;

                        var delay = dateTimeService.Now.Date.Add(notificationTime.ToTimeSpan()) - dateTimeService.Now;
                        await Task.Delay(delay, _cancellationToken.Token);
                        _callback();
                    }

                    await Task.Delay(dateTimeService.Now.Date.AddDays(1) - dateTimeService.Now, _cancellationToken.Token);
                }
            }, _cancellationToken.Token);
        }

        public void Dispose()
        {
            _cancellationToken.Cancel();
            _cancellationToken.Dispose();

            RunningTask.Dispose();
        }

        ~ShiftAlert() => Dispose();
    }
}
