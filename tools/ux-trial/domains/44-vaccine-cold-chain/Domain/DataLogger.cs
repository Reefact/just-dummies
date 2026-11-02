using AgriTech.Domain;

namespace ColdChain.Domain;

/// <summary>
/// A calibrated temperature data logger travelling with a pallet. It samples at a fixed interval;
/// a calibration certificate is valid for at most one year.
/// </summary>
public sealed class DataLogger : IEquatable<DataLogger>
{
    public DataLogger(SensorId id, int recordingIntervalMinutes, DateOnly calibratedOn, DateOnly calibrationDueOn)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (recordingIntervalMinutes < 1 || recordingIntervalMinutes > 60)
        {
            throw new ArgumentOutOfRangeException(nameof(recordingIntervalMinutes), recordingIntervalMinutes, "A logger records every 1 to 60 minutes.");
        }

        if (calibrationDueOn <= calibratedOn)
        {
            throw new ArgumentException("A calibration falls due after the day it was done.", nameof(calibrationDueOn));
        }

        if (calibrationDueOn > calibratedOn.AddYears(1))
        {
            throw new ArgumentException("A calibration certificate is valid for at most one year.", nameof(calibrationDueOn));
        }

        Id = id;
        RecordingIntervalMinutes = recordingIntervalMinutes;
        CalibratedOn = calibratedOn;
        CalibrationDueOn = calibrationDueOn;
    }

    public SensorId Id { get; }

    public int RecordingIntervalMinutes { get; }

    public DateOnly CalibratedOn { get; }

    public DateOnly CalibrationDueOn { get; }

    /// <summary>The longest silence between two readings that still counts as a continuous record: two intervals.</summary>
    public TimeSpan MaxSilence => TimeSpan.FromMinutes(2 * RecordingIntervalMinutes);

    public bool IsCalibratedOn(DateOnly day) => day >= CalibratedOn && day <= CalibrationDueOn;

    public bool Equals(DataLogger? other) => other is not null && other.Id.Equals(Id);

    public override bool Equals(object? obj) => Equals(obj as DataLogger);

    public override int GetHashCode() => Id.GetHashCode();

    public override string ToString() => Id.ToString();
}
