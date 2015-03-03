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

/// <summary>
/// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556
/// </summary>
namespace Tracks
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MapPage : Page
    {
        #region Private members
        /// <summary>
        /// Navigation helper used to navigate through pages
        /// </summary>
        private NavigationHelper _navigationHelper; 

        /// <summary>
        /// View model for this page
        /// </summary>
        private ObservableDictionary _defaultViewModel = new ObservableDictionary(); 

        /// <summary>
        /// Represents the selected item from the day selection list
        /// </summary>
        private static DaySelectionItem _selected; 

        /// <summary>
        /// Collection of options of type DaySelectionItem
        /// </summary>
        private readonly List<DaySelectionItem> _optionList = new List<DaySelectionItem>(); 

        /// <summary>
        /// Collection of stay durations
        /// </summary>
        private readonly List<string> _filterList = new List<string>(); 

        /// <summary>
        /// Used for simplified access to app resources such as app UI strings.
        /// </summary>
        ResourceLoader _loader = new ResourceLoader(); 

        /// <summary>
        /// Defines how small duration is shown
        /// </summary>
        private string _filterTime = "all";

        /// <summary>
        /// Curent location index
        /// </summary>
        int locationIndex = 0;

        /// <summary>
        /// Map Page instance
        /// </summary>
        public static MapPage _instanceMap;
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public MapPage()
        {
            this.InitializeComponent();
            if (_instanceMap == null)
                _instanceMap = this;
            this._navigationHelper = new NavigationHelper(this);
            this._navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this._navigationHelper.SaveState += this.NavigationHelper_SaveState;
            FillDateList();
            FillFilterList();
            this.listSource.Source = _optionList;
            this.filterSource.Source = _filterList;
            TracksMap.MapServiceToken = "xxx";
            if(_selected == null)
                _selected = _optionList[0];
            InitCore();
            // Activate and deactivate the SensorCore when the visibility of the app changes
            Window.Current.VisibilityChanged += async (oo, ee) =>
            {
                if (_tracker != null)
                {
                    if (!ee.Visible)
                    {
                        await CallSensorcoreApiAsync(async () => { await _tracker.DeactivateAsync(); });
                        await CallSensorcoreApiAsync(async () => { await _placeMonitor.DeactivateAsync(); });
                    }
                    else
                    {
                        await CallSensorcoreApiAsync(async () => { await _tracker.ActivateAsync(); });
                        await CallSensorcoreApiAsync(async () => { await _placeMonitor.ActivateAsync(); });
                    }
                }
            };
        }

        /// <summary>
        /// Fill the list with current day of week, and in descending order rest of the weekdays
        /// </summary>
        private void FillDateList()
        {
            // Current day
            int today = (int)DateTime.Now.DayOfWeek; 
            int count = 0;
            for (int i = today; i >= 0; i--)
            {
                var item = new DaySelectionItem { Day = DateTime.Now.Date - TimeSpan.FromDays(count) };
                var nameOfDay = System.Globalization.DateTimeFormatInfo.CurrentInfo.DayNames[i];
                // Add an indicator to current day
                if (count == 0)
                {
                    nameOfDay += " " + _loader.GetString("Today");
                }
                GeographicRegion userRegion = new GeographicRegion();
                var userDateFormat = new Windows.Globalization.DateTimeFormatting.DateTimeFormatter("shortdate", new[] { userRegion.Code });
                var dateDefault = userDateFormat.Format(item.Day);
                item.Name = nameOfDay + " " + dateDefault;
                _optionList.Add(item);
                count++;
                // First day of the week, but not all weekdays still listed,
                // continue from the last weekday
                if (i == 0 && count <= 6)
                {
                    i = 7;
                }
                // All weekdays listed, exit the loop
                else if (count == 7) 
                {
                    i = 0;
                }
            }
            // Add the option to show everything
            _optionList.Add(new DaySelectionItem { Name = _loader.GetString("All") });
        }

        /// <summary>
        /// Fill the list with minutes, and in ascending order
        /// </summary>
        private void FillFilterList()
        {
            var minutes = _loader.GetString("minutes");
            _filterList.Add("0 - 10 " + minutes);
            _filterList.Add("15 " + minutes);
            _filterList.Add("30 " + minutes);
            _filterList.Add("60 " + minutes);
            _filterList.Add(_loader.GetString("all"));
            _filterTime = "all";
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this._navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this._defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration
        /// <summary>
        /// Called when a page becomes the active page in a frame.
        /// </summary>
        /// <param name="e">Provides data for non-cancelable navigation events</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this._navigationHelper.OnNavigatedTo(e);
        }

        /// <summary>
        /// Called when a page is no longer the active page in a frame.
        /// </summary>
        /// <param name="e">Provides data for non-cancelable navigation events</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this._navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnFiltered(ListPickerFlyout sender, ItemsPickedEventArgs e)
        {
            switch (FilterFlyout.SelectedIndex)
            {
                case 0:
                    _filterTime = "0 - 10 minutes";
                    break;
                case 1:
                    _filterTime = "15 minutes";
                    break;
                case 2:
                    _filterTime = "30 minutes";
                    break;
                case 3:
                    _filterTime = "60 minutes";
                    break;
                default:
                    _filterTime = "0 minutes";
                    break;
            }
            await DrawRoute();
        }

        /// <summary>
        /// Navigates to the About page of the application.
        /// </summary>
        /// <param name="sender">The control that the action is for.</param>
        /// <param name="e">Parameter that contains the event data.</param>
        private void OnAboutClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AboutPage));
        }

        /// <summary>
        /// Draws the route for all the tracks in the selected day.
        /// </summary>
        /// <param name="sender">The control that the action is for.</param>
        /// <param name="args">Parameter that contains the event data.</param>
        private async void OnPicked(ListPickerFlyout sender, ItemsPickedEventArgs args)
        {
            if (args.AddedItems.Count == 1)
            {
                _selected = (DaySelectionItem)args.AddedItems[0];
                await DrawRoute();
                FilterTime.Text = _selected.Name;
            }
            else
                _selected = _optionList[0];
        }

        /// <summary>
        /// Enter fullscreen mode.
        /// </summary>
        /// <param name="sender">The control that the action is for.</param>
        /// <param name="e">Parameter that contains the event data.</param>
        private void FullScreeButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            _fullScreen = !_fullScreen;
            if (_fullScreen)
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
        /// Go to Previous point
        /// </summary>
        /// <param name="sender">The control that the action is for.</param>
        /// <param name="e">Parameter that contains the event data.</param>
        private void GoToPreviousLocation(object sender, RoutedEventArgs e)
        {
            locationIndex--;
            if (locationIndex >= 0)
                TracksMap.Center = new Geopoint(points[locationIndex].Position);
            else
                locationIndex = 0;
        }
        
        /// <summary>
        /// Go to Previous point
        /// </summary>
        /// <param name="sender">The control that the action is for.</param>
        /// <param name="e">Parameter that contains the event data.</param>
        private void GoToNextLocation(object sender, RoutedEventArgs e)
        {
            locationIndex++;
            if (locationIndex < points.Count)
                TracksMap.Center = new Geopoint(points[locationIndex].Position);
            else
                locationIndex = points.Count - 1;
        }

        /// <summary>
        /// Draws the route for all the tracks in the selected day.
        /// </summary>
        /// <param name="sender">The control that the action is for.</param>
        /// <param name="args">Parameter that contains the event data.</param>
        private async void flyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var flyoutItem = e.OriginalSource as MenuFlyoutItem;
            try
            {
                for (int i = 0; i < listSource.View.Count; i++)
                    if (flyoutItem.Text.Contains(listSource.View[i].ToString()))
                        _selected = (DaySelectionItem)listSource.View[i];
                 await DrawRoute();
                FilterTime.Text = _selected.Name;
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Filters time and draws the tracks from the selected time interval.
        /// </summary>
        /// <param name="sender">The control that the action is for.</param>
        /// <param name="e">Parameter that contains the event data.</param>
        private async void FilterItem_Click(object sender, RoutedEventArgs e)
        {
            var flyoutItem = e.OriginalSource as MenuFlyoutItem;
            try
            {
                _filterTime = flyoutItem.Text;
               await DrawRoute();
            }
            catch (Exception)
            {
            }
        }
    }
}
