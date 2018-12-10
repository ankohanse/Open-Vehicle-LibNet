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
using OpenVehicle.App.Entities;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace OpenVehicle.App
{
    public enum SettingsForVehicleDialogMode
    {
        Add, 
        EditNoDelete, 
        EditWithDelete
    }


    public enum SettingsForVehicleDialogResult
    {
        Save, 
        Delete, 
        Cancel
    }


    public sealed partial class SettingsForVehicleDialog : ContentDialog
    {
        //
        // Properties for x:Bind
        //
        private AppCarSettings                  m_CarSettings       = null;
        private Dictionary<string,string>       m_CarImages         = App.RootViewModel.AppCarImages; 


        // Property to return which button was pressed.
        // The standard method does not seem to work after an inner popup confirmation button was displayed.
        public SettingsForVehicleDialogResult   Result              { get; private set; } = SettingsForVehicleDialogResult.Cancel;             


        public SettingsForVehicleDialog(AppCarSettings carSettings, SettingsForVehicleDialogMode mode)
        {
            this.InitializeComponent();

            m_CarSettings = carSettings;
            
            this.Title              = (mode == SettingsForVehicleDialogMode.Add) ? "Add vehicle" : "Edit vehicle";
            this.PrimaryButtonText  = "Save";
            this.PrimaryButtonClick += ContentDialog_Save_ButtonClick;

            if (mode == SettingsForVehicleDialogMode.EditWithDelete)
            {
                this.SecondaryButtonText  = "Delete";
                this.SecondaryButtonClick += ContentDialog_Delete_ButtonClick;
            }
            this.CloseButtonText = "Cancel";
        }


        private void ContentDialog_Save_ButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;

            // Check all required properties are filled
            if ( CheckNonEmpty(txtVehicleID)  &&
                 CheckNonEmpty(txtServerPwd)  &&
                 CheckNonEmpty(txtModulePwd)  &&
                 CheckNonEmpty(txtOvmsServer) &&
                 CheckNonEmpty(txtOvmsPort) )
            {
                // All OK
                args.Cancel = false;
                Result = SettingsForVehicleDialogResult.Save;
            }
        }


        private async void ContentDialog_Delete_ButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Set Cancel so the ContentDialog does not yet close.
            // We will close it once response from the MessageDialog is received
            args.Cancel = true;

            MessageDialog dlg = new MessageDialog("Delete this car?");
            dlg.Commands.Add(new UICommand { Label = "No",  Id = 0 });
            dlg.Commands.Add(new UICommand { Label = "Yes", Id = 1 });
            dlg.DefaultCommandIndex = 0;
            dlg.CancelCommandIndex  = 0;

            // For some reason, MessageDialog does NOT await the ShowAsync
            IUICommand res = await dlg.ShowAsync();

            if ((int)res.Id == 1)
            {
                // User answered "Yes"
                args.Cancel = false;
                Result = SettingsForVehicleDialogResult.Delete;
                this.Hide();
            }
        }


        private bool CheckNonEmpty(TextBox control)
        {
            if (string.IsNullOrWhiteSpace(control.Text))
            {
                ShowFlyout(control, "Please enter a value");
                return false;
            }
            return true;
        }

        private bool CheckNonEmpty(PasswordBox control)
        {
            if (string.IsNullOrWhiteSpace(control.Password))
            {
                ShowFlyout(control, "Please enter a value");
                return false;
            }
            return true;
        }


        private void ShowFlyout(FrameworkElement placementTarget, string msg)
        {
            Flyout flyout = new Flyout()
            {
                Content = new TextBlock() { Text = msg }
            };

            flyout.ShowAt(placementTarget);
        }
    }
}
