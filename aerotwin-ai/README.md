# AeroTwin XR — AI Layer (Predictive Maintenance)

Machine-learning component of **AeroTwin XR**, an AI-powered VR training system
for aviation maintenance, built by **Team TechNest** (Amman Arab University,
Jordan) for **AI Expo Jordan 2026 — Track 1 (Student Projects), Education sector**.

This module trains and serves **Random Forest** models that analyze engine
sensor data, so a trainee can learn to detect, identify, and anticipate faults
safely inside the simulation. The same models are designed to run on a physical
inspection robot's real sensor feed once the hardware is built.

---

## The three models

| # | Task | Dataset | Script | Result |
|---|------|---------|--------|--------|
| 1 | **Fault detection** (healthy vs faulty) | AI4I 2020 (10,000 samples) | `train_model.py` | Accuracy 98.25%, ROC-AUC 0.968 |
| 2 | **Fault-type identification** (which fault) | AI4I 2020 (failure-type labels) | `train_fault_type.py` | Accuracy 98.2% |
| 3 | **Failure prediction** (needs maintenance soon) | NASA C-MAPSS FD001 (turbofan) | `train_nasa_cmaps.py` | Accuracy 96.1%, Recall 84.4%, ROC-AUC 0.990 |

**Model 1** flags whether an engine reading is healthy or faulty.
**Model 2** identifies the specific fault category (heat-dissipation, power, or
overstrain failure) from the combined sensor pattern.
**Model 3** predicts how close an engine is to failure (remaining useful life ≤
30 cycles → needs maintenance), warning before a fault becomes critical.

> **Known limitation (honest note):** failures are rare in the AI4I data, so the
> binary model's recall on the failure class is ~54% at the default threshold;
> threshold tuning raises it (≈73% at 0.30). Improving rare-fault recall is a
> planned next step.

---

## Method

- **Algorithm:** Random Forest (ensemble of decision trees that vote).
- **Split:** 80/20 stratified train/test (real class ratio preserved).
- **Imbalance handling:** balanced class weights.
- **Feature selection:** constant/uninformative sensors removed (17 of 24 kept for the turbofan model).
- **Label engineering:** turbofan remaining-useful-life reframed into a maintenance-decision label.
- **Reproducibility:** fixed random seeds — anyone re-running gets the same numbers.

---

## The bridge (model → simulation → robot)

`prediction_service.py` is a Flask REST API. The Unity simulation sends a live
sensor reading; the service runs the model and returns the status, a confidence
score, a message for the trainee, and a command for the robot.
`UnityAIClient.cs` is the matching Unity (C#) side that calls the service.

---

## How to run

```bash
pip install -r requirements.txt

python train_model.py        # Model 1 — fault detection (AI4I)
python train_fault_type.py   # Model 2 — fault-type identification (AI4I)
python train_nasa_cmaps.py   # Model 3 — failure prediction (NASA C-MAPSS)

python predict.py            # classify one example sensor reading
python prediction_service.py # start the prediction API (bridge to Unity)
```

---

## Files

| File | Purpose |
|------|---------|
| `train_model.py` | Trains Model 1 (fault detection) |
| `train_fault_type.py` | Trains Model 2 (fault-type identification) |
| `train_nasa_cmaps.py` | Trains Model 3 (failure prediction) |
| `predict.py` | Loads a model and classifies one reading |
| `prediction_service.py` | Flask API that bridges the model to the simulation |
| `UnityAIClient.cs` | Unity (C#) client that calls the API |
| `ai4i2020.csv` | AI4I 2020 dataset |
| `cmaps_nasa/` | NASA C-MAPSS FD001 data files |
| `requirements.txt` | Python dependencies |

---

## Role inside AeroTwin XR

The simulation sends a live sensor reading → the model returns status +
confidence → the in-VR assistant explains it to the trainee and the digital-twin
robot reacts. The models operate on standard engine sensor signals, so the same
intelligence transfers to the physical robot once it is built.

**Team TechNest — Amman Arab University**
