using System;
using System.IO;
using Microsoft.Web.WebView2.Core;

namespace CountlySDK.UI
{
    /// <summary>Detects whether the WebView2 Evergreen Runtime is available on the machine.</summary>
    public static class WebView2Runtime
    {
#if NETFRAMEWORK
        private static readonly object LoaderLock = new object();
        private static bool _loaderChosen;
#endif

        /// <summary>
        /// Returns true if the WebView2 runtime is installed. <paramref name="version"/> receives
        /// the installed version string, or null if unavailable. Never throws.
        /// </summary>
        public static bool IsAvailable(out string version)
        {
            version = null;
            try {
                UseAppLoader();
                version = CoreWebView2Environment.GetAvailableBrowserVersionString();
                return !string.IsNullOrEmpty(version);
            } catch {
                return false;
            }
        }

        /// <summary>
        /// On .NET Framework, points WebView2 at the WebView2Loader.dll the app ships for the process architecture before
        /// WebView2 loads one. Otherwise Windows may load an older copy from a folder on PATH, which lacks functions this
        /// package calls. Runs once, and does nothing on .NET or when the app ships no matching loader.
        /// </summary>
        private static void UseAppLoader()
        {
#if NETFRAMEWORK
            lock (LoaderLock) {
                if (_loaderChosen) {
                    return;
                }
                _loaderChosen = true;
                string folder = AppLoaderFolder(AppDomain.CurrentDomain.BaseDirectory, Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"));
                if (folder != null) {
                    try {
                        CoreWebView2Environment.SetLoaderDllFolderPath(folder);
                    } catch (InvalidOperationException) {
                        // WebView2 refuses once a loader is loaded, and keeps that one.
                    }
                }
            }
#endif
        }

        /// <summary>
        /// The folder of the app holding a WebView2Loader.dll built for the process architecture: the
        /// runtimes\win-{arch}\native folder WebView2's build files create, or the app's base directory itself.
        /// </summary>
        /// <param name="baseDirectory">The app's base directory.</param>
        /// <param name="processorArchitecture">The process's PROCESSOR_ARCHITECTURE: AMD64, ARM64 or x86.</param>
        /// <returns>The folder, or null when neither holds a loader for that architecture.</returns>
        internal static string AppLoaderFolder(string baseDirectory, string processorArchitecture)
        {
            string arch;
            ushort machine;
            switch ((processorArchitecture ?? "").ToUpperInvariant()) {
                case "AMD64":
                    arch = "x64";
                    machine = 0x8664;
                    break;
                case "ARM64":
                    arch = "arm64";
                    machine = 0xAA64;
                    break;
                case "X86":
                    arch = "x86";
                    machine = 0x014C;
                    break;
                default:
                    return null;
            }
            foreach (string folder in new[] { Path.Combine(baseDirectory, "runtimes", "win-" + arch, "native"), baseDirectory }) {
                if (MachineOf(Path.Combine(folder, "WebView2Loader.dll")) == machine) {
                    return folder;
                }
            }
            return null;
        }

        /// <summary>The machine type in a DLL's PE header, such as 0x8664 for x64.</summary>
        /// <param name="path">The DLL.</param>
        /// <returns>The machine type, or 0 when the file is missing or is not a PE image.</returns>
        internal static ushort MachineOf(string path)
        {
            try {
                using (BinaryReader reader = new BinaryReader(File.OpenRead(path))) {
                    reader.BaseStream.Seek(0x3C, SeekOrigin.Begin);
                    reader.BaseStream.Seek(reader.ReadInt32(), SeekOrigin.Begin);
                    return reader.ReadUInt32() == 0x00004550 ? reader.ReadUInt16() : (ushort)0;
                }
            } catch (Exception) {
                return 0;
            }
        }
    }
}
