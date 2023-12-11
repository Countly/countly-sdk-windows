using System;
using System.Threading;
using System.Threading.Tasks;

namespace CountlySDK.Helpers
{
    public delegate void TimerCallback(object state, EventArgs args);

    /// <summary>
    /// Used in place of DispatchTimer
    /// </summary>
    public sealed class TimerHelper : CancellationTokenSource, IDisposable
    {
        public TimerHelper(TimerCallback callback, object state, int dueTime, int period)
        {
            Task.Delay(dueTime, Token).ContinueWith(async (t, s) => {
                var tuple = (Tuple<TimerCallback, object>)s;

                while (true) {
                    if (IsCancellationRequested) {
                        break;
                    }

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
