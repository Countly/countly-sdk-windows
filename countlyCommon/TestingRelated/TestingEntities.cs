﻿using CountlySDK;
using CountlySDK.CountlyCommon.Entities;
using CountlySDK.Entities;
using CountlySDK.Entities.EntityBase;
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
    public class TestingEntities : IDisposable
    {
        ITestOutputHelper output;
        
        /// <summary>
        /// Test setup
        /// </summary>
        public TestingEntities(ITestOutputHelper output)
        {
            this.output = output;
            TestHelper.CleanDataFiles();
        }

        /// <summary>
        /// Test cleanup
        /// </summary>
        public void Dispose()
        {
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void ComparingEntitiesSession(int i)
        {
            long ts = TimeHelper.UnixTimeNow();
            BeginSession bs0 = TestHelper.CreateBeginSession(i, i, ts);
            BeginSession bs1 = TestHelper.CreateBeginSession(i, i, ts);
            BeginSession bs2 = TestHelper.CreateBeginSession(i + 1, i, ts);
            BeginSession bs3 = TestHelper.CreateBeginSession(i, i + 1, ts);

            Assert.Equal(bs0, bs1);
            Assert.NotEqual(bs1, bs2);
            Assert.NotEqual(bs1, bs3);

            EndSession es0 = TestHelper.CreateEndSession(i, ts);
            EndSession es1 = TestHelper.CreateEndSession(i, ts);
            EndSession es2 = TestHelper.CreateEndSession(i + 1, ts);

            Assert.Equal(es0, es1);
            Assert.NotEqual(es1, es2);

            UpdateSession us0 = TestHelper.CreateUpdateSession(i, i, ts);
            UpdateSession us1 = TestHelper.CreateUpdateSession(i, i, ts);
            UpdateSession us2 = TestHelper.CreateUpdateSession(i + 1, i, ts);

            Assert.Equal(us0, us1);
            Assert.NotEqual(us1, us2);
        }

        [Fact]
        public void ComparingEntitiesSessionNull()
        {
            long ts = TimeHelper.UnixTimeNow();
            BeginSession bs0 = TestHelper.CreateBeginSession(0, 0, ts);
            BeginSession bs1 = TestHelper.CreateBeginSession(0, 0, ts);
            bs1.Content = bs0.Content;
            
            Assert.Equal(bs0, bs1);
            bs0.Content = null;
            bs1.Content = null;
            Assert.Equal(bs0, bs1);

            bs0 = TestHelper.CreateBeginSession(0, 0, ts);
            bs1 = TestHelper.CreateBeginSession(0, 0, ts);
            bs1.Content = null;
            Assert.NotEqual(bs0, bs1);
            Assert.NotEqual(bs1, bs0);

            EndSession es1 = TestHelper.CreateEndSession(0, ts);
            EndSession es2 = TestHelper.CreateEndSession(0, ts);
            
            Assert.Equal(es1, es2);
            es1.Content = null;
            es2.Content = null;
            Assert.Equal(es1, es2);

            es1 = TestHelper.CreateEndSession(0, ts);
            es2 = TestHelper.CreateEndSession(0, ts);
            es1.Content = null;
            Assert.NotEqual(es1, es2);
            Assert.NotEqual(es2, es1);

            UpdateSession us1 = TestHelper.CreateUpdateSession(0, 0, ts);
            UpdateSession us2 = TestHelper.CreateUpdateSession(0, 0, ts);

            Assert.Equal(us1, us2);
            us1.Content = null;
            us2.Content = null;
            Assert.Equal(us1, us2);

            us1 = TestHelper.CreateUpdateSession(0, 0);
            us2 = TestHelper.CreateUpdateSession(0, 0);
            us2.Content = null;
            Assert.NotEqual(us1, us2);
            Assert.NotEqual(us2, us1);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void ComparingEntitiesCustomInfo(int i)
        {
            CustomInfoItem cii0 = TestHelper.CreateCustomInfoItem(i);
            CustomInfoItem cii1 = TestHelper.CreateCustomInfoItem(i);
            CustomInfoItem cii2 = TestHelper.CreateCustomInfoItem(i + 1);

            Assert.Equal(cii0, cii1);
            Assert.NotEqual(cii1, cii2);

            CustomInfo ci0 = TestHelper.CreateCustomInfo(i);
            CustomInfo ci1 = TestHelper.CreateCustomInfo(i);
            CustomInfo ci2 = TestHelper.CreateCustomInfo(i + 1);

            Assert.Equal(ci0, ci1);
            Assert.NotEqual(ci1, ci2);
        }

        [Fact]
        public void ComparingEntitiesCustomInfoItemNull()
        {
            CustomInfoItem cii0 = TestHelper.CreateCustomInfoItem(0);
            CustomInfoItem cii1 = TestHelper.CreateCustomInfoItem(0);
            
            Assert.Equal(cii0, cii1);
            cii0.Name = null;
            cii1.Name = null;
            Assert.Equal(cii0, cii1);
            cii0.Value = null;
            cii1.Value = null;
            Assert.Equal(cii0, cii1);

            cii0 = TestHelper.CreateCustomInfoItem(0);
            cii1 = TestHelper.CreateCustomInfoItem(0);
            cii1.Name = null;
            Assert.NotEqual(cii0, cii1);
            Assert.NotEqual(cii1, cii0);

            cii1 = TestHelper.CreateCustomInfoItem(0);
            cii1.Value = null;
            Assert.NotEqual(cii0, cii1);
            Assert.NotEqual(cii1, cii0);
        }

        [Fact]
        public void ComparingEntitiesCustomInfoNull()
        {         
            CustomInfo ci1 = TestHelper.CreateCustomInfo(0);
            CustomInfo ci2 = TestHelper.CreateCustomInfo(0);

            Assert.Equal(ci1, ci2);
            ci1.items = null;
            ci2.items = null;
            Assert.Equal(ci1, ci2);

            ci1 = TestHelper.CreateCustomInfo(0);
            ci2 = TestHelper.CreateCustomInfo(0);

            ci2.items = null;
            Assert.NotEqual(ci1, ci2);
            Assert.NotEqual(ci2, ci1);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void ComparingEntitiesCountlyUserDetails(int i)
        {
            CountlyUserDetails cud0 = TestHelper.CreateCountlyUserDetails(i, i);
            CountlyUserDetails cud1 = TestHelper.CreateCountlyUserDetails(i, i);
            CountlyUserDetails cud2 = TestHelper.CreateCountlyUserDetails(i + 1, i);
            CountlyUserDetails cud3 = TestHelper.CreateCountlyUserDetails(i, i + 1);

            Assert.Equal(cud0, cud1);
            Assert.NotEqual(cud1, cud2);
            Assert.NotEqual(cud1, cud3);
        }

        [Fact]
        public void ComparingEntitiesCountlyUserDetailsNull()
        {
            CountlyUserDetails cud0 = TestHelper.CreateCountlyUserDetails(0, 0);
            CountlyUserDetails cud1 = TestHelper.CreateCountlyUserDetails(0, 0);
            
            Assert.Equal(cud0, cud1);
            cud0.BirthYear = null;
            cud1.BirthYear = null;
            Assert.Equal(cud0, cud1);
            cud0.Custom = null;
            cud1.Custom = null;
            Assert.Equal(cud0, cud1);
            cud0.Email = null;
            cud1.Email = null;
            Assert.Equal(cud0, cud1);
            cud0.Gender = null;
            cud1.Gender = null;
            Assert.Equal(cud0, cud1);
            cud0.Name = null;
            cud1.Name = null;
            Assert.Equal(cud0, cud1);
            cud0.Organization = null;
            cud1.Organization = null;
            Assert.Equal(cud0, cud1);
            cud0.Phone = null;
            cud1.Phone = null;
            Assert.Equal(cud0, cud1);
            cud0.Picture = null;
            cud1.Picture = null;
            Assert.Equal(cud0, cud1);
            cud0.Username = null;
            cud1.Username = null;
            Assert.Equal(cud0, cud1);

            cud0 = TestHelper.CreateCountlyUserDetails(0, 0);
            cud1 = TestHelper.CreateCountlyUserDetails(0, 0);
            cud1.Name = null;
            Assert.NotEqual(cud0, cud1);
            Assert.NotEqual(cud1, cud0);

            cud1 = TestHelper.CreateCountlyUserDetails(0, 0);
            cud1.Organization = null;
            Assert.NotEqual(cud0, cud1);
            Assert.NotEqual(cud1, cud0);

            cud1 = TestHelper.CreateCountlyUserDetails(0, 0);
            cud1.Phone = null;
            Assert.NotEqual(cud0, cud1);
            Assert.NotEqual(cud1, cud0);

            cud1 = TestHelper.CreateCountlyUserDetails(0, 0);
            cud1.Picture = null;
            Assert.NotEqual(cud0, cud1);
            Assert.NotEqual(cud1, cud0);

            cud1 = TestHelper.CreateCountlyUserDetails(0, 0);
            cud1.Username = null;
            Assert.NotEqual(cud0, cud1);
            Assert.NotEqual(cud1, cud0);

            cud1 = TestHelper.CreateCountlyUserDetails(0, 0);
            cud1.BirthYear = null;
            Assert.NotEqual(cud0, cud1);
            Assert.NotEqual(cud1, cud0);

            cud1 = TestHelper.CreateCountlyUserDetails(0, 0);
            cud1.Email = null;
            Assert.NotEqual(cud0, cud1);
            Assert.NotEqual(cud1, cud0);

            cud1 = TestHelper.CreateCountlyUserDetails(0, 0);
            cud1.Gender = null;
            Assert.NotEqual(cud0, cud1);
            Assert.NotEqual(cud1, cud0);

            cud1 = TestHelper.CreateCountlyUserDetails(0, 0);
            cud1.Custom = null;
            Assert.NotEqual(cud0, cud1);
            Assert.NotEqual(cud1, cud0);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void ComparingEntitiesDeviceId(int i)
        {
            DeviceId did0 = TestHelper.CreateDeviceId(i, i);
            DeviceId did1 = TestHelper.CreateDeviceId(i, i);
            DeviceId did2 = TestHelper.CreateDeviceId(i + 1, i);
            DeviceId did3 = TestHelper.CreateDeviceId(i, i + 1);
            DeviceId did4 = TestHelper.CreateDeviceId(i, i + 2);

            Assert.Equal(did0, did1);
            Assert.NotEqual(did1, did2);
            Assert.NotEqual(did1, did3);
            Assert.NotEqual(did1, did4);
        }

        [Fact]
        public void ComparingEntitiesDeviceIdNull()
        {
            DeviceId did0 = TestHelper.CreateDeviceId(0, 0);
            DeviceId did1 = TestHelper.CreateDeviceId(0, 0);           

            Assert.Equal(did0, did1);
            did0.deviceId = null;
            did1.deviceId = null;
            Assert.Equal(did0, did1);

            did0 = TestHelper.CreateDeviceId(0, 0);
            did1 = TestHelper.CreateDeviceId(0, 0);
            did0.deviceId = null;

            Assert.NotEqual(did0, did1);
            Assert.NotEqual(did1, did0);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void ComparingEntitiesExceptionEvent(int i)
        {
            ExceptionEvent did0 = TestHelper.CreateExceptionEvent(i);
            ExceptionEvent did1 = TestHelper.CreateExceptionEvent(i);
            ExceptionEvent did2 = TestHelper.CreateExceptionEvent(i + 1);

            Assert.Equal(did0, did1);
            Assert.NotEqual(did1, did2);
        }

        [Fact]
        public void ComparingEntitiesExceptionEventNull()
        {
            ExceptionEvent ee0 = TestHelper.CreateExceptionEvent(0);
            ExceptionEvent ee1 = TestHelper.CreateExceptionEvent(0);

            Assert.Equal(ee0, ee1);
            ee0.AppVersion = null;
            ee1.AppVersion = null;
            Assert.Equal(ee0, ee1);
            ee0.Custom = null;
            ee1.Custom = null;
            Assert.Equal(ee0, ee1);
            ee0.Device = null;
            ee1.Device = null;
            Assert.Equal(ee0, ee1);
            ee0.Error = null;
            ee1.Error = null;
            Assert.Equal(ee0, ee1);
            ee0.Logs = null;
            ee1.Logs = null;
            Assert.Equal(ee0, ee1);
            ee0.Manufacture = null;
            ee1.Manufacture = null;
            Assert.Equal(ee0, ee1);
            ee0.Name = null;
            ee1.Name = null;
            Assert.Equal(ee0, ee1);
            ee0.Orientation = null;
            ee1.Orientation = null;
            Assert.Equal(ee0, ee1);
            ee0.OS = null;
            ee1.OS = null;
            Assert.Equal(ee0, ee1);
            ee0.OSVersion = null;
            ee1.OSVersion = null;
            Assert.Equal(ee0, ee1);
            ee0.RamCurrent = null;
            ee1.RamCurrent = null;
            Assert.Equal(ee0, ee1);
            ee0.RamTotal = null;
            ee1.RamTotal = null;
            Assert.Equal(ee0, ee1);
            ee0.Resolution = null;
            ee1.Resolution = null;
            Assert.Equal(ee0, ee1);            

            ee0 = TestHelper.CreateExceptionEvent(0);
            ee1 = TestHelper.CreateExceptionEvent(0);

            Assert.Equal(ee0, ee1);            
            ee1.AppVersion = null;
            Assert.NotEqual(ee0, ee1);
            Assert.NotEqual(ee1, ee0);

            ee1 = TestHelper.CreateExceptionEvent(0);
            ee1.Custom = null;
            Assert.NotEqual(ee0, ee1);
            Assert.NotEqual(ee1, ee0);

            ee1 = TestHelper.CreateExceptionEvent(0);
            ee1.Device = null;
            Assert.NotEqual(ee0, ee1);
            Assert.NotEqual(ee1, ee0);

            ee1 = TestHelper.CreateExceptionEvent(0);
            ee1.Error = null;
            Assert.NotEqual(ee0, ee1);
            Assert.NotEqual(ee1, ee0);

            ee1 = TestHelper.CreateExceptionEvent(0);
            ee1.Logs = null;
            Assert.NotEqual(ee0, ee1);
            Assert.NotEqual(ee1, ee0);

            ee1 = TestHelper.CreateExceptionEvent(0);
            ee1.Manufacture = null;
            Assert.NotEqual(ee0, ee1);
            Assert.NotEqual(ee1, ee0);

            ee1 = TestHelper.CreateExceptionEvent(0);
            ee1.Name = null;
            Assert.NotEqual(ee0, ee1);
            Assert.NotEqual(ee1, ee0);

            ee1 = TestHelper.CreateExceptionEvent(0);
            ee1.Orientation = null;
            Assert.NotEqual(ee0, ee1);
            Assert.NotEqual(ee1, ee0);

            ee1 = TestHelper.CreateExceptionEvent(0);
            ee1.OS = null;
            Assert.NotEqual(ee0, ee1);
            Assert.NotEqual(ee1, ee0);

            ee1 = TestHelper.CreateExceptionEvent(0);
            ee1.OSVersion = null;
            Assert.NotEqual(ee0, ee1);
            Assert.NotEqual(ee1, ee0);

            ee1 = TestHelper.CreateExceptionEvent(0);
            ee1.RamCurrent = null;
            Assert.NotEqual(ee0, ee1);
            Assert.NotEqual(ee1, ee0);

            ee1 = TestHelper.CreateExceptionEvent(0);
            ee1.RamTotal = null;
            Assert.NotEqual(ee0, ee1);
            Assert.NotEqual(ee1, ee0);

            ee1 = TestHelper.CreateExceptionEvent(0);
            ee1.Resolution = null;
            Assert.NotEqual(ee0, ee1);
            Assert.NotEqual(ee1, ee0);


        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void ComparingEntitiesMetrics(int i)
        {
            Metrics m1 = TestHelper.CreateMetrics(i);
            Metrics m2 = TestHelper.CreateMetrics(i);
            Metrics m3 = TestHelper.CreateMetrics(i + 1);

            Assert.Equal(m1, m2);
            Assert.NotEqual(m1, m3);
        }

        [Fact]
        public void ComparingEntitiesMetricsNull()
        {
            Metrics m1 = TestHelper.CreateMetrics(0);
            Metrics m2 = TestHelper.CreateMetrics(0);

            Assert.Equal(m1, m2);
            m1.AppVersion = null;
            m2.AppVersion = null;
            Assert.Equal(m1, m2);
            m1.Carrier = null;
            m2.Carrier = null;
            Assert.Equal(m1, m2);
            m1.Device = null;
            m2.Device = null;
            Assert.Equal(m1, m2);
            m1.OS = null;
            m2.OS = null;
            Assert.Equal(m1, m2);
            m1.OSVersion = null;
            m2.OSVersion = null;
            Assert.Equal(m1, m2);
            m1.Resolution = null;
            m2.Resolution = null;
            Assert.Equal(m1, m2);
            
            m1 = TestHelper.CreateMetrics(0);
            m2 = TestHelper.CreateMetrics(0);

            m2.AppVersion = null;
            Assert.NotEqual(m1, m2);
            Assert.NotEqual(m2, m1);

            m2 = TestHelper.CreateMetrics(0);
            m2.Carrier = null;
            Assert.NotEqual(m1, m2);
            Assert.NotEqual(m2, m1);

            m2 = TestHelper.CreateMetrics(0);
            m2.Device = null;
            Assert.NotEqual(m1, m2);
            Assert.NotEqual(m2, m1);

            m2 = TestHelper.CreateMetrics(0);
            m2.OS = null;
            Assert.NotEqual(m1, m2);
            Assert.NotEqual(m2, m1);

            m2 = TestHelper.CreateMetrics(0);
            m2.OSVersion = null;
            Assert.NotEqual(m1, m2);
            Assert.NotEqual(m2, m1);

            m2 = TestHelper.CreateMetrics(0);
            m2.Resolution = null;
            Assert.NotEqual(m1, m2);
            Assert.NotEqual(m2, m1);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void ComparingEntitiesSegmentationItem(int i)
        {
            SegmentationItem si1 = TestHelper.CreateSegmentationItem(i);
            SegmentationItem si2 = TestHelper.CreateSegmentationItem(i);
            SegmentationItem si3 = TestHelper.CreateSegmentationItem(i + 1);

            Assert.Equal(si1, si2);
            Assert.NotEqual(si1, si3);
        }

        [Fact]
        public void ComparingEntitiesSegmentationItemNull()
        {
            SegmentationItem si1 = TestHelper.CreateSegmentationItem(0);
            SegmentationItem si2 = TestHelper.CreateSegmentationItem(0);

            Assert.Equal(si1, si2);
            si1.Key = null;
            si2.Key = null;
            Assert.Equal(si1, si2);
            si1.Value = null;
            si2.Value = null;
            Assert.Equal(si1, si2);

            si1 = TestHelper.CreateSegmentationItem(0);
            si2 = TestHelper.CreateSegmentationItem(0);
            
            si2.Key = null;
            Assert.NotEqual(si1, si2);
            Assert.NotEqual(si2, si1);

            si2 = TestHelper.CreateSegmentationItem(0);
            si2.Value = null;
            Assert.NotEqual(si1, si2);
            Assert.NotEqual(si2, si1);       
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void ComparingEntitiesSegmentation(int i)
        {
            Segmentation si1 = TestHelper.CreateSegmentation(i);
            Segmentation si2 = TestHelper.CreateSegmentation(i);
            Segmentation si3 = TestHelper.CreateSegmentation(i + 1);

            Assert.Equal(si1, si2);
            Assert.NotEqual(si1, si3);
        }

        [Fact]
        public void ComparingEntitiesSegmentationNull()
        {
            Segmentation si1 = TestHelper.CreateSegmentation(0);
            Segmentation si2 = TestHelper.CreateSegmentation(0);

            Assert.Equal(si1, si2);
            si1.segmentation = null;
            si2.segmentation = null;
            Assert.Equal(si1, si2);
            
            si1 = TestHelper.CreateSegmentation(0);
            si2 = TestHelper.CreateSegmentation(0);

            si2.segmentation = null;
            Assert.NotEqual(si1, si2);
            Assert.NotEqual(si2, si1);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void ComparingEntitiesCountlyEvent(int i)
        {
            CountlyEvent ce1 = TestHelper.CreateCountlyEvent(i);
            CountlyEvent ce2 = TestHelper.CreateCountlyEvent(i);
            CountlyEvent ce3 = TestHelper.CreateCountlyEvent(i + 1);

            Assert.Equal(ce1, ce2);
            Assert.NotEqual(ce1, ce3);
        }

        [Fact]
        public void ComparingEntitiesCountlyEventNull()
        {
            CountlyEvent ce1 = TestHelper.CreateCountlyEvent(0);
            CountlyEvent ce2 = TestHelper.CreateCountlyEvent(0);

            Assert.Equal(ce1, ce2);
            ce1.Key = null;
            ce2.Key = null;
            Assert.Equal(ce1, ce2);
            ce1.Segmentation = null;
            ce2.Segmentation = null;
            Assert.Equal(ce1, ce2);
            ce1.Sum = null;
            ce2.Sum = null;
            Assert.Equal(ce1, ce2);

            ce1 = TestHelper.CreateCountlyEvent(0);
            ce2 = TestHelper.CreateCountlyEvent(0);
           
            ce2.Key = null;
            Assert.NotEqual(ce1, ce2);
            Assert.NotEqual(ce2, ce1);

            ce2 = TestHelper.CreateCountlyEvent(0);
            ce2.Segmentation = null;
            Assert.NotEqual(ce1, ce2);
            Assert.NotEqual(ce2, ce1);

            ce2 = TestHelper.CreateCountlyEvent(0);
            ce2.Sum = null;
            Assert.NotEqual(ce1, ce2);
            Assert.NotEqual(ce2, ce1);
        }

        [Fact]
        public void SerializingEntitiesSession()
        {
            BeginSession bs = TestHelper.CreateBeginSession(0, 0);
            String s1 = JsonConvert.SerializeObject(bs);
            BeginSession bs2 = JsonConvert.DeserializeObject<BeginSession>(s1);
            Assert.Equal(bs.Content, bs2.Content);

            EndSession es = TestHelper.CreateEndSession(0);
            String s2 = JsonConvert.SerializeObject(es);
            EndSession es2 = JsonConvert.DeserializeObject<EndSession>(s2);
            Assert.Equal(es.Content, es2.Content);

            UpdateSession us = TestHelper.CreateUpdateSession(0, 0);
            String s3 = JsonConvert.SerializeObject(us);
            UpdateSession us2 = JsonConvert.DeserializeObject<UpdateSession>(s3);
            Assert.Equal(us.Content, us2.Content);
        }

        [Fact]
        public void SerializingEntitiesEvent()
        {
            CountlyEvent ce = TestHelper.CreateCountlyEvent(0);
            String s4 = JsonConvert.SerializeObject(ce);
            CountlyEvent ce2 = JsonConvert.DeserializeObject<CountlyEvent>(s4);

            Assert.Equal(ce, ce2);
        }

        [Fact]
        public void SerializingEntitiesCustomInfo()
        {
            CustomInfoItem cii = TestHelper.CreateCustomInfoItem(0);
            String s1 = JsonConvert.SerializeObject(cii);
            CustomInfoItem cii2 = JsonConvert.DeserializeObject<CustomInfoItem>(s1);

            Assert.Equal(cii, cii2);

            CustomInfo ci = TestHelper.CreateCustomInfo(0);
            String s2 = JsonConvert.SerializeObject(ci);
            CustomInfo ci2 = JsonConvert.DeserializeObject<CustomInfo>(s2);

            Assert.Equal(ci.items, ci2.items);
        }

        [Fact]
        public void SerializingEntitiesUserDetails()
        {
            CountlyUserDetails cud = TestHelper.CreateCountlyUserDetails(0, 0);
            String s5 = JsonConvert.SerializeObject(cud);
            CountlyUserDetails cud2 = JsonConvert.DeserializeObject<CountlyUserDetails>(s5);

            Assert.Equal(cud, cud2);
        }

        [Fact]
        public void SerializingEntitiesDeviceId()
        {
            DeviceId did = TestHelper.CreateDeviceId(0, 0);
            String s = JsonConvert.SerializeObject(did);
            DeviceId did2 = JsonConvert.DeserializeObject<DeviceId>(s);

            Assert.Equal(did, did2);
        }

        [Fact]
        public void SerializingEntitiesExceptionEvent()
        {
            ExceptionEvent ee1 = TestHelper.CreateExceptionEvent(0);
            String s = JsonConvert.SerializeObject(ee1);
            ExceptionEvent ee2 = JsonConvert.DeserializeObject<ExceptionEvent>(s);

            Assert.Equal(ee1, ee2);
        }       

        [Fact]
        public void SerializingEntitiesMetrics()
        {
            Metrics m1 = TestHelper.CreateMetrics(0);
            String s = JsonConvert.SerializeObject(m1);
            Metrics m2 = JsonConvert.DeserializeObject<Metrics>(s);

            Assert.Equal(m1, m2);
        }

        [Fact]
        public void SerializingEntitiesSegmentationItem()
        {
            SegmentationItem si1 = TestHelper.CreateSegmentationItem(0);
            String s = JsonConvert.SerializeObject(si1);
            SegmentationItem si2 = JsonConvert.DeserializeObject<SegmentationItem>(s);

            Assert.Equal(si1, si2);
        }

        [Fact]
        public void SerializingEntitiesSegmentation()
        {
            Segmentation si1 = TestHelper.CreateSegmentation(0);
            String s = JsonConvert.SerializeObject(si1);
            Segmentation si2 = JsonConvert.DeserializeObject<Segmentation>(s);

            Assert.Equal(si1, si2);
        }
    }
}
