using System;
using System.Reflection;
using CountlySDK;
using CountlySDK.UI;
using Microsoft.Web.WebView2.Core;

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
            bool available = WebView2Runtime.IsAvailable(out version);
            Console.WriteLine("webView2=" + (available ? version : "missing"));
            if (!available)
            {
                // IsAvailable swallows the reason, so the probe asks WebView2 itself to report it.
                try
                {
                    CoreWebView2Environment.GetAvailableBrowserVersionString();
                }
                catch (Exception error)
                {
                    Console.WriteLine("webView2Error=" + error.GetType().FullName + ": " + error.Message.Replace('\r', ' ').Replace('\n', ' '));
                }
            }
            return 0;
        }
    }
}
