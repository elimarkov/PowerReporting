# PowerTrading Reporting Service

A Windows service that generates periodic intraday position reports from power trading data.

## Architecture

The service uses an event-driven architecture with the following key components:

- **IReportTrigger**: Interface for triggering report generation events
- **PeriodicReportTrigger**: Timer-based implementation that triggers reports at configurable intervals
- **IntradayPositionReporter**: Background service that listens for trigger events and generates reports

## Configuration

The service can be configured via `appsettings.json`:

```json
{
  "PeriodicReportTrigger": {
    "IntervalMinutes": 1
  }
}
```

## Important Considerations

### Overlapping Report Generation

**Scenario**: If report generation takes longer than the configured timer interval, multiple report generation processes will run concurrently.

**Current Behavior**: 
The `PeriodicReportTrigger` will fire events at the configured interval regardless of whether previous reports have completed. This means if a report takes 2 minutes to generate but the interval is set to 1 minute, you'll have 2+ reports running simultaneously.

## Deployment

The service can be deployed as:
- **Console Application**: For development and testing
- **Windows Service**: For production environments
- **Container**: Using Docker for cloud deployment