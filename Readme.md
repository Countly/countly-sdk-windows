##What's Countly?

[Countly](http://count.ly) is an innovative, real-time, open source mobile analytics application. It collects data from mobile devices, and visualizes this information to analyze mobile application usage and end-user behavior. There are two parts of Countly: the server that collects and analyzes data, and mobile SDK that sends this data. Both parts are open source with different licensing terms.

This repository includes the SDK for Windows Phone

##Installing Windows Phone SDK

  1. Download Countly Windows Phone SDK

  2. Extract all files to any folder

  3. In Solution Explorer open context menu on References folder

  4. Click the Add Reference button to open the Add Reference dialog box

  5. In the Add Reference dialog box, click on Browse and select **Countly.dll**, **Newtonsoft.Json.dll** and **OpenUDIDPhone.dll** from /SDK folder

##Set up SDK

Countly SDK requires **ID_CAP_IDENTITY_DEVICE** and **ID_CAP_NETWORKING** to be enabled. Open WMAppManifest.xml, click on Capabilities section and make them enabled

Add **using CountlySDK;** in the App.xaml.cs usings section

Call **Countly.StartSession("http://YOUR_SERVER", "YOUR_APP_KEY")** in App.xaml.cs **Application_Launching** and **Application_Activated** events, which requires your App key and the URL of your Countly server (use https://cloud.count.ly for Countly Cloud)

Call **Countly.EndSession()** in App.xaml.cs **Application_Deactivated** and **Application_Closing** events

<pre class="prettyprint">
...
   // Code to execute when the application is launching (eg, from Start)
   // This code will not execute when the application is reactivated
   private void Application_Launching(object sender, LaunchingEventArgs e)
   {
      Countly.StartSession("http://YOUR_SERVER", "YOUR_APP_KEY");
   }

   // Code to execute when the application is activated (brought to foreground)
   // This code will not execute when the application is first launched
   private void Application_Activated(object sender, ActivatedEventArgs e)
   {
      Countly.StartSession("http://YOUR_SERVER", "YOUR_APP_KEY");
   }
...
</pre>

**Note:** Make sure you use App Key (found under Management -> Applications) and not API Key. Entering API Key will not work.

##Record events

Add **using CountlySDK;** in the usings section

There are several Countly.RecordEvent methods with different parameters. You can choose one that most fits your event:

<pre class="prettyprint">
   Countly.RecordEvent("purchase");

   Countly.RecordEvent("purchase", 1);

   Countly.RecordEvent("purchase", 1, 0.99);

   Segmentation segmentation = new Segmentation();
   segmentation.Add("country", "Turkey");
   segmentation.Add("app_version", "1.0");
   Countly.RecordEvent("purchase", 1, segmentation);
</pre>

**Note:**
For record events from Background Agent, call **Countly.StartBackgroundSession("http://YOUR_SERVER", "YOUR_APP_KEY")** in OnInvoke method

<pre class="prettyprint">
   protected override async void OnInvoke(ScheduledTask task)
   {
      Countly.StartBackgroundSession("http://YOUR_SERVER", "YOUR_APP_KEY");
      await Countly.RecordEvent("purchase");
      NotifyComplete();
   }
</pre>

**Note:** use **async/await** in background agent. This allows to call NotifyComplete() only when record is processed 

##Exploring Windows Phone SDK Sample

  1. Download Countly Windows Phone SDK

  2. Extract all files to any folder

  3. Open **Countly.sln** file with Visual Studio 2012 or higher version

  4. Now you have two projects in your solution : Countly (SDK library) and CountlySample (quickstarter project).

  5. Open App.xaml.cs and MainPage.xaml.cs and type **ServerUrl** and **AppKey** to prepared fields

  6. Right click to CountlySample project and click **"Set as StartUp Project"**

  7. You can run your application on a device or an emulator provided by Visual Studio Windows Phone 8 development kit.

##Use some extra features

How can I make sure that requests to Countly are sent correctly?

Enable logging:

<pre class="prettyprint">
    Countly.IsLoggingEnabled = true;
</pre>

You can turn it on and off in any place

##Other

Check Countly Server source code here: 

- [Countly Server (countly-server)](https://github.com/Countly/countly-server)

There are also other Countly SDK repositories below:

- [Countly iOS SDK](https://github.com/Countly/countly-sdk-ios)
- [Countly Android SDK](https://github.com/Countly/countly-sdk-android)
- [Countly Windows Phone SDK](https://github.com/Countly/countly-sdk-windows-phone)
- [Countly Blackberry Webworks SDK](https://github.com/Countly/countly-sdk-blackberry-webworks)
- [Countly Blackberry Cascades SDK](https://github.com/craigmj/countly-sdk-blackberry10-cascades) (Community supported)
- [Countly Mac OS X SDK](https://github.com/mrballoon/countly-sdk-osx) (Community supported)
- [Countly Appcelerator Titanium SDK](https://github.com/euforic/Titanium-Count.ly) (Community supported)
- [Countly Unity3D SDK](https://github.com/Countly/countly-sdk-unity) (Community supported)


##How can I help you with your efforts?
Glad you asked. We need ideas, feedbacks and constructive comments. All your suggestions will be taken care with upmost importance. 

We are on [Twitter](http://twitter.com/gocountly) and [Facebook](http://www.facebook.com/Countly) if you would like to keep up with our fast progress!

For community support page, see [http://support.count.ly](http://support.count.ly "Countly Support").

