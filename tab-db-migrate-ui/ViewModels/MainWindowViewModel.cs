using CommunityToolkit.Mvvm.ComponentModel;
using tab_db_migrate;

namespace TabDbMigrateUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentView;

    public MainWindowViewModel()
    {
        // Start with authentication view
        var authViewModel = new AuthenticationViewModel();
        authViewModel.AuthenticationSucceeded += OnAuthenticationSucceeded;
        _currentView = authViewModel;
    }

    private void OnAuthenticationSucceeded(object? sender, TableauAuthResponse authResponse)
    {
        // Switch to connections view
        CurrentView = new ConnectionsViewModel(authResponse);
    }
}
