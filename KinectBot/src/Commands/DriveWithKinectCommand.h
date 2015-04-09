#ifndef DriveWithKinectCommand_H
#define DriveWithKinectCommand_H

#include "../CommandBase.h"
#include "WPILib.h"

class DriveWithKinectCommand: public CommandBase
{
public:
	DriveWithKinectCommand();
	void Initialize();
	void Execute();
	bool IsFinished();
	void End();
	void Interrupted();
};

#endif
