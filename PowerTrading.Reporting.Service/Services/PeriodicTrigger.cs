using Microsoft.Extensions.Options;
using PowerTrading.Reporting.Service.Options;

namespace PowerTrading.Reporting.Service.Services;

/// <summary>
/// Periodic implementation of ITrigger that triggers at specified intervals
/// </summary>
public sealed class PeriodicTrigger : ITrigger, IDisposable
{
    private readonly ILogger<PeriodicTrigger> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _interval;
    private ITimer? _timer;
    private bool _disposed;

    public event EventHandler<TriggerEventArgs>? Triggered;

    /// <summary>
    /// Initializes a new instance of PeriodicTrigger
    /// </summary>
    /// <param name="timeProvider">Time provider for creating timers</param>
    /// <param name="options">Configuration options for the periodic trigger</param>
    /// <param name="logger">Logger instance</param>
    public PeriodicTrigger(TimeProvider timeProvider, IOptions<PeriodicTriggerOptions> options, ILogger<PeriodicTrigger> logger)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(options.Value.Interval, TimeSpan.Zero, nameof(options));

        _timeProvider = timeProvider;
        _interval = options.Value.Interval;
        _logger = logger;
        
        _logger.LogInformation("PeriodicTrigger configured with interval: {IntervalMinutes} minutes", options.Value.IntervalMinutes);
    }

    public void Start()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PeriodicTrigger));
        }

        if (_timer != null)
        {
            _logger.LogWarning("PeriodicTrigger is already started");
            return;
        }

        _logger.LogInformation("Starting periodic trigger");
        // Fire the first event immediately, then at the configured interval
        _timer = _timeProvider.CreateTimer(OnTimerElapsed, state: null, TimeSpan.Zero, _interval);
        _logger.LogInformation("Periodic trigger started successfully");
    }

    public void Stop()
    {
        if (_disposed)
        {
            _logger.LogWarning("Cannot stop PeriodicTrigger: already disposed");
            return;
        }

        if (_timer == null)
        {
            _logger.LogWarning("PeriodicTrigger is already stopped");
            return;
        }

        _logger.LogInformation("Stopping periodic trigger");
        _timer.Dispose();
        _timer = null;
        _logger.LogInformation("Periodic trigger stopped successfully");
    }

    private void OnTimerElapsed(object? state)
    {
        try
        {
            if (_disposed)
            {
                return;
            }

            _logger.LogDebug("Timer elapsed - raising Triggered event");
            
            var eventArgs = new TriggerEventArgs { TriggeredAt = _timeProvider.GetLocalNow().DateTime };
            _logger.LogInformation("Triggering via timer at {TriggeredAt}", eventArgs.TriggeredAt);
            Triggered?.Invoke(this, eventArgs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in timer elapsed handler");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop(); // Stop the trigger if it's running
        _disposed = true;
        _logger.LogInformation("PeriodicTrigger disposed");
    }
}