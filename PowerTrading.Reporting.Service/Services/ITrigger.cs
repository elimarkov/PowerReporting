namespace PowerTrading.Reporting.Service.Services;

/// <summary>
/// Interface for triggering with different implementations
/// </summary>
public interface ITrigger
{
    /// <summary>
    /// Event raised by trigger
    /// </summary>
    event EventHandler<TriggerEventArgs>? Triggered;
}