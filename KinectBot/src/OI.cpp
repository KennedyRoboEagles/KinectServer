#include "OI.h"

OI::OI()
{
	// Process operator interface input here.
	this->kinect = new Kinect();
}

Kinect *OI::GetKinect() {
	return this->kinect;
}
