// Started from `any generate DataLogger`, then rewritten by hand. The scaffolder paired the two dates with
// `Any.OrderedPair` (due after calibration) but flagged the second rule: a certificate is valid for at most
// one year. Here the due date is the calibration day plus 1 to 365 days, so the rule holds by construction.

using System;

using AgriTech.Domain;
using ColdChain.Domain;
using JustDummies;

namespace ColdChain.Tests.Expert.Generators;

public sealed partial class AnyDataLogger : IAny<DataLogger> {

    private IAny<SensorId> _id                       = new AnySensorId();
    private AnyInt32       _intervalRecipe           = Any.Int32().Between(1, 60);
    private int?           _intervalPin;
    private IAny<DateOnly> _calibratedOn             = Any.DateOnly().Between(new DateOnly(2020, 1, 1), new DateOnly(2040, 12, 31));
    private DateOnly?      _calibratedOnPin;
    private DateOnly?      _calibrationDueOnPin;
    private DateOnly?      _validThrough;

    public AnyDataLogger() { }

    private AnyDataLogger(AnyDataLogger from) {
        _id                       = from._id;
        _intervalRecipe           = from._intervalRecipe;
        _intervalPin              = from._intervalPin;
        _calibratedOn             = from._calibratedOn;
        _calibratedOnPin          = from._calibratedOnPin;
        _calibrationDueOnPin      = from._calibrationDueOnPin;
        _validThrough             = from._validThrough;
    }

    public AnyDataLogger WithId(SensorId value) {
        AnyDataLogger copy = new AnyDataLogger(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    public AnyDataLogger WithRecordingIntervalMinutes(int value) {
        AnyDataLogger copy = new AnyDataLogger(this);

        copy._intervalPin = value;

        return copy;
    }

    public AnyDataLogger WithRecordingIntervalMinutes(Func<AnyInt32, AnyInt32> configure) {
        AnyDataLogger copy = new AnyDataLogger(this);

        copy._intervalRecipe = configure(_intervalRecipe);
        copy._intervalPin    = null;

        return copy;
    }

    /// <summary>Pins the calibration day; the due date is drawn 1 to 365 days after it.</summary>
    public AnyDataLogger WithCalibratedOn(DateOnly value) {
        AnyDataLogger copy = new AnyDataLogger(this);

        copy._calibratedOnPin = value;
        copy._validThrough    = null;

        return copy;
    }

    /// <summary>Pins the due date; the calibration day is drawn 1 to 365 days before it.</summary>
    public AnyDataLogger WithCalibrationDueOn(DateOnly value) {
        AnyDataLogger copy = new AnyDataLogger(this);

        copy._calibrationDueOnPin = value;
        copy._validThrough        = null;

        return copy;
    }

    /// <summary>
    ///     A logger whose certificate is valid on every day from <paramref name="from" /> to <paramref name="to" />:
    ///     calibrated 1 to 60 days before the first, due 0 to 60 days after the last. A certificate lasts a year at most.
    /// </summary>
    public AnyDataLogger CoveringDays(DateOnly from, DateOnly to) {
        if (to.DayNumber - from.DayNumber > 240) {
            throw new ArgumentException("A calibration certificate lasts one year: it cannot cover more than 240 days with the margins drawn here.", nameof(to));
        }

        AnyDataLogger copy = new AnyDataLogger(this);

        copy._calibratedOn        = Any.DateOnly().Between(from.AddDays(-60), from.AddDays(-1));
        copy._calibratedOnPin     = null;
        copy._calibrationDueOnPin = null;
        copy._validThrough        = to;

        return copy;
    }

    public DataLogger Generate() {
        DateOnly calibratedOn;
        DateOnly calibrationDueOn;

        if (_calibratedOnPin is { } pinnedOn && _calibrationDueOnPin is { } pinnedDue) {
            calibratedOn     = pinnedOn;
            calibrationDueOn = pinnedDue;
        } else if (_calibrationDueOnPin is { } due) {
            calibrationDueOn = due;
            calibratedOn     = due.AddDays(-Any.Int32().Between(1, 365).Generate());
        } else if (_calibratedOnPin is { } on) {
            calibratedOn     = on;
            calibrationDueOn = on.AddDays(Any.Int32().Between(1, 365).Generate());
        } else if (_validThrough is { } through) {
            calibratedOn     = _calibratedOn.Generate();
            calibrationDueOn = through.AddDays(Any.Int32().Between(0, 60).Generate());
        } else {
            calibratedOn     = _calibratedOn.Generate();
            calibrationDueOn = calibratedOn.AddDays(Any.Int32().Between(1, 365).Generate());
        }

        return new DataLogger(_id.Generate(), _intervalPin ?? _intervalRecipe.Generate(), calibratedOn, calibrationDueOn);
    }

}
