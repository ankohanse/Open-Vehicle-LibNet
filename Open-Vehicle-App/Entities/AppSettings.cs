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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using OpenVehicle.App.Helpers;
using OpenVehicle.LibNet.Entities;
using Windows.UI.Notifications;

namespace OpenVehicle.App.Entities
{

    // Note: we use Fody to inject INotifyPropertyChanged code into all property setters
    //
    public class AppSettings : INotifyPropertyChanged
    {
        #region constants

        // The key names of our settings
        const string        KEY_NAME_CAR_SETTINGS_LIST              = "CarSettingsList";
        const string        KEY_NAME_CAR_SETTINGS_SEL_INDEX         = "CarSettingsSelectedIndex";

        const string        KEY_NAME_LIVETILE_LASTUPDATE            = "LiveTileLastUpdate";
        const string        KEY_NAME_UNIT_TEMPERATURE               = "UnitTemperature";

        // The default value of our settings
        readonly int        KEY_DEFAULT_CAR_SETTINGS_SEL_INDEX      = 0;

        readonly DateTime   KEY_DEFAULT_LIVETILE_LASTUPDATE         = DateTime.MinValue;
        readonly string     KEY_DEFAULT_UNIT_TEMPERATURE            = "C";

        #endregion constants


        #region members

        #endregion members


        #region Properties

        /// <summary>
        /// Return as ReadOnly collection to prevent Add or Remove of items without saving into settings
        /// </summary>
        public AppCarSettingsCollection CarSettingsList
        {
            get
            {
                return new AppCarSettingsCollection( SettingsHelper.GetSettingClass<AppCarSettingsList>( KEY_NAME_CAR_SETTINGS_LIST, MakeDefaultCarSettingsList() ) );
            }
        }


        public bool CarSettingsListContainsMultiple
        {
            get { return CarSettingsList.Count > 1; }
        }


        public void CarSettingsListAdd(AppCarSettings car)
        {
            AppCarSettingsList lst = SettingsHelper.GetSettingClass<AppCarSettingsList>( KEY_NAME_CAR_SETTINGS_LIST, MakeDefaultCarSettingsList() );
            lst.Add(car);
            SettingsHelper.SetSettingClass<AppCarSettingsList>( KEY_NAME_CAR_SETTINGS_LIST, lst);

            OnPropertyChanged("CarSettingsList");
            OnPropertyChanged("CarSettingsListContainsMultiple");
        }
        
        public void CarSettingsListSet(int idx, AppCarSettings car)
        {
            AppCarSettingsList lst = SettingsHelper.GetSettingClass<AppCarSettingsList>( KEY_NAME_CAR_SETTINGS_LIST, MakeDefaultCarSettingsList() );
            lst[idx] = car;
            SettingsHelper.SetSettingClass<AppCarSettingsList>( KEY_NAME_CAR_SETTINGS_LIST, lst);

            OnPropertyChanged("CarSettingsList");
            OnPropertyChanged("CarSettingsListContainsMultiple");

            if (idx == CarSettingsSelIndex)
            {
                UpdateCarSettings();
            }        
        }
        
        public void CarSettingsListDel(int idx)
        {
            AppCarSettingsList lst = SettingsHelper.GetSettingClass<AppCarSettingsList>( KEY_NAME_CAR_SETTINGS_LIST, MakeDefaultCarSettingsList() );
            lst.RemoveAt(idx);
            SettingsHelper.SetSettingClass<AppCarSettingsList>( KEY_NAME_CAR_SETTINGS_LIST, lst);

            OnPropertyChanged("CarSettingsList");
            OnPropertyChanged("CarSettingsListContainsMultiple");
        
            if (idx == CarSettingsSelIndex)
            {
                UpdateCarSettings();
            }        
        }

        
        /// <summary>
        /// The currently selected index in the CarSettingsList
        /// </summary>
        public int CarSettingsSelIndex
        {
            get
            {
                AppCarSettingsList lst = SettingsHelper.GetSettingClass<AppCarSettingsList>( KEY_NAME_CAR_SETTINGS_LIST, MakeDefaultCarSettingsList() );
                int                sel = SettingsHelper.GetSetting<int>(KEY_NAME_CAR_SETTINGS_SEL_INDEX, KEY_DEFAULT_CAR_SETTINGS_SEL_INDEX);

                return (sel < lst.Count) ? sel : 0; 
            }
            set
            {
                SettingsHelper.SetSetting<int>(KEY_NAME_CAR_SETTINGS_SEL_INDEX, value);

                UpdateCarSettings();
            }
        }


        /// <summary>
        /// We do not switch from one AppSettings object to another when the selected car changes because that would confuse the x:Bind
        /// Instead, we let the x:Bind point to one object instance and change the contents of that object
        /// </summary>
        public void UpdateCarSettings()
        {
            // Make sure to only have one thread at a time trigger an update, to prevent a race condition
            AppCarSettingsCollection lst = CarSettingsList;
            int                      sel = CarSettingsSelIndex;

            if (sel >= 0 && sel < lst.Count)
            {
                AppCarSettings.Instance.CloneFrom( lst[sel] );
            }
        }


        public string UnitTemperature
        {
            get { return SettingsHelper.GetSetting<string>(KEY_NAME_UNIT_TEMPERATURE, KEY_DEFAULT_UNIT_TEMPERATURE); }
            set
            {
                SettingsHelper.SetSetting<string>(KEY_NAME_UNIT_TEMPERATURE, value);

                OVMSPreferences.UnitTemperature newTemp = (value=="F") ? OVMSPreferences.UnitTemperature.Fahrenheit : OVMSPreferences.UnitTemperature.Celcius;
                OVMSPreferences.UnitTemperature oldTemp = OVMSPreferences.Instance.UnitForTemperature;
                if (newTemp != oldTemp)
                {
                    OVMSPreferences.Instance.UnitForTemperature = newTemp;

                    // A change in temperature unit implies all displayed CarData needs a refresh
                    App.RootViewModel.CarData.OnPropertyChanged(null);
                }
            }
        }


        public bool LiveTileEnabled
        {
            get
            {
                TileUpdater updater = TileUpdateManager.CreateTileUpdaterForApplication();
                return (updater.Setting == NotificationSetting.Enabled) ? true : false;
            }
        }


        public DateTime LiveTileLastUpdate
        {
            get { return SettingsHelper.GetSetting<DateTime>(KEY_NAME_LIVETILE_LASTUPDATE, KEY_DEFAULT_LIVETILE_LASTUPDATE); }
            set { SettingsHelper.SetSetting<DateTime>(KEY_NAME_LIVETILE_LASTUPDATE, value); }
        }

        #endregion Properties


        #region Complex default property values

        private AppCarSettingsList MakeDefaultCarSettingsList()
        {
            return new AppCarSettingsList() { AppCarSettings.Default };
        }

        #endregion Complex default property values


        #region singleton

        private AppSettings()
        {
            // UpdateCarSettings();
        }


        public static AppSettings Instance
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

            internal static readonly AppSettings instance = new AppSettings();
        }

        #endregion singleton


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
