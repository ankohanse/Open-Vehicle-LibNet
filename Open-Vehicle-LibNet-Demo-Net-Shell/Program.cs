/*
;    Project:       Open-Vehicle-LibNet
;    Date:          November 2018
;
;    Changes:
;    1.0  Initial release
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
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenVehicle.LibNet;
using OpenVehicle.LibNet.Entities;

namespace OpenVehicle.Demo
{
    class Program
    {
        // Logging
        private static NLog.Logger    logger           = NLog.LogManager.GetCurrentClassLogger();

        // Events
        private static AutoResetEvent glb_startedEvent = new AutoResetEvent(false);
        private static AutoResetEvent glb_stopEvent    = new AutoResetEvent(false);
        private static AutoResetEvent glb_inputEvent   = new AutoResetEvent(false);



        static async Task Main(string[] args)
        {
            logger.Info("------------------------------------");
            logger.Info("Open-Vehicle-Demo-Net-Shell starting");

            // Attach our Progress Handler
            OVMSService.Instance.OnProgress += OnProgress;

            // If needed, prompt to enter config values
            string ovmsServer      = ConfigurationManager.AppSettings["ovmsServer"];
            string ovmsPort        = ConfigurationManager.AppSettings["ovmsPort"];
            string selVehicleId    = ConfigurationManager.AppSettings["selVehicleId"];
            string selVehicleLabel = ConfigurationManager.AppSettings["selVehicleLabel"];
            string selServerPwd    = ConfigurationManager.AppSettings["selServerPwd"];

            while (string.IsNullOrWhiteSpace(selVehicleId))
            {
                Console.WriteLine();
                Console.WriteLine("Please enter your OVMS Vehicle Id.");
                selVehicleId = Console.ReadLine();
            }

            while (string.IsNullOrWhiteSpace(selServerPwd))
            {
                Console.WriteLine();
                Console.WriteLine("Please enter your OVMS Server Password.");
                selServerPwd = Console.ReadLine();
            }

            // Start the OVMSService
            Console.WriteLine();
            Console.WriteLine("Connecting to OVMS Server {0} for vehicle {1}", ovmsServer, selVehicleId);

            CarSettings settings = new CarSettings()
            {
                ovms_server         = ovmsServer,
                ovms_port           = int.Parse(ovmsPort),
            
                vehicle_id          = selVehicleId,
                vehicle_label       = selVehicleLabel,
                server_pwd          = selServerPwd,
            };

            await OVMSService.Instance.StartAsync(settings);
            glb_startedEvent.WaitOne();

            // Show brief help on commands
            Console.WriteLine();
            Console.WriteLine("Enter your OVMS Shell command.");
            Console.WriteLine("Examples:");
            Console.WriteLine("    stat");
            Console.WriteLine("    config list");
            Console.WriteLine("    config list vehicle");
            Console.WriteLine("    metrics list         (can be slow to respond)");
            Console.WriteLine("    exit");
            Console.WriteLine("    ?");
            Console.WriteLine();
            
            // Process input until the user signals to stop
            while (WaitHandle.WaitAny( new WaitHandle[] { glb_stopEvent, glb_inputEvent } ) != 0)
            {
                Console.Write("$OVMS ");
                string str = Console.ReadLine();

                if (str == "exit")
                {
                    glb_stopEvent.Set();
                }
                else
                {
                    // Trace
                    logger.Debug("Command: {0}", str);

                    // Send the command to the OVMS server
                    if (str.StartsWith("*"))
                    {
                        // MMI/USSD command
                        await OVMSService.Instance.TransmitCommandAsync( OVMSService.Command.MMI_USSD, str);
                    }
                    else if (str.StartsWith("@"))
                    {
                        // Modem command
                        await OVMSService.Instance.TransmitCommandAsync( OVMSService.Command.Modem, str.Substring(1) );
                    }
                    else
                    {
                        // SMS command
                        await OVMSService.Instance.TransmitCommandAsync( OVMSService.Command.Shell, str);
                    }
                }
            }

            // Stop the OVMSService
            Console.WriteLine();
            Console.WriteLine("Exit");

            await OVMSService.Instance.StopAsync();

            logger.Info("Open-Vehicle-Demo-Net-Shell stopped");
        }


        private static void OnProgress(OVMSService.ProgressType pt, string msg)
        {
            if (pt == OVMSService.ProgressType.ConnectComplete)
            {
                Console.WriteLine("Connected");

                // Signal started and to read the first user input line
                glb_startedEvent.Set();
                glb_inputEvent.Set();
            }
            else if (pt == OVMSService.ProgressType.Command)
            {
                string[] values = msg.Split(new char[] { ','} );

                if (values.Length > 2 && values[0] == "7")
                {
                    string   resCode  = values[1];
                    string[] resLines = values[2].Split(new char[] { '\r' } );

                    switch (resCode)
                    {
                        case "0":   // ok
                            break;

                        case "1":   // failed
                            Console.WriteLine("Failed");
                            break;

                        case "2":   // unsupported
                            Console.WriteLine("Not supported");
                            break;

                        case "3":   // unimplemented
                            Console.WriteLine("Not implemented");
                            break;

                        default:    // error
                            Console.WriteLine("Error");
                            break;
                    }
                    foreach (string line in resLines)
                    {
                        // Show response on screen
                        Console.WriteLine(line);

                        // Also trace the response lines
                        logger.Debug(line);
                    }
                }

                // Signal to read the next user input line 
                glb_inputEvent.Set();
            }
            else if (pt == OVMSService.ProgressType.Error)
            {
                Console.WriteLine("ERROR:{0}", msg);
                logger.Error(msg);
            }
        }

    }
}
