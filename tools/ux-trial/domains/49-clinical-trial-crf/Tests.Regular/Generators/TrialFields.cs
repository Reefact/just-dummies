using System;

using ClinicalTrial.Domain;
using JustDummies;

namespace ClinicalTrial.Domain.Tests.Regular.Generators;

/// <summary>Shared building blocks of the hand-completed generators.</summary>
public static class TrialFields {

    public static AnyDateOnly Date() {
        return Any.DateOnly().Between(new DateOnly(2020, 1, 1), new DateOnly(2030, 12, 31));
    }

    public static AnyPattern AnalyteCode() {
        return Any.StringMatching("[A-Z][A-Z0-9]{1,9}");
    }

    public static AnyPattern VisitCode() {
        return Any.StringMatching("[A-Z][A-Z0-9]{0,7}");
    }

    public static IAny<MissingReason> AnyReason() {
        return Any.Enum<MissingReason>();
    }

    /// <summary>A resolved field: a measured value.</summary>
    public static IAny<Observed<T>?> Observation<T>(IAny<T> value) where T : struct {
        return value.As(v => (Observed<T>?)Observed<T>.Of(v));
    }

    /// <summary>A resolved field: missing, with a reason.</summary>
    public static IAny<Observed<T>?> Absence<T>() where T : struct {
        return Any.Enum<MissingReason>().As(r => (Observed<T>?)Observed<T>.Missing(r));
    }

    /// <summary>A resolved field: missing, with any reason but the one given.</summary>
    public static IAny<Observed<T>?> AbsenceExcept<T>(MissingReason excluded) where T : struct {
        return Any.Enum<MissingReason>().DifferentFrom(excluded).As(r => (Observed<T>?)Observed<T>.Missing(r));
    }

    /// <summary>A field not entered yet.</summary>
    public static IAny<Observed<T>?> Blank<T>() where T : struct {
        return Any.Fixed<Observed<T>?>(null);
    }

    /// <summary>Mostly measured, sometimes missing with a reason, sometimes not entered yet.</summary>
    public static IAny<Observed<T>?> Entered<T>(IAny<T> value) where T : struct {
        IAny<Observed<T>?> observed = Observation(value);
        IAny<Observed<T>?> missing  = Absence<T>();
        IAny<Observed<T>?> blank    = Blank<T>();

        return Any.Int32().Between(0, 9).Then(k => k < 6 ? observed : k < 8 ? missing : blank);
    }

}
