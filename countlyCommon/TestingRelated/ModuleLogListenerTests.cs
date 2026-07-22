using System;
using System.Collections.Generic;
using CountlySDK;
using CountlySDK.CountlyCommon;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using Xunit;
using static CountlySDK.CountlyCommon.CountlyBase;

namespace TestProject_common
{
    public class ModuleLogListenerTests : IDisposable
    {
        public ModuleLogListenerTests()
        {
            CountlyImpl.SetPCLStorageIfNeeded();
            Countly.Halt();
            TestHelper.CleanDataFiles();
            UtilityHelper.LogListenerHook = null;
            Countly.IsLoggingEnabled = false;
        }

        public void Dispose()
        {
            UtilityHelper.LogListenerHook = null;
            Countly.IsLoggingEnabled = false;
        }

        // ---- Task 1: core hook behavior ----

        [Fact]
        /// <summary>Listener set, console flag OFF: receives the raw message + level (no [LEVEL] prefix).</summary>
        public void Listener_FlagOff_ReceivesRawMessageAndLevel()
        {
            Countly.IsLoggingEnabled = false;
            string gotMsg = null;
            LogLevel gotLevel = LogLevel.DEBUG;
            UtilityHelper.LogListenerHook = (m, l) => { gotMsg = m; gotLevel = l; };

            UtilityHelper.CountlyLogging("hello world", LogLevel.INFO);

            Assert.Equal("hello world", gotMsg);          // raw, unprefixed
            Assert.Equal(LogLevel.INFO, gotLevel);
            Assert.DoesNotContain("[INFO]", gotMsg);
        }

        [Fact]
        /// <summary>Every level is delivered with its exact enum value.</summary>
        public void Listener_ReceivesEveryLevel()
        {
            var seen = new List<LogLevel>();
            UtilityHelper.LogListenerHook = (m, l) => seen.Add(l);

            foreach (LogLevel lvl in new[] { LogLevel.VERBOSE, LogLevel.DEBUG, LogLevel.INFO, LogLevel.WARNING, LogLevel.ERROR }) {
                UtilityHelper.CountlyLogging("m", lvl);
            }

            Assert.Equal(new[] { LogLevel.VERBOSE, LogLevel.DEBUG, LogLevel.INFO, LogLevel.WARNING, LogLevel.ERROR }, seen);
        }

        [Fact]
        /// <summary>Listener fires even when the console flag is ON (console path is additive).</summary>
        public void Listener_FiresWhenFlagOn()
        {
            Countly.IsLoggingEnabled = true;
            int count = 0;
            UtilityHelper.LogListenerHook = (m, l) => count++;

            UtilityHelper.CountlyLogging("x", LogLevel.DEBUG);

            Assert.Equal(1, count);
        }

        [Fact]
        /// <summary>A throwing listener is swallowed and does not break subsequent logging.</summary>
        public void Listener_Throwing_DoesNotBreakLogging()
        {
            UtilityHelper.LogListenerHook = (m, l) => throw new InvalidOperationException("boom");
            // must not throw:
            UtilityHelper.CountlyLogging("first", LogLevel.ERROR);

            string got = null;
            UtilityHelper.LogListenerHook = (m, l) => got = m;
            UtilityHelper.CountlyLogging("second", LogLevel.DEBUG);

            Assert.Equal("second", got);   // SDK logging still works after a bad listener
        }

        // ---- Task 2: config surface ----

        [Fact]
        /// <summary>SetLogListener chains fluently and stores the delegate.</summary>
        public void ConfigApi_SetLogListener_ChainsAndStores()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            Action<string, LogLevel> cb = (m, l) => { };

            CountlyConfig returned = (CountlyConfig)cc.SetLogListener(cb);

            Assert.Same(cc, returned);
            Assert.Same(cb, cc.LogListener);
        }

        // ---- Task 3: init/halt lifecycle ----

        [Fact]
        /// <summary>Init wires the config listener; it receives SDK logs emitted during init.</summary>
        public void Init_WiresListenerFromConfig()
        {
            MockHttpServer server = new MockHttpServer((body) => "{\"result\":\"Success\"}");
            var messages = new List<string>();

            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            cc.SetLogListener((m, l) => messages.Add(m));

            Countly.Instance.Init(cc).Wait();

            Assert.NotEmpty(messages);
            Assert.Contains(messages, m => m.Contains("InitBase"));
            server.Dispose();
        }

        [Fact]
        /// <summary>Halt clears the listener hook; later logs no longer reach the callback.</summary>
        public void Halt_ClearsListener()
        {
            int count = 0;
            UtilityHelper.LogListenerHook = (m, l) => count++;

            Countly.Instance.HaltInternal().Wait();

            Assert.Null(UtilityHelper.LogListenerHook);

            // Halt itself logs a few messages before it clears the hook, so reset the baseline
            // here: the point is that a log emitted AFTER the hook is cleared reaches nobody.
            count = 0;
            UtilityHelper.CountlyLogging("after halt", LogLevel.DEBUG);
            Assert.Equal(0, count);
        }
    }
}
