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
using OpenVehicle.LibNet;
using OpenVehicle.LibNet.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace OpenVehicle.App.Tasks
{
    public class UpdateTileTask : IBackgroundTask
    {
        public const string TASK_NAME               = "UpdateTileTask";

        public const int    TILES_CARS_MAX          = 5;         // Create tiles for max 5 cars

        public const int    UPDATE_PERIOD_MIN       =  5;        //  5 minutes
        public const int    UPDATE_PERIOD_DEFAULT   = 15;        // 15 minutes

        public static string ICON_PATH_SMALL        { get { return "ms-appx:///Assets/Logo/SmallTile.png"; } }
        public static string ICON_PATH_MEDIUM       { get { return "ms-appx:///Assets/Logo/Square150x150Logo.png"; } }
        public static string ICON_PATH_WIDE         { get { return "ms-appx:///Assets/Logo/Wide310x150Logo.png"; } }
        public static string ICON_PATH_LARGE        { get { return "ms-appx:///Assets/Logo/LargeTile.png"; } }

        public static int    TILE_WIDTH_SMALL       { get { return 71; } }         
        public static int    TILE_WIDTH_MEDIUM      { get { return 150; } }         
        public static int    TILE_WIDTH_WIDE        { get { return 310; } }       
        public static int    TILE_WIDTH_LARGE       { get { return 310; } }      

        public static int    TILE_HEIGHT_SMALL      { get { return 71; } }       
        public static int    TILE_HEIGHT_MEDIUM     { get { return 150; } }      
        public static int    TILE_HEIGHT_WIDE       { get { return 150; } }      
        public static int    TILE_HEIGHT_LARGE      { get { return 310; } }      

        private const string TEMPLATE_NAME_SMALL    = "TileSmall";
        private const string TEMPLATE_NAME_MEDIUM   = "TileMedium";
        private const string TEMPLATE_NAME_WIDE     = "TileWide";
        private const string TEMPLATE_NAME_LARGE    = "TileLarge";

        private const string TEMPLATE_TILE          = "<tile>"
                                                        + "<visual version='3'>"
                                                        + "{0}"
                                                        + "</visual>"
                                                        + "</tile>";

        private const string TEMPLATE_BINDING_NONE  = "<binding template='{0}' branding='none'  hint-overlay='0' >"
                                                        + "<image src='{1}' placement='background'/>"
                                                        + "</binding>";

        private const string TEMPLATE_BINDING_APP   = "<binding template='{0}' branding='name'  hint-overlay='0' >"
                                                        + "<image src='{1}' placement='background'/>"
                                                        + "</binding>";

        private const string TEMPLATE_BINDING_NAME   = "<binding template='{0}' branding='name' displayName='{2}'  hint-overlay='0' >"
                                                        + "<image src='{1}' placement='background'/>"
                                                        + "</binding>";

        // Logging
        private static NLog.Logger          logger              = NLog.LogManager.GetCurrentClassLogger();

        // Trigger 
        private static ApplicationTrigger   glb_appTrigger      = null;

        // Events
        private volatile AutoResetEvent     m_dataEvent         = new AutoResetEvent(false);
        private volatile ManualResetEvent   m_canceledEvent     = new ManualResetEvent(false);
        private volatile bool               m_bCanceled         = false;
        
        // The messages to wait for before we can generate the Tiles
        private readonly List<char>         m_requiredMsgCodes  = new List<char>(new char[] { 'S' });     // wait until we have: Status msg (S)
        private List<char>                  m_receivedMsgCodes  = new List<char>();


        public static async Task Trigger()
        {
            try
            {
                // Make sure we are registered
                await Register(false);

                // Now pull the trigger...
                await glb_appTrigger?.RequestAsync();
            }
            catch (Exception)
            {
            }
        }


        public static async Task Register(bool bForce)
        {
            var backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();

            if (backgroundAccessStatus == BackgroundAccessStatus.AlwaysAllowed ||
                backgroundAccessStatus == BackgroundAccessStatus.AllowedSubjectToSystemPolicy)
            {
                bool bIsRegistered = false;

                // Check for existing registrations of this background task.
                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name == TASK_NAME)
                    {
                        // The task is already registered.
                        bIsRegistered = true;

                        if (bForce)
                            task.Value.Unregister(true);
                    }
                }

                if (!bIsRegistered || bForce || glb_appTrigger == null)
                {
                    // Register the background task.
                    var taskBuilder = new BackgroundTaskBuilder
                    {
                        Name = TASK_NAME,
                        // Do not register an entrypoint. We want to handle this in-process inside this app
                    };
                    taskBuilder.SetTrigger( new TimeTrigger(UPDATE_PERIOD_DEFAULT, false) );
                    taskBuilder.Register();

                    // Also register via an ApplicationTrigger
                    glb_appTrigger = new ApplicationTrigger();
                    taskBuilder = new BackgroundTaskBuilder
                    {
                        Name = TASK_NAME,
                        // Do not register an entrypoint. We want to handle this in-process inside this app
                    };
                    taskBuilder.SetTrigger(glb_appTrigger);
                    taskBuilder.Register();

                    logger.Info(TASK_NAME + " registered");
                }
            }
        }


        /// <summary>
        /// Called from the generic app entry point OnBackgroundActivated
        /// Do not forget to add handling code there...
        /// </summary>
        /// <param name="taskInstance"></param>
        public async void Run(IBackgroundTaskInstance taskInstance)
        { 
            // Get a deferral, to prevent the task from closing prematurely
            // while asynchronous code is still running.
            var deferral = taskInstance.GetDeferral();

            taskInstance.Canceled += OnCanceled;
            try
            {
                await Task.Factory.StartNew( async () => await RunAsync(false) );
            }
            catch (Exception)
            {
            }
            finally
            {            
                // Inform the system that the task is finished.
               deferral.Complete();
            }
        }


        public async Task RunAsync(bool bForce)
        {
            logger.Info("------------------------------------");
            logger.Info(TASK_NAME + " starting");

            try
            {
                List<string> tileDefinitions = new List<string>();

                if (!AppSettings.Instance.LiveTileEnabled)
                {
                    logger.Info("Live tile not enabled");

                    // We are allowed to still update the live tiles in this situation, even though they are not displayed.
                    // But we chose not to do so to not needlesly use data and battery resources of the host computer.
                    return;
                }

                // Do we need to update?
                DateTime dtLastUpdate = AppSettings.Instance.LiveTileLastUpdate;
                DateTime dtNextUpdate = dtLastUpdate.AddMinutes(UPDATE_PERIOD_MIN);

                if (!bForce && DateTime.Now < dtNextUpdate)
                {
                    logger.Info("Live tile update not yet due");

                    return;
                }

                logger.Info("Live tile update");

                // Enumerate all defined cars (up to max of 6)
                for (int carIdx = 0; carIdx < TILES_CARS_MAX && carIdx < AppSettings.Instance.CarSettingsList.Count && !m_bCanceled; carIdx++)
                {
                    // Make an extra OVMS Service instance for this car
                    AppCarSettings carSettings = AppSettings.Instance.CarSettingsList[carIdx];

                    using (OVMSService ovmsService = OVMSService.CreateExtraInstance())
                    {
                        m_dataEvent.Reset();
                        m_receivedMsgCodes.Clear();

                        // Propagate other AppSettings used by the OVMSService
                        OVMSPreferences.Instance.UnitForTemperature = (AppSettings.Instance.UnitTemperature == "F") ? OVMSPreferences.UnitTemperature.Fahrenheit : OVMSPreferences.UnitTemperature.Celcius;

                        // Attach our Progress Handler
                        // and start the OVMSService
                        ovmsService.OnProgress += OnProgress;

                        await ovmsService.StartAsync(carSettings);

                        // Wait until service is started and data is available
                        int result = WaitHandle.WaitAny( new WaitHandle[] { m_dataEvent, m_canceledEvent } );
                        if (result == 0)
                        {
                            // Generate the live tiles for this car
                            string tileDefCar = GenerateTileDef_Live(carSettings, ovmsService.CarData);

                            tileDefinitions.Add(tileDefCar);
                        }
                        else if (result == 1)
                        {
                            // Cancelled
                            logger.Debug(TASK_NAME + " cancel requested");
                        }
                        else if (result == WaitHandle.WaitTimeout)
                        {
                            // Timeout
                            logger.Debug(TASK_NAME + " timeout");
                        }

                        // Stop the OVMSService
                        await ovmsService.StopAsync();
                    }
                }

                // Now update the live tile with the new definitions
                if (tileDefinitions.Count > 0 && !m_bCanceled)
                {
                    TileUpdater updater = TileUpdateManager.CreateTileUpdaterForApplication();
                    updater.Clear();
                    updater.EnableNotificationQueue(true);

                    foreach (string tileDef in tileDefinitions)
                    {
                        XmlDocument tileXml = new XmlDocument();
                        tileXml.LoadXml(tileDef);

                        updater.Update(new TileNotification(tileXml));
                    }
                }

            }
            catch (Exception ex)
            {
                logger.Debug(ex, "UpdateTileTask::RunAsync");
            }
            finally
            {
                logger.Info(TASK_NAME + " stopped");
            }
        }



        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            logger.Debug("Background " + sender.Task.Name + " Cancel Requested...");

            // Indicate that the background task is canceled.
            m_bCanceled = true;
            m_canceledEvent.Set();
        }


        private void OnProgress(OVMSService.ProgressType pt, string msg)
        {
            if (pt == OVMSService.ProgressType.ConnectBegin)
            {
                logger.Info(TASK_NAME + " Connecting");
            }
            if (pt == OVMSService.ProgressType.ConnectComplete)
            {
                logger.Info(TASK_NAME + " Connected");
            }
            if (pt == OVMSService.ProgressType.Disconnect)
            {
                logger.Info(TASK_NAME + " Disconnect");
            }
            else if (pt == OVMSService.ProgressType.Update)
            {
                string[] values = msg.Split(new char[] { ','} );

                char     msgCode   = (!string.IsNullOrEmpty(values.First())) ? values.First()[0] : '\0';
                string[] msgParams = values.Skip(1).ToArray();

                logger.Info( TASK_NAME + $" Received msg: '{msgCode}'");

                if (!m_receivedMsgCodes.Contains(msgCode))
                {
                    m_receivedMsgCodes.Add(msgCode);

                    // Did we receive every message we need?
                    if (m_requiredMsgCodes.All(code => m_receivedMsgCodes.Contains(code)))
                    {
                        // Signal data available
                        m_dataEvent.Set();
                    }
                }
            }
            else if (pt == OVMSService.ProgressType.Error)
            {
                logger.Error(msg);
            }
        }


        private string GenerateTileDef_Static()
        {
            // Define static tiles
            return string.Format( TEMPLATE_TILE,
                                  string.Format(TEMPLATE_BINDING_NONE, TEMPLATE_NAME_SMALL,  ICON_PATH_SMALL) +
                                  string.Format(TEMPLATE_BINDING_APP,  TEMPLATE_NAME_MEDIUM, ICON_PATH_MEDIUM) +
                                  string.Format(TEMPLATE_BINDING_APP,  TEMPLATE_NAME_WIDE,   ICON_PATH_WIDE) +
                                  string.Format(TEMPLATE_BINDING_APP,  TEMPLATE_NAME_LARGE,  ICON_PATH_LARGE) 
                                );
        }


        private string GenerateTileDef_Live(AppCarSettings carSettings, CarData carData)
        {
            // Get derived data used on various tiles
            UpdateTileViewModel tileData = new UpdateTileViewModel(carSettings, carData);

            // Generate tile definitions
            string sTileSmall  = _GenerateTileSmall(tileData);
            string sTileMedium = _GenerateTileMedium(tileData);
            string sTileWide   = _GenerateTileWide(tileData);
            string sTileLarge  = _GenerateTileLarge(tileData);

            return string.Format( TEMPLATE_TILE,
                                  sTileSmall +              
                                  sTileMedium +              
                                  sTileWide +
                                  sTileLarge
                                );
        }


        // https://blogs.msdn.microsoft.com/tiles_and_toasts/2015/06/30/adaptive-tile-templates-schema-and-documentation/

        private string _GenerateTileSmall(UpdateTileViewModel data)
        {
            StringBuilder strResult = new StringBuilder();
            try
            { 
                logger.Trace(TASK_NAME + " _GenerateTileSmall");

                string sCaption = ( AppSettings.Instance.CarSettingsList.Count > 1) ? data.vehicle_id : data.bat_range_estim_or_ideal;

                // Now generate the Tile Template
                strResult.AppendLine( "<binding template='" + TEMPLATE_NAME_SMALL + "' branding='none' hint-textStacking='bottom' hint-overlay='0' >" );
                strResult.AppendLine(     "<text hint-style='base' hint-align='center'>" + XmlEscape(data.bat_soc) + "</text>" );
                strResult.AppendLine(     "<text hint-style='captionSubtle' hint-align='center'>" + XmlEscape(sCaption) + "</text>" );
                strResult.AppendLine( "</binding>" );
            }
            catch (Exception e)
            {
                logger.Debug(e, "_GenerateTileSmall");
            }
            return strResult.ToString();
        }


        private string _GenerateTileMedium(UpdateTileViewModel data)
        {
            StringBuilder strResult = new StringBuilder();
            try
            { 
                logger.Trace(TASK_NAME + " _GenerateTileMedium");

                // Now generate the Tile Template
                strResult.AppendLine( "<binding template='" + TEMPLATE_NAME_MEDIUM + "' branding='name' displayName='" + XmlEscapeAttr(data.vehicle_label) + "' hint-textStacking='center' hint-overlay='0' >" );
                strResult.AppendLine(     "<text hint-style='subtitle' hint-align='center'>" + XmlEscape(data.bat_soc) + "</text>" );
                strResult.AppendLine(     "<image src='" + XmlEscape(data.bat_soc_img) + "' placement='inline' hint-removeMargin='true' />" );
                strResult.AppendLine(     "<text hint-style='baseSubtle' hint-align='center'>" + XmlEscape(data.bat_range_estim_or_ideal) + "</text>" );
                strResult.AppendLine( "</binding>" );
            }
            catch (Exception e)
            {
                logger.Debug(e, "_GenerateTileMedium");
            }
            return strResult.ToString();
        }


        private string _GenerateTileWide(UpdateTileViewModel data)
        {
            StringBuilder strResult = new StringBuilder();
            try
            { 
                logger.Trace(TASK_NAME + " _GenerateTileMedium");

                // Now generate the Tile Template
                strResult.AppendLine( "<binding template='" + TEMPLATE_NAME_WIDE + "' branding='name'  displayName='" + XmlEscapeAttr(data.vehicle_label) + "' hint-textStacking='center' hint-overlay='0' >" );
                strResult.AppendLine(     "<group>" );
                strResult.AppendLine(         "<subgroup hint-weight='2'>" );
                strResult.AppendLine(             "<text hint-style='subtitle' hint-align='center'>" + XmlEscape(data.bat_soc) + "</text>" );
                strResult.AppendLine(         "</subgroup>" );
                strResult.AppendLine(         "<subgroup hint-weight='3'>" );
                strResult.AppendLine(             "<text hint-style='baseSubtle' hint-align='center'>" + XmlEscape(data.bat_range_estim_and_ideal) + "</text>");
                strResult.AppendLine(         "</subgroup>" );
                strResult.AppendLine(     "</group>" );
                strResult.AppendLine(     "<image src='" + XmlEscape(data.bat_soc_img) + "' placement='inline' hint-removeMargin='true' />" );
                strResult.AppendLine( "</binding>" );
            }
            catch (Exception e)
            {
                logger.Debug(e, "_GenerateTileMedium");
            }
            return strResult.ToString();
        }


        private string _GenerateTileLarge(UpdateTileViewModel data)
        {
            StringBuilder strResult = new StringBuilder();
            try
            { 
                logger.Trace(TASK_NAME + " _GenerateTileMedium");

                // Now generate the Tile Template
                strResult.AppendLine( "<binding template='" + TEMPLATE_NAME_LARGE + "' branding='name'  displayName='" + XmlEscapeAttr(data.vehicle_label) + "' hint-textStacking='top' hint-overlay='0' >" );
                strResult.AppendLine(     "<group>" );
                strResult.AppendLine(         "<subgroup hint-weight='2'>" );
                strResult.AppendLine(             "<text hint-style='subtitle' hint-align='center'>" + XmlEscape(data.bat_soc) + "</text>" );
                strResult.AppendLine(         "</subgroup>" );
                strResult.AppendLine(         "<subgroup hint-weight='3'>" );
                strResult.AppendLine(             "<text hint-style='baseSubtle' hint-align='center'>" + XmlEscape(data.bat_range_estim_and_ideal) + "</text>" );
                strResult.AppendLine(         "</subgroup>" );
                strResult.AppendLine(     "</group>" );
                strResult.AppendLine(     "<image src='" + XmlEscape(data.bat_soc_img) + "' placement='inline' />" );

                // Still some room left to add more stuff...

                strResult.AppendLine( "</binding>" );
            }
            catch (Exception e)
            {
                logger.Debug(e, "_GenerateTileMedium");
            }
            return strResult.ToString();
        }


        private string XmlEscapeAttr(string unescaped)
        {

            System.Xml.XmlDocument  doc  = new System.Xml.XmlDocument();
            System.Xml.XmlAttribute attr = doc.CreateAttribute("attr");
            attr.InnerText = unescaped;

            return attr.InnerXml;
        }

        private string XmlEscape(string unescaped)
        { 
            System.Xml.XmlDocument doc  = new System.Xml.XmlDocument();
            System.Xml.XmlElement  node = doc.CreateElement("node");
            node.InnerText = unescaped;

            return node.InnerXml;
        }      



    }
}
