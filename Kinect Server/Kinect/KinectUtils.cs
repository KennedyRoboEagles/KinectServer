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
using Microsoft.Kinect;

namespace Edu.FIRST.WPI.Kinect.KinectServer.Kinect
{
    class KinectUtils
    {
        /// <summary>
        /// Counts the number of actively tracked skeletons at any given time.
        /// </summary>
        /// <param name="skeletons">The array of skeletons provided by the Kinect sensor.</param>
        /// <returns>The number of skeletons which are being actively tracked.</returns>
        public static ushort CountTrackedSkeletons(Skeleton[] skeletons)
        {
            ushort count = 0;

            foreach (Skeleton skeleton in skeletons)
            {
                if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Selects the closest skeleton. Prefers actively tracked skeletons over passively tracked skeletons.
        /// </summary>
        /// <param name="skeletons">The array of skeletons provided by the Kinect sensor.</param>
        /// <returns>The SkeletonData object for the skeleton closest to the sensor.</returns>
        public static Skeleton SelectBestSkeleton(Skeleton[] skeletons)
        {
            Skeleton bestSkeleton = skeletons[0];

            foreach (Skeleton skeleton in skeletons)
            {
                if (skeleton.TrackingState == SkeletonTrackingState.Tracked && bestSkeleton.TrackingState != SkeletonTrackingState.Tracked ||
                    (skeleton.TrackingState == bestSkeleton.TrackingState && skeleton.Position.Z < bestSkeleton.Position.Z))
                    bestSkeleton = skeleton;
            }

            return bestSkeleton;
        }

    }
}
