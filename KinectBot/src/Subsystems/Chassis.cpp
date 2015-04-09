#include "Chassis.h"
#include "../RobotMap.h"

Chassis::Chassis() : Subsystem("ExampleSubsystem")
{
	this->left = new Talon(CHASSIS_TALON_LEFT);
	this->right = new Talon(CHASSIS_TALON_RIGHT);
	this->drive = new RobotDrive(this->left, this->right);
	this->drive->SetSafetyEnabled(false);
}

void Chassis::InitDefaultCommand()
{
	// Set the default command for a subsystem here.
	//SetDefaultCommand(new MySpecialCommand());
}

// Put methods for controlling this subsystem
// here. Call these from Commands.

void Chassis::Stop() {
	this->drive->ArcadeDrive(0.0,0.0);
}

RobotDrive *Chassis::GetDrive() {
	return this->drive;
}
