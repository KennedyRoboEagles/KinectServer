/////////////////////////////////////////////////////////////////////////
// Copyright (c) FIRST 2011. All Rights Reserved.							  
// Open Source Software - may be modified and shared by FRC teams. The code   
// must be accompanied by, and comply with the terms of, the license found at
// \FRC Kinect Server\License_for_KinectServer_code.txt which complies
// with the Microsoft Kinect for Windows SDK (Beta) 
// License Agreement: http://kinectforwindows.org/download/EULA.htm
/////////////////////////////////////////////////////////////////////////

using System;
using System.Configuration;
using System.Windows;

namespace Edu.FIRST.WPI.Kinect.KinectServer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var mainWindow = new Edu.FIRST.WPI.Kinect.KinectServer.MainWindow();

            //By default don't show a taskbar icon or have the window take focus when launched
            mainWindow.ShowInTaskbar = false;
            mainWindow.ShowActivated = false;

            //Loop through the arguments passed in
            foreach (String s in e.Args)
            {
                switch (s)
                {
                    //If the -debug switch is passed, show the main window, show a taskbar icon and grab focus when launched
                    case "-debug":
                        mainWindow.Minimal = false;
                        mainWindow.ShowInTaskbar = true;
                        mainWindow.ShowActivated = true;
                        break;
                    //If the -logFPS switch is passed set the LogFPS flag to log framerate
                    case "-logFPS":
                        mainWindow.LogFPS = true;
                        break;
                    //If the -color flag is passed, set the RenderColor flag to display the color stream
                    case "-color":
                        mainWindow.RenderColor = true;
                        break;
                    default:
                        break;
                }
            }
            mainWindow.Show();

            //If the -debug switch was not passed hide the window
            if(mainWindow.Minimal)
                MainWindow.Visibility = Visibility.Hidden;
        }
    }
}