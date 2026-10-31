// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Voice --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;
using MusicScore.Domain;

namespace MusicScore.Tests.Expert.Arbitrary;

/// <summary>
///     A generator of arbitrary <see cref="Voice" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyVoice : IAny<Voice> {

    private readonly IAny<int>                       _number;
    private readonly IAny<IReadOnlyList<VoiceEntry>> _entries;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVoice()
        : this(number:  AnyValidNumber(),
               entries: AnyValidEntries()) { }

    private static AnyInt32 AnyValidNumber() {
        return Any.Int32().Between(1, Voice.MaxVoices);
    }

    private static AnyList<VoiceEntry> AnyValidEntries() {
        return Any.ListOf(new AnyVoiceEntry()).WithCountBetween(1, 8);
    }

    private AnyVoice(IAny<int>                       number,
                     IAny<IReadOnlyList<VoiceEntry>> entries) {
        _number  = number;
        _entries = entries;
    }

    /// <summary>Pins <c>number</c> to a fixed value.</summary>
    public AnyVoice WithNumber(int value) {
        return new AnyVoice(new FixedValue<int>(value), _entries);
    }

    /// <summary>Draws <c>number</c> from <c>AnyValidNumber()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVoice WithNumber(Func<AnyInt32, AnyInt32> configure) {
        return new AnyVoice(configure(AnyValidNumber()), _entries);
    }

    /// <summary>Pins <c>entries</c> to a fixed value.</summary>
    public AnyVoice WithEntries(IReadOnlyList<VoiceEntry> value) {
        return new AnyVoice(_number, new FixedValue<IReadOnlyList<VoiceEntry>>(value));
    }

    /// <summary>Draws <c>entries</c> from <c>AnyValidEntries()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVoice WithEntries(Func<AnyList<VoiceEntry>, AnyList<VoiceEntry>> configure) {
        return new AnyVoice(_number, configure(AnyValidEntries()));
    }

    /// <summary>Produces one arbitrary <see cref="Voice" />.</summary>
    public Voice Generate() {
        return new Voice(_number.Generate(),
                         _entries.Generate());
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
