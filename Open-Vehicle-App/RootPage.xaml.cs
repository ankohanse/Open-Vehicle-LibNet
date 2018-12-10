/*
;    Project:       Open-Vehicle-LibNet
;
;    Changes:
;    1.0    2018-12-01  Initial release
;
;    (C) 2018       Anko Hanse
;
; Permission is hereby granted, free of charge, to any person obtaining a copy
; of this software and associated documentation files (the "Software"), to deal
; in the Software without restriction, including without limitation the rights
; to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
; copies of the Software, and to permit persons to whom the Software is
; furnished to do so, subject to the following conditions:
;
; The above copyright notice and this permission notice shall be included in
; all copies or substantial portions of the Software.
;
; THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
; IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
; FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
; AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
; LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
; OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
; THE SOFTWARE.
*/

using OpenVehicle.App.Entities;
using OpenVehicle.LibNet;
using OpenVehicle.LibNet.Entities;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using nav = OneCode.Windows.UWP.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace OpenVehicle.App
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public partial class RootPage : Page, INotifyPropertyChanged
    {
        public static RootPage      Instance;

        public Frame                RootFrame       => rootFrame;
        public nav.NavigationView   RootNav         => rootNav;

         // ViewModel
        public RootViewModel        ViewModel       { get; private set; }


        // General
        // For use in x:Bind
        protected CarData           CarData             => App.RootViewModel.CarData;
        protected AppCarSettings    CarSettings         => App.RootViewModel.CarSettings;


        private bool                HomeLinkVisible     => OVMSService.Instance.IsCommandSupported( OVMSService.Command.HomeLink );


        public RootPage()
        {
            this.InitializeComponent();

            Instance   = this;
            this.ViewModel = new RootViewModel();
            this.RootNav.Header = this.ViewModel;

            CoreWindow                  coreWindow   = Window.Current.CoreWindow;
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;

            coreWindow.SizeChanged            += (s, e) => OnSizeChangedForAppTitle();
            coreTitleBar.LayoutMetricsChanged += (s, e) => OnSizeChangedForAppTitle();
        }


        private void OnSizeChangedForAppTitle()
        {
            var full = (ApplicationView.GetForCurrentView().IsFullScreenMode);
            var left = 12 + (full ? 0 : CoreApplication.GetCurrentView().TitleBar.SystemOverlayLeftInset);
            AppTitle.Margin = new Thickness(left, 8, 0, 0);
        }


        private void OnNavItemInvoked(nav.NavigationView sender, nav.NavigationViewItemInvokedEventArgs args)
        {
            string sTag = "";

            if (args.IsSettingsInvoked)
            {
                sTag = "SettingsPage";
            }
            else
            {
                foreach (object item in rootNav.MenuItems)
                {
                    if (item.GetType() != typeof(nav.NavigationViewItem))
                        continue;

                    nav.NavigationViewItem navItem = item as nav.NavigationViewItem;
                    if (string.Equals(navItem.Content, args.InvokedItem))
                    {
                        sTag = navItem.Tag.ToString();
                    }
                }
            }

            switch (sTag)
            {
                default:
                case "BatteryPage":     rootFrame.Navigate(typeof(BatteryPage));    break;
                case "ChargePage":      rootFrame.Navigate(typeof(ChargePage));     break;
                case "ClimatePage":     rootFrame.Navigate(typeof(ClimatePage));    break;
                case "DoorsPage":       rootFrame.Navigate(typeof(DoorsPage));      break;
                case "TyresPage":       rootFrame.Navigate(typeof(TyresPage));      break;
                case "LocationPage":    rootFrame.Navigate(typeof(LocationPage));   break;
                case "InfoPage":        rootFrame.Navigate(typeof(InfoPage));       break;
                case "SettingsPage":    rootFrame.Navigate(typeof(SettingsPage));   break;

                case "Flyout":  break;      // Do not navigate, instead is handled by Tapped handler
            }
        }


        private void OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (RootFrame.CanGoBack)
            {
                RootFrame.GoBack();
            }
        }

        private void BackInvoked (KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            OnBackRequested(null, null);
            args.Handled = true;
        }


        private async void OnCarSelectedChanged(object sender, SelectionChangedEventArgs e)
        {
            // Notify the ViewModel that 
            // - The OVMS Service needs to restart using the new selected car
            // - The selected car settings need to be renewed
            // - The displayed car data need to be renewed
            await ViewModel.RestartAsync();
        }


        private void OnHomeLinkMenu(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;

            FrameworkElement element = (FrameworkElement)sender;
            if (element != null)
                HomeLinkFlyout.ShowAt(element, new Point(40,0));
        }


        private async void OnHomeLinkDefault(object sender, RoutedEventArgs e)
        {
            await OVMSService.Instance.TransmitCommandAsync( OVMSService.Command.HomeLink );
        }


        private async void OnHomeLink0(object sender, RoutedEventArgs e)
        {
            await OVMSService.Instance.TransmitCommandAsync( OVMSService.Command.HomeLink, "0" );
        }


        private async void OnHomeLink1(object sender, RoutedEventArgs e)
        {
            await OVMSService.Instance.TransmitCommandAsync( OVMSService.Command.HomeLink, "1" );
        }


        private async void OnHomeLink2(object sender, RoutedEventArgs e)
        {
            await OVMSService.Instance.TransmitCommandAsync( OVMSService.Command.HomeLink, "2" );
        }


        public void ShowPopup(string sTitle, string sMsg)
        {
            FlyoutTitle.Text   = sTitle;
            FlyoutMessage.Text = sMsg;

            Flyout.ShowAttachedFlyout(FLyoutAnchor);
        }


        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        

        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // Raise the PropertyChanged event, passing the name of the property whose value has changed.
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged    
    }
}
