# Tableau Connection Manager - Web Application

A professional web-based interface for managing Tableau data source and workbook connections across your Tableau Cloud/Server environment.

## Features

- 🔐 **Secure Authentication** - PAT (Personal Access Token) based authentication
- 🔍 **Connection Discovery** - Automatically enumerate all data sources and workbooks
- 📊 **Unique Connection Grouping** - Groups connections by server, port, and username
- ✏️ **Bulk Updates** - Update multiple connections with new credentials in one action
- 📈 **Real-time Status** - See which assets will be affected before updating
- 🎨 **Enterprise UI** - Professional, clean interface with responsive design

## Prerequisites

- Node.js 18+ and npm
- Tableau Cloud or Tableau Server account
- Personal Access Token (PAT) with appropriate permissions

## Installation

1. Navigate to the web app directory:
   ```bash
   cd tab-db-migrate-web
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

## Running the Application

Start the server:
```bash
npm start
```

The application will be available at: **http://localhost:3000**

## Usage

### 1. Authentication

1. Open your browser to `http://localhost:3000`
2. Enter your Tableau credentials:
   - **Tableau Server URL**: Your Tableau Cloud/Server URL (e.g., `https://10ay.online.tableau.com`)
   - **PAT Token Name**: Your Personal Access Token name
   - **PAT Token Secret**: Your Personal Access Token secret
   - **Site Name**: Your site's content URL (leave blank for default site)
3. Click **Connect**

### 2. Managing Connections

After successful authentication:

1. **View Connections**: Browse the list of unique connections in the left panel
2. **Select Connection**: Click on a connection to see details and affected assets
3. **Update Connection**: 
   - Enter new server address, port, username, and password
   - Review the list of affected data sources and workbooks
   - Click **Update All Connections** to apply changes
4. **View Results**: See which updates succeeded or failed

### 3. Sign Out

Click the **Sign Out** button in the top right to end your session.

## API Endpoints

The application provides the following REST API endpoints:

- `POST /api/auth/signin` - Authenticate with Tableau
- `POST /api/auth/signout` - Sign out from Tableau
- `GET /api/auth/status` - Check authentication status
- `GET /api/connections` - Get all unique connections
- `POST /api/connections/update` - Update connections

## Project Structure

```
tab-db-migrate-web/
├── server.js                 # Express server and API routes
├── lib/
│   ├── tableauAuth.js       # Authentication module
│   └── tableauConnections.js # Connection management module
├── public/
│   ├── index.html           # Main HTML page
│   ├── styles.css           # Enterprise styling
│   └── app.js               # Client-side JavaScript
├── package.json
└── README.md
```

## Security Notes

- Sessions are stored in-memory (not persistent across server restarts)
- For production use, consider:
  - Using HTTPS
  - Implementing persistent session storage (Redis, database)
  - Adding rate limiting
  - Setting secure session cookies
  - Adding CSRF protection

## Troubleshooting

### Connection Errors

If you get authentication errors:
- Verify your Tableau Server URL is correct
- Ensure your PAT has not expired
- Check that your PAT has sufficient permissions
- Verify the site name is correct (or leave blank for default)

### Browser Console Errors

Open browser developer tools (F12) to view detailed error messages in the console.

### Server Not Starting

- Ensure port 3000 is not already in use
- Check that all dependencies are installed (`npm install`)
- Verify Node.js version is 18 or higher (`node --version`)

## Development

To modify the application:

1. **Backend**: Edit `server.js`, `lib/tableauAuth.js`, or `lib/tableauConnections.js`
2. **Frontend**: Edit `public/index.html`, `public/styles.css`, or `public/app.js`
3. **Restart** the server to see changes (for backend files)
4. **Refresh** the browser to see changes (for frontend files)

## Environment Variables

You can customize the port using environment variables:

```bash
PORT=8080 npm start
```

## License

ISC

## Related Projects

- **CLI Version**: See `../tab-db-migrate/` for the command-line interface
- **Desktop UI**: See `../tab-db-migrate-ui/` for the Avalonia desktop application

## Support

For issues or questions, please create an issue on the GitHub repository.
