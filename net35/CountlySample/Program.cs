using CountlySDK;
using CountlySDK.Entities;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static CountlySDK.CountlyCommon.CountlyBase;

namespace CountlySample
{
    class Program
    {
        const string serverURL = "http://try.count.ly";//put your server URL here
        const string appKey = "YOUR_APP_KEY";//put your server APP key here       

        public int threadIterations = 100;
        int threadWaitStart = 100;
        int threadWaitEnd = 1000;
        int threadCount = 20;

        static void Main(string[] args)
        {
            //to use TLS 1.2
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

            new Program().Run().GetAwaiter().GetResult();
        }

        public async Task Run()
        {
            Console.WriteLine("Hello to the Countly sample console program");

            Countly.IsLoggingEnabled = true;

            CountlyConfig countlyConfig = new CountlyConfig();
            countlyConfig.serverUrl = serverURL;
            countlyConfig.appKey = appKey;
            countlyConfig.appVersion = "123";

            await Countly.Instance.Init(countlyConfig);
            _ = Countly.Instance.SessionBegin();

            while (true) {
                Console.WriteLine("");
                Console.WriteLine("Choose your option:");
                Console.WriteLine("1) Record basic event");
                Console.WriteLine("2) Record event with segmentation");
                Console.WriteLine("3) Record event with sum and count");
                Console.WriteLine("4) Record event with sum and segmentation");
                Console.WriteLine("5) Record timed event with sum, count, duration and segmentation");

                Console.WriteLine("6) Record start view");
                Console.WriteLine("7) Record another view");

                Console.WriteLine("8) Set user profile");
                Console.WriteLine("9) Set user custom profile");

                Console.WriteLine("10) Set location");
                Console.WriteLine("11) Disable location");

                Console.WriteLine("12) Change deviceId with server merge");
                Console.WriteLine("13) Change deviceId without server merge");

                Console.WriteLine("14) Record an exception");
                Console.WriteLine("15) Record an exception with segmentation");

                Console.WriteLine("16) Device id type test");

                Console.WriteLine("17) Start a timed event");
                Console.WriteLine("18) End timed event");

                Console.WriteLine("19) Threading test");

                Console.WriteLine("0 Exit");

                string line = System.Console.ReadLine();

                int input = -1;
                try {
                    input = int.Parse(line);
                } catch (Exception ex) {
                }
                Console.WriteLine("");


                if (input == 1) {
                    _ = Countly.RecordEvent("Basic Event");
                } else if (input == 2) {
                    _ = Countly.RecordEvent("Event With Sum And Count", 2, 23);
                } else if (input == 3) {
                    Segmentation segment = new Segmentation();
                    segment.Add("Time Spent", "60");
                    segment.Add("Retry Attempts", "3");

                    _ = Countly.RecordEvent("Event With Count And Segment", 1, segment);

                } else if (input == 4) {
                    Segmentation segment = new Segmentation();
                    segment.Add("Time Spent", "60");
                    segment.Add("Retry Attempts", "3");

                    _ = Countly.RecordEvent("Event With Sum, Count And Segment", 1, 23, segment);

                } else if (input == 5) {
                    Segmentation segment = new Segmentation();
                    segment.Add("Time Spent", "60");
                    segment.Add("Retry Attempts", "3");

                    _ = Countly.RecordEvent("Event With Sum, Count, Duration And Segment", 1, 23, 12.0, segment);

                } else if (input == 8) {
                    Countly.UserDetails.Name = "full name";
                    Countly.UserDetails.Username = "username1";
                    Countly.UserDetails.Email = "test@count.ly";
                    Countly.UserDetails.Organization = "organization";
                    Countly.UserDetails.Phone = "000-111-000 ";
                    Countly.UserDetails.Gender = "Male";
                } else if (input == 9) {
                    CustomInfo customInfo = new CustomInfo();
                    customInfo.Add("Height", "5'10");
                    customInfo.Add("Weight", "79 kg");
                    customInfo.Add("Black", "Hair Color");

                    Countly.UserDetails.Custom = customInfo;
                    break;
                } else if (input == 6) {
                    Console.WriteLine("6");
                    _ = Countly.Instance.RecordView("Another view");
                } else if (input == 7) {
                    Console.WriteLine("7");
                    _ = Countly.Instance.RecordView("Start view");

                } else if (input == 10) {
                    _ = Countly.Instance.SetLocation("31.5204, 74.3587", "192.0.0.1", "PK", "Lahore");
                } else if (input == 11) {
                    _ = Countly.Instance.DisableLocation();
                } else if (input == 12) {
                    _ = Countly.Instance.ChangeDeviceId("new-device-id-1");
                } else if (input == 13) {
                    _ = Countly.Instance.ChangeDeviceId("new-device-id-2", true);
                } else if (input == 14) {

                    try {
                        throw new Exception("This is some bad exception");
                    } catch (Exception ex) {
                        _ = Countly.RecordException(ex.Message, ex.StackTrace);
                    }
                } else if (input == 15) {

                    try {
                        throw new Exception("Exception with segmentation");
                    } catch (Exception ex) {
                        Dictionary<string, string> customInfo = new Dictionary<string, string>();
                        customInfo.Add("customData", "importantStuff");
                        Countly.Instance.AddBreadCrumbs("test-breadcrum");
                        _ = Countly.RecordException(ex.Message, ex.StackTrace, customInfo);
                    }
                } else if (input == 16) {
                    DeviceIdType type = Countly.Instance.GetDeviceIDType();

                    if (type == DeviceIdType.SDKGenerated) {
                        await Countly.Instance.ChangeDeviceId("device id", true);
                    } else if (type == DeviceIdType.DeveloperProvided) {
                        await Countly.Instance.ChangeDeviceId("new device id", false);
                    }

                } else if (input == 17) {
                    Countly.Instance.StartEvent("timed event");
                } else if (input == 18) {
                    Segmentation segment = new Segmentation();
                    segment.Add("Time Spent", "60");
                    segment.Add("Retry Attempts", "5");

                    Countly.Instance.EndEvent("timed event", segment, 2, 10.0);
                } else if (input == 19) {
                    Console.WriteLine("==== Running threaded debug tests ====");
                    ThreadTest();
                } else if (input == 0) {
                    break;
                } else {
                    Console.WriteLine("Wrong input, please try again.");
                }
            };

            _ = Countly.Instance.SessionEnd();

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

            System.Console.WriteLine("==== Threading test is over. ====");
        }

        void ThreadWorkEvents()
        {
            string[] eventKeys = new string[] { "key_1", "key_2", "key_3", "key_4", "key_5", "key_6" };

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

            Dictionary<string, string> dict = new Dictionary<string, string>();
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
