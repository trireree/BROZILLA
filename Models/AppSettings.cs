namespace BROZILLA.Models;

/// <summary>
/// Persisted user preferences for BROZILLA.
/// </summary>
public sealed class AppSettings
{
    public string SelectedProfileId { get; set; } = "turkey_standard";
    public bool StartWithWindows { get; set; } = false;
    public bool AutoConnectOnLaunch { get; set; } = false;
}
