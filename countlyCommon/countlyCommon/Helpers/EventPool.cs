using System;
using System.Collections.Generic;
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

        internal EventPool(int eventCacheSize, Func<string, string, List<CountlyEvent>, Task> bufferCallback)
        {
            this.eventCacheSize = eventCacheSize;
            this.bufferCallback = bufferCallback;
            appEventCacheSize = eventCacheSize * 10;
            globalEventCacheSize = appEventCacheSize * 10;

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
                    bufferCallback.Invoke(deviceId, appKey, events);
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

        private List<CountlyEvent> RemoveAndGet(string appKey, string deviceId)
        {
            perAppKeyEventCache.TryGetValue(appKey, out Dictionary<string, List<CountlyEvent>> eventCache);
            eventCache.TryGetValue(deviceId, out List<CountlyEvent> events);
            eventCache.Remove(deviceId);
            appEventCacheCounts[appKey] -= events.Count();
            globalEventCount -= events.Count();
            return events;
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
                bufferCallback.Invoke(deviceId, appKey, events);
            }
        }

        private void CallCallbackForAppKey(string appKey)
        {
            foreach (string key in perAppKeyEventCache[appKey].Keys) {
                bufferCallback.Invoke(key, appKey, RemoveAndGet(appKey, key));
            }
        }

        internal void Dump()
        {
            foreach (string key in perAppKeyEventCache.Keys) {
                CallCallbackForAppKey(key);
            }
        }
    }
}
