using System.Windows;
using Microsoft.Kinect;
using Microsoft.Samples.Kinect.WpfViewers;

namespace Edu.FIRST.WPI.Kinect.KinectServer
{
    public class KinectWindow : Window
    {
        public KinectWindow(bool renderColor)
        {
            _KinectDiagnosticViewer = new KinectDiagnosticViewer(renderColor);
            _KinectDiagnosticViewer.KinectColorViewer.CollectFrameRate = true;
            _KinectDiagnosticViewer.KinectDepthViewer.CollectFrameRate = true;
            Content = _KinectDiagnosticViewer;
            Width = 1024;
            Height = 600;
            Title = "FRC Kinect Server";
            this.Closed += new System.EventHandler(KinectWindow_Closed);
        }

        void KinectWindow_Closed(object sender, System.EventArgs e)
        {
            Kinect = null;
        }

        public KinectSensor Kinect
        {
            get
            {
                return _Kinect;
            }
            set
            {
                _Kinect = value;
                _KinectDiagnosticViewer.Kinect = _Kinect;
            }
        }

        public void StatusChanged()
        {
            _KinectDiagnosticViewer.StatusChanged();
        }

        #region Private state
        private KinectDiagnosticViewer _KinectDiagnosticViewer;
        private KinectSensor _Kinect;
        #endregion Private state
    }
}
