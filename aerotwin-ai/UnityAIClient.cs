// AeroTwin XR - Unity side (C#)
// Attach this to a GameObject. Call SendReading(...) with the engine's
// current sensor values; it asks the Python prediction service for a
// verdict and gives you back the trainee message + robot command.
//
// Requires the prediction service running:  python prediction_service.py

using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AeroTwinAIClient : MonoBehaviour
{
    private const string URL = "http://127.0.0.1:5000/predict";

    [System.Serializable]
    public class SensorReading
    {
        public int Type;
        public float Air_temperature;
        public float Process_temperature;
        public float Rotational_speed;
        public float Torque;
        public float Tool_wear;
    }

    // Call this whenever you want a prediction (e.g. every second of the scenario)
    public void SendReading()
    {
        // Build the JSON exactly the way the Python service expects it.
        string json =
            "{\"Type\":1,\"Air temperature [K]\":298.1,\"Process temperature [K]\":308.6," +
            "\"Rotational speed [rpm]\":1551,\"Torque [Nm]\":42.8,\"Tool wear [min]\":10}";
        StartCoroutine(PostReading(json));
    }

    private IEnumerator PostReading(string json)
    {
        using (UnityWebRequest req = new UnityWebRequest(URL, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string response = req.downloadHandler.text;
                Debug.Log("AI response: " + response);
                // response contains: status, confidence, trainee_message, robot_command
                // -> show trainee_message in the chatbot UI
                // -> send robot_command to the robot controller
            }
            else
            {
                Debug.LogError("AI service error: " + req.error);
            }
        }
    }
}
