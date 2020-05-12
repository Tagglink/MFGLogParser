# Log Parser Specification

The log file contains the following:<br>
logID: (int)<br>
deviceType: Handheld<br>
date: yyyymmddhhmmss<br>
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


END: LogDataItems<br>
START: inputBufferTouchLogs
* InputBufferTouchLog:
  * globalTimestamp: (seconds from start of game)
  * inputID
  * touchPositions: (SerializedPosition[])
    * SerializedPosition:
      * x: (int)
      * y: (int)


END: inputBufferTouchLogs

Gesture IDs:<br>
-1 = Null gesture<br>
0 = DownUp<br>
1 = Hadouken<br>
2 = Left<br>
3 = Shoryuken<br>

Direction IDs:<br>
0 = RIGHT<br>
... clockwise<br>
8 = NEUTRAL<br>

Log types:<br>
StartedTesting (when INIT is pressed)<br>
StartedChallange (when START is pressed)<br>
StoppedTesting (when measuring stops (happens when a challenge is complete))<br>
StoppedChallange (when a challenge is stopped (message contains which challenge))<br>
ChangedJoystick (when the joystick is changed)<br>
AttackClick (a click of the attack button)<br>

## What we need for the dissertation
For each attack;
1. average failed attempts of the attack
2. average time spent on the attack
3. some kind of mean deviation of the finger position 

## What this program should do
1. List the following in a CSV format:<br>
    Attack Click ID;Joystick Type;Target Gesture ID;Success;Time Spent
2. Calculate the following and output it in CSV format:<br>
    Target Gesture ID;Mean Area of Screen used;Joystick Type

## Known errors
The prototype had an off-by-one error at the time of testing. This meant that the final attack was never recorded. This was always a Shoryuken.


