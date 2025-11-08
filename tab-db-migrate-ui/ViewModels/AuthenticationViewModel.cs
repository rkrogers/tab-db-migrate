using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using tab_db_migrate;

namespace TabDbMigrateUI.ViewModels;

public partial class AuthenticationViewModel : ViewModelBase
{
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

    public event EventHandler<TableauAuthResponse>? AuthenticationSucceeded;

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
