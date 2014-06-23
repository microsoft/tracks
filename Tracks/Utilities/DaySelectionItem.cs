using System;

namespace Tracks.Utilities
{
    public class DaySelectionItem
    {
        public string Name { get; set; }
        public DateTime Day { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
