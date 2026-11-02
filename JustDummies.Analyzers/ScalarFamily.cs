using System;
using System.Globalization;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     A generator family JD023, JD024 and JD038 reason about: the integer generators, the temporal ones through the
///     ticks or the day number they compare by, and <c>Any.Decimal()</c> — the types whose comparison is exact
///     (issue #356). The binary floating-point families stay out: neighbours and rounding do not fold like integers.
/// </summary>
/// <remarks>
///     A family knows how the run time names its type in a refusal, how to read a constant argument of its own type
///     and render it as the run time renders a constraint call, which model holds its domain, and what its bound
///     methods are called — <c>After</c> is the temporal spelling of <c>GreaterThan</c>, and the model reads one name.
/// </remarks>
internal sealed class ScalarFamily {

    private readonly Func<Func<long, long>, IScalarDomain>      _unconstrained;
    private readonly Func<IOperation, (decimal Value, string Rendered)?> _read;

    private ScalarFamily(string name, TemporalKind? kind, Func<Func<long, long>, IScalarDomain> unconstrained, Func<IOperation, (decimal Value, string Rendered)?> read) {
        Name           = name;
        Kind           = kind;
        _unconstrained = unconstrained;
        _read          = read;
    }

    /// <summary>The factory's name, which is also the type name the run time writes in "no X value satisfies it".</summary>
    public string Name { get; }

    /// <summary>The temporal type the family draws, or <c>null</c> for a numeric one.</summary>
    public TemporalKind? Kind { get; }

    /// <summary>The family rooted at the JustDummies factory <paramref name="factory" />, or <c>null</c> for one these rules do not read.</summary>
    public static ScalarFamily? Of(string factory) {
        return factory switch {
            "Int32"          => Integer(factory, int.MinValue, int.MaxValue),
            "Int16"          => Integer(factory, short.MinValue, short.MaxValue),
            "Int64"          => Integer(factory, long.MinValue, long.MaxValue),
            "Byte"           => Integer(factory, byte.MinValue, byte.MaxValue),
            "SByte"          => Integer(factory, sbyte.MinValue, sbyte.MaxValue),
            "UInt16"         => Integer(factory, ushort.MinValue, ushort.MaxValue),
            "UInt32"         => Integer(factory, uint.MinValue, uint.MaxValue),
            // Every value above long.MaxValue remains on a ulong, whose top the model cannot hold and so leaves open.
            "UInt64"         => Integer(factory, 0, null),
            "DateOnly"       => Temporal(TemporalKind.DateOnly),
            "DateTime"       => Temporal(TemporalKind.DateTime),
            "DateTimeOffset" => Temporal(TemporalKind.DateTimeOffset),
            "TimeOnly"       => Temporal(TemporalKind.TimeOnly),
            "TimeSpan"       => Temporal(TemporalKind.TimeSpan),
            "Decimal"        => new ScalarFamily(factory, null, _ => DecimalConstraintState.Unconstrained(), ReadDecimal),
            _                => null
        };
    }

    /// <summary>
    ///     The domain before any constraint. <paramref name="toValue" /> maps an ordinal of the model back to the
    ///     value it stands for, so a bound is rendered as the run time renders it — the identity, except for a time
    ///     of day read in a frame that starts the day elsewhere than midnight.
    /// </summary>
    public IScalarDomain Unconstrained(Func<long, long> toValue) {
        return _unconstrained(toValue);
    }

    /// <summary>
    ///     A constant argument of the family's own type: the value the model reasons about, and the text the run time
    ///     writes it with in a constraint call; <c>null</c> for an argument that does not fold.
    /// </summary>
    public (decimal Value, string Rendered)? Read(IOperation operation) {
        return _read(operation);
    }

    /// <summary>The name the model reads a bound by: the temporal <c>After</c> and <c>Before</c> are the numeric <c>GreaterThan</c> and <c>LessThan</c>.</summary>
    public static string Normalize(string name) {
        return name switch {
            "After"           => "GreaterThan",
            "AfterOrEqualTo"  => "GreaterThanOrEqualTo",
            "Before"          => "LessThan",
            "BeforeOrEqualTo" => "LessThanOrEqualTo",
            _                 => name
        };
    }

    private static ScalarFamily Integer(string name, long minimum, long? maximum) {
        return new ScalarFamily(name, null, toValue => ScalarConstraintState.Unconstrained(minimum, maximum, ordinal => Invariant(toValue(ordinal))), ReadInteger);
    }

    private static ScalarFamily Temporal(TemporalKind kind) {
        (long minimum, long maximum) = TemporalFacts.Domain(kind);

        return new ScalarFamily(kind.ToString(), kind,
                                toValue => ScalarConstraintState.Unconstrained(minimum, maximum, ordinal => TemporalFacts.Render(kind, toValue(ordinal))),
                                operation => TemporalFacts.TryRead(operation, out TemporalConstant? constant) && constant!.Kind == kind ? (constant.Ordinal, constant.Rendered) : null);
    }

    /// <summary>An integer constant, as the run time writes it: invariantly, whatever minus sign the build's culture spells.</summary>
    private static (decimal Value, string Rendered)? ReadInteger(IOperation operation) {
        Optional<object?> constant = GeneratorFacts.Unwrap(operation).ConstantValue;
        if (!constant.HasValue) { return null; }

        // The unsigned trio too: Any.UInt32().GreaterThan(5) arrives as an int once Unwrap strips the conversion,
        // GreaterThan(5u) as a uint, and the verdict must not turn on how the caller spelled the literal. A ulong
        // past long.MaxValue is read, and the model then abandons the chain it cannot hold.
        decimal? value = constant.Value switch {
            int i     => i,
            long l    => l,
            short s   => s,
            byte b    => b,
            sbyte sb  => sb,
            ushort us => us,
            uint ui   => ui,
            ulong ul  => ul,
            _         => null
        };

        return value is decimal read ? (read, read.ToString(CultureInfo.InvariantCulture)) : null;
    }

    /// <summary>
    ///     A decimal constant, as the run time writes it — a literal keeps the scale it was written with, so
    ///     <c>1.50m</c> renders <c>1.50</c> — or an integer the compiler converts to one.
    /// </summary>
    private static (decimal Value, string Rendered)? ReadDecimal(IOperation operation) {
        if (operation.ConstantValue is { HasValue: true, Value: decimal converted }) { return (converted, DecimalConstraintState.Render(converted)); }

        return ReadInteger(operation) is (decimal value, _) ? (value, DecimalConstraintState.Render(value)) : null;
    }

    private static string Invariant(long value) {
        return value.ToString(CultureInfo.InvariantCulture);
    }

}
