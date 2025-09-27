using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerTrading.Reporting.Service.Models;
using PowerTrading.Reporting.Service.Options;
using PowerTrading.Reporting.Service.Services;
using System.Collections.Generic;

namespace PowerTrading.Reporting.Service.Tests;

[TestClass]
public class CsvReportExporterTests
{
    private string _tempDir = null!;
    private IOptions<CsvExporterOptions> _options = null!;
    private CsvReportExporter _exporter = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    _options = Microsoft.Extensions.Options.Options.Create(new CsvExporterOptions { OutputDirectory = _tempDir });
        _exporter = new CsvReportExporter(_options);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [TestMethod]
    public async Task Export_WritesCsvFileWithCorrectContent()
    {
        // Arrange
        var report = new PowerPositionReport(
            new DateTime(2025, 9, 27, 14, 30, 0),
            new List<ReportingPowerPeriod>
            {
                new ReportingPowerPeriod(1, 100.5), // test rounding
                new ReportingPowerPeriod(2, 200.0)
            }
        );

        // Act
        await _exporter.Export(report);

        // Assert
        var expectedFile = Path.Combine(_tempDir, "PowerPosition_report.20250927_1430.csv");
        Assert.IsTrue(File.Exists(expectedFile), "CSV file was not created");
        var content = await File.ReadAllTextAsync(expectedFile);
        StringAssert.Contains(content, "Local Time,Volume");
        StringAssert.Contains(content, ",100");
        StringAssert.Contains(content, ",200");
    }

    [TestMethod]
    public async Task Export_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var newDir = Path.Combine(_tempDir, "subdir");
        if (Directory.Exists(newDir))
        {
            Directory.Delete(newDir, true);
        }
        var options = Microsoft.Extensions.Options.Options.Create(new CsvExporterOptions { OutputDirectory = newDir });
        var exporter = new CsvReportExporter(options);
        var report = new PowerPositionReport(
            DateTime.UtcNow,
            new List<ReportingPowerPeriod>()
        );

        // Act
        await exporter.Export(report);

        // Assert
        Assert.IsTrue(Directory.Exists(newDir), "Output directory was not created");
    }
}
