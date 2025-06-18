# train_models.py
import pandas as pd
import numpy as np
import joblib
from sklearn.model_selection import train_test_split
from sklearn.ensemble import RandomForestRegressor
from sklearn.preprocessing import OneHotEncoder
from sklearn.compose import ColumnTransformer
from sklearn.pipeline import Pipeline
import os

# --- 1. Laad Data ---
try:
    # Dit deel is prima zoals het is, of je nu een echt JSON-bestand hebt
    # of dat je dummy data genereert. De data moet de juiste kolommen hebben
    # voor het trainen van de modellen.
    df = pd.read_json('data/sensoring_data.json')
    print("sensoring_data.json succesvol geladen.")
    print("Eerste 5 rijen:")
    print(df.head())
    print("\nKolommen en datatypen:")
    df.info()

    required_cols = ['dateTime', 'category', 'locationLat', 'locationLon', 'temperature', 'confidence']
    if not all(col in df.columns for col in required_cols):
        raise ValueError(f"Ontbrekende kolommen in sensoring_data.json. Vereist: {required_cols}")
    if not pd.api.types.is_numeric_dtype(df['locationLat']) or \
       not pd.api.types.is_numeric_dtype(df['locationLon']) or \
       not pd.api.types.is_numeric_dtype(df['temperature']):
        print("Waarschuwing: locationLat, locationLon of temperature zijn niet numeriek. Controleer je data.")

except FileNotFoundError:
    print("Fout: 'data/sensoring_data.json' niet gevonden.")
    print("Genereer dummy data voor demonstratie.")
    # --- Genereer dummy data als bestand niet gevonden is ---
    np.random.seed(42)
    num_samples = 1000
    categories = ['organisch', 'grof metaal', 'blikjes', 'plastic', 'papier', 'glas', 'sigaret']
    days = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday']

    data = {
        'dateTime': pd.to_datetime('2024-01-01 00:00:00') + pd.to_timedelta(np.random.randint(0, 365*24, num_samples), unit='H'),
        'category': np.random.choice(categories, num_samples),
        'locationLat': np.random.uniform(51.4, 51.7, num_samples),
        'locationLon': np.random.uniform(4.5, 5.0, num_samples),
        'temperature': np.random.uniform(10.0, 30.0, num_samples),
        'confidence': np.random.uniform(0.5, 1.0, num_samples) # Dummy confidence
    }
    df = pd.DataFrame(data)
    print("Dummy data gegenereerd.")

# --- 2. Preprocessing ---
df = df[df['confidence'] >= 0.5].copy() # Gebruik .copy() om SettingWithCopyWarning te voorkomen

# Extraheer dag van de week
df['dateTime'] = pd.to_datetime(df['dateTime'])
df['day_of_week'] = df['dateTime'].dt.day_name() # Zorgt voor namen zoals 'Monday'

# Selecteer features en targets
# Features voor de modellen zijn nu de input die je krijgt van de frontend/C# API:
# 'category' en 'day_of_week'
X_model_features = df[['category', 'day_of_week']] # Dit zijn de features!

# Targets zijn de waarden die we willen voorspellen: locatie en temperatuur
y_latitude = df['locationLat']
y_longitude = df['locationLon']
y_temperature = df['temperature']

# Preprocessor voor categorische features (alleen category en day_of_week)
categorical_features_for_ohe = ['category', 'day_of_week']
preprocessor = ColumnTransformer(
    transformers=[
        ('onehot', OneHotEncoder(handle_unknown='ignore', sparse_output=False), categorical_features_for_ohe)
    ],
    remainder='passthrough' # Zorgt ervoor dat andere kolommen (die er hier niet zijn) behouden blijven
)

# --- 3. Modellen Trainen en Opslaan ---
output_dir = 'model'
os.makedirs(output_dir, exist_ok=True)

# Pipeline voor Latitude Voorspelling
pipeline_latitude = Pipeline(steps=[
    ('preprocessor', preprocessor),
    ('regressor', RandomForestRegressor(n_estimators=100, random_state=42))
])
print("\nTrainen Latitude model...")
pipeline_latitude.fit(X_model_features, y_latitude)
joblib.dump(pipeline_latitude, os.path.join(output_dir, 'model_predict_latitude.pkl'))
print(f"Latitude model opgeslagen als '{os.path.join(output_dir, 'model_predict_latitude.pkl')}'")

# Pipeline voor Longitude Voorspelling
pipeline_longitude = Pipeline(steps=[
    ('preprocessor', preprocessor),
    ('regressor', RandomForestRegressor(n_estimators=100, random_state=42))
])
print("Trainen Longitude model...")
pipeline_longitude.fit(X_model_features, y_longitude)
joblib.dump(pipeline_longitude, os.path.join(output_dir, 'model_predict_longitude.pkl'))
print(f"Longitude model opgeslagen als '{os.path.join(output_dir, 'model_predict_longitude.pkl')}'")

# Pipeline voor Temperatuur Voorspelling
pipeline_temperature = Pipeline(steps=[
    ('preprocessor', preprocessor),
    ('regressor', RandomForestRegressor(n_estimators=100, random_state=42))
])
print("Trainen Temperatuur model...")
pipeline_temperature.fit(X_model_features, y_temperature)
joblib.dump(pipeline_temperature, os.path.join(output_dir, 'model_predict_temperature.pkl'))
print(f"Temperatuur model opgeslagen als '{os.path.join(output_dir, 'model_predict_temperature.pkl')}'")

print("\nAlle modellen succesvol getraind en opgeslagen in de 'model/' map.")

# Optioneel: Print de verwachte featurenamen voor debuggen of handmatige check
print("\nVerwachte feature namen die de modellen gebruiken (na preprocessing):")
try:
    # Haal de namen op na transformatie door de preprocessor
    # Dit is belangrijk om te zien hoe de one-hot encoding de kolommen noemt
    example_input = pd.DataFrame([['plastic', 'Monday']], columns=['category', 'day_of_week'])
    transformed_features = preprocessor.fit_transform(example_input) # Fit_transform om de feature namen te krijgen
    feature_names_out = preprocessor.get_feature_names_out()
    print(list(feature_names_out))
except Exception as e:
    print(f"Kon feature namen niet ophalen: {e}")