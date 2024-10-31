using System;
using System.Diagnostics;
using CountlySDK;
using CountlySDK.Entities;

namespace CountlySampleUWP
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            Debug.WriteLine("Calling [App]");
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.Resuming += OnResuming;
            this.LeavingBackground += App_LeavingBackground;
            this.EnteredBackground += App_EnteredBackground;
            this.UnhandledException += App_UnhandledException;
        }

        private async void App_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            await Countly.RecordException(e.Message, e.Exception.StackTrace, null, true);
        }

        private async void App_EnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            Debug.WriteLine("Calling [App_EnteredBackground]");
            await Countly.Instance.SessionEnd();
        }

        private async void App_LeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            Debug.WriteLine("Calling [App_LeavingBackground]");
            await Countly.Instance.SessionBegin();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null) {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated) {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false) {
                if (rootFrame.Content == null) {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
            Launched();
        }

        private const string serverURL = "https://your.server.ly";
        private const string appKey = "YOUR_APP_KEY";

        private async void Launched()
        {
            if (serverURL.Equals("https://your.server.ly") || appKey.Equals("YOUR_APP_KEY")) {
                Debug.WriteLine("Please do not use default set of app key and server url");
            }

            Debug.WriteLine("Calling [Launched]");
            //create the Countly init object
            Countly.IsLoggingEnabled = true;
            var cc = new CountlyConfig {
                serverUrl = serverURL,
                appKey = appKey,
                appVersion = "1.2.3",
            };

            await Countly.Instance.Init(cc);
            await Countly.Instance.SessionBegin();
        }


        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            Debug.WriteLine("Calling [OnSuspending]");
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity


            deferral.Complete();
        }

        private async void OnResuming(object sender, object e)
        {
            Debug.WriteLine("Calling [OnResuming]");
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            Debug.WriteLine("Calling [OnActivated]");
        }
    }
}
