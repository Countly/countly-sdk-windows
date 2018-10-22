using CountlySDK;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace TestProject_common
{
    public class ThreadingTestCases : IDisposable
    {
        //how many work iterations in each thread
        public int threadIterations = 10;

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
        public ThreadingTestCases()
        {
            threadSync = new ManualResetEvent(false);
            CountlyImpl.SetPCLStorageIfNeeded();
            Countly.Halt();
            TestHelper.CleanDataFiles();
            //Countly.Instance.deferUpload = true;

            CountlyConfig cc = TestHelper.CreateConfig();
            Countly.Instance.Init(cc).Wait();
            Countly.Instance.SessionBegin().Wait();
        }

        /// <summary>
        /// Test cleanup
        /// </summary>
        public void Dispose()
        {
            TestHelper.ValidateDataPointUpload().Wait();
            Countly.Instance.SessionEnd().Wait();
            TestHelper.ValidateDataPointUpload().Wait();
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
        }

        Action[] PrepareThreadActions()
        {
            List<Action> actionsToDo = new List<Action>();

            actionsToDo.Add(ThreadWorkEvents);
            actionsToDo.Add(ThreadWorkExceptions);
            actionsToDo.Add(ThreadWorkUserDetails);
            actionsToDo.Add(ThreadWorkMergeDeviceId);
            actionsToDo.Add(ThreadWorkSetLocation);

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
                int choice = a % 6;

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
                    case 5:
                        Segmentation segm3 = new Segmentation();
                        segm3.Add("what3", "is");
                        segm3.Add("world2", "ending");
                        Countly.RecordEvent(eventKeys[4], 3, 4.3, 6.7, segm3);
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
                bool res;

                switch (choice)
                {
                    case 0:
                        res = Countly.RecordException("Big error 1").Result;
                        break;
                    case 1:
                        res = Countly.RecordException(exToUse.Message, exToUse.StackTrace).Result;
                        break;
                    case 2:
                        res = Countly.RecordException(exToUse.Message, exToUse.StackTrace, dict).Result;
                        break;
                    case 3:
                    default:
                        res = Countly.RecordException(exToUse.Message, exToUse.StackTrace, dict, false).Result;
                        break;
                }

                Assert.True(res);
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

        void ThreadWorkMergeDeviceId()
        {
            Random rnd = new Random(2);
            Api.Instance.DeviceMergeWaitTime = 500;
            for (int a = 0; a < threadIterations; a++)
            {
                String deviceId = "SDSDSD" + rnd.Next();

                switch (a % 2)
                {
                    case 0:
                        Countly.Instance.ChangeDeviceId(deviceId, false);
                        break;
                    case 1:
                        Countly.Instance.ChangeDeviceId(deviceId, true);
                        break;
                }

                Thread.Sleep(rnd.Next(threadWaitStart, threadWaitEnd));
            }
        }

        void ThreadWorkSetLocation()
        {
            Random rnd = new Random(2);
            for (int a = 0; a < threadIterations; a++)
            {
                bool res;

                switch (a % 2)
                {
                    case 0:
                        res = Countly.Instance.SetLocation("fdsf").Result;
                        break;
                    case 1:
                    default:
                        res = Countly.Instance.DisableLocation().Result;
                        break;
                }

                Assert.True(res);
                Thread.Sleep(rnd.Next(threadWaitStart, threadWaitEnd));
            }
        }
    }
}
