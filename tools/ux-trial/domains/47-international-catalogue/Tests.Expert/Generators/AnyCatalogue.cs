using System;

using JustDummies;

namespace InternationalCatalogue.Domain;

/// <summary>Hand-written: a catalogue named in any script, with no control character in it.</summary>
public sealed partial class AnyCatalogue : IAny<Catalogue> {

    public Catalogue Generate() {
        return new Catalogue(Any.Enum<WritingScript>().Then(script => ScriptTexts.TextIn(script)).Generate());
    }

}
