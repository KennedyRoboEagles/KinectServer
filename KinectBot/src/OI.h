#ifndef OI_H
#define OI_H

#include "WPILib.h"
#include "Kinect.h"

class OI
{
private:
	Kinect *kinect;
public:
	OI();
	Kinect *GetKinect();
};

#endif
