using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private const string _formatString = @"<!DOCTYPE html>
<html>
 <head>
  <title>{0}</title>

  <script>
   window.external.notify('Complete');
  </script>

  <style type=""text/css"">
   body {{
      font-family: Segoe UI;
      padding: 0;
      background-color: black;
      color: white;
   }}

   a {{
      color: #fde405;
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
            // Remove weird dangling element and nodeIndex attributes
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

            ContentWebView.LoadCompleted += new Windows.UI.Xaml.Navigation.LoadCompletedEventHandler(ContentWebView_LoadCompleted);
            ContentWebView.ScriptNotify += new NotifyEventHandler(ContentWebView_ScriptNotify);

            if (Item != null)
            {
                PageTitle.Text = Item.Title;

                var content = WrapHtml(Item.Title, Item.Content);

                //ContentWebView.Navigate(new Uri("http://www.bing.com/"));

                ContentWebView.NavigateToString(content);
            }
        }

        private void ContentWebView_ScriptNotify(object sender, NotifyEventArgs e)
        {
            Debug.WriteLine("ScriptNotify: " + e.Value);
        }

        private void ContentWebView_LoadCompleted(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            Debug.WriteLine("Redrawing WebViewBrush");
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

        private void ApplicationBar_Opened(object sender, object e)
        {
            var webViewBrush = new WebViewBrush();

            webViewBrush.SourceName = "ContentWebView";
            webViewBrush.Redraw();

            WebViewRectangle.Fill = webViewBrush;

            WebViewRectangle.Visibility = Visibility.Visible;

            ContentWebView.Visibility = Visibility.Collapsed;
        }

        private void ApplicationBar_Closed(object sender, object e)
        {
            ContentWebView.Visibility = Visibility.Visible;

            WebViewRectangle.Visibility = Visibility.Collapsed;
        }
    }
}