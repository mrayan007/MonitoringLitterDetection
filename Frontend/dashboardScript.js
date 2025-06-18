const map = L.map('map', {
  center: [51.585719, 4.793235],
  zoom: 13,
  zoomControl: false
});

L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
  attribution: '&copy; OpenStreetMap, Carto'
}).addTo(map);

var marker; // To store the map marker, so we can update it later

async function predict() {
    const category = document.getElementById('category').value;
    const dayOfWeek = document.getElementById('day').value;
    const predictionType = document.getElementById('prediction').value;
    const predictionResultLabel = document.getElementById('predictionResult');

    // Your C# API base URL (replace with your actual C# API address)
    // Make sure this matches one of the `origins` in your FastAPI's CORS settings.
    const CSHARP_API_BASE_URL = 'https://localhost:7013'; // Confirmed C# API URL

    predictionResultLabel.textContent = "Loading prediction...";

    try {
        // --- THIS IS THE CRUCIAL CHANGE ---
        // Added '/api/Monitoring' to match the C# controller's route attribute
        const response = await fetch(`${CSHARP_API_BASE_URL}/api/Monitoring/predict/${predictionType}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                category: category,
                day_of_week: dayOfWeek
            })
        });

        if (!response.ok) {
            const errorData = await response.json(); // Still try to parse for detailed error
            throw new Error(`C# API Error: ${response.status} - ${errorData.detail || JSON.stringify(errorData)}`);
        }

        const data = await response.json();
        console.log("Prediction data from C# API:", data); // For debugging

        if (predictionType === 'location') {
            // For location prediction, the C# API will return an object with latitude, longitude, and address
            const { latitude, longitude, address } = data; // Destructure the response
            predictionResultLabel.textContent = `Predicted Location: ${address} (Lat: ${latitude.toFixed(4)}, Lon: ${longitude.toFixed(4)})`;

            // Update map
            if (marker) {
                map.removeLayer(marker); // Remove existing marker
            }
            marker = L.marker([latitude, longitude]).addTo(map)
                .bindPopup(`Predicted location for ${category} on ${dayOfWeek}:<br>${address}`)
                .openPopup();
            map.setView([latitude, longitude], 15); // Zoom to the new location

        } else if (predictionType === 'temperature') {
            // For temperature prediction, the C# API will return an object with 'prediction' and 'unit'
            const { prediction, unit } = data;
            predictionResultLabel.textContent = `Predicted Temperature: ${prediction.toFixed(2)} ${unit}`;

            // Remove marker if previous prediction was location
            if (marker) {
                map.removeLayer(marker);
                marker = null; // Clear marker reference
            }
        }

    } catch (error) {
        console.error("Error during prediction:", error);
        predictionResultLabel.textContent = `Error: ${error.message}`;
        if (marker) {
            map.removeLayer(marker);
            marker = null;
        }
    }
}