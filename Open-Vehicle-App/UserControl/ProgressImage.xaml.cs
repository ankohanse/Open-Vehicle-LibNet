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
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using System.Threading.Tasks;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace OpenVehicle.App.UserControl
{
    /// <summary>
    /// Draw a progress image from two images.
    /// </summary>
    /// <remarks>
    /// Slightly rough version using a static background image and a scaling foreground image.
    /// Depending on what the images look like this may work fine or have some side-effects for low scaling numbers.
    /// </remarks>
    public partial class ProgressImage  : INotifyPropertyChanged
    {

        #region Constructors

        /// <summary>
        /// Construct a ProgressImage instance
        /// </summary>
        public ProgressImage()
        {
            InitializeComponent();

            BackgroundImage = null;
            SliderImage     = null;
            Min             = 0;
            Max             = 100;
            Value           = 100;
        }

        #endregion Constructors

        
        #region Properties

        public string BackgroundImage
        {
            get => (string)GetValue(BackgroundImageProperty);
            set
            {
                SetValue(BackgroundImageProperty, value);
            }
        }

        public static readonly DependencyProperty BackgroundImageProperty = DependencyProperty.Register( "BackgroundImage", 
                                                                                                         typeof(string), 
                                                                                                         typeof(ProgressImage), 
                                                                                                         new PropertyMetadata(default(string)) );     

        public string SliderImage
        {
            get => (string)GetValue(SliderImageProperty);
            set
            {
                SetValue(SliderImageProperty, value);
            }
        }

        public static readonly DependencyProperty SliderImageProperty = DependencyProperty.Register( "SliderImage", 
                                                                                                     typeof(string), 
                                                                                                     typeof(ProgressImage), 
                                                                                                     new PropertyMetadata(default(string)) );     


        public double Min
        {
            get => (double)GetValue(MinProperty);
            set
            {
                SetValue(MinProperty, value);
                UpdateSlider();
            }
        }

        public static readonly DependencyProperty MinProperty = DependencyProperty.Register( "Min", 
                                                                                             typeof(double), 
                                                                                             typeof(ProgressImage), 
                                                                                             new PropertyMetadata(default(double)) );

        public double Max
        {
            get => (double)GetValue(MaxProperty);
            set
            {
                SetValue(MaxProperty, value);
                UpdateSlider();
            }
        }

        public static readonly DependencyProperty MaxProperty = DependencyProperty.Register( "Max", 
                                                                                             typeof(double), 
                                                                                             typeof(ProgressImage), 
                                                                                             new PropertyMetadata(default(double)) );

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set
            {
                SetValue(ValueProperty, value);
                UpdateSlider();
            }

        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register( "Value", 
                                                                                               typeof(double), 
                                                                                               typeof(ProgressImage), 
                                                                                               new PropertyMetadata(default(double)) );

        public double SliderAdjust
        {
            get => (double)GetValue(SliderMarginProperty);
            set
            {
                SetValue(SliderMarginProperty, value);
                UpdateSlider();
            }

        }

        public static readonly DependencyProperty SliderMarginProperty = DependencyProperty.Register( "SliderMargin", 
                                                                                                     typeof(double), 
                                                                                                     typeof(ProgressImage), 
                                                                                                     new PropertyMetadata(default(double)) );

        #endregion Properties


        #region Derived Properties

        private string      ValuePercent     => $"{Value:0} %";

        private double      SliderWidth      { get; set; }
        private Thickness   SliderMargin     { get; set; }


        private void UpdateSlider()
        {
            double ratio = (Max-Min > 0.0) 
                                ? Value / (Max - Min) 
                                : 0.0;

            // Take into account that the leftmost and rightmost part of the slider image are actually
            // transparent parts and not part of the slider...
            // As the slider ratio gets smaller we need to shift the slider more to the right to compensate
            double sliderShift  = (1.0 - ratio) * SliderAdjust;
            SliderMargin = new Thickness(sliderShift, 0, 0, 0);

            SliderWidth  = ratio * (Width - sliderShift);

            OnPropertyChanged("SliderWidth");
        }

        #endregion Derived Properties


        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateSlider();
        }


        #region INotifiedProperty Block

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        #endregion INotifiedProperty Block
    }
}
