// Scaffolded by dum (JustDummies). This file is yours: read it, edit it, commit it.
// `dum generate Pattern --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using JustDummies;

namespace Shop.Legacy {

    /// <summary>
    ///     A generator of arbitrary <see cref="Pattern" /> values. It draws from the ambient random
    ///     context, so a reproducibility scope pins it; to draw from an isolated
    ///     <c>Any.WithSeed(...)</c> context, pass that context's generators through the
    ///     <c>With…</c> overloads.
    /// </summary>
    public sealed partial class AnyPattern : IAny<Pattern> {

        private readonly IAny<string> _text;

        /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
        public AnyPattern()
            : this(text: TextFactory()) { }

        private static IAny<string> TextFactory() {
            return Any.String().NonEmpty();
        }

        private AnyPattern(IAny<string> text) {
            _text = text;
        }

        /// <summary>Pins <c>text</c> to a fixed value.</summary>
        public AnyPattern WithText(string value) {
            return WithText(new FixedValue<string>(value));
        }

        /// <summary>Draws <c>text</c> from <paramref name="generator" />.</summary>
        public AnyPattern WithText(IAny<string> generator) {
            return new AnyPattern(generator);
        }

        /// <summary>Produces one arbitrary <see cref="Pattern" />.</summary>
        public Pattern Generate() {
            return new Pattern(_text.Generate());
        }

        private sealed class FixedValue<TValue> : IAny<TValue> {

            private readonly TValue _value;

            public FixedValue(TValue value) {
                _value = value;
            }

            public TValue Generate() {
                return _value;
            }

        }

    }

}
