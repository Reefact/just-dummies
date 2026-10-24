namespace JustDummies.PropertyTests;

/// <summary>
///     One call of the <c>Any.String()</c> DSL, carried twice: as the call the run time receives and as the step the
///     <see cref="StringModel" /> takes. The two are written side by side so that a command can never mean one thing
///     to the generator and another to the model.
/// </summary>
internal sealed class StringModelCommand {

    private readonly Func<AnyString, AnyString> _apply;
    private readonly Func<StringModel, bool>    _step;

    public StringModelCommand(string call, Func<AnyString, AnyString> apply, Func<StringModel, bool> step, StringModelCommandKind kind = StringModelCommandKind.Shape) {
        Call   = call;
        Kind   = kind;
        _apply = apply;
        _step  = step;
    }

    /// <summary>The call as it reads in a chain — <c>WithLength(3)</c>.</summary>
    public string Call { get; }

    /// <summary>Whether the call declares a value set, an exclusion, or neither.</summary>
    public StringModelCommandKind Kind { get; }

    /// <summary>Applies the call to the generator under test.</summary>
    public AnyString ApplyTo(AnyString generator) {
        return _apply(generator);
    }

    /// <summary>Takes the model's step; <c>false</c> when the model says the chain now contradicts itself.</summary>
    public bool StepModel(StringModel model) {
        return _step(model);
    }

    /// <inheritdoc />
    public override string ToString() {
        return Call;
    }

}
