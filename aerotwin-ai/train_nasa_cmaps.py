"""
AeroTwin XR - Predictive Maintenance (NASA Turbofan)
Random Forest classifier on NASA C-MAPSS FD001 turbofan engine data.
Task: predict whether an engine "needs maintenance" (RUL <= 30 cycles).

Dataset: NASA C-MAPSS FD001 (Turbofan Engine Degradation Simulation)
Author: TechNest Team - Amman Arab University
"""

import pandas as pd
from sklearn.model_selection import train_test_split
from sklearn.ensemble import RandomForestClassifier
from sklearn.metrics import (accuracy_score, recall_score,
                             precision_score, roc_auc_score, confusion_matrix)
import joblib

# 1) LOAD ------------------------------------------------------------------
# 26 columns: engine id, cycle, 3 settings, 21 sensors
cols = ["unit", "cycle", "set1", "set2", "set3"] + [f"s{i}" for i in range(1, 22)]
train = pd.read_csv("cmaps_nasa/train_FD001.txt", sep=r"\s+", header=None, names=cols)

# 2) BUILD LABEL -----------------------------------------------------------
# RUL = remaining cycles before this engine's last recorded cycle.
train["RUL"] = train.groupby("unit")["cycle"].transform("max") - train["cycle"]
# Reframe as classification: 1 = needs maintenance soon (RUL <= 30).
THRESHOLD = 30
train["label"] = (train["RUL"] <= THRESHOLD).astype(int)

# 3) SELECT FEATURES -------------------------------------------------------
# Drop sensors that never change (carry no information).
candidates = [f"s{i}" for i in range(1, 22)] + ["set1", "set2", "set3"]
features = [c for c in candidates if train[c].std() > 1e-6]

X, y = train[features], train["label"]

# 4) SPLIT + TRAIN ---------------------------------------------------------
X_train, X_test, y_train, y_test = train_test_split(
    X, y, test_size=0.2, random_state=42, stratify=y)
model = RandomForestClassifier(
    n_estimators=200, class_weight="balanced", random_state=42, n_jobs=-1)
model.fit(X_train, y_train)

# 5) EVALUATE --------------------------------------------------------------
pred = model.predict(X_test)
proba = model.predict_proba(X_test)[:, 1]
print(f"Accuracy : {accuracy_score(y_test, pred) * 100:.2f}%")
print(f"ROC-AUC  : {roc_auc_score(y_test, proba):.3f}")
print(f"Recall   : {recall_score(y_test, pred) * 100:.1f}%")
print(f"Precision: {precision_score(y_test, pred) * 100:.1f}%")
print(confusion_matrix(y_test, pred))

# 6) SAVE ------------------------------------------------------------------
joblib.dump(model, "nasa_fault_model.pkl")
print("Saved -> nasa_fault_model.pkl")
