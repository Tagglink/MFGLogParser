using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Globalization;
using System.Linq;

public enum LogState { DEMO, TRAIN, CHALLENGE };

public struct Vector2
{
    public float x;
    public float y;
}

public struct InputPositionLog
{
    public float timestamp;
    public int inputID;
    public Vector2 joystickFingerPos;
}

public struct ChallengeData
{
    public ChallengeData(string name = "") {
        attempts = new int[4];
	failures = new int[4];
	attacksDone = new int[4];
	totalTimeSpent = new float[4];
	totalDistance = new double[4];
	totalArea = new double[4];

	this.name = name;
    }

    public int[] attempts;
    public int[] failures;
    public int[] attacksDone;
    public float[] totalTimeSpent;
    public double[] totalDistance;
    public double[] totalArea;
    public string name;
}

public class LogParser
{
    public static void Main(string[] args)
    {
        string[] logfiles;
	string[] csvfiles;
	string indexname = "";
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: log_parser.exe logdir csvdir");
	    return;
        }
	else if (Directory.Exists(args[1]))
	{
	    try { 
	        logfiles = Directory.EnumerateFiles(args[0], "*.xml").ToArray();
            } catch (Exception e) {
	        Console.WriteLine(e.Message);
		return;
            }
	    if (logfiles.Length <= 0) {
	        Console.WriteLine("No log files found in directory.");
		return;
            }
	}
	else {
	    Console.WriteLine("CSV directory does not exist.");
	    return;
	}

	csvfiles = new string[logfiles.Length];
	List<ChallengeData> smallChallengeData = new List<ChallengeData>();
	List<ChallengeData> bigChallengeData = new List<ChallengeData>();
        
	for (int i = 0; i < logfiles.Length; i++) {
	    csvfiles[i] = args[1] + 
	        Path.GetFileNameWithoutExtension(logfiles[i]) + 
		".csv";
	    Console.WriteLine(csvfiles[i]);

	    LogParser parser = new LogParser();
	    parser.ParseLog(logfiles[i], csvfiles[i]);
	    ChallengeData[] chData = parser.GetChallengeData();
	    smallChallengeData.Add(chData[0]);
	    bigChallengeData.Add(chData[1]);
	}
        
        indexname = Path.GetDirectoryName(csvfiles[0]) + "/index.csv";

        using (StreamWriter sw = new StreamWriter(indexname)) {
	    sw.WriteLine("--------Small Joystick Total Averages--------");
	    LogParser.WriteTotalAverages(smallChallengeData.ToArray(), sw);
	    sw.WriteLine("--------Big Joystick Total Averages--------");
	    LogParser.WriteTotalAverages(bigChallengeData.ToArray(), sw);
	}
    }

    static CultureInfo usCulture = new CultureInfo("en-US");

    float m_timeSetTargetMoveLast = 0f;
    ChallengeData[] m_chData = new ChallengeData[2];

    LogParser() {
        m_chData[0] = new ChallengeData("Small Joystick");
	m_chData[1] = new ChallengeData("Big Joystick");
    }

    void ParseLog(string inFilename, string outFilename) {
        string header = "Attack ID," +
          "Joystick Type (1 = Conventional)," +
          "Input Gesture ID," +
          "Times Done this Input Gesture ID," +
          "Target Gesture ID," +
          "Success," +
          "Time Spent," +
          "Total Distance," +
          "Total Area";

        XmlDocument doc = new XmlDocument();
        doc.PreserveWhitespace = true;

        try { doc.Load(inFilename); }
        catch
        {
            Console.WriteLine("File not found!");
        }

        XmlNode root = doc.DocumentElement;
        XmlNodeList dataItems;
        XmlNodeList positionDataItems;

        dataItems = root["LogDataItems"].ChildNodes;
        positionDataItems = root["inputBufferTouchLogs"].SelectNodes("InputBufferTouchLog");

        using (StreamWriter sw = new StreamWriter(outFilename))
        {
            List<InputPositionLog> positionLogs = new List<InputPositionLog>();
            foreach (XmlNode childNode in positionDataItems)
            {
                InputPositionLog ipl = GetInputPositionLog(childNode);
                if (ipl.timestamp > 0)
                {
                    positionLogs.Add(ipl);
                }
            }

            InputPositionLog[] touchPositionLogs = positionLogs.ToArray();

            sw.WriteLine(header);
            foreach (XmlNode childNode in dataItems)
            {
                ParseNode(childNode, sw, touchPositionLogs);
            }

	    WriteResult(sw);
        }
    }

    ChallengeData[] GetChallengeData() {
        return m_chData;
    }

    void ParseNode(XmlNode currentNode, StreamWriter sw, InputPositionLog[] touchPositionLogs)
    {
        XmlNode typeNode = currentNode["logType"];
        if (typeNode == null)
        {
            return;
        }

        string type = typeNode.InnerText;
        if (type == "ChangedMove")
        {
            ParseChangedMove(currentNode, sw);
        }
        else if (type == "AttackClick")
        {
            ParseAttackClick(currentNode, sw, touchPositionLogs);
        }
        else if (type == "StartedTesting")
        {
            ParseStartedTesting(sw);
            // writing "Challange" instead of "Challenge" below is an intentional typo
        }
        else if (type == "StartedChallange")
        {
            ParseStartedChallenge(sw);
        }
        else if (type == "StoppedTesting")
        {
            ParseStoppedTesting(sw);
        }
        else if (type == "StoppedChallange")
        {
            ParseStoppedChallenge(sw);
        }
    }

    void ParseStartedTesting(StreamWriter sw)
    {
        sw.WriteLine("--------LOGSTART--------");
    }

    void ParseStartedChallenge(StreamWriter sw)
    {
        sw.WriteLine("--------CHALLENGESTART--------");
    }

    void ParseStoppedTesting(StreamWriter sw)
    {
        sw.WriteLine("--------LOGEND--------");
    }

    void ParseStoppedChallenge(StreamWriter sw)
    {
        sw.WriteLine("--------CHALLENGEEND--------");
    }

    void ParseChangedMove(XmlNode node, StreamWriter sw)
    {
        // Note: messages use capital S in "timeStamp" node
        XmlNode timestampNode = node["timeStamp"];
        if (timestampNode == null) return;
        m_timeSetTargetMoveLast = Convert.ToSingle(timestampNode.InnerText, usCulture);
    }

    void ParseAttackClick(XmlNode node, StreamWriter sw, InputPositionLog[] touchPositionLogs)
    {
        XmlNode gestureLogDataNode = node["gestureLogData"];
        if (gestureLogDataNode == null) return;

        string id = gestureLogDataNode["id"].InnerText;
        string conventionalJoystick = gestureLogDataNode["conventionalJoystick"].InnerText;
        string targetGesture = gestureLogDataNode["targetGesture"].InnerText;
        string inputGesture = gestureLogDataNode["inputGesture"].InnerText;
        // Note: attack clicks use lower case s in "timestamp" node
        string timestamp = gestureLogDataNode["timestamp"].InnerText;
        string globalTimestamp = gestureLogDataNode["globalTimestamp"].InnerText;

        float timestamp_single = Convert.ToSingle(timestamp, usCulture);
        string success = LogParser.GetSuccessValue(inputGesture, targetGesture);
        string timeSpent = LogParser.GetTimeSpent(timestamp_single, m_timeSetTargetMoveLast);
        string joystickType = LogParser.GetJoystickType(conventionalJoystick);
	int joystickID = Convert.ToInt32(joystickType);
        string attacksDone = "-1";

        int targetGestureID = Convert.ToInt32(targetGesture);
        int inputGestureID = Convert.ToInt32(inputGesture);

        float globalTimestamp_single = Convert.ToSingle(globalTimestamp, usCulture);
        // Touch area.
        List<Vector2> positions = new List<Vector2>();
        for (int i = 0; i < touchPositionLogs.Length; i++)
        {
            // We look back 1/3 of a second because in the game the gesture interpreter looks back 10 frames. The game is supposed to run at 30 fps, which makes 10 frames 1/3 of a second (without lag).
            if (touchPositionLogs[i].timestamp <= globalTimestamp_single &&
                  touchPositionLogs[i].timestamp >= (globalTimestamp_single - (1f / 3f)))
            {
                positions.Add(touchPositionLogs[i].joystickFingerPos);
            }
        }

        // We calculate the total distance BEFORE sorting because we want the actual distance the finger travelled and not some arbitrary route.
        double totalDistance = CalculateTotalDistanceFromPoints(positions.ToArray());
    
        // Get the center position of all the points.
        Vector2 center = GetCenterFromPoints(positions.ToArray());

        // This function for calculating the area of irregular polygons requires the array of positions to be sorted clockwise.
        positions.Sort((a, b) => SortCornersClockwiseCenter(a, b, center));
        double totalArea = CalculatePolygonArea(positions.ToArray());
        
        float t = Convert.ToSingle(timeSpent, usCulture);
        if (targetGestureID >= 0)
        {
            m_chData[joystickID].attempts[targetGestureID]++;
	    m_chData[joystickID].totalDistance[targetGestureID] += totalDistance;
	    m_chData[joystickID].totalArea[targetGestureID] += totalArea;
        }
        if (inputGestureID >= 0)
        {
            m_chData[joystickID].attacksDone[inputGestureID]++;
            attacksDone = m_chData[joystickID].attacksDone[inputGestureID].ToString();
        }
        if (success == "0")
        {
            m_chData[joystickID].failures[targetGestureID]++;
        }
        else if (success == "1")
        {
            m_chData[joystickID].totalTimeSpent[targetGestureID] += t;
        }

        // Final print
        string line = id + ",";
        line += joystickType + ",";
        line += inputGesture + ",";
        line += attacksDone + ",";
        line += targetGesture + ",";
        line += success + ",";
        line += timeSpent + ",";
        line += totalDistance.ToString(usCulture) + ",";
        line += totalArea.ToString(usCulture);
        sw.WriteLine(line);
    }

    void WriteResult(StreamWriter sw) {
        foreach (ChallengeData chData in m_chData) {
	    WriteJoystickResult(chData, sw);
	}	
	sw.WriteLine("--------Total Averages--------");
	WriteTotalAverages(m_chData, sw);
    }

    void WriteJoystickResult(ChallengeData chData, StreamWriter sw)
    {
        float[] averageTimeSpent = new float[4];
	float[] averageFailures = new float[4];
	double[] averageDistance = new double[4];
	double[] averageArea = new double[4];
        string header = "Attack,Average Time Spent,Average Failures,Average Distance,Average Area";
	sw.WriteLine("--------" + chData.name + "--------");
        sw.WriteLine(header);

        for (int i = 0; i < 4; i++)
        {
	    int attackID = LogParser.ReorderAttackID(i);
            averageTimeSpent[attackID] = chData.totalTimeSpent[attackID] / (
	        chData.attempts[attackID] - chData.failures[attackID]
		);
            averageFailures[attackID] = (float)chData.failures[attackID] / chData.attempts[attackID];
	    averageDistance[attackID] = chData.totalDistance[attackID] / chData.attempts[attackID];
	    averageArea[attackID] = chData.totalArea[attackID] / chData.attempts[attackID];

            string line = LogParser.AttackIDToName(attackID) + ",";
            line += averageTimeSpent[attackID].ToString(usCulture) + ",";
            line += averageFailures[attackID].ToString(usCulture) + ",";
	    line += averageDistance[attackID].ToString(usCulture) + ",";
	    line += averageArea[attackID].ToString(usCulture);
            sw.WriteLine(line);
        }
    }

    static void WriteTotalAverages(ChallengeData[] chData, StreamWriter sw) {
        string header = "Attack,Average Time Spent,Average Failures,Average Distance,Average Area";
	sw.WriteLine(header);
	for (int i = 0; i < 4; i++) {
	    int attackID = LogParser.ReorderAttackID(i);
	    int attempts = 0;
	    int failures = 0;
	    int successes = 0;
	    float timeSpent = 0f;
	    double distance = 0.0;
	    double area = 0.0;

	    for (int subjectID = 0; subjectID < chData.Length; subjectID++) {
	        int subjectSuccesses = chData[subjectID].attempts[attackID] - chData[subjectID].failures[attackID];
		if (subjectSuccesses > 0) {
		    // When calculating the total number of successes we must avoid cases where
		    // the subject did not succeed impacting the averages as negative values.
		    // (Remember that totalTimeSpent == 0 when a subject skips an attack)
		    successes += subjectSuccesses;
		}
	        attempts += chData[subjectID].attempts[attackID];
		failures += chData[subjectID].failures[attackID];
		timeSpent += chData[subjectID].totalTimeSpent[attackID];
		distance += chData[subjectID].totalDistance[attackID];
		area += chData[subjectID].totalArea[attackID];
	    }

	    float averageTimeSpent = timeSpent / successes;
	    float averageFailures = (float)failures / attempts;
	    double averageDistance = distance / attempts;
	    double averageArea = area / attempts;

	    string line = LogParser.AttackIDToName(attackID) + ",";
	    line += averageTimeSpent.ToString(usCulture) + ",";
            line += averageFailures.ToString(usCulture) + ",";
	    line += averageDistance.ToString(usCulture) + ",";
	    line += averageArea.ToString(usCulture);
	    sw.WriteLine(line);
	}
    }

    static int ReorderAttackID(int attackID) {
        switch (attackID) {
	    case 0: return 2;
	    case 1: return 0;
	    case 2: return 1;
	    case 3: return 3;
	    default: return -1;
	}
    }

    static string AttackIDToName(int attackID) {
        switch (attackID) {
	    case 0: return "DownUp";
	    case 1: return "Hadouken";
	    case 2: return "Left";
	    case 3: return "Shoryuken";
	    default: return "Null gesture";
	}
    }

    static InputPositionLog GetInputPositionLog(XmlNode currentNode)
    {
        InputPositionLog result = new InputPositionLog();

        XmlNode timestampNode = currentNode["globalTimestamp"];
        if (timestampNode == null)
        {
            result.timestamp = -1;
            return result;
        }

        result.timestamp = Convert.ToSingle(timestampNode.InnerText, usCulture);
        result.inputID = Convert.ToInt32(currentNode["inputID"].InnerText);

        Vector2 pos = new Vector2();
        XmlNodeList positions = currentNode["touchPositions"].ChildNodes;
        foreach (XmlNode childNode in positions)
        {
            XmlNode x = childNode["x"];
            XmlNode y = childNode["y"];
            if (x != null && y != null)
            {
                float posX = Convert.ToSingle(x.InnerText, usCulture);
                float posY = Convert.ToSingle(y.InnerText, usCulture);

                if (posX > 1000 || posY > 1000)
                {
                    continue;
                }

                pos.x = posX;
                pos.y = posY;
            }
        }

        result.joystickFingerPos = pos;

        return result;
    }


    static string GetTimeSpent(float timestamp, float timeSetTargetMoveLast)
    {
        float t = timestamp - timeSetTargetMoveLast;
        return t.ToString(usCulture);
    }

    static string GetJoystickType(string conventionalJoystick)
    {
        if (conventionalJoystick == "true")
        {
            return "1";
        }
        else
        {
            return "0";
        }
    }

    static string GetSuccessValue(string inputGesture, string targetGesture)
    {
        if (targetGesture == "-1")
        {
            return "-1";
        }
        else if (targetGesture == inputGesture)
        {
            return "1";
        }
        else
        {
            return "0";
        }
    }

    static double CalculateDistance(Vector2 a, Vector2 b)
    {
        return Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
    }

    static double CalculateTotalDistanceFromPoints(Vector2[] points)
    {
        double totalDistance = 0f;
        for (int i = 0; i < points.Length - 1; i++)
        {
            totalDistance += CalculateDistance(points[i], points[i + 1]);
        }

        return totalDistance;
    }

    static int SortCornersClockwiseCenter(Vector2 A, Vector2 B, Vector2 center)
    {
        //  Variables to Store the atans
        double aTanA, aTanB;

        //  Fetch the atans, (256, 256) is the center of the joystick.
        aTanA = Math.Atan2(A.y - center.y, A.x - center.x);
        aTanB = Math.Atan2(B.y - center.y, B.x - center.x);

        //  Determine next point in Clockwise rotation
        if (aTanA > aTanB) return -1;
        else if (aTanA < aTanB) return 1;
        return 0;
    }

    static double CalculateAngleFromVector(Vector2 vert, Vector2 center)
    {
        return Math.Atan2(vert.y - center.y, vert.x - center.x);
    }

    static Vector2 GetCenterFromPoints(Vector2[] points)
    {
        float sumX = 0f;
        float sumY = 0f;

        for (int i = 0; i < points.Length; i++)
        {
            sumX += points[i].x;
            sumY += points[i].y;
        }

        Vector2 result = new Vector2();
        result.x = sumX / points.Length;
        result.y = sumY / points.Length;

        return result;
    }

    static double CalculatePolygonArea(Vector2[] points)
    {
        double area = 0;   // Accumulates area 
        int j = points.Length - 1;

        for (int i = 0; i < points.Length; i++)
        {
            area += (points[j].x + points[i].x) * (points[j].y - points[i].y);
            j = i;  //j is previous vertex to i
        }

        return area / 2;
    }
}
