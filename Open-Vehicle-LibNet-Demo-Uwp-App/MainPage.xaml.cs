/*
;    Project:       Open-Vehicle-LibNet
;
;    Changes:
;    1.0    2018-11-01  Initial release
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

using OpenVehicle.LibNet.Entities;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace OpenVehicle.Demo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Logging
        private static NLog.Logger    logger           = NLog.LogManager.GetCurrentClassLogger();

        // ViewModel
        public MainViewModel          ViewModel     { get; set; }
        

        public MainPage()
        {
            this.InitializeComponent();

            this.ViewModel = new MainViewModel();
            this.HubMain.DataContext = ViewModel;
        }


        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await ViewModel.StartAsync();
        }


        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.StartAsync();
        }


        protected async override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            await ViewModel.StopAsync();

            base.OnNavigatingFrom(e);
        }


        private void UnitTemp_Checked(object s, RoutedEventArgs e)
        {
            var radio = s as RadioButton;
            switch (radio.Tag.ToString())
            {
                default:
                case "C": ViewModel.Preferences.UnitForTemperature = OVMSPreferences.UnitTemperature.Celcius; break;
                case "F": ViewModel.Preferences.UnitForTemperature = OVMSPreferences.UnitTemperature.Fahrenheit; break;
            }
            ViewModel.OnPreferencesChanged();
        }

    }
}
