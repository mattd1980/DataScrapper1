import {HubConnectionBuilder } from "./signalr/browser-index.js"


const connection = new HubConnectionBuilder()
    .withUrl("/ordertracker")
    .build();

// Start the connection
connection.start().then(() => {
    console.log("Connected to SignalR hub");
}).catch(err => console.error("Error connecting to SignalR hub:", err));

// Subscribe to updates from the server
connection.on("Update", (history) => {
    console.log("Received status history:", history);
    
    // Display the status updates
    const statusList = document.getElementById('statusList') || createStatusList();
    statusList.innerHTML = '';
    
    history.forEach(status => {
        const listItem = document.createElement('li');
        listItem.className = 'status-item';
        listItem.innerHTML = `<span class="timestamp">${status.timestamp}</span>: <span class="status">${status.statusCode}</span>`;
        statusList.appendChild(listItem);
    });
    
    showNotification('Status History Updated', `Received ${history.length} status updates`);
});

// Add event listeners for buttons
document.getElementById('subscribeBtn').addEventListener('click', () => {
    const clientName = prompt('Enter your client name:');
    const trackingNumber = prompt('Enter tracking number to watch:');
    
    if (clientName && trackingNumber) {
        connection.invoke('RegisterTrackingNumberAsync', clientName, trackingNumber)
            .then(() => {
                showNotification('Subscribed', `Now watching tracking number: ${trackingNumber}`);
            })
            .catch(err => {
                console.error("Error subscribing:", err);
                showNotification('Error', 'Failed to subscribe to tracking updates');
            });
    }
});

document.getElementById('revealBtn').addEventListener('click', async () => {
    try {        
        await connection.invoke('GetStatusHistoryAsync')
        showNotification('Request Sent', 'Requesting latest status history...');
    } catch (err) {
        console.error("Error requesting status history:", err);
        showNotification('Error', 'Failed to request status history');        
    }
});

function showNotification(title, message) {
    alert(`${title}: ${message}`);
}

function createStatusList() {
    const container = document.createElement('div');
    container.className = 'status-container';
    
    const heading = document.createElement('h2');
    heading.textContent = 'Status History';
    container.appendChild(heading);
    
    const list = document.createElement('ul');
    list.id = 'statusList';
    list.className = 'status-list';
    container.appendChild(list);
    
    document.body.appendChild(container);
    
    return list;
}