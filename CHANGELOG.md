20.11.0
* Fixed bug that occurred while getting device and system information on restricted machines.
* Removed deprecated function "EndSession"
* Removed deprecated function "StartSession"
* [net35, net40] Removed deprecated function "SetCustomDataPath"
* PLC flavour (.NETPortable4.5-Profile259) is deprecated and will be removed on next major release

20.05.1
* Fixed bug that occured when no cpu_id was got returned from OS
* Fixed bug with API requests, which failed if the request data contained '&'

20.05
* Added sdk metadata to requests
* Improved handling of failed API requests
* Fixed null access bug in user details
* Fixed session duration bug in end session requests

19.08.1
* Fixed a consent bug regarding "view" consent.

19.08
* Added functionality to manually record views

19.02.1
* [Common, UWP] Reworked Storage for UWP so that it does not use PCLStorage

19.02
* Added support for uploading larger server requests
* Fixed metrics that might reveal more information about users then wanted

18.10.0
* Removing support for wp7 and wp8 platform targets
* SDK should now have xml documentation
* Refactored internals so that they are the same among all platform targets. Each target has it's own platform specific overrides.
* Assemblies are now strong name signed
* Transformed the 'Countly' from a static class into a singleton
* Added new init and session control calls
* [net35] Adding option for providing custom data path.
* DeviceId is now saved and loaded on each start
* Adding new ways for generating deviceId
* Added functionality to used developer supplied deviceId
* Added functionality to change deviceId with or without on server merge
* Added functionality to create events with duration
* Added functinality to send location
* Adding functionality for user feature consent
* Added locale to metrics
* Fixed deserialization bugs
* Fixed bug where crashes would not show up in dashboard
* Fixed a timestamp bug


18.1.0
* Fixing multithreading bug
* Adding multithreading protections
* Adding UWP as target platform