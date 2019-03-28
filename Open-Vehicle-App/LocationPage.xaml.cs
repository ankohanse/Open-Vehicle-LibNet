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
using OpenVehicle.LibNet.Entities;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace OpenVehicle.App
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public partial class LocationPage : Page, INotifyPropertyChanged
    {

        // For use in x:Bind
        private CarData         CarData             => App.RootViewModel.CarData;
        private AppCarSettings  CarSettings         => App.RootViewModel.CarSettings;


        private Visibility VisibleIfParked(int parkedtime)
        {
            if (parkedtime > 0)
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }
       

        private Visibility VisibleIfNotParked(int parkedtime)
        {
            if (parkedtime > 0)
                return Visibility.Collapsed;
            else
                return Visibility.Visible;
        }
       

        private Geopoint Geopoint(double latitude, double longitude, double altitude = 0.0)
        {
            return new Geopoint(
                new BasicGeoposition()
                {
                    Latitude  = latitude,
                    Longitude = longitude,
                    Altitude  = altitude
                }
            );
        }


        private string HeadingFromDirection(double value)
        {
            double dir = (double)value % 360.0;

            if      (dir <   0.0) return "unknown";
            else if (dir <  25.0) return "north";
            else if (dir <  65.0) return "north-east";
            else if (dir < 115.0) return "east";
            else if (dir < 155.0) return "south-east";
            else if (dir < 205.0) return "south";
            else if (dir < 245.0) return "south-west";
            else if (dir < 295.0) return "west";
            else if (dir < 335.0) return "north-west";
            else if (dir < 360.0) return "north";
            else                  return "unknown";
        }


        private string DurationFromSeconds(int seconds)
        {
            int minutes = (int)Math.Floor(seconds / 60.0);
            int hours   = (int)Math.Floor(seconds / 60.0 / 60.0);

            if      (hours >= 2)   return string.Format("{0:0} hours", hours);
            else if (minutes > 45) return string.Format("{0:0} minutes", (int)(Math.Floor(minutes/15.0)*15.0) );
            else if (minutes > 10) return string.Format("{0:0} minutes", (int)(Math.Floor(minutes/5.0)*5.0) );
            else if (minutes > 1)  return string.Format("{0:0} minutes", minutes);
            else                   return "";
        }


        public LocationPage()
        {
            this.InitializeComponent();
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            App.RootViewModel.PageTitle = "Location";
        }


        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        

        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // Raise the PropertyChanged event, passing the name of the property whose value has changed.
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged    
        
    }
}
