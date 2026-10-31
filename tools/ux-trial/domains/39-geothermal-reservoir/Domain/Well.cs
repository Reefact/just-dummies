using System.Text.RegularExpressions;

namespace GeothermalReservoir.Domain;

public enum WellKind
{
    Injector,
    Producer,
}

/// <summary>
/// A well completed in one cell. Its identifier is "INJ-nn" for an injector and "PRD-nn" for a producer;
/// an injector carries the temperature of the fluid it injects, a producer never does.
/// </summary>
public sealed partial class Well
{
    public const double MaximumRateCubicMetresPerSecond = 0.5;

    public Well(string id, WellKind kind, CellCoordinate completion, double rateCubicMetresPerSecond, Temperature? injectionTemperature)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(completion);
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown well kind.");
        Match match = IdPattern().Match(id);
        if (!match.Success)
        {
            throw new FormatException($"Well id '{id}' must look like INJ-01 or PRD-01.");
        }

        WellKind idKind = match.Groups[1].Value == "INJ" ? WellKind.Injector : WellKind.Producer;
        if (idKind != kind)
        {
            throw new ArgumentException($"Well id '{id}' denotes a {idKind}, but the well is a {kind}.", nameof(id));
        }

        if (double.IsNaN(rateCubicMetresPerSecond) || rateCubicMetresPerSecond <= 0.0 || rateCubicMetresPerSecond > MaximumRateCubicMetresPerSecond)
        {
            throw new ArgumentOutOfRangeException(nameof(rateCubicMetresPerSecond), rateCubicMetresPerSecond,
                $"Well rate must be in (0, {MaximumRateCubicMetresPerSecond}] m³/s.");
        }

        if (kind == WellKind.Injector && injectionTemperature is null)
        {
            throw new ArgumentException("An injector must state the temperature of the fluid it injects.", nameof(injectionTemperature));
        }

        if (kind == WellKind.Producer && injectionTemperature is not null)
        {
            throw new ArgumentException("A producer has no injection temperature.", nameof(injectionTemperature));
        }

        Id = id;
        Kind = kind;
        Completion = completion;
        RateCubicMetresPerSecond = rateCubicMetresPerSecond;
        InjectionTemperature = injectionTemperature;
    }

    public string Id { get; }
    public WellKind Kind { get; }
    public CellCoordinate Completion { get; }

    /// <summary>Volumetric rate magnitude in m³/s; its sign comes from <see cref="Kind"/>.</summary>
    public double RateCubicMetresPerSecond { get; }

    public Temperature? InjectionTemperature { get; }

    /// <summary>Signed source term: positive into the reservoir, negative out of it.</summary>
    public double SignedRate => Kind == WellKind.Injector ? RateCubicMetresPerSecond : -RateCubicMetresPerSecond;

    [GeneratedRegex("^(INJ|PRD)-[0-9]{2}$")]
    private static partial Regex IdPattern();
}
