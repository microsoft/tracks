Tracks
==========

Tracks is Track Point Monitor API sample focusing on visualising the routes on the screen.

1. Instructions
--------------------------------------------------------------------------------

Learn about the Lumia SensorCore SDK from the Lumia Developer's Library. The
example requires the Lumia SensorCore SDK's NuGet package but will retrieve it
automatically (if missing) on first build.

To build the application you need to have Windows 8.1 and Windows Phone SDK 8.1
installed.

Using the Windows Phone 8.1 SDK:

1. Open the SLN file: File > Open Project, select the file `tracks.sln`
2. Remove the "AnyCPU" configuration (not supported by the Lumia SensorCore SDK)
or simply select ARM
3. Select the target 'Device'.
4. Press F5 to build the project and run it on the device.

Please see the official documentation for
deploying and testing applications on Windows Phone devices:
http://msdn.microsoft.com/en-us/library/gg588378%28v=vs.92%29.aspx


2. Implementation
--------------------------------------------------------------------------------

**Important files and classes:**

The core of this app's implementation is in MapPage.xaml.Sensors.cs. We can get 
the known track points from the TrackPointMonitor class by querying the 
GetTrackPointsAsync method. The list will include all track points that the phone 
has registered within the timespan provided.

The API is called through the CallSensorcoreApiAsync() helper function, which helps
handling the typical errors, like required features being disabled in the system
settings.

**Required capabilities:**

The SensorSore SDK (via its NuGet package) automatically inserts in the manifest
file the capabilities required for it to work:

    <DeviceCapability Name="location" />
    <m2:DeviceCapability Name="humaninterfacedevice">
      <m2:Device Id="vidpid:0421 0716">
        <m2:Function Type="usage:ffaa 0001" />
        <m2:Function Type="usage:ffee 0001" />
        <m2:Function Type="usage:ffee 0002" />
        <m2:Function Type="usage:ffee 0003" />
        <m2:Function Type="usage:ffee 0004" />
      </m2:Device>
    </m2:DeviceCapability>
	
	
3. License
--------------------------------------------------------------------------------

See the license text file delivered with this project. The license file is also
available online at https://github.com/nokia-developer/tracks/blob/master/License.txt


4. Version history
--------------------------------------------------------------------------------

* Version 1.0: The first release.


5. See also
--------------------------------------------------------------------------------

The projects listed below are exemplifying the usage of the other SensorCore APIs

* Steps -  https://github.com/nokia-developer/steps
* Places - https://github.com/nokia-developer/places
* Recorder - https://github.com/nokia-developer/recorder
* Activities - https://github.com/nokia-developer/activities
