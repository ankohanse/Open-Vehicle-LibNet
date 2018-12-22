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
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using OpenVehicle.LibNet.Entities;
using OpenVehicle.LibNet.Helpers;

#if OPENVEHICLE_LIBNET_LOG
using OpenVehicle.LibNet.Logging;
#elif OPENVEHICLE_LIBNET_NLOG 
using NLog;
#endif

namespace OpenVehicle.LibNet
{
    public class OVMSConnection : IDisposable
    {
#region constants

        private const int OVMS_TOKEN_SIZE    = 22;

#endregion constants


#region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _Disconnect();
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

        public bool Connected
        {
            get { return m_socket != null && m_socket.Connected; }
        }

        #endregion properties


        #region members
#if OPENVEHICLE_LIBNET_LOG
        // For logging from the library
        // LibLog is compatible with NLog, Log4Net, SeriLog, Loupe in calling application
        private static readonly ILog    logger              = OpenVehicle.LibNet.Logging.LogProvider.For<OVMSConnection>();
#elif OPENVEHICLE_LIBNET_NLOG 
        private static NLog.Logger      logger              = NLog.LogManager.GetCurrentClassLogger();
#else 
        private static readonly ILog    logger              = null;
#endif 

        // Our communication settings
        private CarSettings     m_carSettings   = null;

        // Socket communications
        private TcpClient       m_socket        = null;
        private NetworkStream   m_socketStream  = null;
        private StreamReader    m_socketReader  = null;
        private BinaryWriter    m_socketWriter  = null;

        // RC4 encryption
        private CryptoRC4       m_rxCrypt       = null;
        private CryptoRC4       m_txCrypt       = null;

        #endregion members


        #region Connect / Disconnect

        public static async Task<OVMSConnection> ConnectAsync(CarSettings carSettings)
        {
            OVMSConnection conn = new OVMSConnection
            {
                m_carSettings = carSettings
            };

            return await conn._ConnectAsync();
        }


        private OVMSConnection()
        {
        }


        private  async Task<OVMSConnection> _ConnectAsync()
        {
            // When switching to a new selected car, disconnect from previous settings
            if (m_socket != null)
            {
                await DisconnectAsync();
            }

            // Clear any previous encryption
            m_rxCrypt = null;
            m_txCrypt = null;

            m_socket = new TcpClient(AddressFamily.InterNetworkV6);
            m_socket.Client.DualMode = true;
            await m_socket.ConnectAsync(m_carSettings.ovms_server, m_carSettings.ovms_port);
            
            if (!m_socket.Connected)
                return null;

            m_socketStream = m_socket.GetStream();
            m_socketReader = new StreamReader(m_socketStream, Encoding.UTF8);
            m_socketWriter = new BinaryWriter(m_socketStream, Encoding.UTF8, true);

            // Make a (semi-)random client token
            char[]  cb64            = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/".ToCharArray();
            Random  random          = new Random();
            string  clientTokenStr  = "";

            for (int k = 0; k < OVMS_TOKEN_SIZE; k++)
            {
                clientTokenStr += cb64[ random.Next() % 64 ];
            }

            byte[] clientTokenData  = Encoding.UTF8.GetBytes(clientTokenStr);
            byte[] key              = Encoding.UTF8.GetBytes(m_carSettings.server_pwd);
            HMACMD5 hmac            = new HMACMD5(key);
            hmac.Initialize();

            byte[]  clientDigestData = hmac.ComputeHash(clientTokenData);
            string  clientDigestStr  = Convert.ToBase64String(clientDigestData);

            // Send the welcome string
            await TransmitLineAsync( $"MP-A 0 {clientTokenStr} {clientDigestStr} {m_carSettings.vehicle_id}" );

            // Receive the welcome response
            string  response = await ReceiveLineAsync();
            if (response == null)
            {
                logger.Warn("*** No server welcome response received");
                return null;
            }

            string[] parts = response.Split(' ');
            if (parts.Length < 4)
            {
                logger.Warn("*** Server welcome response too short");
                return null;
            }

            string serverTokenStr  = parts[2];
            string serverDigestStr = parts[3];

            if (string.IsNullOrEmpty(serverTokenStr) || string.IsNullOrEmpty(serverDigestStr))
            {
                logger.Warn("*** Server welcome response contains no token");
                return null;
            }

            // Check for token-replay attach
            if (serverTokenStr == clientTokenStr)
            {
                logger.Warn("*** Server welcome response token replay attack");
                return null;
            }

            // Validate server token
            byte[]  testTokenData  = Encoding.UTF8.GetBytes(serverTokenStr);
            byte[]  testDigestData = hmac.ComputeHash(testTokenData);
            string  testDigestStr  = Convert.ToBase64String(testDigestData);

            if (testDigestStr != serverDigestStr)
            {
                logger.Warn("*** Server welcome response token is invalid");
                return null;
            }

            // Ok, at this point, our token is ok
            // Setup and prime the RX and TX cryptos
            string cryptTokenStr   = serverTokenStr + clientTokenStr;
            byte[] cryptTokenData  = Encoding.UTF8.GetBytes(cryptTokenStr);
            byte[] cryptKey        = hmac.ComputeHash(cryptTokenData);

            string primeStr = "".PadRight(1024, '0');

            m_rxCrypt = new CryptoRC4(cryptKey);
            m_rxCrypt.Crypt( Encoding.UTF8.GetBytes(primeStr) );

            m_txCrypt = new CryptoRC4(cryptKey);
            m_txCrypt.Crypt( Encoding.UTF8.GetBytes(primeStr) );

            return this;
        }


        public async Task DisconnectAsync()
        {
            _Disconnect();
            
            await Task.Delay(0);        // To keep the compiler happy
        }


        private void _Disconnect()
        {
            if (m_socketReader != null)
            {
                m_socketReader.Dispose();
                m_socketReader = null;
            }
            if (m_socketWriter != null)
            {
                m_socketWriter.Dispose();
                m_socketWriter = null;
            }
            if (m_socketStream != null)
            {
                m_socketStream.Dispose();
                m_socketStream = null;
            }
            if (m_socket != null)
            {
                m_socket.Dispose();
                m_socket = null;
            }
        }

#endregion Connect / Disconnect

        
#region Transmit / Receive on line-by-line level
        
        public async Task TransmitLineAsync(string msg)
        {
            logger.Trace( $"TX: {msg}" );

            if (m_socketWriter == null)
                return;

            if (m_txCrypt != null)
            {
                byte[] buf      = Encoding.UTF8.GetBytes(msg);
                byte[] bufCrypt = m_txCrypt.Crypt(buf);

                msg = Convert.ToBase64String(bufCrypt);
            }

            // Tried StreamWriter instead of BinaryWriter, but could not get it going...
            byte[] bin = Encoding.UTF8.GetBytes( msg + "\r\n" );

            m_socketWriter.Write(bin);
            m_socketWriter.Flush();

            await Task.Delay(0);        // To keep the compiler happy
        }
        

        public async Task<string> ReceiveLineAsync()
        {
            if (m_socketReader == null)
                return null;

            string msg = await m_socketReader.ReadLineAsync();
            if (msg == null)
                return null;

            if (m_rxCrypt != null)
            {
                byte[] bufCrypt = Convert.FromBase64String(msg);
                byte[] buf      = m_rxCrypt.Crypt(bufCrypt);

                msg = Encoding.UTF8.GetString(buf);
            }

            logger.Trace( $"RX: {msg}" );
            return msg;
        }


#endregion Send / Receive on line by line level

    }
}
