// Uncomment this so the library can make use of Windows Phone 8 API's
// Note: You will also have to upgrade the library to Windows Phone 8,
// Just right click on the project in visual studio and go Upgrade To
// Windows Phone 8.0

//#define WP8

using System;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using Microsoft.Phone.Info;
using Microsoft.Phone.Net.NetworkInformation;
using System.Globalization;
using System.Windows;

using OpenUDIDPhone;

// Please see http://support.count.ly/kb/reference/countly-server-api-reference

namespace Countly
{
    public class Countly
    {
        private static Countly sharedInstance;
        private Timer timer;
        private ConnectionQueue queue;
        private bool isVisible;
        private double unsentSessionLength;
        private double previousTime;
        private DateTime startTime = DateTime.Now;
        private List<CountlyEvent> eventqueue;

        public static Countly SharedInstance()
        {
            if (null == sharedInstance)
            {
                sharedInstance = new Countly();
            }

            return sharedInstance;
        }

        private Countly()
        {
            queue = new ConnectionQueue();
            eventqueue = new List<CountlyEvent>();
            timer = new Timer( new TimerCallback( ( object o ) =>
                {
                    OnTimer();
                }), 
                null,
                30 * 1000,
                30 * 1000);
            isVisible = false;
            unsentSessionLength = 0;
        }

        public void init(string serverURL, string appKey)
        {
            queue.setServerURL(serverURL);
            queue.setAppKey(appKey);
        }

        public void OnStart()
        {
            previousTime = (DateTime.Now - startTime).TotalMilliseconds / 1000;

            queue.beginSession();

            isVisible = true;
        }

        public void OnStop()
        {
            isVisible = false;

            double currentTime = (DateTime.Now - startTime).TotalMilliseconds / 1000;
            unsentSessionLength += currentTime - previousTime;

            int duration = (int)unsentSessionLength;
            queue.endSession(duration);
            unsentSessionLength -= duration;
        }

        public void PostEvent(CountlyEvent Event)
        {
            if (Event == null)
            {
                throw new ArgumentNullException("Event");
            }

            eventqueue.Add(Event);

            if (eventqueue.Count >= 5)
            {
                queue.QueueEvents(eventqueue);
                eventqueue.Clear();
            }
        }

        private void OnTimer()
        {
            if (isVisible == false)
            {
                return;
            }

            double currentTime = (DateTime.Now - startTime).TotalMilliseconds / 1000;
            unsentSessionLength += currentTime - previousTime;
            previousTime = currentTime;

            int duration = (int)unsentSessionLength;
            queue.updateSession(duration);
            unsentSessionLength -= duration;
        }

        public class CountlyEvent
        {
            public CountlyEvent()
            {
                Key = "";
                UsingSum = false;
                UsingSegmentation = false;
            }

            /// <summary>
            /// String describing the event that has occured. (Mandatory)
            /// </summary>
            public string   Key     { get; set; }
            /// <summary>
            /// The number of times this event has occured. (Mandatory)
            /// </summary>
            public Int32    Count   { get; set; }

            /// <summary>
            /// Flags if Sum will be used in the event call. Automatically set
            /// when Sum is modified.
            /// </summary>
            public bool     UsingSum    { get; set; }
            /// <summary>
            /// Value used in the summation of similar event. For example the price
            /// of an in app purchase, so total revenue can be monitored. (Optional)
            /// </summary>
            public double   Sum         { get; set { UsingSum = true; Sum = value; } }

            /// <summary>
            /// Flags if Segmentation will be used in the event call. Automatically
            /// set when Segmentation is modified.
            /// </summary>
            public bool                         UsingSegmentation   { get; set; }
            /// <summary>
            /// Used to define characteristics of the event which can be filtered by. (Optional)
            /// </summary>
            public Dictionary<String, String>   Segmentation        { get; set { UsingSegmentation = true; Segmentation = value; } }
        }
    }

    public class ConnectionQueue
    {
        private volatile Queue<string> queue = new Queue<string>();
        private Thread thread;
        private volatile bool StopThread = false;
        private string AppKey;
        private string ServerURL;

        public void setAppKey(string input)
        {
            AppKey = input;
        }

        public void setServerURL(string input)
        {
            ServerURL = input;
        }

        public void beginSession()
        {
            string data;
            data = "app_key=" + AppKey;
            data += "&" + "device_id=" + DeviceInfo.getUDID();
            data += "&" + "sdk_version=" + "1.0";
            data += "&" + "begin_session=" + "1";
            data += "&" + "metrics=" + DeviceInfo.getMetrics();

            queue.Enqueue(data);

            Tick();
        }

        public void updateSession(int duration)
        {
            string data;
            data = "app_key=" + AppKey;
            data += "&" + "device_id=" + DeviceInfo.getUDID();
            data += "&" + "session_duration=" + duration;

            queue.Enqueue(data);

            Tick();
        }

        public void endSession(int duration)
        {
            string data;
            data = "app_key=" + AppKey;
            data += "&" + "device_id=" + DeviceInfo.getUDID();
            data += "&" + "end_session=" + "1";
            data += "&" + "session_duration=" + duration;

            queue.Enqueue(data);

            Tick();
            //thread.Join();
            /*
            StopThread = true;
            if (null != thread && ThreadState.Unstarted != thread.ThreadState)
            {
                thread.Join();
            }

            ManualResetEvent Continue = new ManualResetEvent(false);

            foreach (String CurrentQuery in queue)
            {
                try
                {
                    WebClient Downloader = new WebClient();
                    Downloader.OpenReadCompleted += new OpenReadCompletedEventHandler((object sender, OpenReadCompletedEventArgs args) =>
                        {
                            try
                            {
                                if (null != args.Error)
                                {
                                    throw args.Error;
                                }

#if (DEBUG)
                                Debug.WriteLine("Countly:\t" + "ok -> " + CurrentQuery);
#endif
                            }
                            catch (Exception E)
                            {
                            }
                            finally
                            {
                                if (null != args.Result)
                                {
                                    args.Result.Close();
                                }

                                Continue.Set();
                            }
                        });
                    Downloader.OpenReadAsync(new Uri(ServerURL + "/i?" + CurrentQuery, UriKind.Absolute));
                    Continue.WaitOne();
                }
                catch (Exception E)
                {
#if (DEBUG)
#endif
                }
                finally
                {
                    Continue.Reset();
                }
            }
            */
        }

        public void QueueEvents(List<Countly.CountlyEvent> Events)
        {
            string data = "";
            data += "app_key=" + AppKey;
            data += "&" + "device_id=" + DeviceInfo.getUDID();
            data += "&" + "events=" + "[";

            foreach(Countly.CountlyEvent CurrentEvent in Events)
            {
                data += "{";
                data += "\"" + "key" + "\"" + ":" + "\"" + CurrentEvent.Key + "\"" + ",";
                data += "\"" + "count" + "\"" + ":" + CurrentEvent.Count;
                if (CurrentEvent.UsingSum)
                {
                    data += ",";
                    data += "\"" + "sum" + "\"" + ":" + CurrentEvent.Sum;
                }
                if (CurrentEvent.UsingSegmentation)
                {
                    data += ",";
                    data += "\"" + "segmentation" + "\"" + ":" + "{";

                    bool First = true;
                    foreach (String CurrentKey in CurrentEvent.Segmentation.Keys)
                    {
                        if (First)
                        {
                            First = false;
                        }
                        else
                        {
                            data += ",";
                        }
                        data += "\"" + CurrentKey + "\"" + ":" + "\"" + CurrentEvent.Segmentation[CurrentKey] + "\"";
                    }

                    data += "}";
                }
                data += "}";
            }

            data += "]";

            queue.Enqueue(data);
        }

        private void Tick()
        {
            if (thread != null && thread.IsAlive)
            {
                return;
            }

            if (queue.Count == 0)
            {
                return;
            }

            thread = new Thread(new ThreadStart(() =>
            {
                    string data = string.Empty;
                    ManualResetEvent signal = new ManualResetEvent(false);

                    while (!StopThread)
                    {
                        try
                        {
                            data = queue.Peek();

                            WebClient client = new WebClient();
                            client.OpenReadCompleted += new OpenReadCompletedEventHandler((object sender, OpenReadCompletedEventArgs args) =>
                                {
                                    try
                                    {
                                        if (args.Error != null)
                                        {
                                            throw args.Error;
                                        }

                                        args.Result.Close();
#if (DEBUG)
                                        Debug.WriteLine("Countly:\t" + "ok -> " + data);
#endif
                                        queue.Dequeue();
                                    }
                                    catch (Exception E)
                                    {
#if (DEBUG)
                                        Debugger.Break();

                                        if (E is WebException)
                                        {
                                            Debug.WriteLine("Countly:\t" + (E as WebException).Message + "\t" + (E as WebException).Status);
                                        }
#endif
                                    }
                                    finally
                                    {
                                        signal.Set(); // Allow working thread to continue
                                    }
                                });

                            signal.Reset();

                            client.OpenReadAsync(new Uri(ServerURL + "/i?" + data));

                            signal.WaitOne(); // Block thread until 'OpenReadCompleted' method has completed
                        }
                        catch (Exception E)
                        {
                            if (E is ThreadAbortException)
                            {
#if (DEBUG)
                                Debug.WriteLine("Countly:\t" + "Worker thread abort recieved and respected");
#endif
                                return;
                            }

                            if (E is InvalidOperationException)
                            {
                                //return;
                            }

#if (DEBUG)
                            Debug.WriteLine("Countly:\t" + E.ToString());
                            Debug.WriteLine("Countly:\t" + "error -> " + data);
#endif
                            break;
                        }
                        finally
                        {
                            data = string.Empty;
                        }
                    }
                }));

            thread.IsBackground = true;

#if (DEBUG)
            thread.Name = "Countly Worker Thread";
#endif

            StopThread = false;

            thread.Start();
        }
    }

    public class DeviceInfo
    {
        public static string getUDID()
        {
            return OpenUDID.value;
        }

        public static string getOS()
        {
            return "WindowsPhone";
        }

        public static string getOSVersion()
        {
            return Environment.OSVersion.Version.ToString();
        }

        public static string getDevice()
        {
            return DeviceStatus.DeviceName;
        }

        public static string getResolution()
        {
#if WP8
            switch(Application.Current.Host.Content.ScaleFactor)
            {
                case 100:
                    return "800x480";

                case 150:
                    return "1280x720";

                case 160:
                    return "1280x768";

                default:
                    return "???";
            }
#else
            return "800x480";
#endif
        }

        public static string getCarrier()
        {
            return DeviceNetworkInformation.CellularMobileOperator;
        }

        public static string getLocal()
        {
            return CultureInfo.CurrentCulture.Name;
        }

        public static string getMetrics()
        {
            string output;

            output = "{";
            output +=       "\"" + "_device=" +     "\"" + ":" + "\"" + getDevice() +       "\"";
            output += "," + "\"" + "_os" +          "\"" + ":" + "\"" + getOS() +           "\"";
            output += "," + "\"" + "_os_version" +  "\"" + ":" + "\"" + getOSVersion() +    "\"";
            output += "," + "\"" + "_carrier" +     "\"" + ":" + "\"" + getCarrier() +      "\"";
            output += "," + "\"" + "_resolution" +  "\"" + ":" + "\"" + getResolution() +   "\"";
            output += "," + "\"" + "_local" +       "\"" + ":" + "\"" + getLocal() +        "\"";
            output += "}";

            output = HttpUtility.UrlEncode(output);

            return output;
        }
    }
}
