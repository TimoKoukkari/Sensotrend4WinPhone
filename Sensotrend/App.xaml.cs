using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;

namespace Sensotrend
{

    public partial class App : Application
    {
        public static string accessToken;

        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public PhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            // Global handler for uncaught exceptions. 
            UnhandledException += Application_UnhandledException;

            // Standard Silverlight initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            // Show graphics profiling information while debugging.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Display the current frame rate counters.
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode, 
                // which shows areas of a page that are handed off to GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Disable the application idle detection by setting the UserIdleDetectionMode property of the
                // application's PhoneApplicationService object to Disabled.
                // Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }

        }

        /* 
         * This sends data to Taltioni using stored access token. 
         */
        public enum TaltioniDataType
        {
            BLOOD_GLUCOSE,
            INSULIN,
            CARBONHYDRATE,
            EXERCISE,
            NOTE
        };

        /**
         * Generates unique request ID for Taltioni OAuth request.
         * @return
         *     Unique request ID.
         */
        protected Guid getUniqueRequestID()
        {
            Guid requestId = new Guid();
            Guid id = new Guid();
            id.setGuid(UUID.randomUUID().toString());
            requestId.setRequestId(id);
            return requestId;
        }
        public static void SendToTaltioni(DateTime date, TaltioniDataType type, ValueType data)
        {
            HealthRecordClient.Data.ObservationItem oi = new HealthRecordClient.Data.ObservationItem();
            oi.EffectiveDateTime = date;
            switch (type)
            {
                case TaltioniDataType.BLOOD_GLUCOSE:
                    oi.TypeId = "Glucose";
                    oi.Unit = "mmol/l";
                    oi.NumberValue = (double)data; 
                    break;
            }
            HealthRecordClient.Data.Observation o = new HealthRecordClient.Data.Observation();
            o.EffectiveDateTime = date;
            o.TypeId = "BloodGlucose";
            o.ObservationItems.SetValue(oi, 0);
            
            HealthRecordClient.Data.HealthRecordData hrd = new HealthRecordClient.Data.HealthRecordData();
            hrd.Observations.SetValue(o, 0);

            Guid requestId = Guid.NewGuid();
            string timestamp = System.Xml.XmlConvert.ToString(DateTime.UtcNow, System.Xml.XmlDateTimeSerializationMode.Utc);
            HealthRecordClient.Data.CodeFilter filter = new HealthRecordClient.Data.CodeFilter();
            StoreHealthRecordItemsRequest request = new StoreHealthRecordItemsRequest(accessToken, Page1.APPLICATION_ID, Page1.authCode, ref requestId, timestamp, hrd, false);

            HealthRecordClient.Data.StoreHealthRecordItemsResponse response;
            var client = new TaltioniAPIClient();
            client.StoreHealthRecordItems
            
            //MessageBox.Show(data);
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            try
            {
                accessToken = (string)IsolatedStorageSettings.ApplicationSettings["Token"];
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
            } 
            MessageBox.Show("Access granted, token: " + accessToken);
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            accessToken = (string)IsolatedStorageSettings.ApplicationSettings["Token"];
            MessageBox.Show("Access granted, token: " + accessToken);
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            IsolatedStorageSettings.ApplicationSettings.Remove("Token");
            IsolatedStorageSettings.ApplicationSettings.Add("Token", App.accessToken);
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            IsolatedStorageSettings.ApplicationSettings.Remove("Token");
            IsolatedStorageSettings.ApplicationSettings.Add("Token", App.accessToken);
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion
    }
}