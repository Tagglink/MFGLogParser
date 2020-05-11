# Log Parser Specification

The log file contains the following:
logID: (int)
deviceType: Handheld
date: yyyymmddhhmmss
START: LogDataItems
* AttackClickData:
  * message: Gesture Attempt
  * logType: AttackClick
  * globalTimeStamp: (seconds)
  * gestureLogData:
    * id: (ordered id)
    * conventionalJoystick: (bool)
    * inputGesture: (Gesture ID)
    * targetGesture: (Gesture ID)
    * globalTimestamp: (seconds from start of game)
    * timestamp: (seconds from FIRST press of "init")
    * inputBuffer: (Direction ID[10])
* MessageData:
  * message
  * logType
  * globalTimeStamp: (seconds from start of game)
  * timeStamp: (seconds from FIRST press of "init")
END: LogDataItems
START: inputBufferTouchLogs
* InputBufferTouchLog:
  * globalTimestamp: (seconds from start of game)
  * inputID
  * touchPositions: (SerializedPosition[])
    * SerializedPosition:
      * x: (int)
      * y: (int)
END: inputBufferTouchLogs

Gesture IDs:
-1 = Null gesture
0 = DownUp
1 = Hadouken
2 = Left
3 = Shoryuken

Direction IDs:
0 = RIGHT
... clockwise
8 = NEUTRAL

Log types:
StartedTesting (when INIT is pressed)
StartedChallange (when START is pressed)
StoppedTesting (when measuring stops (happens when a challenge is complete))
StoppedChallange (when a challenge is stopped (message contains which challenge))
ChangedJoystick (when the joystick is changed)
AttackClick (a click of the attack button)

## What we need for the dissertation
For each attack;
1. average failed attempts of the attack
2. average time spent on the attack
3. some kind of mean deviation of the finger position 

## What this program should do
1. List the following in a CSV format:
    Attack Click ID;Joystick Type;Target Gesture ID;Success;Time Spent
2. Calculate the following and output it in CSV format:
    Target Gesture ID;Mean Area of Screen used;Joystick Type

## Known errors
The prototype had an off-by-one error at the time of testing. This meant that the final attack was never recorded. This was always a Shoryuken.


