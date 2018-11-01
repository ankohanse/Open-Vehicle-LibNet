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
using System.Runtime.CompilerServices;

namespace OpenVehicle.LibNet.Entities
{
    // Note: we use Fody to inject INotifyPropertyChanged code into all property setters
    //
    public class CarSettings : INotifyPropertyChanged
    {

        #region properties
       
        #pragma warning disable IDE1006 // Naming Styles
        public string ovmsServer        { get; set; }
        public int    ovmsPort          { get; set; }

        public string selVehicleId      { get; set; }
        public string selVehicleLabel   { get; set; }
        public string selVehicleImage   { get; set; }
        public string selServerPwd      { get; set; }
        public string selModulePwd      { get; set; }

        #endregion properties


        #region construction

        public CarSettings ()
        {
            // Initialize with defaults
            ovmsServer          = "tmc.openvehicles.com";
            ovmsPort            = 6867;
            
            selVehicleId        = "DEMO";
            selVehicleLabel     = "Demonstration Car";
            selServerPwd        = "DEMO";
        }

        #endregion construction

        
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
