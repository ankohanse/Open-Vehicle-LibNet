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
using System.Diagnostics;
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

        // Our main instance
        public static OVMSService Instance
        {
            get { return Nested.instance; }
        }

        // Allow to create extra / parallel instances next to the main instance
        // This is usefull for background threads
        public static OVMSService CreateExtraInstance()
        {
            return new OVMSService();
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
        public CarSettings  SelectedCarSettings { get; private set; } = null;

        // Car Data received from the server
        public CarData      CarData             { get; private set; } = new CarData();

        #endregion properties


        #region members

#if OPENVEHICLE_LIBNET_LOG
        // For logging from the library
        // LibLog is compatible with NLog, Log4Net, SeriLog, Loupe in calling application
        private static readonly ILog    logger              = OpenVehicle.LibNet.Logging.LogProvider.For<OVMSConnection>();
#else 
        private static readonly ILog    logger              = null;
#endif 

        // The connection to the server
        private OVMSConnection          m_ovmsConnection    = null;

        // Main loop
        private Task                    m_loopTask          = null;
        private volatile SemaphoreSlim  m_loopSemaphore     = new SemaphoreSlim(1);
        private volatile bool           m_loopIsRunning     = false;
        private volatile AutoResetEvent m_loopStopped       = new AutoResetEvent(false);

        // Periodic ping to keep connection to server alive
        private Timer                   m_pingTimer         = null;


        // Paranoid Mode encryption key
        private byte[]                  m_pmKey             = null;

#endregion members

        
#region Start / Stop

        public async Task StartAsync(CarSettings settings)
        {
            // Sanity check
            if (settings == null)
                return;

            // Do not allow a next start or stop before the previous one has completed
            // Otherwise we risk having multiple MainLoops
            await m_loopSemaphore.WaitAsync();
            try
            {
                // If needed, stop service for previous car selection
                if (m_loopIsRunning)
                    await _StopAsync();

                logger.Info("Starting OVMS connection");

                SelectedCarSettings = settings;

                // Start main loop
                m_loopIsRunning = true;
                m_loopTask      = _MainLoop();        // Do not await _MainLoop here! We await it during the StopAsync instead...
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error in OVMSService.StartAsync. ", ex);
            }        
            finally
            {
                m_loopSemaphore.Release();
            }
        }


        // Do not allow a next start or stop before the previous one has completed
        // Otherwise we risk having multiple MainLoops
        public async Task StopAsync()
        {
            await m_loopSemaphore.WaitAsync();
            try
            {
                await _StopAsync();
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error in OVMSService.StopAsync. ", ex);
            }        
            finally
            {
                m_loopSemaphore.Release();
            }
        }


        private async Task _StopAsync()
        {
            logger.Info("Stopping OVMS connection");

            // signal main loop to stop
            m_loopIsRunning = false;

            // Trigger a final ping to get the waiting m_ovmsConnection.ReceiveLine() to return
            await _TransmitPing();

            // wait till loop is done
            if (m_loopTask != null)
            {
                await m_loopTask;
                m_loopTask = null;

                //AJH m_loopStopped.WaitOne();
            }

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
                    await PublishProgressAsync(ProgressType.ConnectBegin, SelectedCarSettings.ovms_server);

                    m_ovmsConnection = await OVMSConnection.ConnectAsync(SelectedCarSettings);
            
                    if (m_ovmsConnection == null || !m_ovmsConnection.Connected)
                    {
                        await PublishProgressAsync(ProgressType.Error, "Could not connect to the OVMS server");
                        return;
                    }
                    await PublishProgressAsync(ProgressType.ConnectComplete, SelectedCarSettings.ovms_server);

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
                {
                    await m_ovmsConnection.DisconnectAsync();
                    m_ovmsConnection = null;
                }

                await PublishProgressAsync(ProgressType.Disconnect, SelectedCarSettings.ovms_server);
            }

            // Signal the main loop has now stopped
            //AJH m_loopStopped.Set();
        }

#endregion Main Loop


#region Ping

        private const int   PING_PERIOD     = 5*60*1000;        // 5 minutes


        private void StartPing()
        {
            if (m_pingTimer == null)
                m_pingTimer = new Timer(_OnPing, null, PING_PERIOD, PING_PERIOD);
            else
                m_pingTimer.Change(PING_PERIOD, PING_PERIOD);
        }


        private void StopPing()
        {
            m_pingTimer.Change(Timeout.Infinite, Timeout.Infinite);
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


#region Transmit on command level

        public enum Command : byte
        {
            GetFeatureList          = 1,
            SetFeature              = 2,    // with param <feature_number>,<value>
            GetParameterList        = 3,    
            SetParameter            = 4,    // with param <param_number>,<value>
            Reboot                  = 5,
            Shell                   = 7,    // with param <cmd>
            SetChargeMode           = 10,   // with param <mode>
            ChargeStart             = 11,
            ChargeStop              = 12,
            SetChargeCurrentLimit   = 15,   // with param <current> as current limit in Amps
            SetChargeModeCurrent    = 16,   // with param <mode>,<current> 
            WakeUpCar               = 18,
            WakeUpClimateSubSystem  = 19,
            Lock                    = 20,   // with param <pin>
            ValetEnable             = 21,   // with param <pin>
            Unlock                  = 22,   // with param <pin>
            ValetDisable            = 23,   // with param <pin>
            HomeLink                = 24,   // with param 0, 1, 2 or without param for default
            Aircon                  = 26,   // with param 0=turn off, 1=turn on
            CellularUsage           = 30,
            MMI_USSD                = 41,   // with param <cmd>
            Modem                   = 49,   // with param <cmd>
            SetChargeAlerts         = 204   // with param <suffRange>,<suffSOC>,<chgPower>,<chgMode>
        }


        public bool IsCommandSupported(Command cmdCode)
        {
            return CarData.command_support[ (int)cmdCode ];
        }


        public async Task TransmitCommandAsync(Command cmdCode, string cmdText = "")
        {
            OVMSMessage msg;

            if (string.IsNullOrEmpty(cmdText))
                msg = new OVMSMessage('C', new string[] { cmdCode.ToString() } );
            else
                msg = new OVMSMessage('C', new string[] { cmdCode.ToString(), cmdText } );

            await TransmitMessageAsync(msg);
        }

#endregion Transmit on command level


#region Transmit / Receive on message level

        private async Task TransmitMessageAsync(OVMSMessage msg)
        {
            if (m_ovmsConnection == null || !m_ovmsConnection.Connected)
                return;

            char   code  = msg.Code;
            string parms = string.Join(",", msg.Params);

            await m_ovmsConnection.TransmitLineAsync( $"MP-0 {code}{parms}" );
        }


        private async Task<OVMSMessage> ReceiveMessageAsync()
        {
            if (m_ovmsConnection == null || !m_ovmsConnection.Connected)
                return null;

            string line = await m_ovmsConnection.ReceiveLineAsync();
            if (line == null)
                return null;

            if (line.Length < 6 || !line.StartsWith("MP-0 ") )
            {
                logger.WarnFormat("*** Unknown protection scheme: {0}", line.Substring(0,5) );
                return null;
            }

            char   msgCode = line[5];
            string msgData = line.Substring(6);

            if (msgCode == 'E')
            {
                // We have a paranoid mode message
                char pmCode = (msgData.Length > 0) ? msgData[0] : '\0';

                if (pmCode == 'T')
                {
                    // Set the paranoid token
                    try
                    {
                        string  pmToken = msgData.Substring(1);
                        byte[]  key     = Encoding.UTF8.GetBytes(SelectedCarSettings.server_pwd);
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
				    msgCode = (msgData.Length > 1) ? msgData[1] : '\0';
                    msgData = (msgData.Length > 1) ? msgData.Substring(2) : "";

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
            string[] msgParams = (msgData != null) ? msgData?.Split( new char[] { ',' } ) : new string[0];

            return new OVMSMessage(msgCode,  msgParams);
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
