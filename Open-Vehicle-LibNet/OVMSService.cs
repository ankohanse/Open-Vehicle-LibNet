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
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenVehicle.LibNet.Entities;
using OpenVehicle.LibNet.Helpers;
using OpenVehicle.LibNet.Logging;

namespace OpenVehicle.LibNet
{
    public class OVMSService : IDisposable
    {
        #region singleton

        private OVMSService()
        {
        }

        public static OVMSService Instance
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

            internal static readonly OVMSService instance = new OVMSService();
        }

        #endregion singleton


        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (m_ovmsConnection != null)
                        m_ovmsConnection.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~OVMSService() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support


        #region properties

        // Our config settings and resulting car-data
        public CarData      CarData             { get; private set; } = new CarData();
        public CarSettings  SelectedCarSettings { get; private set; } = null;

        #endregion properties

        #region members

        // For logging from the library
        // Compatible with NLog, Log4Net, SeriLog, Loupe in calling application
        private static readonly ILog logger = LogProvider.For<OVMSService>();

        // The connection to the server
        private OVMSConnection  m_ovmsConnection    = null;

        // Main loop
        private bool            m_loopIsRunning     = false;
        private Task            m_loopTask          = null;

        // Periodic ping to keep connection to server alive
        private Timer           m_pingTimer         = null;

        // Paranoid Mode encryption key
        private byte[]          m_pmKey             = null;

        #endregion members


        #region Start / Stop

        public async Task StartAsync(CarSettings settings)
        {
            // If needed, stop service for previous car selection
            if (m_loopIsRunning ||
                m_loopTask != null)
            {
                await StopAsync();
            }

            logger.Info("Starting OVMS connection");

            SelectedCarSettings = settings;

            // Start main loop
            m_loopIsRunning = true;
            m_loopTask = _MainLoop();        // do not await!

            // To keep the compiler happy
            await Task.Delay(0);    
        }


        public async Task StopAsync()
        {
            logger.Info("Stopping OVMS connection");

            // signal main loop to stop
            m_loopIsRunning = false;

            // Trigger an ping to get the waiting m_ovmsConnection.ReceiveLine() to return
            await _TransmitPing();

            // wait till loop is done
            await m_loopTask.ConfigureAwait(false);
            m_loopTask = null;

            logger.Info("Stopped OVMS connection");
        }

        #endregion Start / Stop


        #region Main loop

        private async Task _MainLoop()
        {
            while (m_loopIsRunning)
            {
                try
                {
                    if (m_ovmsConnection != null)
                    {
                        await m_ovmsConnection.DisconnectAsync();
                        m_ovmsConnection = null;
                    }

                    CarData.ProcessResetServer();

                    // Connect to the OVMS server
                    await PublishProgressAsync(ProgressType.ConnectBegin, SelectedCarSettings.ovmsServer);

                    m_ovmsConnection = await OVMSConnection.ConnectAsync(SelectedCarSettings);
            
                    if (m_ovmsConnection == null || !m_ovmsConnection.Connected)
                    {
                        await PublishProgressAsync(ProgressType.Error, "Could not connect to the OVMS server");
                        return;
                    }
                    await PublishProgressAsync(ProgressType.ConnectComplete, SelectedCarSettings.ovmsServer);

                    // Start ping timer
                    StartPing();

                    // main RX loop:
                    while (m_loopIsRunning  && m_ovmsConnection != null && m_ovmsConnection.Connected)
                    {
                        OVMSMessage msg = await ReceiveMessageAsync();
                        if (msg != null)
                        {
                            if (await ProcessMessageAsync(msg))
                            {
                                await PublishProgressAsync(ProgressType.Update, msg);
                            }
                        }
                        else
                        {
                            // Timeout waiting for data or communication error
                        }
                    }
                }
                catch (Exception ex)
                {
                    await PublishProgressAsync(ProgressType.Error, ex.Message);
                }

                // Stop and cleanup connection
                CarData.ProcessResetServer();

                // Stop ping timer
                StopPing();
                
                if (m_ovmsConnection != null)
                    await m_ovmsConnection.DisconnectAsync();

                await PublishProgressAsync(ProgressType.Disconnect, SelectedCarSettings.ovmsServer);
            }
        }

        #endregion Main Loop


        #region Ping

        private const int   PING_PERIOD     = 5*60*1000;        // 5 minutes


        private void StartPing()
        {
            m_pingTimer = new Timer(_OnPing, null, PING_PERIOD, PING_PERIOD );
        }


        private void StopPing()
        {
            m_pingTimer.Change(0, 0);
            m_pingTimer.Dispose();
        }


        private async void _OnPing(object state)
        {
            await _TransmitPing();
        }
     

        private async Task _TransmitPing()
        {
            logger.Debug("Send Ping");
            await TransmitMessageAsync( new OVMSMessage('A') );
        }

        #endregion Ping


        #region Transmit / Receive on message level

        public async Task TransmitMessageAsync(OVMSMessage cmd)
        {
            string msg = string.Format("MP-0 {0}{1}", cmd.Code, string.Join(",", cmd.Params) );

            await m_ovmsConnection.TransmitLineAsync(msg);
        }


        public async Task<OVMSMessage> ReceiveMessageAsync()
        {
            string msg = await m_ovmsConnection.ReceiveLineAsync();
            if (msg == null)
                return null;

            if (msg.Length < 6 || !msg.StartsWith("MP-0 ") )
            {
                logger.WarnFormat("*** Unknown protection scheme: {0}", msg.Substring(0,5) );
                return null;
            }

            char   msgCode = msg[5];
            string msgData = msg.Substring(6);

            if (msgCode == 'E')
            {
                // We have a paranoid mode message
                char pmCode = msgData[0];

                if (pmCode == 'T')
                {
                    // Set the paranoid token
                    try
                    {
                        string  pmToken = msgData.Substring(1);
                        byte[]  key     = Encoding.UTF8.GetBytes(SelectedCarSettings.selServerPwd);
                        HMACMD5 hmac    = new HMACMD5(key);
                        hmac.Initialize();

                        m_pmKey = hmac.ComputeHash( Encoding.UTF8.GetBytes(pmToken) );

                        logger.Debug("Paranoid Mode token accepted. Entering privacy mode.");
                    }
                    catch (Exception ex)
                    {
                        logger.ErrorException("Error accepting the paranoid token. ", ex);
                    }
                }
                else if (pmCode == 'M')
                {
                    // Decrypt the paranoid message
				    msgCode = msgData[1];
                    msgData = msgData.Substring(2);

                    try
                    {
                        // Setup and prime the Paranoid Mode crypto
                        string primeStr = "".PadRight(1024, '0');

                        CryptoRC4 pmCrypt  = new CryptoRC4(m_pmKey);
                        pmCrypt.Crypt( Encoding.UTF8.GetBytes(primeStr) );

                        // Now decrypt the Paranoid Mode message
                        byte[] binCrypt = Convert.FromBase64String(msgData);
                        byte[] binData  = pmCrypt.Crypt( binCrypt );

                        msgData = Encoding.UTF8.GetString(binData);
                    }
                    catch (Exception ex)
                    {
                        logger.ErrorException("Error decrypting the paranoid message. ", ex);
                    }
                }

                // Register in our carSettings that we are now in Paranoid Mode
                if (!CarData.server_paranoid)
                {
                    logger.Debug("Paranoid Mode Detected");
                    CarData.server_paranoid = true;
                }
            }

            logger.TraceFormat("{0} MSG Received: {1}", msgCode, msgData);

            return new OVMSMessage(msgCode, msgData.Split( new char[] { ',' } ) );
        }

        #endregion Transmit / Receive on message level


        #region Process messages

        private async Task<bool> ProcessMessageAsync(OVMSMessage msg)
        {
            try
            {
                switch (msg.Code)
                {
                    //
                    // OVMS PROTOCOL
                    //

                    case 'f':       // OVMS Server version
                        CarData.ProcessMsgServer(msg);
                        break;

                    case 'Z':       // Number of connected cars
                        CarData.ProcessMsgConnectedCars(msg);
                        break;

                    case 'T':       // Timestamp of last update
                        CarData.ProcessMsgTimestamp(msg);
                        break;

                    case 'a':       // Ping
                        logger.Debug("Server acknowledged ping");
                        break;

                    case 'c':       // Command response
                        logger.Debug("Command response received");

                        // Nothing to do here , the main loop will do a PublishProgress(ProgresType.Command, msg) for this
                        break;

                    case 'P':       // Push notification
                        logger.Debug("Push notification received");
                        
                        // Nothing to do here , the main loop will do a PublishProgress(ProgresType.Push, msg) for this
                        break;


                    //
                    // CAR VERSION AND CAPABILITIES
			        //

		            case 'F':       // CAR VIN and Firmware version
                        CarData.ProcessMsgFirmware(msg);
     			        break;

		            case 'V':       // CAR firmware capabilities
                        CarData.ProcessMsgCapabilities(msg);
                        break;


                    //
                    // STANDARD CAR MODEL DATA
                    //

                    case 'S':       // Status
                        CarData.ProcessMsgStatus(msg);
                        break;

                    case 'L':       // Location
                        CarData.ProcessMsgLocation(msg);
                        break;

                    case 'D':       // Doors / Switches & environment
                        CarData.ProcessMsgDoors(msg);
                        break;

                    case 'W':       // Tire Pressure
                        CarData.ProcessMsgTirePressure(msg);
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.ErrorException("{0} MSG Invalid. ", ex, msg.Code);
                await PublishProgressAsync(ProgressType.Error, string.Format("{0} MSG Invalid.", msg.Code));

                return false;
            }
        }


        #endregion Process commands


        #region OnProgress events

        public enum ProgressType
        {
            ConnectBegin,
            ConnectComplete,
            Disconnect,
            Update,
            Command,
            Push,
            Error
        }

        public delegate void OnProgressHandler(ProgressType pt, string msg);
        public delegate Task OnProgressHandlerAsync(ProgressType pt, string msg);

        public event  OnProgressHandler         OnProgress;         
        public event  OnProgressHandlerAsync    OnProgressAsync;   


        private async Task PublishProgressAsync(ProgressType pt, OVMSMessage msg)
        {
            string str = "";

            switch (msg.Code)
            {
                case 'c':
                    {
                        pt  = ProgressType.Command;
                        str = string.Join(",", msg.Params);
                    }
                    break;

                case 'P':
                    {
                        pt = ProgressType.Push;
                        str = string.Join(",", msg.Params);
                    }
                    break;

                default:
                    {
                        str = msg.Code + string.Join(",", msg.Params);
                    }
                    break;
            }

            await PublishProgressAsync(pt, str);
        }

        private async Task PublishProgressAsync(ProgressType pt, string msg)
        {
            try
            {
                if (OnProgress != null)
                {
                    OnProgress.Invoke(pt, msg);
                }
                if (OnProgressAsync != null)
                {
                    await OnProgressAsync.Invoke(pt, msg);
                }
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error in PublishProgress callback. ", ex);
            }
            await Task.Delay(0);
        }

        #endregion OnProgress events


    }
}
