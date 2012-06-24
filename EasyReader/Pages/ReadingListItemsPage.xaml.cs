using System;
using System.Collections.Generic;
using System.Diagnostics;

using Windows.Networking.Connectivity;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

using EasyReader.Data;

namespace EasyReader.Pages
{
    /// <summary>
    /// A page that displays a collection of item previews.  In the Split Application this page
    /// is used to display and select one of the available groups.
    /// </summary>
    public sealed partial class ReadingListItemsPage
    {
        private ReadingListDataSource _readingListDataSource;

        public ReadingListItemsPage()
        {
            InitializeComponent();
        }

        private void Items_VectorChanged(Windows.Foundation.Collections.IObservableVector<object> sender, Windows.Foundation.Collections.IVectorChangedEventArgs @event)
        {
            //Debug.WriteLine("Collection changed.");
        }

        public void UpdateSubTitle()
        {
            string text = "(Offline)";

            if (App.HasConnectivity)
            {
                text = _readingListDataSource.IsUpdating ? "(Online, Updating)" : "(Online)";
            }

            pageSubTitle.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, delegate
            {
                pageSubTitle.Text = text;
            });
        }

        private void NetworkInformation_NetworkStatusChanged(object sender)
        {
            UpdateSubTitle();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            Debug.WriteLine("Calling base.OnNavigatedTo()");

            base.OnNavigatedTo(e);

            Debug.WriteLine("ReadingListItemsPage.OnNavigatedTo()");

            // XXX: Probably a better way to do this; LoadState?
            if (_readingListDataSource == null)
            {
                NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;

                _readingListDataSource = (ReadingListDataSource)App.Current.Resources["readingListDataSource"];

                if (_readingListDataSource == null)
                {
                    Debug.WriteLine("_readingListDataSource was null!");
                    
                    return;
                }

                DefaultViewModel["Items"] = _readingListDataSource.Items;

                _readingListDataSource.Items.VectorChanged += Items_VectorChanged;

                _readingListDataSource.UpdatingStatusChanged += UpdatingStatusChanged;

                if (_readingListDataSource.Items.Count == 0)
                {
                    await _readingListDataSource.UpdateReadingList();
                }
            }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            Debug.WriteLine("ReadingListItemsPage.LoadState()");
        }

        private void UpdatingStatusChanged(object sender, EventArgs e)
        {
            UpdateSubTitle();
        }

        private void navigateToItem(ReadingListDataItem item)
        {
            Debug.WriteLine("navigateToItem()");

            Frame.Navigate(typeof (DetailPage), item);
        }

        private void itemListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var selection = e.ClickedItem as ReadingListDataItem;

            navigateToItem(selection);
        }

        private void itemGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var selection = e.ClickedItem as ReadingListDataItem;

            navigateToItem(selection);
        }
    }
}