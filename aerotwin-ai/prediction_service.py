"""
AeroTwin XR - Prediction Service (the bridge)
==============================================
This is the link between the AI model and the rest of the system.

Flow:
  Unity simulation sends a sensor reading  ->  this service
  this service runs the trained Random Forest model
  -> returns: status + confidence
            + a MESSAGE for the trainee (the AI assistant / chatbot says it)
            + a COMMAND for the robot (what the robot should do)

Run it:   python prediction_service.py
Then the Unity sim (C#) POSTs sensor readings to  http://127.0.0.1:5000/predict
"""

from flask import Flask, request, jsonify
import joblib
import pandas as pd

app = Flask(__name__)

# Load the trained model once at startup.
model = joblib.load("fault_model.pkl")

FEATURES = [
    "Type",
    "Air temperature [K]",
    "Process temperature [K]",
    "Rotational speed [rpm]",
    "Torque [Nm]",
    "Tool wear [min]",
]


def build_trainee_message(status, confidence, reading):
    """The AI assistant / chatbot turns the prediction into words for the trainee."""
    if status == "HEALTHY":
        return (f"Engine readings are within the normal range "
                f"(confidence {confidence}%). You can safely continue the inspection.")
    # failure case: point the trainee at the most relevant signals
    return (f"Warning: the model predicts a fault risk (confidence {confidence}%). "
            f"Inspect the engine now. Key signals - "
            f"Torque: {reading['Torque [Nm]']} Nm, "
            f"Rotational speed: {reading['Rotational speed [rpm]']} rpm, "
            f"Tool wear: {reading['Tool wear [min]']} min.")


def build_robot_command(status):
    """The prediction decides what the assistant robot does next."""
    if status == "HEALTHY":
        return "CONTINUE_MONITORING"
    return "MOVE_TO_ENGINE_AND_INSPECT"


@app.route("/predict", methods=["POST"])
def predict():
    reading = request.get_json()                 # sensor reading from the sim
    row = pd.DataFrame([reading], columns=FEATURES)

    label = int(model.predict(row)[0])           # 0 = healthy, 1 = failure
    confidence = round(float(model.predict_proba(row)[0].max()) * 100, 1)
    status = "FAILURE" if label == 1 else "HEALTHY"

    return jsonify({
        "status": status,
        "confidence": confidence,
        "trainee_message": build_trainee_message(status, confidence, reading),
        "robot_command": build_robot_command(status),
    })


@app.route("/health", methods=["GET"])
def health():
    return jsonify({"service": "AeroTwin XR prediction service", "status": "running"})


if __name__ == "__main__":
    app.run(host="127.0.0.1", port=5000)
