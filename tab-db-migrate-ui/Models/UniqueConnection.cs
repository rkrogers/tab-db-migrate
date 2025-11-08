using System.Collections.Generic;

namespace TabDbMigrateUI.Models;

/// <summary>
/// Represents a unique connection configuration found across data sources and workbooks
/// </summary>
public class UniqueConnection
{
    public string ServerAddress { get; set; } = string.Empty;
    public string ServerPort { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int DataSourceCount { get; set; }
    public int WorkbookCount { get; set; }
    public List<string> AffectedAssets { get; set; } = new();
    public List<ConnectionReference> ConnectionReferences { get; set; } = new();

    /// <summary>
    /// Gets a display-friendly identifier for this connection
    /// </summary>
    public string DisplayName => $"{ServerAddress}:{ServerPort} ({UserName})";

    /// <summary>
    /// Gets the total count of affected assets
    /// </summary>
    public int TotalAffectedAssets => DataSourceCount + WorkbookCount;
}

/// <summary>
/// Represents a reference to a specific connection that can be updated
/// </summary>
public class ConnectionReference
{
    public string ConnectionId { get; set; } = string.Empty;
    public string ParentId { get; set; } = string.Empty;
    public string ParentName { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty; // "datasource" or "workbook"
}
