using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

using Windows.Foundation;
using Windows.Graphics.Display;

using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;

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
        
        private void UserControl_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerEventArgs e)
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
            var selection = (sender as Selector).SelectedItem;
            var selectedItem = selection as Data.ReadingListDataItem;

            App.ShowDetail(selectedItem);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Windows.Networking.Connectivity.NetworkInformation.NetworkStatusChanged += new Windows.Networking.Connectivity.NetworkStatusChangedEventHandler(NetworkInformation_NetworkStatusChanged);

            Items = App.ReadingList;

            Items.VectorChanged += new Windows.Foundation.Collections.VectorChangedEventHandler<object>(Items_VectorChanged);

            SetupViewState();

            UpdateSubTitle();
        }

        private void Items_VectorChanged(Windows.Foundation.Collections.IObservableVector<object> sender, Windows.Foundation.Collections.IVectorChangedEventArgs @event)
        {
            Debug.WriteLine("Collection changed.");

            Dispatcher.Invoke(Windows.UI.Core.CoreDispatcherPriority.Normal, (o, target) =>
            {
                //ItemGridView.UpdateLayout();
                //ItemListView.UpdateLayout();
            }, this, null);
        }

        public void UpdateSubTitle()
        {
            //var text = App.HasConnectivity ? "(Online)" : "(Offline)";

            string text = "(Offline)";

            if (App.HasConnectivity)
            {
                if (App.IsUpdating)
                {
                    text = "(Online, Updating)";
                }
                else
                {
                    text = "(Online)";
                }
            }

            PageSubTitle.Dispatcher.Invoke(Windows.UI.Core.CoreDispatcherPriority.Normal, (o, target) =>
            {
                PageSubTitle.Text = text;
            }, this, null);
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
                return this._items;
            }

            set
            {
                this._items = value;

                CollectionViewSource.Source = value;
            }
        }

        private Object _item;
        public Object Item
        {
            get
            {
                return this._item;
            }

            set
            {
                this._item = value;
                
                LayoutRoot.DataContext = value;
            }
        }
        #endregion

        #region View state management
        // View state management for switching among Full, Fill, Snapped, and Portrait states
        private DisplayPropertiesEventHandler _displayHandler;
        private TypedEventHandler<ApplicationLayout, ApplicationLayoutChangedEventArgs> _layoutHandler;

        private void SetupViewState()
        {
            if (_displayHandler == null)
            {
                _displayHandler = Page_OrientationChanged;
                _layoutHandler = Page_LayoutChanged;
            }

            DisplayProperties.OrientationChanged += _displayHandler;

            ApplicationLayout.GetForCurrentView().LayoutChanged += _layoutHandler;

            SetCurrentViewState(this);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            DisplayProperties.OrientationChanged -= _displayHandler;

            ApplicationLayout.GetForCurrentView().LayoutChanged -= _layoutHandler;
        }

        private void Page_LayoutChanged(object sender, ApplicationLayoutChangedEventArgs e)
        {
            SetCurrentViewState(this);
        }

        private void Page_OrientationChanged(object sender)
        {
            SetCurrentViewState(this);
        }

        private void SetCurrentViewState(Control viewStateAwareControl)
        {
            VisualStateManager.GoToState(viewStateAwareControl, this.GetViewState(), false);
        }

        private String GetViewState()
        {
            var orientation = DisplayProperties.CurrentOrientation;

            if (orientation == DisplayOrientations.Portrait ||
                orientation == DisplayOrientations.PortraitFlipped)
            {
                return "Portrait";
            }
            
            var layout = ApplicationLayout.Value;

            if (layout == ApplicationLayoutState.Filled)
            {
                return "Fill";
            }

            if (layout == ApplicationLayoutState.Snapped)
            {
                return "Snapped";
            }

            return "Full";
        }
        #endregion
    }
}