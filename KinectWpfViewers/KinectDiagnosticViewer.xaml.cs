using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Kinect;

namespace Microsoft.Samples.Kinect.WpfViewers
{
    /// <summary>
    /// Interaction logic for KinectDiagnosticViewer.xaml
    /// </summary>
    public partial class KinectDiagnosticViewer : UserControl
    {
        #region Public API
        public KinectDiagnosticViewer(bool renderColor)
        {
            InitializeComponent();
            _kinectSettings = new KinectSettings(this, renderColor);
            _kinectSettings.PopulateComboBoxesWithFormatChoices();
            Settings.Content = _kinectSettings;
            KinectColorViewer = colorViewer;
            StatusChanged();
        }

        public void StatusChanged()
        {
            if (kinectAppConflict)
            {
                status.Text = "KinectAppConflict";
            }
            else if (Kinect == null)
            {
                status.Text = "Kinect initialize failed";
            }
            else
            {
                status.Text = Kinect.Status.ToString();

                if (Kinect.Status == KinectStatus.Connected)
                {
                    //Update comboboxes' selected value based on stream isenabled/format.
                    _kinectSettings.colorFormats.SelectedValue = Kinect.ColorStream.Format;
                    _kinectSettings.depthFormats.SelectedValue = Kinect.DepthStream.Format;
                    
                }
            }
        }
        #endregion Public API

        #region Kinect discovery + setup

        /* To add Kinect support, an app can:
         *   copy the entire "Kinect discovery + setup" region into your app's code file.
         *   call KinectStart() from Window loaded.
         *   call KinectStop() from Window closed.
         *   modify ShowStatus method to display app specific feedback to the user for each of the conditions.
         *   modify Initialize/UnitializeKinectServices for your app.
         */


        //Kinect enabled apps should customize which Kinect services it initializes here.
        private KinectSensor InitializeKinectServices(KinectSensor sensor)
        {

            //Inform the viewers of the Kinect KinectSensor.
            KinectColorViewer.Kinect = sensor;
            KinectDepthViewer.Kinect = sensor;
            KinectSkeletonViewerOnColor.Kinect = sensor;
            KinectSkeletonViewerOnDepth.Kinect = sensor;

            return sensor;
        }

        //Kinect enabled apps should uninitialize all Kinect services that were initialized in InitializeKinectServices() here.
        private void UninitializeKinectServices(KinectSensor sensor)
        {
            //Inform the viewers that they no longer have a Kinect KinectSensor.
            KinectColorViewer.Kinect = null;
            KinectDepthViewer.Kinect = null;
            KinectSkeletonViewerOnColor.Kinect = null;
            KinectSkeletonViewerOnDepth.Kinect = null;

            //Disable skeletonengine, as only one Kinect can have it enabled at a time.
            if (sensor.SkeletonStream != null)
            {
                sensor.SkeletonStream.Disable();
            }
        }

        #region Most apps won't modify this code
        private void KinectStart()
        {

        }

        private void KinectStop()
        {
            if (Kinect != null)
            {
                Kinect = null;
            }
        }

        private bool IsKinectStarted
        {
            get { return Kinect != null; }
        }

        public KinectSensor Kinect
        {
            get
            {
                return _Kinect;
            }
            set
            {
                if (_Kinect != null)
                {
                    bool wasInitialized = false;
                    SensorIsInitialized.TryGetValue(_Kinect, out wasInitialized);
                    if (wasInitialized)
                    {
                        UninitializeKinectServices(_Kinect);
                        SensorIsInitialized[_Kinect] = false;
                    }
                }
                _Kinect = value;
                _kinectSettings.Kinect = value;
                if (_Kinect != null)
                {
                    if (_Kinect.Status == KinectStatus.Connected)
                    {
                        _Kinect = InitializeKinectServices(_Kinect);

                        if (_Kinect != null)
                        {
                            SensorIsInitialized[_Kinect] = true;
                        }
                    }
                }
                StatusChanged(); //update the UI about this sensor
            }
        }

        private KinectSensor _Kinect;
        const int KinectAppConflictErrorCode = -2147220947;
        bool kinectAppConflict = false;
        Dictionary<KinectSensor, bool> SensorIsInitialized = new Dictionary<KinectSensor, bool>();
        #endregion Most apps won't modify this code

        #endregion Kinect discovery + setup

        #region Private State
        public ImageViewer KinectColorViewer;
        private KinectSettings _kinectSettings;
        #endregion Private State

        private void kinectAudioViewer_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {

        }
    }
}
