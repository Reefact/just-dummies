using System;
using System.Collections.Generic;
using System.Linq;

using Spectre.Console.Cli;

namespace JustDummies.Cli;

/// <summary>
///     The smallest thing that lets a command be constructed with what it needs.
/// </summary>
/// <remarks>
///     Spectre builds command instances itself, so a command taking a constructor argument needs a registrar to
///     supply it. A container would be the usual answer; this tool has exactly one thing to inject — the two
///     consoles — and taking a dependency to hold one registration would be worse than the twenty lines below.
/// </remarks>
internal sealed class ToolTypeRegistrar : ITypeRegistrar {

    private readonly Dictionary<Type, object> instances = [];

    private readonly Dictionary<Type, Func<object>> factories = [];

    private readonly Dictionary<Type, Type> implementations = [];

    /// <inheritdoc />
    public void Register(Type service, Type implementation) {
        implementations[service] = implementation;
    }

    /// <inheritdoc />
    public void RegisterInstance(Type service, object implementation) {
        instances[service] = implementation;
    }

    /// <inheritdoc />
    public void RegisterLazy(Type service, Func<object> factory) {
        factories[service] = factory;
    }

    /// <inheritdoc />
    public ITypeResolver Build() {
        return new ToolTypeResolver(instances, factories, implementations);
    }

}
