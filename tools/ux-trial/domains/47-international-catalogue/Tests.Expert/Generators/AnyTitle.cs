using System;

using JustDummies;

namespace InternationalCatalogue.Domain;

/// <summary>
///     Hand-written (the scaffold drew <c>Any.String()</c>, which cannot honour a script): a title in one
///     writing script, drawn arbitrarily unless a test names it with <see cref="InScript" />.
/// </summary>
public sealed partial class AnyTitle : IAny<Title> {

    private readonly IAny<WritingScript> _script;

    public AnyTitle()
        : this(Any.Enum<WritingScript>()) { }

    private AnyTitle(IAny<WritingScript> script) {
        _script = script;
    }

    /// <summary>Titles whose letters are all of <paramref name="script" />.</summary>
    public AnyTitle InScript(WritingScript script) {
        return new AnyTitle(Any.Fixed(script));
    }

    public Title Generate() {
        WritingScript script = _script.Generate();

        return new Title(ScriptTexts.TextIn(script).Generate());
    }

}
