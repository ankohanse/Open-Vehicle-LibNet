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

using NLog;
using NLog.Targets;
using OpenVehicle.App.Tasks;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using nav = OneCode.Windows.UWP.Controls;


namespace OpenVehicle.App
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        // Logging
        private static NLog.Logger          logger          = NLog.LogManager.GetCurrentClassLogger();

        // Static helpers
        public static RootPage              RootPage        { get { return RootPage.Instance; } }
        public static Frame                 RootFrame       { get { return RootPage.Instance.RootFrame; } }
        public static nav.NavigationView    RootNav         { get { return RootPage.Instance.RootNav; } }
        public static RootViewModel         RootViewModel   { get { return RootPage.Instance.ViewModel; } }


        public static string Title
        {
            get
            {
                var assembly = typeof(App).GetTypeInfo().Assembly;
                return assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
            }
        }


        public static string Version
        {
            get
            {
                var assembly = typeof(App).GetTypeInfo().Assembly;
                var assemblyVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
                return assemblyVersion.ToString();
            }
        }

               
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending        += OnSuspending;
            this.Resuming          += OnResuming;
            this.EnteredBackground += OnEnteredBackground;
            this.LeavingBackground += OnLeavingBackground;

            // Init logging
#if DEBUG
            NLog.LogManager.ThrowExceptions = true;

            var fileTarget = new NLog.Targets.FileTarget
            {
                Name            = "file",
                FileName        = "${var:LogPath}\\nlog.txt",
                Layout          = "${date}|${level}|${message}|${exception:format=tostring}",
                ArchiveFileName = "${var:LogPath}\\nlog.{##}.txt",
                ArchiveNumbering= ArchiveNumberingMode.Sequence,
                ArchiveEvery    = FileArchivePeriod.Day,
                MaxArchiveFiles = 5
            };
            var fileRule = new NLog.Config.LoggingRule("*", LogLevel.Trace, fileTarget);

            NLog.LogManager.Configuration.AddTarget( "file", fileTarget );
            NLog.LogManager.Configuration.LoggingRules.Add( fileRule );
            NLog.LogManager.Configuration.Variables["LogPath"] = Windows.Storage.ApplicationData.Current.LocalFolder.Path;

            NLog.LogManager.ReconfigExistingLoggers();
#endif

            logger.Info("------------------------------------");
            logger.Info("App starting");
        }


        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {

            Frame rootFrame = GetRootFrame();

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(BatteryPage), e.Arguments);
            }

            // Ensure the current window is active
            Window.Current.Activate();
        }


        private Frame GetRootFrame()
        {
            Frame rootFrame;

            RootPage rootPage = (RootPage)Window.Current.Content;
            if (rootPage == null)
            {
                rootPage = new RootPage();
                rootFrame = (Frame)rootPage.FindName("rootFrame");
                if (rootFrame == null)
                {
                    throw new Exception("Root frame not found");
                }

                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];
                rootFrame.NavigationFailed += OnNavigationFailed;

                Window.Current.Content = rootPage;
            }
            else
            {
                rootFrame = (Frame)rootPage.FindName("rootFrame");
            }

            return rootFrame;
        }


        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }


        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            // No  longer used. Now done via OnEnteredBackground and OnLeavingBackground
            await Task.Delay(0);
        }


        private async void OnResuming(object sender, object e)
        {
            // No  longer used. Now done via OnEnteredBackground and OnLeavingBackground
            await Task.Delay(0);
        }


        private async void OnEnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            var deferral = e.GetDeferral();
            try
            {
                logger.Info("------------------------------------");
                logger.Info("App entered background");

                await RootViewModel.StopAsync();
            }
            catch (Exception)
            {
            }
            finally
            {
                deferral.Complete();
            }
        }


        private async void OnLeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            var deferral = e.GetDeferral();
            try
            {
                logger.Info("------------------------------------");
                logger.Info("App leaving background");

                await RootViewModel.StartAsync();

                // Re-register background tasks
                await ReRegisterTask.Trigger(); 

                // Trigger refresh of the live tiles (after a delay to let the app start first)
                Task noAwait = Task.Factory.StartNew( async () => {
                    await Task.Delay(30*1000);
                    await UpdateTileTask.Trigger();
                });
            }
            catch (Exception)
            {
            }
            finally
            {
                deferral.Complete();
            }
        }


        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            base.OnBackgroundActivated(args);

            IBackgroundTaskInstance  taskInstance = args.TaskInstance;
            switch (taskInstance.Task.Name)
            {
                case ReRegisterTask.TASK_NAME:
                    {
                        ReRegisterTask task = new ReRegisterTask();
                        task.Run(taskInstance);
                    }
                    break;

                case UpdateTileTask.TASK_NAME:
                    {
                        UpdateTileTask task = new UpdateTileTask();
                        task.Run(taskInstance);
                    }
                    break;

                default:
                    {
                        logger.Warn($"Unknown background task name: {taskInstance.Task.Name}");
                    }
                    break;
            }
        }

    }
}
