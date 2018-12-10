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
using OpenVehicle.App.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace OpenVehicle.App
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {

        // General
        // For use in x:Bind
        private AppSettings AppSettings         => App.RootViewModel.AppSettings;


        #region Properties for x:Bind

        // 
        // Preferences panel
        //
        private bool UnitTemperatureIsCelcius
        {
            get { return (AppSettings.UnitTemperature=="C"); }
            set { if (value) AppSettings.UnitTemperature="C"; }
        }

        private bool UnitTemperatureIsFahrenheit
        {
            get { return (AppSettings.UnitTemperature=="F"); }
            set { if (value) AppSettings.UnitTemperature="F"; }
        }


        //
        // About panel
        //
        private string AppTitle
        {
            get { return App.Title; }
        }

        private string AppInfo
        {
            get
            {
                StringBuilder str = new StringBuilder();
                str.AppendLine($"version {App.Version}" );
                str.AppendLine( "wwww.openvehicles.com" );
                return str.ToString();
            }
        }

        #endregion Properties for x:Bind


        #region Page Open/Close

        public SettingsPage()
        {
            this.InitializeComponent();
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            App.RootViewModel.PageTitle = "Settings";
        }

        #endregion Page Open/Close


        #region Handlers

        private async void OnVehicleAdd(object sender, RoutedEventArgs e)
        {
            // Remember current selection
            int selIndex = App.RootViewModel.AppSettings.CarSettingsSelIndex;

            // Add a new car definition
            AppCarSettings               newCar  = AppCarSettings.Empty;
            SettingsForVehicleDialogMode mode    = SettingsForVehicleDialogMode.Add;
            
            SettingsForVehicleDialog dlg = new SettingsForVehicleDialog(newCar, mode);
            await dlg.ShowAsync();

            if (dlg.Result == SettingsForVehicleDialogResult.Save)
            {
                App.RootViewModel.AppSettings.CarSettingsListAdd(newCar);
            }

            // Restore selection
            App.RootViewModel.AppSettings.CarSettingsSelIndex = selIndex;

        }


        private async void OnVehicleEdit(object sender, RoutedEventArgs e)
        {
            // Remember current selection
            int selIndex = App.RootViewModel.AppSettings.CarSettingsSelIndex;

            // Lookup for which car the button was pressed 
            // (can be different from current selected car)
            string tag = ((AppBarButton)sender).Tag.ToString();

            AppCarSettingsCollection lst       = App.RootViewModel.AppSettings.CarSettingsList;
            int                      idx       = ((IEnumerable<AppCarSettings>)lst).FirstIndexMatch(c => c.vehicle_id == tag);

            // Now edit the found car definition
            AppCarSettings               editCar  = lst[idx];
            SettingsForVehicleDialogMode mode     = (lst.Count > 1) ? SettingsForVehicleDialogMode.EditWithDelete : SettingsForVehicleDialogMode.EditNoDelete;

            SettingsForVehicleDialog dlg = new SettingsForVehicleDialog(editCar, mode);
            await dlg.ShowAsync();

            // Ignore the return value from the await: for some reason the Secondary button response never 
            // is returned after a confirmation dialog was displayed in its ButtonClicked handler.
            // Instead, we explicitly pass a Result property

            if (dlg.Result == SettingsForVehicleDialogResult.Save)
            {
                App.RootViewModel.AppSettings.CarSettingsListSet(idx, editCar);
            }
            else if (dlg.Result == SettingsForVehicleDialogResult.Delete)
            {
                App.RootViewModel.AppSettings.CarSettingsListDel(idx);
            }

            // Restore selection
            App.RootViewModel.AppSettings.CarSettingsSelIndex = selIndex;
        }


        private void OnVehiclesSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Prevent deselect of item when using ctrl-click
            if (e.AddedItems.Count == 0 && e.RemovedItems.Count > 0)
            {
                lstVehicles.SelectedItem = e.RemovedItems[0];
            }
        }

        #endregion Handlers
    }
}
