// Scaffolded by `any`, then completed by hand: both parts of a name must be written in one script, a rule
// between two parameters that the scaffolder cannot express, so Generate() draws the script first.

using System;

using InternationalCatalogue.Domain.Tests.Discovery;

using JustDummies;

namespace InternationalCatalogue.Domain;

public sealed partial class AnyPersonName : IAny<PersonName> {

    private IAny<WritingScript> _script;
    private IAny<string>?       _familyName;
    private IAny<string?>?      _givenNames;

    public AnyPersonName() {
        _script = Any.Enum<WritingScript>();
    }

    private AnyPersonName(AnyPersonName from) {
        _script     = from._script;
        _familyName = from._familyName;
        _givenNames = from._givenNames;
    }

    public AnyPersonName InScript(WritingScript script) {
        AnyPersonName copy = new AnyPersonName(this);

        copy._script = Any.Fixed(script);

        return copy;
    }

    public AnyPersonName WithFamilyName(string familyName) {
        AnyPersonName copy = new AnyPersonName(this);

        copy._familyName = Any.Fixed(familyName);

        return copy;
    }

    public AnyPersonName WithGivenNames(string? givenNames) {
        AnyPersonName copy = new AnyPersonName(this);

        copy._givenNames = Any.Fixed<string?>(givenNames);

        return copy;
    }

    public AnyPersonName WithoutGivenNames() {
        return WithGivenNames(null);
    }

    public PersonName Generate() {
        WritingScript script = _script.Generate();
        string        family = _familyName is null ? Scripts.FamilyNameText(script).Generate() : _familyName.Generate();
        string?       given  = _givenNames is null ? Scripts.GivenNamesText(script).OrNull().Generate() : _givenNames.Generate();

        return new PersonName(family, given);
    }

}
