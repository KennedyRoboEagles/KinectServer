/*
 * Kinect.h
 *
 *  Created on: Apr 6, 2015
 *      Author: nowireless
 */

#ifndef SRC_KINECT_H_
#define SRC_KINECT_H_

#include <tables/ITable.h>
#include <tables/ITableListener.h>

class Kinect : public ITableListener {
private:
	ITable *table;
	double lastValueUpdate;
public:
	enum Stick {
		Left,
		Right
	};

	Kinect();
	Kinect(ITable *table);
	virtual ~Kinect();

	double GetJoystickY(Stick side);
	bool IsAlive();
	int GetPlayerCount();

	void ValueChanged(ITable* source, const std::string& key, EntryValue value, bool isNew);
};

#endif /* SRC_KINECT_H_ */
