using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace TabDbMigrateUI.Services;

public class CredentialsService
{
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TableauConnectionManager"
    );
    
    private static readonly string CredentialsFile = Path.Combine(AppDataFolder, "credentials.json");

    public class SavedCredentials
    {
        public string ServerUrl { get; set; } = string.Empty;
        public string TokenName { get; set; } = string.Empty;
        public string TokenSecret { get; set; } = string.Empty;
        public string SiteName { get; set; } = string.Empty;
        public DateTime LastUsed { get; set; }
    }

    public async Task<SavedCredentials?> LoadCredentialsAsync()
    {
        try
        {
            if (!File.Exists(CredentialsFile))
                return null;

            var json = await File.ReadAllTextAsync(CredentialsFile);
            return JsonSerializer.Deserialize<SavedCredentials>(json);
        }
        catch
        {
            // If there's any error reading/parsing, return null
            return null;
        }
    }

    public async Task SaveCredentialsAsync(string serverUrl, string tokenName, string tokenSecret, string siteName)
    {
        try
        {
            // Ensure directory exists
            Directory.CreateDirectory(AppDataFolder);

            var credentials = new SavedCredentials
            {
                ServerUrl = serverUrl,
                TokenName = tokenName,
                TokenSecret = tokenSecret,
                SiteName = siteName,
                LastUsed = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(credentials, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            await File.WriteAllTextAsync(CredentialsFile, json);
        }
        catch
        {
            // Silently fail if we can't save - not critical
        }
    }

    public void ClearCredentials()
    {
        try
        {
            if (File.Exists(CredentialsFile))
                File.Delete(CredentialsFile);
        }
        catch
        {
            // Silently fail
        }
    }
}
