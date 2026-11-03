// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Catalogue --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace InternationalCatalogue.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Catalogue" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCatalogue : IAny<Catalogue> {

    private IAny<string> _name;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCatalogue()
        : this(name: AnyValidName()) { }

    private static IAny<string> AnyValidName() {
        return Any.Enum<WritingScript>().Then(script => Generated.Words(script, 6));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCatalogue(IAny<string> name) {
        _name       = name;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCatalogue(AnyCatalogue from) {
        _name       = from._name;
    }

    /// <summary>Pins <c>name</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCatalogue WithName(string value) {
        AnyCatalogue copy = new AnyCatalogue(this);

        copy._name = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Catalogue" />.</summary>
    public Catalogue Generate() {
        return new Catalogue(_name.Generate());
    }

}
