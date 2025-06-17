const body = document.body;

document.addEventListener('mousedown', (e) => {
    body.classList.add('grabbing');
});

document.addEventListener('mouseup', () => {
    body.classList.remove('grabbing');
});

const windowContainer = document.getElementById('windowContainer');

function openWindow() {
    if (getComputedStyle(windowContainer).display === "none") {
        windowContainer.style.display = "block";
        setTimeout(() => {
            windowContainer.style.transform = "scale(1)";
        }, 10);
    }
}

function closeWindow() {
    windowContainer.style.transform = "scale(0)";
    setTimeout(() => {
        windowContainer.style.display = "none";
    }, 200); // Match transition duration
}

function updateDateTime() {
    dateTime = new Date();
    document.getElementById('dateTime').textContent = dateTime.toLocaleString();
}

setInterval(updateDateTime, 1000);