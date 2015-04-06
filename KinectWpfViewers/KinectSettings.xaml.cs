using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Kinect;

namespace Microsoft.Samples.Kinect.WpfViewers
{
    /// <summary>
    /// Interaction logic for KinectSettings.xaml
    /// </summary>
    internal partial class KinectSettings : UserControl
    {
        public KinectSettings(KinectDiagnosticViewer diagViewer, bool renderColor)
        {
            DiagViewer = diagViewer;
            InitializeComponent();
            if (renderColor)
            {
                ColorStreamEnable.IsChecked = true;
            }
            DisplayColumnBasedOnIsChecked(ColorStreamEnable, 1, 2);
            DisplayPanelBasedOnIsChecked(ColorStreamEnable, DiagViewer.colorPanel);
        }

        public KinectDiagnosticViewer DiagViewer { get; set; }

        private KinectSensor _Kinect;

        public KinectSensor Kinect
        {
            get { return _Kinect; }
            set { _Kinect = value; }
        }

        #region UI Event Handlers

        internal void PopulateComboBoxesWithFormatChoices()
        {
            foreach (ColorImageFormat colorImageFormat in Enum.GetValues(typeof (ColorImageFormat)))
            {
                if (colorImageFormat == ColorImageFormat.RawYuvResolution640x480Fps15 || colorImageFormat == ColorImageFormat.Undefined)
                {
                    //don't add Undefined to combobox
                    //don't add RawYuv to combobox..
                    //That colorImageFormat works, but needs YUV->RGB conversion code which this sample doesn't have yet.
                }
                else
                {
                    colorFormats.Items.Add(colorImageFormat);
                }
            }

            foreach (DepthImageFormat depthImageFormat in Enum.GetValues(typeof (DepthImageFormat)))
            {
                if (depthImageFormat == DepthImageFormat.Undefined)
                {
                    //don't add Undefined to combobox
                }
                else
                {
                    depthFormats.Items.Add(depthImageFormat);
                }
            }
        }

        private void colorFormats_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            if (Kinect != null && Kinect.Status == KinectStatus.Connected && comboBox.SelectedItem != null)
            {
                if (Kinect.ColorStream.IsEnabled)
                {
                    Kinect.ColorStream.Enable((ColorImageFormat) colorFormats.SelectedItem);
                }
            }
        }

        private void depthFormats_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            if (Kinect != null && Kinect.Status == KinectStatus.Connected && comboBox.SelectedItem != null)
            {
                if (Kinect.DepthStream.IsEnabled)
                {
                    Kinect.DepthStream.Enable((DepthImageFormat) depthFormats.SelectedItem);
                }
            }
        }

        private void ColorStream_Enabled(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox) sender;
            DisplayColumnBasedOnIsChecked(checkBox, 1, 2);
            DisplayPanelBasedOnIsChecked(checkBox, DiagViewer.colorPanel);
            if (Kinect != null)
            {
                EnableColorImageStreamBasedOnIsChecked(checkBox, Kinect.ColorStream, colorFormats);
            }
        }

        private void EnableColorImageStreamBasedOnIsChecked(CheckBox checkBox, ColorImageStream imageStream,
                                                            ComboBox colorFormats)
        {
            if (checkBox.IsChecked.HasValue && checkBox.IsChecked.Value)
            {
                imageStream.Enable((ColorImageFormat) colorFormats.SelectedItem);
            }
            else
            {
                imageStream.Disable();
            }
        }

        private void DisplayPanelBasedOnIsChecked(CheckBox checkBox, Grid panel)
        {
            //on load of XAML page, panel will be null.
            if (panel == null)
                return;

            if (checkBox.IsChecked.HasValue && checkBox.IsChecked.Value)
            {
                panel.Visibility = Visibility.Visible;
            }
            else
            {
                panel.Visibility = Visibility.Collapsed;
            }
        }

        private void DisplayColumnBasedOnIsChecked(CheckBox checkBox, int column, int stars)
        {
            if (checkBox.IsChecked.HasValue && checkBox.IsChecked.Value)
            {
                DiagViewer.LayoutRoot.ColumnDefinitions[column].Width = new GridLength(stars, GridUnitType.Star);
            }
            else
            {
                DiagViewer.LayoutRoot.ColumnDefinitions[column].Width = new GridLength(0);
            }
        }

        #endregion UI Event Handlers
    }
}
