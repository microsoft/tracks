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
using System.Collections.Generic;
using Windows.ApplicationModel.Resources;
using Windows.Globalization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Tracks.Common;
using Tracks.Utilities;
using Windows.Devices.Geolocation;
using RouteTracker = Lumia.Sense.TrackPointMonitor;

namespace Tracks
{
    /// <summary>
    /// Page displaying user's tracks on a map
    /// </summary>
    public sealed partial class MapPage : Page
    {
        #region Private members
        /// <summary>
        /// Navigation helper used to navigate through pages
        /// </summary>
        private NavigationHelper _navigationHelper;

        /// <summary>
        /// Day selection list items
        /// </summary>
        private readonly List<DaySelectionItem> _daySelectionList = new List<DaySelectionItem>();

        /// <summary>
        /// Time filter list items
        /// </summary>
        private readonly List<TimeFilterItem> _timeFilterList = new List<TimeFilterItem>();

        /// <summary>
        /// Used for simplified access to app resources such as app UI strings.
        /// </summary>
        ResourceLoader _resourceLoader = new ResourceLoader();

        /// <summary>
        /// Curent location index
        /// </summary>
        int _locationIndex = 0;
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public MapPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;

            this._navigationHelper = new NavigationHelper( this );
            FillDateList();
            FillTimeFilterList();
            DaySelectionSource.Source = _daySelectionList;
            TimeFilterSource.Source = _timeFilterList;
            FilterTime.Text = _daySelectionList[ 0 ].Name;
            TracksMap.MapServiceToken = "xxx";
            // Activate and deactivate the SensorCore when the visibility of the app changes
            Window.Current.VisibilityChanged += async ( oo, ee ) =>
            {
                if( !ee.Visible )
                {
                    if( _tracker != null )
                    {
                        await CallSensorcoreApiAsync( async () => { await _tracker.DeactivateAsync(); } );
                    }
                }
                else
                {
                    await ValidateSettingsAsync();
                    if( _tracker == null )
                    {
                        await CallSensorcoreApiAsync( async () => { _tracker = await RouteTracker.GetDefaultAsync(); } );
                    }
                    else
                    {
                        await CallSensorcoreApiAsync( async () => { await _tracker.ActivateAsync(); } );
                    }
                    await DrawRoute();
                }
            };
        }

        /// <summary>
        /// Fill the list with current day of week, and in descending order rest of the weekdays
        /// </summary>
        private void FillDateList()
        {
            GeographicRegion userRegion = new GeographicRegion();
            var userDateFormat = new Windows.Globalization.DateTimeFormatting.DateTimeFormatter( "shortdate", new[] { userRegion.Code } );
            for( int i = 0; i < 7; i++ )
            {
                var nameOfDay = System.Globalization.DateTimeFormatInfo.CurrentInfo.DayNames[ ( (int)DateTime.Now.DayOfWeek + 7 - i ) % 7 ];
                if( i == 0 )
                {
                    nameOfDay += " " + _resourceLoader.GetString( "Today" );
                }
                DateTime itemDate = DateTime.Now.Date - TimeSpan.FromDays( i );
                _daySelectionList.Add( 
                    new DaySelectionItem( 
                        nameOfDay + " " + userDateFormat.Format( itemDate ), 
                        itemDate ) );
            }
            // Add the option to show everything
            _daySelectionList.Add( new DaySelectionItem( _resourceLoader.GetString( "All" ), null ) );
        }

        /// <summary>
        /// Populates the time filter list
        /// </summary>
        private void FillTimeFilterList()
        {
            var minutes = _resourceLoader.GetString( "Minutes" );
            _timeFilterList.Add( new TimeFilterItem( _resourceLoader.GetString( "All" ), 0 ) );
            _timeFilterList.Add( new TimeFilterItem( ">= 0 - 10 " + minutes, 10 ) );
            _timeFilterList.Add( new TimeFilterItem( ">= 15 " + minutes, 15 ) );
            _timeFilterList.Add( new TimeFilterItem( ">= 30 " + minutes, 30 ) );
            _timeFilterList.Add( new TimeFilterItem( ">= 60 " + minutes, 60 ) );
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this._navigationHelper; }
        }

        #region NavigationHelper registration
        /// <summary>
        /// Called when a page becomes the active page in a frame.
        /// </summary>
        /// <param name="e">Provides data for non-cancelable navigation events</param>
        protected async override void OnNavigatedTo( NavigationEventArgs e )
        {
            this._navigationHelper.OnNavigatedTo( e );

            if( e.NavigationMode == NavigationMode.Back )
            {
                await ValidateSettingsAsync();
                if( _tracker == null )
                {
                    await CallSensorcoreApiAsync( async () => { _tracker = await RouteTracker.GetDefaultAsync(); } );
                }
                await DrawRoute();
            }
        }

        /// <summary>
        /// Called when a page is no longer the active page in a frame.
        /// </summary>
        /// <param name="e">Provides data for non-cancelable navigation events</param>
        protected override void OnNavigatedFrom( NavigationEventArgs e )
        {
            this._navigationHelper.OnNavigatedFrom( e );
        }
        #endregion

        /// <summary>
        /// Time filter flyout change event handler
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private async void TimeFilterFlyout_ItemsPicked( ListPickerFlyout sender, ItemsPickedEventArgs args )
        {
            await DrawRoute();
        }

        /// <summary>
        /// About button click event handler
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void AboutButton_Click( object sender, RoutedEventArgs e )
        {
            this.Frame.Navigate( typeof( AboutPage ) );
        }

        /// <summary>
        /// Draws the route for all the tracks in the selected day.
        /// </summary>
        /// <param name="sender">The control that the action is for.</param>
        /// <param name="args">Parameter that contains the event data.</param>
        private async void DateFlyout_ItemsPicked( ListPickerFlyout sender, ItemsPickedEventArgs args )
        {
            if( args.AddedItems.Count == 1 )
            {
                FilterTime.Text = ( sender.SelectedItem as DaySelectionItem ).Name;
                await DrawRoute();
            }
        }

        /// <summary>
        /// Enter fullscreen mode.
        /// </summary>
        /// <param name="sender">The control that the action is for.</param>
        /// <param name="e">Parameter that contains the event data.</param>
        private void FullScreeButton_OnTapped( object sender, TappedRoutedEventArgs e )
        {
            _fullScreen = !_fullScreen;
            if( _fullScreen )
            {
                CmdBar.Visibility = Visibility.Collapsed;
                TopPanel.Visibility = Visibility.Collapsed;
                FullScreeButton.Symbol = Symbol.BackToWindow;
            }
            else
            {
                CmdBar.Visibility = Visibility.Visible;
                TopPanel.Visibility = Visibility.Visible;
                FullScreeButton.Symbol = Symbol.FullScreen;
            }
        }

        /// <summary>
        /// Previous location button click event handler
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void PreviousButton_Click( object sender, RoutedEventArgs e )
        {
            _locationIndex--;
            if( _locationIndex >= 0 )
            {
                TracksMap.Center = new Geopoint( _points[ _locationIndex ].Position );
            }
            else
            {
                _locationIndex = 0;
            }
        }

        /// <summary>
        /// Next location button click event handler
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void NextButton_Click( object sender, RoutedEventArgs e )
        {
            _locationIndex++;
            if( _locationIndex < _points.Count )
            {
                TracksMap.Center = new Geopoint( _points[ _locationIndex ].Position );
            }
            else
            {
                _locationIndex = _points.Count - 1;
            }
        }
    }
}
