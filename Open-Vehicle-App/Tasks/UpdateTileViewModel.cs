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

using OpenVehicle.LibNet.Entities;
using System;

namespace OpenVehicle.App.Tasks
{

    public class UpdateTileViewModel
    {
        private CarSettings  _carSettings = null;
        private CarData      _carData     = null;


        public UpdateTileViewModel(CarSettings carSettings, CarData carData)
        {
            _carSettings = carSettings;
            _carData     = carData;
        }


        #region Properties
        #pragma warning disable IDE1006 // Naming Styles

        public string vehicle_id        =>  _carSettings.vehicle_id;
        public string vehicle_label     =>  _carSettings.vehicle_label;

        public string bat_soc           => _carData.bat_soc;

        public string bat_soc_img
        {
            get
            {
                int soc_mod4  = (int)Math.Round(_carData.bat_soc_raw / 4.0f) * 4;

                return $"ms-appx:///Assets/Misc/battery_{soc_mod4:000}.png";
            }
        }

        public string bat_range_estim_and_ideal
        {
            get 
            {
                if (_carData.bat_range_estimated_raw > 0.0f && _carData.bat_range_ideal_raw > 0.0f)
                {
                    return $"{_carData.bat_range_estimated_raw:0} - {_carData.bat_range_ideal_raw:0} {_carData.unit_distance}";
                }
                else if (_carData.bat_range_estimated_raw > 0.0f)
                {
                    return $"{_carData.bat_range_estimated_raw:0} {_carData.unit_distance}";
                }
                else if (_carData.bat_range_ideal_raw > 0.0f)
                {
                    return $"{_carData.bat_range_ideal_raw:0} {_carData.unit_distance}";
                }
                return "";
            }
        }

        public string bat_range_estim_or_ideal
        {
            get 
            {
                if (_carData.bat_range_estimated_raw > 0.0f)
                {
                    return $"{_carData.bat_range_estimated_raw:0} {_carData.unit_distance}";
                }
                else if (_carData.bat_range_ideal_raw > 0.0f)
                {
                    return $"{_carData.bat_range_ideal_raw:0} {_carData.unit_distance}";
                }
                return "";
            }
        }

        #pragma warning restore IDE1006 // Naming Styles
        #endregion Properties

    }
}
