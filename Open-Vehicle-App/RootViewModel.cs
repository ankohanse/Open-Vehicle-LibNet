/*
;    Project:       Open-Vehicle-App
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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using OpenVehicle.App.Entities;
using OpenVehicle.LibNet;
using OpenVehicle.LibNet.Entities;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace OpenVehicle.App
{
    // Note: we use Fody to inject INotifyPropertyChanged code into all property setters
    //
    public class RootViewModel : INotifyPropertyChanged
    {
        #region Properties

        // Logging
        private static readonly NLog.Logger logger              = NLog.LogManager.GetCurrentClassLogger();

        // App Car Images collections
        public Dictionary<string,string>    AppCarImages        { get; private set; }  = new Dictionary<string,string>();
        public Dictionary<string,string>    AppCarMapImages     { get; private set; }  = new Dictionary<string,string>();
        public Dictionary<string,string>    AppCarOutlines      { get; private set; }  = new Dictionary<string,string>();

        public string                       PrefixCarImages     { get; private set; }  = "OpenVehicle.App.Assets.CarImages.";
        public string                       PrefixCarMapImages  { get; private set; }  = "OpenVehicle.App.Assets.CarMapImages.map_";
        public string                       PrefixCarOutlines   { get; private set; }  = "OpenVehicle.App.Assets.CarOutlines.ol_";

        // All app Settings
        public AppSettings                  AppSettings         { get; private set; }  = AppSettings.Instance;

        // Selected car within App Settings
        public AppCarSettings               CarSettings         { get; private set; }  = AppCarSettings.Instance;

        // Live Car Data
        public CarData                      CarData             { get; private set; }  = OVMSService.Instance.CarData;

        // Connect status of the OVMSService
        public string                       ConnectStatus       { get; private set; }  = "Disconnected";

        #endregion Properties


        #region Header Template

        // Title of current page
        public string                       PageTitle           { get; set;         } = " ";


        // Live / Lastupdate indication
        public string GetConnectionText(DateTime dtLastUpdated )
        {
            if (ConnectStatus != "Connected")
            {
                return ConnectStatus;
            }
            else if (dtLastUpdated != null)
            {
                long seconds = (long)Math.Round(DateTime.Now.Subtract(dtLastUpdated).TotalSeconds);
                long minutes = seconds / 60;
                long hours   = minutes / 60;
                long days    = hours   / 24;

                if      (days > 5)     return "";
                if      (days > 1)     return $"{days:0} days";
                else if (hours > 1)    return $"{hours:0} hours";
                else if (minutes > 1)  return $"{minutes:0} mins";
                else if (minutes == 1) return $"{minutes:0} min";
                else                   return "live";
            }
            return "";
        }


        public ImageSource GetConnectionImage(DateTime dtLastUpdated, bool bParanoid)
        {
            string url = "ms-appx://Assets/Misc/connection_unknown.png";

            if (dtLastUpdated != null)
            {
                long seconds = (long)Math.Round(DateTime.Now.Subtract(CarData.server_lastupdated).TotalSeconds);
                long minutes = seconds / 60;
                long hours   = minutes / 60;
                long days    = hours   / 24;

                if (minutes > 10)
                    url = (CarData.server_paranoid) ? "ms-appx:///Assets/Misc/connection_bad_paranoid.png"  : "ms-appx:///Assets/Misc/connection_bad.png";
                else
                    url = (CarData.server_paranoid) ? "ms-appx:///Assets/Misc/connection_good_paranoid.png" : "ms-appx:///Assets/Misc/connection_good.png";
            }
            return new BitmapImage( new Uri(url) );
        }


        public ImageSource GetGsmBarsImage(int? bars)
        {
            string url;

            switch (bars)
            {
                default:
                case 0: url = "ms-appx:///Assets/Misc/signal_strength_0.png"; break;
                case 1: url = "ms-appx:///Assets/Misc/signal_strength_1.png"; break;
                case 2: url = "ms-appx:///Assets/Misc/signal_strength_2.png"; break;
                case 3: url = "ms-appx:///Assets/Misc/signal_strength_3.png"; break;
                case 4: url = "ms-appx:///Assets/Misc/signal_strength_4.png"; break;
                case 5: url = "ms-appx:///Assets/Misc/signal_strength_5.png"; break;
            }
            return new BitmapImage( new Uri(url) );
        }

        #endregion Header Template



        public RootViewModel()
        {
            // Link our Progress Handler
            OVMSService.Instance.OnProgress += OnProgress;

            //Retrieve all available car images from the Assets
            DetectAssets();
        }


        public async Task StartAsync()
        {
            await RestartAsync();
        }


        /// <summary>
        /// Called in case the selected car changes.
        /// </summary>
        /// <returns></returns>
        public async Task RestartAsync()
        {
            // Propagate selected car from AppSettings into CarSettings
            AppSettings.UpdateCarSettings();

            // Propagate other AppSettings used by the OVMSService
            OVMSPreferences.Instance.UnitForTemperature = (AppSettings.UnitTemperature=="F") ? OVMSPreferences.UnitTemperature.Fahrenheit : OVMSPreferences.UnitTemperature.Celcius;

            // No need to explicitly call StopAsync, the StartAsync will do it for us when needed...
            await OVMSService.Instance.StartAsync(CarSettings);

            // A change in selected car requires the current CarSettings properties to be renewed
            CarSettings.OnPropertyChanged(null);

            // A change in selected car requires all CarData properties to be renewed
            CarData.OnPropertyChanged(null);
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
                    break;

                case OVMSService.ProgressType.Command:
                    ShowCommandResult(msg);
                    break;

                case OVMSService.ProgressType.Push:
                    App.RootPage.ShowPopup("Push", msg);
                    break;

                case OVMSService.ProgressType.Error:
                    logger.Error(msg);
                    App.RootPage.ShowPopup("Error", msg);
                    break;
            }
        }


        private void ShowCommandResult(string msg)
        {
            if (msg == null)
                return;

            string[] parts = msg.Split(',');
            if (parts.Length < 2)
                return;

            string   cmd     = parts[0];
            string   resCode = parts[1];
            string   resText = (parts.Length > 2) ? parts[2] : "";

            switch (resCode)
            {
                case "0":  // ok
                    App.RootPage.ShowPopup("Command result", "Success.");
                    break;

                case "1":  // failed
                    App.RootPage.ShowPopup("Command result", "Failed.\n" + resText);
                    break;

                case "2":  // unsupported
                    App.RootPage.ShowPopup("Command result", "Not supported.\n" + resText);
                    break;

                case "3":  // unimplemented
                    App.RootPage.ShowPopup("Command result", "Not implemented.\n" + resText);
                    break;

                default:   // error
                    App.RootPage.ShowPopup("Command result", "Error.\n" + resText);
                    break;
            }
        }


        private void DetectAssets()
        {
            string[] lstResourceNames = App.Current.GetType().GetTypeInfo().Assembly.GetManifestResourceNames();

            AppCarImages    = lstResourceNames
                              .Where(   r => r.StartsWith(PrefixCarImages) )
                              .OrderBy( r => r )
                              .ToDictionary(r => r.Substring(PrefixCarImages.Length), r => r );

            AppCarMapImages = lstResourceNames
                              .Where(   r => r.StartsWith(PrefixCarMapImages) )
                              .OrderBy( r => r )
                              .ToDictionary(r => r.Substring(PrefixCarMapImages.Length), r => r );

            AppCarOutlines  = lstResourceNames
                              .Where(   r => r.StartsWith(PrefixCarOutlines) )
                              .OrderBy( r => r )
                              .ToDictionary(r => r.Substring(PrefixCarOutlines.Length), r => r );
        }


        private ImageSource GetManifestResourceImage(string sManifestResourceName)
        {
            using (Stream imgStream = App.Current.GetType().GetTypeInfo().Assembly.GetManifestResourceStream(sManifestResourceName))
            using (var memStream = new MemoryStream())
            {
                imgStream.CopyTo(memStream);
                memStream.Position = 0;

                using (var raStream = memStream.AsRandomAccessStream())
                {
                    BitmapImage img = new BitmapImage();                    
                    img.SetSource(raStream);

                    return img;
                }
            }
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
