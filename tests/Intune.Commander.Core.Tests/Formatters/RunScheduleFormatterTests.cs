using Intune.Commander.Core.Formatters;
using Microsoft.Graph.Beta.Models;
using Microsoft.Kiota.Abstractions;

namespace Intune.Commander.Core.Tests.Formatters;

public class RunScheduleFormatterTests
{
    [Fact]
    public void Format_NullSchedule_ReturnsNoSchedule()
    {
        var result = RunScheduleFormatter.Format(null);
        Assert.Equal("No schedule", result);
    }

    [Fact]
    public void Format_HourlySchedule_Interval1_ReturnsEveryHour()
    {
        var schedule = new DeviceHealthScriptHourlySchedule { Interval = 1 };
        var result = RunScheduleFormatter.Format(schedule);
        Assert.Equal("Every hour", result);
    }

    [Fact]
    public void Format_HourlySchedule_Interval3_ReturnsEvery3Hours()
    {
        var schedule = new DeviceHealthScriptHourlySchedule { Interval = 3 };
        var result = RunScheduleFormatter.Format(schedule);
        Assert.Equal("Every 3 hours", result);
    }

    [Fact]
    public void Format_HourlySchedule_NullInterval_DefaultsTo1()
    {
        var schedule = new DeviceHealthScriptHourlySchedule();
        var result = RunScheduleFormatter.Format(schedule);
        Assert.Equal("Every hour", result);
    }

    [Fact]
    public void Format_DailySchedule_Interval1_WithTimeUtc()
    {
        var schedule = new DeviceHealthScriptDailySchedule
        {
            Interval = 1,
            Time = new Time(14, 30, 0),
            UseUtc = true
        };
        var result = RunScheduleFormatter.Format(schedule);
        Assert.Equal("Daily at 14:30 (UTC)", result);
    }

    [Fact]
    public void Format_DailySchedule_Interval7_LocalTime()
    {
        var schedule = new DeviceHealthScriptDailySchedule
        {
            Interval = 7,
            Time = new Time(8, 0, 0),
            UseUtc = false
        };
        var result = RunScheduleFormatter.Format(schedule);
        Assert.Equal("Every 7 days at 08:00 (local time)", result);
    }

    [Fact]
    public void Format_DailySchedule_NoTime()
    {
        var schedule = new DeviceHealthScriptDailySchedule
        {
            Interval = 1,
            UseUtc = true
        };
        var result = RunScheduleFormatter.Format(schedule);
        Assert.Equal("Daily (UTC)", result);
    }

    [Fact]
    public void Format_RunOnceSchedule_WithDateAndTime()
    {
        var schedule = new DeviceHealthScriptRunOnceSchedule
        {
            Date = new Date(2026, 3, 15),
            Time = new Time(9, 0, 0),
            UseUtc = true
        };
        var result = RunScheduleFormatter.Format(schedule);
        Assert.Equal("Once on 2026-03-15 at 09:00 (UTC)", result);
    }

    [Fact]
    public void Format_RunOnceSchedule_NoDate()
    {
        var schedule = new DeviceHealthScriptRunOnceSchedule
        {
            Time = new Time(10, 45, 0),
            UseUtc = false
        };
        var result = RunScheduleFormatter.Format(schedule);
        Assert.Equal("Once on unspecified date at 10:45 (local time)", result);
    }

    [Fact]
    public void Format_TimeSchedule_BaseType()
    {
        var schedule = new DeviceHealthScriptTimeSchedule
        {
            Interval = 2,
            Time = new Time(6, 0, 0),
            UseUtc = true
        };
        var result = RunScheduleFormatter.Format(schedule);
        Assert.Equal("Every 2 interval(s) at 06:00 (UTC)", result);
    }

    [Fact]
    public void Format_BaseSchedule_UnknownType()
    {
        var schedule = new DeviceHealthScriptRunSchedule { Interval = 5 };
        var result = RunScheduleFormatter.Format(schedule);
        Assert.Equal("Every 5 interval(s)", result);
    }

    [Fact]
    public void Format_DailySchedule_NullUseUtc_DefaultsToLocalTime()
    {
        var schedule = new DeviceHealthScriptDailySchedule
        {
            Interval = 1,
            Time = new Time(12, 0, 0)
        };
        var result = RunScheduleFormatter.Format(schedule);
        Assert.Equal("Daily at 12:00 (local time)", result);
    }
}
