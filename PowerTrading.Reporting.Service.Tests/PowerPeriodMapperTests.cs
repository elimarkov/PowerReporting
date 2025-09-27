using PowerTrading.Reporting.Service.Models;
using PowerTrading.Reporting.Service.Services;
using Services;

namespace PowerTrading.Reporting.Service.Tests;

[TestClass]
public sealed class PowerPeriodMapperTests
{
    private PowerPeriodMapper _mapper = null!;

    [TestInitialize]
    public void Setup()
    {
        _mapper = new PowerPeriodMapper();
    }

    [TestMethod]
    public void Map_WithPeriod1_ShouldMapToHour23()
    {
        // Arrange
        var sourcePeriod = new PowerPeriod { Period = 1, Volume = 100.5 };

        // Act
        var result = _mapper.Map(sourcePeriod);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(23, result.Period.Hour);
        Assert.AreEqual(0, result.Period.Minute);
        Assert.AreEqual(100.5, result.Volume);
    }

    [TestMethod]
    public void Map_WithPeriod2_ShouldMapToHour0()
    {
        // Arrange
        var sourcePeriod = new PowerPeriod { Period = 2, Volume = 150.75 };

        // Act
        var result = _mapper.Map(sourcePeriod);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Period.Hour);
        Assert.AreEqual(0, result.Period.Minute);
        Assert.AreEqual(150.75, result.Volume);
    }

    [TestMethod]
    public void Map_WithPeriod3_ShouldMapToHour1()
    {
        // Arrange
        var sourcePeriod = new PowerPeriod { Period = 3, Volume = 200.25 };

        // Act
        var result = _mapper.Map(sourcePeriod);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Period.Hour);
        Assert.AreEqual(0, result.Period.Minute);
        Assert.AreEqual(200.25, result.Volume);
    }

    [TestMethod]
    public void Map_WithPeriod24_ShouldMapToHour22()
    {
        // Arrange
        var sourcePeriod = new PowerPeriod { Period = 24, Volume = 300.0 };

        // Act
        var result = _mapper.Map(sourcePeriod);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(22, result.Period.Hour);
        Assert.AreEqual(0, result.Period.Minute);
        Assert.AreEqual(300.0, result.Volume);
    }

    [TestMethod]
    public void Map_WithZeroVolume_ShouldPreserveZeroVolume()
    {
        // Arrange
        var sourcePeriod = new PowerPeriod { Period = 6, Volume = 0.0 };

        // Act
        var result = _mapper.Map(sourcePeriod);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(4, result.Period.Hour);
        Assert.AreEqual(0.0, result.Volume);
    }

    [TestMethod]
    public void Map_WithNegativeVolume_ShouldPreserveNegativeVolume()
    {
        // Arrange
        var sourcePeriod = new PowerPeriod { Period = 11, Volume = -50.25 };

        // Act
        var result = _mapper.Map(sourcePeriod);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(9, result.Period.Hour);
        Assert.AreEqual(-50.25, result.Volume);
    }

    [TestMethod]
    public void Map_WithLargeVolume_ShouldPreserveLargeVolume()
    {
        // Arrange
        var sourcePeriod = new PowerPeriod { Period = 16, Volume = 999999.99 };

        // Act
        var result = _mapper.Map(sourcePeriod);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(14, result.Period.Hour);
        Assert.AreEqual(999999.99, result.Volume);
    }

    [TestMethod]
    public void Map_WithAllPossiblePeriods_ShouldMapCorrectly()
    {
        // Test the full mapping logic for all possible periods (1-24)
        var testCases = new[]
        {
            new { Period = 1, ExpectedHour = 23 },
            new { Period = 2, ExpectedHour = 0 },
            new { Period = 3, ExpectedHour = 1 },
            new { Period = 4, ExpectedHour = 2 },
            new { Period = 5, ExpectedHour = 3 },
            new { Period = 6, ExpectedHour = 4 },
            new { Period = 7, ExpectedHour = 5 },
            new { Period = 8, ExpectedHour = 6 },
            new { Period = 9, ExpectedHour = 7 },
            new { Period = 10, ExpectedHour = 8 },
            new { Period = 11, ExpectedHour = 9 },
            new { Period = 12, ExpectedHour = 10 },
            new { Period = 13, ExpectedHour = 11 },
            new { Period = 14, ExpectedHour = 12 },
            new { Period = 15, ExpectedHour = 13 },
            new { Period = 16, ExpectedHour = 14 },
            new { Period = 17, ExpectedHour = 15 },
            new { Period = 18, ExpectedHour = 16 },
            new { Period = 19, ExpectedHour = 17 },
            new { Period = 20, ExpectedHour = 18 },
            new { Period = 21, ExpectedHour = 19 },
            new { Period = 22, ExpectedHour = 20 },
            new { Period = 23, ExpectedHour = 21 },
            new { Period = 24, ExpectedHour = 22 }
        };

        foreach (var testCase in testCases)
        {
            // Arrange
            var sourcePeriod = new PowerPeriod { Period = testCase.Period, Volume = testCase.Period * 10.0 };

            // Act
            var result = _mapper.Map(sourcePeriod);

            // Assert
            Assert.AreEqual(testCase.ExpectedHour, result.Period.Hour, 
                $"Period {testCase.Period} should map to hour {testCase.ExpectedHour}, but got {result.Period.Hour}");
            Assert.AreEqual(testCase.Period * 10.0, result.Volume,
                $"Volume should be preserved for period {testCase.Period}");
        }
    }

    [TestMethod]
    public void Map_MultipleCalls_ShouldProduceSameResults()
    {
        // Arrange
        var sourcePeriod = new PowerPeriod { Period = 13, Volume = 123.45 };

        // Act
        var result1 = _mapper.Map(sourcePeriod);
        var result2 = _mapper.Map(sourcePeriod);

        // Assert
        Assert.AreEqual(result1.Period.Hour, result2.Period.Hour);
        Assert.AreEqual(result1.Period.Minute, result2.Period.Minute);
        Assert.AreEqual(result1.Volume, result2.Volume);
    }

    [TestMethod]
    public void Map_WithPreciseDecimalVolume_ShouldPreservePrecision()
    {
        // Arrange
        var sourcePeriod = new PowerPeriod { Period = 9, Volume = 123.456789 };

        // Act
        var result = _mapper.Map(sourcePeriod);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(7, result.Period.Hour);
        Assert.AreEqual(123.456789, result.Volume, delta: 0.000001);
    }

    [TestMethod]
    public void Map_ImplementsCorrectInterface()
    {
        // Assert
        Assert.IsInstanceOfType(_mapper, typeof(IMapper<PowerPeriod, ReportingPowerPeriod>));
    }
}