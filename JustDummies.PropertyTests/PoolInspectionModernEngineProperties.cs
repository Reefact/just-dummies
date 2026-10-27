namespace JustDummies.PropertyTests;

/// <summary>
///     The half of <see cref="PoolInspectionEngineProperties" /> for the five generators the netstandard2.0 asset
///     cannot carry. Excluded from the .NET Framework 4.7.2 floor by the project file, because the types themselves are.
///     The 128-bit generators ride their own engine, so their anchors sit at both edges of the 128-bit domain, where a
///     sign-bit flip or a truncation to 64 bits would fold the pool.
/// </summary>
public sealed partial class PoolInspectionEngineProperties {

    private const string AfterOrEqualTo = nameof(AnyDateOnly.AfterOrEqualTo);

    [Fact(DisplayName = "An Int128 pool reports the split its constraints make, at both edges of the 128-bit domain.")]
    public void Int128Pool() {
        Verify(new Engine<Int128, AnyInt128>([-Width / 2, Int128.MinValue, Int128.MaxValue - Width],
                                             (anchor, number) => anchor + number,
                                             context => context.Int128(),
                                             (generator, pool) => generator.OneOf(pool),
                                             (generator, values) => generator.Except(values),
                                             AtLeast<Int128, AnyInt128>((generator, floor) => generator.GreaterThanOrEqualTo(floor)),
                                             Lattice<Int128, AnyInt128>((generator, step) => generator.MultipleOf(step), (value, step) => value % step == 0)));
    }

    [Fact(DisplayName = "A UInt128 pool reports the split its constraints make, at both edges of the 128-bit domain.")]
    public void UInt128Pool() {
        Verify(new Engine<UInt128, AnyUInt128>([UInt128.Zero, UInt128.MaxValue - Width],
                                               (anchor, number) => anchor + (UInt128)number,
                                               context => context.UInt128(),
                                               (generator, pool) => generator.OneOf(pool),
                                               (generator, values) => generator.Except(values),
                                               AtLeast<UInt128, AnyUInt128>((generator, floor) => generator.GreaterThanOrEqualTo(floor)),
                                               Lattice<UInt128, AnyUInt128>((generator, step) => generator.MultipleOf((UInt128)step), (value, step) => value % (UInt128)step == 0)));
    }

    [Fact(DisplayName = "A Half pool reports the split its constraints make.")]
    public void HalfPool() {
        // Every whole number up to 2048 is exact in Half, so the anchors keep the model's numbers distinct.
        Verify(new Engine<Half, AnyHalf>([(Half)(-Width / 2), (Half)1000],
                                         (anchor, number) => (Half)((float)anchor + number),
                                         context => context.Half(),
                                         (generator, pool) => generator.OneOf(pool),
                                         (generator, values) => generator.Except(values),
                                         AtLeast<Half, AnyHalf>((generator, floor) => generator.GreaterThanOrEqualTo(floor)),
                                         Step: null));
    }

    [Fact(DisplayName = "A DateOnly pool reports the split its constraints make, at both ends of the calendar.")]
    public void DateOnlyPool() {
        Verify(new Engine<DateOnly, AnyDateOnly>([DateOnly.MinValue, new DateOnly(2026, 1, 1), DateOnly.MaxValue.AddDays(-Width)],
                                                 (anchor, number) => anchor.AddDays(number),
                                                 context => context.DateOnly(),
                                                 (generator, pool) => generator.OneOf(pool),
                                                 (generator, values) => generator.Except(values),
                                                 new Floor<DateOnly, AnyDateOnly>(AfterOrEqualTo, (generator, floor) => generator.AfterOrEqualTo(floor)),
                                                 Step: null));
    }

    [Fact(DisplayName = "A TimeOnly pool reports the split its constraints make, granularity included.")]
    public void TimeOnlyPool() {
        Verify(new Engine<TimeOnly, AnyTimeOnly>([TimeOnly.MinValue, new TimeOnly(TimeOnly.MaxValue.Ticks - Width)],
                                                 (anchor, number) => new TimeOnly(anchor.Ticks + number),
                                                 context => context.TimeOnly(),
                                                 (generator, pool) => generator.OneOf(pool),
                                                 (generator, values) => generator.Except(values),
                                                 new Floor<TimeOnly, AnyTimeOnly>(AfterOrEqualTo, (generator, floor) => generator.AfterOrEqualTo(floor)),
                                                 new Step<TimeOnly, AnyTimeOnly>(WithGranularity,
                                                                                 (generator, step) => generator.WithGranularity(TimeSpan.FromTicks(step)),
                                                                                 (value, step) => value.Ticks % step == 0)));
    }

}
