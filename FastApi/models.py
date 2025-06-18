# FastAPIMonitoring/models.py
from pydantic import BaseModel
from typing import Literal

class PredictInput(BaseModel):
    category: str
    day_of_week: str

class LocationPredictionOutput(BaseModel):
    latitude: float
    longitude: float
    unit: Literal["degrees"] = "degrees" # Optioneel: specificeer de eenheid expliciet

class TemperaturePredictionOutput(BaseModel): # Bestaande output voor temperatuur hernoemen voor duidelijkheid
    prediction: float
    unit: Literal["degrees Celsius"] = "degrees Celsius" # Optioneel: specificeer de eenheid expliciet

# Dit is de oude, generieke PredictionOutput die je nu waarschijnlijk niet meer nodig hebt:
# class PredictionOutput(BaseModel):
#     prediction: float
#     unit: str