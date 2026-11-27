using System;
using System.Reflection;
using CountlySDK;
using CountlySDK.UI;

namespace CountlyConsumer
{
    /// <summary>
    /// Uses the Countly WebView2 UI package the way an integrator's app does, built against one target framework.
    /// </summary>
    internal static class Probe
    {
        /// <summary>
        /// Prints what the runtime loaded: the UI assembly and its file version, the core SDK assembly it bound to,
        /// and the WebView2 runtime the UI found on the machine.
        /// </summary>
        /// <returns>0 when every step ran.</returns>
        private static int Main()
        {
            Assembly ui = typeof(WebView2Runtime).Assembly;
            Console.WriteLine("assembly=" + ui.FullName);
            Console.WriteLine("fileVersion=" + ((AssemblyFileVersionAttribute)Attribute.GetCustomAttribute(ui, typeof(AssemblyFileVersionAttribute))).Version);
            Console.WriteLine("core=" + typeof(Countly).Assembly.FullName);
            Console.WriteLine("runtime=" + Environment.Version);
            string version;
            Console.WriteLine("webView2=" + (WebView2Runtime.IsAvailable(out version) ? version : "missing"));
            return 0;
        }
    }
}
