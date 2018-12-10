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

using System.ComponentModel;
using System.Runtime.CompilerServices;
using OpenVehicle.App.Entities;
using OpenVehicle.LibNet;
using OpenVehicle.LibNet.Entities;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace OpenVehicle.App
{
    public partial class ClimatePage : Page, INotifyPropertyChanged
    {

        // General
        // For use in x:Bind
        private CarData         CarData             => App.RootViewModel.CarData;
        private AppCarSettings  CarSettings         => App.RootViewModel.CarSettings;
        

        private string AirconStatusText             => (CarData.env_aircon_on) ? "on" : "off";
        private string AirconButtonText             => (OVMSService.Instance.IsCommandSupported( OVMSService.Command.Aircon )) ? ((CarData.env_aircon_on) ? "Switch off" : "Switch on") : "";


        public ClimatePage()
        {
            this.InitializeComponent();
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            App.RootViewModel.PageTitle = "Temperature";
        }


        private async void OnClimateControl(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // Sanity check: does the currently selected car support this command?
            if (!OVMSService.Instance.IsCommandSupported(OVMSService.Command.Aircon))
                return;

            // Send the command
            if (CarData.env_aircon_on)
                await OVMSService.Instance.TransmitCommandAsync( OVMSService.Command.Aircon, "0");   // turn off
            else
                await OVMSService.Instance.TransmitCommandAsync( OVMSService.Command.Aircon, "1" );  // turn on
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



