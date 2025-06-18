const map = L.map('map', {
  center: [51.585719, 4.793235],
  zoom: 13,
  zoomControl: false
});

L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
  attribution: '&copy; OpenStreetMap, Carto'
}).addTo(map);

L.marker([51.585719, 4.793235], {
  icon: L.icon({
    iconUrl: 'Art/trashCan.png',
    iconSize: [40, 40],
    className: 'mapIcon'
  })
}).addTo(map)
  .bindPopup('Avans Hogeschool')
  .openPopup();

L.circle([51.585719, 4.793235], {
  radius: 300,
  color: 'red',
  fillColor: '#f03',
  fillOpacity: 0.3,
  className: 'mapIcon'
}).addTo(map);