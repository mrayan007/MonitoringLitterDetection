// --- Global variables for JWT and Map ---
let jwtToken = null;
let tokenExpiry = null; // To keep track of token expiration

let map = null; // Initialize map as null
let marker = null; // To store the map marker, so we can update it later

// Your C# API base URL (replace with your actual C# API address)
const CSHARP_API_BASE_URL = 'https://localhost:32773'; // Confirmed C# API URL

// --- Leaflet Map Initialization ---
// Initialize the map once the DOM is ready, or directly here if 'map' div exists on load
document.addEventListener('DOMContentLoaded', () => {
    map = L.map('map', {
        center: [51.585719, 4.793235], // Center around Roosendaal, Netherlands
        zoom: 13,
        zoomControl: false // Keep zoom controls off if desired
    });

    L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
        attribution: '&copy; OpenStreetMap, Carto'
    }).addTo(map);

    // --- Trigger initial login when the page loads ---
    login().catch(err => console.error("Initial login on page load failed:", err.message));
});


// --- Login Function ---
async function login() {
    // These credentials must match what your C# AuthController expects (e.g., in LoginRequestDto)
    const username = "admin";
    const password = "password123";

    try {
        console.log("Attempting to log in...");
        const response = await fetch(`${CSHARP_API_BASE_URL}/api/Auth/login`, { // Adjust URL if needed
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ username, password })
        });

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(`Login failed: ${response.status} - ${JSON.stringify(errorData)}`);
        }

        const data = await response.json();
        jwtToken = data.accessToken;
        tokenExpiry = new Date(data.expiresAt); // Parse the expiry date string into a Date object

        console.log("Login successful! Token acquired. Expires at:", tokenExpiry);

    } catch (error) {
        console.error('Login error:', error);
        // Alert the user only for critical login failures, especially on initial load
        alert(`Authentication required: ${error.message}\nPlease ensure the C# API is running and credentials are correct.`);
        jwtToken = null; // Ensure token is cleared on failure
        tokenExpiry = null; // Clear expiry as well
        throw error; // Re-throw to prevent further operations that depend on authentication
    }
}


// --- Prediction Function ---
async function predict() {
    const category = document.getElementById('category').value;
    const dayOfWeek = document.getElementById('day').value;
    const predictionType = document.getElementById('prediction').value;
    const predictionResultLabel = document.getElementById('predictionResult');

    predictionResultLabel.textContent = "Loading prediction...";

    // --- Authentication Check (Crucial for protected endpoints) ---
    // If no token, or if token exists but is expired, attempt to log in again.
    if (!jwtToken || (tokenExpiry && new Date() >= tokenExpiry)) {
        console.log("No valid JWT found or token expired. Attempting to re-login...");
        try {
            await login(); // Attempt to re-authenticate
        } catch (authError) {
            // If re-login fails, display error and stop the prediction process
            predictionResultLabel.textContent = `Error: Authentication failed. ${authError.message}`;
            return; // Exit the function
        }
    }

    try {
        // --- THIS IS THE CRUCIAL CHANGE: Adding Authorization header ---
        const headers = {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${jwtToken}` // Include the JWT here
        };

        // Construct the URL dynamically based on predictionType
        const response = await fetch(`${CSHARP_API_BASE_URL}/api/Monitoring/predict/${predictionType}`, {
            method: 'POST',
            headers: headers, // Use the headers object with JWT
            body: JSON.stringify({
                category: category,
                day_of_week: dayOfWeek
            })
        });

        if (!response.ok) {
            const errorData = await response.json(); // Still try to parse for detailed error
            // Specific handling for 401 Unauthorized, in case the token became invalid after login()
            if (response.status === 401) {
                alert("Your session has expired or is invalid. Please refresh the page to re-authenticate.");
                jwtToken = null; // Clear invalid token
                tokenExpiry = null; // Clear expiry
            }
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