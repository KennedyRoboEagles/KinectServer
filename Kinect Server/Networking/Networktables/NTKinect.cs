using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using edu.wpi.first.wpilibj.networktables;
using Edu.FIRST.WPI.Kinect.KinectServer.Networking.Protocols;
using Edu.FIRST.WPI.Kinect.KinectServer.Networking.WritableElements;

namespace Edu.FIRST.WPI.Kinect.KinectServer.Networking.Networktables
{

    class NTKinect 
    {
        private static NetworkTable table;
        private static DateTime jan1970;

        public static NetworkTable GetTable()
        {
            return table;
        }

        public static void Init()
        {
            Console.WriteLine("Initializing NT");
            NetworkTable.setClientMode();
            NetworkTable.setIPAddress("localhost");
            table = NetworkTable.getTable("kinect");

            jan1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        public static void UpdateFromPacket(KinectProtocol_v1 packet)
        {
            if (table != null)
            {
                string version = packet.VersionNumber.Get();
                table.putString("Version", version);
                int playerCount = packet.PlayerCount.Get();
                table.putNumber("PlayerCount", playerCount);
            }
        }

        public static void UpdateJoysticks(double joy1X, double joy1Y, double joy2X, double joy2Y)
        {
            if (table != null)
            {
                table.putNumber("Joy1X", joy1X);
                table.putNumber("Joy1Y", joy1Y);
                table.putNumber("Joy2X", joy2X);
                table.putNumber("Joy2Y", joy2Y);
            }
        }

        public static void UpdateHeartBeat()
        {
            if(table != null) {
                long milli = (long)(DateTime.UtcNow - jan1970).TotalMilliseconds;
                table.putNumber("HeartBeat", milli);
            }
        }
    }
}
