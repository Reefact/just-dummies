#region Usings declarations

using FsCheck;
using FsCheck.Fluent;

using JetBrains.Annotations;

#endregion

namespace JustDummies.PropertyTests;

/// <summary>
///     Property-based tests for <see cref="AnyBoolean" />'s pin — <c>True()</c>, <c>False()</c> and
///     <c>DifferentFrom</c>. The domain has exactly two values, so an example test could enumerate it whole; these
///     are kept as properties, mirroring <see cref="StringShapeProperties" />'s treatment of casing (also a
///     two-valued choice), so the redeclaration and the exclusion algebra are quantified the same way everywhere
///     they occur rather than singled out here because the count happens to be small.
/// </summary>
[TestSubject(typeof(AnyBoolean))]
public sealed class BooleanProperties {

    #region Statics members declarations

    /// <summary>Applies one of the two pins, so a property can quantify over the pin itself.</summary>
    private static AnyBoolean ApplyPin(AnyBoolean generator, bool value) {
        return value ? generator.True() : generator.False();
    }

    #endregion

    [Fact(DisplayName = "A pin conflicts unless it repeats the first, whichever two are combined.")]
    public void APinConflictsUnlessItRepeatsTheFirst() {
        Gen<(bool First, bool Second)> cases =
            from first in Gen.Elements(false, true)
            from second in Gen.Elements(false, true)
            select (First: first, Second: second);

        Prop.ForAll(cases.ToArbitrary(),
                    // Repeating the same pin is not a contradiction — the value asked for is the one already in
                    // force — so it is a no-op; the opposite pin contradicts it.
                    testCase => testCase.First == testCase.Second
                                    ? Expect.EveryDraw(ApplyPin(ApplyPin(Any.Boolean(), testCase.First), testCase.Second),
                                                       value => value == testCase.First)
                                    : Expect.Throws<ConflictingAnyConstraintException>(
                                        () => ApplyPin(ApplyPin(Any.Boolean(), testCase.First), testCase.Second)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "DifferentFrom pins to the opposite value, whichever value is supplied.")]
    public void DifferentFromPinsToTheOpposite() {
        Prop.ForAll(Gen.Elements(false, true).ToArbitrary(),
                    value => Expect.EveryDraw(Any.Boolean().DifferentFrom(value), drawn => drawn != value))
            .QuickCheckThrowOnFailure();
    }

}
