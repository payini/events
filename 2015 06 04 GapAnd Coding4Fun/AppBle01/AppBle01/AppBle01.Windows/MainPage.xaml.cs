﻿using System;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using AppBle01.ViewModels.IndividualObjects;
using AppBle01.ViewModels.Lists;

namespace AppBle01
{
    public sealed partial class MainPage : Page
    {
        #region ---------------------- Variables ----------------------
        private ListBox _deviceListBox;
        public BEDeviceListVM DevicesVM { get; }

        public Visibility BackgroundAccessProblem
        {
            get
            {
                if (GlobalSettings.BackgroundAccessRequested)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }
        #endregion // Variables

        #region ---------------------- Page Navigation Functions ----------------------
        private bool _firstEntry;
        public MainPage()
        {
            InitializeComponent();

            // var appView = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();

            DevicesVM = new BEDeviceListVM();
            _firstEntry = true;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            // Reset Device List selection
            if (_deviceListBox != null)
            {
                _deviceListBox.SelectedIndex = -1;
            }

            if (_firstEntry)
            {
                var backgroundAccessProblemVisibility = Visibility.Collapsed;
                _firstEntry = false;

                // Don't show the background access error for now
                SetValue(BackgroundAccessProblemProperty, Visibility.Collapsed);

                // Request for background access
                // Make sure this is done at the end of this function, as it returns
                // once the first blocking call is encountered
                await GlobalSettings.RequestBackgroundAccessAsync();
                if (!GlobalSettings.BackgroundAccessRequested)
                {
                    backgroundAccessProblemVisibility = Visibility.Visible;
                }
                SetValue(BackgroundAccessProblemProperty, backgroundAccessProblemVisibility);

                // Populate the Device List off the UI thread, but don't block on it
                Utilities.RunFuncAsTask(PopulateLEDeviceListAsync);
            }
        }
        #endregion

        #region ---------------------- Device List Manipulation ---------------------
        /// <summary>
        /// Retrieves the list of Bluetooth LE devices from the OS, initializes our internal
        /// data structures, and attempt to connect to them, if they are advertising.
        /// </summary>
        /// <returns></returns>
        private async Task PopulateLEDeviceListAsync()
        {
            await Utilities.RunActionOnUiThreadAsync(() => IsUserInteractionEnabled = false);

            await GlobalSettings.PopulateDeviceListAsync();

            await Utilities.RunActionOnUiThreadAsync(
                () =>
                {
                    DevicesVM.Initialize(GlobalSettings.PairedDevices);
                    IsUserInteractionEnabled = true;
                });
        }

        #region disabling user interaction on device loading
        public static readonly DependencyProperty IsUserInteractionEnabledProperty =
            DependencyProperty.Register("IsUserInteractionEnabled", typeof(bool), typeof(MainPage), new PropertyMetadata(false));
        public static readonly DependencyProperty IsUpdatingDeviceListProperty =
            DependencyProperty.Register("IsUpdatingDeviceList", typeof(Visibility), typeof(MainPage), new PropertyMetadata(false));
        public static readonly DependencyProperty ShowDeviceListProperty =
            DependencyProperty.Register("ShowDeviceList", typeof(Visibility), typeof(MainPage), new PropertyMetadata(false));
        public static readonly DependencyProperty BackgroundAccessProblemProperty =
            DependencyProperty.Register("BackgroundAccessProblem", typeof(Visibility), typeof(MainPage), new PropertyMetadata(false));
        public bool IsUserInteractionEnabled
        {
            get
            {
                return (bool)GetValue(IsUserInteractionEnabledProperty);
            }
            set
            {
                SetValue(IsUserInteractionEnabledProperty, value);
                if (value)
                {
                    SetValue(IsUpdatingDeviceListProperty, Visibility.Collapsed);
                    SetValue(ShowDeviceListProperty, Visibility.Visible);
                }
                else
                {
                    SetValue(IsUpdatingDeviceListProperty, Visibility.Visible);
                    SetValue(ShowDeviceListProperty, Visibility.Collapsed);
                }
            }
        }
        #endregion // Disable user interaction on device loading

        private void OnDeviceListLoaded(object sender, RoutedEventArgs e)
        {
            // XAML elements are template-based and cannot be directly access
            // hook the ListBox instance here
            if (sender != null)
            {
                _deviceListBox = (ListBox)sender;
            }
        }

        private void OnDeviceSelectionChanged(object sender, RoutedEventArgs e)
        {
            var listBox = (ListBox)sender;

            if (listBox.SelectedIndex == -1)
            {
                return;
            }

            foreach (var listBoxItem in listBox.SelectedItems)
            {
                var device = listBoxItem as BEDeviceVM;
                GlobalSettings.SelectedDevice = device.DeviceM;
            }
            Frame.Navigate(typeof(DeviceInfo));
        }
        #endregion // Device List Manipulation

        #region ---------------------- Bottom Buttons ----------------------
        private void deviceListRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Utilities.RunFuncAsTask(PopulateLEDeviceListAsync);
        }

        private async void settingsButton_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings-bluetooth:"));
        }

        private void goToAbout_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AboutPage));
        }
        #endregion  // Bottom Buttons
    }
}
