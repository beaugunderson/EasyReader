using System;
using System.Collections.Generic;

using Windows.Foundation;
using Windows.Graphics.Display;

using Windows.UI.ApplicationSettings;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;

namespace EasyReader
{
    public sealed partial class CollectionSummaryPage
    {
        public CollectionSummaryPage()
        {
            InitializeComponent();

            var pane = SettingsPane.GetForCurrentView();
            
            var preferencesCommand = new SettingsCommand(KnownSettingsCommand.Preferences, (handler) =>
            {
                SettingsUserControl.Margin = ThicknessHelper.FromUniformLength(0);
            });

            pane.ApplicationCommands.Add(preferencesCommand);

            Windows.UI.ApplicationSettings.SettingsPane.Show();
        }

        void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Construct the appropriate destination page and set its context appropriately
            App.ShowGroupedCollection();
        }

        void ItemView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Construct the appropriate destination page and set its context appropriately
            var selection = (sender as Selector).SelectedItem;
            var selectedItem = selection as Expression.Blend.SampleData.SampleDataSource.SampleDataItem;

            App.ShowDetail(selectedItem.Collection, selectedItem);
        }

        private IEnumerable<Object> _items;
        public IEnumerable<Object> Items
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

        // View state management for switching among Full, Fill, Snapped, and Portrait states

        private DisplayPropertiesEventHandler _displayHandler;
        private TypedEventHandler<ApplicationLayout, ApplicationLayoutChangedEventArgs> _layoutHandler;

        private void Page_Loaded(object sender, RoutedEventArgs e)
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
                orientation == DisplayOrientations.PortraitFlipped) return "Portrait";

            var layout = ApplicationLayout.Value;
            
            if (layout == ApplicationLayoutState.Filled) return "Fill";
            if (layout == ApplicationLayoutState.Snapped) return "Snapped";
            
            return "Full";
        }

        private void UserControl_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerEventArgs e)
        {
            if (SettingsUserControl.Margin.Right == 0)
            {
                SettingsUserControl.Margin = ThicknessHelper.FromLengths(0, 0, -346, 0);
            }
        }

        private void Button_Click(EventArgs e)
        {

        }
    }
}