#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     The named cases of <see cref="AnyEnum{TEnum}.AllowingCombinations" />: the exact universe a given enum shape
///     yields, the conflict wording, and the boundary between the declared-members default and the opt-in. The
///     universal half — that every draw belongs to the universe whatever the constraints — lives in
///     <c>JustDummies.PropertyTests</c>.
/// </summary>
public sealed class AnyEnumCombinationTests {

    // Enough draws that an eight-value universe is exhausted with overwhelming probability, while a missing value is
    // not attributed to bad luck. The assertions below are on the SET observed, so they are reachability claims.
    private const int SampleCount = 2000;

    [Flags]
    private enum Permissions {

        None  = 0,
        Read  = 1,
        Write = 2,
        Exec  = 4

    }

    // No zero member: the empty combination is not a value this enum defines.
    [Flags]
    private enum Sides {

        Left  = 1,
        Right = 2

    }

    // A declared composite: ReadWrite is already Read | Write, so it must not widen the universe.
    [Flags]
    private enum Access {

        Read      = 1,
        Write     = 2,
        ReadWrite = 3

    }

    private enum OrderStatus {

        Draft,
        Validated,
        Cancelled

    }

    [Fact(DisplayName = "A [Flags] enum still draws only declared members until combinations are allowed.")]
    public void FlagsEnumDrawsDeclaredMembersByDefault() {
        HashSet<Permissions> seen = [];
        for (int i = 0; i < SampleCount; i++) { seen.Add(Any.Enum<Permissions>().Generate()); }

        // The contract the opt-in exists to leave untouched: the default never depends on the [Flags] attribute, so a
        // combination is unreachable until the test asks for one.
        Check.That(seen).IsOnlyMadeOf(Permissions.None, Permissions.Read, Permissions.Write, Permissions.Exec);
    }

    [Fact(DisplayName = "AllowingCombinations: the universe is every combination, and the declared zero value.")]
    public void CombinationsCoverTheWholeUniverse() {
        HashSet<Permissions> seen = [];
        for (int i = 0; i < SampleCount; i++) { seen.Add(Any.Enum<Permissions>().AllowingCombinations().Generate()); }

        Check.That(seen.Count).IsEqualTo(8);
        for (int bits = 0; bits <= 7; bits++) { Check.That(seen).Contains((Permissions)bits); }
    }

    [Fact(DisplayName = "AllowingCombinations: an enum declaring no zero member never yields the empty combination.")]
    public void CombinationsOmitZeroWhenItIsNotDeclared() {
        HashSet<Sides> seen = [];
        for (int i = 0; i < SampleCount; i++) { seen.Add(Any.Enum<Sides>().AllowingCombinations().Generate()); }

        Check.That(seen).IsOnlyMadeOf(Sides.Left, Sides.Right, Sides.Left | Sides.Right);
    }

    [Fact(DisplayName = "AllowingCombinations: a declared composite adds nothing — it already is a combination.")]
    public void DeclaredCompositeDoesNotWidenTheUniverse() {
        HashSet<Access> seen = [];
        for (int i = 0; i < SampleCount; i++) { seen.Add(Any.Enum<Access>().AllowingCombinations().Generate()); }

        Check.That(seen).IsOnlyMadeOf(Access.Read, Access.Write, Access.ReadWrite);
    }

    [Fact(DisplayName = "AllowingCombinations: applying it to an enum that is not [Flags] conflicts, naming why.")]
    public void CombinationsRequireAFlagsEnum() {
        Check.ThatCode(() => Any.Enum<OrderStatus>().AllowingCombinations())
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("AllowingCombinations()", "OrderStatus", "[Flags]");
    }

    [Fact(DisplayName = "AllowingCombinations: applying it twice is a no-op, not a conflict.")]
    public void CombinationsAreIdempotent() {
        AnyEnum<Permissions> generator = Any.Enum<Permissions>().AllowingCombinations().AllowingCombinations();

        HashSet<Permissions> seen = [];
        for (int i = 0; i < SampleCount; i++) { seen.Add(generator.Generate()); }

        // Idempotent, not cumulative: the universe is the same eight values a single application yields.
        Check.That(seen.Count).IsEqualTo(8);
    }

    [Fact(DisplayName = "OneOf: a combination is accepted with no opt-in — writing one is asking for it.")]
    public void OneOfAcceptsACombinationWithoutTheOptIn() {
        AnyEnum<Permissions> generator = Any.Enum<Permissions>().OneOf(Permissions.Read | Permissions.Write, Permissions.Exec);

        HashSet<Permissions> seen = [];
        for (int i = 0; i < SampleCount; i++) { seen.Add(generator.Generate()); }

        // The allow-list IS the pool, so nothing here draws over the combination universe: the caller named two exact
        // values and gets those two. AllowingCombinations() answers the other question — what a PLAIN draw ranges
        // over — and the default it guards is untouched, as FlagsEnumDrawsDeclaredMembersByDefault still shows.
        Check.That(seen).IsOnlyMadeOf(Permissions.Read | Permissions.Write, Permissions.Exec);
    }

    [Fact(DisplayName = "OneOf: the opt-in beside it changes nothing, in either order.")]
    public void OneOfAndTheOptInAgreeInEitherOrder() {
        Permissions combination = Permissions.Read | Permissions.Write;

        Check.That(Any.Enum<Permissions>().AllowingCombinations().OneOf(combination).Generate()).IsEqualTo(combination);
        Check.That(Any.Enum<Permissions>().OneOf(combination).AllowingCombinations().Generate()).IsEqualTo(combination);
        Check.That(Any.Enum<Permissions>().OneOf(combination).Generate()).IsEqualTo(combination);
    }

    [Fact(DisplayName = "OneOf: a value no combination of declared members produces is refused, naming both.")]
    public void OneOfRefusesAValueTheTypeDoesNotDefine() {
        // Bit 3 is declared nowhere, so 8 is neither a member nor an OR of members — the case the acceptance above
        // must not swallow, since no constraint the caller could add would ever make it drawable.
        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.Enum<Permissions>().OneOf((Permissions)8));

        Check.That(error.Message).Contains("neither a declared member of Permissions nor a combination of its declared members");
        // The advice is gone with the refusal it belonged to: AllowingCombinations() cannot rescue this value.
        Check.That(error.Message).Not.Contains("AllowingCombinations()");
    }

    [Fact(DisplayName = "OneOf: the empty combination is refused where the enum declares no zero member.")]
    public void OneOfRefusesZeroWhereItIsNotDeclared() {
        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.Enum<Sides>().OneOf(default(Sides)));

        Check.That(error.Message).Contains("neither a declared member of Sides nor a combination of its declared members");
    }

    [Fact(DisplayName = "OneOf: a non-[Flags] enum still admits only its declared members.")]
    public void OneOfRefusesACompositeOnANonFlagsEnum() {
        // Nothing about the acceptance leaks to the enums that never asked to be combined.
        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.Enum<OrderStatus>().OneOf((OrderStatus)7));

        Check.That(error.Message).Contains("is not a declared member of OrderStatus");
    }

    [Fact(DisplayName = "OneOf: a combination is accepted on an enum too wide for the universe to be enumerated.")]
    public void OneOfAcceptsACombinationBeyondTheCombinableCeiling() {
        // What deciding membership arithmetically — rather than by searching the universe — buys: twenty-one non-zero
        // members put AllowingCombinations() past its ceiling, and OneOf never has to go there.
        AnyEnum<WideBits> generator = Any.Enum<WideBits>().OneOf(WideBits.B00 | WideBits.B20);

        Check.That(generator.Generate()).IsEqualTo(WideBits.B00 | WideBits.B20);
    }

    [Fact(DisplayName = "Except: exclusions compare by equality, so a combination carrying an excluded bit survives.")]
    public void ExclusionsCompareByEquality() {
        AnyEnum<Permissions> generator = Any.Enum<Permissions>().AllowingCombinations().Except(Permissions.Read);

        HashSet<Permissions> seen = [];
        for (int i = 0; i < SampleCount; i++) { seen.Add(generator.Generate()); }

        // Read itself is gone; Read | Write is a different value and stays drawable — Except is not a bit mask.
        Check.That(seen).Not.Contains(Permissions.Read);
        Check.That(seen).Contains(Permissions.Read | Permissions.Write);
        Check.That(seen.Count).IsEqualTo(7);
    }

    [Fact(DisplayName = "AllowingCombinations: the widened universe feeds the distinct-collection cardinality check.")]
    public void CombinationsWidenTheCardinalityHint() {
        // Eight distinct values exist, so eight are obtainable and nine conflict eagerly — the same check that caps a
        // declared-members draw at four.
        HashSet<Permissions> eight = Any.SetOf(Any.Enum<Permissions>().AllowingCombinations()).WithCount(8).Generate();
        Check.That(eight.Count).IsEqualTo(8);

        Check.ThatCode(() => Any.SetOf(Any.Enum<Permissions>().AllowingCombinations()).WithCount(9).Generate())
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("9");

        Check.ThatCode(() => Any.SetOf(Any.Enum<Permissions>()).WithCount(5).Generate())
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(capped => capped.Message).Contains("5");
    }

    [Fact(DisplayName = "AllowingCombinations: excluding the whole universe conflicts, naming both sides.")]
    public void ExcludingTheWholeUniverseConflicts() {
        Permissions[] everything = Enumerable.Range(0, 8).Select(bits => (Permissions)bits).ToArray();

        Check.ThatCode(() => Any.Enum<Permissions>().AllowingCombinations().Except(everything).Generate())
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("Except(", "Permissions");
    }

    [Fact(DisplayName = "AllowingCombinations: it widens the universe wherever in the chain it was declared.")]
    public void AllowingCombinationsIsHonouredWhereverItWasDeclared() {
        // Excluding both declared members of a two-member flags enum empties the DECLARED universe, but the
        // combination Left | Write is still there to draw once combinations are allowed. Declared after the
        // exclusion it used to arrive too late, so the same two constraints were refused in one order and honoured
        // in the other.
        Check.That(Any.Enum<Sides>().Except(Sides.Left, Sides.Right).AllowingCombinations().Generate()).IsEqualTo(Sides.Left | Sides.Right);
        Check.That(Any.Enum<Sides>().AllowingCombinations().Except(Sides.Left, Sides.Right).Generate()).IsEqualTo(Sides.Left | Sides.Right);
    }

    [Fact(DisplayName = "Excluding every declared member is still refused when no combination is allowed to rescue it.")]
    public void ExcludingEveryDeclaredMemberWithoutCombinationsIsStillRefused() {
        // The other side of the same guard: giving the widening constraint its chance must not relax the refusal
        // when the caller never wrote it. The message is the one it always was.
        Check.ThatCode(() => Any.Enum<Sides>().Except(Sides.Left, Sides.Right).Generate())
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply Except(Left, Right) because it forbids every declared Sides member.");
    }

    [Fact(DisplayName = "AllowingCombinations: an enum with too many members to enumerate is refused, naming the ceiling.")]
    public void TooManyMembersIsRefused() {
        Check.ThatCode(() => Any.Enum<WideBits>().AllowingCombinations())
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("AllowingCombinations()", "21", "20", "OneOf");
    }

    [Fact(DisplayName = "AllowingCombinations: a seeded context replays the same combinations.")]
    public void CombinationsReplayUnderASeed() {
        List<Permissions> Batch(int seed) {
            AnyContext          context   = Any.WithSeed(seed);
            AnyEnum<Permissions> generator = context.Enum<Permissions>().AllowingCombinations();

            return Enumerable.Range(0, 20).Select(_ => generator.Generate()).ToList();
        }

        Check.That(Batch(4242)).ContainsExactly(Batch(4242));
    }

    // Twenty-one single-bit members: one past the ceiling AllowingCombinations() will enumerate.
    [Flags]
    private enum WideBits {

        B00 = 1 << 0, B01 = 1 << 1, B02 = 1 << 2, B03 = 1 << 3, B04 = 1 << 4, B05 = 1 << 5, B06 = 1 << 6,
        B07 = 1 << 7, B08 = 1 << 8, B09 = 1 << 9, B10 = 1 << 10, B11 = 1 << 11, B12 = 1 << 12, B13 = 1 << 13,
        B14 = 1 << 14, B15 = 1 << 15, B16 = 1 << 16, B17 = 1 << 17, B18 = 1 << 18, B19 = 1 << 19, B20 = 1 << 20

    }

}
