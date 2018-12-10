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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using OpenVehicle.LibNet.Entities;

namespace OpenVehicle.App.Entities
{
    // The bare minimum settings from OpenVehicle.LibNet, plus some extra's
    //
    public sealed class AppCarSettings : CarSettings
    {
        #region constants

        public static AppCarSettings  Default
        {
            get
            {
                return new AppCarSettings()
                {
                    vehicle_id    = "DEMO",
                    vehicle_label = "Demo vehicle",
                    vehicle_image = "car__default.png",
                    server_pwd    = "DEMO",
                    module_pwd    = "DEMO"
                };
            }
        }

        public static AppCarSettings  Empty
        {
            get
            {
                return new AppCarSettings()
                {
                    vehicle_id    = "",
                    vehicle_label = "",
                    vehicle_image = "car__default.png",
                    server_pwd    = "",
                    module_pwd    = ""
                };
            }
        }

        #endregion constants


        #region Properties
        #pragma warning disable IDE1006 // Naming Styles

        public string vehicle_displayname
        {
            get { return (!string.IsNullOrWhiteSpace(vehicle_label)) ? vehicle_label : vehicle_id; }
        }

        public string vehicle_image_resourcename
        {
            get
            {
                if (!App.RootViewModel.AppCarImages.TryGetValue(vehicle_image, out string result))
                {
                    result = App.RootViewModel.AppCarImages.First().Value;
                }
                return result;
            }
        }

        public string vehicle_mapimage_resourcename
        {
            get
            {
                if (!App.RootViewModel.AppCarMapImages.TryGetValue(vehicle_image, out string result))
                {
                    result = App.RootViewModel.AppCarMapImages.First().Value;
                }
                return result;
            }
        }

        public string vehicle_outline_resourcename
        {
            get
            {
                if (!App.RootViewModel.AppCarOutlines.TryGetValue(vehicle_image, out string result))
                {
                    result = App.RootViewModel.AppCarOutlines.First().Value;
                }
                return result;
            }
        }

#pragma warning restore IDE1006 // Naming Styles
#endregion Properties



#region singleton

        private AppCarSettings()
        {
        }


        public static AppCarSettings Instance
        {
            get { return Nested.instance; }
        }


        private class Nested
        {
            // Explicit static constructor to tell C# compiler
            // not to mark type as beforefieldinit
            static Nested()
            {
            }

            internal static readonly AppCarSettings instance = new AppCarSettings();
        }

#endregion singleton


#region Cloning

        public void CloneFrom(AppCarSettings src)
        {
            base.CloneFrom( (CarSettings)src );

            // Copy any new properties here too
        }

#endregion Cloning


#region INotifyPropertyChanged

        public override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            // Add handling for extra properties defined in this class
            if (propertyName == "vehicle_id" || propertyName == "vehicle_label")
            {
                base.OnPropertyChanged("vehicle_displayname");
            }
            else if (propertyName == "vehicle_image")
            {
                base.OnPropertyChanged("vehicle_image_resourcename");
            }
                
        }

#endregion INotifyPropertyChanged
    }


    public sealed class AppCarSettingsList : List<AppCarSettings>
    {
    }

    public sealed class AppCarSettingsCollection : ReadOnlyCollection<AppCarSettings>
    {
        public AppCarSettingsCollection(IList<AppCarSettings> lst)
            : base(lst)
        {
        }
    }

    
}
