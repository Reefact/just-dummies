using System.Diagnostics.CodeAnalysis;

using JustDummies.Diagnostics;

namespace JustDummies.PropertyTests;

/// <summary>
///     A generated <c>Any.String()</c> chain, and the comparison <see cref="StringModelProperties" /> makes of it: the
///     run time and the <see cref="StringModel" /> are given the same calls, and must refuse at the same call or draw
///     only values the model accepts.
/// </summary>
internal sealed class StringModelChain {

    /// <summary>A generator is a recipe, not a value: a handful of draws per chain tests more of its randomness than one.</summary>
    private const int DrawsPerChain = 8;

    public StringModelChain(IReadOnlyList<StringModelCommand> commands) {
        Commands = commands;
    }

    public IReadOnlyList<StringModelCommand> Commands { get; }

    #region Statics members declarations

    /// <summary>A drawn value as a reader can see it, its control characters escaped.</summary>
    private static string Show(string value) {
        return "\"" + string.Concat(value.Select(character => char.IsControl(character) ? $"\\u{(int)character:X4}" : character.ToString())) + "\"";
    }

    #endregion

    /// <summary>
    ///     <c>null</c> when the run time agrees with the model on this chain under <paramref name="seed" />; otherwise
    ///     what they disagree on, which is the counter-example's explanation.
    /// </summary>
    [SuppressMessage(JustDummiesRule.JD030.Category, JustDummiesRule.JD030.Id, Justification = SuppressionJustification.JD030.LengthIsNotWhatThisPins)]
    public string? Disagreement(int seed) {
        StringModel model        = new();
        int?        modelRefusal = null;
        for (int call = 0; call < Commands.Count && modelRefusal is null; call++) {
            if (!Commands[call].StepModel(model)) { modelRefusal = call; }
        }

        AnyString generator      = Any.WithSeed(seed).String();
        int?      runtimeRefusal = null;
        for (int call = 0; call < Commands.Count && runtimeRefusal is null; call++) {
            try {
                generator = Commands[call].ApplyTo(generator);
            } catch (ConflictingAnyConstraintException) {
                runtimeRefusal = call;
            }
        }

        if (modelRefusal != runtimeRefusal) { return $"the model refuses at {Describe(modelRefusal)}, the run time at {Describe(runtimeRefusal)}"; }
        if (modelRefusal is not null) { return null; }

        return DrawDisagreement(model, generator);
    }

    /// <summary>The draws of a chain both sides accepted: refused at the draw by both, or yielding only values the model accepts.</summary>
    private static string? DrawDisagreement(StringModel model, AnyString generator) {
        bool drawable = model.IsDrawable();
        for (int draw = 0; draw < DrawsPerChain; draw++) {
            string value;
            try {
                value = generator.Generate();
            } catch (ConflictingAnyConstraintException) {
                return drawable ? "the run time refuses to draw a chain the model draws" : null;
            }

            if (!drawable) { return $"the run time draws {Show(value)} from a chain the model refuses to draw"; }
            if (!model.Accepts(value)) { return $"the run time draws {Show(value)}, which the model does not accept"; }
        }

        return null;
    }

    private string Describe(int? call) {
        return call is int index ? $"{Commands[index].Call} (call {index + 1})" : "no call";
    }

    /// <summary>
    ///     The chains one call shorter, so FsCheck reports the smallest chain that still disagrees. Dropping the value
    ///     set drops the exclusions with it: the model judges an exclusion only beside a value set.
    /// </summary>
    public IEnumerable<StringModelChain> Shrink() {
        for (int call = 0; call < Commands.Count; call++) {
            int removed = call;

            yield return Commands[removed].Kind == StringModelCommandKind.ValueSet
                             ? new StringModelChain(Commands.Where(command => command.Kind == StringModelCommandKind.Shape).ToList())
                             : new StringModelChain(Commands.Where((_, index) => index != removed).ToList());
        }
    }

    /// <inheritdoc />
    public override string ToString() {
        return "Any.String()" + string.Concat(Commands.Select(command => "." + command.Call));
    }

}
