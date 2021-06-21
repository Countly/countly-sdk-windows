using CountlySDK;
using CountlySDK.Entities;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CountlySample
{
    class Program
    {
        const String serverURL = "http://try.count.ly";//put your server URL here
        const String appKey = null;//put your server APP key here       
        const bool enableDebugOptions = false;
        public int threadIterations = 100;
        int threadWaitStart = 100;
        int threadWaitEnd = 1000;
        int threadCount = 20;

        static void Main(string[] args)
        {
            //to use TLS 1.2
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

            (new Program().Run()).GetAwaiter().GetResult();
        }

        public async Task Run()
        {
            Console.WriteLine("Hello to the Countly sample console program");

            if (serverURL == null || appKey == null) {
                Console.WriteLine("");
                Console.WriteLine("Problem encountered, you have not set up either the serverURL or the appKey");
                Console.ReadKey();
                return;
            }

            Countly.IsLoggingEnabled = true;
            //Countly.SetCustomDataPath(@"D:\123z\");//usable only when targeting .net3.5
            //Countly.SetCustomDataPath(null);

            CountlyConfig countlyConfig = new CountlyConfig();
            countlyConfig.serverUrl = serverURL;
            countlyConfig.appKey = appKey;
            countlyConfig.appVersion = "123";

            await Countly.Instance.Init(countlyConfig);
            await Countly.Instance.SessionBegin();

            /*
            //Countly.deferUpload = true;//this call is only available by allowing access to internal members to this project, should not be used
            await Countly.StartSession(serverURL, appKey, "1.234", Countly.DeviceIdMethod.multipleFields);
            */
            Console.WriteLine("DeviceID: " + await Countly.GetDeviceId());

            System.Console.WriteLine("DeviceID: " + await Countly.GetDeviceId());

            while (true) {
                Console.WriteLine("");
                Console.WriteLine("Choose your option:");
                Console.WriteLine("1) Sample event");
                Console.WriteLine("2) Sample caught exception");
                Console.WriteLine("3) Change deviceID to a random value (create new user)");
                Console.WriteLine("4) Change the name of the current user");
                Console.WriteLine("5) Exit");
                Console.WriteLine("6) Another caught Exception");
                Console.WriteLine("7) Some view");
                Console.WriteLine("8) Another view");
                Console.WriteLine("9) Test");

                if (enableDebugOptions) {
                    Console.WriteLine("8) (debug) Threading test");
                }


                ConsoleKeyInfo cki = System.Console.ReadKey();
                Console.WriteLine("");

                if (cki.Key == ConsoleKey.D1) {
                    System.Console.WriteLine("1");
                    Countly.RecordEvent("Some event");
                } else if (cki.Key == ConsoleKey.D2) {
                    Console.WriteLine("2");

                    try {
                        throw new Exception("This is some bad exception 3");
                    } catch (Exception ex) {
                        Dictionary<string, string> customInfo = new Dictionary<string, string>();
                        customInfo.Add("customData", "importantStuff");
                        Countly.RecordException(ex.Message, ex.StackTrace, customInfo);
                    }
                } else if (cki.Key == ConsoleKey.D3) {
                    Console.WriteLine("3");
                    //await Device.SetDeviceId("ID-" + (new Random()).Next());
                } else if (cki.Key == ConsoleKey.D4) {
                    Console.WriteLine("4");
                    Countly.UserDetails.Name = "Some Username " + (new Random()).Next();
                } else if (cki.Key == ConsoleKey.D5) {
                    Console.WriteLine("5");
                    break;
                } else if (cki.Key == ConsoleKey.D6) {
                    Console.WriteLine("6");
                    await Countly.RecordException("What is here", "");
                } else if (cki.Key == ConsoleKey.D7) {
                    Console.WriteLine("7");
                    await Countly.Instance.RecordView("Some view Name");

                } else if (cki.Key == ConsoleKey.D8) {
                    Console.WriteLine("8");
                    await Countly.Instance.RecordView("Another view Name");

                } else if (enableDebugOptions && cki.Key == ConsoleKey.D9) {
                    Console.WriteLine("8");
                    Console.WriteLine("Running threaded debug test");
                    ThreadTest();
                } else {
                    Console.WriteLine("Wrong input, please try again.");
                }
            };

            await Countly.Instance.SessionEnd();
        }



        void ThreadTest()
        {
            List<Thread> threads = new List<Thread>();

            for (int a = 0; a < threadCount; a++) {
                threads.Add(new Thread(new ThreadStart(ThreadWorkEvents)));
                threads.Add(new Thread(new ThreadStart(ThreadWorkExceptions)));
            }


            for (int a = 0; a < threads.Count; a++) {
                threads[a].Start();
            }

            for (int a = 0; a < threads.Count; a++) {
                threads[a].Join();
            }

            System.Console.WriteLine("Threading test is over.");
        }

        void ThreadWorkEvents()
        {
            String[] eventKeys = new string[] { "key_1", "key_2", "key_3", "key_4", "key_5", "key_6" };

            for (int a = 0; a < threadIterations; a++) {
                int choice = a % 5;

                switch (choice) {
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
            try {
                throw new Exception("This is some bad exception 35454");
            } catch (Exception ex) {
                exToUse = ex;
            }

            Dictionary<String, String> dict = new Dictionary<string, string>();
            dict.Add("booh", "waah");


            for (int a = 0; a < threadIterations; a++) {
                int choice = a % 4;

                switch (choice) {
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
