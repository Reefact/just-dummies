using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     Reads the statically-known value of an argument. Deliberately wider than <see cref="Optional{T}" /> on
///     <c>IOperation.ConstantValue</c>: a <see cref="System.TimeSpan" /> is never a C# constant, yet
///     <c>TimeSpan.Zero</c> and <c>TimeSpan.FromSeconds(1)</c> are as statically known as any literal, and the
///     constraints that take one are exactly the ones worth checking.
/// </summary>
/// <remarks>
///     Every reader answers "no" rather than guessing. A rule built on this can therefore only under-report, which is
///     the right failure direction for a diagnostic that claims a call is certainly wrong.
/// </remarks>
internal static class ConstantFacts {

    private const string TimeSpanMetadataName = "System.TimeSpan";
    private const string ZeroFieldName        = "Zero";

    /// <summary>Reads a constant <see cref="int" />, folding named constants as the compiler does.</summary>
    public static bool TryGetInt32(IOperation operation, out int value) {
        value = 0;

        IOperation unwrapped = GeneratorFacts.Unwrap(operation);
        if (unwrapped.ConstantValue is not { HasValue: true, Value: int constant }) { return false; }

        value = constant;

        return true;
    }

    /// <summary>Reads a constant <see cref="string" />. A <c>null</c> literal answers <c>false</c>: a null argument is
    /// the null-guard's business, not a constraint rule's.</summary>
    public static bool TryGetString(IOperation operation, out string value) {
        value = string.Empty;

        IOperation unwrapped = GeneratorFacts.Unwrap(operation);
        if (unwrapped.ConstantValue is not { HasValue: true, Value: string constant }) { return false; }

        value = constant;

        return true;
    }

    /// <summary>
    ///     Whether the operation is a statically-known <b>non-positive</b> <see cref="System.TimeSpan" /> — the shape
    ///     every granularity guard rejects. Recognises <c>TimeSpan.Zero</c> and the <c>TimeSpan.FromXxx(constant)</c>
    ///     factories; anything else answers <c>false</c>.
    /// </summary>
    public static bool IsNonPositiveTimeSpan(IOperation operation, Compilation compilation) {
        INamedTypeSymbol? timeSpan = compilation.GetTypeByMetadataName(TimeSpanMetadataName);
        if (timeSpan is null) { return false; }

        IOperation unwrapped = GeneratorFacts.Unwrap(operation);

        if (unwrapped is IFieldReferenceOperation field) {
            return field.Field.Name == ZeroFieldName && SymbolEqualityComparer.Default.Equals(field.Field.ContainingType, timeSpan);
        }

        // TimeSpan.FromSeconds(0), FromMinutes(-1) ... — a single constant numeric argument settles the sign.
        if (unwrapped is IInvocationOperation { Arguments.Length: 1 } invocation
         && SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, timeSpan)
         && invocation.TargetMethod.Name.StartsWith("From", System.StringComparison.Ordinal)) {

            IOperation argument = GeneratorFacts.Unwrap(invocation.Arguments[0].Value);
            if (argument.ConstantValue is { HasValue: true, Value: { } raw }) {
                return raw switch {
                    double d  => d <= 0,
                    int i     => i <= 0,
                    long l    => l <= 0,
                    _         => false,
                };
            }
        }

        return false;
    }

    /// <summary>
    ///     Reads the ticks of a statically-known <see cref="System.TimeSpan" />: <c>TimeSpan.Zero</c>,
    ///     <c>TimeSpan.FromHours(2)</c> and its siblings with every further argument left at zero, and
    ///     <c>new TimeSpan(...)</c> from ticks or from days, hours, minutes, seconds and milliseconds.
    /// </summary>
    /// <remarks>
    ///     Only integral arguments are read (ADR-0046). A fractional one goes through floating-point conversions whose
    ///     rounding has changed between .NET versions, and the analyzer does not run on the reader's runtime, so a value
    ///     it computed could differ from the one the test draws against. An overflow answers <c>false</c> too.
    /// </remarks>
    public static bool TryGetTimeSpanTicks(IOperation operation, out long ticks) {
        IOperation unwrapped = GeneratorFacts.Unwrap(operation);
        long? read = unwrapped switch {
            IFieldReferenceOperation { Field.Name: ZeroFieldName } field when IsTimeSpan(field.Field.ContainingType) => 0,
            IInvocationOperation invocation when IsTimeSpan(invocation.TargetMethod.ContainingType)                => TimeSpanFactoryTicks(invocation),
            IObjectCreationOperation creation when IsTimeSpan(creation.Type)                                       => TimeSpanConstructorTicks(creation),
            _                                                                                                      => null
        };
        ticks = read ?? 0;

        return read.HasValue;
    }

    /// <summary><c>TimeSpan.FromXxx(n)</c>, with every further argument, if any, left at zero.</summary>
    private static long? TimeSpanFactoryTicks(IInvocationOperation invocation) {
        long? unit = invocation.TargetMethod.Name switch {
            "FromDays"         => System.TimeSpan.TicksPerDay,
            "FromHours"        => System.TimeSpan.TicksPerHour,
            "FromMinutes"      => System.TimeSpan.TicksPerMinute,
            "FromSeconds"      => System.TimeSpan.TicksPerSecond,
            "FromMilliseconds" => System.TimeSpan.TicksPerMillisecond,
            "FromTicks"        => 1,
            _                  => null
        };
        if (unit is not long perUnit || invocation.Arguments.Length == 0) { return null; }
        if (invocation.Arguments.Skip(1).Any(argument => Integral(argument.Value) != 0)) { return null; }

        return Integral(invocation.Arguments[0].Value) is long count ? Multiplied(count, perUnit) : null;
    }

    /// <summary><c>new TimeSpan(ticks)</c>, or the days, hours, minutes, seconds and milliseconds forms.</summary>
    private static long? TimeSpanConstructorTicks(IObjectCreationOperation creation) {
        long?[] parts = creation.Arguments.Select(argument => Integral(argument.Value)).ToArray();
        if (parts.Length == 0 || parts.Any(part => part is null)) { return null; }
        if (parts.Length == 1) { return parts[0]; }

        // (hours, minutes, seconds), (days, hours, minutes, seconds) and (days, hours, minutes, seconds, milliseconds).
        long[] units = parts.Length switch {
            3 => [System.TimeSpan.TicksPerHour, System.TimeSpan.TicksPerMinute, System.TimeSpan.TicksPerSecond],
            4 => [System.TimeSpan.TicksPerDay, System.TimeSpan.TicksPerHour, System.TimeSpan.TicksPerMinute, System.TimeSpan.TicksPerSecond],
            5 => [System.TimeSpan.TicksPerDay, System.TimeSpan.TicksPerHour, System.TimeSpan.TicksPerMinute, System.TimeSpan.TicksPerSecond, System.TimeSpan.TicksPerMillisecond],
            _ => []
        };
        if (units.Length == 0) { return null; }

        long? total = 0;
        for (int index = 0; index < units.Length && total is long sum; index++) {
            total = Multiplied(parts[index]!.Value, units[index]) is long ticks ? Added(sum, ticks) : null;
        }

        return total;
    }

    /// <summary>A constant integral value, or a floating-point constant with no fractional part; <c>null</c> otherwise.</summary>
    private static long? Integral(IOperation operation) {
        if (operation.ConstantValue is not { HasValue: true, Value: { } value }) { return null; }

        return value switch {
            int or long or short or sbyte or byte or ushort or uint => System.Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture),
            double number when System.Math.Abs(number % 1) < double.Epsilon && System.Math.Abs(number) < long.MaxValue => (long)number,
            _ => null
        };
    }

    private static long? Multiplied(long left, long right) {
        try {
            return checked(left * right);
        } catch (System.OverflowException) {
            return null;
        }
    }

    private static long? Added(long left, long right) {
        try {
            return checked(left + right);
        } catch (System.OverflowException) {
            return null;
        }
    }

    private static bool IsTimeSpan(ITypeSymbol? type) {
        return type?.ToDisplayString() == TimeSpanMetadataName;
    }

}
