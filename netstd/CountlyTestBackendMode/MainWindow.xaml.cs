using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using CountlySDK;
using CountlySDK.Entities;

namespace CountlyTestBackendMode
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public const string serverURL = "https://your.server.ly";
        public const string appKey = "YOUR_APP_KEY";

        public MainWindow()
        {
            if (serverURL.Equals("https://your.server.ly") || appKey.Equals("YOUR_APP_KEY")) {
                throw new Exception("Please do not use default set of app key and server url");
            }
            InitializeComponent();
            Debug.WriteLine("Before init");

            Countly.IsLoggingEnabled = true;

            CountlyConfig countlyConfig = new CountlyConfig();
            countlyConfig.serverUrl = serverURL;
            countlyConfig.appKey = appKey;
            countlyConfig.appVersion = "123";
            countlyConfig.EnableBackendMode();

            Countly.Instance.Init(countlyConfig).Wait();
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            int appCountValue = int.Parse(appCount.Text);
            int eventCountValue = int.Parse(eventCount.Text);
            int deviceCountValue = int.Parse(deviceCount.Text);

            GenerateAndRecordEvent(appCountValue, eventCountValue, deviceCountValue);
        }

        internal class StringPair
        {
            internal string one;
            internal string two;

            internal StringPair(string one, string two)
            {
                this.one = one;
                this.two = two;
            }
        }

        private void GenerateAndRecordEvent(int appCount, int eventCount, int deviceCount)
        {

            List<StringPair> devices = new List<StringPair>();
            string[] apps = { "YOUR_APP_KEY1", "YOUR_APP_KEY2" };

            Random random = new Random();

            for (int i = 0; i < 20; i++) {
                devices.Add(new(apps[random.Next(2)], $"NewDevice_{random.Next(deviceCount)}"));
            }

            for (int i = 0; i < 20; i++) {
                RecordSomething(8, devices);
            }
        }

        static Dictionary<string, string>[] locations = { new() { { "location", "68.93599659319828,121.39708887992593" } },
         new() { { "city", "Batman" }, { "country_code", "TR" } },
        new() { { "ip", "103.88.235.255" } } };

        private void RecordSomething(int selection, List<StringPair> devices)
        {
            Random random = new Random();

            for (int i = 0; i < deviceCount; i++) {
                devices.Add(new($"App_{random.Next(appCount)}", $"Device_{random.Next(deviceCount)}"));
            switch (selection) {
                case 0:
                    StringPair pair = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().BeginSession(pair.two, pair.one, location: locations[random.Next(3)]);
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().BeginSession(pair.two, timestamp: 1705389956784, location: locations[random.Next(3)]);
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().BeginSession(pair.two, pair.one, metrics: new Dictionary<string, string>() { { "_os", "Android" }, { "_os_version", "12" }, { "device", "OsPhone" } }, location: locations[random.Next(3)]);
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().BeginSession(pair.two, pair.one, location: locations[random.Next(3)]);
                    break;
                case 1:
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().UpdateSession(pair.two, random.Next(180) + 1, pair.one);
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().UpdateSession(pair.two, random.Next(180) + 1, timestamp: 1705389956784);
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().UpdateSession(pair.two, random.Next(180) + 1, pair.one);
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().UpdateSession(pair.two, random.Next(180) + 1, pair.one);
                    break;
                case 2:
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().EndSession(pair.two, random.Next(180) + 1, pair.one);
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().EndSession(pair.two, random.Next(180) + 1, timestamp: 1705389956784);
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().EndSession(pair.two, random.Next(180) + 1, pair.one);
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().EndSession(pair.two, random.Next(180) + 1, pair.one);
                    break;
                case 3:
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().StartView(pair.two, Guid.NewGuid().ToString());
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().StartView(pair.two, Guid.NewGuid().ToString(), appKey: pair.one, segment: "iOS", timestamp: 1705389956784);
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().StartView(pair.two, Guid.NewGuid().ToString(), firstView: true);
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    Segmentation segm = new();
                    segm.Add("Level", random.Next(100) + "");
                    segm.Add("Prr", "Yea");
                    Countly.Instance.BackendMode().StartView(pair.two, Guid.NewGuid().ToString(), segm, "Android", pair.one, true);
                    break;
                case 4:
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().StopView(pair.two, Guid.NewGuid().ToString(), random.Next(180) + 1);
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().StopView(pair.two, Guid.NewGuid().ToString(), random.Next(180) + 1, appKey: pair.one, segment: "iOS", timestamp: 1705389956784);
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().StopView(pair.two, Guid.NewGuid().ToString(), random.Next(180) + 1);
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    segm = new();
                    segm.Add("Level", random.Next(100) + "");
                    segm.Add("Prr", "Yea");
                    Countly.Instance.BackendMode().StopView(pair.two, Guid.NewGuid().ToString(), random.Next(180) + 1, segm, "Android", pair.one);
                    break;
                case 5:
                    segm = new();
                    segm.Add("Level", random.Next(100) + "");
                    segm.Add("Prr", "Yea");
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().RecordEvent(pair.two, Guid.NewGuid().ToString(), segm);
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().RecordEvent(pair.two, Guid.NewGuid().ToString(), pair.one);
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().RecordEvent(pair.two, Guid.NewGuid().ToString(), segm, 3, 5.6, 6, pair.one);
                    break;
                case 6:
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().RecordException(pair.two, Guid.NewGuid().ToString());
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().RecordException(pair.two, Guid.NewGuid().ToString(), appKey: pair.one, timestamp: 1705389956784);
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().RecordException(pair.two, Guid.NewGuid().ToString(), unhandled: true);
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().RecordException(pair.two, Guid.NewGuid().ToString(), "This is \na\nstack trace", new List<string>() { "First", "Detected" },
                        new Dictionary<string, object> { { "gone", "girl" }, { "girl", 5.6 } }, new Dictionary<string, string> { { "_os", "steamOS" }, { "_ram_total", "32" } }, true, pair.one);
                    break;
                case 7:
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    IDictionary<string, object> userProperties = Dict(
                         "int", 5,
                         "long", 1044151383000,
                         "float", 56.45678,
                         "string", "value",
                         "bool", true,
                         "double", -5.4E-79,
                         "invalid", Dict("test", "out"),
                         "name", "John",
                         "username", "Dohn",
                         "organization", "Fohn",
                         "email", "johnjohn@john.jo",
                         "phone", "+123456789",
                         "gender", "Unkown",
                         "byear", 1969,
                         "picture", "https://image.png",
                         "nullable", null,
                         "action", "{$push: \"black\"}"
                     );
                    userProperties["marks"] = "{$inc: 1}";
                    userProperties["point"] = "{$mul: 1.89}";
                    userProperties["gpa"] = "{$min: 1.89}";
                    userProperties["gpa"] = "{$max: 1.89}";
                    userProperties["fav"] = "{$setOnce: \"FAV\"}";
                    userProperties["permissions"] = "{$pull: [\"Create\", \"Update\"]}";
                    userProperties["langs"] = "{$push: [\"Python\", \"Ruby\", \"Ruby\"]}";
                    userProperties["langs"] = "{$addToSet: [\"Python\", \"Python\"]}";
                    Countly.Instance.BackendMode().RecordUserProperties(pair.two, userProperties);
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().RecordUserProperties(pair.two, userProperties, appKey: pair.one, timestamp: 1705389956784);
                    break;
                case 8:
                    pair = devices[Math.Abs(random.Next(20) - 1)];
                    StringPair pair1 = devices[Math.Abs(random.Next(20) - 1)];
                    Countly.Instance.BackendMode().ChangeDeviceIdWithMerge(pair.two, pair1.two, pair.one);
                    break;
            }
        }

            DateTime startTime = DateTime.Now;
            for (int i = 0; i < eventCount; i++) {
                StringPair pair = devices[Math.Abs(random.Next(deviceCount) - 1)];
                Countly.Instance.BackendMode().RecordEvent(pair.two, pair.one, $"Event_{i}", 0, 1, 0, null, 1);
        private IDictionary<string, object> Dict(params object[] values)
        {
            IDictionary<string, object> result = new Dictionary<string, object>();
            if (values == null || values.Length == 0 || values.Length % 2 != 0) { return result; }

            for (int i = 0; i < values.Length; i += 2) {
                result[values[i].ToString()] = values[i + 1];
            }

            DateTime endTime = DateTime.Now;
            testResult.Text = $"Testing took: {endTime - startTime} minutes";
            return result;
        }
    }
}
