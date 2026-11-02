// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Surface --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace RealEstate.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Surface" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySurface : IAny<Surface> {

    // A surface is measured to the hundredth of a square metre: it is drawn as a whole number of hundredths
    // (1 to 1 000 000 000, i.e. up to Surface.MaxSquareMeters) and divided by 100.
    private IAny<int> _hundredths;

    private AnyInt32 _hundredthsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySurface()
        : this(hundredths: AnyValidHundredths()) { }

    private static AnyInt32 AnyValidHundredths() {
        return Any.Int32().Between(1, (int)(Surface.MaxSquareMeters * 100d));
    }

    private AnySurface(AnyInt32 hundredths) {
        _hundredths       = hundredths;
        _hundredthsRecipe = hundredths;
    }

    private AnySurface(AnySurface from) {
        _hundredths       = from._hundredths;
        _hundredthsRecipe = from._hundredthsRecipe;
    }

    /// <summary>Pins the surface to <paramref name="squareMeters" />, which must be a whole number of hundredths.</summary>
    public AnySurface WithSquareMeters(double squareMeters) {
        AnySurface copy = new AnySurface(this);

        copy._hundredths = Any.Fixed((int)Math.Round(squareMeters * 100d));

        return copy;
    }

    /// <summary>Refines the whole number of hundredths of a square metre the surface is drawn from.</summary>
    public AnySurface WithHundredths(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySurface copy = new AnySurface(this);

        copy._hundredthsRecipe = configure(_hundredthsRecipe);
        copy._hundredths       = copy._hundredthsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Surface" />.</summary>
    public Surface Generate() {
        return new Surface(_hundredths.Generate() / 100d);
    }

}
