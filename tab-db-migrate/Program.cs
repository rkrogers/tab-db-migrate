// Tableau Data Source Connection Manager
// Authenticates with PAT and allows interactive updating of data source connections

using tab_db_migrate;

// Tableau Cloud configuration
const string serverUrl = "https://10ay.online.tableau.com"; // Your Tableau Cloud server URL
const string apiVersion = "3.21"; // API version

Console.WriteLine("Tableau Data Source Connection Manager");
Console.WriteLine("======================================\n");

// Get PAT credentials
string tokenName, tokenSecret, siteName;

if (args.Length >= 2)
{
    tokenName = args[0];
    tokenSecret = args[1];
    siteName = args.Length > 2 ? args[2] : "";
}
else
{
    Console.Write("Enter your PAT token name: ");
    tokenName = Console.ReadLine() ?? "";

    Console.Write("Enter your PAT token secret: ");
    tokenSecret = ReadPassword();
    Console.WriteLine();

    Console.Write("Enter site name (leave empty for default site): ");
    siteName = Console.ReadLine() ?? "";

    if (string.IsNullOrWhiteSpace(tokenName) || string.IsNullOrWhiteSpace(tokenSecret))
    {
        Console.WriteLine("Error: PAT token name and secret are required.");
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

    // Step 3: Display all connections with their details
    Console.WriteLine("\n" + new string('=', 80));
    Console.WriteLine("DATA SOURCE CONNECTIONS");
    Console.WriteLine(new string('=', 80));

    var allConnections = new List<(string DataSourceId, string DataSourceName, ConnectionInfo Connection)>();
    int displayIndex = 1;

    foreach (var ds in inventory.DataSources)
    {
        if (ds.Connections.Count > 0)
        {
            Console.WriteLine($"\nData Source: {ds.Name} (Type: {ds.Type})");
            Console.WriteLine(new string('-', 80));

            foreach (var conn in ds.Connections)
            {
                Console.WriteLine($"  [{displayIndex}] Connection ID: {conn.Id}");
                Console.WriteLine($"      Type: {conn.Type}");
                Console.WriteLine($"      Server Address: {conn.ServerAddress}");
                Console.WriteLine($"      Server Port: {conn.ServerPort}");
                Console.WriteLine($"      Username: {conn.UserName}");
                Console.WriteLine();

                allConnections.Add((ds.Id, ds.Name, conn));
                displayIndex++;
            }
        }
    }

    if (allConnections.Count == 0)
    {
        Console.WriteLine("\nNo connections found.");
        await authenticator.SignOutAsync(authResponse.Token, authResponse.SiteId);
        return;
    }

    // Step 4: Ask user which connection to modify
    Console.WriteLine(new string('=', 80));
    Console.Write($"\nEnter the number of the connection you want to modify [1-{allConnections.Count}] (or 'q' to quit): ");
    string? selection = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(selection) || selection.ToLower() == "q")
    {
        Console.WriteLine("\nExiting without making changes.");
        await authenticator.SignOutAsync(authResponse.Token, authResponse.SiteId);
        return;
    }

    if (!int.TryParse(selection, out int selectedIndex) || selectedIndex < 1 || selectedIndex > allConnections.Count)
    {
        Console.WriteLine("\nInvalid selection.");
        await authenticator.SignOutAsync(authResponse.Token, authResponse.SiteId);
        return;
    }

    var selectedConnection = allConnections[selectedIndex - 1];
    Console.WriteLine($"\nSelected: {selectedConnection.DataSourceName} - Connection {selectedConnection.Connection.Id}");

    // Step 5: Prompt for new connection details
    Console.WriteLine("\n" + new string('=', 80));
    Console.WriteLine("ENTER NEW CONNECTION DETAILS");
    Console.WriteLine(new string('=', 80));
    Console.WriteLine("(Press Enter to keep current value)\n");

    Console.Write($"Server Address [{selectedConnection.Connection.ServerAddress}]: ");
    string? newServerAddress = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(newServerAddress))
        newServerAddress = selectedConnection.Connection.ServerAddress;

    Console.Write($"Server Port [{selectedConnection.Connection.ServerPort}]: ");
    string? newServerPort = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(newServerPort))
        newServerPort = selectedConnection.Connection.ServerPort;

    Console.Write($"Username [{selectedConnection.Connection.UserName}]: ");
    string? newUserName = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(newUserName))
        newUserName = selectedConnection.Connection.UserName ?? "";

    Console.Write("Password: ");
    string newPassword = ReadPassword();
    Console.WriteLine();

    if (string.IsNullOrWhiteSpace(newPassword))
    {
        Console.WriteLine("\n✗ Password is required to update the connection.");
        await authenticator.SignOutAsync(authResponse.Token, authResponse.SiteId);
        return;
    }

    // Step 6: Confirm update
    Console.WriteLine("\n" + new string('=', 80));
    Console.WriteLine("CONFIRM UPDATE");
    Console.WriteLine(new string('=', 80));
    Console.WriteLine($"Data Source: {selectedConnection.DataSourceName}");
    Console.WriteLine($"Connection ID: {selectedConnection.Connection.Id}");
    Console.WriteLine($"New Server Address: {newServerAddress}");
    Console.WriteLine($"New Server Port: {newServerPort}");
    Console.WriteLine($"New Username: {newUserName}");
    Console.WriteLine($"Password: ********");
    Console.Write("\nProceed with update? (y/n): ");
    string? confirm = Console.ReadLine();

    if (confirm?.ToLower() != "y")
    {
        Console.WriteLine("\nUpdate cancelled.");
        await authenticator.SignOutAsync(authResponse.Token, authResponse.SiteId);
        return;
    }

    // Step 7: Update the connection
    Console.WriteLine("\nUpdating connection...");
    bool success = await lister.UpdateDataSourceConnectionAsync(
        authResponse.Token,
        authResponse.SiteId,
        selectedConnection.DataSourceId,
        selectedConnection.Connection.Id,
        newServerAddress,
        newServerPort,
        newUserName,
        newPassword);

    if (success)
    {
        Console.WriteLine("\n✓ Connection updated successfully!");
    }
    else
    {
        Console.WriteLine("\n✗ Failed to update connection. See error messages above.");
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
