let mainWebsocket;

async function getWebSocketUrl() {
    try {
        const response = await fetch('/websocket/mainurl');
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        const data = await response.json();
        return data.url; // Assuming your server returns { "url": "ws://your-websocket-url" }
    } catch (error) {
        console.error('Error fetching WebSocket URL:', error);
    }
}

async function initializeWebSocket() {
    const url = await getWebSocketUrl();
    if (url) {
        //console.log(url);
        mainWebsocket = new WebSocket(url);

        mainWebsocket.onopen = () => {
            console.log('WebSocket connection established');
        };

        mainWebsocket.onmessage = (event) => {
            console.log('Message from server:', event.data);
            displayMessage(event.data);
        };

        mainWebsocket.onclose = async () => {
            console.log('WebSocket connection closed');

            setTimeout(initializeWebSocket,5000);
        };

        mainWebsocket.onerror = (error) => {
            console.error('WebSocket error:', error);

            setTimeout(initializeWebSocket,5000);
        };
    }
    else
        setTimeout(initializeWebSocket,5000);
}

function displayMessage(message) {
    const messagesDiv = document.getElementById('messages');
    const newMessage = document.createElement('div');
    newMessage.textContent = message; // Display the received message
    messagesDiv.appendChild(newMessage);
    messagesDiv.scrollTop = messagesDiv.scrollHeight; // Scroll to the bottom
}

// Call the function to initialize the WebSocket
initializeWebSocket();