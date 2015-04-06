/////////////////////////////////////////////////////////////////////////
//
// This module contains code to do Kinect NUI initialization and
// processing and also to display NUI streams on screen.
//
// Copyright © Microsoft Corporation.  All rights reserved.  
// This code is licensed under the terms of the 
// Microsoft Kinect for Windows SDK (Beta) 
// License Agreement: http://kinectforwindows.org/KinectSDK-ToU
//
/////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using System.IO;
using Edu.FIRST.WPI.Kinect.KinectServer.Networking.Protocols;

namespace Edu.FIRST.WPI.Kinect.KinectServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Public State
        //Flags for program options, set by passing in paramaters when launching program
        public bool Minimal = true;
        public bool RenderColor = false;
        public bool LogFPS = false;

        #region Skeleton ROI Constants
        public const double X_MIN_VALUE = -.9;  //X +/- 3 ft for the width of Kinect play area (values in meters)
        public const double X_MAX_VALUE = .9;
        public const double Z_MIN_VALUE = .9;  //Z 3-11 ft depth of Kinect play area
        public const double Z_MAX_VALUE = 3.4;
        #endregion
        #endregion

        #region Private state
        KinectProtocol_v1Manager manager;
        DateTime lastTime = DateTime.MaxValue;
        int frameRate = 0;
        int lastFrames = 0;
        int totalFrames = 0;
        StreamWriter fpsLog;
        Skeleton[] skeletonArray;
        int trackingID;
        BackgroundWorker worker;
        KinectSensorChooser sensorChooser;
        KinectSensorItem sensorItem;
        #endregion Private state

        #region ctor & Window events
        /// <summary>
        /// Constructor for the MainWindow
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            sensorChooser = new KinectSensorChooser();
        }

        /// <summary>
        /// Method called when the Window loads.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, EventArgs e)
        {
            //Set process priority to BelowNormal to avoid affecting DS behavior on CPU limited systems.
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;

            //Kill other instances of the Kinect Server if found. Only one copy can access the Kinect at a time
            Process[] otherServers = System.Diagnostics.Process.GetProcessesByName("KinectServer");
            foreach (System.Diagnostics.Process killThis in otherServers)
            {
                if (killThis.Id != Process.GetCurrentProcess().Id)
                {
                    killThis.Kill();
                }
                System.Threading.Thread.Sleep(500); //wait to allow any instance using a Kinect to release it
            }

            //If logging framerate create the log file in the FRC folder where DS info is stored
            if (LogFPS)
            {
                //Loop until an available file name is found. File names are of the form KinectFPS#.txt
                int i = 0;
                while (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments) + "\\FRC\\KinectFPS" + i + ".txt"))
                    i++;

                fpsLog = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments) + "\\FRC\\KinectFPS" + i + ".txt", false);
            }

            manager = new KinectProtocol_v1Manager("1.05.13.00", "localhost", 1155);

            //Start the Kinect
            KinectStart();

            //Create the thread to do the polling for new Skeleton frames
            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += new DoWorkEventHandler(PollForSkeletons);
            worker.RunWorkerAsync();
            this.Unloaded += (s, ev) => { worker.CancelAsync(); };
        }

        /// <summary>
        /// Method called when the MainWindow is closed. Close the log file if necessary and stop the Kinect
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            if (LogFPS)
                fpsLog.Close();

            KinectStop();
        }
        #endregion ctor & Window events

        #region Kinect Management
        /// <summary>
        /// Start the Kinect SensorChooser and register for events on Status and Kinect changes
        /// </summary>
        private void KinectStart()
        {
            //listen for any change to the Kinect device or device status
            sensorChooser.PropertyChanged += new PropertyChangedEventHandler(Kinects_StatusChanged);
            sensorChooser.KinectChanged += new EventHandler<KinectChangedEventArgs>(Kinect_Changed);

            sensorChooser.Start();
        }

        /// <summary>
        /// Stop the Kinect SensorChooser, unregister from event handlers and close the Kinect window if one is open
        /// </summary>
        private void KinectStop()
        {
            sensorChooser.PropertyChanged -= new PropertyChangedEventHandler(Kinects_StatusChanged);
            sensorChooser.KinectChanged -= new EventHandler<KinectChangedEventArgs>(Kinect_Changed);
            sensorChooser.Stop();
            if (sensorItem.Window != null)
            {
                sensorItem.Window.Kinect = null;
                sensorItem.Window.Close();
            }
        }

        /// <summary>
        /// Display the sensor status on the MainWindow and update the status being sent out over the network
        /// </summary>
        /// <param name="kinectStatus"></param>
        private void ShowStatus(ChooserStatus kinectStatus)
        {
            string statusText = "No Kinect";

            switch (kinectStatus)
            {
                case ChooserStatus.None:
                    statusText = "Error";
                    break;
                case ChooserStatus.NoAvailableSensors:
                    statusText = "No Kinect";
                    break;
                case ChooserStatus.SensorConflict:
                    statusText = "AppConflict";
                    break;
                case ChooserStatus.SensorError:
                    statusText = "Sensor Err";
                    break;
                case ChooserStatus.SensorInitializing:
                    statusText = "Init";
                    break;
                case ChooserStatus.SensorInsufficientBandwidth:
                    statusText = "No USB BW";
                    break;
                case ChooserStatus.SensorNotGenuine:
                    statusText = "Bad Sensor";
                    break;
                case ChooserStatus.SensorNotPowered:
                    statusText = "No Power";
                    break;
                case ChooserStatus.SensorNotSupported:
                    statusText = "Bad Sensor";
                    break;
                case ChooserStatus.SensorStarted:
                    statusText = "OK";
                    break;
            }
            manager.SetKinectStatus(statusText);
            sensorStatusChanges.Text += statusText + "\n";
        }

        /// <summary>
        /// Method to handle Kinect device changes. Initializes and Unitializes Kinect devices as appropriate. 
        /// Kinect.Start and Kinect.Stop calls handled automatically by the KinectSensorChooser
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Kinect_Changed(object sender, KinectChangedEventArgs e)
        {
            //If we had a sensor, uninitialize it and close the display window
            if (e.OldSensor != null)
            {
                UninitializeKinectServices(e.OldSensor);
                if (sensorItem.Window != null)
                {
                    sensorItem.Window.Close();
                    sensorItem.Window = null;
                }
                sensorItem = null;
            }

            //If we have a new sensor initialize it, and open a new display window if appropriate
            if (e.NewSensor != null)
            {
                InsertKinect.Visibility = System.Windows.Visibility.Collapsed;
                InitializeKinectServices(e.NewSensor);
                sensorItem = new KinectSensorItem()
                                    {
                                        Window = null,
                                        Sensor = e.NewSensor,
                                        Status = e.NewSensor.Status
                                    };
                if (!Minimal && sensorItem.Window == null && e.NewSensor.Status != KinectStatus.Initializing)
                {
                    var kinectWindow = new KinectWindow(RenderColor);
                    kinectWindow.Kinect = e.NewSensor;
                    kinectWindow.Show();

                    sensorItem.Window = kinectWindow;
                }
                sensorItem.Status = e.NewSensor.Status;
            }
            else
            {
                InsertKinect.Visibility = System.Windows.Visibility.Visible;
            }
        }

        /// <summary>
        /// Method to handle Kinect status changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Kinects_StatusChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName){
                case "Status":
                    ShowStatus(sensorChooser.Status);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Checks if the Skeletal Viewer is currently available. Returns True if the Skeletal Engine is not in use
        /// </summary>
        private bool IsSkeletalViewerAvailable()
        {
            if (sensorChooser.Kinect.SkeletonStream.IsEnabled && sensorChooser.Kinect.IsRunning)
                return false;
            return true;
        }
        #endregion Kinect Management

        #region Kinect Services
        /// <summary>
        /// Initializes a Kinect by opening the appropriate streams, and setting an error condition if appropriate.
        /// </summary>
        /// <param name="runtime">The Kinect to initialize</param>
        private void InitializeKinectServices(KinectSensor runtime)
        {
            bool skeletalViewerAvailable = IsSkeletalViewerAvailable();

            runtime.ElevationAngle = 0;

            if (!Minimal && RenderColor)
            {
                runtime.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            }

            runtime.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);

            if (skeletalViewerAvailable)
            {
                runtime.SkeletonStream.AppChoosesSkeletons = true;
                //runtime.SkeletonStream.AppChoosesSkeletons = false;
                runtime.SkeletonStream.Enable();
            }

            trackingID = 0;

        }

        /// <summary>
        /// Method to uninitialize a Kinect. Closes all streams and removes event handlers
        /// </summary>
        /// <param name="runtime">The Kinect to be uninitialized</param>
        private void UninitializeKinectServices(KinectSensor runtime)
        {
            runtime.Stop();
        }
        #endregion Kinect Services

        #region Skeleton Processing Methods
        /// <summary>
        /// Method to choose and set which skeleton to actively track. Selects a skeleton within a defined Region
        /// of Interest and continues to track it until it is no longer detected or leaves the ROI
        /// </summary>
        /// <param name="skeletonData">The sekelton data to choose a skeleton from</param>
        private void ChooseSkeleton(IEnumerable<Skeleton> skeletonData)
        {
            List<int> newList = new List<int>();

            //Add skeletons within the ROI to the list
            foreach (Skeleton skeleton in skeletonData)
            {
                if (skeleton.TrackingState != SkeletonTrackingState.NotTracked &&
                    skeleton.Position.X > X_MIN_VALUE && skeleton.Position.X < X_MAX_VALUE &&
                    skeleton.Position.Z > Z_MIN_VALUE && skeleton.Position.Z < Z_MAX_VALUE)
                {
                    newList.Add(skeleton.TrackingId);
                }
            }

            //If no skeletons are withing the ROI track the default skeleton and reset the Tracking ID
            if (newList.Count == 0)
            {
                if(sensorChooser.Kinect.SkeletonStream.IsEnabled)
                    sensorChooser.Kinect.SkeletonStream.ChooseSkeletons();
                trackingID = 0;
            }
            else
            {
                //If a skeleton is within the ROI, but does not match the current Tracking ID, track it and set the ID
                if (!newList.Contains(trackingID))
                {
                    trackingID = newList.First();
                    sensorChooser.Kinect.SkeletonStream.ChooseSkeletons(trackingID);
                }
                //If the current TrackingID is in the list, do nothing
            }
        }
       
        /// <summary>
        /// The method used to poll for new skeleton frames. Should be called using a BackgroundWorker thread
        /// </summary>
        /// <param name="sender">Background Worker</param>
        /// <param name="e"></param>
        void PollForSkeletons(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            if (worker != null)
            {
                while (!worker.CancellationPending)
                {
                    //Check if the SensorChooser has found and started a Kinect
                    if (sensorChooser.Status == ChooserStatus.SensorStarted)
                    {
                        //Check that the Kinect is still running and the SkeletonStream is enabled
                        if (sensorChooser.Kinect.IsRunning && sensorChooser.Kinect.SkeletonStream.IsEnabled)
                        {
                            //Poll for a new SkeletonFrame. If no frame is available after 50ms return null
                            using (SkeletonFrame skeletonFrame = sensorChooser.Kinect.SkeletonStream.OpenNextFrame(50))
                            {
                                //KinectSDK TODO: This nullcheck shouldn't be required. 
                                //Unfortunately, this version of the Kinect Runtime will continue to fire some skeletonFrameReady events after the Kinect USB is unplugged.
                                if (skeletonFrame != null)
                                {
                                    if (LogFPS)
                                        CalculateFrameRate();

                                    //If an appropriately sized skeletonArray does not exist, allocate one
                                    if ((skeletonArray == null) || (skeletonArray.Length != skeletonFrame.SkeletonArrayLength))
                                    {
                                        skeletonArray = new Skeleton[skeletonFrame.SkeletonArrayLength];
                                    }

                                    //Copy skeleton data to the skeletonArray and choose the skeleton(s) to track in subsequent frames
                                    skeletonFrame.CopySkeletonDataTo(skeletonArray);
                                    ChooseSkeleton(skeletonArray);

                                    //Process the SkeletonFrame and send data
                                    manager.ProcessSkeletonData(skeletonFrame);
                                }
                                System.Threading.Thread.Yield();
                            }
                        }
                        else System.Threading.Thread.Sleep(50);
                    }
                    //If the sensor was being used by another app, check if it still is
                    else if (sensorChooser.Status == ChooserStatus.SensorConflict)
                    {
                        sensorChooser.TryResolveConflict();
                        System.Threading.Thread.Sleep(10);
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(10);
                    }
                }
            }
            e.Cancel = true;
        }

        /// <summary>
        /// Method to calculate and log the frame rate. Called every time a new frame is processed, logs the number of frames
        /// processed once every second.
        /// </summary>
        private void CalculateFrameRate()
        {
            ++totalFrames;

            DateTime cur = DateTime.Now;
            if (lastTime == DateTime.MaxValue || cur.Subtract(lastTime) > TimeSpan.FromSeconds(1))
            {
                frameRate = totalFrames - lastFrames;
                lastFrames = totalFrames;
                lastTime = cur;
                fpsLog.WriteLine(frameRate);
                fpsLog.Flush();
            }
        }
        #endregion Skeleton Processing Methods
    }

    #region HelperClasses - KinectSensorItem, VisibilityConverter

    public class KinectSensorItem : INotifyPropertyChanged
    {
        public KinectSensor Sensor { get; set; }
        public KinectStatus Status
        {
            get { return _Status; }
            set
            {
                _Status = value; NotifyPropertyChanged("Status");
            }
        }
        public KinectWindow Window
        {
            get { return _Window; }
            set
            {
                _Window = value; NotifyPropertyChanged("Window");
                if (_Window != null)
                {
                    _Window.DataContext = this;
                }
            }
        }

        private KinectStatus _Status;
        private KinectWindow _Window;

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }

    public class VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = (int)value > 0;
            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    #endregion
}
