using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.Graphics.Display;

using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

using EasyReader.Data;

namespace EasyReader.Pages
{
    public sealed partial class WebViewDetailPage
    {
        public WebViewDetailPage()
        {
            InitializeComponent();
        }

        // View state management for switching among Full, Fill, Snapped, and Portrait states
        private DisplayPropertiesEventHandler _displayHandler;
        private TypedEventHandler<ApplicationLayout, ApplicationLayoutChangedEventArgs> _layoutHandler;

        private const string _formatString = @"<html>
 <head>
  <title>{0}</title>

  <style type=""text/css"">
   body {{
      font-family: Segoe UI;
      padding: 1em;
      background-color: black;
      color: white;
   }}

   pre {{
      margin-left: 2em;
   }}
  </style>
 </head>

 <body>
  {1}
 </body>
</html>";

        private string WrapHtml(string title, string body)
        {
            // Remove weird dangling element
            Regex dangling = new Regex(@"\s*</d\s*$", RegexOptions.IgnoreCase);
            Regex nodeIndex = new Regex(@"\s+nodeIndex=""\d+""", RegexOptions.IgnoreCase);

            body = dangling.Replace(body, "");
            body = nodeIndex.Replace(body, "");

            body = body.Trim();

            return string.Format(_formatString, title, body);
        }

        public ReadingListDataItem Item { get; set; }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (_displayHandler == null)
            {
                _displayHandler = Page_OrientationChanged;
                _layoutHandler = Page_LayoutChanged;
            }

            DisplayProperties.OrientationChanged += _displayHandler;
            ApplicationLayout.GetForCurrentView().LayoutChanged += _layoutHandler;
            
            SetCurrentOrientation(this);

            if (Item != null)
            {
                var content = WrapHtml(Item.Title, Item.Content);

                PageTitle.Text = Item.Title;

                ContentWebView.NavigateToString(content);
            }
        }

        void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            App.ShowCollectionSummary();
        }

        void BackButton_Click(object sender, RoutedEventArgs e)
        {
            App.ShowCollectionSummary();
        }

        void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
        }

        void NextButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            DisplayProperties.OrientationChanged -= _displayHandler;
            ApplicationLayout.GetForCurrentView().LayoutChanged -= _layoutHandler;
        }

        private void Page_LayoutChanged(object sender, ApplicationLayoutChangedEventArgs e)
        {
            SetCurrentOrientation(this);
        }

        private void Page_OrientationChanged(object sender)
        {
            SetCurrentOrientation(this);
        }

        private void SetCurrentOrientation(Control viewStateAwareControl)
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
    }
}