using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CountlySDK.Helpers;
using Xunit;

namespace TestProject_common
{
    public class TimeHelperTests
    {
        /// <summary>
        /// GetUniqueUnixTime must hand out a strictly-unique value on every call, even when
        /// hammered from many threads at once. The counter is a read-modify-write on a shared
        /// field, so without synchronization two threads can observe the same value and both
        /// return it. This drives concurrent callers hard and asserts every value is unique.
        /// </summary>
        [Fact]
        public void GetUniqueUnixTime_UnderConcurrency_ReturnsAllUniqueValues()
        {
            TimeHelper timeHelper = new TimeHelper();
            const int threadCount = 8;
            const int perThread = 20000;

            ConcurrentQueue<long> results = new ConcurrentQueue<long>();

            using (Barrier barrier = new Barrier(threadCount)) {
                Task[] tasks = new Task[threadCount];
                for (int t = 0; t < threadCount; t++) {
                    tasks[t] = Task.Run(() => {
                        barrier.SignalAndWait();
                        for (int i = 0; i < perThread; i++) {
                            results.Enqueue(timeHelper.GetUniqueUnixTime());
                        }
                    });
                }

                Task.WaitAll(tasks);
            }

            int total = threadCount * perThread;
            int distinct = results.Distinct().Count();
            Assert.Equal(total, distinct);
        }
    }
}
