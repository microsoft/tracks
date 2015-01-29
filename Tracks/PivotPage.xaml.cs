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
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Tracks.Common;
using Tracks.Utilities;
using Lumia.Sense;

/// <summary>
/// The Pivot Application template is documented at http://go.microsoft.com/fwlink/?LinkID=391641
/// </summary>
namespace Tracks
{
    /// <summary>
    /// This page will display additional information for a tapped point on the map
    /// </summary>
    public sealed partial class PivotPage : Page
    {
        #region Private members
        /// <summary>
        /// Navigation helper used to navigate through pages
        /// </summary>
        private readonly NavigationHelper _navigationHelper;
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public PivotPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
            this._navigationHelper = new NavigationHelper(this);
            this._navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this._navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this._navigationHelper; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation. Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>.
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session. The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            if (e.NavigationParameter != null)
            {
                var icon = e.NavigationParameter as MapIcon;
                if (icon != null)
                {
                    CreatePivotItem(MapExtensions.GetValue(icon));
                }
            }
        }

        /// <summary>
        /// Create a Pivot and PivotItem, and fill with tPoint info
        /// </summary>
        /// <param name="tPoint">TrackPoint for which additional data will be displayed.</param>
        private void CreatePivotItem(TrackPoint tPoint)
        {
            MainGrid.Children.Clear();
            var pivot = new Pivot { Title = "SENSORCORE SAMPLE", Margin = new Thickness(0, 12, 0, 0), Foreground = new SolidColorBrush(Colors.Black) };
            var item = new PivotItem { Header = "Routepoint", Foreground = new SolidColorBrush(Colors.Black) };
            var stack = new StackPanel();
            stack.Children.Add(CreateTextBlock("Latitude:", tPoint.Position.Latitude.ToString()));
            stack.Children.Add(CreateTextBlock("Longitude:", tPoint.Position.Longitude.ToString()));
            stack.Children.Add(CreateTextBlock("Duration:", tPoint.LengthOfStay.TotalMinutes.ToString() + " min"));
            stack.Children.Add(CreateTextBlock("Radius:", tPoint.Radius.ToString() + " m"));
            stack.Children.Add(CreateTextBlock("Timestamp:", tPoint.Timestamp.DateTime.ToString() + " m"));
            item.Content = stack;
            pivot.Items.Add(item);
            MainGrid.Children.Add(pivot);
        }

        /// <summary>
        /// Helper method to create a TextBlock to be used in the PivotItem
        /// </summary>
        /// <param name="text">Label of the text block.</param>
        /// <param name="value">Value to be shown in the text block.</param>
        /// <returns></returns>
        private static TextBlock CreateTextBlock(string text, string value)
        {
            return new TextBlock
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 24,
                TextWrapping = Windows.UI.Xaml.TextWrapping.Wrap,
                Text = text + " " + value
            };
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache. Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/>.</param>
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
    }
}
