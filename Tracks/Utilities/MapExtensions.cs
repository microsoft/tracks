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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Maps;
using Lumia.Sense;

namespace Tracks.Utilities
{
    /// <summary>
    /// Extension class for the MapView
    /// </summary>
    public static class MapExtensions
    {
        #region Private members
        /// <summary>
        /// Dependency property that is registered with the dependency property system
        /// </summary>
        private static readonly DependencyProperty _ValueProperty = DependencyProperty.RegisterAttached(
            "TrackPoint", 
            typeof(TrackPoint), 
            typeof(MapExtensions), 
            new PropertyMetadata(default(uint)));
        #endregion

        /// <summary>
        /// Set value property for element
        /// </summary>
        /// <param name="element">The object for which to set the value.</param>
        /// <param name="value">Corresponding value for the dependency object.</param>
        private static void SetValueProperty(DependencyObject element, TrackPoint value)
        {
            element.SetValue(_ValueProperty, value);
        }

        /// <summary>
        /// Return value propert for element
        /// </summary>
        /// <param name="element">Element fo which to get the value.</param>
        /// <returns>Value property for the element.</returns>
        private static TrackPoint GetValueProperty(DependencyObject element)
        {
            return (TrackPoint)element.GetValue(_ValueProperty);
        }

        /// <summary>
        /// Set TrackPoint value for MapElement
        /// </summary>
        /// <param name="target">Target object for which to set the value.</param>
        /// <param name="value">Value for the target object.</param>
        public static void SetValue(MapElement target, TrackPoint value)
        {
            SetValueProperty(target, value);
        }

        /// <summary>
        /// Get TrackPoint value from MapElement
        /// </summary>
        /// <param name="target">Target object for which to get the value.</param>
        /// <returns>Value for the object sent as parameter.</returns>
        public static TrackPoint GetValue(MapElement target)
        {
            return GetValueProperty(target);
        }
    }
}
