# Tableau Data Source Connection Manager

A command-line tool for managing and batch-updating data source connections in Tableau Cloud and Tableau Server. This tool allows you to efficiently update database credentials across multiple data sources that share the same connection details.

## Features

- 🔐 **Secure Authentication** - Uses Personal Access Tokens (PAT) for Tableau Cloud authentication
- 📊 **Smart Grouping** - Automatically groups connections by unique server/port/username combinations
- 🔄 **Batch Updates** - Update multiple data source connections at once
- 💻 **Interactive Console** - User-friendly command-line interface
- 🔒 **Secure Password Input** - Masked password entry for security
- ✅ **Validation & Confirmation** - Shows what will be updated before making changes

## What It Does

The tool performs the following operations:

1. **Authenticates** with Tableau Cloud using your Personal Access Token
2. **Enumerates** all data sources and their connections on your site
3. **Groups** connections by unique combinations of server address, port, and username
4. **Displays** a numbered list of unique connections with the data sources that use them
5. **Allows** you to select a connection group to update
6. **Prompts** for new connection details (server, port, username, password)
7. **Updates** all data sources that use the selected connection in one batch operation

### Example Use Case

If you have 10 data sources all connecting to the same database with the same credentials, instead of updating each one individually, you can update all 10 at once by modifying a single entry in the list.

## Compatibility

This tool works with:
- ✅ **Tableau Cloud** - All sites
- ✅ **Tableau Server** - Version 2019.4 and later (when PAT authentication was introduced)

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- A Tableau Cloud or Tableau Server account with:
  - Personal Access Token (PAT)
  - Permission to modify data source connections
- Git (for cloning the repository)

## Installation

### 1. Clone the Repository

```bash
git clone https://github.com/rkrogers/tab-db-migrate.git
cd tab-db-migrate
```

### 2. Build the Application

The application can be built on Windows, macOS, and Linux.

#### On Windows

```powershell
cd tab-db-migrate
dotnet build -c Release
```

#### On macOS/Linux

```bash
cd tab-db-migrate
dotnet build -c Release
```

### 3. Create a Standalone Executable (Optional)

To create a self-contained executable that doesn't require .NET to be installed:

#### Windows (x64)

```powershell
dotnet publish -c Release -r win-x64 --self-contained
```

The executable will be in: `tab-db-migrate/bin/Release/net9.0/win-x64/publish/tab-db-migrate.exe`

#### macOS (Apple Silicon)

```bash
dotnet publish -c Release -r osx-arm64 --self-contained
```

The executable will be in: `tab-db-migrate/bin/Release/net9.0/osx-arm64/publish/tab-db-migrate`

#### macOS (Intel)

```bash
dotnet publish -c Release -r osx-x64 --self-contained
```

#### Linux (x64)

```bash
dotnet publish -c Release -r linux-x64 --self-contained
```

The executable will be in: `tab-db-migrate/bin/Release/net9.0/linux-x64/publish/tab-db-migrate`

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
- PAT token name
- PAT token secret (hidden input)
- Site name

#### Option 2: Command-Line Arguments

```bash
dotnet run "token-name" "token-secret" "site-name"
```

Example:
```bash
dotnet run "my-pat-token" "abc123xyz789..." "mysite"
```

#### Option 3: Using the Published Executable

If you created a standalone executable:

**Windows:**
```powershell
.\tab-db-migrate.exe "token-name" "token-secret" "site-name"
```

**macOS/Linux:**
```bash
./tab-db-migrate "token-name" "token-secret" "site-name"
```

### Workflow

1. **Authentication**: The tool authenticates with Tableau Cloud
2. **Enumeration**: Displays all unique connection combinations with their IDs
3. **Selection**: Enter the ID number of the connection you want to modify
4. **Input**: Provide new connection details:
   - Server address
   - Server port
   - Username
   - Password (masked input)
5. **Confirmation**: Review the changes and confirm
6. **Update**: The tool updates all matching connections
7. **Results**: See a summary of successful/failed updates

### Example Session

```
Tableau Data Source Connection Manager
======================================

Authenticating with Tableau Cloud using PAT...
✓ Authentication successful!

Enumerating data sources and connections...

================================================================================
UNIQUE DATA SOURCE CONNECTIONS
================================================================================

[1] Server: db.example.com, Port: 5432, Username: admin
    Used by 5 connection(s):
      - Sales Data
      - Customer Data
      - Product Catalog
      - Orders Database
      - Inventory System

[2] Server: warehouse.example.com, Port: 1521, Username: etl_user
    Used by 2 connection(s):
      - Data Warehouse
      - Analytics DB

================================================================================

Enter the ID of the connection you want to modify [1-2] (or 'q' to quit): 1

Selected connection group #1
Current: Server=db.example.com, Port=5432, Username=admin
This will update 5 connection(s)

New Server Address: db-new.example.com
New Server Port: 5432
New Username: admin
New Password: ********

Proceed with batch update? (y/n): y

Updating connections...
  Updating Sales Data... ✓
  Updating Customer Data... ✓
  Updating Product Catalog... ✓
  Updating Orders Database... ✓
  Updating Inventory System... ✓

✓ Batch update complete!
  Successfully updated: 5

Signing out...
✓ Signed out successfully!
```

## Configuration

### For Tableau Cloud

The default configuration is set for Tableau Cloud. To use with your specific Tableau Cloud instance, edit the `serverUrl` constant at the top of `Program.cs`:

```csharp
const string serverUrl = "https://10ay.online.tableau.com";  // Change to your pod
const string apiVersion = "3.21";
```

### For Tableau Server (On-Premises)

To use with an on-premises Tableau Server, update the `serverUrl` in `Program.cs` to point to your Tableau Server:

```csharp
const string serverUrl = "https://tableau.mycompany.com";  // Your Tableau Server URL
const string apiVersion = "3.21";  // Or your server's API version
```

**Important for Tableau Server:**
- Ensure your Tableau Server is version 2019.4 or later (required for PAT support)
- Use the base server URL without the `/api` path
- The site name should be the content URL of your site (use empty string `""` for the Default site)
- Verify that Personal Access Tokens are enabled on your server

**Example for Default Site:**
```bash
dotnet run "my-pat-token" "abc123xyz789..." ""
```

**Example for Named Site:**
```bash
dotnet run "my-pat-token" "abc123xyz789..." "finance"
```

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

### "Failed to query data sources"
- Verify your account has permission to view data sources
- Check that the site name matches your Tableau Cloud site

### "Failed to update connection"
- Ensure your account has permission to modify data source connections
- Verify the new connection details are valid
- Check that the data source connection supports updates

## Technical Details

### Built With

- [.NET 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Tableau REST API](https://help.tableau.com/current/api/rest_api/en-us/REST/rest_api.htm)

### API Endpoints Used

- `POST /api/{version}/auth/signin` - Authentication with PAT
- `GET /api/{version}/sites/{site-id}/datasources` - Query data sources
- `GET /api/{version}/sites/{site-id}/datasources/{datasource-id}/connections` - Query connections
- `PUT /api/{version}/sites/{site-id}/datasources/{datasource-id}/connections/{connection-id}` - Update connection
- `POST /api/{version}/auth/signout` - Sign out

## Project Structure

```
tab-db-migrate/
├── tab-db-migrate/
│   ├── Program.cs              # Main application and user interface
│   ├── TableauAuthenticator.cs # PAT authentication handling
│   ├── List.cs                 # Data source enumeration and updates
│   └── tab-db-migrate.csproj   # Project configuration
├── tab-db-migrate.sln          # Solution file
└── README.md                   # This file
```

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## License

This project is provided as-is for use with Tableau Cloud.

## Support

For issues or questions:
- Open an issue on GitHub
- Refer to [Tableau REST API Documentation](https://help.tableau.com/current/api/rest_api/en-us/REST/rest_api.htm)

## Links

- [.NET SDK Download](https://dotnet.microsoft.com/download)
- [Tableau Cloud](https://www.tableau.com/products/cloud)
- [Tableau REST API Documentation](https://help.tableau.com/current/api/rest_api/en-us/REST/rest_api.htm)
- [Personal Access Tokens Guide](https://help.tableau.com/current/server/en-us/security_personal_access_tokens.htm)
