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
using RouteTracker = Lumia.Sense.TrackPointMonitor;
using Windows.Foundation;
using Tracks.Utilities;
using System.Linq;
using Windows.Storage.Streams;

namespace Tracks
{
    /// <summary>
    /// Page displaying user's tracks on a map (sensor related functions)
    /// </summary>
    public sealed partial class MapPage
    {
        #region Private members
        /// <summary>
        /// Track point monitor instance
        /// </summary>
        private RouteTracker _tracker;

        /// <summary>
        /// Are we currently in full screen mode?
        /// </summary>
        private bool _fullScreen;

        /// <summary>
        /// Collection of track points
        /// </summary>
        IList<TrackPoint> _points = null;

        /// <summary>
        /// API set supported by the device
        /// </summary>
        uint _apiSet = 0;
        #endregion

        /// <summary>
        /// Makes sure necessary settings are enabled in order to use SensorCore
        /// </summary>
        /// <returns>Asynchronous task</returns>
        private async Task ValidateSettingsAsync()
        {
            try
            {
                if( !( await TrackPointMonitor.IsSupportedAsync() ) )
                {
                    MessageDialog dlg = new MessageDialog( "Unfortunately this device does not support viewing tracks" );
                    await dlg.ShowAsync();
                    Application.Current.Exit();
                }
                else
                {
                    _apiSet = await SenseHelper.GetSupportedApiSetAsync();
                    MotionDataSettings settings = await SenseHelper.GetSettingsAsync();
                    if( !settings.LocationEnabled )
                    {
                        MessageDialog dlg = new MessageDialog( "In order to collect and view visited tracks you need to enable location in system settings. Do you want to open settings now? if not, application will exit", "Information" );
                        dlg.Commands.Add( new UICommand( "Yes", new UICommandInvokedHandler( async ( cmd ) => await SenseHelper.LaunchLocationSettingsAsync() ) ) );
                        dlg.Commands.Add( new UICommand( "No", new UICommandInvokedHandler( ( cmd ) => { Application.Current.Exit(); } ) ) );
                        await dlg.ShowAsync();
                    }
                    if( !settings.PlacesVisited )
                    {
                        MessageDialog dlg = null;
                        if( settings.Version < 2 )
                        {
                            // Device has old Motion data settings
                            dlg = new MessageDialog( "In order to collect and view visited tracks you need to enable 'Motion data collection' in Motion data settings. Do you want to open settings now? If not, application will exit.", "Information" );
                        }
                        else
                        {
                            dlg = new MessageDialog( "In order to collect and view visited tracks you need to enable 'Places visited' in Motion data settings. Do you want to open settings now? If not, application will exit.", "Information" );
                        }
                        dlg.Commands.Add( new UICommand( "Yes", new UICommandInvokedHandler( async ( cmd ) => await SenseHelper.LaunchSenseSettingsAsync() ) ) );
                        dlg.Commands.Add( new UICommand( "No", new UICommandInvokedHandler( ( cmd ) => { Application.Current.Exit(); } ) ) );
                        await dlg.ShowAsync();
                    }
                }
            }
            catch( Exception )
            {
            }
        }

        /// <summary>
        /// Draw the route points and the lines connecting them
        /// </summary>
        private async Task DrawRoute()
        {
            TracksMap.MapElements.Clear();
            TracksMap.Children.Clear();
            _points = null;
            
            if( await CallSensorcoreApiAsync( async () =>
            {
                // Get selected day routes, else all routes from last 10 days
                DaySelectionItem dayItem = ( DateFlyout.SelectedItem as DaySelectionItem );
                if( dayItem.Day.HasValue )
                {
                    _points = await _tracker.GetTrackPointsAsync( dayItem.Day.Value, TimeSpan.FromHours( 24 ) );
                }
                else
                {
                    _points = await _tracker.GetTrackPointsAsync( DateTime.Now - TimeSpan.FromDays( 10 ), TimeSpan.FromDays( 10 ) );
                }
            } ) )
            {
                if( _points != null && _points.Count > 0 )
                {
                    TimeSpan filterTime = TimeSpan.FromMinutes( ( TimeFilterFlyout.SelectedItem as TimeFilterItem ).Time );
                    List<BasicGeoposition> path = new List<BasicGeoposition>();
                    foreach( TrackPoint point in _points )
                    {
                        if( filterTime.TotalMinutes == 0 ||
                            point.LengthOfStay.CompareTo( filterTime ) >= 0 )
                        {
                            path.Add( point.Position );
                            if( _apiSet >= 2 )
                            {
                                // Place id is available starting from API set 2.
                                // Draw map icon if there is a known place associated with this point.
                                if( point.Id != 0 )
                                {
                                    // Use larger icon if stay length was greater than 15 minutes
                                    CreateMapIcon(
                                        point,
                                        point.LengthOfStay.CompareTo( TimeSpan.FromMinutes( 15 ) ) > 0 );
                                }
                            }
                            else if( point.LengthOfStay.CompareTo( TimeSpan.FromMinutes( 5 ) ) > 0 )
                            {
                                // If place id is not supported, draw icon if stay length was at least five minutes.
                                // Use larger icon if stay length was greater than 15 minutes.
                                CreateMapIcon(
                                    point,
                                    point.LengthOfStay.CompareTo( TimeSpan.FromMinutes( 15 ) ) > 0 );
                            }
                        }
                    }
                    MapPolyline mapShape = new MapPolyline();
                    mapShape.Path = new Geopath( path );
                    mapShape.StrokeThickness = 3;
                    mapShape.StrokeColor = Color.FromArgb( 255, 100, 100, 255 );
                    mapShape.StrokeDashed = false;
                    TracksMap.MapElements.Add( mapShape );
                    TracksMap.ZoomLevel = 13;

                    // Center map to latest location
                    if( path.Count > 0 )
                    {
                        TracksMap.Center = new Geopoint( path.First() );
                    }
                }
            }
        }

        /// <summary>
        /// Creates map icon for track point
        /// </summary>
        /// <param name="point">Track point to create map icon for</param>
        /// <param name="largeIcon">If <c>true</c> will use large icon, if <c>false</c> will use small icon</param>
        private void CreateMapIcon( TrackPoint point, bool largeIcon )
        {
            var icon = new MapIcon
            {
                NormalizedAnchorPoint = new Windows.Foundation.Point( 0.5, 0.5 ),
                Location = new Geopoint( point.Position )
            };
            if( largeIcon )
            {
                icon.Image = RandomAccessStreamReference.CreateFromUri( new Uri( "ms-appx:///Assets/dot.png" ) );
            }
            else
            {
                icon.Image = RandomAccessStreamReference.CreateFromUri( new Uri( "ms-appx:///Assets/dotSmall.png" ) );
            }
            MapExtensions.SetValue( icon, point );
            TracksMap.MapElements.Add( icon );
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
                        dialog = new MessageDialog( "In order to collect and view visited tracks you need to enable location in system settings. Do you want to open settings now? If not, application will exit.", "Information" );
                        dialog.Commands.Add( new UICommand( "Yes", new UICommandInvokedHandler( async ( cmd ) => await SenseHelper.LaunchLocationSettingsAsync() ) ) );
                        dialog.Commands.Add( new UICommand( "No", new UICommandInvokedHandler( ( cmd ) => { Application.Current.Exit(); } ) ) );
                        await dialog.ShowAsync();
                        new System.Threading.ManualResetEvent( false ).WaitOne( 500 );
                        return false;
                    case SenseError.SenseDisabled:
                    {
                        MotionDataSettings settings = await SenseHelper.GetSettingsAsync();
                        if( settings.Version < 2 )
                        {
                            // Device has old Motion data settings
                            dialog = new MessageDialog( "In order to collect and view visited tracks you need to enable 'Motion data collection' in Motion data settings. Do you want to open settings now? If not, application will exit.", "Information" );
                        }
                        else
                        {
                            dialog = new MessageDialog( "In order to collect and view visited tracks you need to enable 'Places visited' in Motion data settings. Do you want to open settings now? if not, application will exit.", "Information" );
                        }
                        dialog.Commands.Add( new UICommand( "Yes", new UICommandInvokedHandler( async ( cmd ) => await SenseHelper.LaunchSenseSettingsAsync() ) ) );
                        dialog.Commands.Add( new UICommand( "No", new UICommandInvokedHandler( ( cmd ) => { Application.Current.Exit(); } ) ) );
                        await dialog.ShowAsync();
                        new System.Threading.ManualResetEvent( false ).WaitOne( 500 );
                        return false;
                    }
                    default:
                        return false;
                }
            }
            return true;
        }
    }
}
