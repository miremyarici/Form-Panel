let map;
let baseLayer;
let labelLayer;
let drawInteraction;
let drawSource;
let drawLayer;
let searchSource;
let searchLayer;

function createMap(layerType = "uydu") {
    const view = new ol.View({
        center: ol.proj.fromLonLat([32.8541, 39.9208]),
        zoom: 10
    });

    baseLayer = new ol.layer.Tile({
        source: new ol.source.XYZ({
            url: getLayerUrl(layerType),
            attributions: '© Google'
        })
    });

    labelLayer = new ol.layer.Tile({
        source: new ol.source.XYZ({
            url: "http://mt1.google.com/vt/lyrs=h&x={x}&y={y}&z={z}",
            attributions: '© Google'
        }),
        opacity: 0.8
    });

    drawSource = new ol.source.Vector();
    drawLayer = new ol.layer.Vector({
        source: drawSource,
        style: feature => new ol.style.Style({
            stroke: new ol.style.Stroke({
                color: getSelectedColor(),
                width: 2
            }),
            fill: new ol.style.Fill({
                color: hexToRGBA(getSelectedColor(), 0.3)
            }),
            image: new ol.style.Circle({
                radius: 6,
                fill: new ol.style.Fill({ color: getSelectedColor() })
            })
        })
    });

    searchSource = new ol.source.Vector();
    searchLayer = new ol.layer.Vector({
        source: searchSource,
        style: new ol.style.Style({
            image: new ol.style.Icon({
                anchor: [0.5, 1],
                src: 'data:image/svg+xml;base64,' + btoa(`
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                        <path d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7z" fill="#FF0000"/>
                        <circle cx="12" cy="9" r="2.5" fill="white"/>
                    </svg>
                `)
            })
        })
    });

    map = new ol.Map({
        target: 'map',
        layers: [baseLayer, labelLayer, drawLayer, searchLayer],
        view: view
    });

    const popup = new ol.Overlay({
        element: document.createElement('div'),
        positioning: 'bottom-center',
        stopEvent: false,
        offset: [0, -10]
    });
    map.addOverlay(popup);

    map.on('click', function (evt) {
        const feature = map.forEachFeatureAtPixel(evt.pixel, function (feature) {
            return feature;
        });

        if (feature && feature.get('isSearchResult')) {
            const coordinates = feature.getGeometry().getCoordinates();
            const name = feature.get('name');

            popup.getElement().innerHTML = `
                <div class="bg-white border rounded p-2 shadow">
                    <strong>${name}</strong>
                </div>
            `;
            popup.setPosition(coordinates);
        }
    });
}

function getLayerUrl(type) {
    switch (type) {
        case "uydu":
            return "http://mt1.google.com/vt/lyrs=s&x={x}&y={y}&z={z}";
        case "arazi":
            return "http://mt1.google.com/vt/lyrs=m&x={x}&y={y}&z={z}";
        default:
            return "http://mt1.google.com/vt/lyrs=m&x={x}&y={y}&z={z}";
    }
}

function getSelectedColor() {
    const renk = document.getElementById("cizimRengi");
    return renk?.value || "#FC6C85";
}

function hexToRGBA(hex, opacity) {
    let r = parseInt(hex.slice(1, 3), 16),
        g = parseInt(hex.slice(3, 5), 16),
        b = parseInt(hex.slice(5, 7), 16);
    return `rgba(${r},${g},${b},${opacity})`;
}

function changeLayer(type) {
    const newSource = new ol.source.XYZ({
        url: getLayerUrl(type),
        attributions: '© Google'
    });
    baseLayer.setSource(newSource);
}

function toggleLabels(show) {
    labelLayer.setVisible(show);
}

function addDrawing(type) {
    if (drawInteraction) {
        map.removeInteraction(drawInteraction);
    }
    let geometryType;
    switch (type) {
        case "nokta":
            geometryType = "Point"; break;
        case "line":
            geometryType = "LineString"; break;
        case "circle":
            geometryType = "Circle"; break;
        default:
            return;
    }
    drawInteraction = new ol.interaction.Draw({
        source: drawSource,
        type: geometryType
    });
    map.addInteraction(drawInteraction);
}

function clearDrawings() {
    drawSource.clear();
    if (drawInteraction) {
        map.removeInteraction(drawInteraction);
        drawInteraction = null;
    }
}

function clearSearch() {
    searchSource.clear();
    document.getElementById('searchResults').style.display = 'none';
    document.getElementById('searchLocation').value = '';
}

async function searchLocation(query) {
    if (!query.trim()) return;

    try {
        const response = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}&limit=5&accept-language=tr`);
        const results = await response.json();

        if (results.length === 0) {
            alert('Arama sonucu bulunamadı.');
            return;
        }

        searchSource.clear();

        const features = results.map(result => {
            const coordinate = ol.proj.fromLonLat([parseFloat(result.lon), parseFloat(result.lat)]);
            const feature = new ol.Feature({
                geometry: new ol.geom.Point(coordinate),
                name: result.display_name,
                isSearchResult: true
            });
            return feature;
        });

        searchSource.addFeatures(features);

        if (features.length > 0) {
            const firstFeature = features[0];
            const coordinate = firstFeature.getGeometry().getCoordinates();
            map.getView().setCenter(coordinate);
            map.getView().setZoom(12);
        }

        displaySearchResults(results);

    } catch (error) {
        console.error('Arama hatası:', error);
        alert('Arama sırasında bir hata oluştu.');
    }
}

function displaySearchResults(results) {
    const resultsDiv = document.getElementById('resultsList');
    resultsDiv.innerHTML = '';

    results.forEach((result, index) => {
        const resultItem = document.createElement('div');
        resultItem.className = 'border-bottom py-2 cursor-pointer';
        resultItem.style.cursor = 'pointer';
        resultItem.innerHTML = `
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <strong>${result.display_name.split(',')[0]}</strong>
                    <br>
                    <small class="text-muted">${result.display_name}</small>
                </div>
                <button class="btn btn-sm btn-outline-primary" onclick="goToLocation(${result.lat}, ${result.lon})">
                    Git
                </button>
            </div>
        `;
        resultsDiv.appendChild(resultItem);
    });

    document.getElementById('searchResults').style.display = 'block';
}

function goToLocation(lat, lon) {
    const coordinate = ol.proj.fromLonLat([parseFloat(lon), parseFloat(lat)]);
    map.getView().setCenter(coordinate);
    map.getView().setZoom(15);
}

document.addEventListener("DOMContentLoaded", function () {
    createMap("uydu");

    document.getElementById("arazi")?.addEventListener("change", function (e) {
        const selected = e.target.value;
        if (selected === "uydu" || selected === "arazi") {
            changeLayer(selected);
        }
    });

    document.getElementById("cizimSekli")?.addEventListener("change", function (e) {
        const selectedShape = e.target.value;
        if (selectedShape) {
            addDrawing(selectedShape);
        }
    });

    document.getElementById("showLabels")?.addEventListener("change", function (e) {
        toggleLabels(e.target.checked);
    });

    document.getElementById("searchBtn")?.addEventListener("click", function (e) {
        e.preventDefault();
        const query = document.getElementById("searchLocation").value;
        searchLocation(query);
    });

    document.getElementById("searchLocation")?.addEventListener("keypress", function (e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            const query = this.value;
            searchLocation(query);
        }
    });

    document.getElementById("clearBtn")?.addEventListener("click", function (e) {
        e.preventDefault();
        clearDrawings();
    });

    document.getElementById("clearSearchBtn")?.addEventListener("click", function (e) {
        e.preventDefault();
        clearSearch();
    });

    if (!document.getElementById("cizimRengi").value) {
        document.getElementById("cizimRengi").value = "#FC6C85";
    }
});