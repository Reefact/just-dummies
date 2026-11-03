using System;

using JustDummies;

namespace InternationalCatalogue.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PersonName" /> values. The family and given names always share one
///     script, drawn from the seven supported ones unless <see cref="InScript" /> pins it.
/// </summary>
public sealed partial class AnyPersonName : IAny<PersonName> {

    private IAny<WritingScript>                _script;
    private Func<WritingScript, IAny<string>>  _familyName;
    private Func<WritingScript, IAny<string?>> _givenNames;

    public AnyPersonName()
        : this(Any.Enum<WritingScript>(),
               script => Generated.Words(script, 2),
               script => Generated.Words(script, 2).OrNull()) { }

    private AnyPersonName(IAny<WritingScript> script, Func<WritingScript, IAny<string>> familyName, Func<WritingScript, IAny<string?>> givenNames) {
        _script     = script;
        _familyName = familyName;
        _givenNames = givenNames;
    }

    private AnyPersonName(AnyPersonName from)
        : this(from._script, from._familyName, from._givenNames) { }

    public AnyPersonName InScript(WritingScript script) {
        AnyPersonName copy = new AnyPersonName(this);

        copy._script = Any.Fixed(script);

        return copy;
    }

    public AnyPersonName WithFamilyName(string value) {
        AnyPersonName copy = new AnyPersonName(this);

        copy._familyName = _ => Any.Fixed(value);

        return copy;
    }

    public AnyPersonName WithGivenNames(string? value) {
        AnyPersonName copy = new AnyPersonName(this);

        copy._givenNames = _ => Any.Fixed(value);

        return copy;
    }

    public AnyPersonName WithoutGivenNames() {
        return WithGivenNames(null);
    }

    public PersonName Generate() {
        WritingScript script = _script.Generate();

        return new PersonName(_familyName(script).Generate(), _givenNames(script).Generate());
    }

}
