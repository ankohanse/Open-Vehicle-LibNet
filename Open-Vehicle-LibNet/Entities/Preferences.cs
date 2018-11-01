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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace OpenVehicle.LibNet.Entities
{
    // Note: we use Fody to inject INotifyPropertyChanged code into all property setters
    //
    public class Preferences : INotifyPropertyChanged
    {

        #region constants

        public enum UnitTemperature
        {
            Celcius,
            Fahrenheit
        }

        #endregion constants


        #region Properties

        // Whether or not to place a space between amount and unit
        public bool             UnitUseSpace        { get; set; }
        public string           UnitSpacer          { get { return (UnitUseSpace) ? " " : ""; }}

        // Unit to be used for temperature
        public UnitTemperature  UnitForTemperature  { get; set; }

        #endregion Properties


        #region singleton

        private Preferences()
        {
            UnitUseSpace       = true;
            UnitForTemperature = UnitTemperature.Fahrenheit;
        }


        public static Preferences Instance
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

            internal static readonly Preferences instance = new Preferences();
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
