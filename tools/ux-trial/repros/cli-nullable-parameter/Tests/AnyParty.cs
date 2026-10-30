// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Party --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace NullRepro;

/// <summary>
///     A generator of arbitrary <see cref="Party" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyParty : IAny<Party> {

    private readonly IAny<string> _name;
    private readonly IAny<Bic>    _agent;
    private readonly IAny<string> _note;
    private readonly IAny<int?>   _priority;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyParty()
        : this(name:     AnyValidName(),
               agent:    AnyValidAgent(),
               note:     AnyValidNote(),
               priority: AnyValidPriority().AsNullable()) { }

    private static AnyString AnyValidName() {
        return Any.String().NonEmpty();
    }

    private static AnyBic AnyValidAgent() {
        return new AnyBic();
    }

    private static AnyString AnyValidNote() {
        return Any.String().NonEmpty();
    }

    private static AnyInt32 AnyValidPriority() {
        return Any.Int32();
    }

    private AnyParty(IAny<string> name,
                     IAny<Bic>    agent,
                     IAny<string> note,
                     IAny<int?>   priority) {
        _name     = name;
        _agent    = agent;
        _note     = note;
        _priority = priority;
    }

    /// <summary>Pins <c>name</c> to a fixed value.</summary>
    public AnyParty WithName(string value) {
        return new AnyParty(new FixedValue<string>(value), _agent, _note, _priority);
    }

    /// <summary>Draws <c>name</c> from <c>AnyValidName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyParty WithName(Func<AnyString, AnyString> configure) {
        return new AnyParty(configure(AnyValidName()), _agent, _note, _priority);
    }

    /// <summary>Pins <c>agent</c> to a fixed value.</summary>
    public AnyParty WithAgent(Bic value) {
        return new AnyParty(_name, new FixedValue<Bic>(value), _note, _priority);
    }

    /// <summary>Draws <c>agent</c> from <c>AnyValidAgent()</c>, refined by <paramref name="configure" />.</summary>
    public AnyParty WithAgent(Func<AnyBic, AnyBic> configure) {
        return new AnyParty(_name, configure(AnyValidAgent()), _note, _priority);
    }

    /// <summary>Pins <c>note</c> to a fixed value.</summary>
    public AnyParty WithNote(string value) {
        return new AnyParty(_name, _agent, new FixedValue<string>(value), _priority);
    }

    /// <summary>Draws <c>note</c> from <c>AnyValidNote()</c>, refined by <paramref name="configure" />.</summary>
    public AnyParty WithNote(Func<AnyString, AnyString> configure) {
        return new AnyParty(_name, _agent, configure(AnyValidNote()), _priority);
    }

    /// <summary>Pins <c>priority</c> to a fixed value.</summary>
    public AnyParty WithPriority(int? value) {
        return new AnyParty(_name, _agent, _note, new FixedValue<int?>(value));
    }

    /// <summary>Draws <c>priority</c> from <c>AnyValidPriority()</c>, refined by <paramref name="configure" />.</summary>
    public AnyParty WithPriority(Func<AnyInt32, AnyInt32> configure) {
        return new AnyParty(_name, _agent, _note, configure(AnyValidPriority()).AsNullable());
    }

    /// <summary>Produces one arbitrary <see cref="Party" />.</summary>
    public Party Generate() {
        return new Party(_name.Generate(),
                         _agent.Generate(),
                         _note.Generate(),
                         _priority.Generate());
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
