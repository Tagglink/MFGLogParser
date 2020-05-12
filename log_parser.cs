using System;
using System.Xml;
using System.IO;
using System.Globalization;

public enum LogState { DEMO, TRAIN, CHALLENGE };

public class LogParser {

  public static void Main(string[] args) {
    string outFilename = "";
    string header = "Attack ID," +
      "Joystick Type (1 = Conventional)," +
      "Input Gesture ID," +
      "Times Done this Input Gesture ID," +
      "Target Gesture ID," + 
      "Success," +
      "Time Spent";
      
    XmlDocument doc = new XmlDocument();
    doc.PreserveWhitespace = true;

    if (args.Length < 1) {
      Console.WriteLine("Usage: log_parser.exe UserLog*.xml [UserLog*.csv]");
    } else if (args.Length < 2) {
      outFilename = Path.GetFileNameWithoutExtension(args[0]) + ".csv";
      Console.WriteLine("Writing to default filename " + outFilename);
    } else {
      outFilename = args[1];
    }

    try { doc.Load(args[0]); }
    catch {
      Console.WriteLine("File not found!");
    }

    XmlNode root = doc.DocumentElement;
    XmlNodeList dataItems;
    LogParser parser = new LogParser();
    dataItems = root["LogDataItems"].ChildNodes;

    using (StreamWriter sw = new StreamWriter(outFilename)) {
      sw.WriteLine(header);
      foreach (XmlNode childNode in dataItems) {
        parser.Parse(childNode, sw);
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

  void Parse(XmlNode currentNode, StreamWriter sw) {
    XmlNode typeNode = currentNode["logType"];
    if (typeNode == null) return;
    string type = typeNode.InnerText;
    if (type == "ChangedMove") {
      ParseChangedMove(currentNode, sw);
    } else if (type == "AttackClick") {
      ParseAttackClick(currentNode, sw);
    } else if (type == "StartedTesting") {
      ParseStartedTesting(sw);
    // writing "Challange" instead of "Challenge" below is an intentional typo
    } else if (type == "StartedChallange") {
      ParseStartedChallenge(sw);
    } else if (type == "StoppedTesting") {
      ParseStoppedTesting(sw);
    } else if (type == "StoppedChallange") {
      ParseStoppedChallenge(sw);
    }

  }

  void ParseStartedTesting(StreamWriter sw) {
    sw.WriteLine("======== LOGGING START ========");
  }

  void ParseStartedChallenge(StreamWriter sw) {
    sw.WriteLine("======== CHALLENGE START ==========");
  }

  void ParseStoppedTesting(StreamWriter sw) {
    sw.WriteLine("======== LOGGING END ========");
  }

  void ParseStoppedChallenge(StreamWriter sw) {
    sw.WriteLine("======== CHALLENGE END =========");
  }

  void ParseChangedMove(XmlNode node, StreamWriter sw) {
    // Note: messages use capital S in "timeStamp" node
    XmlNode timestampNode = node["timeStamp"];
    if (timestampNode == null) return;
    m_timeSetTargetMoveLast = Convert.ToSingle(timestampNode.InnerText, usCulture);
  }

  void ParseAttackClick(XmlNode node, StreamWriter sw) {
    XmlNode gestureLogDataNode = node["gestureLogData"];
    if (gestureLogDataNode == null) return;

    string id = gestureLogDataNode["id"].InnerText;
    string conventionalJoystick = gestureLogDataNode["conventionalJoystick"].InnerText;
    string targetGesture = gestureLogDataNode["targetGesture"].InnerText;
    string inputGesture = gestureLogDataNode["inputGesture"].InnerText;
    // Note: attack clicks use lower case s in "timestamp" node
    string timestamp = gestureLogDataNode["timestamp"].InnerText;
    
    float timestamp_single = Convert.ToSingle(timestamp, usCulture);
    string success = LogParser.GetSuccessValue(inputGesture, targetGesture);
    string timeSpent = LogParser.GetTimeSpent(
      timestamp_single, m_timeSetTargetMoveLast);
    string joystickType = LogParser.GetJoystickType(conventionalJoystick);
    string attacksDone = "-1";
    
    int targetGestureID = Convert.ToInt32(targetGesture);
    int inputGestureID = Convert.ToInt32(inputGesture);
    float t = Convert.ToSingle(timeSpent, usCulture);
    if (targetGestureID >= 0) {
      m_attempts[targetGestureID]++;
      m_totalTimeSpent[targetGestureID] += t;
    }
    if (inputGestureID >= 0) {
      m_attacksDone[inputGestureID]++;
      attacksDone = m_attacksDone[inputGestureID].ToString();
    }
    if (success == "0") {
      m_failures[targetGestureID]++;
    }

    string line = id + ",";
    line += joystickType + ",";
    line += inputGesture + ",";
    line += attacksDone + ",";
    line += targetGesture + ",";
    line += success + ",";
    line += timeSpent;
    sw.WriteLine(line);
  }

  void WriteResults(StreamWriter sw) {
    float[] averageTimeSpent = new float[4];
    float[] averageFailures = new float[4];
    string header = "Attack,Average Time Spent,Average Failures";
    sw.WriteLine(header);

    for (int i = 0; i < 4; i++) {
      averageTimeSpent[i] = (float)m_totalTimeSpent[i] / m_attempts[i];
      averageFailures[i] = (float)m_failures[i] / m_attempts[i];

      string line = i.ToString() + ",";
      line += averageTimeSpent[i].ToString(usCulture) + ",";
      line += averageFailures[i].ToString(usCulture);

      sw.WriteLine(line);
    }
  }

  static string GetTimeSpent(float timestamp, float timeSetTargetMoveLast) {
    float t = timestamp - timeSetTargetMoveLast;
    return t.ToString(usCulture);
  }

  static string GetJoystickType(string conventionalJoystick) {
    if (conventionalJoystick == "true") {
      return "1";
    } else {
      return "0";
    }
  }

  static string GetSuccessValue(string inputGesture, string targetGesture) {
    if (targetGesture == "-1") {
      return "-1";
    } else if (targetGesture == inputGesture) {
      return "1";
    } else {
      return "0";
    }
  }

  

}
