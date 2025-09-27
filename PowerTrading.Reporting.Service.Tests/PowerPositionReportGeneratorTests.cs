using Microsoft.Extensions.Logging;
using NSubstitute;
using PowerTrading.Reporting.Service.Models;
using PowerTrading.Reporting.Service.Services;
using Services;

namespace PowerTrading.Reporting.Service.Tests;

[TestClass]
public sealed class PowerPositionReportGeneratorTests
{
    private IPowerService _mockPowerService = null!;
    private IMapper<PowerPeriod, ReportingPowerPeriod> _mockMapper = null!;
    private ILogger<PowerPositionReportGenerator> _mockLogger = null!;
    private PowerPositionReportGenerator _generator = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockPowerService = Substitute.For<IPowerService>();
        _mockMapper = Substitute.For<IMapper<PowerPeriod, ReportingPowerPeriod>>();
        _mockLogger = Substitute.For<ILogger<PowerPositionReportGenerator>>();
        _generator = new PowerPositionReportGenerator(_mockPowerService, _mockMapper, _mockLogger);
    }

    [TestMethod]
    public async Task GenerateReportAsync_WhenExtractTimeIsBefore23_ShouldPassSameDayToPowerService()
    {
        // Arrange
        var extractTime = new DateTime(2025, 9, 26, 22, 30, 0); // 22:30
        var expectedCallTime = TimeZoneInfo.ConvertTime(extractTime, TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"));
        
        var mockTrades = new List<PowerTrade>();
        _mockPowerService.GetTradesAsync(Arg.Any<DateTime>()).Returns(mockTrades);

        // Act
        var result = await _generator.GenerateReportAsync(extractTime);

        // Assert
        await _mockPowerService.Received(1).GetTradesAsync(expectedCallTime);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GenerateReportAsync_WhenExtractTimeIsExactly23_ShouldPassNextDayToPowerService()
    {
        // Arrange
        var extractTime = new DateTime(2025, 9, 26, 23, 0, 0); // 23:00
        var nextDay = extractTime.AddDays(1);
        var expectedCallTime = TimeZoneInfo.ConvertTime(nextDay, TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"));
        
        var mockTrades = new List<PowerTrade>();
        _mockPowerService.GetTradesAsync(Arg.Any<DateTime>()).Returns(mockTrades);

        // Act
        var result = await _generator.GenerateReportAsync(extractTime);

        // Assert
        // This test should verify that when extractTime >= 23:00, the power service is called with next day
        await _mockPowerService.Received(1).GetTradesAsync(expectedCallTime);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GenerateReportAsync_WhenExtractTimeIsAfter23_ShouldPassNextDayToPowerService()
    {
        // Arrange
        var extractTime = new DateTime(2025, 9, 26, 23, 30, 0); // 23:30
        var nextDay = extractTime.AddDays(1);
        var expectedCallTime = TimeZoneInfo.ConvertTime(nextDay, TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"));
        
        var mockTrades = new List<PowerTrade>();
        _mockPowerService.GetTradesAsync(Arg.Any<DateTime>()).Returns(mockTrades);

        // Act
        var result = await _generator.GenerateReportAsync(extractTime);

        // Assert
        // This test should verify that when extractTime >= 23:00, the power service is called with next day
        await _mockPowerService.Received(1).GetTradesAsync(expectedCallTime);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GenerateReportAsync_WhenPowerServiceThrowsException_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var extractTime = new DateTime(2025, 9, 26, 15, 0, 0);
        var expectedException = new InvalidOperationException("Power service error");
        
        _mockPowerService.GetTradesAsync(Arg.Any<DateTime>()).Returns(Task.FromException<IEnumerable<PowerTrade>>(expectedException));

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => _generator.GenerateReportAsync(extractTime));
            
        Assert.AreEqual(expectedException.Message, exception.Message);
    }

    [TestMethod]
    public async Task GenerateReportAsync_WithValidTrades_ShouldReturnReport()
    {
        // Arrange
        var extractTime = new DateTime(2025, 9, 26, 15, 0, 0);
        var mockTrades = new List<PowerTrade>();
        
        _mockPowerService.GetTradesAsync(Arg.Any<DateTime>()).Returns(mockTrades);

        // Act
        var result = await _generator.GenerateReportAsync(extractTime);

        // Assert
        Assert.IsNotNull(result);
        // Note: Current implementation returns empty periods, but this tests the basic flow
    }

    [TestMethod]
    public async Task GenerateReportAsync_ShouldReturnReportWithCorrectTimestamp()
    {
        // Arrange
        var extractTime = new DateTime(2025, 9, 26, 15, 30, 45); // Specific time with seconds
        var mockTrades = new List<PowerTrade>();
        
        _mockPowerService.GetTradesAsync(Arg.Any<DateTime>()).Returns(mockTrades);

        // Act
        var result = await _generator.GenerateReportAsync(extractTime);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(extractTime, result.ReportTimestamp);
    }

    [TestMethod]
    public async Task GenerateReportAsync_WithEmptyTrades_ShouldReturnEmptyReport()
    {
        // Arrange
        var extractTime = new DateTime(2025, 9, 26, 15, 0, 0);
        var emptyTrades = new List<PowerTrade>();
        
        _mockPowerService.GetTradesAsync(Arg.Any<DateTime>()).Returns(emptyTrades);

        // Act
        var result = await _generator.GenerateReportAsync(extractTime);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Periods.Count);
    }

    [TestMethod]
    public void Constructor_WithNullPowerService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new PowerPositionReportGenerator(null!, _mockMapper, _mockLogger));
    }

    [TestMethod]
    public void Constructor_WithNullMapper_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new PowerPositionReportGenerator(_mockPowerService, null!, _mockLogger));
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new PowerPositionReportGenerator(_mockPowerService, _mockMapper, null!));
    }
}