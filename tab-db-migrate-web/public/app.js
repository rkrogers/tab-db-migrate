// Application State
let currentConnections = null;
let selectedConnection = null;

// DOM Elements
const authScreen = document.getElementById('auth-screen');
const connectionsScreen = document.getElementById('connections-screen');
const authForm = document.getElementById('auth-form');
const authError = document.getElementById('auth-error');
const connectBtn = document.getElementById('connect-btn');
const logoutBtn = document.getElementById('logout-btn');
const connectionsList = document.getElementById('connections-list');
const loadingConnections = document.getElementById('loading-connections');
const noSelection = document.getElementById('no-selection');
const updateFormContainer = document.getElementById('update-form-container');
const updateForm = document.getElementById('update-form');
const updateBtn = document.getElementById('update-btn');
const updateResults = document.getElementById('update-results');

// Initialize app
document.addEventListener('DOMContentLoaded', () => {
    checkAuthStatus();
    setupEventListeners();
});

// Check if user is already authenticated
async function checkAuthStatus() {
    try {
        const response = await fetch('/api/auth/status');
        const data = await response.json();
        
        if (data.authenticated) {
            showConnectionsScreen();
            loadConnections();
        }
    } catch (error) {
        console.error('Error checking auth status:', error);
    }
}

// Setup event listeners
function setupEventListeners() {
    authForm.addEventListener('submit', handleLogin);
    logoutBtn.addEventListener('click', handleLogout);
    updateForm.addEventListener('submit', handleUpdate);
}

// Handle login
async function handleLogin(e) {
    e.preventDefault();
    
    const formData = new FormData(authForm);
    const credentials = {
        serverUrl: formData.get('serverUrl'),
        tokenName: formData.get('tokenName'),
        tokenSecret: formData.get('tokenSecret'),
        siteName: formData.get('siteName')
    };

    // Show loading state
    setButtonLoading(connectBtn, true);
    hideError();

    try {
        const response = await fetch('/api/auth/signin', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(credentials)
        });

        const data = await response.json();

        if (response.ok && data.success) {
            showConnectionsScreen();
            loadConnections();
        } else {
            showError(data.details || data.error || 'Authentication failed');
        }
    } catch (error) {
        showError('Network error: ' + error.message);
    } finally {
        setButtonLoading(connectBtn, false);
    }
}

// Handle logout
async function handleLogout() {
    try {
        await fetch('/api/auth/signout', { method: 'POST' });
    } catch (error) {
        console.error('Error signing out:', error);
    } finally {
        showAuthScreen();
        authForm.reset();
    }
}

// Load connections
async function loadConnections() {
    loadingConnections.style.display = 'block';
    connectionsList.innerHTML = '';

    try {
        const response = await fetch('/api/connections');
        const data = await response.json();

        if (response.ok && data.success) {
            currentConnections = data.uniqueConnections;
            displayConnections(currentConnections);
        } else {
            connectionsList.innerHTML = `
                <div class="error-message">
                    Failed to load connections: ${data.error || 'Unknown error'}
                </div>
            `;
        }
    } catch (error) {
        connectionsList.innerHTML = `
            <div class="error-message">
                Network error: ${error.message}
            </div>
        `;
    } finally {
        loadingConnections.style.display = 'none';
    }
}

// Display connections in the list
function displayConnections(connections) {
    if (!connections || connections.length === 0) {
        connectionsList.innerHTML = `
            <div class="no-selection">
                <p>No connections found</p>
            </div>
        `;
        return;
    }

    connectionsList.innerHTML = connections.map(conn => `
        <div class="connection-item" data-connection-id="${conn.id}">
            <h3>${conn.serverAddress || '(empty)'}:${conn.serverPort || '(empty)'}</h3>
            <div class="connection-detail">
                <strong>Username:</strong> ${conn.userName || '(empty)'}
            </div>
            <div class="connection-counts">
                <span class="count-badge">
                    📊 Data Sources: ${conn.dataSourceCount}
                </span>
                <span class="count-badge">
                    📈 Workbooks: ${conn.workbookCount}
                </span>
            </div>
        </div>
    `).join('');

    // Add click handlers
    document.querySelectorAll('.connection-item').forEach(item => {
        item.addEventListener('click', () => {
            const connectionId = parseInt(item.dataset.connectionId);
            selectConnection(connectionId);
        });
    });
}

// Select a connection
function selectConnection(connectionId) {
    const connection = currentConnections.find(c => c.id === connectionId);
    if (!connection) return;

    selectedConnection = connection;

    // Update UI
    document.querySelectorAll('.connection-item').forEach(item => {
        item.classList.remove('selected');
    });
    document.querySelector(`[data-connection-id="${connectionId}"]`).classList.add('selected');

    // Show update form
    noSelection.style.display = 'none';
    updateFormContainer.style.display = 'block';
    updateResults.style.display = 'none';

    // Populate current connection details
    document.getElementById('current-server').textContent = connection.serverAddress || '(empty)';
    document.getElementById('current-port').textContent = connection.serverPort || '(empty)';
    document.getElementById('current-username').textContent = connection.userName || '(empty)';
    document.getElementById('current-ds-count').textContent = connection.dataSourceCount;
    document.getElementById('current-wb-count').textContent = connection.workbookCount;

    // Pre-populate form with current values
    document.getElementById('new-server').value = connection.serverAddress || '';
    document.getElementById('new-port').value = connection.serverPort || '';
    document.getElementById('new-username').value = connection.userName || '';
    document.getElementById('new-password').value = '';

    // Display affected assets
    displayAffectedAssets(connection);
}

// Display affected assets
function displayAffectedAssets(connection) {
    const assetsCount = connection.dataSourceCount + connection.workbookCount;
    document.getElementById('assets-count').textContent = assetsCount;

    const assetsList = document.getElementById('affected-assets-list');
    
    let html = '';
    
    // Add data sources
    if (connection.dataSources && connection.dataSources.length > 0) {
        connection.dataSources.forEach(ds => {
            html += `
                <div class="asset-item">
                    <span class="asset-type">DATA SOURCE</span>
                    ${ds.name}
                </div>
            `;
        });
    }
    
    // Add workbooks
    if (connection.workbooks && connection.workbooks.length > 0) {
        connection.workbooks.forEach(wb => {
            html += `
                <div class="asset-item">
                    <span class="asset-type">WORKBOOK</span>
                    ${wb.name}
                </div>
            `;
        });
    }

    assetsList.innerHTML = html || '<p>No assets found</p>';
}

// Handle update
async function handleUpdate(e) {
    e.preventDefault();

    if (!selectedConnection) {
        alert('No connection selected');
        return;
    }

    const formData = new FormData(updateForm);
    const updateData = {
        connections: selectedConnection.connections,
        serverAddress: formData.get('serverAddress'),
        serverPort: formData.get('serverPort'),
        userName: formData.get('userName'),
        password: formData.get('password')
    };

    // Show loading state
    setButtonLoading(updateBtn, true);
    updateResults.style.display = 'none';

    try {
        const response = await fetch('/api/connections/update', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(updateData)
        });

        const data = await response.json();

        if (response.ok && data.success) {
            displayUpdateResults(data.results);
        } else {
            showError(data.error || 'Update failed');
        }
    } catch (error) {
        showError('Network error: ' + error.message);
    } finally {
        setButtonLoading(updateBtn, false);
    }
}

// Display update results
function displayUpdateResults(results) {
    updateResults.style.display = 'block';
    
    let html = '';
    
    // Successful updates
    if (results.successful && results.successful.length > 0) {
        html += '<div class="result-success"><strong>✓ Successful Updates:</strong></div>';
        results.successful.forEach(item => {
            html += `
                <div class="result-item result-success">
                    ✓ ${item.type === 'datasource' ? '📊' : '📈'} ${item.name}
                </div>
            `;
        });
    }
    
    // Failed updates
    if (results.failed && results.failed.length > 0) {
        html += '<div class="result-failure" style="margin-top: 12px;"><strong>✗ Failed Updates:</strong></div>';
        results.failed.forEach(item => {
            html += `
                <div class="result-item result-failure">
                    ✗ ${item.type === 'datasource' ? '📊' : '📈'} ${item.name}
                    ${item.error ? `<br><small>${item.error}</small>` : ''}
                </div>
            `;
        });
    }
    
    // Summary
    const total = (results.successful?.length || 0) + (results.failed?.length || 0);
    const successCount = results.successful?.length || 0;
    
    html += `
        <div class="result-summary">
            Summary: ${successCount} of ${total} connections updated successfully
        </div>
    `;

    document.getElementById('results-content').innerHTML = html;

    // Reload connections to reflect updates
    setTimeout(() => {
        loadConnections();
    }, 2000);
}

// Show/hide screens
function showAuthScreen() {
    authScreen.classList.add('active');
    connectionsScreen.classList.remove('active');
}

function showConnectionsScreen() {
    authScreen.classList.remove('active');
    connectionsScreen.classList.add('active');
}

// Show error message
function showError(message) {
    authError.textContent = message;
    authError.style.display = 'block';
}

// Hide error message
function hideError() {
    authError.style.display = 'none';
}

// Set button loading state
function setButtonLoading(button, isLoading) {
    const textSpan = button.querySelector('.btn-text');
    const loadingSpan = button.querySelector('.btn-loading');
    
    if (isLoading) {
        textSpan.style.display = 'none';
        loadingSpan.style.display = 'inline';
        button.disabled = true;
    } else {
        textSpan.style.display = 'inline';
        loadingSpan.style.display = 'none';
        button.disabled = false;
    }
}
