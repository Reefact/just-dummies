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

        private IAny<string> _text;

        private AnyString _textRecipe;

        /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
        public AnyPattern()
            : this(text: AnyValidText()) { }

        private static AnyString AnyValidText() {
            return Any.String();
        }

        /// <summary>Draws every parameter from the recipe given for it.</summary>
        private AnyPattern(AnyString text) {
            _text       = text;
            _textRecipe = text;
        }

        /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
        private AnyPattern(AnyPattern from) {
            _text       = from._text;
            _textRecipe = from._textRecipe;
        }

        /// <summary>Pins <c>text</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
        public AnyPattern WithText(string value) {
            AnyPattern copy = new AnyPattern(this);

            copy._text = Any.Fixed(value);

            return copy;
        }

        /// <summary>Refines the recipe <c>text</c> is drawn from — <c>AnyValidText()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
        /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
        public AnyPattern WithText(Func<AnyString, AnyString> configure) {
            AnyPattern copy = new AnyPattern(this);

            copy._textRecipe = configure(_textRecipe);
            copy._text       = copy._textRecipe;

            return copy;
        }

        /// <summary>Produces one arbitrary <see cref="Pattern" />.</summary>
        public Pattern Generate() {
            return new Pattern(_text.Generate());
        }

    }

}
