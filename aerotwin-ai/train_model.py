"""
AeroTwin XR - Predictive Maintenance Module
Random Forest classifier for engine/machine fault detection.

Dataset: AI4I 2020 Predictive Maintenance Dataset (UCI, 10,000 samples)
Author: TechNest Team - Amman Arab University
"""

import pandas as pd
from sklearn.model_selection import train_test_split
from sklearn.ensemble import RandomForestClassifier
from sklearn.metrics import (accuracy_score, roc_auc_score,
                             classification_report, confusion_matrix)
import joblib

# 1) LOAD DATA -------------------------------------------------------------
df = pd.read_csv("ai4i2020.csv")

# 2) PREPROCESS ------------------------------------------------------------
# Encode machine quality (L/M/H) into numbers so the model can read it.
df["Type"] = df["Type"].map({"L": 0, "M": 1, "H": 2})

# Sensor inputs (features) the model learns from.
features = [
    "Type",
    "Air temperature [K]",
    "Process temperature [K]",
    "Rotational speed [rpm]",
    "Torque [Nm]",
    "Tool wear [min]",
]
X = df[features]
y = df["Machine failure"]          # 0 = healthy, 1 = failure

# 3) SPLIT -----------------------------------------------------------------
# stratify keeps the same healthy/failure ratio in train and test sets.
X_train, X_test, y_train, y_test = train_test_split(
    X, y, test_size=0.2, random_state=42, stratify=y
)

# 4) TRAIN -----------------------------------------------------------------
# class_weight="balanced" stops the model from ignoring the rare failures.
model = RandomForestClassifier(
    n_estimators=200, class_weight="balanced", random_state=42, n_jobs=-1
)
model.fit(X_train, y_train)

# 5) EVALUATE --------------------------------------------------------------
pred = model.predict(X_test)
proba = model.predict_proba(X_test)[:, 1]

print(f"Accuracy : {accuracy_score(y_test, pred) * 100:.2f}%")
print(f"ROC-AUC  : {roc_auc_score(y_test, proba):.3f}\n")
print("Confusion matrix (rows=actual, cols=predicted):")
print(confusion_matrix(y_test, pred), "\n")
print(classification_report(y_test, pred, target_names=["Healthy", "Failure"], digits=3))

print("Feature importance:")
for f, imp in sorted(zip(features, model.feature_importances_), key=lambda t: -t[1]):
    print(f"  {f:28s} {imp * 100:5.1f}%")

# 6) SAVE ------------------------------------------------------------------
joblib.dump(model, "fault_model.pkl")
print("\nSaved trained model -> fault_model.pkl")
