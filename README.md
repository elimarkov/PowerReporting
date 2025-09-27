# PowerTrading Reporting Service

A .NET 8 Windows service that generates intraday position reports from power trading data. The service generates an initial report immediately on startup and then continues with periodic reports based on a configurable timer interval.

## Architecture

The service uses an event-driven architecture with explicit lifecycle control and the following key components:

### Core Components

- **ITrigger**: Interface for triggering report generation events with Start/Stop lifecycle methods
- **PeriodicTrigger**: Timer-based implementation with configurable intervals and explicit lifecycle control
- **IntradayPositionReporter**: Background service that generates initial report on startup and listens for trigger events
- **IReportGenerator**: Interface for report generation with PowerPositionReportGenerator implementation
- **PowerPositionReport**: Domain model with ordered power periods using 1-based indexing

### Domain Models

- **ReportingPowerPeriod**: Represents power trading periods with IComparable implementation for proper ordering
- **PowerPositionReport**: Contains report timestamp and collection of ordered periods with validation
- **PowerPeriodMapper**: Maps external power periods to internal reporting periods

### Key Features

- **Immediate Startup Report**: Generates initial report as soon as the service starts
- **Event-Driven Architecture**: Uses ITrigger events for decoupled report generation
- **Explicit Lifecycle Control**: PeriodicTrigger requires explicit Start/Stop calls
- **Data Validation**: Uses data annotations for configuration validation
- **Power Trading Day Ordering**: 23:00 first, then 00:00-22:00 following day
- **Comprehensive Logging**: Structured logging throughout the application
- **Robust Error Handling**: Graceful error handling with continued operation

## Configuration

The service uses configuration validation with data annotations:

```json
{
  "PeriodicTrigger": {
    "IntervalMinutes": 1
  }
}
```

### Configuration Validation

- **IntervalMinutes**: Required, must be between 1 and 1440 (24 hours)
- **Startup Validation**: Service fails fast if configuration is invalid
- **Data Annotations**: Uses Microsoft.Extensions.Options.DataAnnotations for validation

## Report Generation Behavior

### Startup Sequence
1. Service starts and logs startup message
2. **Generates initial report immediately** using current timestamp
3. Subscribes to trigger events and starts periodic trigger
4. Continues with periodic reports based on configured interval

### Report Content
- **Timestamp**: Report generation time
- **Power Periods**: 24 periods representing hourly data
- **Ordering**: Uses power trading day sequence (23:00 first, then 00:00-22:00)
- **Validation**: Ensures no duplicate periods and proper ordering

## Important Considerations

### Overlapping Report Generation

**Scenario**: If report generation takes longer than the configured timer interval, multiple report generation processes will run concurrently.

**Current Behavior**: 
The `PeriodicTrigger` will fire events at the configured interval regardless of whether previous reports have completed. This means if a report takes 2 minutes to generate but the interval is set to 1 minute, you'll have 2+ reports running simultaneously.

**Recommendation**: Set the interval to be longer than typical report generation time.

### Error Handling
- Initial report failure does not prevent service startup
- Periodic report failures are logged but don't stop the service
- Service continues operation even when individual reports fail

## Testing

The solution includes comprehensive unit tests:

- **60 test methods** covering all components
- **Constructor validation** for all services
- **Lifecycle testing** for trigger start/stop behavior
- **Event handling testing** for report generation
- **Error scenario testing** for robust failure handling
- **Integration testing** for complete workflows

### Running Tests
```bash
dotnet test
```

## Development

### Technologies
- **.NET 8.0**: Latest LTS version with modern C# features
- **Microsoft.Extensions.Hosting**: Background service framework
- **Serilog**: Structured logging
- **MSTest & NSubstitute**: Unit testing framework and mocking
- **Data Annotations**: Configuration validation

### Project Structure
```
PowerTrading.Reporting.Service/
├── Models/                 # Domain models and data structures
├── Services/              # Core business logic and interfaces
├── Options/               # Configuration classes with validation
└── Program.cs            # Service configuration and startup

PowerTrading.Reporting.Service.Tests/
├── Unit tests for all components
└── Integration tests for workflows
```

## Deployment

The service can be deployed as:
- **Console Application**: For development and testing
- **Windows Service**: For production environments using `--environment Production`
- **Container**: Using Docker for cloud deployment

### Running the Service
```bash
# Development
dotnet run --project PowerTrading.Reporting.Service --environment Development

# Production
dotnet run --project PowerTrading.Reporting.Service --environment Production
```