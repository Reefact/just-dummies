// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Pattern --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Shop.Legacy {

    /// <summary>
    ///     A generator of arbitrary <see cref="Pattern" /> values. It draws from the ambient random
    ///     context, so a reproducibility scope pins every draw it makes; outside a test,
    ///     <c>Any.UseSeed(...)</c> opens one.
    /// </summary>
    public sealed partial class AnyPattern : IAny<Pattern> {

        private readonly IAny<string> _text;

        /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
        public AnyPattern()
            : this(text: AnyValidText()) { }

        private static AnyString AnyValidText() {
            return Any.String().NonEmpty();
        }

        private AnyPattern(IAny<string> text) {
            _text = text;
        }

        /// <summary>Pins <c>text</c> to a fixed value.</summary>
        public AnyPattern WithText(string value) {
            return new AnyPattern(new FixedValue<string>(value));
        }

        /// <summary>Draws <c>text</c> from <c>AnyValidText()</c>, refined by <paramref name="configure" />.</summary>
        public AnyPattern WithText(Func<AnyString, AnyString> configure) {
            return new AnyPattern(configure(AnyValidText()));
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
