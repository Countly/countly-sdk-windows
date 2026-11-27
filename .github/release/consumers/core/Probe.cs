using System;
using System.Reflection;
using CountlySDK;
using CountlySDK.Entities;

namespace CountlyConsumer
{
    /// <summary>
    /// Uses the Countly package the way an integrator's app does, built against one target framework.
    /// </summary>
    internal static class Probe
    {
        /// <summary>
        /// Prints what the runtime loaded: the SDK assembly, its file version, the version and name it reports to the
        /// server, and the assembly providing Task. Given a server address and an app key, it then starts a session,
        /// records an event and ends the session there, and waits until the SDK has delivered the stored requests.
        /// </summary>
        /// <param name="args">Nothing, or the server address and the app key.</param>
        /// <returns>0 when every step ran.</returns>
        private static int Main(string[] args)
        {
            Assembly sdk = typeof(Countly).Assembly;
            Console.WriteLine("assembly=" + sdk.FullName);
            Console.WriteLine("fileVersion=" + ((AssemblyFileVersionAttribute)Attribute.GetCustomAttribute(sdk, typeof(AssemblyFileVersionAttribute))).Version);
            // The version the SDK sends as sdk_version is a protected constant, so only reflection reads it from outside.
            FieldInfo sdkVersion = typeof(Countly).BaseType.GetField("sdkVersion", BindingFlags.NonPublic | BindingFlags.Static);
            Console.WriteLine("sdkVersion=" + (sdkVersion == null ? "missing" : sdkVersion.GetValue(null)));
            Console.WriteLine("sdkName=" + Countly.Instance.sdkName());
            Console.WriteLine("runtime=" + Environment.Version);
            Console.WriteLine("tasks=" + typeof(System.Threading.Tasks.Task).Assembly.GetName().Name);
            if (args.Length < 2)
            {
                return 0;
            }

            CountlyConfig config = new CountlyConfig { serverUrl = args[0], appKey = args[1], appVersion = "1.0" };
            Countly.Instance.Init(config).Wait();
            Countly.Instance.SessionBegin().Wait();
            Countly.RecordEvent("release_probe").Wait();
            Countly.Instance.SessionEnd().Wait();
            Countly.Instance.AttemptToSendStoredRequests().Wait();
            Console.WriteLine("sent=true");
            // Ends the process right after delivery, so no SDK thread on any flavor can hold a run until the tool's timeout.
            Environment.Exit(0);
            return 0;
        }
    }
}
