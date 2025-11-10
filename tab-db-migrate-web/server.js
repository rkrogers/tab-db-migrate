const express = require('express');
const session = require('express-session');
const bodyParser = require('body-parser');
const path = require('path');
const TableauAuth = require('./lib/tableauAuth');
const TableauConnections = require('./lib/tableauConnections');

const app = express();
const PORT = process.env.PORT || 3000;

// Middleware
app.use(bodyParser.json());
app.use(bodyParser.urlencoded({ extended: true }));
app.use(session({
    secret: 'tableau-connection-manager-secret-key',
    resave: false,
    saveUninitialized: false,
    cookie: { secure: false } // Set to true if using HTTPS
}));
app.use(express.static('public'));

// API Routes

/**
 * POST /api/auth/signin
 * Authenticate with Tableau using PAT
 */
app.post('/api/auth/signin', async (req, res) => {
    try {
        const { serverUrl, tokenName, tokenSecret, siteName } = req.body;

        if (!serverUrl || !tokenName || !tokenSecret) {
            return res.status(400).json({ error: 'Missing required fields' });
        }

        const auth = new TableauAuth(serverUrl);
        const authResponse = await auth.signInWithPAT(tokenName, tokenSecret, siteName || '');

        // Store auth details in session
        req.session.auth = authResponse;

        res.json({
            success: true,
            token: authResponse.token,
            siteId: authResponse.siteId,
            userId: authResponse.userId
        });
    } catch (error) {
        console.error('Authentication error:', error.message);
        res.status(401).json({ 
            error: 'Authentication failed', 
            details: error.message 
        });
    }
});

/**
 * POST /api/auth/signout
 * Sign out from Tableau
 */
app.post('/api/auth/signout', async (req, res) => {
    try {
        if (!req.session.auth) {
            return res.status(401).json({ error: 'Not authenticated' });
        }

        const auth = new TableauAuth(req.session.auth.serverUrl);
        await auth.signOut(req.session.auth.token, req.session.auth.siteId);

        req.session.destroy();
        res.json({ success: true });
    } catch (error) {
        console.error('Sign out error:', error.message);
        res.status(500).json({ error: 'Sign out failed', details: error.message });
    }
});

/**
 * GET /api/connections
 * Get all unique connections (data sources + workbooks)
 */
app.get('/api/connections', async (req, res) => {
    try {
        if (!req.session.auth) {
            return res.status(401).json({ error: 'Not authenticated' });
        }

        const connections = new TableauConnections(
            req.session.auth.serverUrl,
            req.session.auth.apiVersion
        );

        // Enumerate data sources
        const dataSources = await connections.enumerateDataSources(
            req.session.auth.token,
            req.session.auth.siteId
        );

        // Enumerate workbooks
        const workbooks = await connections.enumerateWorkbooks(
            req.session.auth.token,
            req.session.auth.siteId
        );

        // Group unique connections
        const uniqueConnections = connections.groupUniqueConnections(dataSources, workbooks);

        res.json({
            success: true,
            uniqueConnections,
            dataSources,
            workbooks
        });
    } catch (error) {
        console.error('Error fetching connections:', error.message);
        res.status(500).json({ 
            error: 'Failed to fetch connections', 
            details: error.message 
        });
    }
});

/**
 * POST /api/connections/update
 * Update connections for a specific unique connection group
 */
app.post('/api/connections/update', async (req, res) => {
    try {
        if (!req.session.auth) {
            return res.status(401).json({ error: 'Not authenticated' });
        }

        const { connections: connectionsToUpdate, serverAddress, serverPort, userName, password } = req.body;

        if (!connectionsToUpdate || !serverAddress || !serverPort || !userName || !password) {
            return res.status(400).json({ error: 'Missing required fields' });
        }

        const connections = new TableauConnections(
            req.session.auth.serverUrl,
            req.session.auth.apiVersion
        );

        const results = await connections.updateConnections(
            req.session.auth.token,
            req.session.auth.siteId,
            connectionsToUpdate,
            serverAddress,
            serverPort,
            userName,
            password
        );

        res.json({
            success: true,
            results
        });
    } catch (error) {
        console.error('Error updating connections:', error.message);
        res.status(500).json({ 
            error: 'Failed to update connections', 
            details: error.message 
        });
    }
});

/**
 * GET /api/auth/status
 * Check if user is authenticated
 */
app.get('/api/auth/status', (req, res) => {
    if (req.session.auth) {
        res.json({ 
            authenticated: true,
            serverUrl: req.session.auth.serverUrl,
            siteId: req.session.auth.siteId
        });
    } else {
        res.json({ authenticated: false });
    }
});

// Serve the main application
app.get('/', (req, res) => {
    res.sendFile(path.join(__dirname, 'public', 'index.html'));
});

// Start server
app.listen(PORT, () => {
    console.log(`╔═══════════════════════════════════════════════════════╗`);
    console.log(`║  Tableau Connection Manager - Web Application        ║`);
    console.log(`╠═══════════════════════════════════════════════════════╣`);
    console.log(`║  Server running on: http://localhost:${PORT}         ║`);
    console.log(`║  Open your browser and navigate to the URL above     ║`);
    console.log(`╚═══════════════════════════════════════════════════════╝`);
});
