namespace JustDummies;

/// <summary>
///     The grid a <see cref="decimal" /> lattice restricts values to, and the arithmetic that lands a draw on it —
///     the multiples of <c>10^-scale</c> that <c>WithScale</c> declares, or the multiples of a step that
///     <c>MultipleOf</c> declares (ADR-0015, ADR-0123).
/// </summary>
/// <remarks>
///     Every operation is exact: a grid point is built as a whole number of steps, never found by filtering, and the
///     only division taken is one whose quotient the type represents. A grid whose points the type cannot all hold
///     exactly ends where they stop: see <see cref="Reach" />.
/// </remarks>
internal abstract class DecimalGrid {

    /// <summary>Ten raised to <paramref name="power" /> as an exact <see cref="decimal" /> (<paramref name="power" /> in <c>[0, 28]</c>).</summary>
    internal static decimal Pow10(int power) {
        decimal result = 1m;
        for (int i = 0; i < power; i++) { result *= 10m; }

        return result;
    }

    /// <summary>The grid of the values expressible in <paramref name="scale" /> decimal places.</summary>
    internal static DecimalGrid OfScale(int scale) {
        return new ScaleGrid(scale);
    }

    /// <summary>
    ///     The grid of the multiples of <paramref name="step" />, strictly positive. The multiples of <c>10^-n</c> —
    ///     <c>0.01</c>, <c>1</c> — are exactly the values <c>WithScale(n)</c> describes, so such a step reuses that
    ///     grid whole, reach included.
    /// </summary>
    internal static DecimalGrid OfStep(decimal step) {
        int places = PlacesOf(step);

        return step == 1m / Pow10(places) ? new ScaleGrid(places) : new StepGrid(step, places);
    }

    /// <summary>The fewest decimal places <paramref name="value" /> is written in — <c>2</c> for <c>0.250</c>.</summary>
    internal static int PlacesOf(decimal value) {
        int places = 0;
        while (places < DecimalIntervalSpec.MaxScale && Math.Round(value, places, MidpointRounding.ToEven) != value) { places++; }

        return places;
    }

    /// <summary>The distance between two neighbouring points.</summary>
    internal abstract decimal Step { get; }

    /// <summary>
    ///     The largest magnitude the grid reaches: past it, its points stop being values the type holds exactly, and
    ///     the grid ends rather than hand back one rounded off it.
    /// </summary>
    internal abstract decimal Reach { get; }

    /// <summary>Whether <paramref name="value" /> is a point of the grid.</summary>
    internal abstract bool Contains(decimal value);

    /// <summary>The point nearest <paramref name="value" />, held within <see cref="Reach" />.</summary>
    internal abstract decimal Snap(decimal value);

    /// <summary>
    ///     The first and the last point inside <c>[minimum, maximum]</c>, or <c>false</c> when the interval holds
    ///     none.
    /// </summary>
    internal abstract bool TryBounds(decimal minimum, decimal maximum, out decimal first, out decimal last);

    /// <summary>
    ///     The number of points from <paramref name="first" /> to <paramref name="last" />, both on the grid, or
    ///     <c>null</c> when that exceeds <see cref="long.MaxValue" />.
    /// </summary>
    internal abstract long? PointCount(decimal first, decimal last);

    /// <summary>
    ///     The multiples of <c>10^-scale</c>. A coarser value is on it too, so every value the type holds at a
    ///     magnitude too large for that many places still is: this grid reaches the whole domain.
    /// </summary>
    private sealed class ScaleGrid : DecimalGrid {

        private readonly int     _scale;
        private readonly decimal _step;

        internal ScaleGrid(int scale) {
            _scale = scale;
            _step  = 1m / Pow10(scale);
        }

        internal override decimal Step  => _step;
        internal override decimal Reach => decimal.MaxValue;

        internal override bool Contains(decimal value) {
            return Math.Round(value, _scale, MidpointRounding.ToEven) == value;
        }

        internal override decimal Snap(decimal value) {
            return Math.Round(value, _scale, MidpointRounding.ToEven);
        }

        internal override bool TryBounds(decimal minimum, decimal maximum, out decimal first, out decimal last) {
            decimal ceiled = Math.Round(minimum, _scale, MidpointRounding.ToEven);
            decimal floored = Math.Round(maximum, _scale, MidpointRounding.ToEven);
            first = ceiled >= minimum ? ceiled : ceiled + _step;
            last  = floored <= maximum ? floored : floored - _step;

            return first <= last;
        }

        internal override long? PointCount(decimal first, decimal last) {
            decimal maxCountable = _step * long.MaxValue; // _step is at most 1, so this never overflows
            // The span itself can exceed the decimal range (an unconstrained WithScale spans MinValue..MaxValue). Only
            // a straddling range risks that; when either half alone already outruns the countable span there are too
            // many points, so short-circuit before forming a difference that would throw.
            if (first < 0m && last > 0m && (last > maxCountable || -first > maxCountable)) { return null; }

            decimal span = last - first;
            if (span > maxCountable) { return null; }

            return (long)(span / _step) + 1;
        }

    }

    /// <summary>
    ///     The multiples of a step <c>p × 10^-n</c> that is not a power of ten — <c>0.25</c>, <c>0.3</c>, <c>5</c>. A
    ///     point is a whole number of steps <c>k</c>, and it is a value the type holds exactly while <c>k × p</c>
    ///     fits its 96-bit mantissa: the grid reaches about <c>decimal.MaxValue × 10^-n</c>, and no further.
    /// </summary>
    private sealed class StepGrid : DecimalGrid {

        private readonly decimal _step;
        private readonly decimal _maxIndex;
        private readonly decimal _reach;

        internal StepGrid(decimal step, int places) {
            _step = step;
            decimal mantissa = step * Pow10(places); // a whole number below 2^96: the step written without its point
            decimal maxIndex = decimal.Truncate(decimal.MaxValue / mantissa);
            // The division rounds where its quotient carries more digits than the type: one step back if it rounded up.
            if (decimal.MaxValue - mantissa * (maxIndex - 1m) < mantissa) { maxIndex -= 1m; }
            _maxIndex = maxIndex;
            _reach    = maxIndex * step;
        }

        internal override decimal Step  => _step;
        internal override decimal Reach => _reach;

        internal override bool Contains(decimal value) {
            return value >= -_reach && value <= _reach && Snap(value) == value;
        }

        internal override decimal Snap(decimal value) {
            decimal within = Math.Min(Math.Max(value, -_reach), _reach);

            return Clamp(Math.Round(within / _step, MidpointRounding.ToEven)) * _step;
        }

        internal override bool TryBounds(decimal minimum, decimal maximum, out decimal first, out decimal last) {
            first = minimum;
            last  = maximum;
            decimal low  = Math.Max(minimum, -_reach);
            decimal high = Math.Min(maximum, _reach);
            if (low > high) { return false; }

            // The quotient is within the index range, so the division is safe; it may round, so each index is moved
            // the one step that rounding can leave it off.
            decimal lowIndex = Clamp(Math.Ceiling(low / _step));
            if (lowIndex * _step < low) { lowIndex += 1m; } else if (lowIndex > -_maxIndex && (lowIndex - 1m) * _step >= low) { lowIndex -= 1m; }

            decimal highIndex = Clamp(Math.Floor(high / _step));
            if (highIndex * _step > high) { highIndex -= 1m; } else if (highIndex < _maxIndex && (highIndex + 1m) * _step <= high) { highIndex += 1m; }

            first = lowIndex  * _step;
            last  = highIndex * _step;

            return lowIndex <= highIndex;
        }

        internal override long? PointCount(decimal first, decimal last) {
            decimal low  = Math.Round(first / _step, MidpointRounding.ToEven);
            decimal high = Math.Round(last  / _step, MidpointRounding.ToEven);
            // Two indices on either side of zero can lie further apart than the type holds; past long.MaxValue on
            // either side the count is out of range anyway, so it is answered before the difference is formed.
            if (low < 0m && high > 0m && (high > long.MaxValue || -low > long.MaxValue)) { return null; }

            decimal span = high - low;

            return span >= long.MaxValue ? null : (long)span + 1;
        }

        private decimal Clamp(decimal index) {
            return Math.Min(Math.Max(index, -_maxIndex), _maxIndex);
        }

    }

}
