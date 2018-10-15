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
    public class CountlyTestCases : IDisposable
    {
        ITestOutputHelper output;

        public int threadIterations = 10;
        int threadWaitStart = 100;
        int threadWaitEnd = 1000;
        int threadCount = 20;

        /// <summary>
        /// Test setup
        /// </summary>
        public CountlyTestCases(ITestOutputHelper output)
        {
            this.output = output;   
            Countly.StartSession(ServerInfo.serverURL, ServerInfo.appKey, ServerInfo.appVersion, FileSystem.Current).Wait();
        }

        /// <summary>
        /// Test cleanup
        /// </summary>
        public void Dispose()
        {
            Countly.EndSession().Wait();
        }

        [Fact]
        public async void BasicStorage()
        {
            const String filename = "SampleFilename.xml";

            List<String> sampleValues = new List<string>();
            sampleValues.Add("Book");
            sampleValues.Add("Car");

            await Storage.Instance.DeleteFile(filename);
            List<String> res1 = await Storage.Instance.LoadFromFile<List<String>>(filename);            
            Assert.Null(res1);

            await Storage.Instance.SaveToFile<List<String>>(filename, sampleValues);

            List<String> res2 = await Storage.Instance.LoadFromFile<List<String>>(filename);

            Assert.Equal(sampleValues, res2);
        }

        [Fact]
        public async void BasicDeviceID()
        {

        }        

        [Fact]
        public async void MultipleExceptions()
        {
            try
            {
                throw new Exception("This is some bad exception 3");
            }
            catch (Exception ex)
            {
                Dictionary<string, string> customInfo = new Dictionary<string, string>();
                customInfo.Add("customData", "importantStuff");
                await Countly.RecordException(ex.Message, ex.StackTrace, customInfo);
            }

            Exception exToUse;
            try
            {
                throw new Exception("This is some bad exception 35454");
            }
            catch (Exception ex)
            {
                exToUse = ex;
            }

            Dictionary<String, String> dict = new Dictionary<string, string>();
            dict.Add("booh", "waah");

            await Countly.RecordException("Big error 1");
            await Countly.RecordException(exToUse.Message, exToUse.StackTrace);
            await Countly.RecordException(exToUse.Message, exToUse.StackTrace, dict);
            await Countly.RecordException(exToUse.Message, exToUse.StackTrace, dict, false);
        }

        [Fact]
        public async void MultipleEvents()
        {
            await Countly.RecordEvent("Some event");
            await Countly.RecordEvent("Some event", 123);
            await Countly.RecordEvent("Some event", 123, 456);

            Segmentation segm = new Segmentation();
            segm.Add("oneKey", "SomeValue");
            segm.Add("anotherKey", "SomeOtherValue");

            await Countly.RecordEvent("Some event", 123, 456, segm);
        }

        
        void ThreadTest()
        {
            output.WriteLine("Threading test");
            List<Thread> threads = new List<Thread>();

            for (int a = 0; a < threadCount; a++)
            {
                threads.Add(new Thread(new ThreadStart(ThreadWorkEvents)));
                threads.Add(new Thread(new ThreadStart(ThreadWorkExceptions)));
            }


            for (int a = 0; a < threads.Count; a++)
            {
                threads[a].Start();
            }

            for (int a = 0; a < threads.Count; a++)
            {
                threads[a].Join();
            }

            output.WriteLine("Threading test is over.");
        }

        void ThreadWorkEvents()
        {
            String[] eventKeys = new string[] { "key_1", "key_2", "key_3", "key_4", "key_5", "key_6" };

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
                        Countly.RecordEvent(eventKeys[5], 2, 5.3, segm2);
                        break;
                    default:
                        break;
                }

                Thread.Sleep((new Random()).Next(threadWaitStart, threadWaitEnd));
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

                Thread.Sleep((new Random()).Next(threadWaitStart, threadWaitEnd));
            }
        }
    }
}
