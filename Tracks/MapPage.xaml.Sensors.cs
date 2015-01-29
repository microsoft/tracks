/*
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
using Tracks.Utilities;
using Tracker = Lumia.Sense.TrackPointMonitor;

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
        #endregion

        /// <summary>
        /// Initialize SensorCore
        /// </summary>
        private async Task InitCore()
        {
            // IsSupportedAsync is not implemented by the simulator, comment out for the TrackPointMonitorSimulator
            if (await Tracker.IsSupportedAsync())
            {
                if (_tracker == null)
                {
                    // Init SensorCore
                    if (await CallSensorcoreApiAsync(async () => { _tracker = await Tracker.GetDefaultAsync(); }))
                    {
                        Debug.WriteLine("RouteTracker initialized.");
                    }
                    else
                        return;
                }
                // Activate and deactivate the SensorCore when the visibility of the app changes
                Window.Current.VisibilityChanged += async (oo, ee) =>
                {
                    if (_tracker != null)
                    {
                        if (!ee.Visible)
                        {
                            await CallSensorcoreApiAsync(async () => { await _tracker.DeactivateAsync(); });
                        }
                        else
                        {
                            await CallSensorcoreApiAsync(async () => { await _tracker.ActivateAsync(); });
                        }
                    }
                };
            }
            else
            {
                MessageDialog dialog;
                dialog = new MessageDialog("Your device doesn't support Motion Data. Application will be closed", "Information");
                dialog.Commands.Add(new UICommand("OK"));
                await dialog.ShowAsync();
                new System.Threading.ManualResetEvent(false).WaitOne(500);
                Application.Current.Exit();
            }
        }

        /// <summary>
        /// Draw the route points and the lines connecting them
        /// </summary>
        private async void DrawRoute()
        {
            TracksMap.MapElements.Clear();
            IList<TrackPoint> points = null;
            if (await CallSensorcoreApiAsync(async () =>
            {
                // Get selected day routes, else all routes from last 10 days
                if (_selected != null && !_selected.Name.Equals("All", StringComparison.CurrentCultureIgnoreCase))
                {
                    System.Diagnostics.Debug.WriteLine("DrawRoute: " + _selected.Name);
                    points = await _tracker.GetTrackPointsAsync(_selected.Day, TimeSpan.FromHours(24));
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("DrawRoute: Drawing all");
                    points = await _tracker.GetTrackPointsAsync(DateTime.Now - TimeSpan.FromDays(10), TimeSpan.FromDays(10));
                }
            }))
            {
                // Make sure that there were RoutePoints for the timespan
                if (points.Count > 0)
                {
                    TracksMap.Center = new Geopoint(points[0].Position);
                    var previous = new BasicGeoposition();
                    bool first = true;
                    foreach (var p in points)
                    {
                        switch (_filterTime)
                        {
                            case 0:
                                break;
                            case 10:
                                if (p.LengthOfStay.CompareTo(TimeSpan.FromMinutes(10)) <= 0)
                                    continue;
                                break;
                            case 15:
                                if (p.LengthOfStay.CompareTo(TimeSpan.FromMinutes(15)) <= 0)
                                    continue;
                                break;
                            case 30:
                                if (p.LengthOfStay.CompareTo(TimeSpan.FromMinutes(30)) <= 0)
                                    continue;
                                break;
                            case 60:
                                if (p.LengthOfStay.CompareTo(TimeSpan.FromMinutes(60)) <= 0)
                                    continue;
                                break;
                        }
                        CreateMapIcon(p);
                        // Create a line connecting to the previous map point
                        if (!first)
                        {
                            var mapShape = new MapPolyline
                            {
                                Path = new Geopath(new List<BasicGeoposition> {previous, p.Position}),
                                StrokeThickness = 3,
                                StrokeColor = Color.FromArgb(255, 100, 100, 255),
                                StrokeDashed = false
                            };
                            TracksMap.MapElements.Add(mapShape);
                        }
                        else
                        {
                            TracksMap.Center = new Geopoint(p.Position);
                            TracksMap.ZoomLevel = 16;
                            first = false;
                        }
                        previous = p.Position;
                    }
                }
            }
        }

        /// <summary>
        /// Create icon for route points, smaller for those where lenght of stay was under 10 mins
        /// </summary>
        /// <param name="p">TrackPoint that will be drawn on the map.</param>
        private void CreateMapIcon(TrackPoint p)
        {
            var icon = new MapIcon
            {
                NormalizedAnchorPoint = new Windows.Foundation.Point(0.5, 0.5),
                Location = new Geopoint(p.Position)
            };
            MapExtensions.SetValue(icon, p);
            TracksMap.MapElements.Add(icon);
        }

        /// <summary>
        /// Error handling for device level error conditions
        /// </summary>
        /// <param name="sender">The control that the action is for.</param>
        /// <param name="args">Parameter that contains data for the AccesChanged event.</param>
        public async void OnAccessChanged(DeviceAccessInformation sender, DeviceAccessChangedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (DeviceAccessStatus.DeniedByUser == args.Status)
                {
                    Debug.WriteLine("Location has been disabled by the user. Enable access through the settings charm.");
                }
                else if (DeviceAccessStatus.DeniedBySystem == args.Status)
                {
                    Debug.WriteLine("Location has been disabled by the system. The administrator of the device must enable location access through the location control panel.");
                }
                else if (DeviceAccessStatus.Unspecified == args.Status)
                {
                    Debug.WriteLine("Location has been disabled by unspecified source. The administrator of the device may need to enable location access through the location control panel, then enable access through the settings charm.");
                }
                else if (DeviceAccessStatus.Allowed == args.Status)
                {
                }
                else
                {
                    Debug.WriteLine("Unknown device access information status");
                }
            });
        }

        /// <summary>
        /// Performs asynchronous Sensorcore SDK operation and handles any exceptions
        /// </summary>
        /// <param name="action">Action for which the SensorCore will be activated.</param>
        /// <returns><c>true</c> if call was successful, <c>false</c> otherwise</returns>
        private async Task<bool> CallSensorcoreApiAsync(Func<Task> action)
        {
            Exception failure = null;
            try
            {
                await action();
            }
            catch (Exception e)
            {
                failure = e;
            }
            if (failure != null)
            {
                MessageDialog dialog;
                switch (SenseHelper.GetSenseError(failure.HResult))
                {
                    case SenseError.LocationDisabled:
                        dialog = new MessageDialog("Location has been disabled. Do you want to open Location settings now? If you choose no, application will exit.", "Information");
                        dialog.Commands.Add(new UICommand("Yes", async cmd => await SenseHelper.LaunchLocationSettingsAsync()));
                        dialog.Commands.Add(new UICommand("No"));
                        await dialog.ShowAsync();
                        new System.Threading.ManualResetEvent(false).WaitOne(500);
                        return false;
                    case SenseError.SenseDisabled: dialog = new MessageDialog("Motion data has been disabled. Do you want to open Motion data settings now? If you choose no, application will exit.", "Information");
                        dialog.Commands.Add(new UICommand("Yes", async cmd => await SenseHelper.LaunchSenseSettingsAsync()));
                        dialog.Commands.Add(new UICommand("No"));
                        await dialog.ShowAsync();
                        new System.Threading.ManualResetEvent(false).WaitOne(500);
                        return false;
                    default:
                        dialog = new MessageDialog("Failure: " + SenseHelper.GetSenseError(failure.HResult) + " while initializing Motion data. Application will exit.", "");
                        await dialog.ShowAsync();
                        return false;
                }
            }
            return true;
        }
    }
}
