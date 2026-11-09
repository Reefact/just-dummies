// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Node --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace Tree.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Node" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyNode : IAny<Node> {

    private readonly IAny<string>              _name;
    private readonly IAny<IReadOnlyList<Node>> _children;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyNode()
        : this(name:     AnyValidName(),
               children: AnyValidChildren()) { }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank();
    }

    private static AnyList<Node> AnyValidChildren() {
        return Any.ListOf(new AnyNode());
    }

    private AnyNode(IAny<string>              name,
                    IAny<IReadOnlyList<Node>> children) {
        _name     = name;
        _children = children;
    }

    /// <summary>Pins <c>name</c> to a fixed value.</summary>
    public AnyNode WithName(string value) {
        return new AnyNode(new FixedValue<string>(value), _children);
    }

    /// <summary>Draws <c>name</c> from <c>AnyValidName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyNode WithName(Func<AnyString, AnyString> configure) {
        return new AnyNode(configure(AnyValidName()), _children);
    }

    /// <summary>Pins <c>children</c> to a fixed value.</summary>
    public AnyNode WithChildren(IReadOnlyList<Node> value) {
        return new AnyNode(_name, new FixedValue<IReadOnlyList<Node>>(value));
    }

    /// <summary>Draws <c>children</c> from <c>AnyValidChildren()</c>, refined by <paramref name="configure" />.</summary>
    public AnyNode WithChildren(Func<AnyList<Node>, AnyList<Node>> configure) {
        return new AnyNode(_name, configure(AnyValidChildren()));
    }

    /// <summary>Produces one arbitrary <see cref="Node" />.</summary>
    public Node Generate() {
        return new Node(_name.Generate(),
                        _children.Generate());
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
