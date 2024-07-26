using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CountlySDK;
using CountlySDK.CountlyCommon.Entities;
using CountlySDK.Entities;
using CountlySDK.Entities.EntityBase;
using CountlySDK.Helpers;
using Xunit;
using static CountlySDK.CountlyCommon.CountlyBase;
using static CountlySDK.Helpers.TimeHelper;

namespace TestProject_common
{
    internal class TestHelper
    {
        public const String testDataLocation = "..\\..\\..\\..\\countlyCommon\\TestingRelated\\TestContent";

        //list of generic string values
        public static String[] v = new string[] { "123", "234", "345", "a", "b", "c", "d", "e", "f", "t", "u", "p", "s", "g", "1t", "1u", "1p", "12s", "12g", "12t", "1u2", "12p", "122", "12g" };
        public static int[] iv = new int[] { 12, 23, 34, 45, 56, 67, 52, 23, 76, 975, 3345 };
        public static bool[] bv = new bool[] { true, false };
        public static long[] lv = new long[] { 23, 345, 543, 76, 87, 3245, 3543, 9780, 43534, 123, 5634 };
        public static double[] dv = new double[] { 123.43, 456.43, 678.543, 456.23, 765.34, 3.232323, 7.5345435, 878.5452, 98.00496, 35.15766 };

        public static string[] locales = new string[] { "pt-BR", "en-US", "nl-NL", "fr-CA", "de-DE", "th-TH", "ja-JP" };

        public static string APP_KEY = "APP_KEY";
        public static string SERVER_URL = "https://domin.com";
        public static string APP_VERSION = "1.0";
        public static string DEVICE_ID = "TEST_DEVICE_ID";
        public static string SDK_VERSION = "24.1.1";


        public static BeginSession CreateBeginSession(int indexData, int indexMetrics, TimeInstant timeInstant)
        {
            Metrics m = CreateMetrics(indexMetrics);
            DeviceId dId = new DeviceId(v[indexData + 1], DeviceBase.DeviceIdMethodInternal.developerSupplied);
            BeginSession bs = new BeginSession(v[indexData + 0], dId, v[indexData + 2], m, v[indexData + 3], timeInstant);
            return bs;
        }

        public static EndSession CreateEndSession(int index, TimeInstant timeInstant)
        {
            DeviceId dId = new DeviceId(v[index + 1], DeviceBase.DeviceIdMethodInternal.developerSupplied);
            EndSession es = new EndSession(v[index + 0], dId, v[index + 2], v[index + 3], timeInstant);
            return es;
        }

        public static UpdateSession CreateUpdateSession(int indexData, int indexDuration, TimeInstant timeInstant)
        {
            DeviceId dId = new DeviceId(v[indexData + 1], DeviceBase.DeviceIdMethodInternal.developerSupplied);
            UpdateSession us = new UpdateSession(v[indexData + 0], dId, iv[indexDuration], v[indexData + 2], v[indexData + 3], timeInstant);
            return us;
        }

        public static CustomInfoItem CreateCustomInfoItem(int index)
        {
            CustomInfoItem cii = new CustomInfoItem(v[index], v[index + 1]);
            return cii;
        }

        public static CustomInfo CreateCustomInfo(int index)
        {
            CustomInfo ci = new CustomInfo();
            ci.Add(v[index + 0], v[index + 1]);
            ci.Add(v[index + 2], v[index + 3]);
            return ci;
        }

        public static CountlyUserDetails CreateCountlyUserDetails(int indexData, int indexCustomInfo)
        {
            CountlyUserDetails cud = new CountlyUserDetails();
            PopulateCountlyUserDetails(cud, indexData, indexCustomInfo);

            return cud;
        }

        public static void PopulateCountlyUserDetails(CountlyUserDetails cud, int indexData, int indexCustomInfo)
        {
            cud.BirthYear = iv[indexData];
            cud.Custom.Add(v[indexData + 0], v[indexData + 1]);
            cud.Custom.Add(v[indexData + 2], v[indexData + 3]);
            cud.Email = v[indexData + 4];
            cud.Gender = v[indexData + 5];
            cud.Username = v[indexData + 6];
            cud.Phone = v[indexData + 7];
            cud.Organization = v[indexData + 8];
            cud.Name = v[indexData + 9];
            cud.Picture = v[indexData + 10];

            CustomInfo cui = CreateCustomInfo(indexCustomInfo);
            cud.Custom = cui;
        }

        public static DeviceId CreateDeviceId(int index, int indexIdMethod)
        {
            DeviceBase.DeviceIdMethodInternal method;
            indexIdMethod = indexIdMethod % 6;
            switch (indexIdMethod) {
                case 0:
                    method = DeviceBase.DeviceIdMethodInternal.none;
                    break;
                case 1:
                    method = DeviceBase.DeviceIdMethodInternal.cpuId;
                    break;
                case 2:
                    method = DeviceBase.DeviceIdMethodInternal.windowsGUID;
                    break;
                case 3:
                    method = DeviceBase.DeviceIdMethodInternal.winHardwareToken;
                    break;
                case 4:
                    method = DeviceBase.DeviceIdMethodInternal.multipleWindowsFields;
                    break;
                case 5:
                    method = DeviceBase.DeviceIdMethodInternal.developerSupplied;
                    break;
                default:
                    method = DeviceBase.DeviceIdMethodInternal.none;
                    break;
            }

            DeviceId did = new DeviceId(v[index], method);
            return did;
        }

        public static ExceptionEvent CreateExceptionEvent(int index)
        {
            TimeSpan ts = new TimeSpan(iv[index], iv[index + 1], iv[index + 2], iv[index + 3]);
            ExceptionEvent ee = new ExceptionEvent();
            ee.AppVersion = v[index];

            Dictionary<String, String> cust = new Dictionary<string, string>();
            cust.Add(v[index + 1], v[index + 2]);
            cust.Add(v[index + 3], v[index + 4]);

            ee.Custom = cust;
            ee.Device = v[index + 5];
            ee.Error = v[index + 6];
            ee.Logs = v[index + 7];
            ee.Manufacture = v[index + 8];
            ee.Name = v[index + 9];
            ee.NonFatal = bv[index % 2];
            ee.Online = bv[(index + 1) % 2];
            ee.Orientation = v[index + 10];
            ee.OS = v[index + 11];
            ee.OSVersion = v[index + 12];
            ee.RamCurrent = lv[index];
            ee.RamTotal = lv[index + 1];
            ee.Resolution = v[index + 13];
            ee.Run = lv[index + 2];

            return ee;
        }

        public static Metrics CreateMetrics(int index)
        {
            String locale = locales[index % locales.Length];
            Metrics m = new Metrics(v[index], v[index + 1], v[index + 2], v[index + 3], v[index + 4], v[index + 5], locale);//todo, fix locale

            return m;
        }

        public static SegmentationItem CreateSegmentationItem(int index)
        {
            SegmentationItem si = new SegmentationItem(v[index], v[index + 1]);
            return si;
        }

        public static Segmentation CreateSegmentation(int index)
        {
            Segmentation se = new Segmentation();
            se.Add(v[index], v[index + 1]);
            se.Add(v[index + 2], v[index + 3]);
            return se;
        }

        public static CountlyEvent CreateCountlyEvent(int index)
        {
            Segmentation se = CreateSegmentation(index);
            CountlyEvent ce = new CountlyEvent(v[index], iv[index], dv[index], dv[index + 1], se, 234343);

            Dictionary<String, String> cust = new Dictionary<string, string>();
            cust.Add(v[index + 1], v[index + 2]);
            cust.Add(v[index + 3], v[index + 4]);

            return ce;
        }

        public static StoredRequest CreateStoredRequest(int index)
        {
            StoredRequest br = new StoredRequest(v[index]);
            return br;
        }

        public static List<CountlyEvent> CreateListEvents(int count)
        {
            List<CountlyEvent> eventList = new List<CountlyEvent>();

            for (int a = 0; a < count; a++) {
                CountlyEvent ce = TestHelper.CreateCountlyEvent(a % 5);
                eventList.Add(ce);
            }

            return eventList;
        }

        public static List<ExceptionEvent> CreateListExceptions(int count)
        {
            List<ExceptionEvent> exceptionList = new List<ExceptionEvent>();

            for (int a = 0; a < count; a++) {
                ExceptionEvent ce = TestHelper.CreateExceptionEvent(a % 5);
                exceptionList.Add(ce);
            }

            return exceptionList;
        }

        public static List<SessionEvent> CreateListSessions(int count)
        {
            TimeHelper timeHelper = new TimeHelper();
            List<SessionEvent> sessionList = new List<SessionEvent>();

            for (int a = 0; a < count; a++) {
                SessionEvent se;
                switch (a % 3) {
                    case 0:
                        se = TestHelper.CreateBeginSession(a % 5, a % 4, timeHelper.GetUniqueInstant());
                        break;
                    case 1:
                        se = TestHelper.CreateEndSession(a % 5, timeHelper.GetUniqueInstant());
                        break;
                    case 2:
                    default:
                        se = TestHelper.CreateUpdateSession(a % 5, a % 6, timeHelper.GetUniqueInstant());
                        break;
                }

                sessionList.Add(se);
            }

            return sessionList;
        }

        public static Queue<StoredRequest> CreateQueueStoredRequests(int count)
        {
            Queue<StoredRequest> srQueue = new Queue<StoredRequest>();

            for (int a = 0; a < count; a++) {
                StoredRequest sr = CreateStoredRequest(a % 10);
                srQueue.Enqueue(sr);
            }

            return srQueue;
        }

        public static async Task<T> StorageSerDes<T>(T obj, String filename) where T : class
        {
            await Storage.Instance.DeleteFile(filename);
            T res1 = await Storage.Instance.LoadFromFile<T>(filename);
            Assert.Null(res1);

            await Storage.Instance.SaveToFile<T>(filename, obj);

            return await Storage.Instance.LoadFromFile<T>(filename);
        }

        public static async void StorageSerDesComp<T>(T obj, String filename) where T : class
        {
            T res = await StorageSerDes<T>(obj, filename);

            Assert.Equal(obj, res);
        }

        public static async void StorageSerDesCompList<T>(List<T> obj, String filename) where T : IComparable<T>
        {
            List<T> res = await StorageSerDes<List<T>>(obj, filename);

            Assert.Equal(0, UtilityHelper.CompareLists<T>(obj, res));
        }

        public static async void StorageSerDesCompQueue<T>(Queue<T> obj, String filename) where T : IComparable<T>
        {
            Queue<T> res = await StorageSerDes<Queue<T>>(obj, filename);

            Assert.Equal(0, UtilityHelper.CompareQueues<T>(obj, res));
        }

        public static async Task ValidateDataPointUpload()
        {
            if (Countly.Instance.deferUpload) {
                return;
            }

            while (Countly.Instance.Events.Count > 0 ||
                Countly.Instance.Exceptions.Count > 0 ||
                Countly.Instance.Sessions.Count > 0 ||
                Countly.Instance.StoredRequests.Count > 0 ||
                Countly.UserDetails.isChanged) {
                Thread.Sleep(100);
                if (!Countly.Instance.uploadInProgress) {
                    await Countly.Instance.Upload();
                }
            }
        }

        public static async void CleanDataFiles()
        {
            CountlyImpl.SetPCLStorageIfNeeded();

            Storage.Instance.DeleteFile(Countly.eventsFilename).Wait();
            Storage.Instance.DeleteFile(Countly.exceptionsFilename).Wait();
            Storage.Instance.DeleteFile(Countly.sessionsFilename).Wait();
            Storage.Instance.DeleteFile(Countly.unhandledExceptionFilename).Wait();
            Storage.Instance.DeleteFile(Countly.userDetailsFilename).Wait();
            Storage.Instance.DeleteFile(Countly.storedRequestsFilename).Wait();
            Storage.Instance.DeleteFile(Device.deviceFilename).Wait();
        }

        public static string DCSSerialize(object obj)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (StreamReader reader = new StreamReader(memoryStream)) {
                DataContractSerializer serializer = new DataContractSerializer(obj.GetType());
                serializer.WriteObject(memoryStream, obj);
                memoryStream.Position = 0;
                return reader.ReadToEnd();
            }
        }

        public static T DCSDeserialize<T>(string xml)
        {
            Type toType = typeof(T);
            using (Stream stream = new MemoryStream()) {
                byte[] data = System.Text.Encoding.UTF8.GetBytes(xml);
                stream.Write(data, 0, data.Length);
                stream.Position = 0;
                DataContractSerializer deserializer = new DataContractSerializer(toType);
                return (T)deserializer.ReadObject(stream);
            }
        }


        public static void MemoryStreamWrite(String filepath, MemoryStream ms)
        {
            ms.Seek(0, SeekOrigin.Begin);//go to start of stream
            using (FileStream file = new FileStream(filepath, FileMode.Create, System.IO.FileAccess.Write)) {
                //file.Write(ms.GetBuffer(), 0, (int)file.Length);


                byte[] bytes = new byte[ms.Length];
                //ms.Read(bytes, 0, (int)ms.Length);

                ReadWholeArray(ms, bytes);
                file.Write(bytes, 0, bytes.Length);
                //ms.Close();
            }
        }

        public static MemoryStream MemoryStreamRead(String filepath)
        {
            MemoryStream ms = new MemoryStream();
            using (FileStream file = new FileStream(filepath, FileMode.Open, System.IO.FileAccess.Read)) {
                //ms.SetLength(file.Length);
                //file.Read(ms.GetBuffer(), 0, (int)file.Length);

                byte[] bytes = new byte[file.Length];
                ReadWholeArray(file, bytes);
                //file.Read(bytes, 0, (int)file.Length);
                ms.Write(bytes, 0, (int)file.Length);
            }
            return ms;
        }

        /// <summary>
        /// Reads data into a complete array, throwing an EndOfStreamException
        /// if the stream runs out of data first, or if an IOException
        /// naturally occurs.
        /// </summary>
        /// <param name="stream">The stream to read data from</param>
        /// <param name="data">The array to read bytes into. The array
        /// will be completely filled from the stream, so an appropriate
        /// size must be given.</param>
        /// http://jonskeet.uk/csharp/readbinary.html
        public static void ReadWholeArray(Stream stream, byte[] data)
        {
            int offset = 0;
            int remaining = data.Length;
            while (remaining > 0) {
                int read = stream.Read(data, offset, remaining);
                if (read <= 0) {
                    throw new EndOfStreamException(String.Format("End of stream reached with {0} bytes left to read", remaining));
                }
                remaining -= read;
                offset += read;
            }
        }

        public static CountlyConfig CreateConfig()
        {
            //enable this globally
            //https://stackoverflow.com/questions/2859790/the-request-was-aborted-could-not-create-ssl-tls-secure-channel
            //to use TLS 1.2
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

            return CountlyImpl.CreateCountlyConfig();
        }

        public static CountlyConfig GetConfig()
        {
            return new CountlyConfig() { serverUrl = SERVER_URL, appKey = APP_KEY, appVersion = APP_VERSION, developerProvidedDeviceId = DEVICE_ID };
        }

        public static Dictionary<ConsentFeatures, bool> AllConsentValues(bool IsGiven)
        {
            Dictionary<ConsentFeatures, bool> consent = new Dictionary<ConsentFeatures, bool>();
            consent.Add(ConsentFeatures.Crashes, IsGiven);
            consent.Add(ConsentFeatures.Events, IsGiven);
            consent.Add(ConsentFeatures.Location, IsGiven);
            consent.Add(ConsentFeatures.Sessions, IsGiven);
            consent.Add(ConsentFeatures.Users, IsGiven);

            return consent;
        }

        public static String[] CreateLargeStrings(int stepAmount, int stepSize)
        {
            StringBuilder sbSingleStep = new StringBuilder();

            for (int a = 0; a < stepSize; a++) {
                sbSingleStep.Append("A");
            }

            String acc = "";
            String[] steps = new string[stepAmount];

            for (int a = 0; a < stepAmount; a++) {
                acc += sbSingleStep.ToString();
                steps[a] = acc;
            }

            return steps;
        }

        public static Dictionary<string, string> GetParams(string query)
        {
            string[] queryParams = query.Split('?')[1].Split('&');
            Dictionary<string, string> result = new Dictionary<string, string>();

            if (queryParams.Length < 1) {
                return result;
            }

            foreach (string param in queryParams) {
                string[] kv = param.Split('=');
                if (kv.Length > 0) {
                    result[kv[0]] = Uri.UnescapeDataString(kv[1]);
                }
            }

            return result;
        }
    }
}
