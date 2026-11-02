using System;
using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     Reads the statically-known value of a temporal argument. None of these types has a C# constant, yet
///     <c>new DateOnly(2026, 1, 1)</c>, <c>DateTime.MaxValue</c> or <c>TimeSpan.FromHours(2)</c> is as statically known
///     as any literal, and the bounds that take one are exactly the ones worth checking (issues #356 and #366).
/// </summary>
/// <remarks>
///     <para>
///         Every reader answers "no" rather than guessing, as <see cref="ConstantFacts" /> does, so a rule built on this
///         can only under-report. A <c>DateTime</c> whose kind is <c>Local</c> is not read: the run time refuses it as
///         a bound (ADR-0114), and what instant it names depends on the machine. A <c>DateTimeOffset</c> built from a
///         <c>DateTime</c> of unspecified kind takes the machine's offset, so it is not read either. An argument the
///         type's own constructor would refuse — a thirteenth month, an offset past ±14:00 — is not read: the call
///         throws on the argument before any generator sees it, which is not a constraint rule's to report.
///     </para>
///     <para>
///         This assembly targets netstandard2.0, which has neither <c>DateOnly</c> nor <c>TimeOnly</c>, so a date is
///         computed through <see cref="DateTime" /> and a time of day through its ticks; the arithmetic is the one
///         those types perform, and the rendering is the one their <c>O</c> format writes.
///     </para>
/// </remarks>
internal static class TemporalFacts {

    private const string DateOnlyName       = "System.DateOnly";
    private const string DateTimeName       = "System.DateTime";
    private const string DateTimeOffsetName = "System.DateTimeOffset";
    private const string TimeOnlyName       = "System.TimeOnly";
    private const string TimeSpanName       = "System.TimeSpan";
    private const string DateTimeKindName   = "System.DateTimeKind";
    private const string MinValueName       = "MinValue";
    private const string MaxValueName       = "MaxValue";
    private const string UnixEpochName      = "UnixEpoch";

    private const int  DateTimeKindUtc        = 1;
    private const int  DateTimeKindLocal      = 2;
    private const int  MaxHour                = 23;
    private const int  MaxMinuteOrSecond      = 59;
    private const int  MaxMillisecond         = 999;
    private const long MaxDayNumber           = 3652058;
    private const long OffsetLimitTicks       = 14 * TimeSpan.TicksPerHour;
    private static readonly long UnixEpochTicks = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;

    /// <summary>Reads a temporal constant in any spelling the class remarks list, or answers <c>false</c>.</summary>
    public static bool TryRead(IOperation operation, out TemporalConstant? constant) {
        IOperation unwrapped = GeneratorFacts.Unwrap(operation);
        constant = unwrapped switch {
            IMemberReferenceOperation member  => KnownMember(member),
            IInvocationOperation invocation   => Factory(invocation),
            IObjectCreationOperation creation => Constructed(creation),
            _                                 => null
        };

        return constant is not null;
    }

    /// <summary>The ordinal rendered as the run time writes a value of <paramref name="kind" />.</summary>
    public static string Render(TemporalKind kind, long ordinal) {
        return kind switch {
            TemporalKind.DateOnly       => new DateTime(ordinal * TimeSpan.TicksPerDay, DateTimeKind.Unspecified).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TemporalKind.DateTime       => new DateTime(ordinal, DateTimeKind.Utc).ToString("O", CultureInfo.InvariantCulture),
            TemporalKind.DateTimeOffset => new DateTimeOffset(ordinal, TimeSpan.Zero).ToString("O", CultureInfo.InvariantCulture),
            TemporalKind.TimeOnly       => new DateTime(ordinal, DateTimeKind.Unspecified).ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture),
            _                           => new TimeSpan(ordinal).ToString("c", CultureInfo.InvariantCulture)
        };
    }

    /// <summary>The smallest and largest ordinal a value of <paramref name="kind" /> holds.</summary>
    public static (long Minimum, long Maximum) Domain(TemporalKind kind) {
        return kind switch {
            TemporalKind.DateOnly                           => (0, MaxDayNumber),
            TemporalKind.DateTime or TemporalKind.DateTimeOffset => (0, DateTime.MaxValue.Ticks),
            TemporalKind.TimeOnly                           => (0, TimeSpan.TicksPerDay - 1),
            _                                               => (long.MinValue, long.MaxValue)
        };
    }

    // A field on DateTime, DateTimeOffset and TimeSpan, a property on DateOnly and TimeOnly: read as the member either is.
    private static TemporalConstant? KnownMember(IMemberReferenceOperation member) {
        string owner = member.Member.ContainingType.ToDisplayString();
        string name  = member.Member.Name;

        return (owner, name) switch {
            (DateTimeName, MinValueName)        => Of(TemporalKind.DateTime, 0),
            (DateTimeName, MaxValueName)        => Of(TemporalKind.DateTime, DateTime.MaxValue.Ticks),
            (DateTimeName, UnixEpochName)       => Of(TemporalKind.DateTime, UnixEpochTicks),
            (DateOnlyName, MinValueName)        => Of(TemporalKind.DateOnly, 0),
            (DateOnlyName, MaxValueName)        => Of(TemporalKind.DateOnly, MaxDayNumber),
            (TimeOnlyName, MinValueName)        => Of(TemporalKind.TimeOnly, 0),
            (TimeOnlyName, MaxValueName)        => Of(TemporalKind.TimeOnly, TimeSpan.TicksPerDay - 1),
            (DateTimeOffsetName, MinValueName)  => Of(TemporalKind.DateTimeOffset, 0),
            (DateTimeOffsetName, MaxValueName)  => Of(TemporalKind.DateTimeOffset, DateTime.MaxValue.Ticks),
            (DateTimeOffsetName, UnixEpochName) => Of(TemporalKind.DateTimeOffset, UnixEpochTicks),
            (TimeSpanName, "Zero")            => Of(TemporalKind.TimeSpan, 0),
            (TimeSpanName, MinValueName)        => Of(TemporalKind.TimeSpan, long.MinValue),
            (TimeSpanName, MaxValueName)        => Of(TemporalKind.TimeSpan, long.MaxValue),
            _                                 => null
        };
    }

    private static TemporalConstant? Factory(IInvocationOperation invocation) {
        string owner = invocation.TargetMethod.ContainingType.ToDisplayString();

        switch (owner) {
            case TimeSpanName:
                return ConstantFacts.TryGetTimeSpanTicks(invocation, out long ticks) ? Of(TemporalKind.TimeSpan, ticks) : null;

            // DateOnly.FromDayNumber(n), the one factory whose argument is the ordinal itself.
            case DateOnlyName when invocation.TargetMethod.Name == "FromDayNumber" && invocation.Arguments.Length == 1:
                return ConstantFacts.Integral(invocation.Arguments[0].Value) is long day && day is >= 0 and <= MaxDayNumber ? Of(TemporalKind.DateOnly, day) : null;

            // TimeOnly.FromTimeSpan(duration), for a duration within the day.
            case TimeOnlyName when invocation.TargetMethod.Name == "FromTimeSpan" && invocation.Arguments.Length == 1:
                return ConstantFacts.TryGetTimeSpanTicks(invocation.Arguments[0].Value, out long within) && within >= 0 && within < TimeSpan.TicksPerDay ? Of(TemporalKind.TimeOnly, within) : null;

            default:
                return null;
        }
    }

    private static TemporalConstant? Constructed(IObjectCreationOperation creation) {
        if (creation.Constructor is null || creation.Type is null) { return null; }

        IOperation[] arguments = creation.Arguments.OrderBy(argument => argument.Parameter?.Ordinal ?? int.MaxValue).Select(argument => argument.Value).ToArray();

        return creation.Type.ToDisplayString() switch {
            TimeSpanName       => ConstantFacts.TryGetTimeSpanTicks(creation, out long ticks) ? Of(TemporalKind.TimeSpan, ticks) : null,
            DateTimeName       => ConstructedDateTime(creation.Constructor, arguments),
            DateOnlyName       => ConstructedDateOnly(creation.Constructor, arguments),
            TimeOnlyName       => ConstructedTimeOnly(creation.Constructor, arguments),
            DateTimeOffsetName => ConstructedDateTimeOffset(creation.Constructor, arguments),
            _                  => null
        };
    }

    /// <summary>
    ///     <c>new DateTime(ticks)</c>, <c>(year, month, day)</c>, then hours, minutes, seconds and milliseconds, each
    ///     with or without a trailing <c>DateTimeKind</c> that is not <c>Local</c>. Rendered with the kind written,
    ///     as the run time renders the caller's value.
    /// </summary>
    private static TemporalConstant? ConstructedDateTime(IMethodSymbol constructor, IOperation[] arguments) {
        if (!TrySplitKind(constructor, arguments, out long[] parts, out DateTimeKind kind)) { return null; }

        try {
            DateTime value = parts.Length switch {
                1 => new DateTime(parts[0], kind),
                3 => new DateTime((int)parts[0], (int)parts[1], (int)parts[2], 0, 0, 0, kind),
                6 => new DateTime((int)parts[0], (int)parts[1], (int)parts[2], (int)parts[3], (int)parts[4], (int)parts[5], kind),
                7 => new DateTime((int)parts[0], (int)parts[1], (int)parts[2], (int)parts[3], (int)parts[4], (int)parts[5], (int)parts[6], kind),
                _ => throw new ArgumentOutOfRangeException(nameof(arguments))
            };

            return new TemporalConstant(TemporalKind.DateTime, value.Ticks, value.ToString("O", CultureInfo.InvariantCulture));
        } catch (ArgumentOutOfRangeException) {
            return null;
        }
    }

    /// <summary><c>new DateOnly(year, month, day)</c>.</summary>
    private static TemporalConstant? ConstructedDateOnly(IMethodSymbol constructor, IOperation[] arguments) {
        if (constructor.Parameters.Length != 3 || !constructor.Parameters.All(IsIntegralParameter)) { return null; }

        long?[] parts = arguments.Select(ConstantFacts.Integral).ToArray();
        if (parts.Length != 3 || parts.Any(part => part is null) || parts.Any(part => part < int.MinValue || part > int.MaxValue)) { return null; }

        try {
            return Of(TemporalKind.DateOnly, new DateTime((int)parts[0]!.Value, (int)parts[1]!.Value, (int)parts[2]!.Value, 0, 0, 0, DateTimeKind.Unspecified).Ticks / TimeSpan.TicksPerDay);
        } catch (ArgumentOutOfRangeException) {
            return null;
        }
    }

    /// <summary><c>new TimeOnly(ticks)</c>, <c>(hour, minute)</c>, then seconds, milliseconds and microseconds.</summary>
    private static TemporalConstant? ConstructedTimeOnly(IMethodSymbol constructor, IOperation[] arguments) {
        if (!constructor.Parameters.All(IsIntegralParameter)) { return null; }

        long?[] parts = arguments.Select(ConstantFacts.Integral).ToArray();
        if (parts.Any(part => part is null)) { return null; }

        if (parts.Length == 1) { return parts[0] is long ticks && ticks >= 0 && ticks < TimeSpan.TicksPerDay ? Of(TemporalKind.TimeOnly, ticks) : null; }
        if (parts.Length is < 2 or > 5) { return null; }

        long[] units  = [TimeSpan.TicksPerHour, TimeSpan.TicksPerMinute, TimeSpan.TicksPerSecond, TimeSpan.TicksPerMillisecond, TimeSpan.TicksPerMillisecond / 1000];
        long[] limits = [MaxHour, MaxMinuteOrSecond, MaxMinuteOrSecond, MaxMillisecond, MaxMillisecond];
        long   total  = 0;
        for (int index = 0; index < parts.Length; index++) {
            long part = parts[index]!.Value;
            if (part < 0 || part > limits[index]) { return null; }

            total += part * units[index];
        }

        return Of(TemporalKind.TimeOnly, total);
    }

    /// <summary>
    ///     <c>new DateTimeOffset(ticks, offset)</c>, the date-and-time forms with a trailing offset, and the two forms
    ///     built from a <c>DateTime</c> — a UTC one alone, or a non-local one with an offset. Rendered with the offset
    ///     written, as the run time renders the caller's value.
    /// </summary>
    private static TemporalConstant? ConstructedDateTimeOffset(IMethodSymbol constructor, IOperation[] arguments) {
        if (arguments.Length == 0) { return null; }

        (long Local, long Offset)? read = constructor.Parameters[0].Type.ToDisplayString() == DateTimeName
                                              ? FromDateTime(arguments)
                                              : FromParts(constructor, arguments);
        if (read is not (long local, long written)) { return null; }
        if (written % TimeSpan.TicksPerMinute != 0 || written < -OffsetLimitTicks || written > OffsetLimitTicks) { return null; }

        long utc = local - written;
        if (utc < 0 || utc > DateTime.MaxValue.Ticks) { return null; }

        return new TemporalConstant(TemporalKind.DateTimeOffset, utc, new DateTimeOffset(local, new TimeSpan(written)).ToString("O", CultureInfo.InvariantCulture));
    }

    /// <summary>The local ticks and the offset of a <c>DateTimeOffset</c> built from a <c>DateTime</c>: a UTC one alone takes no offset, a non-local one takes the offset written beside it.</summary>
    private static (long Local, long Offset)? FromDateTime(IOperation[] arguments) {
        if (!TryReadDateTimeArgument(arguments[0], out long ticks, out DateTimeKind kind)) { return null; }

        if (arguments.Length == 1) { return kind == DateTimeKind.Utc ? (ticks, 0) : null; }
        if (arguments.Length != 2 || !ConstantFacts.TryGetTimeSpanTicks(arguments[1], out long offset)) { return null; }

        return kind != DateTimeKind.Utc || offset == 0 ? (ticks, offset) : null;
    }

    /// <summary>The local ticks and the offset of a <c>DateTimeOffset</c> built from its parts and a trailing offset.</summary>
    private static (long Local, long Offset)? FromParts(IMethodSymbol constructor, IOperation[] arguments) {
        int count = constructor.Parameters.Length;
        if (count < 2 || arguments.Length != count) { return null; }
        if (constructor.Parameters[count - 1].Type.ToDisplayString() != TimeSpanName) { return null; }
        if (!constructor.Parameters.Take(count - 1).All(IsIntegralParameter)) { return null; }
        if (!ConstantFacts.TryGetTimeSpanTicks(arguments[count - 1], out long offset)) { return null; }

        long?[] parts = arguments.Take(count - 1).Select(ConstantFacts.Integral).ToArray();
        if (parts.Any(part => part is null)) { return null; }

        return LocalTicks(parts.Select(part => part!.Value).ToArray()) is long local ? (local, offset) : null;
    }

    /// <summary>The ticks of a date-and-time spelled as its parts: ticks alone, or year, month, day and so on down to milliseconds.</summary>
    private static long? LocalTicks(long[] parts) {
        try {
            return parts.Length switch {
                1 => new DateTime(parts[0], DateTimeKind.Unspecified).Ticks,
                6 => new DateTime((int)parts[0], (int)parts[1], (int)parts[2], (int)parts[3], (int)parts[4], (int)parts[5], DateTimeKind.Unspecified).Ticks,
                7 => new DateTime((int)parts[0], (int)parts[1], (int)parts[2], (int)parts[3], (int)parts[4], (int)parts[5], (int)parts[6], DateTimeKind.Unspecified).Ticks,
                _ => null
            };
        } catch (ArgumentOutOfRangeException) {
            return null;
        }
    }

    /// <summary>
    ///     The integral parts of a <c>DateTime</c> constructor call and the kind it names, <c>Unspecified</c> when it
    ///     names none; <c>false</c> for a <c>Local</c> kind, a non-constant part or a parameter of another type.
    /// </summary>
    private static bool TrySplitKind(IMethodSymbol constructor, IOperation[] arguments, out long[] parts, out DateTimeKind kind) {
        parts = [];
        kind  = DateTimeKind.Unspecified;

        int count = constructor.Parameters.Length;
        if (count == 0 || arguments.Length != count) { return false; }

        if (constructor.Parameters[count - 1].Type.ToDisplayString() == DateTimeKindName) {
            if (arguments[count - 1].ConstantValue is not { HasValue: true, Value: int named } || named == DateTimeKindLocal) { return false; }

            kind  = named == DateTimeKindUtc ? DateTimeKind.Utc : DateTimeKind.Unspecified;
            count -= 1;
        }

        if (!constructor.Parameters.Take(count).All(IsIntegralParameter)) { return false; }

        long?[] read = arguments.Take(count).Select(ConstantFacts.Integral).ToArray();
        if (read.Any(part => part is null)) { return false; }

        parts = read.Select(part => part!.Value).ToArray();

        return count == 1 || parts.All(part => part >= int.MinValue && part <= int.MaxValue);
    }

    /// <summary>A <c>DateTime</c> argument of a <c>DateTimeOffset</c> constructor, with the kind it was written with.</summary>
    private static bool TryReadDateTimeArgument(IOperation argument, out long ticks, out DateTimeKind kind) {
        ticks = 0;
        kind  = DateTimeKind.Unspecified;

        IOperation unwrapped = GeneratorFacts.Unwrap(argument);
        if (unwrapped is IObjectCreationOperation { Constructor: { } constructor } creation && creation.Type?.ToDisplayString() == DateTimeName) {
            IOperation[] parts = creation.Arguments.OrderBy(part => part.Parameter?.Ordinal ?? int.MaxValue).Select(part => part.Value).ToArray();
            if (ConstructedDateTime(constructor, parts) is not TemporalConstant built || !TrySplitKind(constructor, parts, out _, out kind)) { return false; }

            ticks = built.Ordinal;

            return true;
        }

        if (unwrapped is IMemberReferenceOperation member && KnownMember(member) is { Kind: TemporalKind.DateTime } known) {
            ticks = known.Ordinal;
            kind  = member.Member.Name == UnixEpochName ? DateTimeKind.Utc : DateTimeKind.Unspecified;

            return true;
        }

        return false;
    }

    private static bool IsIntegralParameter(IParameterSymbol parameter) {
        return parameter.Type.SpecialType is SpecialType.System_Int32 or SpecialType.System_Int64;
    }

    private static TemporalConstant Of(TemporalKind kind, long ordinal) {
        return new TemporalConstant(kind, ordinal, Render(kind, ordinal));
    }

}
