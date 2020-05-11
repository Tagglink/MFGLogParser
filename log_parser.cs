using System;
using System.Xml;
using System.IO;
using System.Globalization;

public class LogParser {

  public static void Main(string[] args) {
    string outFilename = "";
    string header = "Attack ID," +
      "Joystick Type (1 = Conventional)," +
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
    }
  }

  static CultureInfo usCulture = new CultureInfo("en-US");

  float m_timeSetTargetMoveLast = 0f;

  void Parse(XmlNode currentNode, StreamWriter sw) {
    XmlNode typeNode = currentNode["logType"];
    if (typeNode == null) return;
    string type = typeNode.InnerText;
    if (type == "ChangedMove") {
      ParseChangedMove(currentNode, sw);
    } else if (type == "AttackClick") {
      ParseAttackClick(currentNode, sw);
    }
  }

  void ParseChangedMove(XmlNode node, StreamWriter sw) {
    // Note: messages use capital S in "timeStamp" node
    XmlNode timestampNode = node["timeStamp"];
    if (timestampNode == null) return;
    m_timeSetTargetMoveLast = Convert.ToSingle(timestampNode.InnerText, usCulture);
  }

  void ParseAttackClick(XmlNode node, StreamWriter sw) {
    // Note: attack clicks use lower case s in "timestamp" node
    XmlNode gestureLogDataNode = node["gestureLogData"];
    if (gestureLogDataNode == null) return;
    string line = gestureLogDataNode["id"].InnerText + ",";
    line += LogParser.GetJoystickType(gestureLogDataNode) + ",";
    line += gestureLogDataNode["targetGesture"].InnerText + ",";
    line += LogParser.GetSuccessValue(gestureLogDataNode) + ",";
    line += LogParser.GetTimeSpent(gestureLogDataNode, m_timeSetTargetMoveLast);
    sw.WriteLine(line);
  }

  static string GetTimeSpent(XmlNode node, float timeSetTargetMoveLast) {
    XmlNode timestampNode = node["timestamp"];
    if (timestampNode == null) return "";

    float timestamp = Convert.ToSingle(timestampNode.InnerText, usCulture);
    float t = timestamp - timeSetTargetMoveLast;
    return t.ToString(usCulture);
  }

  static string GetJoystickType(XmlNode attackClick) {
    string conventionalJoystick = attackClick["conventionalJoystick"].InnerText;
    if (conventionalJoystick == "true") {
      return "1";
    } else {
      return "0";
    }
  }

  static string GetSuccessValue(XmlNode attackClick) {
    string inputGesture = attackClick["inputGesture"].InnerText;
    string targetGesture = attackClick["targetGesture"].InnerText;
    if (targetGesture != "-1" && inputGesture == targetGesture) {
      return "1";
    } else {
      return "0";
    }
  }

  static int[] GetFails(XmlNodeList attacks) {
    int[] fails = new int[4];

    for (int i = 0; i < attacks.Count; i++) {
      XmlNode attempt = attacks[i];
      int inputGesture = Convert.ToInt32(
        attempt["gestureLogData"]["inputGesture"].InnerText
	);
      int targetGesture = Convert.ToInt32(
        attempt["gestureLogData"]["targetGesture"].InnerText
	);
      if (targetGesture >= 0 && inputGesture != targetGesture) {
        fails[targetGesture]++;
      }
    }
    return fails;
  }


}
