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
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace OpenVehicle.App.Tasks
{
    public class ReRegisterTask
    {
        public const string                 TASK_NAME               = "ReRegisterTask";

        // Logging
        private static NLog.Logger          logger                  = NLog.LogManager.GetCurrentClassLogger();

        // Trigger 
        private static ApplicationTrigger   glb_appTrigger          = null;

        // Events
        private volatile ManualResetEvent   m_canceledEvent         = new ManualResetEvent(false);
        private volatile bool               m_bCanceled             = false;


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
                    taskBuilder.SetTrigger(new SystemTrigger(SystemTriggerType.ServicingComplete, false));
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
                }

                logger.Info(TASK_NAME + " registered");
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
            BackgroundTaskDeferral deferral = taskInstance?.GetDeferral();

            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceledAsync);
            try
            {
                await Task.Factory.StartNew( async () => await RunAsync() );
            }
            catch (Exception)
            {
            }
            finally
            {            
                taskInstance.Canceled -= new BackgroundTaskCanceledEventHandler(OnCanceledAsync);

                // Inform the system that the task is finished.
                deferral?.Complete();
            }
        }


        public async Task RunAsync()
        {
            logger.Info("------------------------------------");
            logger.Info(TASK_NAME + " starting");
            
            if (!m_bCanceled)
            {
                await UpdateTileTask.Register(true);
            }

            logger.Info(TASK_NAME + " stopped");
        }


        private void OnCanceledAsync(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            logger.Debug("Background " + sender.Task.Name + " Cancel Requested...");

            // Indicate that the background task is canceled.
            m_bCanceled = true;
            m_canceledEvent.Set();
        }



    }
}
