#include "DriveWithKinectCommand.h"
#include "../Kinect.h"

const float SCALE = 0.5;

DriveWithKinectCommand::DriveWithKinectCommand()
{
	// Use Requires() here to declare subsystem dependencies
	// eg. Requires(chassis);
	Requires(chassis);
}

// Called just before this Command runs the first time
void DriveWithKinectCommand::Initialize()
{
	chassis->Stop();
}

// Called repeatedly when this Command is scheduled to run
void DriveWithKinectCommand::Execute()
{
	double left = oi->GetKinect()->GetJoystickY(Kinect::Left);
	double right = oi->GetKinect()->GetJoystickY(Kinect::Right);

	left *= SCALE;
	right *= SCALE;

	chassis->GetDrive()->TankDrive(left, right);

}

// Make this return true when this Command no longer needs to run execute()
bool DriveWithKinectCommand::IsFinished()
{
	return false;
}

// Called once after isFinished returns true
void DriveWithKinectCommand::End()
{

}

// Called when another command which requires one or more of the same
// subsystems is scheduled to run
void DriveWithKinectCommand::Interrupted()
{

}
