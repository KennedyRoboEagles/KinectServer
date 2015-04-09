/*
 * Kinect.cpp
 *
 *  Created on: Apr 6, 2015
 *      Author: nowireless
 */

#include <Kinect.h>
#include <networktables/NetworkTable.h>
#include <Timer.h>
#include <DriverStation.h>

const double TIME_OUT = 0.5;

Kinect::Kinect() {
	this->table = NetworkTable::GetTable("kinect");
	this->table->AddTableListener(this);
	this->lastValueUpdate = Timer::GetFPGATimestamp();
}

Kinect::Kinect(ITable *table) {
	this->table = table;
	this->table->AddTableListener(this);
	this->lastValueUpdate = Timer::GetFPGATimestamp();
}

Kinect::~Kinect() {}

bool Kinect::IsAlive() {
	return (Timer::GetFPGATimestamp() - this->lastValueUpdate) < TIME_OUT;
}

int Kinect::GetPlayerCount() {
	return (int) this->table->GetNumber("PlayerCount");
}

double Kinect::GetJoystickY(Stick side) {
	double y = 0.0;
	if(this->IsAlive()) {
		if(this->GetPlayerCount() >= 1) {
			switch (side) {
				case Left:
					y = this->table->GetNumber("Joy1Y");
					break;
				case Right:
					y = this->table->GetNumber("Joy2Y");
					break;
				default:
					y = 0.0;
					break;
			}

			y = y / 128.0;
		}
	} else {
		DriverStation::GetInstance()->ReportError("Kinect Server Timed Out");
	}
	return y;
}

void Kinect::ValueChanged(ITable* source, const std::string& key, EntryValue value, bool isNew) {
	if(key == "HeartBeat") {
		printf("Heartbeat Updated\n");
		this->lastValueUpdate = Timer::GetFPGATimestamp();
	}
}
