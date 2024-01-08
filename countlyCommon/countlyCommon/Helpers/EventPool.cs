using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CountlySDK.Entities;

namespace CountlySDK.CountlyCommon
{
    internal class EventPool
    {
        private readonly int eventCacheSize;
        private readonly int appEventCacheSize;
        private readonly int globalEventCacheSize;

        private readonly Dictionary<string, Dictionary<string, List<CountlyEvent>>> perAppKeyEventCache;
        private readonly Dictionary<string, int> appEventCacheCounts;
        private readonly Func<string, string, List<CountlyEvent>, Task> bufferCallback;
        private int globalEventCount;

        internal EventPool(int eventCacheSize, int appEQSize, int serverEQSize, Func<string, string, List<CountlyEvent>, Task> bufferCallback)
        {
            this.eventCacheSize = eventCacheSize;
            this.bufferCallback = bufferCallback;
            appEventCacheSize = appEQSize;
            globalEventCacheSize = serverEQSize;

            perAppKeyEventCache = new Dictionary<string, Dictionary<string, List<CountlyEvent>>>();
            appEventCacheCounts = new Dictionary<string, int>();
        }

        internal void Put(string deviceId, string appKey, CountlyEvent record)
        {
            lock (perAppKeyEventCache) {
                List<CountlyEvent> events = EnsureAndGet(deviceId, appKey);

                if (!appEventCacheCounts.TryGetValue(appKey, out int appCount)) {
                    appCount = 0;
                }

                appEventCacheCounts[appKey] = ++appCount;
                globalEventCount++;

                events.Add(record);

                CheckAndProcessLimits(appKey, deviceId, events, appCount);
            }
        }

        private List<CountlyEvent> EnsureAndGet(string deviceId, string appKey)
        {
            List<CountlyEvent> events;

            if (perAppKeyEventCache.TryGetValue(appKey, out Dictionary<string, List<CountlyEvent>> appSpecificEvents)) {
                if (!appSpecificEvents.TryGetValue(deviceId, out events)) {
                    events = new List<CountlyEvent>(eventCacheSize);
                    appSpecificEvents[deviceId] = events;
                }

                if (events.Count() >= eventCacheSize) {
                    bufferCallback.Invoke(deviceId, appKey, RemoveAndGet(appKey, deviceId, events));
                    events = InitEventList(appKey, deviceId);
                }

            } else {
                perAppKeyEventCache[appKey] = new Dictionary<string, List<CountlyEvent>>();
                events = InitEventList(appKey, deviceId);
            }

            return events;
        }

        private List<CountlyEvent> InitEventList(string appKey, string deviceId)
        {
            List<CountlyEvent> events = new List<CountlyEvent>(eventCacheSize);
            perAppKeyEventCache[appKey][deviceId] = events;
            return events;
        }

        private List<CountlyEvent> RemoveAndGet(string appKey, string deviceId, List<CountlyEvent> events = null)
        {
            lock (perAppKeyEventCache) {
                perAppKeyEventCache.TryGetValue(appKey, out Dictionary<string, List<CountlyEvent>> eventCache);
                if (events == null) {
                    eventCache.TryGetValue(deviceId, out events);

                }
                eventCache.Remove(deviceId);
                appEventCacheCounts[appKey] -= events.Count();
                globalEventCount -= events.Count();
                return events;
            }
        }

        private void CheckAndProcessLimits(string appKey, string deviceId, List<CountlyEvent> events, int appCount)
        {
            if (globalEventCount >= globalEventCacheSize) {
                foreach (string key in perAppKeyEventCache.Keys) {
                    CallCallbackForAppKey(key);
                }
            } else if (appCount >= appEventCacheSize) {
                CallCallbackForAppKey(appKey);
            } else if (events.Count() >= eventCacheSize) {
                bufferCallback.Invoke(deviceId, appKey, RemoveAndGet(appKey, deviceId, events));
            }
        }

        private void CallCallbackForAppKey(string appKey)
        {
            lock (perAppKeyEventCache) {
                while (perAppKeyEventCache[appKey].Count > 0) {
                    KeyValuePair<string, List<CountlyEvent>> kp = perAppKeyEventCache[appKey].ElementAt(0);
                    bufferCallback.Invoke(kp.Key, appKey, RemoveAndGet(appKey, kp.Key, kp.Value));
                }
            }
        }

        internal void Dump()
        {
            lock (perAppKeyEventCache) {
                foreach (string key in perAppKeyEventCache.Keys) {
                    CallCallbackForAppKey(key);
                }
            }
        }

        internal void PrintState()
        {
            lock (perAppKeyEventCache) {
                Debug.WriteLine("Current Event Sizes For each APP and DEVICE");
                int totalCount = 0;
                foreach (string key in perAppKeyEventCache.Keys) {
                    Debug.WriteLine($"APP {key}");
                    int totalForApp = 0;
                    foreach (string device in perAppKeyEventCache[key].Keys) {
                        int count = perAppKeyEventCache[key][device].Count;
                        totalForApp += count;
                        Debug.WriteLine($"Event count for the device {device}: {count}");
                    }

                    Debug.WriteLine("Event count for this app " + totalForApp);
                    Debug.WriteLine("Calculated internal event count " + appEventCacheCounts[key]);
                    totalCount += totalForApp;
                }

                Debug.WriteLine("Total calculated count " + totalCount);
                Debug.WriteLine("Total internal count " + globalEventCount);
            }
        }
    }
}
