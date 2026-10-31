using System.Text.RegularExpressions;

namespace BatchGenealogy.Domain;

/// <summary>
/// One manufacturing step: input lot quantities consumed, output lot quantities produced.
/// The output never exceeds the input; the difference is the process loss.
/// </summary>
public sealed partial class Transformation
{
    public Transformation(string id, TransformationKind kind, IReadOnlyList<LotQuantity> inputs, IReadOnlyList<LotQuantity> outputs, DateOnly performedOn)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(outputs);
        if (!IdPattern().IsMatch(id))
        {
            throw new FormatException($"'{id}' is not a transformation id: expected TRF- followed by 6 digits.");
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown transformation kind.");
        }

        if (inputs.Any(input => input is null) || outputs.Any(output => output is null))
        {
            throw new ArgumentException("Inputs and outputs cannot contain null entries.");
        }

        EnsureArity(kind, inputs.Count, outputs.Count);
        EnsureDistinct(inputs, nameof(inputs));
        EnsureDistinct(outputs, nameof(outputs));

        LotQuantity? both = outputs.FirstOrDefault(output => inputs.Any(input => input.Lot.Equals(output.Lot)));
        if (both is not null)
        {
            throw new ArgumentException($"Lot {both.Lot} cannot be both an input and an output of the same transformation.", nameof(outputs));
        }

        Quantity reference = inputs[0].Quantity;
        if (inputs.Concat(outputs).Any(part => !part.Quantity.IsCommensurableWith(reference)))
        {
            throw new ArgumentException("All inputs and outputs of a transformation are measured in the same dimension (mass or volume).");
        }

        if (kind == TransformationKind.Split)
        {
            LotNumber source = inputs[0].Lot;
            LotQuantity? stranger = outputs.FirstOrDefault(output => !output.Lot.IsSubLot || output.Lot.BaseLot != source.BaseLot);
            if (stranger is not null)
            {
                throw new ArgumentException($"A split of {source} produces sub-lots of {source.BaseLot}; {stranger.Lot} is not one.", nameof(outputs));
            }
        }

        decimal totalInput = inputs.Sum(input => input.Quantity.BaseAmount);
        decimal totalOutput = outputs.Sum(output => output.Quantity.BaseAmount);
        if (totalOutput > totalInput)
        {
            throw new ArgumentException($"A transformation cannot produce more than it consumes ({totalOutput} > {totalInput} in base units).", nameof(outputs));
        }

        Id = id;
        Kind = kind;
        Inputs = inputs.ToArray();
        Outputs = outputs.ToArray();
        PerformedOn = performedOn;
    }

    public string Id { get; }

    public TransformationKind Kind { get; }

    public IReadOnlyList<LotQuantity> Inputs { get; }

    public IReadOnlyList<LotQuantity> Outputs { get; }

    public DateOnly PerformedOn { get; }

    public Quantity TotalInput => Inputs.Skip(1).Aggregate(Inputs[0].Quantity, (sum, input) => sum.Add(input.Quantity));

    public Quantity TotalOutput => Outputs.Skip(1).Aggregate(Outputs[0].Quantity.ConvertTo(Inputs[0].Quantity.Unit), (sum, output) => sum.Add(output.Quantity));

    /// <summary>Material lost by the step (dust, residue, sampling), in the unit of the first input.</summary>
    public Quantity Loss => TotalInput.Subtract(TotalOutput);

    /// <summary>Output over input, in [0, 1].</summary>
    public decimal Yield => TotalOutput.BaseAmount / TotalInput.BaseAmount;

    /// <summary>
    /// The fraction of every output that comes from <paramref name="input"/>, assuming homogeneous mixing:
    /// its share of the total input mass or volume.
    /// </summary>
    public decimal ShareOf(LotNumber input)
    {
        ArgumentNullException.ThrowIfNull(input);
        LotQuantity part = Inputs.FirstOrDefault(candidate => candidate.Lot.Equals(input))
            ?? throw new ArgumentException($"Lot {input} is not an input of {Id}.", nameof(input));
        return part.Quantity.BaseAmount / TotalInput.BaseAmount;
    }

    private static void EnsureArity(TransformationKind kind, int inputCount, int outputCount)
    {
        (bool valid, string rule) = kind switch
        {
            TransformationKind.Blend => (inputCount >= 2 && outputCount == 1, "a blend takes at least two inputs and produces one output"),
            TransformationKind.Split => (inputCount == 1 && outputCount >= 2, "a split takes one input and produces at least two outputs"),
            _ => (inputCount >= 1 && outputCount == 1, "a process takes at least one input and produces one output"),
        };
        if (!valid)
        {
            throw new ArgumentException($"Invalid {kind} with {inputCount} input(s) and {outputCount} output(s): {rule}.");
        }
    }

    private static void EnsureDistinct(IReadOnlyList<LotQuantity> parts, string parameterName)
    {
        if (parts.Select(part => part.Lot).Distinct().Count() != parts.Count)
        {
            throw new ArgumentException("A lot appears at most once on each side of a transformation.", parameterName);
        }
    }

    [GeneratedRegex("^TRF-[0-9]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex IdPattern();
}
