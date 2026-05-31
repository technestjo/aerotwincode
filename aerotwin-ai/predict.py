"""
AeroTwin XR - Live Prediction Example
Loads the trained model and classifies a single sensor reading.

This is the bridge to the simulation: the Unity/C# sim sends a sensor
reading, this returns "Healthy" or "Failure" + a confidence score.
"""

import joblib
import pandas as pd

model = joblib.load("fault_model.pkl")

features = [
    "Type",
    "Air temperature [K]",
    "Process temperature [K]",
    "Rotational speed [rpm]",
    "Torque [Nm]",
    "Tool wear [min]",
]


def predict(reading: dict):
    """reading = one sensor snapshot. Returns label + confidence."""
    row = pd.DataFrame([reading], columns=features)
    label = model.predict(row)[0]
    confidence = model.predict_proba(row)[0].max()
    status = "FAILURE" if label == 1 else "HEALTHY"
    return status, round(confidence * 100, 1)


if __name__ == "__main__":
    # Example: a high-torque, worn-tool, high-temperature reading
    sample = {
        "Type": 0,
        "Air temperature [K]": 302.0,
        "Process temperature [K]": 311.0,
        "Rotational speed [rpm]": 1380,
        "Torque [Nm]": 65.0,
        "Tool wear [min]": 215,
    }
    status, conf = predict(sample)
    print(f"Prediction: {status}  (confidence {conf}%)")
