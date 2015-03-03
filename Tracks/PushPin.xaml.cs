/*	
The MIT License (MIT)
Copyright (c) 2015 Microsoft

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. 
 */
using Windows.Devices.Geolocation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Lumia.Sense;
using System.Collections.Generic;
using System.Globalization;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Tracks
{
    public sealed partial class PushPin : UserControl
    {
        /// <summary>
        /// geopoint to address of the place.
        /// </summary>
        private Geopoint _geopoint;
        /// <summary>
        /// holds kind of the place.
        /// </summary>
        private string _placeKind;
        /// <summary>
        /// TrackPoint Instance
        /// </summary>
        private TrackPoint _point;
        /// <summary>
        /// holds activities 
        /// </summary>
        private string _activities;

        /// <summary>
        /// constructor
        /// </summary>
        public PushPin(TrackPoint point, Geopoint geopoint, string placeKind, string activities)
        {
            InitializeComponent();
            _point = point;
            _geopoint = geopoint;
            _placeKind = placeKind;
            _activities = activities;
            Loaded += PushPin_Loaded;
        }

        // <summary>
        // page load event. sets the pushpin text.
        // </summary>
        // <param name="sender">event details</param>
        // <param name="e">event sender</param>
        void PushPin_Loaded(object sender, RoutedEventArgs e)
        {
            Lbltext.Text = _placeKind + "Place";
        }

        /// <summary>
        /// Tapped event on the pushpin to display details.
        /// </summary>
        /// <param name="sender">event details</param>
        /// <param name="e">event sender</param>
        private void PushPinTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var frame = Window.Current.Content as Frame;
            string time = _point.Timestamp.ToString("MMM dd yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            var resultStr = _point.Position.Latitude.ToString() + "\n" + _point.Position.Longitude.ToString() + "\n" +_point.LengthOfStay.TotalMinutes.ToString() + "\n" +
                _point.Radius.ToString() + "\n" + time.ToString() + "\n" + _activities.ToString(); 
            frame.Navigate(typeof(PivotPage), resultStr);
        }
    }
}