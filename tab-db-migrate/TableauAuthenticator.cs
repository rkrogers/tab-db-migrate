using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace tab_db_migrate;

/// <summary>
/// Handles authentication with Tableau Cloud using the REST API
/// </summary>
public class TableauAuthenticator
{
    private readonly string _serverUrl;
    private readonly string _apiVersion;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the TableauAuthenticator
    /// </summary>
    /// <param name="serverUrl">The Tableau Cloud server URL (e.g., "https://10ay.online.tableau.com" or "https://prod-useast-b.online.tableau.com")</param>
    /// <param name="apiVersion">The Tableau REST API version (e.g., "3.21"). Defaults to "3.21"</param>
    public TableauAuthenticator(string serverUrl, string apiVersion = "3.21")
    {
        // Clean up the server URL - remove any path components that users might accidentally include
        _serverUrl = CleanServerUrl(serverUrl);
        _apiVersion = apiVersion;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
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
    /// Authenticates with Tableau Cloud using a Personal Access Token (PAT)
    /// </summary>
    /// <param name="tokenName">The PAT name</param>
    /// <param name="tokenValue">The PAT secret value</param>
    /// <param name="siteName">The site name (content URL). Use empty string for default site</param>
    /// <returns>TableauAuthResponse containing the authentication token and site details</returns>
    /// <exception cref="HttpRequestException">Thrown when the authentication request fails</exception>
    public async Task<TableauAuthResponse> SignInWithPATAsync(string tokenName, string tokenValue, string siteName = "")
    {
        var signInUrl = $"{_serverUrl}/api/{_apiVersion}/auth/signin";

        var signInRequest = new TableauPATSignInRequest
        {
            Credentials = new PATCredentials
            {
                PersonalAccessTokenName = tokenName,
                PersonalAccessTokenSecret = tokenValue,
                Site = new Site
                {
                    ContentUrl = siteName
                }
            }
        };

        var jsonContent = JsonSerializer.Serialize(signInRequest, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(signInUrl, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Tableau PAT authentication failed with status code {response.StatusCode}. " +
                    $"Response: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var authResponse = JsonSerializer.Deserialize<TableauSignInResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            if (authResponse?.Credentials == null)
            {
                throw new HttpRequestException("Invalid response from Tableau API - missing credentials");
            }

            return new TableauAuthResponse
            {
                Token = authResponse.Credentials.Token ?? string.Empty,
                SiteId = authResponse.Credentials.Site?.Id ?? string.Empty,
                UserId = authResponse.Credentials.User?.Id ?? string.Empty,
                ServerUrl = _serverUrl,
                ApiVersion = _apiVersion
            };
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"An error occurred during Tableau PAT authentication: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Authenticates with Tableau Cloud using username and password
    /// </summary>
    /// <param name="username">The Tableau Cloud username (email address)</param>
    /// <param name="password">The user's password</param>
    /// <param name="siteName">The site name (content URL). Use empty string for default site</param>
    /// <returns>TableauAuthResponse containing the authentication token and site details</returns>
    /// <exception cref="HttpRequestException">Thrown when the authentication request fails</exception>
    public async Task<TableauAuthResponse> SignInAsync(string username, string password, string siteName = "")
    {
        var signInUrl = $"{_serverUrl}/api/{_apiVersion}/auth/signin";

        var signInRequest = new TableauSignInRequest
        {
            Credentials = new Credentials
            {
                Name = username,
                Password = password,
                Site = new Site
                {
                    ContentUrl = siteName
                }
            }
        };

        var jsonContent = JsonSerializer.Serialize(signInRequest, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(signInUrl, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Tableau authentication failed with status code {response.StatusCode}. " +
                    $"Response: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var authResponse = JsonSerializer.Deserialize<TableauSignInResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            if (authResponse?.Credentials == null)
            {
                throw new HttpRequestException("Invalid response from Tableau API - missing credentials");
            }

            return new TableauAuthResponse
            {
                Token = authResponse.Credentials.Token ?? string.Empty,
                SiteId = authResponse.Credentials.Site?.Id ?? string.Empty,
                UserId = authResponse.Credentials.User?.Id ?? string.Empty,
                ServerUrl = _serverUrl,
                ApiVersion = _apiVersion
            };
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"An error occurred during Tableau authentication: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Signs out from Tableau Cloud
    /// </summary>
    /// <param name="authToken">The authentication token received from SignInAsync</param>
    /// <param name="siteId">The site ID received from SignInAsync</param>
    public async Task SignOutAsync(string authToken, string siteId)
    {
        var signOutUrl = $"{_serverUrl}/api/{_apiVersion}/auth/signout";

        var request = new HttpRequestMessage(HttpMethod.Post, signOutUrl);
        request.Headers.Add("X-Tableau-Auth", authToken);

        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Tableau sign out failed with status code {response.StatusCode}. " +
                $"Response: {errorContent}");
        }
    }
}

#region Request Models

internal class TableauSignInRequest
{
    [JsonPropertyName("credentials")]
    public Credentials Credentials { get; set; } = new();
}

internal class Credentials
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("site")]
    public Site Site { get; set; } = new();
}

internal class TableauPATSignInRequest
{
    [JsonPropertyName("credentials")]
    public PATCredentials Credentials { get; set; } = new();
}

internal class PATCredentials
{
    [JsonPropertyName("personalAccessTokenName")]
    public string PersonalAccessTokenName { get; set; } = string.Empty;

    [JsonPropertyName("personalAccessTokenSecret")]
    public string PersonalAccessTokenSecret { get; set; } = string.Empty;

    [JsonPropertyName("site")]
    public Site Site { get; set; } = new();
}

internal class Site
{
    [JsonPropertyName("contentUrl")]
    public string ContentUrl { get; set; } = string.Empty;
}

#endregion

#region Response Models

internal class TableauSignInResponse
{
    [JsonPropertyName("credentials")]
    public CredentialsResponse? Credentials { get; set; }
}

internal class CredentialsResponse
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("site")]
    public SiteResponse? Site { get; set; }

    [JsonPropertyName("user")]
    public UserResponse? User { get; set; }
}

internal class SiteResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("contentUrl")]
    public string? ContentUrl { get; set; }
}

internal class UserResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

#endregion

#region Public Response Model

/// <summary>
/// Contains the authentication details returned from Tableau Cloud
/// </summary>
public class TableauAuthResponse
{
    /// <summary>
    /// The authentication token to use for subsequent API requests
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// The site ID
    /// </summary>
    public string SiteId { get; set; } = string.Empty;

    /// <summary>
    /// The user ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The server URL used for authentication
    /// </summary>
    public string ServerUrl { get; set; } = string.Empty;

    /// <summary>
    /// The API version used
    /// </summary>
    public string ApiVersion { get; set; } = string.Empty;

    /// <summary>
    /// Returns a formatted string with the authentication details
    /// </summary>
    public override string ToString()
    {
        return $"Token: {Token}\nSite ID: {SiteId}\nUser ID: {UserId}\nServer: {ServerUrl}\nAPI Version: {ApiVersion}";
    }
}

#endregion
