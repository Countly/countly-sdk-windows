using CountlySDK;
using CountlySDK.CountlyCommon.Entities;
using CountlySDK.Entities;
using CountlySDK.Entities.EntityBase;
using CountlySDK.Helpers;
using Newtonsoft.Json;
using PCLStorage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace TestProject_common
{
    public class DataStructureSerializationTests : IDisposable
    {
        
        ITestOutputHelper output;

        const int itemAmountInLargeList = 150;

        /// <summary>
        /// Test setup
        /// </summary>
        public DataStructureSerializationTests(ITestOutputHelper output)
        {
            this.output = output;
            Storage.Instance.fileSystem = FileSystem.Current;
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

            Assert.Equal(eventList, eventList2);
        }

        [Fact]
        public void SerializingDCSEntitiesListException()
        {
            List<ExceptionEvent> exceptionList = TestHelper.CreateListExceptions(itemAmountInLargeList);
            String s = TestHelper.DCSSerialize(exceptionList);
            List<ExceptionEvent> exceptionList2 = TestHelper.DCSDeserialize<List<ExceptionEvent>>(s);

            Assert.Equal(exceptionList, exceptionList2);
        }

        [Fact]
        public void SerializingDCSEntitiesListSession()
        {
            List<SessionEvent> sessionList = TestHelper.CreateListSessions(itemAmountInLargeList);
            String s = TestHelper.DCSSerialize(sessionList);
            List<SessionEvent> sessionList2 = TestHelper.DCSDeserialize<List<SessionEvent>>(s);

            Assert.Equal(sessionList, sessionList2);
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
        public async void BasicStorage()
        {
            const String filename = "SampleFilename.xml";

            List<String> sampleValues = new List<string>();
            sampleValues.Add("Book");
            sampleValues.Add("Car");

            TestHelper.StorageSerDesComp<List<String>>(sampleValues, filename);
        }

        [Fact]
        public async void StorageCollections()
        {
            int itemAmount = 150;
            List<SessionEvent> sessionList = TestHelper.CreateListSessions(itemAmount);
            List<ExceptionEvent> exceptionList = TestHelper.CreateListExceptions(itemAmount);
            List<CountlyEvent> eventList = TestHelper.CreateListEvents(itemAmount);
            CountlyUserDetails cud = TestHelper.CreateCountlyUserDetails(0, 0);
            ExceptionEvent unhandledException = TestHelper.CreateExceptionEvent(0);

            TestHelper.StorageSerDesComp<List<SessionEvent>>(sessionList, Countly.sessionsFilename);
            TestHelper.StorageSerDesComp<List<ExceptionEvent>>(exceptionList, Countly.exceptionsFilename);
            TestHelper.StorageSerDesComp<List<CountlyEvent>>(eventList, Countly.eventsFilename);
            TestHelper.StorageSerDesComp<CountlyUserDetails>(cud, Countly.userDetailsFilename);
            TestHelper.StorageSerDesComp<ExceptionEvent>(unhandledException, Countly.unhandledExceptionFilename);
        }

        [Fact]
        public async void BasicDeserialization_18_1()
        {
            IFolder folder = await Storage.Instance.GetFolder(Storage.folder);
            String targetPath = folder.Path + "\\";
            String sourceFolder = TestHelper.testDataLocation + "\\SampleDataFiles\\18_1\\";

            File.Copy(sourceFolder + "sessions.xml", targetPath + "sessions.xml");
            File.Copy(sourceFolder + "userdetails.xml", targetPath + "userdetails.xml");
            File.Copy(sourceFolder + "events.xml", targetPath + "events.xml");
            File.Copy(sourceFolder + "exceptions.xml", targetPath + "exceptions.xml");
            File.Copy(sourceFolder + "devicePCL.xml", targetPath + "device.xml");

            //Thread.Sleep(100);
    
            Countly.Instance.deferUpload = true;
            Countly.StartSession(ServerInfo.serverURL, ServerInfo.appKey, ServerInfo.appVersion, FileSystem.Current).Wait();

            Assert.Equal(50, Countly.Instance.Events.Count);
            Assert.Equal(50, Countly.Instance.Exceptions.Count);
            Assert.Equal(3, Countly.Instance.Sessions.Count);
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
    }
}
