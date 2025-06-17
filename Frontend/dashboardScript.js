const map = L.map('map').setView([51.91667, 4.60278], 13);

// Default skin.
L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {attribution: '&copy; OpenStreetMap contributors'}).addTo(map);

// L.marker([51.505, -0.09]).addTo(map).bindPopup('Default Location').openPopup();