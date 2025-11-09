using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using tab_db_migrate;
using TabDbMigrateUI.Services;

namespace TabDbMigrateUI.ViewModels;

public partial class AuthenticationViewModel : ViewModelBase
{
    private readonly CredentialsService _credentialsService = new();

    [ObservableProperty]
    private string _serverUrl = string.Empty;

    [ObservableProperty]
    private string _tokenName = string.Empty;

    [ObservableProperty]
    private string _tokenSecret = string.Empty;

    [ObservableProperty]
    private string _siteName = string.Empty;

    [ObservableProperty]
    private bool _isAuthenticating = false;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError = false;

    [ObservableProperty]
    private bool _rememberCredentials = true;

    public event EventHandler<TableauAuthResponse>? AuthenticationSucceeded;

    public AuthenticationViewModel()
    {
        // Load saved credentials on startup
        _ = LoadSavedCredentialsAsync();
    }

    private async Task LoadSavedCredentialsAsync()
    {
        var saved = await _credentialsService.LoadCredentialsAsync();
        if (saved != null)
        {
            ServerUrl = saved.ServerUrl;
            TokenName = saved.TokenName;
            TokenSecret = saved.TokenSecret;
            SiteName = saved.SiteName;
            StatusMessage = "Loaded saved credentials";
            HasError = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        IsAuthenticating = true;
        HasError = false;
        StatusMessage = "Authenticating with Tableau...";

        try
        {
            var authenticator = new TableauAuthenticator(ServerUrl);
            var authResponse = await authenticator.SignInWithPATAsync(TokenName, TokenSecret, SiteName);

            StatusMessage = "Authentication successful!";
            
            // Save credentials if user wants to remember them
            if (RememberCredentials)
            {
                await _credentialsService.SaveCredentialsAsync(ServerUrl, TokenName, TokenSecret, SiteName);
            }
            else
            {
                _credentialsService.ClearCredentials();
            }
            
            // Notify that authentication succeeded
            AuthenticationSucceeded?.Invoke(this, authResponse);
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Authentication failed: {ex.Message}";
        }
        finally
        {
            IsAuthenticating = false;
        }
    }

    private bool CanConnect()
    {
        return !string.IsNullOrWhiteSpace(ServerUrl) &&
               !string.IsNullOrWhiteSpace(TokenName) &&
               !string.IsNullOrWhiteSpace(TokenSecret) &&
               !IsAuthenticating;
    }

    partial void OnServerUrlChanged(string value) => ConnectCommand.NotifyCanExecuteChanged();
    partial void OnTokenNameChanged(string value) => ConnectCommand.NotifyCanExecuteChanged();
    partial void OnTokenSecretChanged(string value) => ConnectCommand.NotifyCanExecuteChanged();
}
