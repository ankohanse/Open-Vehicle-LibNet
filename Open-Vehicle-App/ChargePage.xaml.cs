﻿/*
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

using System;
using OpenVehicle.App.Entities;
using OpenVehicle.LibNet;
using OpenVehicle.LibNet.Entities;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace OpenVehicle.App
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ChargePage : Page
    {

        // General
        // For use in x:Bind
        private CarData         CarData             => App.RootViewModel.CarData;
        private AppCarSettings  CarSettings         => App.RootViewModel.CarSettings;
        

        private bool   ChargeStartButtonVisible     => OVMSService.Instance.IsCommandSupported( OVMSService.Command.ChargeStart );
        private bool   ChargeStopButtonVisible      => OVMSService.Instance.IsCommandSupported( OVMSService.Command.ChargeStop );


        private string FormatMins(Int32 val)
        {
            return $"{val} mins";
        }

        private string FormatUntil(string val)
        {
            return $"until {val}";
        }


        public ChargePage()
        {
            this.InitializeComponent();
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            App.RootViewModel.PageTitle = "Charge";
        }


        private async void OnChargeStart(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // Sanity check: does the currently selected car support this command?
            if (!OVMSService.Instance.IsCommandSupported(OVMSService.Command.ChargeStart))
                return;

            // Send the command
            await OVMSService.Instance.TransmitCommandAsync( OVMSService.Command.ChargeStart );
        }


        private async void OnChargeStop(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // Sanity check: does the currently selected car support this command?
            if (!OVMSService.Instance.IsCommandSupported(OVMSService.Command.ChargeStop))
                return;

            // Send the command
            await OVMSService.Instance.TransmitCommandAsync( OVMSService.Command.ChargeStop );
        }



    }
}



