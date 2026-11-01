#region Usings declarations

using System.Diagnostics.CodeAnalysis;
using JustDummies.Diagnostics;

using FsCheck;
using FsCheck.Fluent;

using JetBrains.Annotations;

#endregion

namespace JustDummies.PropertyTests;

/// <summary>
///     Property-based tests for the pool inspection (ADR-0067) on every interval engine behind it, not only the one
///     the example suite and <see cref="PoolInspectionProperties" /> reach through <see cref="Any.String" />,
///     <see cref="Any.Int32" /> and a handful of others. Each engine projects the caller's value into its own ordinal
///     space and back, so a report can be right for <see cref="int" /> and wrong for <see cref="sbyte" />,
///     <see cref="ulong" /> or <see cref="double" /> — a sign flipped, an ordinal never projected back, a constraint
///     blamed that never refused anything.
/// </summary>
/// <remarks>
///     <para>
///         One model drives every engine: a pool of small whole numbers, a floor, an exclusion list and a lattice step,
///         all drawn, and a type-specific <b>anchor</b> the numbers are offset from. The anchors sit at zero and at the
///         edges of each domain, where an ordinal mapping folds if it is going to. The expected split is decided from
///         the model alone — which numbers a floor, an exclusion or a step refuses — and the report is held to it: the
///         survivors and the rejections in the order supplied, every rejection naming exactly the constraints that
///         refuse it, every draw a survivor, and a distinct collection sized to the survivors exactly.
///     </para>
///     <para>
///         The constraints are declared before the pool as often as after it, so the verdict is checked not to depend
///         on the order the chain was written in. When the model leaves no survivor, the declaration itself must be
///         refused: an empty pool is a conflict, never a generator that cannot draw.
///     </para>
///     <para>
///         The generators the netstandard2.0 asset cannot carry are checked in the other half of this class, which the
///         project file keeps off the .NET Framework 4.7.2 floor.
///     </para>
/// </remarks>
[TestSubject(typeof(IPoolInspection<>))]
public sealed partial class PoolInspectionEngineProperties {

    #region Statics members declarations

    /// <summary>
    ///     The largest model number. Small on purpose: a dozen values make a floor, an exclusion and a step collide
    ///     with the pool and with each other often, which is where a report can go wrong.
    /// </summary>
    private const int Width = 11;

    private const string GreaterThanOrEqualTo = nameof(AnyInt64.GreaterThanOrEqualTo);
    private const string MultipleOf           = nameof(AnyInt64.MultipleOf);
    private const string WithGranularity      = nameof(AnyTimeSpan.WithGranularity);

    private static Gen<PoolSplit> Splits() {
        return from pool in Gen.NonEmptyListOf(Gen.Choose(0, Width))
               from floor in Gen.Choose(0, Width)
               from excluded in Gen.ListOf(Gen.Choose(0, Width))
               from step in Gen.Choose(1, 3)
               from anchor in Gen.Choose(0, 2)
               from poolFirst in Gen.Elements(false, true)
               from seed in Generators.Seed()
               select new PoolSplit(pool.Distinct().ToArray(), floor, excluded.Distinct().Take(4).ToArray(), step, anchor, poolFirst, seed);
    }

    private static void Verify<T, TAny>(Engine<T, TAny> engine) where TAny : IAny<T>, IPoolInspection<T> {
        Prop.ForAll(Splits().ToArbitrary(), split => ReportsTheSplit(engine, split))
            .QuickCheckThrowOnFailure();
    }

    private static bool ReportsTheSplit<T, TAny>(Engine<T, TAny> engine, PoolSplit split) where TAny : IAny<T>, IPoolInspection<T> {
        Spelled<T, TAny> spelled   = new(engine, split);
        T[]              survivors = split.Pool.Where(number => spelled.RefusedBy(number).Count == 0).Select(spelled.Of).ToArray();

        TAny generator;
        try { generator = spelled.Declare(); } catch (ConflictingAnyConstraintException) { return survivors.Length == 0 && !engine.RefusesAtTheDraw; }

        // An engine that refuses an empty pool at the draw still owes a report saying why: every supplied value
        // rejected, each under the constraints that refuse it — and then the refusal itself.
        if (survivors.Length == 0) {
            return engine.RefusesAtTheDraw
                && ReportsTheSurvivorsAndRejections(generator, spelled, survivors)
                && Expect.Throws<ConflictingAnyConstraintException>(() => generator.Generate());
        }

        return ReportsTheSurvivorsAndRejections(generator, spelled, survivors)
            && Expect.EveryDraw(generator, value => survivors.Contains(value))
            // The advertised cardinality is the survivor count: a distinct collection that large draws, one larger
            // is refused before any element is drawn.
            && Expect.EveryDraw(Any.SetOf(generator).WithCount(survivors.Length), set => set.Count == survivors.Length, 2)
            && Expect.Throws<ConflictingAnyConstraintException>(() => Any.SetOf(generator).WithCount(survivors.Length + 1).Generate());
    }

    /// <summary>
    ///     The survivors and the rejected values in the order supplied, every rejection naming exactly the constraints
    ///     the oracle says refuse it.
    /// </summary>
    private static bool ReportsTheSurvivorsAndRejections<T, TAny>(IPoolInspection<T> inspection, Spelled<T, TAny> spelled, T[] survivors) where TAny : IAny<T>, IPoolInspection<T> {
        int[]                           refused    = spelled.Pool.Where(number => spelled.RefusedBy(number).Count > 0).ToArray();
        IReadOnlyList<PoolRejection<T>> rejections = inspection.GetRejections();

        return inspection.IsPooled
            && inspection.GetSurvivors().SequenceEqual(survivors)
            && rejections.Select(rejection => rejection.Value).SequenceEqual(refused.Select(spelled.Of))
            && rejections.Zip(refused, (rejection, number) => rejection.RejectedBy.Select(constraint => constraint.Name)
                                                                       .OrderBy(name => name, StringComparer.Ordinal)
                                                                       .SequenceEqual(spelled.RefusedBy(number), StringComparer.Ordinal))
                         .All(named => named);
    }

    private static Step<T, TAny> Lattice<T, TAny>(Func<TAny, int, TAny> apply, Func<T, int, bool> holds) {
        return new Step<T, TAny>(MultipleOf, apply, holds);
    }

    private static Floor<T, TAny> AtLeast<T, TAny>(Func<TAny, T, TAny> apply) {
        return new Floor<T, TAny>(GreaterThanOrEqualTo, apply);
    }

    #endregion

    [Fact(DisplayName = "A byte pool reports the split its constraints make.")]
    public void BytePool() {
        Verify(new Engine<byte, AnyByte>([0, byte.MaxValue - Width],
                                         (anchor, number) => (byte)(anchor + number),
                                         context => context.Byte(),
                                         (generator, pool) => generator.OneOf(pool),
                                         (generator, values) => generator.Except(values),
                                         AtLeast<byte, AnyByte>((generator, floor) => generator.GreaterThanOrEqualTo(floor)),
                                         Lattice<byte, AnyByte>((generator, step) => generator.MultipleOf((byte)step), (value, step) => value % step == 0)));
    }

    [Fact(DisplayName = "An sbyte pool reports the split its constraints make, on both sides of zero.")]
    public void SBytePool() {
        Verify(new Engine<sbyte, AnySByte>([-Width / 2, sbyte.MinValue, sbyte.MaxValue - Width],
                                           (anchor, number) => (sbyte)(anchor + number),
                                           context => context.SByte(),
                                           (generator, pool) => generator.OneOf(pool),
                                           (generator, values) => generator.Except(values),
                                           AtLeast<sbyte, AnySByte>((generator, floor) => generator.GreaterThanOrEqualTo(floor)),
                                           Lattice<sbyte, AnySByte>((generator, step) => generator.MultipleOf((sbyte)step), (value, step) => value % step == 0)));
    }

    [Fact(DisplayName = "An Int16 pool reports the split its constraints make, on both sides of zero.")]
    public void Int16Pool() {
        Verify(new Engine<short, AnyInt16>([-Width / 2, short.MinValue, short.MaxValue - Width],
                                           (anchor, number) => (short)(anchor + number),
                                           context => context.Int16(),
                                           (generator, pool) => generator.OneOf(pool),
                                           (generator, values) => generator.Except(values),
                                           AtLeast<short, AnyInt16>((generator, floor) => generator.GreaterThanOrEqualTo(floor)),
                                           Lattice<short, AnyInt16>((generator, step) => generator.MultipleOf((short)step), (value, step) => value % step == 0)));
    }

    [Fact(DisplayName = "A UInt16 pool reports the split its constraints make.")]
    public void UInt16Pool() {
        Verify(new Engine<ushort, AnyUInt16>([0, ushort.MaxValue - Width],
                                             (anchor, number) => (ushort)(anchor + number),
                                             context => context.UInt16(),
                                             (generator, pool) => generator.OneOf(pool),
                                             (generator, values) => generator.Except(values),
                                             AtLeast<ushort, AnyUInt16>((generator, floor) => generator.GreaterThanOrEqualTo(floor)),
                                             Lattice<ushort, AnyUInt16>((generator, step) => generator.MultipleOf((ushort)step), (value, step) => value % step == 0)));
    }

    [Fact(DisplayName = "A UInt32 pool reports the split its constraints make.")]
    public void UInt32Pool() {
        Verify(new Engine<uint, AnyUInt32>([0U, uint.MaxValue - Width],
                                           (anchor, number) => anchor + (uint)number,
                                           context => context.UInt32(),
                                           (generator, pool) => generator.OneOf(pool),
                                           (generator, values) => generator.Except(values),
                                           AtLeast<uint, AnyUInt32>((generator, floor) => generator.GreaterThanOrEqualTo(floor)),
                                           Lattice<uint, AnyUInt32>((generator, step) => generator.MultipleOf((uint)step), (value, step) => value % (uint)step == 0)));
    }

    [Fact(DisplayName = "An Int64 pool reports the split its constraints make, on both sides of zero.")]
    public void Int64Pool() {
        Verify(new Engine<long, AnyInt64>([-Width / 2, long.MinValue, long.MaxValue - Width],
                                          (anchor, number) => anchor + number,
                                          context => context.Int64(),
                                          (generator, pool) => generator.OneOf(pool),
                                          (generator, values) => generator.Except(values),
                                          AtLeast<long, AnyInt64>((generator, floor) => generator.GreaterThanOrEqualTo(floor)),
                                          Lattice<long, AnyInt64>((generator, step) => generator.MultipleOf(step), (value, step) => value % step == 0)));
    }

    [Fact(DisplayName = "A UInt64 pool reports the split its constraints make.")]
    public void UInt64Pool() {
        Verify(new Engine<ulong, AnyUInt64>([0UL, ulong.MaxValue - Width],
                                            (anchor, number) => anchor + (ulong)number,
                                            context => context.UInt64(),
                                            (generator, pool) => generator.OneOf(pool),
                                            (generator, values) => generator.Except(values),
                                            AtLeast<ulong, AnyUInt64>((generator, floor) => generator.GreaterThanOrEqualTo(floor)),
                                            Lattice<ulong, AnyUInt64>((generator, step) => generator.MultipleOf((ulong)step), (value, step) => value % (ulong)step == 0)));
    }

    [Fact(DisplayName = "A TimeSpan pool reports the split its constraints make, granularity included.")]
    public void TimeSpanPool() {
        Verify(new Engine<TimeSpan, AnyTimeSpan>([TimeSpan.FromTicks(-Width / 2), TimeSpan.MinValue, TimeSpan.MaxValue - TimeSpan.FromTicks(Width)],
                                                 (anchor, number) => anchor + TimeSpan.FromTicks(number),
                                                 context => context.TimeSpan(),
                                                 (generator, pool) => generator.OneOf(pool),
                                                 (generator, values) => generator.Except(values),
                                                 AtLeast<TimeSpan, AnyTimeSpan>((generator, floor) => generator.GreaterThanOrEqualTo(floor)),
                                                 new Step<TimeSpan, AnyTimeSpan>(WithGranularity,
                                                                                 (generator, step) => generator.WithGranularity(TimeSpan.FromTicks(step)),
                                                                                 (value, step) => value.Ticks % step == 0)));
    }

    [Fact(DisplayName = "A Double pool reports the split its constraints make.")]
    [SuppressMessage(JustDummiesRule.JD036.Category, JustDummiesRule.JD036.Id, Justification = SuppressionJustification.JD036.MagnitudeIsNotWhatThisPins)]
    public void DoublePool() {
        Verify(new Engine<double, AnyDouble>([-Width / 2, 1_000_000d],
                                             (anchor, number) => anchor + number,
                                             context => context.Double(),
                                             (generator, pool) => generator.OneOf(pool),
                                             (generator, values) => generator.Except(values),
                                             AtLeast<double, AnyDouble>((generator, floor) => generator.GreaterThanOrEqualTo(floor)),
                                             Step: null));
    }

    [Fact(DisplayName = "A Single pool reports the split its constraints make.")]
    [SuppressMessage(JustDummiesRule.JD036.Category, JustDummiesRule.JD036.Id, Justification = SuppressionJustification.JD036.MagnitudeIsNotWhatThisPins)]
    public void SinglePool() {
        Verify(new Engine<float, AnySingle>([-Width / 2, 1_000_000f],
                                            (anchor, number) => anchor + number,
                                            context => context.Single(),
                                            (generator, pool) => generator.OneOf(pool),
                                            (generator, values) => generator.Except(values),
                                            AtLeast<float, AnySingle>((generator, floor) => generator.GreaterThanOrEqualTo(floor)),
                                            Step: null));
    }

    [Fact(DisplayName = "An enum pool reports the members its exclusions remove.")]
    public void EnumPool() {
        Verify(new Engine<Month, AnyEnum<Month>>([Month.January],
                                                 (anchor, number) => anchor + number,
                                                 context => context.Enum<Month>(),
                                                 (generator, pool) => generator.OneOf(pool),
                                                 (generator, values) => generator.Except(values),
                                                 Floor: null,
                                                 Step: null,
                                                 RefusesAtTheDraw: true));
    }

    #region Nested types declarations

    /// <summary>Twelve members, one per model number: the enum engine has no floor, so the pool is the whole story.</summary>
    private enum Month { January, February, March, April, May, June, July, August, September, October, November, December }

    /// <summary>
    ///     One drawn case, in model numbers from <c>0</c> to <see cref="Width" />: <see cref="Floor" /> is declared
    ///     only above zero and <see cref="Step" /> only above one, so a case also covers the chain without them.
    /// </summary>
    private sealed record PoolSplit(int[] Pool, int Floor, int[] Excluded, int Step, int Anchor, bool PoolFirst, int Seed) {

        // Spelled out, because a record prints an array as its type name and FsCheck reports a counter-example by it.
        public override string ToString() {
            return $"Pool = [{string.Join(", ", Pool)}], Floor = {Floor}, Excluded = [{string.Join(", ", Excluded)}], Step = {Step}, Anchor = {Anchor}, PoolFirst = {PoolFirst}, Seed = {Seed}";
        }

    }

    /// <summary>
    ///     How one generator type spells the model: its anchors, its offset, and the constraints it declares.
    ///     <see cref="RefusesAtTheDraw" /> marks the engine that refuses an emptied pool at the draw rather than at
    ///     declaration, which <see cref="AnyEnum{TEnum}" /> does on purpose: <c>AllowingCombinations()</c> can widen
    ///     its universe later in the chain, so a verdict read mid-chain would depend on the order it was written in.
    /// </summary>
    private sealed record Engine<T, TAny>(T[] Anchors,
                                          Func<T, int, T> Offset,
                                          Func<AnyContext, TAny> Root,
                                          Func<TAny, T[], TAny> OneOf,
                                          Func<TAny, T[], TAny> Except,
                                          Floor<T, TAny>? Floor,
                                          Step<T, TAny>? Step,
                                          bool RefusesAtTheDraw = false);

    /// <summary>One drawn case spelled in one generator type: the values it maps to, the chain, and the oracle.</summary>
    private sealed class Spelled<T, TAny> where TAny : IAny<T>, IPoolInspection<T> {

        private readonly Engine<T, TAny> _engine;
        private readonly PoolSplit       _split;
        private readonly T               _anchor;
        private readonly bool            _floored;
        private readonly bool            _stepped;
        private readonly bool            _excluding;

        public Spelled(Engine<T, TAny> engine, PoolSplit split) {
            _engine    = engine;
            _split     = split;
            _anchor    = engine.Anchors[split.Anchor % engine.Anchors.Length];
            _floored   = engine.Floor is not null && split.Floor > 0;
            _stepped   = engine.Step is not null && split.Step > 1;
            _excluding = split.Excluded.Length > 0;
        }

        public int[] Pool => _split.Pool;

        public T Of(int number) {
            return _engine.Offset(_anchor, number);
        }

        /// <summary>
        ///     The names of the constraints refusing a model number, sorted. Decided from the model alone, so it cannot
        ///     share a mistake with the engine it judges.
        /// </summary>
        public List<string> RefusedBy(int number) {
            List<string> names = [];
            if (_floored && number < _split.Floor) { names.Add(_engine.Floor!.Name); }
            if (_excluding && _split.Excluded.Contains(number)) { names.Add(nameof(AnyInt64.Except)); }
            if (_stepped && !_engine.Step!.Holds(Of(number), _split.Step)) { names.Add(_engine.Step.Name); }
            names.Sort(StringComparer.Ordinal);

            return names;
        }

        /// <summary>The chain, with the pool declared before or after the constraints as the case says.</summary>
        public TAny Declare() {
            TAny generator = _engine.Root(Any.WithSeed(_split.Seed));
            if (_split.PoolFirst) { generator = _engine.OneOf(generator, _split.Pool.Select(Of).ToArray()); }
            if (_floored) { generator = _engine.Floor!.Apply(generator, Of(_split.Floor)); }
            if (_excluding) { generator = _engine.Except(generator, _split.Excluded.Select(Of).ToArray()); }
            if (_stepped) { generator = _engine.Step!.Apply(generator, _split.Step); }
            if (!_split.PoolFirst) { generator = _engine.OneOf(generator, _split.Pool.Select(Of).ToArray()); }

            return generator;
        }

    }

    /// <summary>The inclusive lower bound, under the name the generator declares it with.</summary>
    private sealed record Floor<T, TAny>(string Name, Func<TAny, T, TAny> Apply);

    /// <summary>The lattice constraint, under the name the generator declares it with, and the test a value must pass.</summary>
    private sealed record Step<T, TAny>(string Name, Func<TAny, int, TAny> Apply, Func<T, int, bool> Holds);

    #endregion

}
