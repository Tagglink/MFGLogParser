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

public class LogParser
{
    public static void Main(string[] args)
    {
        string outFilename = "";
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

        if (args.Length < 1)
        {
            Console.WriteLine("Usage: log_parser.exe UserLog*.xml [UserLog*.csv]");
        }
        else if (args.Length < 2)
        {
            outFilename = Path.GetFileNameWithoutExtension(args[0]) + ".csv";
            Console.WriteLine("Writing to default filename " + outFilename);
        }
        else
        {
            outFilename = args[1];
        }

        try { doc.Load(args[0]); }
        catch
        {
            Console.WriteLine("File not found!");
        }

        XmlNode root = doc.DocumentElement;
        XmlNodeList dataItems;
        XmlNodeList positionDataItems;

        LogParser parser = new LogParser();
        dataItems = root["LogDataItems"].ChildNodes;
        positionDataItems = root["inputBufferTouchLogs"].SelectNodes("InputBufferTouchLog");

        using (StreamWriter sw = new StreamWriter(outFilename))
        {
            List<InputPositionLog> positionLogs = new List<InputPositionLog>();
            Console.WriteLine("xml count: " + positionDataItems.Count);
            foreach (XmlNode childNode in positionDataItems)
            {
                InputPositionLog ipl = GetInputPositionLog(childNode);
                if (ipl.timestamp > 0)
                {
                    positionLogs.Add(ipl);
                }
            }

            Console.WriteLine("COUNT:" + positionLogs.Count);

            InputPositionLog[] touchPositionLogs = positionLogs.ToArray();

            sw.WriteLine(header);
            foreach (XmlNode childNode in dataItems)
            {
                parser.Parse(childNode, sw, touchPositionLogs);
            }

            parser.WriteResults(sw);
        }
    }

    static CultureInfo usCulture = new CultureInfo("en-US");

    float m_timeSetTargetMoveLast = 0f;
    int[] m_failures = new int[4];
    int[] m_attempts = new int[4];
    int[] m_attacksDone = new int[4];
    float[] m_totalTimeSpent = new float[4];

    static InputPositionLog GetInputPositionLog(XmlNode currentNode)
    {
        InputPositionLog result = new InputPositionLog();

        //Console.Write(currentNode.Name + ", ");
        XmlNode timeStampNode = currentNode["globalTimestamp"];
        if (timeStampNode == null)
        {
            Console.WriteLine("timestamp node was null");
            result.timestamp = -1;
            return result;
        }

        //Console.WriteLine(currentNode.Name);

        result.timestamp = Convert.ToSingle(timeStampNode.InnerText, usCulture);
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

    void Parse(XmlNode currentNode, StreamWriter sw, InputPositionLog[] touchPositionLogs)
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
        sw.WriteLine("======== LOGGING START ========");
    }

    void ParseStartedChallenge(StreamWriter sw)
    {
        sw.WriteLine("======== CHALLENGE START ==========");
    }

    void ParseStoppedTesting(StreamWriter sw)
    {
        sw.WriteLine("======== LOGGING END ========");
    }

    void ParseStoppedChallenge(StreamWriter sw)
    {
        sw.WriteLine("======== CHALLENGE END =========");
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
        string timeSpent = LogParser.GetTimeSpent(
          timestamp_single, m_timeSetTargetMoveLast);
        string joystickType = LogParser.GetJoystickType(conventionalJoystick);
        string attacksDone = "-1";

        int targetGestureID = Convert.ToInt32(targetGesture);
        int inputGestureID = Convert.ToInt32(inputGesture);
        float t = Convert.ToSingle(timeSpent, usCulture);
        if (targetGestureID >= 0)
        {
            m_attempts[targetGestureID]++;
        }
        if (inputGestureID >= 0)
        {
            m_attacksDone[inputGestureID]++;
            attacksDone = m_attacksDone[inputGestureID].ToString();
        }
        if (success == "0")
        {
            m_failures[targetGestureID]++;
        }
        else if (success == "1")
        {
            m_totalTimeSpent[targetGestureID] += t;
        }

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

        //Console.Write("Count2: " + positions.Count + "; ");

        // We calculate the total distance BEFORE sorting because we want the actual distance the finger travelled and not some arbitrary route.
        double totalDistance = CalculateTotalDistanceFromPoints(positions.ToArray());
    
        // Get the center position of all the points.
        Vector2 center = GetCenterFromPoints(positions.ToArray());

        // This function for calculating the area of irregular polygons requires the array of positions to be sorted clockwise.
        positions.Sort((a, b) => SortCornersClockwiseCenter(a, b, center));
        double totalArea = CalculatePolygonArea(positions.ToArray());

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

    void WriteResults(StreamWriter sw)
    {
        float[] averageTimeSpent = new float[4];
        float[] averageFailures = new float[4];
        string header = "Attack,Average Time Spent,Average Failures";
        sw.WriteLine(header);

        for (int i = 0; i < 4; i++)
        {
            averageTimeSpent[i] = (float)m_totalTimeSpent[i] / m_attempts[i];
            averageFailures[i] = (float)m_failures[i] / m_attempts[i];

            string line = i.ToString() + ",";
            line += averageTimeSpent[i].ToString(usCulture) + ",";
            line += averageFailures[i].ToString(usCulture);

            sw.WriteLine(line);
        }
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
        if (aTanA < aTanB) return -1;
        else if (aTanA > aTanB) return 1;
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
