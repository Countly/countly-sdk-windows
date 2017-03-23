using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CountlySDK.Helpers
{
    internal delegate void TimerCallback(object state, EventArgs args);

    internal sealed class TimerHelper : CancellationTokenSource, IDisposable
    {
        public TimerHelper(TimerCallback callback, object state, int dueTime, int period)
        {
            Task.Delay(dueTime, Token).ContinueWith(async (t, s) =>
            {
                var tuple = (Tuple<TimerCallback, object>)s;

                while (true)
                {
                    if (IsCancellationRequested)
                        break;
                    Task task = Task.Run(() => tuple.Item1(tuple.Item2, EventArgs.Empty));
                    await Task.Delay(period);
                }

            }, Tuple.Create(callback, state), CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.Default);
        }

        public new void Dispose() { base.Cancel(); }
    }
}
