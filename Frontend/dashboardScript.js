const map = L.map('map', {
  center: [51.5, -0.09],
  zoom: 13,
  zoomControl: false
});

L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
  attribution: '&copy; OpenStreetMap, Carto'
}).addTo(map);

L.marker([51.5, -0.09], {
  icon: L.icon({
    iconUrl: 'Art/trashCan.png',
    iconSize: [40, 40],
    className: 'mapIcon'
  })
}).addTo(map);

L.circle([51.5, -0.09], {
  radius: 300,
  color: 'red',
  fillColor: '#f03',
  fillOpacity: 0.3,
  className: 'mapIcon'
}).addTo(map);