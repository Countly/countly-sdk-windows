using System;
using System.Collections.Generic;
using System.IO;
using CountlySDK;
using CountlySDK.CountlyCommon.Entities;
using CountlySDK.Entities;
using CountlySDK.Entities.EntityBase;
using CountlySDK.Helpers;
using Xunit;

namespace TestProject_common
{
    public class DataStructureSerializationTests : IDisposable
    {

        const int itemAmountInLargeList = 150;

        /// <summary>
        /// Test setup
        /// </summary>
        public DataStructureSerializationTests()
        {
            CountlyImpl.SetPCLStorageIfNeeded();
            Countly.Halt();
            TestHelper.CleanDataFiles();
        }

        /// <summary>
        /// Test cleanup
        /// </summary>
        public void Dispose()
        {
        }

        [Fact]
        public void SerializingDCSEntitiesListEvent()
        {
            List<CountlyEvent> eventList = TestHelper.CreateListEvents(itemAmountInLargeList);
            String s = TestHelper.DCSSerialize(eventList);
            List<CountlyEvent> eventList2 = TestHelper.DCSDeserialize<List<CountlyEvent>>(s);

            Assert.Equal(0, UtilityHelper.CompareLists(eventList, eventList2));
        }

        [Fact]
        public void SerializingDCSEntitiesListException()
        {
            List<ExceptionEvent> exceptionList = TestHelper.CreateListExceptions(itemAmountInLargeList);
            String s = TestHelper.DCSSerialize(exceptionList);
            List<ExceptionEvent> exceptionList2 = TestHelper.DCSDeserialize<List<ExceptionEvent>>(s);

            Assert.Equal(0, UtilityHelper.CompareLists(exceptionList, exceptionList2));
        }

        [Fact]
        public void SerializingDCSEntitiesListSession()
        {
            List<SessionEvent> sessionList = TestHelper.CreateListSessions(itemAmountInLargeList);
            String s = TestHelper.DCSSerialize(sessionList);
            List<SessionEvent> sessionList2 = TestHelper.DCSDeserialize<List<SessionEvent>>(s);

            Assert.Equal(0, UtilityHelper.CompareLists(sessionList, sessionList2));
        }

        [Fact]
        public void SerializingDCSEntitiesQueueStoredRequests()
        {
            Queue<StoredRequest> srQueue = TestHelper.CreateQueueStoredRequests(itemAmountInLargeList);
            String s = TestHelper.DCSSerialize(srQueue);
            Queue<StoredRequest> srQueue2 = TestHelper.DCSDeserialize<Queue<StoredRequest>>(s);

            Assert.Equal(0, UtilityHelper.CompareQueues(srQueue, srQueue2));
        }

        [Fact]
        public void SerializingDCSEntitiesUserDetails()
        {
            CountlyUserDetails cud = TestHelper.CreateCountlyUserDetails(0, 0);
            String s = TestHelper.DCSSerialize(cud);
            CountlyUserDetails cud2 = TestHelper.DCSDeserialize<CountlyUserDetails>(s);

            Assert.Equal(cud, cud2);
        }

        [Fact]
        public void SerializingDCSEntitiesStoredRequest()
        {
            StoredRequest sr1 = TestHelper.CreateStoredRequest(0);
            String s4 = TestHelper.DCSSerialize(sr1);
            StoredRequest sr2 = TestHelper.DCSDeserialize<StoredRequest>(s4);

            Assert.Equal(sr1, sr2);
        }

        [Fact]
        public void BasicStorage()
        {
            const String filename = "SampleFilename.xml";

            List<String> sampleValues = new List<string>();
            sampleValues.Add("Book");
            sampleValues.Add("Car");

            TestHelper.StorageSerDesComp<List<String>>(sampleValues, filename);
        }

        [Fact]
        public void StorageCollections()
        {
            int itemAmount = 150;
            List<SessionEvent> sessionList = TestHelper.CreateListSessions(itemAmount);
            List<ExceptionEvent> exceptionList = TestHelper.CreateListExceptions(itemAmount);
            List<CountlyEvent> eventList = TestHelper.CreateListEvents(itemAmount);
            Queue<StoredRequest> requestQueue = TestHelper.CreateQueueStoredRequests(itemAmount);
            CountlyUserDetails cud = TestHelper.CreateCountlyUserDetails(0, 0);
            ExceptionEvent unhandledException = TestHelper.CreateExceptionEvent(0);

            TestHelper.StorageSerDesCompList<SessionEvent>(sessionList, Countly.sessionsFilename);
            TestHelper.StorageSerDesCompList<ExceptionEvent>(exceptionList, Countly.exceptionsFilename);
            TestHelper.StorageSerDesCompList<CountlyEvent>(eventList, Countly.eventsFilename);
            TestHelper.StorageSerDesCompQueue<StoredRequest>(requestQueue, Countly.storedRequestsFilename);
            TestHelper.StorageSerDesComp<CountlyUserDetails>(cud, Countly.userDetailsFilename);
            TestHelper.StorageSerDesComp<ExceptionEvent>(unhandledException, Countly.unhandledExceptionFilename);
        }

        [Fact]
        public async void BasicDeserialization_18_1()
        {
            String targetPath = await Storage.Instance.GetFolderPath(Storage.Instance.folder) + "\\";
            String sourceFolder = TestHelper.testDataLocation + "\\SampleDataFiles\\18_1\\";

            File.Copy(sourceFolder + "sessions.xml", targetPath + "sessions.xml");
            File.Copy(sourceFolder + "userdetails.xml", targetPath + "userdetails.xml");
            File.Copy(sourceFolder + "events.xml", targetPath + "events.xml");
            File.Copy(sourceFolder + "exceptions.xml", targetPath + "exceptions.xml");
            File.Copy(sourceFolder + "devicePCL.xml", targetPath + "device.xml");

            Countly.Instance.deferUpload = true;
            await CountlyImpl.StartLegacyCountlySession(ServerInfo.serverURL, ServerInfo.appKey, ServerInfo.appVersion);

            Assert.Equal(50, Countly.Instance.Events.Count);
            Assert.Equal(50, Countly.Instance.Exceptions.Count);
            Assert.Equal(2, Countly.Instance.Sessions.Count);
            Assert.Equal(1, Countly.Instance.StoredRequests.Count);
            Assert.Equal("B249FB85668941FAA8301E2A5CA95901", await Countly.GetDeviceId());

            CountlyUserDetails cud = Countly.UserDetails;
            Assert.Equal(56, cud.BirthYear);
            Assert.Equal("f", cud.Email);
            Assert.Equal("t", cud.Gender);
            Assert.Equal("g", cud.Name);
            Assert.Equal("s", cud.Organization);
            Assert.Equal("p", cud.Phone);
            Assert.Equal("1t", cud.Picture);
            Assert.Equal("u", cud.Username);
        }

        [Fact]
        public async void BasicDeserialization_002()
        {
            String targetPath = await Storage.Instance.GetFolderPath(Storage.Instance.folder) + "\\";
            String sourceFolder = TestHelper.testDataLocation + "\\SampleDataFiles\\Test_002\\";

            File.Copy(sourceFolder + "sessions.xml", targetPath + "sessions.xml", true);
            File.Copy(sourceFolder + "userdetails.xml", targetPath + "userdetails.xml", true);
            File.Copy(sourceFolder + "events.xml", targetPath + "events.xml", true);
            File.Copy(sourceFolder + "exceptions.xml", targetPath + "exceptions.xml", true);
            File.Copy(sourceFolder + "device.xml", targetPath + "device.xml", true);
            File.Copy(sourceFolder + "storedRequests.xml", targetPath + "storedRequests.xml", true);

            Countly.Instance.deferUpload = true;
            CountlyConfig cc = TestHelper.CreateConfig();
            await Countly.Instance.Init(cc);

            Assert.Equal(100, Countly.Instance.Events.Count);
            Assert.Equal(100, Countly.Instance.Exceptions.Count);
            Assert.Equal(102, Countly.Instance.Sessions.Count);
            Assert.Equal(150, Countly.Instance.StoredRequests.Count);
            Assert.Equal("SDSDSD1570501868", await Countly.GetDeviceId());

            CountlyUserDetails cud = Countly.UserDetails;
            Assert.Equal(975, cud.BirthYear);
            Assert.Equal("g", cud.Email);
            Assert.Equal("1t", cud.Gender);
            Assert.Equal("12g", cud.Name);
            Assert.Equal("12s", cud.Organization);
            Assert.Equal("1p", cud.Phone);
            Assert.Equal("12t", cud.Picture);
            Assert.Equal("1u", cud.Username);
        }

        [Fact]
        public async void DeserializeDeviceIdString_18_1()
        {
            String targetPath = await Storage.Instance.GetFolderPath(Storage.Instance.folder) + "\\";
            String sourceFolder = TestHelper.testDataLocation + "\\SampleDataFiles\\18_1\\";

            File.Copy(sourceFolder + "devicePCL.xml", targetPath + "device.xml");
            CountlyConfig cc = TestHelper.CreateConfig();

            await Countly.Instance.Init(cc);
            String deviceId = "B249FB85668941FAA8301E2A5CA95901";
            Assert.Equal(deviceId, await Countly.GetDeviceId());
            DeviceId res = await Storage.Instance.LoadFromFile<DeviceId>(Device.deviceFilename);
            Assert.Equal(deviceId, res.deviceId);
            Assert.True((DeviceBase.DeviceIdMethodInternal)Countly.DeviceIdMethod.windowsGUID == res.deviceIdMethod);
        }
    }
}
