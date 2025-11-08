# Tableau Connection Manager

A cross-platform tool for managing and batch-updating data source and workbook connections in Tableau Cloud and Tableau Server. Available in both **GUI** and **command-line** versions, this tool allows you to efficiently update database credentials across multiple data sources and workbooks that share the same connection details.

## 🖥️ Two Versions Available

### Desktop GUI Application (NEW!)
A modern, cross-platform desktop application built with Avalonia UI featuring:
- 🎨 **Clean, intuitive interface** - No command-line experience needed
- 🖱️ **Point-and-click operation** - Easy connection management
- 📊 **Visual connection grouping** - See all affected assets at a glance
- 🔄 **Real-time updates** - Watch progress as connections update
- 💻 **Cross-platform** - Runs on Windows, macOS, and Linux

### Command-Line Interface (CLI)
A powerful terminal-based tool perfect for:
- 🤖 **Automation** - Script database credential rotations
- 🔧 **DevOps pipelines** - Integrate with CI/CD workflows
- 🖥️ **Server environments** - No GUI required
- 📜 **Batch operations** - Process multiple updates efficiently

## Features

- 🔐 **Secure Authentication** - Uses Personal Access Tokens (PAT) for Tableau Cloud authentication
- 📊 **Comprehensive Coverage** - Manages connections from both published data sources AND workbooks
- 🔗 **Smart Grouping** - Automatically groups connections by unique server/port/username combinations
- 🔄 **Batch Updates** - Update multiple connections at once across data sources and workbooks
- 💻 **Interactive Console** - User-friendly command-line interface
- 🔒 **Secure Password Input** - Masked password entry for security
- ✅ **Validation & Confirmation** - Shows what will be updated before making changes
- 📈 **Visual Indicators** - Clear icons distinguish data source (📊) from workbook (📈) connections

## What It Does

The tool performs the following operations:

1. **Authenticates** with Tableau Cloud using your Personal Access Token
2. **Enumerates** all data sources and workbooks on your site
3. **Queries** connections from both published data sources and workbook-embedded connections
4. **Groups** connections by unique combinations of server address, port, and username
5. **Displays** a numbered list of unique connections showing which data sources and workbooks use them
6. **Allows** you to select a connection group to update
7. **Prompts** for new connection details (server, port, username, password)
8. **Updates** all data sources and workbooks that use the selected connection in one batch operation

### Example Use Case

If you have 5 data sources and 10 workbooks all connecting to the same database with the same credentials, instead of updating each one individually (15 updates!), you can update all 15 at once by modifying a single entry in the list.

**Real-World Scenario:** You need to update the production database password. With this tool, you can:
- See all 15 assets using that connection in one view
- Update the password once
- Have all 15 connections (data sources + workbooks) updated automatically

## Compatibility

This tool works with:
- ✅ **Tableau Cloud** - All sites
- ✅ **Tableau Server** - Version 2019.4 and later (when PAT authentication was introduced)

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- A Tableau Cloud or Tableau Server account with:
  - Personal Access Token (PAT)
  - Permission to modify data source and workbook connections
- Git (for cloning the repository)

## Installation & Usage

Choose the version that best fits your workflow:

---

## 🎨 Desktop GUI Application

### Installation

#### 1. Clone the Repository

```bash
git clone https://github.com/rkrogers/tab-db-migrate.git
cd tab-db-migrate
```

#### 2. Build the GUI Application

```bash
cd tab-db-migrate-ui
dotnet build -c Release
```

#### 3. Run the GUI Application

**Development Mode:**
```bash
cd tab-db-migrate-ui
dotnet run
```

**From Build Output:**
```bash
cd tab-db-migrate-ui/bin/Release/net9.0
./TabDbMigrateUI        # macOS/Linux
TabDbMigrateUI.exe      # Windows
```

#### 4. Create Standalone Executable (Recommended)

Build a self-contained executable that doesn't require .NET to be installed:

**Windows (x64):**
```powershell
cd tab-db-migrate-ui
dotnet publish -c Release -r win-x64 --self-contained
```
Executable location: `tab-db-migrate-ui/bin/Release/net9.0/win-x64/publish/TabDbMigrateUI.exe`

**macOS (Apple Silicon):**
```bash
cd tab-db-migrate-ui
dotnet publish -c Release -r osx-arm64 --self-contained
```
Executable location: `tab-db-migrate-ui/bin/Release/net9.0/osx-arm64/publish/TabDbMigrateUI`

**macOS (Intel):**
```bash
cd tab-db-migrate-ui
dotnet publish -c Release -r osx-x64 --self-contained
```
Executable location: `tab-db-migrate-ui/bin/Release/net9.0/osx-x64/publish/TabDbMigrateUI`

**Linux (x64):**
```bash
cd tab-db-migrate-ui
dotnet publish -c Release -r linux-x64 --self-contained
```
Executable location: `tab-db-migrate-ui/bin/Release/net9.0/linux-x64/publish/TabDbMigrateUI`

### Using the GUI Application

#### 1. Launch the Application

Double-click the executable or run from terminal:
```bash
./TabDbMigrateUI        # macOS/Linux
TabDbMigrateUI.exe      # Windows (double-click or run from PowerShell)
```

#### 2. Authentication Screen

When the application launches, you'll see the authentication screen:

1. **Tableau Server URL**: Enter your Tableau Cloud or Server URL
   - Example: `https://10ay.online.tableau.com`
   
2. **PAT Token Name**: Your Personal Access Token name

3. **PAT Token Secret**: Your PAT secret (automatically masked)

4. **Site Name**: Your site's content URL (leave blank for default site)
   - Example: `mysite` (not the full URL)

5. Click **Connect**

#### 3. Connection Management Screen

After successful authentication:

**Left Panel - Connection List:**
- View all unique database connections
- Each entry shows:
  - Server address and port
  - Username
  - Number of data sources using this connection
  - Number of workbooks using this connection
- Click a connection to select it

**Right Panel - Update Form:**
- When you select a connection, the form populates with current values
- Enter new connection details:
  - New Server Address
  - New Server Port
  - New Username
  - New Password (automatically masked)
- Click **Update All Connections** to batch-update all assets using this connection
- Watch real-time progress in the results section
- Expand "View Affected Assets" to see the complete list of data sources and workbooks that will be updated

#### 4. Features

- ✅ **Visual Feedback**: Color-coded status messages (green for success, red for errors)
- ✅ **Animated Progress**: Loading indicators during authentication and updates
- ✅ **Smart Pre-population**: Selected connection details auto-fill the update form
- ✅ **Batch Operations**: Update all matching connections with one click
- ✅ **Detailed Results**: See exactly which assets succeeded or failed
- ✅ **Asset Browser**: Expandable list of all affected data sources and workbooks

### GUI Screenshots & Workflow

**Typical Workflow:**
1. **Launch** → Enter Tableau credentials → Click Connect
2. **Browse** → Review the list of unique connections in your site
3. **Select** → Click on a connection to see details and affected assets
4. **Update** → Enter new connection details and click "Update All Connections"
5. **Verify** → Review the results showing which assets were successfully updated

---

## 💻 Command-Line Interface (CLI)

### Installation

#### 1. Clone the Repository

```bash
git clone https://github.com/rkrogers/tab-db-migrate.git
cd tab-db-migrate
```

#### 2. Build the CLI Application

The CLI application can be built on Windows, macOS, and Linux.

**On Windows:**
```powershell
cd tab-db-migrate
dotnet build -c Release
```

**On macOS/Linux:**
```bash
cd tab-db-migrate
dotnet build -c Release
```

#### 3. Create a Standalone Executable (Optional)

To create a self-contained executable that doesn't require .NET to be installed:

**Windows (x64):**
```powershell
cd tab-db-migrate
dotnet publish -c Release -r win-x64 --self-contained
```
Executable location: `tab-db-migrate/bin/Release/net9.0/win-x64/publish/tab-db-migrate.exe`

**macOS (Apple Silicon):**
```bash
cd tab-db-migrate
dotnet publish -c Release -r osx-arm64 --self-contained
```
Executable location: `tab-db-migrate/bin/Release/net9.0/osx-arm64/publish/tab-db-migrate`

**macOS (Intel):**
```bash
cd tab-db-migrate
dotnet publish -c Release -r osx-x64 --self-contained
```

**Linux (x64):**
```bash
cd tab-db-migrate
dotnet publish -c Release -r linux-x64 --self-contained
```
Executable location: `tab-db-migrate/bin/Release/net9.0/linux-x64/publish/tab-db-migrate`

## Usage

### Prerequisites for Running

Before running the tool, you'll need:

1. **Tableau Cloud Server URL** - Your Tableau Cloud site URL (e.g., `https://10ay.online.tableau.com`)
2. **Personal Access Token (PAT)** - Create one in Tableau Cloud:
   - Go to your Tableau Cloud account settings
   - Navigate to "Personal Access Tokens"
   - Click "Create new token"
   - Save the token name and secret
3. **Site Name** - The content URL of your site (e.g., `mysite` from `https://10ay.online.tableau.com/#/site/mysite`)

### Running the Application

#### Option 1: Interactive Mode

```bash
cd tab-db-migrate
dotnet run
```

The application will prompt you for:
- Tableau Server URL
- PAT token name
- PAT token secret (hidden input)
- Site name

#### Option 2: Command-Line Arguments

```bash
dotnet run "server-url" "token-name" "token-secret" "site-name"
```

**Tableau Cloud Example:**
```bash
dotnet run "https://10ay.online.tableau.com" "my-pat-token" "abc123xyz789..." "mysite"
```

**Tableau Server Example:**
```bash
dotnet run "https://tableau.mycompany.com" "my-pat-token" "abc123xyz789..." "finance"
```

**Tableau Server (Default Site) Example:**
```bash
dotnet run "https://tableau.mycompany.com" "my-pat-token" "abc123xyz789..." ""
```

#### Option 3: Using the Published Executable

If you created a standalone executable:

**Windows:**
```powershell
.\tab-db-migrate.exe "server-url" "token-name" "token-secret" "site-name"
```

**macOS/Linux:**
```bash
./tab-db-migrate "server-url" "token-name" "token-secret" "site-name"
```

### Workflow

1. **Authentication**: The tool authenticates with Tableau Cloud
2. **Enumeration**: Queries all data sources and workbooks, displaying unique connection combinations
3. **Selection**: Enter the ID number of the connection you want to modify
4. **Input**: Provide new connection details:
   - Server address
   - Server port
   - Username
   - Password (masked input)
5. **Confirmation**: Review the changes and confirm
6. **Update**: The tool updates all matching connections across data sources and workbooks
7. **Results**: See a summary of successful/failed updates

### Example Session

```
Tableau Data Source Connection Manager
======================================

Authenticating with Tableau Cloud using PAT...
✓ Authentication successful!

Enumerating data sources and workbooks...
Found 12 data sources on the site.
Found 8 workbooks on the site.

================================================================================
UNIQUE CONNECTIONS (Data Sources + Workbooks)
================================================================================

[1] Server: db.example.com, Port: 5432, Username: admin
    Used by 7 connection(s):
      Data Sources: 3, Workbooks: 4
      📊 Sales Data (datasource)
      📊 Customer Data (datasource)
      📊 Product Catalog (datasource)
      📈 Sales Dashboard (workbook)
      📈 Customer Analysis (workbook)
      📈 Executive Summary (workbook)
      📈 Regional Report (workbook)

[2] Server: warehouse.example.com, Port: 1521, Username: etl_user
    Used by 3 connection(s):
      Data Sources: 2, Workbooks: 1
      📊 Data Warehouse (datasource)
      📊 Analytics DB (datasource)
      📈 ETL Monitor (workbook)

[3] Server: 10az.online.tableau.com, Port: 443, Username: 
    Used by 5 connection(s):
      Data Sources: 0, Workbooks: 5
      📈 Admin Insights (workbook)
      📈 User Activity (workbook)
      📈 Content Usage (workbook)
      📈 Performance Metrics (workbook)
      📈 Site Statistics (workbook)

================================================================================

Enter the ID of the connection you want to modify [1-3] (or 'q' to quit): 1

Selected connection group #1
Current: Server=db.example.com, Port=5432, Username=admin
This will update 7 connection(s) (3 data sources, 4 workbooks)

New Server Address: db-new.example.com
New Server Port: 5432
New Username: admin
New Password: ********

Proceed with batch update? (y/n): y

Updating connections...
  Updating Sales Data (datasource)... ✓
  Updating Customer Data (datasource)... ✓
  Updating Product Catalog (datasource)... ✓
  Updating Sales Dashboard (workbook)... ✓
  Updating Customer Analysis (workbook)... ✓
  Updating Executive Summary (workbook)... ✓
  Updating Regional Report (workbook)... ✓

✓ Batch update complete!
  Successfully updated: 7

Signing out...
✓ Signed out successfully!
```

## Connection Types

The tool manages two types of connections:

### 📊 Data Source Connections
Published data sources that are shared across multiple workbooks. These are managed through the Data Sources API.

### 📈 Workbook Connections
Connections embedded directly in workbooks (not using published data sources). These are managed through the Workbooks API.

### Smart Deduplication
Connections are grouped by their unique combination of:
- Server address
- Server port  
- Username

This means a single connection profile might be used by multiple data sources AND workbooks. The tool lets you update all of them at once, regardless of whether they're in published data sources or embedded in workbooks.

## Configuration

### No Code Changes Required!

The application now accepts the server URL as a command-line parameter or interactive prompt, so **you don't need to edit any code** to switch between Tableau Cloud and Tableau Server.

**Simply provide the appropriate server URL when running the application:**

- **For Tableau Cloud:** Use your pod URL (e.g., `https://10ay.online.tableau.com`)
- **For Tableau Server:** Use your on-premises server URL (e.g., `https://tableau.mycompany.com`)

### API Version

The default API version is `3.21`. To use a different version, edit the `apiVersion` constant at the top of `Program.cs`:

```csharp
const string apiVersion = "3.21";  // Change if needed
```

### Important Notes for Tableau Server

When using with on-premises Tableau Server:
- Ensure your Tableau Server is version **2019.4 or later** (required for PAT support)
- Use the base server URL **without** the `/api` path
- For the **Default site**, use an empty string `""` as the site name parameter
- For **named sites**, use the site's content URL (e.g., `finance`, not `https://.../#/site/finance`)
- Verify that Personal Access Tokens are enabled on your server (usually enabled by default)

## Security Considerations

- **Never commit credentials** to version control
- **Use Personal Access Tokens** instead of username/password
- **Rotate tokens regularly** following your security policy
- **Limit PAT permissions** to only what's necessary
- **Store tokens securely** using your organization's secrets management solution

## Troubleshooting

### "Authentication failed: 401 Unauthorized"
- Verify your PAT is valid and not expired
- Ensure the site name is correct
- Check that your account has API access enabled

### "Failed to query data sources" or "Failed to query workbooks"
- Verify your account has permission to view data sources and workbooks
- Check that the site name matches your Tableau Cloud site

### "Failed to update connection"
- Ensure your account has permission to modify data source and workbook connections
- Verify the new connection details are valid
- Check that the data source/workbook connection supports updates

## Technical Details

### Built With

- [.NET 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Tableau REST API](https://help.tableau.com/current/api/rest_api/en-us/REST/rest_api.htm)

### API Endpoints Used

#### Authentication
- `POST /api/{version}/auth/signin` - Authentication with PAT
- `POST /api/{version}/auth/signout` - Sign out

#### Data Sources
- `GET /api/{version}/sites/{site-id}/datasources` - Query data sources
- `GET /api/{version}/sites/{site-id}/datasources/{datasource-id}/connections` - Query data source connections
- `PUT /api/{version}/sites/{site-id}/datasources/{datasource-id}/connections/{connection-id}` - Update data source connection

#### Workbooks
- `GET /api/{version}/sites/{site-id}/workbooks` - Query workbooks
- `GET /api/{version}/sites/{site-id}/workbooks/{workbook-id}/connections` - Query workbook connections
- `PUT /api/{version}/sites/{site-id}/workbooks/{workbook-id}/connections/{connection-id}` - Update workbook connection

## Project Structure

```
tab-db-migrate/
├── tab-db-migrate/              # CLI Version
│   ├── Program.cs              # Main CLI application and user interface
│   ├── TableauAuthenticator.cs # PAT authentication handling
│   ├── List.cs                 # Data source & workbook enumeration and updates
│   └── tab-db-migrate.csproj   # CLI project configuration
├── tab-db-migrate-ui/           # GUI Version (NEW!)
│   ├── Models/
│   │   └── UniqueConnection.cs # Connection grouping model
│   ├── ViewModels/
│   │   ├── ViewModelBase.cs           # Base ViewModel class
│   │   ├── MainWindowViewModel.cs     # Main window orchestration
│   │   ├── AuthenticationViewModel.cs # Login screen logic
│   │   └── ConnectionsViewModel.cs    # Connection management logic
│   ├── Views/
│   │   ├── MainWindow.axaml(.cs)        # Main application window
│   │   ├── AuthenticationView.axaml(.cs) # Login screen UI
│   │   └── ConnectionsView.axaml(.cs)    # Connection management UI
│   ├── TableauAuthenticator.cs # PAT authentication (shared logic)
│   ├── List.cs                 # Data enumeration & updates (shared logic)
│   ├── App.axaml(.cs)          # Avalonia application entry
│   ├── Program.cs              # GUI application entry point
│   ├── ViewLocator.cs          # ViewModel-to-View resolver
│   └── TabDbMigrateUI.csproj   # GUI project configuration
├── tab-db-migrate.sln          # Solution file
└── README.md                   # This file
```

### Architecture Notes

**GUI Version:**
- Built with **Avalonia UI** - Cross-platform XAML-based framework
- Follows **MVVM pattern** - Clean separation of UI and logic
- Uses **CommunityToolkit.Mvvm** - Modern property change notification and commands
- **Reactive UI** - Real-time updates and async operations
- **Shared Backend** - Uses same TableauAuthenticator and List classes as CLI

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## License

This project is provided as-is for use with Tableau Cloud and Tableau Server.

## Support

For issues or questions:
- Open an issue on GitHub
- Refer to [Tableau REST API Documentation](https://help.tableau.com/current/api/rest_api/en-us/REST/rest_api.htm)

## Links

- [.NET SDK Download](https://dotnet.microsoft.com/download)
- [Tableau Cloud](https://www.tableau.com/products/cloud)
- [Tableau REST API Documentation](https://help.tableau.com/current/api/rest_api/en-us/REST/rest_api.htm)
- [Personal Access Tokens Guide](https://help.tableau.com/current/server/en-us/security_personal_access_tokens.htm)
