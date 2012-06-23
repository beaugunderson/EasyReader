using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.Networking.Connectivity;
using Windows.UI.ApplicationSettings;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.Storage;

using EasyReader.Data;
using EasyReader.Hacks;
using EasyReader.Pages;

using ReadItLaterApi.Metro;

namespace EasyReader
{
    partial class App
    {
        private static ReadingListDataSource _readingListData;

        private readonly ApplicationData _applicationData = ApplicationData.Current;

        private DispatcherTimer _updateReadingListTimer;

        private static CollectionSummaryPage _summaryPage;
        private static WebViewDetailPage _detailPage;

        public ReadItLaterApi.Metro.ReadItLaterApi ReadItLaterApi { get; set; }

        public static ObservableVector<object> ReadingList { get; private set; }

        public static bool IsUpdating { get; private set; }

        public App()
        {
            InitializeComponent();
        }

        public static bool HasConnectivity
        {
            get
            {
                var internetConnectionProfile = NetworkInformation.GetInternetConnectionProfile();

                if (internetConnectionProfile == null)
                {
                    return false;
                }

                var connectionStatus = internetConnectionProfile.GetNetworkConnectivityLevel();

                return connectionStatus == NetworkConnectivityLevel.InternetAccess ||
                       connectionStatus == NetworkConnectivityLevel.ConstrainedInternetAccess;
            }
        }

        private async void RegisterBackgroundTasks()
        {
            var builder = new BackgroundTaskBuilder
            {
                Name = "BackgroundUpdateReadingList",
                TaskEntryPoint = "EasyReader.BackgroundTasks.UpdateReadingList"
            };

            await BackgroundExecutionManager.RequestAccessAsync();

            IBackgroundTrigger trigger = new TimeTrigger(15, true);
            builder.SetTrigger(trigger);

            IBackgroundCondition condition = new SystemCondition(SystemConditionType.InternetAvailable);
            builder.AddCondition(condition);

            IBackgroundTaskRegistration task = builder.Register();

            task.Progress += task_Progress;
            task.Completed += task_Completed;
        }

        private void task_Completed(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            throw new NotImplementedException();
        }

        private void task_Progress(BackgroundTaskRegistration sender, BackgroundTaskProgressEventArgs args)
        {
            throw new NotImplementedException();
        }

        private bool CheckCredentials()
        {
            var roamingSettings = _applicationData.RoamingSettings;

            if (roamingSettings.Values.ContainsKey("username") &&
                roamingSettings.Values.ContainsKey("password"))
            {
                var username = roamingSettings.Values["username"] as string;
                var password = roamingSettings.Values["password"] as string;

                if (string.IsNullOrWhiteSpace(username) ||
                    string.IsNullOrWhiteSpace(password))
                {
                    Debug.WriteLine("No credentials.");

                    return false;
                }
                
                ReadItLaterApi = new ReadItLaterApi.Metro.ReadItLaterApi(username, password);

                return true;
            }

            Debug.WriteLine("No credentials.");

            return false;
        }

        public async Task UpdateReadingList()
        {
            Debug.WriteLine("UpdateReadingList()");

            if (!HasConnectivity)
            {
                return;
            }

            if (!CheckCredentials())
            {
                return;
            }

            var readingList = await ReadItLaterApi.GetReadingList();

            // Serialize the reading list to the roaming data directory
            SaveItemToFile(_applicationData.RoamingFolder, "reading-list", readingList.Stringify());
            
            foreach (var item in readingList.List)
            {
                var text = await GetRemoteTextFromListItem(item);

                _summaryPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () => _readingListData.AddItem(item.Value, text));
            }
        }

        private async Task<string> GetRemoteTextFromListItem(KeyValuePair<string, ReadingListItem> item)
        {
            Debug.WriteLine(string.Format("Getting remote text for '{0}'", item.Key));

            var text = await ReadItLaterApi.GetText(item.Value);

            Debug.WriteLine("Writing {0} characters to '{1}'", text.Length, item.Key);

            SaveItemToFile(_applicationData.LocalFolder, item.Key, text);

            return text;
        }

        public async void SaveItemToFile(StorageFolder folder, string id, string data)
        {
            var filename = string.Format("{0}.json", id);

            var file = await folder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

            var stream = await file.OpenAsync(FileAccessMode.ReadWrite);

            var outputStream = stream.GetOutputStreamAt(0);

            var writer = new Windows.Storage.Streams.DataWriter(outputStream);

            writer.WriteString(data);

            await writer.StoreAsync();

            await outputStream.FlushAsync();
        }

        public async Task<string> ReadItemFromFile(StorageFolder folder, string id)
        {
            var filename = string.Format("{0}.json", id);

            Debug.WriteLine(string.Format("Reading from '{0}'", filename));

            var file = await folder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);

            var stream = await file.OpenAsync(FileAccessMode.Read);

            var size = stream.Size;

            if (size == 0)
            {
                return null;
            }

            var inputStream = stream.GetInputStreamAt(0);

            var reader = new Windows.Storage.Streams.DataReader(inputStream);

            await reader.LoadAsync((uint)size);

            var contents = reader.ReadString((uint)size);

            return contents;
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
            RegisterBackgroundTasks();

            _applicationData.DataChanged += applicationData_DataChanged;

            _readingListData = new ReadingListDataSource();

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

            Debug.WriteLine("Waiting for items from disk...");

            await ReadItemsFromDisk();

            Debug.WriteLine("Got items from disk.");

            _updateReadingListTimer = new DispatcherTimer
            {
              Interval = TimeSpan.FromMinutes(5)
            };

            _updateReadingListTimer.Tick += UpdateReadingListTimerOnTick;

            _updateReadingListTimer.Start();

            await UpdateReadingListTimerOnTickInner();
        }

        private async void UpdateReadingListTimerOnTick(object sender, object o)
        {
            await UpdateReadingListTimerOnTickInner();
            
            Debug.WriteLine("UpdateReadingListTimerOnTick() finished.");
        }

        private async Task UpdateReadingListTimerOnTickInner()
        {
            Debug.WriteLine("UpdateReadingListTimerOnTickInner()");

            // XXX: Best way to handle a long-running update?
            if (IsUpdating)
            {
                return;
            }

            IsUpdating = true;

            _summaryPage.UpdateSubTitle();

            await UpdateReadingList();

            IsUpdating = false;

            _summaryPage.UpdateSubTitle();
        }

        private async Task ReadItemsFromDisk()
        {
            var json = await ReadItemFromFile(_applicationData.RoamingFolder, "reading-list");

            if (json == null)
            {
                return;
            }

            var list = new ReadingList(json);

            foreach (var item in list.List)
            {
                var content = await ReadItemFromFile(_applicationData.LocalFolder, item.Key) ?? 
                    (HasConnectivity ? 
                        await GetRemoteTextFromListItem(item) : 
                        "Sorry, this content hasn't been saved for offline reading.");

                _readingListData.AddItem(item.Value, content);
            }
        }

        private void SetupSettingsPane()
        {
            var pane = SettingsPane.GetForCurrentView();

            pane.CommandsRequested += pane_CommandsRequested;
        }

        void pane_CommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            var preferencesCommand = new SettingsCommand("preferences", "Preferences", 
                handler => ShowHomePageAndOpenSettings());
            
            args.Request.ApplicationCommands.Add(preferencesCommand);
        }

        private void applicationData_DataChanged(ApplicationData sender, object args)
        {
            Debug.WriteLine("DataChanged");
        }

        public static void ShowCollectionSummary()
        {
            _summaryPage = new CollectionSummaryPage();

            Debug.WriteLine("BaseUri: " + _summaryPage.BaseUri);

            Window.Current.Content = _summaryPage;
        }

        public static void ShowDetail(object item)
        {
            _detailPage = new WebViewDetailPage
            {
                Item = (ReadingListDataItem) item
            };

            Window.Current.Content = _detailPage;
        }
    }
}