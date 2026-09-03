#region Usings declarations

using System.Diagnostics.CodeAnalysis;

#endregion

namespace JustDummies;

public static partial class Dummy {

    /// <summary>
    ///     Composes two generators into one through a constructor lambda — the reflection-free way to assemble an
    ///     object from constrained parts. Each part draws from its own random context when the composed generator
    ///     generates.
    /// </summary>
    /// <remarks>
    ///     <example>
    ///         <code>
    ///         IDummy&lt;Customer&gt; customer = Dummy.Combine(
    ///             Dummy.String().NonEmpty().WithMaxLength(50),
    ///             Dummy.String().StartingWith("ORD-").WithLength(12),
    ///             (name, reference) =&gt; new Customer(name, OrderReference.Create(reference)));
    ///         </code>
    ///     </example>
    /// </remarks>
    /// <param name="first">The generator of the first part.</param>
    /// <param name="second">The generator of the second part.</param>
    /// <param name="compose">The constructor lambda assembling the parts.</param>
    /// <typeparam name="T1">The type of the first part.</typeparam>
    /// <typeparam name="T2">The type of the second part.</typeparam>
    /// <typeparam name="TResult">The type of the composed value.</typeparam>
    /// <returns>A generator of the composed value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
    public static IDummy<TResult> Combine<T1, T2, TResult>(IDummy<T1> first, IDummy<T2> second, Func<T1, T2, TResult> compose) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }
        if (compose is null) { throw new ArgumentNullException(nameof(compose)); }

        RandomSource? source       = DummyDerivation.SourceOf(first) ?? DummyDerivation.SourceOf(second);
        bool          reproducible = DummyDerivation.DrawsOnlyFrom(first, source) && DummyDerivation.DrawsOnlyFrom(second, source);

        return new DerivedDummy<TResult>(source, reproducible, () => {
            T1 firstValue  = first.Generate();
            T2 secondValue = second.Generate();

            return DummyDerivation.Invoke(() => compose(firstValue, secondValue), source, reproducible, () => $"the composer passed to Combine(...) threw for the generated values ({DummyDerivation.Display(firstValue)}, {DummyDerivation.Display(secondValue)})");
        });
    }

    /// <summary>
    ///     Composes three generators into one through a constructor lambda — see
    ///     <see cref="Combine{T1,T2,TResult}" />.
    /// </summary>
    /// <param name="first">The generator of the first part.</param>
    /// <param name="second">The generator of the second part.</param>
    /// <param name="third">The generator of the third part.</param>
    /// <param name="compose">The constructor lambda assembling the parts.</param>
    /// <typeparam name="T1">The type of the first part.</typeparam>
    /// <typeparam name="T2">The type of the second part.</typeparam>
    /// <typeparam name="T3">The type of the third part.</typeparam>
    /// <typeparam name="TResult">The type of the composed value.</typeparam>
    /// <returns>A generator of the composed value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
    [SuppressMessage(SonarRule.S2436.Category, SonarRule.S2436.Id, Justification = SuppressionJustification.S2436.HeterogeneousCombine)]
    public static IDummy<TResult> Combine<T1, T2, T3, TResult>(IDummy<T1> first, IDummy<T2> second, IDummy<T3> third, Func<T1, T2, T3, TResult> compose) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }
        if (third is null) { throw new ArgumentNullException(nameof(third)); }
        if (compose is null) { throw new ArgumentNullException(nameof(compose)); }

        RandomSource? source       = DummyDerivation.SourceOf(first) ?? DummyDerivation.SourceOf(second) ?? DummyDerivation.SourceOf(third);
        bool          reproducible = DummyDerivation.DrawsOnlyFrom(first, source) && DummyDerivation.DrawsOnlyFrom(second, source) && DummyDerivation.DrawsOnlyFrom(third, source);

        return new DerivedDummy<TResult>(source, reproducible, () => {
            T1 firstValue  = first.Generate();
            T2 secondValue = second.Generate();
            T3 thirdValue  = third.Generate();

            return DummyDerivation.Invoke(() => compose(firstValue, secondValue, thirdValue), source, reproducible, () => $"the composer passed to Combine(...) threw for the generated values ({DummyDerivation.Display(firstValue)}, {DummyDerivation.Display(secondValue)}, {DummyDerivation.Display(thirdValue)})");
        });
    }

    /// <summary>
    ///     Composes four generators into one through a constructor lambda — see <see cref="Combine{T1,T2,TResult}" />.
    /// </summary>
    /// <param name="first">The generator of the first part.</param>
    /// <param name="second">The generator of the second part.</param>
    /// <param name="third">The generator of the third part.</param>
    /// <param name="fourth">The generator of the fourth part.</param>
    /// <param name="compose">The constructor lambda assembling the parts.</param>
    /// <typeparam name="T1">The type of the first part.</typeparam>
    /// <typeparam name="T2">The type of the second part.</typeparam>
    /// <typeparam name="T3">The type of the third part.</typeparam>
    /// <typeparam name="T4">The type of the fourth part.</typeparam>
    /// <typeparam name="TResult">The type of the composed value.</typeparam>
    /// <returns>A generator of the composed value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
    [SuppressMessage(SonarRule.S2436.Category, SonarRule.S2436.Id, Justification = SuppressionJustification.S2436.HeterogeneousCombine)]
    public static IDummy<TResult> Combine<T1, T2, T3, T4, TResult>(IDummy<T1> first, IDummy<T2> second, IDummy<T3> third, IDummy<T4> fourth, Func<T1, T2, T3, T4, TResult> compose) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }
        if (third is null) { throw new ArgumentNullException(nameof(third)); }
        if (fourth is null) { throw new ArgumentNullException(nameof(fourth)); }
        if (compose is null) { throw new ArgumentNullException(nameof(compose)); }

        RandomSource? source       = DummyDerivation.SourceOf(first) ?? DummyDerivation.SourceOf(second) ?? DummyDerivation.SourceOf(third) ?? DummyDerivation.SourceOf(fourth);
        bool          reproducible = DummyDerivation.DrawsOnlyFrom(first, source) && DummyDerivation.DrawsOnlyFrom(second, source) && DummyDerivation.DrawsOnlyFrom(third, source) && DummyDerivation.DrawsOnlyFrom(fourth, source);

        return new DerivedDummy<TResult>(source, reproducible, () => {
            T1 firstValue  = first.Generate();
            T2 secondValue = second.Generate();
            T3 thirdValue  = third.Generate();
            T4 fourthValue = fourth.Generate();

            return DummyDerivation.Invoke(() => compose(firstValue, secondValue, thirdValue, fourthValue), source, reproducible, () => $"the composer passed to Combine(...) threw for the generated values ({DummyDerivation.Display(firstValue)}, {DummyDerivation.Display(secondValue)}, {DummyDerivation.Display(thirdValue)}, {DummyDerivation.Display(fourthValue)})");
        });
    }

    /// <summary>
    ///     Composes five generators into one through a constructor lambda — see <see cref="Combine{T1,T2,TResult}" />.
    /// </summary>
    /// <param name="first">The generator of the first part.</param>
    /// <param name="second">The generator of the second part.</param>
    /// <param name="third">The generator of the third part.</param>
    /// <param name="fourth">The generator of the fourth part.</param>
    /// <param name="fifth">The generator of the fifth part.</param>
    /// <param name="compose">The constructor lambda assembling the parts.</param>
    /// <typeparam name="T1">The type of the first part.</typeparam>
    /// <typeparam name="T2">The type of the second part.</typeparam>
    /// <typeparam name="T3">The type of the third part.</typeparam>
    /// <typeparam name="T4">The type of the fourth part.</typeparam>
    /// <typeparam name="T5">The type of the fifth part.</typeparam>
    /// <typeparam name="TResult">The type of the composed value.</typeparam>
    /// <returns>A generator of the composed value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
    [SuppressMessage(SonarRule.S2436.Category, SonarRule.S2436.Id, Justification = SuppressionJustification.S2436.HeterogeneousCombine)]
    public static IDummy<TResult> Combine<T1, T2, T3, T4, T5, TResult>(IDummy<T1> first, IDummy<T2> second, IDummy<T3> third, IDummy<T4> fourth, IDummy<T5> fifth, Func<T1, T2, T3, T4, T5, TResult> compose) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }
        if (third is null) { throw new ArgumentNullException(nameof(third)); }
        if (fourth is null) { throw new ArgumentNullException(nameof(fourth)); }
        if (fifth is null) { throw new ArgumentNullException(nameof(fifth)); }
        if (compose is null) { throw new ArgumentNullException(nameof(compose)); }

        RandomSource? source       = DummyDerivation.SourceOf(first) ?? DummyDerivation.SourceOf(second) ?? DummyDerivation.SourceOf(third) ?? DummyDerivation.SourceOf(fourth) ?? DummyDerivation.SourceOf(fifth);
        bool          reproducible = DummyDerivation.DrawsOnlyFrom(first, source) && DummyDerivation.DrawsOnlyFrom(second, source) && DummyDerivation.DrawsOnlyFrom(third, source) && DummyDerivation.DrawsOnlyFrom(fourth, source) && DummyDerivation.DrawsOnlyFrom(fifth, source);

        return new DerivedDummy<TResult>(source, reproducible, () => {
            T1 firstValue  = first.Generate();
            T2 secondValue = second.Generate();
            T3 thirdValue  = third.Generate();
            T4 fourthValue = fourth.Generate();
            T5 fifthValue  = fifth.Generate();

            return DummyDerivation.Invoke(() => compose(firstValue, secondValue, thirdValue, fourthValue, fifthValue), source, reproducible, () => $"the composer passed to Combine(...) threw for the generated values ({DummyDerivation.Display(firstValue)}, {DummyDerivation.Display(secondValue)}, {DummyDerivation.Display(thirdValue)}, {DummyDerivation.Display(fourthValue)}, {DummyDerivation.Display(fifthValue)})");
        });
    }

    /// <summary>
    ///     Composes six generators into one through a constructor lambda — see <see cref="Combine{T1,T2,TResult}" />.
    /// </summary>
    /// <param name="first">The generator of the first part.</param>
    /// <param name="second">The generator of the second part.</param>
    /// <param name="third">The generator of the third part.</param>
    /// <param name="fourth">The generator of the fourth part.</param>
    /// <param name="fifth">The generator of the fifth part.</param>
    /// <param name="sixth">The generator of the sixth part.</param>
    /// <param name="compose">The constructor lambda assembling the parts.</param>
    /// <typeparam name="T1">The type of the first part.</typeparam>
    /// <typeparam name="T2">The type of the second part.</typeparam>
    /// <typeparam name="T3">The type of the third part.</typeparam>
    /// <typeparam name="T4">The type of the fourth part.</typeparam>
    /// <typeparam name="T5">The type of the fifth part.</typeparam>
    /// <typeparam name="T6">The type of the sixth part.</typeparam>
    /// <typeparam name="TResult">The type of the composed value.</typeparam>
    /// <returns>A generator of the composed value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
    [SuppressMessage(SonarRule.S2436.Category, SonarRule.S2436.Id, Justification = SuppressionJustification.S2436.HeterogeneousCombine)]
    public static IDummy<TResult> Combine<T1, T2, T3, T4, T5, T6, TResult>(IDummy<T1> first, IDummy<T2> second, IDummy<T3> third, IDummy<T4> fourth, IDummy<T5> fifth, IDummy<T6> sixth, Func<T1, T2, T3, T4, T5, T6, TResult> compose) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }
        if (third is null) { throw new ArgumentNullException(nameof(third)); }
        if (fourth is null) { throw new ArgumentNullException(nameof(fourth)); }
        if (fifth is null) { throw new ArgumentNullException(nameof(fifth)); }
        if (sixth is null) { throw new ArgumentNullException(nameof(sixth)); }
        if (compose is null) { throw new ArgumentNullException(nameof(compose)); }

        RandomSource? source       = DummyDerivation.SourceOf(first) ?? DummyDerivation.SourceOf(second) ?? DummyDerivation.SourceOf(third) ?? DummyDerivation.SourceOf(fourth) ?? DummyDerivation.SourceOf(fifth) ?? DummyDerivation.SourceOf(sixth);
        bool          reproducible = DummyDerivation.DrawsOnlyFrom(first, source) && DummyDerivation.DrawsOnlyFrom(second, source) && DummyDerivation.DrawsOnlyFrom(third, source) && DummyDerivation.DrawsOnlyFrom(fourth, source) && DummyDerivation.DrawsOnlyFrom(fifth, source) && DummyDerivation.DrawsOnlyFrom(sixth, source);

        return new DerivedDummy<TResult>(source, reproducible, () => {
            T1 firstValue  = first.Generate();
            T2 secondValue = second.Generate();
            T3 thirdValue  = third.Generate();
            T4 fourthValue = fourth.Generate();
            T5 fifthValue  = fifth.Generate();
            T6 sixthValue  = sixth.Generate();

            return DummyDerivation.Invoke(() => compose(firstValue, secondValue, thirdValue, fourthValue, fifthValue, sixthValue), source, reproducible, () => $"the composer passed to Combine(...) threw for the generated values ({DummyDerivation.Display(firstValue)}, {DummyDerivation.Display(secondValue)}, {DummyDerivation.Display(thirdValue)}, {DummyDerivation.Display(fourthValue)}, {DummyDerivation.Display(fifthValue)}, {DummyDerivation.Display(sixthValue)})");
        });
    }

    /// <summary>
    ///     Composes seven generators into one through a constructor lambda — see <see cref="Combine{T1,T2,TResult}" />.
    /// </summary>
    /// <param name="first">The generator of the first part.</param>
    /// <param name="second">The generator of the second part.</param>
    /// <param name="third">The generator of the third part.</param>
    /// <param name="fourth">The generator of the fourth part.</param>
    /// <param name="fifth">The generator of the fifth part.</param>
    /// <param name="sixth">The generator of the sixth part.</param>
    /// <param name="seventh">The generator of the seventh part.</param>
    /// <param name="compose">The constructor lambda assembling the parts.</param>
    /// <typeparam name="T1">The type of the first part.</typeparam>
    /// <typeparam name="T2">The type of the second part.</typeparam>
    /// <typeparam name="T3">The type of the third part.</typeparam>
    /// <typeparam name="T4">The type of the fourth part.</typeparam>
    /// <typeparam name="T5">The type of the fifth part.</typeparam>
    /// <typeparam name="T6">The type of the sixth part.</typeparam>
    /// <typeparam name="T7">The type of the seventh part.</typeparam>
    /// <typeparam name="TResult">The type of the composed value.</typeparam>
    /// <returns>A generator of the composed value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
    [SuppressMessage(SonarRule.S107.Category, SonarRule.S107.Id, Justification = SuppressionJustification.S107.HeterogeneousCombine)]
    [SuppressMessage(SonarRule.S2436.Category, SonarRule.S2436.Id, Justification = SuppressionJustification.S2436.HeterogeneousCombine)]
    public static IDummy<TResult> Combine<T1, T2, T3, T4, T5, T6, T7, TResult>(IDummy<T1> first, IDummy<T2> second, IDummy<T3> third, IDummy<T4> fourth, IDummy<T5> fifth, IDummy<T6> sixth, IDummy<T7> seventh, Func<T1, T2, T3, T4, T5, T6, T7, TResult> compose) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }
        if (third is null) { throw new ArgumentNullException(nameof(third)); }
        if (fourth is null) { throw new ArgumentNullException(nameof(fourth)); }
        if (fifth is null) { throw new ArgumentNullException(nameof(fifth)); }
        if (sixth is null) { throw new ArgumentNullException(nameof(sixth)); }
        if (seventh is null) { throw new ArgumentNullException(nameof(seventh)); }
        if (compose is null) { throw new ArgumentNullException(nameof(compose)); }

        RandomSource? source       = DummyDerivation.SourceOf(first) ?? DummyDerivation.SourceOf(second) ?? DummyDerivation.SourceOf(third) ?? DummyDerivation.SourceOf(fourth) ?? DummyDerivation.SourceOf(fifth) ?? DummyDerivation.SourceOf(sixth) ?? DummyDerivation.SourceOf(seventh);
        bool          reproducible = DummyDerivation.DrawsOnlyFrom(first, source) && DummyDerivation.DrawsOnlyFrom(second, source) && DummyDerivation.DrawsOnlyFrom(third, source) && DummyDerivation.DrawsOnlyFrom(fourth, source) && DummyDerivation.DrawsOnlyFrom(fifth, source) && DummyDerivation.DrawsOnlyFrom(sixth, source) && DummyDerivation.DrawsOnlyFrom(seventh, source);

        return new DerivedDummy<TResult>(source, reproducible, () => {
            T1 firstValue   = first.Generate();
            T2 secondValue  = second.Generate();
            T3 thirdValue   = third.Generate();
            T4 fourthValue  = fourth.Generate();
            T5 fifthValue   = fifth.Generate();
            T6 sixthValue   = sixth.Generate();
            T7 seventhValue = seventh.Generate();

            return DummyDerivation.Invoke(() => compose(firstValue, secondValue, thirdValue, fourthValue, fifthValue, sixthValue, seventhValue), source, reproducible, () => $"the composer passed to Combine(...) threw for the generated values ({DummyDerivation.Display(firstValue)}, {DummyDerivation.Display(secondValue)}, {DummyDerivation.Display(thirdValue)}, {DummyDerivation.Display(fourthValue)}, {DummyDerivation.Display(fifthValue)}, {DummyDerivation.Display(sixthValue)}, {DummyDerivation.Display(seventhValue)})");
        });
    }

    /// <summary>
    ///     Composes eight generators into one through a constructor lambda — see <see cref="Combine{T1,T2,TResult}" />.
    ///     Eight is the ceiling; a constructor needing more parts is better assembled from intermediate value objects.
    /// </summary>
    /// <param name="first">The generator of the first part.</param>
    /// <param name="second">The generator of the second part.</param>
    /// <param name="third">The generator of the third part.</param>
    /// <param name="fourth">The generator of the fourth part.</param>
    /// <param name="fifth">The generator of the fifth part.</param>
    /// <param name="sixth">The generator of the sixth part.</param>
    /// <param name="seventh">The generator of the seventh part.</param>
    /// <param name="eighth">The generator of the eighth part.</param>
    /// <param name="compose">The constructor lambda assembling the parts.</param>
    /// <typeparam name="T1">The type of the first part.</typeparam>
    /// <typeparam name="T2">The type of the second part.</typeparam>
    /// <typeparam name="T3">The type of the third part.</typeparam>
    /// <typeparam name="T4">The type of the fourth part.</typeparam>
    /// <typeparam name="T5">The type of the fifth part.</typeparam>
    /// <typeparam name="T6">The type of the sixth part.</typeparam>
    /// <typeparam name="T7">The type of the seventh part.</typeparam>
    /// <typeparam name="T8">The type of the eighth part.</typeparam>
    /// <typeparam name="TResult">The type of the composed value.</typeparam>
    /// <returns>A generator of the composed value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
    [SuppressMessage(SonarRule.S107.Category, SonarRule.S107.Id, Justification = SuppressionJustification.S107.HeterogeneousCombine)]
    [SuppressMessage(SonarRule.S2436.Category, SonarRule.S2436.Id, Justification = SuppressionJustification.S2436.HeterogeneousCombine)]
    public static IDummy<TResult> Combine<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(IDummy<T1> first, IDummy<T2> second, IDummy<T3> third, IDummy<T4> fourth, IDummy<T5> fifth, IDummy<T6> sixth, IDummy<T7> seventh, IDummy<T8> eighth, Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> compose) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }
        if (third is null) { throw new ArgumentNullException(nameof(third)); }
        if (fourth is null) { throw new ArgumentNullException(nameof(fourth)); }
        if (fifth is null) { throw new ArgumentNullException(nameof(fifth)); }
        if (sixth is null) { throw new ArgumentNullException(nameof(sixth)); }
        if (seventh is null) { throw new ArgumentNullException(nameof(seventh)); }
        if (eighth is null) { throw new ArgumentNullException(nameof(eighth)); }
        if (compose is null) { throw new ArgumentNullException(nameof(compose)); }

        RandomSource? source       = DummyDerivation.SourceOf(first) ?? DummyDerivation.SourceOf(second) ?? DummyDerivation.SourceOf(third) ?? DummyDerivation.SourceOf(fourth) ?? DummyDerivation.SourceOf(fifth) ?? DummyDerivation.SourceOf(sixth) ?? DummyDerivation.SourceOf(seventh) ?? DummyDerivation.SourceOf(eighth);
        bool          reproducible = DummyDerivation.DrawsOnlyFrom(first, source) && DummyDerivation.DrawsOnlyFrom(second, source) && DummyDerivation.DrawsOnlyFrom(third, source) && DummyDerivation.DrawsOnlyFrom(fourth, source) && DummyDerivation.DrawsOnlyFrom(fifth, source) && DummyDerivation.DrawsOnlyFrom(sixth, source) && DummyDerivation.DrawsOnlyFrom(seventh, source) && DummyDerivation.DrawsOnlyFrom(eighth, source);

        return new DerivedDummy<TResult>(source, reproducible, () => {
            T1 firstValue   = first.Generate();
            T2 secondValue  = second.Generate();
            T3 thirdValue   = third.Generate();
            T4 fourthValue  = fourth.Generate();
            T5 fifthValue   = fifth.Generate();
            T6 sixthValue   = sixth.Generate();
            T7 seventhValue = seventh.Generate();
            T8 eighthValue  = eighth.Generate();

            return DummyDerivation.Invoke(() => compose(firstValue, secondValue, thirdValue, fourthValue, fifthValue, sixthValue, seventhValue, eighthValue), source, reproducible, () => $"the composer passed to Combine(...) threw for the generated values ({DummyDerivation.Display(firstValue)}, {DummyDerivation.Display(secondValue)}, {DummyDerivation.Display(thirdValue)}, {DummyDerivation.Display(fourthValue)}, {DummyDerivation.Display(fifthValue)}, {DummyDerivation.Display(sixthValue)}, {DummyDerivation.Display(seventhValue)}, {DummyDerivation.Display(eighthValue)})");
        });
    }

    /// <summary>
    ///     Composes two generators into a generator of the value tuple <c>(<typeparamref name="T1" />,
    ///     <typeparamref name="T2" />)</c> — sugar over <see cref="Combine{T1,T2,TResult}" /> for the common case of
    ///     pairing two arbitrary values.
    /// </summary>
    /// <param name="first">The generator of the first component.</param>
    /// <param name="second">The generator of the second component.</param>
    /// <typeparam name="T1">The type of the first component.</typeparam>
    /// <typeparam name="T2">The type of the second component.</typeparam>
    /// <returns>A generator of the paired value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
    public static IDummy<(T1, T2)> PairOf<T1, T2>(IDummy<T1> first, IDummy<T2> second) {
        return Combine(first, second, (one, two) => (one, two));
    }

    /// <summary>
    ///     Composes three generators into a generator of the value tuple <c>(<typeparamref name="T1" />,
    ///     <typeparamref name="T2" />, <typeparamref name="T3" />)</c> — sugar over
    ///     <see cref="Combine{T1,T2,T3,TResult}" />.
    /// </summary>
    /// <param name="first">The generator of the first component.</param>
    /// <param name="second">The generator of the second component.</param>
    /// <param name="third">The generator of the third component.</param>
    /// <typeparam name="T1">The type of the first component.</typeparam>
    /// <typeparam name="T2">The type of the second component.</typeparam>
    /// <typeparam name="T3">The type of the third component.</typeparam>
    /// <returns>A generator of the tripled value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
    public static IDummy<(T1, T2, T3)> TripleOf<T1, T2, T3>(IDummy<T1> first, IDummy<T2> second, IDummy<T3> third) {
        return Combine(first, second, third, (one, two, three) => (one, two, three));
    }

}
