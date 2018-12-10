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
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace OpenVehicle.App
{
    public partial class DoorsPage : Page, INotifyPropertyChanged
    {

        // General
        // For use in x:Bind
        private CarData         CarData             => App.RootViewModel.CarData;
        private AppCarSettings  CarSettings         => App.RootViewModel.CarSettings;
        

        private string LockedImage             =>  (CarData.env_locked)    ? "ms-appx:///Assets/Misc/carlock.png"    : "ms-appx:///Assets/Misc/carunlock.png";
        private string LockedStatusText        =>  (CarData.env_locked)    ? "locked"  : "unlocked";
        private string LockedButtonText        =>  (CarData.env_locked)    ? "Unlock"  : "Lock";
        private bool   LockedButtonVisible     =>  OVMSService.Instance.IsCommandSupported( (CarData.env_locked) ? OVMSService.Command.Unlock : OVMSService.Command.Lock );

        private string ValetModeImage          =>  (CarData.env_valetmode) ? "ms-appx:///Assets/Misc/carvaleton.png" : "ms-appx:///Assets/Misc/carvaletoff.png";
        private string ValetModeStatusText     =>  (CarData.env_valetmode) ? "ON"      : "off";
        private string ValetModeButtonText     =>  (CarData.env_valetmode) ? "Disable" : "Enable";
        private bool   ValetModeButtonVisible  =>  OVMSService.Instance.IsCommandSupported( (CarData.env_valetmode) ? OVMSService.Command.ValetDisable : OVMSService.Command.ValetEnable );



        public DoorsPage()
        {
            this.InitializeComponent();
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            App.RootViewModel.PageTitle = "Doors";
        }


        private async void OnLockUnlock(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            string              title   = CarData.env_locked ? "Unlock Car"                   : "Lock Car";
            string              button  = CarData.env_locked ? "Unlock"                       : "Lock";
            OVMSService.Command cmd     = CarData.env_locked ?  OVMSService.Command.Unlock :  OVMSService.Command.Lock;

            // Sanity check: does the currently selected car support this command?
            if (!OVMSService.Instance.IsCommandSupported(cmd))
                return;
            
            AskPinDialog dlg = new AskPinDialog(title, button);

            if (await dlg.ShowAsync() == ContentDialogResult.Primary)
            {
                await OVMSService.Instance.TransmitCommandAsync( cmd, dlg.Pin );
            }
        }

        
        private async void OnValetModeEnable(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            string              title   = CarData.env_valetmode ? "Disable Valet Mode"                 : "Enable Valet Mode";
            string              button  = CarData.env_valetmode ? "Disable"                            : "Enable";
            OVMSService.Command cmd     = CarData.env_valetmode ?  OVMSService.Command.ValetDisable :  OVMSService.Command.ValetEnable;

            // Sanity check: does the currently selected car support this command?
            if (!OVMSService.Instance.IsCommandSupported(cmd))
                return;
            
            AskPinDialog dlg = new AskPinDialog(title, button);

            if (await dlg.ShowAsync() == ContentDialogResult.Primary)
            {
                await OVMSService.Instance.TransmitCommandAsync( cmd, dlg.Pin );
            }
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



