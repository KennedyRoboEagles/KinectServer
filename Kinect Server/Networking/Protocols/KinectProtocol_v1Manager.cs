/////////////////////////////////////////////////////////////////////////
// Copyright (c) FIRST 2011. All Rights Reserved.							  
// Open Source Software - may be modified and shared by FRC teams. The code   
// must be accompanied by, and comply with the terms of, the license found at
// \FRC Kinect Server\License_for_KinectServer_code.txt which complies
// with the Microsoft Kinect for Windows SDK (Beta) 
// License Agreement: http://kinectforwindows.org/download/EULA.htm
/////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Edu.FIRST.WPI.Kinect.KinectServer.Networking.Serialization;
using Edu.FIRST.WPI.Kinect.KinectServer.Kinect;
using Microsoft.Kinect;
using Edu.FIRST.WPI.Kinect.KinectServer.Networking.WritableElements;
using Edu.FIRST.WPI.Kinect.KinectServer;
using Edu.FIRST.WPI.Kinect.KinectServer.Networking.Networktables;

namespace Edu.FIRST.WPI.Kinect.KinectServer.Networking.Protocols
{
    class KinectProtocol_v1Manager : ISkeletonProcessor, IDisposable
    {
        #region Private state
        protected const int HEARTBEAT_PERIOD_MS = 450;
        protected String m_hostname;
        protected int m_port;
        protected String m_kinectVersion;
        protected String m_kinectStatus;
        protected UdpClient m_udpClient;
        protected KinectProtocol_v1 m_version1Packet;
        protected Timer m_heartbeatTimer;
        protected bool m_started = false;
        protected IGestureProcessor m_gestureProcessor;
        protected Skeleton[] m_skeletonArray;
        protected bool m_disposed = false;
        #endregion

        #region Constructor and Close
        /// <summary>
        /// Manages sending Kinect information as UDP to the specified hostname and port
        /// at least once a second.
        /// 
        /// Uses the FIRST gesture processor to process gestures into joysticks.
        /// </summary>
        /// <param name="kinectVersion">The version string to report to the Driver Station.</param>
        /// <param name="hostname">The destination hostname.</param>
        /// <param name="port">The destination port number.</param>
        public KinectProtocol_v1Manager(String kinectVersion, String hostname, int port)
        {
            m_kinectVersion = kinectVersion;
            m_kinectStatus = "No Kinect";
            m_hostname = hostname;
            m_port = port;
            m_udpClient = new UdpClient();
            m_version1Packet = new KinectProtocol_v1();
            m_heartbeatTimer = new Timer(this.HeartBeatExpired);
            m_heartbeatTimer.Change(HEARTBEAT_PERIOD_MS, HEARTBEAT_PERIOD_MS);
            m_gestureProcessor = new FIRSTGestureProcessor();

            NTKinect.Init();

        }

        /// <summary>
        /// Manages sending Kinect information as UDP to the specified hostname and port
        /// at least once a second.
        /// 
        /// Uses the given gesture processor to process gestures into joysticks.
        /// </summary>
        /// <param name="gp">The IGestureProcessor to use to set joystick values.</param>
        /// <param name="kinectVersion">The version string to report to the Driver Station.</param>
        /// <param name="hostname">The destination hostname.</param>
        /// <param name="port">The destination port number.</param>
        public KinectProtocol_v1Manager(IGestureProcessor gp, String kinectVersion, String hostname, int port)
            : this(kinectVersion,
                   hostname, 
                   port)
        {
            m_gestureProcessor = gp;
        }

        /// <summary>
        /// Stops the heartbeat and closes this Manager's associated UDP port.
        /// </summary>
        public void Close()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose method for this object. Releases the heartbeat timer and UDP port.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                if (disposing)
                {
                    m_heartbeatTimer.Dispose();
                    m_udpClient.Close();
                }
                m_disposed = true;
            }
        }
        #endregion


        /// <summary>
        /// Sets the status string being transmitted as part of the packet based on the given status
        /// </summary>
        /// <param name="status">The current Kinect status</param>
        public void SetKinectStatus(string status)
        {
            m_kinectStatus = status;
            if(m_kinectStatus == "OK")
                m_kinectStatus = m_kinectVersion;
        }

        #region Skeleton Data Processing
        /// <summary>
        /// Processes the given skeleton and updates the network packet data structure, then
        /// sends the resulting packet.
        /// </summary>
        /// <param name="frame">The Kinect Skeleton frame to process.</param>
        public void ProcessSkeletonData(SkeletonFrame frame)
        {
            if ((m_skeletonArray== null) || (m_skeletonArray.Length != frame.SkeletonArrayLength))
            {
                m_skeletonArray = new Skeleton[frame.SkeletonArrayLength];
            }
            frame.CopySkeletonDataTo(m_skeletonArray);
            
            // Do stuff here
            lock (m_version1Packet)
            {
                UpdatePacketGlobalData(frame);

                // Get the best skeleton
                Skeleton skeleton = KinectUtils.SelectBestSkeleton(m_skeletonArray);

                UpdatePacketSkeletonData(skeleton);
                UpdatePacketJoystickData(skeleton);                
            }

            Send();
            m_heartbeatTimer.Change(HEARTBEAT_PERIOD_MS, HEARTBEAT_PERIOD_MS);
            NTKinect.UpdateHeartBeat();
        }

        /// <summary>
        /// Updates the packet with the skeleton-independant data from the Skeleton Frame
        /// </summary>
        /// <param name="frame">Skeleton Frame to use for update</param>
        private void UpdatePacketGlobalData(SkeletonFrame frame)
        {
            m_version1Packet.PlayerCount.Set((byte)KinectUtils.CountTrackedSkeletons(m_skeletonArray));
            m_version1Packet.Flags.Set(0);      //Flags only accessible in the C++ API??
            m_version1Packet.FloorClipPlane.Set(frame.FloorClipPlane.Item1,
                                               frame.FloorClipPlane.Item2,
                                               frame.FloorClipPlane.Item3,
                                               frame.FloorClipPlane.Item4);
            m_version1Packet.GravityNormal.Set(0, 0, 0); //Gravity Normal removed from SDK
        }

        /// <summary>
        /// Updates the packet with skeleton data from the skeleton
        /// </summary>
        /// <param name="skeleton">Skeleton to use for update</param>
        private void UpdatePacketSkeletonData(Skeleton skeleton)
        {
            m_version1Packet.Quality.Set((byte)skeleton.ClippedEdges);
            m_version1Packet.CenterOfMass.Set(skeleton.Position.X,
                                              skeleton.Position.Y,
                                              skeleton.Position.Z);
            m_version1Packet.SkeletonTrackState.Set((uint)skeleton.TrackingState);

            // Loop through joints; get tracking states and positions
            byte[] trackingStates = new byte[20];
            WritableVertex[] vertices = new WritableVertex[20];

            for (uint i = 0; i < skeleton.Joints.Count; i++)
            {
                Joint joint = skeleton.Joints[(JointType)i];

                trackingStates[i] = (byte)(joint.TrackingState);
                vertices[i] = new WritableVertex(joint.Position.X,
                                                 joint.Position.Y,
                                                 joint.Position.Z);
            }
            m_version1Packet.SkeletonTrackingStates.Set(trackingStates);
            m_version1Packet.SkeletonJoints.Set(vertices);
        }

        /// <summary>
        /// Updates packet with new joystick data derived from the selected skeleton.
        /// If no players are being tracked, the joystick data is set to all 0's.
        /// Utilises m_gestureprocessor for conversion of skeleton to Joystick data,
        /// this defaults to the FIRST Gesture processor.
        /// </summary>
        /// <param name="skeleton"></param>
        private void UpdatePacketJoystickData(Skeleton skeleton)
        {
            sbyte[] nullAxis = new sbyte[6];

            WritableJoystick[] sticks = new WritableJoystick[2] { 
                    m_version1Packet.Joystick1, 
                    m_version1Packet.Joystick2
                };
            if (m_version1Packet.PlayerCount.Get() != 0)        //Only process and send valid data if a player is detected
            {
                m_gestureProcessor.ProcessGestures(sticks, skeleton);
            }
            else
            {
                m_version1Packet.Joystick1.Set(nullAxis, 0);
                m_version1Packet.Joystick2.Set(nullAxis, 0);
            }
        }
        #endregion

        /// <summary>
        /// Called when the heart beat timer expires. Sends a "blank" packet with player
        /// count and Joystick data set to 0.
        /// </summary>
        /// <param name="stateInfo"></param>
        private void HeartBeatExpired(Object stateInfo)
        {
            sbyte[] nullAxis = new sbyte[6];

            m_version1Packet.PlayerCount.Set(0);
            m_version1Packet.Joystick1.Set(nullAxis, 0);
            m_version1Packet.Joystick2.Set(nullAxis, 0);
            Send();
            NTKinect.UpdateJoysticks(0.0, 0.0, 0.0, 0.0);
        }

        /// <summary>
        /// Cuts a new UDP packet and sends it to the Driver Station.
        /// </summary>
        private void Send()
        {
            using (MemoryStream udpbuffer = new MemoryStream())
            {
                NetworkOrderBinaryWriter writer = new NetworkOrderBinaryWriter(udpbuffer);

                lock (m_version1Packet)
                {
                    m_version1Packet.VersionNumber.Set(m_kinectStatus);
                    m_version1Packet.Serialize(writer);

                    NTKinect.UpdateFromPacket(m_version1Packet);
                }

                m_udpClient.Send(udpbuffer.GetBuffer(), (int)udpbuffer.Length, m_hostname, m_port);
            }
        }

        public void Dispose()
        {
           Dispose(false);
        }
    }
}
