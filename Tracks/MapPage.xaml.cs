using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Windows.ApplicationModel.Resources;
using Windows.Globalization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Navigation;
using Tracks.Common;
using Tracks.Utilities;
using Tracker = Lumia.Sense.TrackPointMonitor;
//using Tracker = Lumia.Sense.Testing.TrackPointMonitorSimulator;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Tracks
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MapPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private DaySelectionItem selected;
        private readonly List<DaySelectionItem> optionList = new List<DaySelectionItem>();
        private readonly List<string> filterList = new List<string>();
        ResourceLoader loader = new ResourceLoader();
        private int filterTime = 0; // Defines how small duration is shown
        public MapPage()
        {
            this.InitializeComponent();
            
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            FillDateList();
            FillFilterList();
            this.listSource.Source = optionList;
            this.filterSource.Source = filterList;

            TracksMap.MapServiceToken = "xxx";

            this.Loaded += async (sender, args) =>
            {
                await InitCore();
                
                // This is not implemented by the simulator, uncomment for the RouteTracker
                if (await Tracker.IsSupportedAsync())
                {
                    DrawRoute();
                }

                DateFlyout.SelectedIndex = optionList.Count - 1;
            };
        }

        private void CalculateMapSize()
        {
            TracksMap.Width = Window.Current.Bounds.Width;
            TracksMap.Height = LayoutRoot.RowDefinitions[1].ActualHeight;
        }
        /// <summary>
        /// Fill the list with current day of week, and in descending order rest of the weekdays
        /// </summary>
        private void FillDateList()
        {
            int today = (int)DateTime.Now.DayOfWeek; // Current day

            int count = 0;
            for (int i = today; i >= 0; i--)
            {
                var item = new DaySelectionItem {Day = DateTime.Now.Date - TimeSpan.FromDays(count)};
                var nameOfDay = System.Globalization.DateTimeFormatInfo.CurrentInfo.DayNames[i];
                
                // Add an indicator to current day
                if (count == 0)
                {
                    nameOfDay += loader.GetString("Today");
                }

                GeographicRegion userRegion = new GeographicRegion();

                var userDateFormat = new Windows.Globalization.DateTimeFormatting.DateTimeFormatter("shortdate",new [] {userRegion.Code});
                var dateDefault = userDateFormat.Format(item.Day);

                item.Name = nameOfDay + " " + dateDefault;
                optionList.Add(item);
                count++;

                // First day of the week, but not all weekdays still listed,
                // continue from the last weekday
                if (i == 0 && count <= 6)
                {
                    i = 7;
                }
                else if (count == 7) // All weekdays listed, exit the loop
                {
                    i = 0;
                }
            }
            // Add the option to show everything
            optionList.Add(new DaySelectionItem { Name = loader.GetString("All") });
        }

        private void FillFilterList()
        {
            var minutes = loader.GetString("Minutes");
            filterList.Add("10 " + minutes);
            filterList.Add("15 " + minutes);
            filterList.Add("30 " + minutes);
            filterList.Add("60 " + minutes);
            filterList.Add(loader.GetString("All"));
        }
        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
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
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        private void OnTapped(MapControl sender, MapInputEventArgs args)
        {
            try
            {
                var elementList = TracksMap.FindMapElementsAtOffset(args.Position);
                foreach (var element in elementList)
                {
                    var icon = element as MapIcon;

                    if (icon != null)
                    {
                        this.Frame.Navigate(typeof(PivotPage), icon);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        #endregion

        private void OnFiltered(ListPickerFlyout sender, ItemsPickedEventArgs e)
        {
            switch (FilterFlyout.SelectedIndex)
            {
                case 0:
                    filterTime = 10;
                    break;
                case 1:
                    filterTime = 15;
                    break;
                case 2:
                    filterTime = 30;
                    break;
                case 3:
                    filterTime = 60;
                    break;
                default:
                    filterTime = 0;
                    break;
            }

            DrawRoute();
        }

        private void OnAboutClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AboutPage));
        }

        private void OnPicked(ListPickerFlyout sender, ItemsPickedEventArgs args)
        {
            if (args.AddedItems.Count == 1)
            {
                selected = (DaySelectionItem)args.AddedItems[0];
            }
            DrawRoute();
        }
    }
}
