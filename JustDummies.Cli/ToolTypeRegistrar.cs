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

/// <summary>Resolves what the registrar was told, and constructs the rest.</summary>
internal sealed class ToolTypeResolver : ITypeResolver {

    private readonly Dictionary<Type, object> instances;

    private readonly Dictionary<Type, Func<object>> factories;

    private readonly Dictionary<Type, Type> implementations;

    internal ToolTypeResolver(Dictionary<Type, object> instances,
                              Dictionary<Type, Func<object>> factories,
                              Dictionary<Type, Type> implementations) {
        this.instances       = instances;
        this.factories       = factories;
        this.implementations = implementations;
    }

    /// <inheritdoc />
    /// <remarks>
    ///     A type nobody registered is constructed through its single constructor, resolving each parameter the
    ///     same way. That is what makes a command's dependencies ordinary constructor arguments rather than
    ///     something it fetches for itself.
    /// </remarks>
    public object? Resolve(Type? type) {
        if (type is null) { return null; }
        if (instances.TryGetValue(type, out object? instance)) { return instance; }
        if (factories.TryGetValue(type, out Func<object>? factory)) { return factory(); }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
            return Many(type.GetGenericArguments()[0]);
        }

        Type constructed = implementations.TryGetValue(type, out Type? implementation) ? implementation : type;

        if (constructed.IsAbstract || constructed.IsInterface) { return null; }

        System.Reflection.ConstructorInfo? chosen = constructed.GetConstructors()
                                                               .OrderByDescending(candidate => candidate.GetParameters().Length)
                                                               .FirstOrDefault();

        if (chosen is null) { return null; }

        return chosen.Invoke([.. chosen.GetParameters().Select(parameter => Resolve(parameter.ParameterType))]);
    }

    /// <summary>
    ///     Everything registered for <paramref name="element" />, as an array of it — and an empty one when
    ///     nothing is.
    /// </summary>
    /// <remarks>
    ///     Spectre asks for <c>IEnumerable&lt;IHelpProvider&gt;</c> on its way to printing help, and a resolver
    ///     answering null there fails the whole invocation rather than falling back to the built-in provider.
    ///     "Nothing registered" is a legitimate answer to a request for many; it is only a request for exactly
    ///     one that has no answer.
    /// </remarks>
    private object Many(Type element) {
        List<object> found = [];

        if (instances.TryGetValue(element, out object? instance)) {
            found.Add(instance);
        } else if (factories.TryGetValue(element, out Func<object>? factory)) {
            found.Add(factory());
        }

        Array many = Array.CreateInstance(element, found.Count);

        for (int index = 0; index < found.Count; index++) { many.SetValue(found[index], index); }

        return many;
    }

}
