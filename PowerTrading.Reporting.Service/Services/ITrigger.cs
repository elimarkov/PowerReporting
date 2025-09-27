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

    /// <summary>
    /// Starts the trigger
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the trigger
    /// </summary>
    void Stop();
}