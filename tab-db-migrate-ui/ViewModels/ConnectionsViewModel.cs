using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using tab_db_migrate;
using TabDbMigrateUI.Models;

namespace TabDbMigrateUI.ViewModels;

public partial class ConnectionsViewModel : ViewModelBase
{
    private readonly TableauAuthResponse _authResponse;
    private readonly TableauDataSourceLister _lister;
    private List<DataSourceInfo> _dataSources = new();
    private List<WorkbookInfo> _workbooks = new();

    [ObservableProperty]
    private ObservableCollection<UniqueConnection> _connections = new();

    [ObservableProperty]
    private UniqueConnection? _selectedConnection;

    [ObservableProperty]
    private string _newServerAddress = string.Empty;

    [ObservableProperty]
    private string _newServerPort = string.Empty;

    [ObservableProperty]
    private string _newUserName = string.Empty;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private bool _isUpdating = false;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError = false;

    [ObservableProperty]
    private string _updateResults = string.Empty;

    public ConnectionsViewModel(TableauAuthResponse authResponse)
    {
        _authResponse = authResponse;
        _lister = new TableauDataSourceLister(authResponse.ServerUrl, authResponse.ApiVersion);
        
        // Start loading data
        _ = LoadConnectionsAsync();
    }

    private async Task LoadConnectionsAsync()
    {
        IsLoading = true;
        HasError = false;
        StatusMessage = "Loading data sources and workbooks...";

        try
        {
            // Load data sources
            StatusMessage = "Loading data sources...";
            var inventory = await _lister.EnumerateDataSourcesAsync(_authResponse.Token, _authResponse.SiteId);
            _dataSources = inventory.DataSources;

            // Load workbooks
            StatusMessage = "Loading workbooks...";
            _workbooks = await _lister.EnumerateWorkbooksAsync(_authResponse.Token, _authResponse.SiteId);

            // Group connections by unique server/port/username
            StatusMessage = "Analyzing connections...";
            GroupConnections();

            StatusMessage = $"Loaded {Connections.Count} unique connections from {_dataSources.Count} data sources and {_workbooks.Count} workbooks.";
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Failed to load connections: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void GroupConnections()
    {
        var connectionGroups = new Dictionary<string, UniqueConnection>();

        // Process data sources
        foreach (var ds in _dataSources)
        {
            foreach (var conn in ds.Connections)
            {
                var key = $"{conn.ServerAddress}|{conn.ServerPort}|{conn.UserName}";

                if (!connectionGroups.ContainsKey(key))
                {
                    connectionGroups[key] = new UniqueConnection
                    {
                        ServerAddress = conn.ServerAddress,
                        ServerPort = conn.ServerPort,
                        UserName = conn.UserName ?? string.Empty,
                        ConnectionReferences = new List<ConnectionReference>()
                    };
                }

                connectionGroups[key].DataSourceCount++;
                connectionGroups[key].AffectedAssets.Add($"[DS] {ds.Name}");
                connectionGroups[key].ConnectionReferences.Add(new ConnectionReference
                {
                    ConnectionId = conn.Id,
                    ParentId = ds.Id,
                    ParentName = ds.Name,
                    SourceType = "datasource"
                });
            }
        }

        // Process workbooks
        foreach (var wb in _workbooks)
        {
            foreach (var conn in wb.Connections)
            {
                var key = $"{conn.ServerAddress}|{conn.ServerPort}|{conn.UserName}";

                if (!connectionGroups.ContainsKey(key))
                {
                    connectionGroups[key] = new UniqueConnection
                    {
                        ServerAddress = conn.ServerAddress,
                        ServerPort = conn.ServerPort,
                        UserName = conn.UserName ?? string.Empty,
                        ConnectionReferences = new List<ConnectionReference>()
                    };
                }

                connectionGroups[key].WorkbookCount++;
                connectionGroups[key].AffectedAssets.Add($"[WB] {wb.Name}");
                connectionGroups[key].ConnectionReferences.Add(new ConnectionReference
                {
                    ConnectionId = conn.Id,
                    ParentId = wb.Id,
                    ParentName = wb.Name,
                    SourceType = "workbook"
                });
            }
        }

        Connections = new ObservableCollection<UniqueConnection>(connectionGroups.Values.OrderBy(c => c.ServerAddress));
    }

    [RelayCommand(CanExecute = nameof(CanUpdate))]
    private async Task UpdateConnectionAsync()
    {
        if (SelectedConnection == null) return;

        IsUpdating = true;
        HasError = false;
        UpdateResults = string.Empty;
        StatusMessage = $"Updating {SelectedConnection.ConnectionReferences.Count} connections...";

        int successCount = 0;
        int failCount = 0;
        var results = new List<string>();

        try
        {
            foreach (var connRef in SelectedConnection.ConnectionReferences)
            {
                bool success;

                if (connRef.SourceType == "datasource")
                {
                    success = await _lister.UpdateDataSourceConnectionAsync(
                        _authResponse.Token,
                        _authResponse.SiteId,
                        connRef.ParentId,
                        connRef.ConnectionId,
                        NewServerAddress,
                        NewServerPort,
                        NewUserName,
                        NewPassword
                    );
                }
                else // workbook
                {
                    success = await _lister.UpdateWorkbookConnectionAsync(
                        _authResponse.Token,
                        _authResponse.SiteId,
                        connRef.ParentId,
                        connRef.ConnectionId,
                        NewServerAddress,
                        NewServerPort,
                        NewUserName,
                        NewPassword
                    );
                }

                if (success)
                {
                    successCount++;
                    results.Add($"✓ {connRef.SourceType}: {connRef.ParentName}");
                }
                else
                {
                    failCount++;
                    results.Add($"✗ {connRef.SourceType}: {connRef.ParentName}");
                }
            }

            StatusMessage = $"Update complete: {successCount} succeeded, {failCount} failed";
            UpdateResults = string.Join("\n", results);

            // Reload connections to reflect changes
            if (successCount > 0)
            {
                await LoadConnectionsAsync();
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Update failed: {ex.Message}";
        }
        finally
        {
            IsUpdating = false;
        }
    }

    private bool CanUpdate()
    {
        return SelectedConnection != null &&
               !string.IsNullOrWhiteSpace(NewServerAddress) &&
               !string.IsNullOrWhiteSpace(NewServerPort) &&
               !string.IsNullOrWhiteSpace(NewUserName) &&
               !string.IsNullOrWhiteSpace(NewPassword) &&
               !IsUpdating &&
               !IsLoading;
    }

    partial void OnSelectedConnectionChanged(UniqueConnection? value)
    {
        if (value != null)
        {
            // Pre-populate fields with current values
            NewServerAddress = value.ServerAddress;
            NewServerPort = value.ServerPort;
            NewUserName = value.UserName;
            NewPassword = string.Empty; // Don't pre-fill password
        }
        UpdateConnectionCommand.NotifyCanExecuteChanged();
    }

    partial void OnNewServerAddressChanged(string value) => UpdateConnectionCommand.NotifyCanExecuteChanged();
    partial void OnNewServerPortChanged(string value) => UpdateConnectionCommand.NotifyCanExecuteChanged();
    partial void OnNewUserNameChanged(string value) => UpdateConnectionCommand.NotifyCanExecuteChanged();
    partial void OnNewPasswordChanged(string value) => UpdateConnectionCommand.NotifyCanExecuteChanged();
}
