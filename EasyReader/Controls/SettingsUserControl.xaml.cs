using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Windows.Foundation;

using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace EasyReader.Controls
{
    public sealed partial class SettingsUserControl
    {
        private Windows.Storage.ApplicationData _applicationData;
        private Windows.Storage.ApplicationDataContainer _roamingSettings;

        public bool IsInView
        {
            get
            {
                return Margin.Right == 0;
            }
        }

        public SettingsUserControl()
        {
            InitializeComponent();

            _applicationData = Windows.Storage.ApplicationData.Current;
            _roamingSettings = _applicationData.RoamingSettings;

            _applicationData.DataChanged += new TypedEventHandler<Windows.Storage.ApplicationData,object>(_applicationData_DataChanged);
        }

        private void _applicationData_DataChanged(Windows.Storage.ApplicationData sender, object args)
        {
            UpdateInputs();
        }

        public void UpdateInputs()
        {
            // XXX: There has to be an easier way than this...
            if (_roamingSettings.Values.ContainsKey("username"))
            {
                usernameTextBox.Dispatcher.Invoke(Windows.UI.Core.CoreDispatcherPriority.Normal, (o, target) =>
                {
                    usernameTextBox.Text = (string)_roamingSettings.Values["username"];
                }, this, null);
            }

            if (_roamingSettings.Values.ContainsKey("password"))
            {
                passwordPasswordBox.Dispatcher.Invoke(Windows.UI.Core.CoreDispatcherPriority.Normal, (o, target) =>
                {
                    passwordPasswordBox.Password = (string)_roamingSettings.Values["password"];
                }, this, null);
            }
        }

        private void SaveSettings(string username, string password)
        {
            _roamingSettings.Values["username"] = username;
            _roamingSettings.Values["password"] = password;

            _applicationData.SignalDataChanged();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(passwordPasswordBox.Password) &&
                !string.IsNullOrWhiteSpace(usernameTextBox.Text))
            {
                SaveSettings(usernameTextBox.Text.Trim(), passwordPasswordBox.Password);

                Hide();
            }
            else
            {
                var messageDialog = new Windows.UI.Popups.MessageDialog("Please enter your username and password.", "Error");

                await messageDialog.ShowAsync();
            }
        }

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!App.HasConnectivity)
            {
                var messageDialog = new Windows.UI.Popups.MessageDialog("You can't create a Read it Later account until you have Internet access. Please try again once you're connected to the Internet.", "Error");

                await messageDialog.ShowAsync();

                return;
            }

            if (!string.IsNullOrWhiteSpace(passwordPasswordBox.Password) &&
                !string.IsNullOrWhiteSpace(usernameTextBox.Text))
            {
                var username = usernameTextBox.Text;
                var password = passwordPasswordBox.Password;

                var api = new ReadItLaterApi.Metro.ReadItLaterApi(username, password);

                if (api.CreateAccount())
                {
                    SaveSettings(usernameTextBox.Text.Trim(), passwordPasswordBox.Password);

                    Hide();
                }
                else
                {
                    var messageDialog = new Windows.UI.Popups.MessageDialog("There was an error creating your Read it Later account. Please try again later.", "Error");
    
                    await messageDialog.ShowAsync();
                }
            }
        }

        public void Show()
        {
                Margin = ThicknessHelper.FromUniformLength(0);
        }

        public void Hide()
        {
                Margin = ThicknessHelper.FromLengths(0, 0, -346, 0);
        }

        private void LayoutRoot_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateInputs();
        }
    }
}