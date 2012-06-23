using System;
using System.Diagnostics;

using Windows.Graphics.Display;
using Windows.Networking.Connectivity;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

using EasyReader.Data;
using EasyReader.Hacks;

namespace EasyReader.Pages
{
    public sealed partial class CollectionSummaryPage
    {
        public CollectionSummaryPage()
        {
            InitializeComponent();
        }

        public void ShowSettingsUserControl()
        {
            SettingsUserControl.Show();
        }
        
        private void UserControl_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (SettingsUserControl.IsInView)
            {
                SettingsUserControl.Hide();
            }
        }

        void BackButton_Click(object sender, RoutedEventArgs e)
        {
        }

        void ItemView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Construct the appropriate destination page and set its context appropriately
            var selection = ((Selector) sender).SelectedItem;

            var selectedItem = selection as ReadingListDataItem;

            App.ShowDetail(selectedItem);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;

            Items = App.ReadingList;

            Items.VectorChanged += Items_VectorChanged;

            SetupViewState();

            UpdateSubTitle();
        }

        private void Items_VectorChanged(Windows.Foundation.Collections.IObservableVector<object> sender, Windows.Foundation.Collections.IVectorChangedEventArgs @event)
        {
            //Debug.WriteLine("Collection changed.");

            //Dispatcher.RunAsync(CoreDispatcherPriority.Normal, delegate
            //{
            //    ItemGridView.UpdateLayout();   
            //    ItemListView.UpdateLayout();
            //});
        }

        public void UpdateSubTitle()
        {
            string text = "(Offline)";

            if (App.HasConnectivity)
            {
                text = App.IsUpdating ? "(Online, Updating)" : "(Online)";
            }

            PageSubTitle.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, delegate
            {
                PageSubTitle.Text = text;
            });
        }

        private void NetworkInformation_NetworkStatusChanged(object sender)
        {
            UpdateSubTitle();
        }

        #region Properties
        private ObservableVector<object> _items;
        public ObservableVector<object> Items
        {
            get
            {
                return _items;
            }

            set
            {
                _items = value;

                CollectionViewSource.Source = value;
            }
        }

        private Object _item;
        public Object Item
        {
            get
            {
                return _item;
            }

            set
            {
                _item = value;
                
                LayoutRoot.DataContext = value;
            }
        }
        #endregion

        #region View state management
        // View state management for switching among Full, Fill, Snapped, and Portrait states
        private DisplayPropertiesEventHandler _displayHandler;
        private WindowSizeChangedEventHandler _layoutHandler;

        private void SetupViewState()
        {
            if (_displayHandler == null)
            {
                _displayHandler = Page_OrientationChanged;
                _layoutHandler = Page_LayoutChanged;
            }

            DisplayProperties.OrientationChanged += _displayHandler;

            Window.Current.SizeChanged += _layoutHandler;

            SetCurrentViewState(this);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            DisplayProperties.OrientationChanged -= _displayHandler;

            Window.Current.SizeChanged -= _layoutHandler;
        }

        private void Page_LayoutChanged(object sender, WindowSizeChangedEventArgs e)
        {
            SetCurrentViewState(this);
        }

        private void Page_OrientationChanged(object sender)
        {
            SetCurrentViewState(this);
        }

        private void SetCurrentViewState(Control viewStateAwareControl)
        {
            VisualStateManager.GoToState(viewStateAwareControl, GetViewState(), false);
        }

        private String GetViewState()
        {
            var orientation = DisplayProperties.CurrentOrientation;

            if (orientation == DisplayOrientations.Portrait ||
                orientation == DisplayOrientations.PortraitFlipped)
            {
                return "Portrait";
            }

            var layout = ApplicationView.Value;

            if (layout == ApplicationViewState.Filled)
            {
                return "Fill";
            }
            
            if (layout == ApplicationViewState.Snapped)
            {
                return "Snapped";
            }
            
            return "Full";
        }
        #endregion
    }
}