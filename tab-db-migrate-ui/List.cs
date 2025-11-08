using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
        _serverUrl = CleanServerUrl(serverUrl);
        _apiVersion = apiVersion;
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Cleans and validates the server URL by removing common mistakes like site paths
    /// </summary>
    private static string CleanServerUrl(string url)
    {
        url = url.TrimEnd('/');
        
        // Remove common patterns users might include
        // e.g., "https://10ay.online.tableau.com/#/site/mysite" -> "https://10ay.online.tableau.com"
        if (url.Contains("/#/"))
        {
            url = url.Substring(0, url.IndexOf("/#/"));
        }
        
        // Remove "/api" if included
        if (url.EndsWith("/api"))
        {
            url = url.Substring(0, url.Length - 4);
        }
        
        return url;
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

            return inventory;
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Failed to enumerate data sources: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Enumerates all workbooks and their connections on a specified site
    /// </summary>
    /// <param name="authToken">The authentication token from TableauAuthenticator</param>
    /// <param name="siteId">The site ID from TableauAuthenticator</param>
    /// <returns>List of WorkbookInfo containing all workbooks and their connections</returns>
    public async Task<List<WorkbookInfo>> EnumerateWorkbooksAsync(string authToken, string siteId)
    {
        var workbooks = new List<WorkbookInfo>();

        try
        {
            // Step 1: Query all workbooks
            var workbookResponses = await QueryWorkbooksAsync(authToken, siteId);
            
            Console.WriteLine($"Found {workbookResponses.Count} workbooks on the site.");

            // Step 2: For each workbook, query its connections
            foreach (var workbook in workbookResponses)
            {
                Console.WriteLine($"Querying connections for workbook: {workbook.Name} (ID: {workbook.Id})");
                
                var connections = await QueryWorkbookConnectionsAsync(authToken, siteId, workbook.Id);
                
                var workbookInfo = new WorkbookInfo
                {
                    Id = workbook.Id,
                    Name = workbook.Name,
                    ContentUrl = workbook.ContentUrl,
                    ProjectName = workbook.Project?.Name,
                    Connections = connections
                };

                workbooks.Add(workbookInfo);
            }

            return workbooks;
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Failed to enumerate workbooks: {ex.Message}", ex);
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
    /// Queries all workbooks on the specified site
    /// </summary>
    private async Task<List<WorkbookResponse>> QueryWorkbooksAsync(string authToken, string siteId)
    {
        var url = $"{_serverUrl}/api/{_apiVersion}/sites/{siteId}/workbooks";
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Tableau-Auth", authToken);
        request.Headers.Add("Accept", "application/json");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Failed to query workbooks. Status: {response.StatusCode}. Response: {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var workbooksResponse = JsonSerializer.Deserialize<QueryWorkbooksResponse>(responseContent, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return workbooksResponse?.Workbooks?.Workbook ?? new List<WorkbookResponse>();
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
                    UserName = conn.UserName,
                    SourceType = "datasource",
                    ParentId = dataSourceId,
                    ParentName = ""
                });
            }
        }

        return connections;
    }

    /// <summary>
    /// Queries all connections for a specific workbook
    /// </summary>
    private async Task<List<ConnectionInfo>> QueryWorkbookConnectionsAsync(string authToken, string siteId, string workbookId)
    {
        var url = $"{_serverUrl}/api/{_apiVersion}/sites/{siteId}/workbooks/{workbookId}/connections";
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Tableau-Auth", authToken);
        request.Headers.Add("Accept", "application/json");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Warning: Failed to query connections for workbook {workbookId}. Status: {response.StatusCode}");
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
                    UserName = conn.UserName,
                    SourceType = "workbook",
                    ParentId = workbookId,
                    ParentName = ""
                });
            }
        }

        return connections;
    }

    /// <summary>
    /// Updates a data source connection with new credentials and server information
    /// </summary>
    /// <param name="authToken">The authentication token</param>
    /// <param name="siteId">The site ID</param>
    /// <param name="dataSourceId">The data source ID</param>
    /// <param name="connectionId">The connection ID to update</param>
    /// <param name="serverAddress">New server address</param>
    /// <param name="serverPort">New server port</param>
    /// <param name="userName">New username</param>
    /// <param name="password">New password</param>
    /// <returns>True if update successful, false otherwise</returns>
    public async Task<bool> UpdateDataSourceConnectionAsync(
        string authToken, 
        string siteId, 
        string dataSourceId, 
        string connectionId,
        string serverAddress,
        string serverPort,
        string userName,
        string password)
    {
        var url = $"{_serverUrl}/api/{_apiVersion}/sites/{siteId}/datasources/{dataSourceId}/connections/{connectionId}";

        var updateRequest = new UpdateConnectionRequest
        {
            Connection = new UpdateConnectionData
            {
                ServerAddress = serverAddress,
                ServerPort = serverPort,
                UserName = userName,
                Password = password
            }
        };

        var jsonContent = JsonSerializer.Serialize(updateRequest, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = content
        };
        request.Headers.Add("X-Tableau-Auth", authToken);
        request.Headers.Add("Accept", "application/json");

        try
        {
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"✗ Failed to update connection. Status: {response.StatusCode}");
                Console.WriteLine($"Error: {errorContent}");
                return false;
            }

            Console.WriteLine("✓ Connection updated successfully!");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error updating connection: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Updates a workbook connection with new credentials and server information
    /// </summary>
    /// <param name="authToken">The authentication token</param>
    /// <param name="siteId">The site ID</param>
    /// <param name="workbookId">The workbook ID</param>
    /// <param name="connectionId">The connection ID to update</param>
    /// <param name="serverAddress">New server address</param>
    /// <param name="serverPort">New server port</param>
    /// <param name="userName">New username</param>
    /// <param name="password">New password</param>
    /// <returns>True if update successful, false otherwise</returns>
    public async Task<bool> UpdateWorkbookConnectionAsync(
        string authToken, 
        string siteId, 
        string workbookId, 
        string connectionId,
        string serverAddress,
        string serverPort,
        string userName,
        string password)
    {
        var url = $"{_serverUrl}/api/{_apiVersion}/sites/{siteId}/workbooks/{workbookId}/connections/{connectionId}";

        var updateRequest = new UpdateConnectionRequest
        {
            Connection = new UpdateConnectionData
            {
                ServerAddress = serverAddress,
                ServerPort = serverPort,
                UserName = userName,
                Password = password
            }
        };

        var jsonContent = JsonSerializer.Serialize(updateRequest, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = content
        };
        request.Headers.Add("X-Tableau-Auth", authToken);
        request.Headers.Add("Accept", "application/json");

        try
        {
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"✗ Failed to update workbook connection. Status: {response.StatusCode}");
                Console.WriteLine($"Error: {errorContent}");
                return false;
            }

            Console.WriteLine("✓ Workbook connection updated successfully!");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error updating workbook connection: {ex.Message}");
            return false;
        }
    }
}

#region Update Request Models

internal class UpdateConnectionRequest
{
    [JsonPropertyName("connection")]
    public UpdateConnectionData Connection { get; set; } = new();
}

internal class UpdateConnectionData
{
    [JsonPropertyName("serverAddress")]
    public string ServerAddress { get; set; } = string.Empty;

    [JsonPropertyName("serverPort")]
    public string ServerPort { get; set; } = string.Empty;

    [JsonPropertyName("userName")]
    public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

#endregion

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

internal class QueryWorkbooksResponse
{
    [JsonPropertyName("workbooks")]
    public WorkbooksWrapper? Workbooks { get; set; }
}

internal class WorkbooksWrapper
{
    [JsonPropertyName("workbook")]
    public List<WorkbookResponse>? Workbook { get; set; }
}

internal class WorkbookResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("contentUrl")]
    public string ContentUrl { get; set; } = string.Empty;

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
/// Represents a workbook and its connections
/// </summary>
public class WorkbookInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("contentUrl")]
    public string ContentUrl { get; set; } = string.Empty;

    [JsonPropertyName("projectName")]
    public string? ProjectName { get; set; }

    [JsonPropertyName("connections")]
    public List<ConnectionInfo> Connections { get; set; } = new();
}

/// <summary>
/// Represents a data source or workbook connection
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
    
    /// <summary>
    /// The source type of this connection: "datasource" or "workbook"
    /// </summary>
    [JsonPropertyName("sourceType")]
    public string SourceType { get; set; } = string.Empty;
    
    /// <summary>
    /// The parent ID (either DataSource ID or Workbook ID depending on SourceType)
    /// </summary>
    [JsonPropertyName("parentId")]
    public string ParentId { get; set; } = string.Empty;
    
    /// <summary>
    /// The parent name (either DataSource name or Workbook name)
    /// </summary>
    [JsonPropertyName("parentName")]
    public string ParentName { get; set; } = string.Empty;
}

#endregion
