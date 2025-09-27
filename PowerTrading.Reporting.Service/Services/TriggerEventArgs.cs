namespace PowerTrading.Reporting.Service.Services;

/// <summary>
/// Event arguments for trigger events
/// </summary>
public class TriggerEventArgs : EventArgs
{
    public required DateTime TriggeredAt { get; init; }
}