// Example usage of TableauAuthenticator
// This demonstrates how to authenticate with Tableau Cloud

using tab_db_migrate;

// Tableau Cloud configuration
const string serverUrl = "https://10ay.online.tableau.com"; // Your Tableau Cloud server URL
const string apiVersion = "3.21"; // API version

Console.WriteLine("Tableau Cloud Authentication with PAT");
Console.WriteLine("======================================\n");

// Option 1: Pass PAT credentials as arguments
if (args.Length >= 2)
{
    string tokenName = args[0];
    string tokenSecret = args[1];
    string siteName = args.Length > 2 ? args[2] : ""; // Optional site name

    await AuthenticateAndDisplay(serverUrl, apiVersion, tokenName, tokenSecret, siteName);
}
else
{
    // Option 2: Prompt for PAT credentials interactively
    Console.Write("Enter your PAT token name: ");
    string? tokenName = Console.ReadLine();

    Console.Write("Enter your PAT token secret: ");
    string? tokenSecret = ReadPassword();
    Console.WriteLine();

    Console.Write("Enter site name (leave empty for default site): ");
    string? siteName = Console.ReadLine() ?? "";

    if (string.IsNullOrWhiteSpace(tokenName) || string.IsNullOrWhiteSpace(tokenSecret))
    {
        Console.WriteLine("Error: PAT token name and secret are required.");
        return;
    }

    await AuthenticateAndDisplay(serverUrl, apiVersion, tokenName, tokenSecret, siteName);
}

// Helper method to authenticate and display results using PAT
static async Task AuthenticateAndDisplay(string serverUrl, string apiVersion, string tokenName, string tokenSecret, string siteName)
{
    try
    {
        var authenticator = new TableauAuthenticator(serverUrl, apiVersion);

        Console.WriteLine("\nAuthenticating with Tableau Cloud using PAT...");
        var authResponse = await authenticator.SignInWithPATAsync(tokenName, tokenSecret, siteName);

        Console.WriteLine("\n✓ Authentication successful!\n");
        Console.WriteLine("Authentication Details:");
        Console.WriteLine("----------------------");
        Console.WriteLine(authResponse.ToString());

        // Enumerate data sources and their connections
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("Enumerating Data Sources and Connections");
        Console.WriteLine(new string('=', 60) + "\n");
        
        var lister = new TableauDataSourceLister(serverUrl, apiVersion);
        var inventory = await lister.EnumerateDataSourcesAsync(authResponse.Token, authResponse.SiteId);
        
        Console.WriteLine($"\n✓ Successfully enumerated {inventory.DataSources.Count} data sources");
        Console.WriteLine($"Results stored in TableauDataSourceLister.GlobalInventory");

        // Sign out after enumeration
        Console.WriteLine("\nSigning out...");
        await authenticator.SignOutAsync(authResponse.Token, authResponse.SiteId);
        Console.WriteLine("✓ Signed out successfully!");
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"\n✗ Authentication failed: {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n✗ An error occurred: {ex.Message}");
    }
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
