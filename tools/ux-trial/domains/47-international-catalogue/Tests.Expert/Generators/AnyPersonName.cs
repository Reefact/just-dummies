using System;
using System.Collections.Generic;

using JustDummies;

namespace InternationalCatalogue.Domain;

/// <summary>
///     Hand-written: a name's two parts must share one supported script, a rule between parameters the
///     scaffold cannot carry. Given names are present or absent unless a test says which.
/// </summary>
public sealed partial class AnyPersonName : IAny<PersonName> {

    private readonly IAny<WritingScript> _script;
    private readonly bool?               _withGivenNames;

    public AnyPersonName()
        : this(Any.Enum<WritingScript>(), null) { }

    private AnyPersonName(IAny<WritingScript> script, bool? withGivenNames) {
        _script         = script;
        _withGivenNames = withGivenNames;
    }

    public AnyPersonName InScript(WritingScript script) {
        return new AnyPersonName(Any.Fixed(script), _withGivenNames);
    }

    public AnyPersonName WithGivenNames() {
        return new AnyPersonName(_script, true);
    }

    public AnyPersonName WithoutGivenNames() {
        return new AnyPersonName(_script, false);
    }

    public PersonName Generate() {
        WritingScript script = _script.Generate();
        string        family = Part(script);
        bool          given  = _withGivenNames ?? Any.Boolean().Generate();

        return new PersonName(family, given ? Part(script) : null);
    }

    private static string Part(WritingScript script) {
        List<string> words = Any.ListOf(ScriptTexts.WordIn(script)).WithCountBetween(1, 3).Generate();

        return string.Join(" ", words);
    }

}
