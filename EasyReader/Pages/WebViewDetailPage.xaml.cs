using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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
        private WindowSizeChangedEventHandler _layoutHandler; 

        private const string FORMAT_STRING = @"<!DOCTYPE html>
<html>
 <head>
  <title>{0}</title>

  <script>
   window.external.notify('Complete');
  </script>

  <style type=""text/css"">
   html, body {{
      font-family: Segoe UI;
      padding: 0;
      margin: 0;
      padding-left: 1em;
      padding-right: 2em;
      background-color: #0c0c0c;
      color: white;
   }}

   /* XXX: Doesn't work */
   ::selection {{
      background-color: #fde405;
      color: #0c0c0c;
   }}

   a {{
      color: #fde405;
      text-decoration: none;
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
            var dangling = new Regex(@"\s*</d\s*$", RegexOptions.IgnoreCase);
            var nodeIndex = new Regex(@"\s+nodeIndex=""\d+""", RegexOptions.IgnoreCase);

            body = dangling.Replace(body, "");
            body = nodeIndex.Replace(body, "");

            body = body.Trim();

            return string.Format(FORMAT_STRING, title, body);
        }

        public ReadingListDataItem Item { get; set; }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Page_Loaded()");

            if (_displayHandler == null)
            {
                _displayHandler = Page_OrientationChanged;
                _layoutHandler = Page_LayoutChanged;
            }

            DisplayProperties.OrientationChanged += _displayHandler;

            Window.Current.SizeChanged += _layoutHandler;

            SetCurrentOrientation(this);

            ContentWebView.LoadCompleted += ContentWebView_LoadCompleted;
            ContentWebView.ScriptNotify += ContentWebView_ScriptNotify;

            if (Item != null)
            {
                PageTitle.Text = Item.Title;

                // var content = WrapHtml(Item.Title, Item.Content);

                // ContentWebView.Navigate(new Uri("http://www.bing.com/"));

                // ContentWebView.NavigateToString(content);
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

        //void PreviousButton_Click(object sender, RoutedEventArgs e)
        //{
        //}

        //void NextButton_Click(object sender, RoutedEventArgs e)
        //{
        //}

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            DisplayProperties.OrientationChanged -= _displayHandler;

            Window.Current.SizeChanged -= _layoutHandler;
        }

        private void Page_LayoutChanged(object sender, WindowSizeChangedEventArgs e)
        {
            SetCurrentOrientation(this);
        }

        private void Page_OrientationChanged(object sender)
        {
            SetCurrentOrientation(this);
        }

        private void SetCurrentOrientation(Control viewStateAwareControl)
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

        //private void ApplicationBar_Opened(object sender, object e)
        //{
        //    var webViewBrush = new WebViewBrush();

        //    webViewBrush.SourceName = "ContentWebView";
        //    webViewBrush.Redraw();

        //    WebViewRectangle.Fill = webViewBrush;

        //    WebViewRectangle.Visibility = Visibility.Visible;

        //    ContentWebView.Visibility = Visibility.Collapsed;
        //}

        //private void ApplicationBar_Closed(object sender, object e)
        //{
        //    ContentWebView.Visibility = Visibility.Visible;

        //    WebViewRectangle.Visibility = Visibility.Collapsed;
        //}
    }
}