// Tableau Data Source Connection Manager
// Authenticates with PAT and allows batch updating of data source connections

using tab_db_migrate;

// API version configuration
const string apiVersion = "3.21";

Console.WriteLine("Tableau Data Source Connection Manager");
Console.WriteLine("======================================\n");

// Get configuration
string serverUrl, tokenName, tokenSecret, siteName;

if (args.Length >= 3)
{
    // Command-line arguments: serverUrl tokenName tokenSecret [siteName]
    serverUrl = args[0];
    tokenName = args[1];
    tokenSecret = args[2];
    siteName = args.Length > 3 ? args[3] : "";
}
else
{
    // Display tool information when running interactively
    Console.WriteLine("ABOUT THIS TOOL");
    Console.WriteLine(new string('-', 80));
    Console.WriteLine("This tool helps you efficiently manage data source connections in Tableau Cloud");
    Console.WriteLine("and Tableau Server. It allows you to:");
    Console.WriteLine();
    Console.WriteLine("  • Group connections by unique server/port/username combinations");
    Console.WriteLine("  • Update multiple data source AND workbook connections in one batch operation");
    Console.WriteLine("  • Save time when changing database credentials across many assets");
    Console.WriteLine();
    Console.WriteLine("Compatible with:");
    Console.WriteLine("  ✓ Tableau Cloud (all sites)");
    Console.WriteLine("  ✓ Tableau Server 2019.4+ (requires Personal Access Token support)");
    Console.WriteLine();
    Console.WriteLine(new string('-', 80));
    Console.WriteLine();
    Console.WriteLine("AUTHENTICATION REQUIRED");
    Console.WriteLine(new string('-', 80));
    Console.WriteLine("You'll need to provide:");
    Console.WriteLine("  1. Your Tableau Server or Cloud URL");
    Console.WriteLine("  2. A Personal Access Token (PAT) - not username/password");
    Console.WriteLine("  3. Your site name (or leave blank for Default site)");
    Console.WriteLine();
    Console.WriteLine("To create a PAT:");
    Console.WriteLine("  • Tableau Cloud: Account Settings → Personal Access Tokens");
    Console.WriteLine("  • Tableau Server: My Account Settings → Personal Access Tokens");
    Console.WriteLine();
    Console.WriteLine(new string('-', 80));
    Console.WriteLine();

    // Prompt for authentication details
    Console.Write("Enter Tableau Server URL (e.g., https://10ay.online.tableau.com): ");
    serverUrl = Console.ReadLine() ?? "";

    Console.Write("Enter your PAT token name: ");
    tokenName = Console.ReadLine() ?? "";

    Console.Write("Enter your PAT token secret: ");
    tokenSecret = ReadPassword();
    Console.WriteLine();

    Console.Write("Enter site name (leave blank for Default site): ");
    siteName = Console.ReadLine() ?? "";

    if (string.IsNullOrWhiteSpace(serverUrl) || string.IsNullOrWhiteSpace(tokenName) || string.IsNullOrWhiteSpace(tokenSecret))
    {
        Console.WriteLine("Error: Server URL, PAT token name and secret are required.");
        return;
    }
}

try
{
    // Step 1: Authenticate
    var authenticator = new TableauAuthenticator(serverUrl, apiVersion);
    Console.WriteLine("\nAuthenticating with Tableau Cloud using PAT...");
    var authResponse = await authenticator.SignInWithPATAsync(tokenName, tokenSecret, siteName);
    Console.WriteLine("✓ Authentication successful!\n");

    // Step 2: Enumerate data sources and workbooks with their connections
    Console.WriteLine("Enumerating data sources and workbooks...\n");
    var lister = new TableauDataSourceLister(serverUrl, apiVersion);
    
    var dataSourceInventory = await lister.EnumerateDataSourcesAsync(authResponse.Token, authResponse.SiteId);
    var workbooks = await lister.EnumerateWorkbooksAsync(authResponse.Token, authResponse.SiteId);

    // Step 3: Merge and deduplicate connections from both data sources and workbooks
    var uniqueConnections = new Dictionary<string, UniqueConnectionGroup>();
    int groupId = 1;

    // Add data source connections
    foreach (var ds in dataSourceInventory.DataSources)
    {
        foreach (var conn in ds.Connections)
        {
            string key = $"{conn.ServerAddress}|{conn.ServerPort}|{conn.UserName}";
            
            if (!uniqueConnections.ContainsKey(key))
            {
                uniqueConnections[key] = new UniqueConnectionGroup
                {
                    Id = groupId++,
                    ServerAddress = conn.ServerAddress,
                    ServerPort = conn.ServerPort,
                    UserName = conn.UserName,
                    Sources = new List<ConnectionSource>()
                };
            }

            uniqueConnections[key].Sources.Add(new ConnectionSource
            {
                SourceType = "datasource",
                SourceId = ds.Id,
                SourceName = ds.Name,
                ConnectionId = conn.Id
            });
        }
    }

    // Add workbook connections
    foreach (var wb in workbooks)
    {
        foreach (var conn in wb.Connections)
        {
            string key = $"{conn.ServerAddress}|{conn.ServerPort}|{conn.UserName}";
            
            if (!uniqueConnections.ContainsKey(key))
            {
                uniqueConnections[key] = new UniqueConnectionGroup
                {
                    Id = groupId++,
                    ServerAddress = conn.ServerAddress,
                    ServerPort = conn.ServerPort,
                    UserName = conn.UserName,
                    Sources = new List<ConnectionSource>()
                };
            }

            uniqueConnections[key].Sources.Add(new ConnectionSource
            {
                SourceType = "workbook",
                SourceId = wb.Id,
                SourceName = wb.Name,
                ConnectionId = conn.Id
            });
        }
    }

    if (uniqueConnections.Count == 0)
    {
        Console.WriteLine("\nNo connections found.");
        await authenticator.SignOutAsync(authResponse.Token, authResponse.SiteId);
        return;
    }

    // Step 4: Display unique connections with sources
    Console.WriteLine("\n" + new string('=', 80));
    Console.WriteLine("UNIQUE CONNECTIONS (Data Sources + Workbooks)");
    Console.WriteLine(new string('=', 80));
    Console.WriteLine();

    foreach (var group in uniqueConnections.Values.OrderBy(g => g.Id))
    {
        Console.WriteLine($"[{group.Id}] Server: {group.ServerAddress}, Port: {group.ServerPort}, Username: {group.UserName}");
        Console.WriteLine($"    Used by {group.Sources.Count} connection(s):");
        
        var dataSourceCount = group.Sources.Count(s => s.SourceType == "datasource");
        var workbookCount = group.Sources.Count(s => s.SourceType == "workbook");
        
        Console.WriteLine($"      Data Sources: {dataSourceCount}, Workbooks: {workbookCount}");
        
        foreach (var source in group.Sources)
        {
            string icon = source.SourceType == "datasource" ? "📊" : "📈";
            Console.WriteLine($"      {icon} {source.SourceName} ({source.SourceType})");
        }
        Console.WriteLine();
    }

    // Step 5: Ask user which connection group to modify
    Console.WriteLine(new string('=', 80));
    Console.Write($"\nEnter the ID of the connection you want to modify [1-{uniqueConnections.Count}] (or 'q' to quit): ");
    string? selection = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(selection) || selection.ToLower() == "q")
    {
        Console.WriteLine("\nExiting without making changes.");
        await authenticator.SignOutAsync(authResponse.Token, authResponse.SiteId);
        return;
    }

    if (!int.TryParse(selection, out int selectedId))
    {
        Console.WriteLine("\nInvalid selection.");
        await authenticator.SignOutAsync(authResponse.Token, authResponse.SiteId);
        return;
    }

    var selectedGroup = uniqueConnections.Values.FirstOrDefault(g => g.Id == selectedId);
    if (selectedGroup == null)
    {
        Console.WriteLine("\nInvalid ID.");
        await authenticator.SignOutAsync(authResponse.Token, authResponse.SiteId);
        return;
    }

    Console.WriteLine($"\nSelected connection group #{selectedGroup.Id}");
    Console.WriteLine($"Current: Server={selectedGroup.ServerAddress}, Port={selectedGroup.ServerPort}, Username={selectedGroup.UserName}");
    Console.WriteLine($"This will update {selectedGroup.Sources.Count} connection(s) ({selectedGroup.Sources.Count(s => s.SourceType == "datasource")} data sources, {selectedGroup.Sources.Count(s => s.SourceType == "workbook")} workbooks)");

    // Step 6: Prompt for new connection details
    Console.WriteLine("\n" + new string('=', 80));
    Console.WriteLine("ENTER NEW CONNECTION DETAILS");
    Console.WriteLine(new string('=', 80));
    Console.WriteLine();

    Console.Write($"New Server Address: ");
    string? newServerAddress = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(newServerAddress))
    {
        Console.WriteLine("\n✗ Server address is required.");
        await authenticator.SignOutAsync(authResponse.Token, authResponse.SiteId);
        return;
    }

    Console.Write($"New Server Port: ");
    string? newServerPort = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(newServerPort))
    {
        Console.WriteLine("\n✗ Server port is required.");
        await authenticator.SignOutAsync(authResponse.Token, authResponse.SiteId);
        return;
    }

    Console.Write($"New Username: ");
    string? newUserName = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(newUserName))
    {
        Console.WriteLine("\n✗ Username is required.");
        await authenticator.SignOutAsync(authResponse.Token, authResponse.SiteId);
        return;
    }

    Console.Write("New Password: ");
    string newPassword = ReadPassword();
    Console.WriteLine();

    if (string.IsNullOrWhiteSpace(newPassword))
    {
        Console.WriteLine("\n✗ Password is required.");
        await authenticator.SignOutAsync(authResponse.Token, authResponse.SiteId);
        return;
    }

    // Step 7: Confirm update
    Console.WriteLine("\n" + new string('=', 80));
    Console.WriteLine("CONFIRM BATCH UPDATE");
    Console.WriteLine(new string('=', 80));
    Console.WriteLine($"Old: Server={selectedGroup.ServerAddress}, Port={selectedGroup.ServerPort}, Username={selectedGroup.UserName}");
    Console.WriteLine($"New: Server={newServerAddress}, Port={newServerPort}, Username={newUserName}");
    Console.WriteLine($"\nThis will update {selectedGroup.Sources.Count} connection(s):");
    foreach (var source in selectedGroup.Sources)
    {
        Console.WriteLine($"  • {source.SourceName} ({source.SourceType})");
    }
    Console.Write("\nProceed with batch update? (y/n): ");
    string? confirm = Console.ReadLine();

    if (confirm?.ToLower() != "y")
    {
        Console.WriteLine("\nUpdate cancelled.");
        await authenticator.SignOutAsync(authResponse.Token, authResponse.SiteId);
        return;
    }

    // Step 8: Update all connections in the group (data sources and workbooks)
    Console.WriteLine("\nUpdating connections...");
    int successCount = 0;
    int failCount = 0;

    foreach (var source in selectedGroup.Sources)
    {
        Console.Write($"  Updating {source.SourceName} ({source.SourceType})... ");
        bool success;

        if (source.SourceType == "datasource")
        {
            success = await lister.UpdateDataSourceConnectionAsync(
                authResponse.Token,
                authResponse.SiteId,
                source.SourceId,
                source.ConnectionId,
                newServerAddress,
                newServerPort,
                newUserName,
                newPassword);
        }
        else // workbook
        {
            success = await lister.UpdateWorkbookConnectionAsync(
                authResponse.Token,
                authResponse.SiteId,
                source.SourceId,
                source.ConnectionId,
                newServerAddress,
                newServerPort,
                newUserName,
                newPassword);
        }

        if (success)
        {
            successCount++;
        }
        else
        {
            failCount++;
        }
    }

    Console.WriteLine($"\n✓ Batch update complete!");
    Console.WriteLine($"  Successfully updated: {successCount}");
    if (failCount > 0)
    {
        Console.WriteLine($"  Failed: {failCount}");
    }

    // Sign out
    Console.WriteLine("\nSigning out...");
    await authenticator.SignOutAsync(authResponse.Token, authResponse.SiteId);
    Console.WriteLine("✓ Signed out successfully!");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"\n✗ Error: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"\n✗ An unexpected error occurred: {ex.Message}");
}

// Helper method to read password without displaying it
static string ReadPassword()
{
    string password = "";
    ConsoleKeyInfo key;

    do
    {
        key = Console.ReadKey(true);

        if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
        {
            password += key.KeyChar;
            Console.Write("*");
        }
        else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
        {
            password = password[0..^1];
            Console.Write("\b \b");
        }
    }
    while (key.Key != ConsoleKey.Enter);

    return password;
}

// Class to represent a group of connections with the same server/port/username
class UniqueConnectionGroup
{
    public int Id { get; set; }
    public string ServerAddress { get; set; } = string.Empty;
    public string ServerPort { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public List<ConnectionSource> Sources { get; set; } = new();
}

// Class to represent a source using a connection (either data source or workbook)
class ConnectionSource
{
    public string SourceType { get; set; } = string.Empty; // "datasource" or "workbook"
    public string SourceId { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
}
