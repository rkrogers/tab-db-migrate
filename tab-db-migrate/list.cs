using System.Text.Json;
using System.Text.Json.Serialization;

namespace tab_db_migrate;

/// <summary>
/// Handles listing and enumerating data sources and their connections from Tableau Cloud
/// </summary>
public class TableauDataSourceLister
{
    private readonly string _serverUrl;
    private readonly string _apiVersion;
    private readonly HttpClient _httpClient;

    // Global variable to store the results
    public static DataSourceInventory? GlobalInventory { get; set; }

    /// <summary>
    /// Initializes a new instance of the TableauDataSourceLister
    /// </summary>
    /// <param name="serverUrl">The Tableau Cloud server URL</param>
    /// <param name="apiVersion">The Tableau REST API version (e.g., "3.21")</param>
    public TableauDataSourceLister(string serverUrl, string apiVersion = "3.21")
    {
        _serverUrl = serverUrl.TrimEnd('/');
        _apiVersion = apiVersion;
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Enumerates all data sources and their connections on a specified site
    /// </summary>
    /// <param name="authToken">The authentication token from TableauAuthenticator</param>
    /// <param name="siteId">The site ID from TableauAuthenticator</param>
    /// <returns>DataSourceInventory containing all data sources and their connections</returns>
    public async Task<DataSourceInventory> EnumerateDataSourcesAsync(string authToken, string siteId)
    {
        var inventory = new DataSourceInventory
        {
            SiteId = siteId,
            EnumeratedAt = DateTime.UtcNow,
            DataSources = new List<DataSourceInfo>()
        };

        try
        {
            // Step 1: Query all data sources
            var dataSources = await QueryDataSourcesAsync(authToken, siteId);
            
            Console.WriteLine($"Found {dataSources.Count} data sources on the site.");

            // Step 2: For each data source, query its connections
            foreach (var dataSource in dataSources)
            {
                Console.WriteLine($"Querying connections for data source: {dataSource.Name} (ID: {dataSource.Id})");
                
                var connections = await QueryDataSourceConnectionsAsync(authToken, siteId, dataSource.Id);
                
                var dataSourceInfo = new DataSourceInfo
                {
                    Id = dataSource.Id,
                    Name = dataSource.Name,
                    ContentUrl = dataSource.ContentUrl,
                    Type = dataSource.Type,
                    ProjectName = dataSource.Project?.Name,
                    Connections = connections
                };

                inventory.DataSources.Add(dataSourceInfo);
            }

            // Store in global variable
            GlobalInventory = inventory;

            // Output to console as JSON
            var jsonOutput = JsonSerializer.Serialize(inventory, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            Console.WriteLine("\n=== Data Source Inventory (JSON) ===");
            Console.WriteLine(jsonOutput);
            Console.WriteLine("====================================\n");

            return inventory;
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Failed to enumerate data sources: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Queries all data sources on the specified site
    /// </summary>
    private async Task<List<DataSourceResponse>> QueryDataSourcesAsync(string authToken, string siteId)
    {
        var url = $"{_serverUrl}/api/{_apiVersion}/sites/{siteId}/datasources";
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Tableau-Auth", authToken);
        request.Headers.Add("Accept", "application/json");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Failed to query data sources. Status: {response.StatusCode}. Response: {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var dataSourcesResponse = JsonSerializer.Deserialize<QueryDataSourcesResponse>(responseContent, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return dataSourcesResponse?.Datasources?.Datasource ?? new List<DataSourceResponse>();
    }

    /// <summary>
    /// Queries all connections for a specific data source
    /// </summary>
    private async Task<List<ConnectionInfo>> QueryDataSourceConnectionsAsync(string authToken, string siteId, string dataSourceId)
    {
        var url = $"{_serverUrl}/api/{_apiVersion}/sites/{siteId}/datasources/{dataSourceId}/connections";
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Tableau-Auth", authToken);
        request.Headers.Add("Accept", "application/json");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Warning: Failed to query connections for data source {dataSourceId}. Status: {response.StatusCode}");
            return new List<ConnectionInfo>();
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var connectionsResponse = JsonSerializer.Deserialize<QueryConnectionsResponse>(responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var connections = new List<ConnectionInfo>();
        
        if (connectionsResponse?.Connections?.Connection != null)
        {
            foreach (var conn in connectionsResponse.Connections.Connection)
            {
                connections.Add(new ConnectionInfo
                {
                    Id = conn.Id,
                    Type = conn.Type,
                    ServerAddress = conn.ServerAddress,
                    ServerPort = conn.ServerPort,
                    UserName = conn.UserName
                });
            }
        }

        return connections;
    }
}

#region API Response Models

internal class QueryDataSourcesResponse
{
    [JsonPropertyName("datasources")]
    public DataSourcesWrapper? Datasources { get; set; }
}

internal class DataSourcesWrapper
{
    [JsonPropertyName("datasource")]
    public List<DataSourceResponse>? Datasource { get; set; }
}

internal class DataSourceResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("contentUrl")]
    public string ContentUrl { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("project")]
    public ProjectInfo? Project { get; set; }
}

internal class ProjectInfo
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

internal class QueryConnectionsResponse
{
    [JsonPropertyName("connections")]
    public ConnectionsWrapper? Connections { get; set; }
}

internal class ConnectionsWrapper
{
    [JsonPropertyName("connection")]
    public List<ConnectionResponse>? Connection { get; set; }
}

internal class ConnectionResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("serverAddress")]
    public string ServerAddress { get; set; } = string.Empty;

    [JsonPropertyName("serverPort")]
    public string ServerPort { get; set; } = string.Empty;

    [JsonPropertyName("userName")]
    public string? UserName { get; set; }
}

#endregion

#region Output Models

/// <summary>
/// Represents the complete inventory of data sources and their connections
/// </summary>
public class DataSourceInventory
{
    [JsonPropertyName("siteId")]
    public string SiteId { get; set; } = string.Empty;

    [JsonPropertyName("enumeratedAt")]
    public DateTime EnumeratedAt { get; set; }

    [JsonPropertyName("dataSources")]
    public List<DataSourceInfo> DataSources { get; set; } = new();
}

/// <summary>
/// Represents a data source and its connections
/// </summary>
public class DataSourceInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("contentUrl")]
    public string ContentUrl { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("projectName")]
    public string? ProjectName { get; set; }

    [JsonPropertyName("connections")]
    public List<ConnectionInfo> Connections { get; set; } = new();
}

/// <summary>
/// Represents a data source connection
/// </summary>
public class ConnectionInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("serverAddress")]
    public string ServerAddress { get; set; } = string.Empty;

    [JsonPropertyName("serverPort")]
    public string ServerPort { get; set; } = string.Empty;

    [JsonPropertyName("userName")]
    public string? UserName { get; set; }
}

#endregion
