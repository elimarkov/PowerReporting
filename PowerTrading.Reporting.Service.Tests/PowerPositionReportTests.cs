using PowerTrading.Reporting.Service.Models;

namespace PowerTrading.Reporting.Service.Tests;

[TestClass]
public sealed class PowerPositionReportTests
{
    [TestMethod]
    public void Constructor_ShouldOrderPeriodsByPowerTradingDay()
    {
        // Arrange - Create periods in random order
        var reportTimestamp = new DateTime(2025, 9, 26, 15, 30, 0);
        var periods = new[]
        {
            new ReportingPowerPeriod(10, 100.0), // 10:00
            new ReportingPowerPeriod(23, 230.0), // 23:00 - should be first
            new ReportingPowerPeriod(1, 10.0),   // 01:00
            new ReportingPowerPeriod(0, 0.0),    // 00:00 - should be second
            new ReportingPowerPeriod(22, 220.0), // 22:00 - should be last
            new ReportingPowerPeriod(5, 50.0)    // 05:00
        };

        // Act
        var report = new PowerPositionReport(reportTimestamp, periods);

        // Assert - Verify power trading day order: 23:00, 00:00, 01:00, 05:00, 10:00, 22:00
        Assert.AreEqual(6, report.Periods.Count);
        
        // 23:00 should be first
        Assert.AreEqual(23, report.Periods[0].Period.Hour);
        Assert.AreEqual(230.0, report.Periods[0].Volume);
        
        // 00:00 should be second
        Assert.AreEqual(0, report.Periods[1].Period.Hour);
        Assert.AreEqual(0.0, report.Periods[1].Volume);
        
        // 01:00 should be third
        Assert.AreEqual(1, report.Periods[2].Period.Hour);
        Assert.AreEqual(10.0, report.Periods[2].Volume);
        
        // 05:00 should be fourth
        Assert.AreEqual(5, report.Periods[3].Period.Hour);
        Assert.AreEqual(50.0, report.Periods[3].Volume);
        
        // 10:00 should be fifth
        Assert.AreEqual(10, report.Periods[4].Period.Hour);
        Assert.AreEqual(100.0, report.Periods[4].Volume);
        
        // 22:00 should be last
        Assert.AreEqual(22, report.Periods[5].Period.Hour);
        Assert.AreEqual(220.0, report.Periods[5].Volume);
    }

    [TestMethod]
    public void Constructor_WithFullTradingDay_ShouldOrderCorrectly()
    {
        // Arrange - Create a full 24-hour trading day in reverse order
        var reportTimestamp = new DateTime(2025, 9, 26, 15, 30, 0);
        var periods = new ReportingPowerPeriod[24];
        for (int i = 0; i < 24; i++)
        {
            periods[i] = new ReportingPowerPeriod(23 - i, (23 - i) * 10.0);
        }

        // Act
        var report = new PowerPositionReport(reportTimestamp, periods);

        // Assert - Should start with 23:00, then 00:00-22:00
        Assert.AreEqual(24, report.Periods.Count);
        
        // First should be 23:00
        Assert.AreEqual(23, report.Periods[0].Period.Hour);
        
        // Next 23 should be 00:00 through 22:00 in order
        for (int i = 1; i < 24; i++)
        {
            int expectedHour = i - 1; // 0, 1, 2, ..., 22
            Assert.AreEqual(expectedHour, report.Periods[i].Period.Hour, 
                $"Expected hour {expectedHour} at position {i}, but got {report.Periods[i].Period.Hour}");
        }
    }

    [TestMethod]
    public void Constructor_WithDuplicatePeriods_ShouldThrowArgumentException()
    {
        // Arrange
        var reportTimestamp = new DateTime(2025, 9, 26, 15, 30, 0);
        var periods = new[]
        {
            new ReportingPowerPeriod(10, 100.0),
            new ReportingPowerPeriod(10, 200.0) // Duplicate hour
        };

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() =>
            new PowerPositionReport(reportTimestamp, periods));
        
        Assert.IsTrue(exception.Message.Contains("Duplicate periods found"));
        Assert.IsTrue(exception.Message.Contains("10:00"));
    }

    [TestMethod]
    public void Constructor_WithNullPeriods_ShouldThrowArgumentNullException()
    {
        // Arrange
        var reportTimestamp = new DateTime(2025, 9, 26, 15, 30, 0);

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new PowerPositionReport(reportTimestamp, null!));
    }

    [TestMethod]
    public void Constructor_WithEmptyPeriods_ShouldCreateEmptyReport()
    {
        // Arrange
        var reportTimestamp = new DateTime(2025, 9, 26, 15, 30, 0);
        var periods = Array.Empty<ReportingPowerPeriod>();

        // Act
        var report = new PowerPositionReport(reportTimestamp, periods);

        // Assert
        Assert.AreEqual(reportTimestamp, report.ReportTimestamp);
        Assert.AreEqual(0, report.Periods.Count);
    }

    [TestMethod]
    public void Constructor_WithMultipleDuplicatePeriods_ShouldListAllDuplicatesInException()
    {
        // Arrange
        var reportTimestamp = new DateTime(2025, 9, 26, 15, 30, 0);
        var periods = new[]
        {
            new ReportingPowerPeriod(10, 100.0),
            new ReportingPowerPeriod(10, 200.0), // Duplicate 10:00
            new ReportingPowerPeriod(15, 300.0),
            new ReportingPowerPeriod(15, 400.0), // Duplicate 15:00
            new ReportingPowerPeriod(20, 500.0)
        };

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() =>
            new PowerPositionReport(reportTimestamp, periods));
        
        Assert.IsTrue(exception.Message.Contains("Duplicate periods found"));
        Assert.IsTrue(exception.Message.Contains("10:00"));
        Assert.IsTrue(exception.Message.Contains("15:00"));
    }

    [TestMethod]
    public void Constructor_ShouldSetReportTimestamp()
    {
        // Arrange
        var reportTimestamp = new DateTime(2025, 9, 26, 15, 30, 45);
        var periods = new[]
        {
            new ReportingPowerPeriod(10, 100.0)
        };

        // Act
        var report = new PowerPositionReport(reportTimestamp, periods);

        // Assert
        Assert.AreEqual(reportTimestamp, report.ReportTimestamp);
    }

    [TestMethod]
    public void Periods_ShouldBeReadOnly()
    {
        // Arrange
        var reportTimestamp = new DateTime(2025, 9, 26, 15, 30, 0);
        var periods = new[]
        {
            new ReportingPowerPeriod(10, 100.0),
            new ReportingPowerPeriod(11, 200.0)
        };

        // Act
        var report = new PowerPositionReport(reportTimestamp, periods);

        // Assert
        Assert.IsInstanceOfType(report.Periods, typeof(IReadOnlyList<ReportingPowerPeriod>));
        Assert.AreEqual(2, report.Periods.Count);
    }

    [TestMethod]
    public void Constructor_WithMixedVolumes_ShouldPreserveVolumes()
    {
        // Arrange
        var reportTimestamp = new DateTime(2025, 9, 26, 15, 30, 0);
        var periods = new[]
        {
            new ReportingPowerPeriod(1, -50.25),   // Negative volume
            new ReportingPowerPeriod(2, 0.0),      // Zero volume
            new ReportingPowerPeriod(3, 999.99),   // Large volume
            new ReportingPowerPeriod(23, 123.456)  // Precise decimal
        };

        // Act
        var report = new PowerPositionReport(reportTimestamp, periods);

        // Assert - Check volumes are preserved in correct order
        Assert.AreEqual(4, report.Periods.Count);
        
        // 23:00 should be first with volume 123.456
        Assert.AreEqual(23, report.Periods[0].Period.Hour);
        Assert.AreEqual(123.456, report.Periods[0].Volume);
        
        // 01:00 should be second with volume -50.25
        Assert.AreEqual(1, report.Periods[1].Period.Hour);
        Assert.AreEqual(-50.25, report.Periods[1].Volume);
        
        // 02:00 should be third with volume 0.0
        Assert.AreEqual(2, report.Periods[2].Period.Hour);
        Assert.AreEqual(0.0, report.Periods[2].Volume);
        
        // 03:00 should be fourth with volume 999.99
        Assert.AreEqual(3, report.Periods[3].Period.Hour);
        Assert.AreEqual(999.99, report.Periods[3].Volume);
    }

    [TestMethod]
    public void Constructor_WithSinglePeriod_ShouldWork()
    {
        // Arrange
        var reportTimestamp = new DateTime(2025, 9, 26, 15, 30, 0);
        var periods = new[]
        {
            new ReportingPowerPeriod(12, 150.75)
        };

        // Act
        var report = new PowerPositionReport(reportTimestamp, periods);

        // Assert
        Assert.AreEqual(1, report.Periods.Count);
        Assert.AreEqual(12, report.Periods[0].Period.Hour);
        Assert.AreEqual(150.75, report.Periods[0].Volume);
        Assert.AreEqual(reportTimestamp, report.ReportTimestamp);
    }
}