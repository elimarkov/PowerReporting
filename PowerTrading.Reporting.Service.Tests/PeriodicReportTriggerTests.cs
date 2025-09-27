
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PowerTrading.Reporting.Service.Options;
using PowerTrading.Reporting.Service.Services;
using Microsoft.Extensions.Time.Testing;

namespace PowerTrading.Reporting.Service.Tests;

[TestClass]
public sealed class PeriodicReportTriggerTests
{
    private ILogger<PeriodicTrigger> _logger = null!;
    private FakeTimeProvider _mockTimeProvider = null!;
    private IOptions<PeriodicTriggerOptions> _mockOptions = null!;
    private readonly TimeSpan _testInterval = TimeSpan.FromMinutes(1);

    [TestInitialize]
    public void Setup()
    {
    _logger = Substitute.For<ILogger<PeriodicTrigger>>();
    _mockTimeProvider = new FakeTimeProvider();
    _mockOptions = CreateMockOptions(_testInterval);
    }

    private static IOptions<PeriodicTriggerOptions> CreateMockOptions(TimeSpan interval)
    {
        var options = new PeriodicTriggerOptions { IntervalMinutes = (int)interval.TotalMinutes };
        var mockOptions = Substitute.For<IOptions<PeriodicTriggerOptions>>();
        mockOptions.Value.Returns(options);
        return mockOptions;
    }

    [TestMethod]
    public void Start_WhenCalled_ShouldCreateTimer()
    {
        // Arrange
        _mockTimeProvider.SetUtcNow(DateTime.UtcNow);
        DateTime dt = DateTime.MinValue;
        using var trigger = new PeriodicTrigger(_mockTimeProvider, _mockOptions, _logger);
        trigger.Triggered += (_, arg) => { dt = arg.TriggeredAt; };

        // Act
        trigger.Start();

        // Assert
        Assert.AreEqual(_mockTimeProvider.GetLocalNow().DateTime, dt);
    }

    [TestMethod]
    public void Constructor_WithNullTimeProvider_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new PeriodicTrigger(null!, _mockOptions, _logger));
    }

    [TestMethod]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new PeriodicTrigger(_mockTimeProvider, null!, _logger));
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new PeriodicTrigger(_mockTimeProvider, _mockOptions, null!));
    }

    [TestMethod]
    public void Constructor_WithZeroInterval_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var zeroIntervalOptions = CreateMockOptions(TimeSpan.Zero);

        // Act & Assert
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            new PeriodicTrigger(_mockTimeProvider, zeroIntervalOptions, _logger));
    }

    [TestMethod]
    public void Constructor_WithNegativeInterval_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var negativeIntervalOptions = CreateMockOptions(TimeSpan.FromSeconds(-1));

        // Act & Assert
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            new PeriodicTrigger(_mockTimeProvider, negativeIntervalOptions, _logger));
    }

    [TestMethod]
    public void TimerCallback_WhenInvoked_ShouldRaiseReportTriggeredEvent()
    {
        // Arrange
        var eventsRaised = 0;
        using var trigger = new PeriodicTrigger(_mockTimeProvider, _mockOptions, _logger);
        trigger.Triggered += (sender, args) => eventsRaised++;
        trigger.Start();
        _mockTimeProvider.Advance(TimeSpan.FromMinutes(1.5));
        Assert.AreEqual(2, eventsRaised);
    }

    [TestMethod]
    public void Start_ShouldFireEventImmediately_AndNextEventAfterInterval()
    {
        // Arrange
        int eventCount = 0;
        using var trigger = new PeriodicTrigger(_mockTimeProvider, _mockOptions, _logger);
        trigger.Triggered += (sender, args) => eventCount++;

        // Act
        trigger.Start();
        Assert.AreEqual(1, eventCount, "First event should be fired immediately after Start()");

        // Advance time by interval and simulate timer firing
        _mockTimeProvider.Advance(_testInterval);
        // The trigger should fire again after interval
        Assert.AreEqual(2, eventCount, "Second event should be fired after interval");
    }

    [TestMethod]
    public void Dispose_WhenCalled_ShouldDisposeTimer()
    {
        // Arrange
        var mockTimer = Substitute.For<ITimer>();
        var mockTimeProvider = Substitute.For<TimeProvider>();

        mockTimeProvider.CreateTimer(Arg.Any<TimerCallback>(), Arg.Any<object?>(), 
            Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>())
            .Returns(mockTimer);

        var trigger = new PeriodicTrigger(mockTimeProvider, _mockOptions, _logger);
        trigger.Start(); // Start the trigger to create the timer

        // Act
        trigger.Dispose();

        // Assert
        mockTimer.Received(1).Dispose();
    }

    [TestMethod]
    public void TimerCallback_AfterDispose_ShouldNotRaiseEvent()
    {
        // Arrange
        var mockTimer = Substitute.For<ITimer>();
        var eventRaisedCnt = 0;
        var trigger = new PeriodicTrigger(_mockTimeProvider, _mockOptions, _logger);
        trigger.Triggered += (sender, args) => eventRaisedCnt++;

        // Act
        trigger.Start();
        trigger.Dispose();
        _mockTimeProvider.Advance(TimeSpan.FromMinutes(2));

        // Assert
        Assert.AreEqual(1, eventRaisedCnt);
    }

    [TestMethod]
    public void Stop_WhenCalled_ShouldDisposeTimer()
    {
        // Arrange
        var mockTimer = Substitute.For<ITimer>();
        var mockTimeProvider = Substitute.For<TimeProvider>();

        mockTimeProvider.CreateTimer(Arg.Any<TimerCallback>(), Arg.Any<object?>(), 
            Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>())
            .Returns(mockTimer);

        var trigger = new PeriodicTrigger(mockTimeProvider, _mockOptions, _logger);
        trigger.Start();

        // Act
        trigger.Stop();

        // Assert
        mockTimer.Received(1).Dispose();
    }

    [TestMethod]
    public void Start_WhenCalledTwice_ShouldOnlyCreateTimerOnce()
    {
        // Arrange
        var mockTimer = Substitute.For<ITimer>();
        var mockTimeProvider = Substitute.For<TimeProvider>();

        mockTimeProvider.CreateTimer(Arg.Any<TimerCallback>(), Arg.Any<object?>(), 
            Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>())
            .Returns(mockTimer);

        using var trigger = new PeriodicTrigger(mockTimeProvider, _mockOptions, _logger);

        // Act
        trigger.Start();
        trigger.Start(); // Second call should be ignored

        // Assert
        mockTimeProvider.Received(1).CreateTimer(
            Arg.Any<TimerCallback>(),
            Arg.Any<object?>(),
            TimeSpan.Zero,
            _testInterval);
    }

    [TestMethod]
    public void Stop_WhenCalledTwice_ShouldOnlyDisposeTimerOnce()
    {
        // Arrange
        var mockTimer = Substitute.For<ITimer>();
        var mockTimeProvider = Substitute.For<TimeProvider>();

        mockTimeProvider.CreateTimer(Arg.Any<TimerCallback>(), Arg.Any<object?>(), 
            Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>())
            .Returns(mockTimer);

        var trigger = new PeriodicTrigger(mockTimeProvider, _mockOptions, _logger);
        trigger.Start();

        // Act
        trigger.Stop();
        trigger.Stop(); // Second call should be ignored
        // Assert
        mockTimer.Received(1).Dispose();
    }
}