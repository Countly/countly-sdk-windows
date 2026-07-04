using System;
using System.Linq;
using System.Windows;
using CountlySDK.CountlyCommon;
using CountlySDK.Entities;
using CountlySDK.UI;

namespace CountlyFeedbackDemo.Wpf
{
    public partial class MainWindow : Window
    {
        private CountlyFeedbackWidget[] _widgets = new CountlyFeedbackWidget[0];

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void InitBtn_Click(object sender, RoutedEventArgs e)
        {
            try {
                CountlySDK.Countly.Halt();
                CountlyConfig cc = new CountlyConfig {
                    serverUrl = ServerBox.Text.Trim(),
                    appKey = AppKeyBox.Text.Trim(),
                    appVersion = "1.0"
                };
                await CountlySDK.Countly.Instance.Init(cc);
                await CountlySDK.Countly.Instance.SessionBegin();
                _widgets = await CountlySDK.Countly.Instance.Feedback().GetAvailableFeedbackWidgets();
                WidgetList.ItemsSource = _widgets.Select(w => $"{w.type}   {w.name}   ({w.widgetId})").ToArray();

                bool rt = WebView2Runtime.IsAvailable(out string version);
                StatusText.Text = $"Fetched {_widgets.Length} widget(s). WebView2 runtime: {(rt ? version : "NOT INSTALLED")}";
            } catch (Exception ex) {
                StatusText.Text = "Init/fetch failed: " + ex.Message;
            }
        }

        private void ShowBtn_Click(object sender, RoutedEventArgs e)
        {
            int i = WidgetList.SelectedIndex;
            if (i < 0 || i >= _widgets.Length) {
                StatusText.Text = "Select a widget in the list first.";
                return;
            }
            CountlyWebView.PresentFeedbackWidget(this, _widgets[i], () => StatusText.Text = "Widget closed.");
        }

        private void ContentBtn_Click(object sender, RoutedEventArgs e)
        {
            CountlyWebView.EnableContentZone();
            StatusText.Text = "Content zone enabled (polling). Server must have active content for this device.";
        }
    }
}
