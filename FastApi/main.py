# FastAPIMonitoring/main.py
from fastapi import FastAPI, HTTPException, status
from fastapi.middleware.cors import CORSMiddleware
from typing import Dict, List, Any
import joblib
import os
import pandas as pd
from datetime import datetime
from dotenv import load_dotenv

load_dotenv()

# --- IMPORTEER JE MODELLEN EN OUTPUT MODELLEN ---
# Zorg dat de nieuwe LocationPredictionOutput en de hernoemde TemperaturePredictionOutput worden geïmporteerd
from models import PredictInput, LocationPredictionOutput, TemperaturePredictionOutput

app = FastAPI(title="Litter Prediction API", version="1.0")

# --- CORS instellingen (deze blijven hetzelfde) ---
origins = [
    "http://localhost:5000",
    "https://localhost:5001",
    "http://localhost:7052",
    "https://localhost:7052",
    "http://127.0.0.1:5000",
    "https://127.0.0.1:5001",
    "http://127.0.0.1:7052",
    "https://127.0.0.1:7052",
    "http://localhost:8000",
    "http://127.0.0.1:8000"
]

app.add_middleware(
    CORSMiddleware,
    allow_origins=origins,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# --- Globale variabelen voor geladen modellen ---
models: Dict[str, Any] = {}
MODEL_DIR = "model"

@app.on_event("startup")
async def load_models():
    """Laad de getrainde ML-modellen (pipelines) bij het opstarten van de FastAPI applicatie."""
    global models
    try:
        print(f"DEBUG: Opstarten - Modellen laden uit '{MODEL_DIR}/'...")
        models['latitude'] = joblib.load(os.path.join(MODEL_DIR, 'model_predict_latitude.pkl'))
        models['longitude'] = joblib.load(os.path.join(MODEL_DIR, 'model_predict_longitude.pkl'))
        models['temperature'] = joblib.load(os.path.join(MODEL_DIR, 'model_predict_temperature.pkl'))
        print("DEBUG: Alle modellen succesvol geladen.")
    except FileNotFoundError as e:
        print(f"ERROR: Modelbestanden niet gevonden: {e}. Zorg ervoor dat je 'train_models.py' hebt uitgevoerd en dat de 'model/' map bestaat.")
        raise RuntimeError(f"Ontbrekende modelbestanden: {e}. Train de modellen eerst.")
    except Exception as e:
        print(f"ERROR: Fout bij laden van modellen: {e}")
        raise RuntimeError(f"Fout bij laden van modellen: {e}")

# --- FastAPI Endpoints ---

@app.get("/")
async def read_root():
    return {"message": "FastAPI is running!"}

@app.post("/data")
async def receive_litter_data(litter_items: List[Dict[str, Any]]):
    if not litter_items:
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Geen data ontvangen.")
    print(f"\n--- Data Ontvangen van (potentiële) C# API ({datetime.now()}) ---")
    print(f"FastAPI heeft {len(litter_items)} litter items ontvangen (niet verwerkt voor voorspelling in dit endpoint).")
    print("-------------------------------------------\n")
    return {"message": f"Succesvol {len(litter_items)} litter items ontvangen door FastAPI."}


# NIEUW: Endpoint voor het voorspellen van Latitude en Longitude samen
@app.post("/predict/location", summary="Voorspel breedte- en lengtegraad samen")
async def predict_location(input_data: PredictInput) -> LocationPredictionOutput:
    if not models or "latitude" not in models or "longitude" not in models:
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Latitude of Longitude ML-model is niet geladen.")
    
    try:
        input_df = pd.DataFrame([{
            'category': input_data.category,
            'day_of_week': input_data.day_of_week
        }])
        
        # Voorspel beide waarden afzonderlijk met hun respectievelijke modellen
        predicted_latitude = models["latitude"].predict(input_df)[0]
        predicted_longitude = models["longitude"].predict(input_df)[0]
        
        # Geef beide waarden terug in één JSON-object
        return LocationPredictionOutput(latitude=predicted_latitude, longitude=predicted_longitude)
    except Exception as e:
        print(f"ERROR: Fout bij voorspellen locatie: {e}")
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail=f"Fout bij voorspelling: {e}")

# Bestaande Endpoint voor Temperatuur Voorspelling (kleine type-hint aanpassing)
@app.post("/predict/temperature", summary="Voorspel de temperatuur")
async def predict_temperature(input_data: PredictInput) -> TemperaturePredictionOutput:
    if not models or "temperature" not in models:
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Temperatuur ML-model is niet geladen.")

    try:
        input_df = pd.DataFrame([{
            'category': input_data.category,
            'day_of_week': input_data.day_of_week
        }])

        prediction = models["temperature"].predict(input_df)[0]
        # Gebruik nu de hernoemde TemperaturePredictionOutput
        return TemperaturePredictionOutput(prediction=prediction, unit="degrees Celsius")
    except Exception as e:
        print(f"ERROR: Fout bij voorspellen temperatuur: {e}")
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail=f"Fout bij voorspelling: {e}")