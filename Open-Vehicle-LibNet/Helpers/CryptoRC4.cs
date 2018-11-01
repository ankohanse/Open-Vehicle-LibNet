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
using System.Linq;
using System.Text;


namespace OpenVehicle.LibNet.Helpers
{
    public class CryptoRC4
    {
        private byte[] m = new byte[256];
        private int    x = 0;
        private int    y = 0;


        public CryptoRC4(byte[] key)
        {
            for (int i = 0; i < 256; i++)
                m[i] = (byte)i;

            int j = 0;
            int k = 0;
            for (int i = 0; i < 256; i++)
            {
                j = (j + m[i] + key[k]) & 255;
                Swap(i,j);

                if (++k >= key.Length)
                    k = 0;
            }
        }
    

        public byte[] Crypt(byte[] data)
        {
            byte[] result = new byte[data.Length];

            for (int i = 0; i < data.Length; i++)
            { 
                x = (x + 1) & 255;
                y = (y + m[x]) & 255;
                Swap(x,y);

                int t = (m[x] + m[y]) & 255;
                result[i] = (byte)(data[i] ^ m[t]);
            }
            return result;
        }


        private void Swap(int i, int j)
        {
           byte c = m[i];

           m[i] = m[j];
           m[j] = c;
        }
    }
}