"""
AeroTwin XR - Fault TYPE Identification
Random Forest that identifies WHICH fault is present (not just healthy/failure).

Dataset: AI4I 2020 Predictive Maintenance Dataset (UCI)
Author: TechNest Team - Amman Arab University
"""

import pandas as pd
from sklearn.model_selection import train_test_split
from sklearn.ensemble import RandomForestClassifier
from sklearn.metrics import accuracy_score, classification_report
import joblib

df = pd.read_csv("ai4i2020.csv")
df["Type"] = df["Type"].map({"L": 0, "M": 1, "H": 2})

# Build a multi-class label: Healthy, or the specific fault type.
fault_cols = ["TWF", "HDF", "PWF", "OSF", "RNF"]
names = {
    "TWF": "Tool Wear Failure",
    "HDF": "Heat Dissipation Failure",
    "PWF": "Power Failure",
    "OSF": "Overstrain Failure",
    "RNF": "Random Failure",
}


def label_row(r):
    if r["Machine failure"] == 0:
        return "Healthy"
    for c in fault_cols:
        if r[c] == 1:
            return names[c]
    return "Unspecified Failure"


df["fault"] = df.apply(label_row, axis=1)

features = ["Type", "Air temperature [K]", "Process temperature [K]",
            "Rotational speed [rpm]", "Torque [Nm]", "Tool wear [min]"]
X, y = df[features], df["fault"]

X_train, X_test, y_train, y_test = train_test_split(
    X, y, test_size=0.2, random_state=42, stratify=y)

model = RandomForestClassifier(
    n_estimators=300, class_weight="balanced", random_state=42, n_jobs=-1)
model.fit(X_train, y_train)

pred = model.predict(X_test)
print(f"Multi-class accuracy: {accuracy_score(y_test, pred) * 100:.2f}%")
print(classification_report(y_test, pred, digits=2, zero_division=0))

joblib.dump(model, "fault_type_model.pkl")
print("Saved -> fault_type_model.pkl")
