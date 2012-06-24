using System;
using System.Diagnostics;

using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.Networking.Connectivity;
using Windows.UI.ApplicationSettings;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage;

using EasyReader.Controls;
using EasyReader.Data;
using EasyReader.Hacks;
using EasyReader.Helpers;
using EasyReader.Pages;

namespace EasyReader
{
    partial class App
    {
        private static ReadingListDataSource _readingListDataSource;

        private readonly ApplicationData _applicationData = ApplicationData.Current;

        private DispatcherTimer _updateReadingListTimer;

        private SettingsHelper _settingsHelper;
        private static Frame _rootFrame;

        public static ObservableVector<object> ReadingList { get; private set; }

        public App()
        {
            InitializeComponent();

            Suspending += OnSuspending;
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

            try
            {
                await BackgroundExecutionManager.RequestAccessAsync();
            }
            catch (Exception)
            {
                
            }

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
            Debug.WriteLine("task_Completed()");
        }

        private void task_Progress(BackgroundTaskRegistration sender, BackgroundTaskProgressEventArgs args)
        {
            Debug.WriteLine("task_Progress()");
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // Do not repeat app initialization when already running, just ensure that
            // the window is active
            if (args.PreviousExecutionState == ApplicationExecutionState.Running)
            {
                Window.Current.Activate();

                return;
            }

            //RegisterBackgroundTasks();

            _applicationData.DataChanged += applicationData_DataChanged;

            _readingListDataSource = new ReadingListDataSource();

            ReadingList = _readingListDataSource.Items;

            // Setup the Settings command to open our settingsUserControl on the home page
            SetupSettingsPane();

            // Show the home screen
            ShowReadingListItemsPage();

            // Show the settings panel if the username and password aren't set
            if (!_readingListDataSource.CheckCredentials())
            {
                SettingsPane.Show();
            }

            _updateReadingListTimer = new DispatcherTimer
            {
              Interval = TimeSpan.FromMinutes(15)
            };

            _updateReadingListTimer.Tick += UpdateReadingListTimerOnTick;

            //_updateReadingListTimer.Start();

            if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
            {
                // TODO: Load state from previously suspended application
            }
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            // TODO: Save application state and stop any background activity
            
            deferral.Complete();
        }

        private async void UpdateReadingListTimerOnTick(object sender, object o)
        {
            Debug.WriteLine("UpdateReadingListTimerOnTick()");

            await _readingListDataSource.UpdateReadingList();

            Debug.WriteLine("UpdateReadingListTimerOnTick() finished.");
        }

        private void SetupSettingsPane()
        {
            _settingsHelper = new SettingsHelper();

            _settingsHelper.AddCommand<SettingsUserControl>("Preferences");

            SettingsPane.GetForCurrentView().CommandsRequested += OnCommandsRequested;
        }

        private void OnCommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            // XXX: This is a hack because the WebView on the DetailPage is the topmost
            //      object on the page and hides the settings pane.
            if (_rootFrame.CurrentSourcePageType != typeof (ReadingListItemsPage))
            {
                _rootFrame.Navigate(typeof (ReadingListItemsPage));
            }
        }

        private void applicationData_DataChanged(ApplicationData sender, object args)
        {
            Debug.WriteLine("DataChanged");
        }

        public static void ShowReadingListItemsPage()
        {
            // Create a Frame to act as the navigation context and navigate to the first page
            _rootFrame = new Frame();

            _rootFrame.Navigate(typeof(ReadingListItemsPage));

            // Place the frame in the current window and ensure that it is active
            Window.Current.Content = _rootFrame;
            Window.Current.Activate();
        }
    }
}