using System;
using System.Collections.Generic;

using Windows.UI.ApplicationSettings;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace EasyReader.Helpers
{
    public class SettingsHelper
    {
        private readonly Popup _popup = new Popup();

        private readonly List<ICommandInfo> _commands = new List<ICommandInfo>();

        public SettingsHelper()
        {
            SettingsPane.GetForCurrentView().CommandsRequested += (s, e) =>
            {
                foreach (var item in _commands)
                {
                    var command = new SettingsCommand(item.Key, item.Text, 
                        c => Show(item.Instance(), item.Width));

                    e.Request.ApplicationCommands.Add(command);
                }
            };
        }

        public void AddCommand<T>(string text, string key = null,
            PanelWidths width = PanelWidths.Small) where T : UserControl, new()
        {
            _commands.Add(new CommandInfo<T>
            {
                Key = key ?? text,
                Text = text,
                Width = (int)width
            });
        }

        private Popup Show(UserControl control, double width)
        {
            if (control == null)
            {
                throw new Exception("Control is not defined");
            }

            if (double.IsNaN(width))
            {
                throw new Exception("Width is not defined");
            }

            var page = ((Frame) Window.Current.Content).Content as Page;
            
            _popup.Width = width;
            _popup.HorizontalAlignment = HorizontalAlignment.Right;
            _popup.Height = page.RenderSize.Height;

            _popup.SetValue(Canvas.LeftProperty, page.RenderSize.Width - _popup.Width);

            // Make content fit
            _popup.Child = control;

            control.VerticalAlignment = VerticalAlignment.Stretch;
            control.HorizontalAlignment = HorizontalAlignment.Stretch;

            control.Height = _popup.Height;
            control.Width = _popup.Width;

            // Add pretty animation(s)
            _popup.ChildTransitions = new Windows.UI.Xaml.Media.Animation.TransitionCollection 
            {
                new Windows.UI.Xaml.Media.Animation.EntranceThemeTransition 
                { 
                    FromHorizontalOffset = 20d, 
                    FromVerticalOffset = 0d 
                }
            };

            // Setup
            _popup.IsLightDismissEnabled = true;
            _popup.IsOpen = true;

            // Handle when it closes
            _popup.Closed -= Popup_Closed;
            _popup.Closed += Popup_Closed;

            // Handle making it close
            Window.Current.Activated -= Current_Activated;
            Window.Current.Activated += Current_Activated;

            return _popup;
        }

        private void Current_Activated(object sender, WindowActivatedEventArgs e)
        {
            if (_popup == null)
            {
                return;
            }

            if (e.WindowActivationState == CoreWindowActivationState.Deactivated)
            {
                _popup.IsOpen = false;
            }
        }

        private void Popup_Closed(object sender, object e)
        {
            Window.Current.Activated -= Current_Activated;

            if (_popup == null)
            {
                return;
            }
            
            _popup.IsOpen = false;
        }

        private interface ICommandInfo
        {
            string Key { get; set; }
            string Text { get; set; }
            double Width { get; set; }

            UserControl Instance();
        }

        private class CommandInfo<T> : ICommandInfo where T : UserControl, new()
        {
            public string Key { get; set; }
            public string Text { get; set; }
            public double Width { get; set; }
            
            public UserControl Instance()
            {
                return new T();
            }
        }

        public enum PanelWidths
        {
            Small = 346,
            Large = 646
        }
    }
}
