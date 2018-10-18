using CountlySDK;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using Newtonsoft.Json;
using PCLStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace TestProject_common
{
    public class ThreadingTestCases : IDisposable
    {
        ITestOutputHelper output;

        //how many work iterations in each thread
        public int threadIterations = 5;

        //wait times in each thread after every iteration
        int threadWaitStart = 50;
        int threadWaitEnd = 200;

        //how many copies of thread actions
        int threadCountMultiplyer = 10;//Does every action so many times

        //used to start all threads at the same time
        ManualResetEvent threadSync;

        /// <summary>
        /// Test setup
        /// </summary>
        public ThreadingTestCases(ITestOutputHelper output)
        {
            this.output = output;
            threadSync = new ManualResetEvent(false);
            Storage.Instance.fileSystem = FileSystem.Current;
            Countly.Halt();
            TestHelper.CleanDataFiles();
            //Countly.Instance.deferUpload = true;

            Countly.StartSession(ServerInfo.serverURL, ServerInfo.appKey, ServerInfo.appVersion, FileSystem.Current).Wait();
        }

        /// <summary>
        /// Test cleanup
        /// </summary>
        public void Dispose()
        {
            TestHelper.ValidateDataPointUpload();
            Countly.EndSession().Wait();
        }

        [Fact]
        void ThreadTest()
        {
            Action[] threadActions = PrepareThreadActions();
            int[] sleepTimes = PrepareThreadSleepTimes(threadActions.Length * threadCountMultiplyer);


            List<Thread> threads = new List<Thread>();

            for (int a = 0; a < threadCountMultiplyer; a++)
            {
                for (int b = 0; b < threadActions.Length; b++)
                {
                    int idx = b + threadActions.Length * a;
                    int idxA = b;
                    threads.Add(new Thread(new ThreadStart(() =>
                    {
                        threadSync.WaitOne();
                        Thread.Sleep(sleepTimes[idx]);
                        threadActions[idxA]();
                    })));
                }
            }

            //start all threades
            for (int a = 0; a < threads.Count; a++)
            {
                threads[a].Start();
            }

            //signal all threads to start working

            threadSync.Set();

            //wait for all threads to finish
            for (int a = 0; a < threads.Count; a++)
            {
                threads[a].Join();
            }

            output.WriteLine("Threading test is over.");
        }

        Action[] PrepareThreadActions()
        {
            List<Action> actionsToDo = new List<Action>();

            actionsToDo.Add(ThreadWorkEvents);
            actionsToDo.Add(ThreadWorkExceptions);
            actionsToDo.Add(ThreadWorkUserDetails);

            return actionsToDo.ToArray();
        }        

        int[] PrepareThreadSleepTimes(int actionAmount)
        {
            int[] times = new int[actionAmount];

            Random rnd = new Random(100);

            for(int a = 0; a < times.Length; a++)
            {
                times[a] = rnd.Next(10, 200);
            }

            return times;
        }

        void ThreadWorkEvents()
        {
            String[] eventKeys = new string[] { "key_1", "key_2", "key_3", "key_4", "key_5", "key_6" };
            Random rnd = new Random(0);

            for (int a = 0; a < threadIterations; a++)
            {
                int choice = a % 5;

                switch (choice)
                {
                    case 0:
                        Countly.RecordEvent(eventKeys[0]);
                        break;
                    case 1:
                        Countly.RecordEvent(eventKeys[1], 3);
                        break;
                    case 2:
                        Countly.RecordEvent(eventKeys[2], 3, 4);
                        break;
                    case 3:
                        Segmentation segm = new Segmentation();
                        segm.Add("foo", "bar");
                        segm.Add("anti", "dote");
                        Countly.RecordEvent(eventKeys[3], 3, segm);
                        break;
                    case 4:
                        Segmentation segm2 = new Segmentation();
                        segm2.Add("what", "is");
                        segm2.Add("world", "ending");
                        Countly.RecordEvent(eventKeys[4], 3, 4.3, segm2);
                        break;
                    default:
                        break;
                }

                Thread.Sleep(rnd.Next(threadWaitStart, threadWaitEnd));
            }
        }

        void ThreadWorkExceptions()
        {
            Exception exToUse;
            try
            {
                throw new Exception("This is some bad exception 35454");
            }
            catch (Exception ex)
            {
                exToUse = ex;
            }

            Random rnd = new Random(1);

            Dictionary<String, String> dict = new Dictionary<string, string>();
            dict.Add("booh", "waah");


            for (int a = 0; a < threadIterations; a++)
            {
                int choice = a % 4;

                switch (choice)
                {
                    case 0:
                        Countly.RecordException("Big error 1");
                        break;
                    case 1:
                        Countly.RecordException(exToUse.Message, exToUse.StackTrace);
                        break;
                    case 2:
                        Countly.RecordException(exToUse.Message, exToUse.StackTrace, dict);
                        break;
                    case 3:
                        Countly.RecordException(exToUse.Message, exToUse.StackTrace, dict, false);
                        break;
                    default:
                        break;
                }

                Thread.Sleep(rnd.Next(threadWaitStart, threadWaitEnd));
            }
        }

        void ThreadWorkUserDetails()
        {
            Random rnd = new Random(2);

            for (int a = 0; a < threadIterations; a++)
            {
                CountlyUserDetails cud = Countly.UserDetails;
                TestHelper.PopulateCountlyUserDetails(cud, a, a);

                bool res = Countly.Instance.Upload().Result;

                Assert.True(res);
                Thread.Sleep(rnd.Next(threadWaitStart, threadWaitEnd));
            }
        }
    }
}
