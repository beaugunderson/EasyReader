using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

using EasyReader.Common;
using EasyReader.Data;

namespace EasyReader.Pages
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class DetailPage
    {
        public DetailPage()
        {
            InitializeComponent();
        }

        private const string FORMAT_STRING = @"<!DOCTYPE html>
<html>
 <head>
  <title>{0}</title>

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

   body > div {{
      column-count: 2;
      column-width: auto;
      overflow: auto;
      height: {1}px;
      padding-bottom: 20px;
   }}

   img {{
      float: right;
      padding: 7px;
      background-color: #222;
      border: 1px solid #fde405;
      margin-left: 10px;
      margin-top: 10px;
      margin-bottom: 10px;
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
  {2}
 </body>
</html>";

        private string WrapHtml(string title, string body, double height)
        {
            return string.Format(FORMAT_STRING, title, height - 160, body.Trim());
        }

        public ReadingListDataItem Item { get; set; }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            Debug.WriteLine("DetailPage OnNavigatedTo()");

            Item = e.Parameter as ReadingListDataItem;

            if (Item != null)
            {
                pageTitle.Text = Item.Title;

                Debug.WriteLine("Window height: {0}", Window.Current.Bounds.Height);

                var content = WrapHtml(Item.Title, Item.Content, Window.Current.Bounds.Height);

                try
                {
                    ContentWebView.NavigateToString(content);
                }
                catch (Exception)
                {
                    Debug.WriteLine("ContentWebView.NavigateToString exception!");
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
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
        }
    }
}
