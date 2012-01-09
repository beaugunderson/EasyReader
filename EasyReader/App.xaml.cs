using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Windows.ApplicationModel.Activation;

using Windows.UI.ApplicationSettings;
using Windows.UI.Core;
using Windows.UI.Xaml;

using Windows.Storage;

using EasyReader.Data;
using EasyReader.Hacks;
using EasyReader.Pages;
using ReadItLaterApi.Metro;
using Windows.Data.Json;

namespace EasyReader
{
    partial class App
    {
        private static ReadingListDataSource _readingListData;

        private Windows.Storage.ApplicationData _applicationData = Windows.Storage.ApplicationData.Current;

        private Timer _updateReadingListTimer;

        private static CollectionSummaryPage _summaryPage;
        private static WebViewDetailPage _detailPage;

        public ReadItLaterApi.Metro.ReadItLaterApi ReadItLaterApi { get; set; }

        public static ObservableVector<object> ReadingList { get; set; }

        public static bool IsUpdating { get; set; }

        public App()
        {
            InitializeComponent();
        }

        public static bool HasConnectivity
        {
            get
            {
                var _internetConnectionProfile = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();

                if (_internetConnectionProfile != null &&
                    _internetConnectionProfile.Connected)
                {
                    return true;
                }

                return false;
            }
        }

        private bool CheckCredentials()
        {
            var roamingSettings = _applicationData.RoamingSettings;

            if (roamingSettings.Values.ContainsKey("username") &&
                roamingSettings.Values.ContainsKey("password"))
            {
                var _username = roamingSettings.Values["username"] as string;
                var _password = roamingSettings.Values["password"] as string;

                if (string.IsNullOrWhiteSpace(_username) ||
                    string.IsNullOrWhiteSpace(_password))
                {
                    return false;
                }
                else
                {
                    ReadItLaterApi = new ReadItLaterApi.Metro.ReadItLaterApi(_username, _password);

                    return true;
                }
            }

            return false;
        }

        public void UpdateReadingList()
        {
            Debug.WriteLine("UpdateReadingList()");

            if (!App.HasConnectivity)
            {
                return;
            }

            if (!CheckCredentials())
            {
                return;
            }

            var readingList = ReadItLaterApi.GetReadingList();

            // Serialize the reading list to the roaming data directory
            SaveItemToFile(_applicationData.RoamingFolder, "reading-list", readingList.Stringify());

            foreach (var item in readingList.List)
            {
                Debug.WriteLine(string.Format("Getting text for '{0}'", item.Key));

                var text = ReadItLaterApi.GetText(item.Value);

                Debug.WriteLine(string.Format("Saving text for '{0}'", item.Key));

                SaveItemToFile(_applicationData.LocalFolder, item.Key, text);

                _summaryPage.Dispatcher.Invoke(CoreDispatcherPriority.Normal, (o, target) =>
                {
                    _readingListData.AddItem(item.Value, text);
                }, this, null);
            }
        }

        public async void SaveItemToFile(StorageFolder folder, string id, string data)
        {
            Debug.WriteLine(string.Format("Local folder: {0}",
                _applicationData.LocalFolder.Path));

            var filename = string.Format("{0}.json", id);

            var file = await folder.CreateFileAsync(filename, Windows.Storage.CreationCollisionOption.ReplaceExisting);

            var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);

            var outputStream = stream.GetOutputStreamAt(0);

            var writer = new Windows.Storage.Streams.DataWriter(outputStream);

            writer.WriteString(data);

            await writer.StoreAsync();

            await outputStream.FlushAsync();
        }

        public async Task<string> ReadItemFromFile(StorageFolder folder, string id)
        {
            var filename = string.Format("{0}.json", id);

            var file = await folder.CreateFileAsync(filename, Windows.Storage.CreationCollisionOption.OpenIfExists);

            var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);

            var size = stream.Size;

            if (size == 0)
            {
                return null;
            }
            else
            {
                var inputStream = stream.GetInputStreamAt(0);

                var reader = new Windows.Storage.Streams.DataReader(inputStream);

                await reader.LoadAsync((uint)size);

                var contents = reader.ReadString((uint)size);

                return contents;
            }
        }

        public void ShowHomePageAndOpenSettings()
        {
            ShowCollectionSummary();

            Window.Current.Activate();

            var page = Window.Current.Content as CollectionSummaryPage;

            if (page != null)
            {
                page.ShowSettingsUserControl();
            }
        }

        protected async override void OnLaunched(LaunchActivatedEventArgs args)
        {
            _applicationData.DataChanged += new Windows.Foundation.TypedEventHandler<Windows.Storage.ApplicationData, object>(applicationData_DataChanged);

            _readingListData = new ReadingListDataSource();

            var readingList = await ReadItemFromFile(_applicationData.RoamingFolder, "reading-list");

            if (readingList != null)
            {
                var json = new JsonObject(readingList);

                var list = new ReadingList(json);

                foreach (var item in list.List)
                {
                    var content = await ReadItemFromFile(_applicationData.LocalFolder, item.Key);

                    _readingListData.AddItem(item.Value, content);
                }
            }

            ReadingList = _readingListData.Items;

            // Setup the Settings command to open our SettingsUserControl on the home page
            SetupSettingsPane();

            // Show the home screen
            ShowCollectionSummary();

            Window.Current.Activate();

            // Show the settings panel if the username and password aren't set
            if (!CheckCredentials())
            {
                ShowHomePageAndOpenSettings();
            }

            // Start a timer to update the reading list
            _updateReadingListTimer = new Timer(delegate
                {
                    // XXX: Best way to handle a long-running update?
                    if (IsUpdating)
                    {
                        return;
                    }

                    IsUpdating = true;

                    _summaryPage.UpdateSubTitle();

                    UpdateReadingList();

                    IsUpdating = false;

                    _summaryPage.UpdateSubTitle();
                },
                null,
                TimeSpan.FromSeconds(5), 
                TimeSpan.FromMinutes(5));
        }

        private void SetupSettingsPane()
        {
            var preferencesCommand = new SettingsCommand(KnownSettingsCommand.Preferences, (handler) =>
            {
                ShowHomePageAndOpenSettings();
            });

            var pane = SettingsPane.GetForCurrentView();

            pane.ApplicationCommands.Add(preferencesCommand);
        }

        private void applicationData_DataChanged(Windows.Storage.ApplicationData sender, object args)
        {
            // XXX: Do something here?
        }

        public static void ShowCollectionSummary()
        {
            _summaryPage = new CollectionSummaryPage();

            Debug.WriteLine("BaseUri: " + _summaryPage.BaseUri);

            Window.Current.Content = _summaryPage;
        }

        public static void ShowDetail(object item)
        {
            _detailPage = new WebViewDetailPage();

            _detailPage.Item = (ReadingListDataItem)item;

            Window.Current.Content = _detailPage;
        }
    }
}