using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Maps;
using Lumia.Sense;

namespace Tracks.Utilities
{
    public static class MapExtensions
    {
        private static readonly DependencyProperty ValueProperty = DependencyProperty.RegisterAttached(
          "TrackPoint",
          typeof(TrackPoint),
          typeof(MapExtensions),
          new PropertyMetadata(default(uint)));
        private static void SetValueProperty(DependencyObject element, TrackPoint value)
        {
            element.SetValue(ValueProperty, value);
        }
        private static TrackPoint GetValueProperty(DependencyObject element)
        {
            return (TrackPoint)element.GetValue(ValueProperty);
        }

        public static void SetValue(MapElement target, TrackPoint value)
        {
            SetValueProperty(target, value);
        }

        public static TrackPoint GetValue(MapElement target)
        {
            return GetValueProperty(target);
        }
    }
}
