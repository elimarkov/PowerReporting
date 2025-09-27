using PowerTrading.Reporting.Service.Models;

namespace PowerTrading.Reporting.Service.Tests;

[TestClass]
public sealed class ReportingPowerPeriodTests
{
    [TestMethod]
    public void CompareTo_ShouldOrderByPowerTradingDay()
    {
        // Arrange
        var period23 = new ReportingPowerPeriod(23, 100.0); // Should be first
        var period00 = new ReportingPowerPeriod(0, 200.0);  // Should be second
        var period01 = new ReportingPowerPeriod(1, 300.0);  // Should be third
        var period22 = new ReportingPowerPeriod(22, 400.0); // Should be last

        var periods = new[] { period01, period22, period23, period00 };

        // Act
        var ordered = periods.OrderBy(p => p).ToArray();

        // Assert
        Assert.AreEqual(23, ordered[0].Period.Hour); // 23:00 first
        Assert.AreEqual(0, ordered[1].Period.Hour);  // 00:00 second
        Assert.AreEqual(1, ordered[2].Period.Hour);  // 01:00 third
        Assert.AreEqual(22, ordered[3].Period.Hour); // 22:00 last
    }

    [TestMethod]
    public void CompareTo_WithSamePeriod_ShouldReturnZero()
    {
        // Arrange
        var period1 = new ReportingPowerPeriod(10, 100.0);
        var period2 = new ReportingPowerPeriod(10, 200.0); // Different volume, same hour

        // Act
        var result = period1.CompareTo(period2);

        // Assert
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void CompareTo_WithNull_ShouldReturnPositive()
    {
        // Arrange
        var period = new ReportingPowerPeriod(10, 100.0);

        // Act
        var result = period.CompareTo(null);

        // Assert
        Assert.IsTrue(result > 0);
    }

    [TestMethod]
    public void CompareTo_Period23VsPeriod00_ShouldReturn23First()
    {
        // Arrange
        var period23 = new ReportingPowerPeriod(23, 100.0);
        var period00 = new ReportingPowerPeriod(0, 200.0);

        // Act
        var result23vs00 = period23.CompareTo(period00);
        var result00vs23 = period00.CompareTo(period23);

        // Assert
        Assert.IsTrue(result23vs00 < 0, "Period 23:00 should come before 00:00");
        Assert.IsTrue(result00vs23 > 0, "Period 00:00 should come after 23:00");
    }

    [TestMethod]
    public void CompareTo_FullTradingDaySequence_ShouldOrderCorrectly()
    {
        // Arrange - Create periods in reverse order
        var periods = new List<ReportingPowerPeriod>();
        for (int hour = 23; hour >= 0; hour--)
        {
            periods.Add(new ReportingPowerPeriod(hour, hour * 10.0));
        }

        // Act
        var ordered = periods.OrderBy(p => p).ToArray();

        // Assert
        // First should be 23:00
        Assert.AreEqual(23, ordered[0].Period.Hour);
        
        // Next should be 00:00 through 22:00 in order
        for (int i = 1; i < 24; i++)
        {
            int expectedHour = i - 1; // 0, 1, 2, ..., 22
            Assert.AreEqual(expectedHour, ordered[i].Period.Hour, 
                $"Expected hour {expectedHour} at position {i}, but got {ordered[i].Period.Hour}");
        }
    }

    [TestMethod]
    public void Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var period = new ReportingPowerPeriod(15, 123.45);

        // Assert
        Assert.AreEqual(15, period.Period.Hour);
        Assert.AreEqual(0, period.Period.Minute);
        Assert.AreEqual(123.45, period.Volume);
    }
}