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

using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using OpenVehicle.LibNet;
using OpenVehicle.LibNet.Entities;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml;

namespace OpenVehicle.Demo
{
    // Note: we use Fody to inject INotifyPropertyChanged code into all property setters
    //
    public class MainViewModel : INotifyPropertyChanged
    {

        // Logging
        private static NLog.Logger      logger           = NLog.LogManager.GetCurrentClassLogger();

        // Car Settings and Data
        public CarSettings              CarSettings     { get; private set; }  = new CarSettings();
        public CarData                  CarData         { get; private set; }  = OVMSService.Instance.CarData;

        // App preferences
        public OVMSPreferences              Preferences     { get; private set; }  = OVMSPreferences.Instance;

        // Connect status of the OVMSService
        public string                   ConnectStatus   { get; private set; }  = "Disconnected";


        public string AppVersion
        {
            get
            {
                var assembly = typeof(App).GetTypeInfo().Assembly;
                var assemblyVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
                return assemblyVersion.ToString();
            }
        }


        public MainViewModel()
        {
            // Set CarSettings from App.config
            CarSettings = new CarSettings()
            {
                ovms_server      = AppSettings.GetSetting("ovmsServer"),
                ovms_port        = int.Parse(AppSettings.GetSetting("ovmsPort")),                 
                vehicle_id    = AppSettings.GetSetting("selVehicleId"),
                vehicle_label = AppSettings.GetSetting("selVehicleLabel"),
                server_pwd    = AppSettings.GetSetting("selServerPwd"),
            };

            // Link our Progress Handler
            OVMSService.Instance.OnProgress += OnProgress;
        }


        public async Task StartAsync()
        {
            await OVMSService.Instance.StartAsync(CarSettings);
        }


        public async Task StopAsync()
        {
            await OVMSService.Instance.StopAsync();
        }


        private void OnProgress(OVMSService.ProgressType pt, string msg)
        {
            switch (pt)
            {
                case OVMSService.ProgressType.ConnectBegin:
                    ConnectStatus = "Connecting...";
                    break;

                case OVMSService.ProgressType.ConnectComplete:
                    ConnectStatus = "Connected";
                    break;

                case OVMSService.ProgressType.Disconnect:
                    ConnectStatus = "Disconnected";
                    break;

                case OVMSService.ProgressType.Update:
                    // OnCarDataChanged();
                    break;

                case OVMSService.ProgressType.Command:
                case OVMSService.ProgressType.Push:
                    break;

                case OVMSService.ProgressType.Error:
                    logger.Error(msg);
                    break;
            }
        }


        public void OnPreferencesChanged()
        {
            // A change in preferences requires all CarData properties to be renewed
            this.CarData.OnPropertyChanged(null);
        }


        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // Raise the PropertyChanged event, passing the name of the property whose value has changed.
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged

    }
}
