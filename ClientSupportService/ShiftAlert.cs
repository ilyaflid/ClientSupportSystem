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
        public ShiftAlert(Action callback, IEnumerable<TimeOnly> notificationTimes)
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
                        if (DateTime.Now.Date.Add(notificationTime.ToTimeSpan()) < DateTime.Now)
                            continue;

                        var delay = DateTime.Now.Date.Add(notificationTime.ToTimeSpan()) - DateTime.Now;
                        await Task.Delay(delay, _cancellationToken.Token);
                        _callback();
                    }

                    await Task.Delay(DateTime.Now.Date.AddDays(1) - DateTime.Now, _cancellationToken.Token);
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
