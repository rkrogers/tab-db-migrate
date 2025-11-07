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
    Console.Write("Enter Tableau Server URL (e.g., https://10ay.online.tableau.com): ");
    serverUrl = Console.ReadLine() ?? "";

    Console.Write("Enter your PAT token name: ");
    tokenName = Console.ReadLine() ?? "";

    Console.Write("Enter your PAT token secret: ");
    tokenSecret = ReadPassword();
    Console.WriteLine();

    Console.Write("Enter site name (leave empty for default site): ");
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

    // Step 2: Enumerate data sources and connections
    Console.WriteLine("Enumerating data sources and connections...\n");
    var lister = new TableauDataSourceLister(serverUrl, apiVersion);
    var inventory = await lister.EnumerateDataSourcesAsync(authResponse.Token, authResponse.SiteId);

    // Step 3: Group connections by unique (serverAddress, serverPort, userName) combinations
    var uniqueConnections = new Dictionary<string, UniqueConnectionGroup>();
    int groupId = 1;

    foreach (var ds in inventory.DataSources)
    {
        foreach (var conn in ds.Connections)
        {
            // Create a unique key based on server, port, and username
            string key = $"{conn.ServerAddress}|{conn.ServerPort}|{conn.UserName}";
            
            if (!uniqueConnections.ContainsKey(key))
            {
                uniqueConnections[key] = new UniqueConnectionGroup
                {
                    Id = groupId++,
                    ServerAddress = conn.ServerAddress,
                    ServerPort = conn.ServerPort,
                    UserName = conn.UserName,
                    Connections = new List<(string DataSourceId, string DataSourceName, string ConnectionId)>()
                };
            }

            uniqueConnections[key].Connections.Add((ds.Id, ds.Name, conn.Id));
        }
    }

    if (uniqueConnections.Count == 0)
    {
        Console.WriteLine("\nNo connections found.");
        await authenticator.SignOutAsync(authResponse.Token, authResponse.SiteId);
        return;
    }

    // Step 4: Display unique connections
    Console.WriteLine("\n" + new string('=', 80));
    Console.WriteLine("UNIQUE DATA SOURCE CONNECTIONS");
    Console.WriteLine(new string('=', 80));
    Console.WriteLine();

    foreach (var group in uniqueConnections.Values.OrderBy(g => g.Id))
    {
        Console.WriteLine($"[{group.Id}] Server: {group.ServerAddress}, Port: {group.ServerPort}, Username: {group.UserName}");
        Console.WriteLine($"    Used by {group.Connections.Count} connection(s):");
        foreach (var (dsId, dsName, connId) in group.Connections)
        {
            Console.WriteLine($"      - {dsName}");
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
    Console.WriteLine($"This will update {selectedGroup.Connections.Count} connection(s)");

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
    Console.WriteLine($"\nThis will update {selectedGroup.Connections.Count} connection(s) in the following data sources:");
    foreach (var (dsId, dsName, connId) in selectedGroup.Connections)
    {
        Console.WriteLine($"  - {dsName}");
    }
    Console.Write("\nProceed with batch update? (y/n): ");
    string? confirm = Console.ReadLine();

    if (confirm?.ToLower() != "y")
    {
        Console.WriteLine("\nUpdate cancelled.");
        await authenticator.SignOutAsync(authResponse.Token, authResponse.SiteId);
        return;
    }

    // Step 8: Update all connections in the group
    Console.WriteLine("\nUpdating connections...");
    int successCount = 0;
    int failCount = 0;

    foreach (var (dsId, dsName, connId) in selectedGroup.Connections)
    {
        Console.Write($"  Updating {dsName}... ");
        bool success = await lister.UpdateDataSourceConnectionAsync(
            authResponse.Token,
            authResponse.SiteId,
            dsId,
            connId,
            newServerAddress,
            newServerPort,
            newUserName,
            newPassword);

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
    public List<(string DataSourceId, string DataSourceName, string ConnectionId)> Connections { get; set; } = new();
}
