/*
 * The MIT License (MIT)
 * Copyright (c) 2015 Microsoft
 * Permission is hereby granted, free of charge, to any person obtaining a copy 
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.  
 */
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Windows.Devices.Enumeration;
using Windows.UI;
using Windows.UI.Xaml.Controls.Maps;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml;
using Windows.UI.Popups;
using Windows.UI.Core;
using Lumia.Sense;
using Tracker = Lumia.Sense.TrackPointMonitor;
using Windows.Foundation;
using Tracks.Utilities;
using System.Linq;

/// <summary>
/// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556
/// </summary>
namespace Tracks
{
    /// <summary>
    /// MapControl class
    /// </summary>
    public sealed partial class MapPage
    {
        #region Private members
        /// <summary>
        /// Tracker object that identifies and maintains a list of visited points
        /// </summary>
        private Tracker _tracker;

        /// <summary>
        /// Used to verify if the option is set to full screen 
        /// </summary>
        private bool _fullScreen;

        /// <summary>
        /// holds all the pushpins in the map
        /// </summary>
        IList<PushPin> pins = null;

        /// <summary>
        /// Collection of points
        /// </summary>
        IList<TrackPoint> points = null;

        /// <summary>
        /// List of activities for a place
        /// </summary>
        List<Activity> activitiesToShow = new List<Activity>();

        /// <summary>
        /// Places object that identifies and maintains a list of visited places
        /// </summary>
        PlaceMonitor _placeMonitor = null;
        #endregion

        /// <summary>
        /// Initialize SensorCore
        /// </summary>
        private async void InitCore()
        {
            // await ActivityReader.Instance.Initialize();
            try
            {
                // Following code assumes that device has new software(SensorCoreSDK1.1 based)
                if( !( await TrackPointMonitor.IsSupportedAsync() ) )
                {
                    MessageDialog dlg = new MessageDialog( "Unfortunately this device does not support viewing TrackPoints places" );
                    await dlg.ShowAsync();
                    Application.Current.Exit();
                }
                else
                {
                    uint apiSet = await SenseHelper.GetSupportedApiSetAsync();
                    MotionDataSettings settings = await SenseHelper.GetSettingsAsync();
                    if( !settings.LocationEnabled )
                    {
                        MessageDialog dlg = new MessageDialog( "In order to collect and view visited tracks you need to enable location in system settings. Do you want to open settings now? if no, applicatoin will exit", "Information" );
                        dlg.Commands.Add( new UICommand( "Yes", new UICommandInvokedHandler( async ( cmd ) => await SenseHelper.LaunchLocationSettingsAsync() ) ) );
                        dlg.Commands.Add( new UICommand( "No", new UICommandInvokedHandler( ( cmd ) =>
                        {
                            Application.Current.Exit();
                        } ) ) );
                        await dlg.ShowAsync();
                    }
                    if( !settings.PlacesVisited )
                    {
                        MessageDialog dlg = null;
                        if( settings.Version < 2 )
                        {
                            //device which has old motion data settings.
                            //this is equal to motion data settings on/off in old system settings(SDK1.0 based)
                            dlg = new MessageDialog( "In order to collect and view visited tracks you need to enable Motion data in Motion data settings. Do you want to open settings now? if no, application will exit", "Information" );
                        }
                        else
                        {
                            dlg = new MessageDialog( "In order to collect and view visited tracks you need to 'enable Places visited' and 'DataQuality to detailed' in Motion data settings. Do you want to open settings now? if no, application will exit", "Information" );
                        }
                        dlg.Commands.Add( new UICommand( "Yes", new UICommandInvokedHandler( async ( cmd ) => await SenseHelper.LaunchSenseSettingsAsync() ) ) );
                        dlg.Commands.Add( new UICommand( "No", new UICommandInvokedHandler( ( cmd ) =>
                        {
                            Application.Current.Exit();
                        } ) ) );
                        await dlg.ShowAsync();
                    }
                }
            }
            catch( Exception )
            {
            }

            if( _tracker == null )
            {
                // Init SensorCore
                if( await CallSensorcoreApiAsync( async () =>
                {
                    _tracker = await Tracker.GetDefaultAsync();
                    _placeMonitor = await PlaceMonitor.GetDefaultAsync();
                } ) )
                {
                    await ActivityReader.Instance().Initialize();
                    await _sync.WaitAsync();
                    try
                    {
                        await DrawRoute();
                    }
                    finally
                    {
                        _sync.Release();
                    }
                    FilterTime.Text = _selected != null ? _selected.Name : _loader.GetString( "NoTimespanSelected/Text" );
                }
            }
        }

        /// <summary>
        /// Draw the route points and the lines connecting them
        /// </summary>
        private async Task DrawRoute()
        {
            TracksMap.MapElements.Clear();
            TracksMap.Children.Clear();
            if( pins != null )
            {
                pins.Clear();
                pins = null;
            }
            pins = new List<PushPin>();
            if( points != null )
            {
                points.Clear();
                points = null;
            }
            
            if( await CallSensorcoreApiAsync( async () =>
            {
                // Get selected day routes, else all routes from last 10 days
                if( _selected != null && !_selected.Name.Equals( "All", StringComparison.CurrentCultureIgnoreCase ) )
                {
                    System.Diagnostics.Debug.WriteLine( "DrawRoute: " + _selected.Name );
                    points = await _tracker.GetTrackPointsAsync( _selected.Day, TimeSpan.FromHours( 24 ) );
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine( "DrawRoute: Drawing all" );
                    points = await _tracker.GetTrackPointsAsync( DateTime.Now - TimeSpan.FromDays( 10 ), TimeSpan.FromDays( 10 ) );
                }
            } ) )
            {
                // Make sure that there were RoutePoints for the timespan
                if( points!=null && points.Count > 0 )
                {
                    // Returns list of activies occured during given time period
                    await CallSensorcoreApiAsync(async () =>
                    {
                        ActivityReader.Instance().History = await ActivityReader.Instance().ActivityMonitorProperty.GetActivityHistoryAsync(DateTime.Today - TimeSpan.FromDays(10), TimeSpan.FromDays(10));
                    });
                    TracksMap.Center = new Geopoint( points[ 0 ].Position );
                    var previous = new BasicGeoposition();
                    bool first = true;
                    int i = 0;
                    for (int j = 1; j < points.Count; j++)
                    {
                           activitiesToShow.Clear();
                           for (int k = 1; k < ActivityReader.Instance().History.Count; k++)
                           {
                               // Compare time of entry to the last location and the current location with the activity timestamp
                               // Retrieve the activities in a list
                               if ((ActivityReader.Instance().History.ElementAt(k-1).Timestamp.ToLocalTime()>=points.ElementAt(j - 1).Timestamp.ToLocalTime()) && (ActivityReader.Instance().History.ElementAt(k).Timestamp.ToLocalTime() < points.ElementAt(j).Timestamp.ToLocalTime()))
                               {
                                   if (!activitiesToShow.Contains(ActivityReader.Instance().History.ElementAt(k).Mode))
                                   {
                                       //Add the activity to the list
                                       activitiesToShow.Add(ActivityReader.Instance().History.ElementAt(k).Mode);
                                   }
                               }
                           }                    
                        int time = 0;
                        if (_filterTime.Equals("0 - 10 minutes"))
                        {
                            time = 10;
                        }
                        else if (_filterTime.Equals("15 minutes"))
                        {
                            time = 15;
                        }
                        else if (_filterTime.Equals("30 minutes"))
                        {
                            time = 30;
                        }
                        else if (_filterTime.Equals("60 minutes"))
                        {
                            time = 60;
                        }
                        if (points[j].LengthOfStay.CompareTo(TimeSpan.FromMinutes(time)) <= 0 || _filterTime.ToLower().Equals("all"))
                        {
                            await CreateMapIcon(points[j], i);
                            // Create a line connecting to the previous map point
                            if (!first)
                            {
                                var mapShape = new MapPolyline
                                {
                                    Path = new Geopath(new List<BasicGeoposition> { previous, points[j].Position }),
                                    StrokeThickness = 3,
                                    StrokeColor = Color.FromArgb(255, 100, 100, 255),
                                    StrokeDashed = false
                                    
                                };
                                TracksMap.MapElements.Add(mapShape);
                            }
                            else
                            {
                                TracksMap.Center = new Geopoint(points[j].Position);
                                TracksMap.ZoomLevel = 13;
                                first = false;
                            }
                            previous = points[j].Position;
                            i = i + 1;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Create icon for route points, smaller for those where lenght of stay was under 10 mins
        /// </summary>
        /// <param name="p">TrackPoint that will be drawn on the map.</param>
        private async Task CreateMapIcon( TrackPoint point, int pos)
        {
            Place place = null;
            await CallSensorcoreApiAsync( async () => place = await _placeMonitor.GetPlaceAtAsync( point.Timestamp ) );
            pins.Add(new PushPin(point, new Geopoint(point.Position), place != null ? place.Kind.ToString() : "unknown", DisplayMembers(activitiesToShow)));
            MapControl.SetLocation( pins[ pos ], new Geopoint( point.Position ) );
            MapControl.SetNormalizedAnchorPoint( pins[ pos ], new Point( 0.15, 1 ) );
            TracksMap.Children.Add( pins[ pos ] );
        }

        /// <summary>
        /// Error handling for device level error conditions
        /// </summary>
        /// <param name="sender">The control that the action is for.</param>
        /// <param name="args">Parameter that contains data for the AccesChanged event.</param>
        public async void OnAccessChanged( DeviceAccessInformation sender, DeviceAccessChangedEventArgs args )
        {
            await Dispatcher.RunAsync( CoreDispatcherPriority.Normal, () =>
            {
                if( DeviceAccessStatus.DeniedByUser == args.Status )
                {
                    Debug.WriteLine( "Location has been disabled by the user. Enable access through the settings charm." );
                }
                else if( DeviceAccessStatus.DeniedBySystem == args.Status )
                {
                    Debug.WriteLine( "Location has been disabled by the system. The administrator of the device must enable location access through the location control panel." );
                }
                else if( DeviceAccessStatus.Unspecified == args.Status )
                {
                    Debug.WriteLine( "Location has been disabled by unspecified source. The administrator of the device may need to enable location access through the location control panel, then enable access through the settings charm." );
                }
                else if( DeviceAccessStatus.Allowed == args.Status )
                {
                }
                else
                {
                    Debug.WriteLine( "Unknown device access information status" );
                }
            } );
        }

        /// <summary>
        /// Display activities for given list
        /// </summary>
        /// <param name="activities">List of activities</param>
        /// <returns>String item</returns>  
        public string DisplayMembers(List<Activity> activities)
        {
            string displayActivities = string.Empty;
            if (activities.Count != 0)
            {
                displayActivities = string.Join(", ", activities.ToList());
            }
            else
            {
                displayActivities = "Idle";
            }
            return displayActivities;
        }

        /// <summary>
        /// Performs asynchronous Sensorcore SDK operation and handles any exceptions
        /// </summary>
        /// <param name="action">Action for which the SensorCore will be activated.</param>
        /// <returns><c>true</c> if call was successful, <c>false</c> otherwise</returns>
        public async Task<bool> CallSensorcoreApiAsync( Func<Task> action )
        {
            Exception failure = null;
            try
            {
                await action();
            }
            catch( Exception e )
            {
                failure = e;
            }
            if( failure != null )
            {
                MessageDialog dialog;
                switch( SenseHelper.GetSenseError( failure.HResult ) )
                {
                    case SenseError.LocationDisabled:
                        dialog = new MessageDialog( "Location has been disabled. Do you want to open Location settings now? If you choose no, application will exit.", "Information" );
                        dialog.Commands.Add( new UICommand( "Yes", async cmd => await SenseHelper.LaunchLocationSettingsAsync() ) );
                        dialog.Commands.Add( new UICommand( "No" ) );
                        await dialog.ShowAsync();
                        new System.Threading.ManualResetEvent( false ).WaitOne( 500 );
                        return false;
                    case SenseError.SenseDisabled:
                        dialog = new MessageDialog( "Motion data has been disabled. Do you want to open Motion data settings now? If you choose no, application will exit.", "Information" );
                        dialog.Commands.Add( new UICommand( "Yes", async cmd => await SenseHelper.LaunchSenseSettingsAsync() ) );
                        dialog.Commands.Add( new UICommand( "No" ) );
                        await dialog.ShowAsync();
                        new System.Threading.ManualResetEvent( false ).WaitOne( 500 );
                        return false;
                    default:
                        return false;
                }
            }
            return true;
        }
    }
}
