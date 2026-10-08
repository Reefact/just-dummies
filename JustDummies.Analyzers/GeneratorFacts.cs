using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     Facts about the generator surface: recognising a value that is an <c>IAny&lt;T&gt;</c> recipe rather than the
///     value that recipe would draw. Every rule in the <c>JustDummies.Usage</c> category rests on this distinction.
/// </summary>
internal static class GeneratorFacts {

    private const string GenerateMethodName = "Generate";

    /// <summary>
    ///     Whether <paramref name="type" /> is a JustDummies generator — the <c>IAny&lt;T&gt;</c> interface itself, or
    ///     any type implementing it. Matching the interface rather than a list of concrete builders keeps the rules
    ///     correct for <c>As(...)</c> and <c>Combine(...)</c> derivations, and for a consumer's own generator.
    /// </summary>
    public static bool IsGenerator(ITypeSymbol? type, INamedTypeSymbol iAnyType) {
        if (type is null) { return false; }

        if (type is INamedTypeSymbol named && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, iAnyType)) { return true; }

        return type.AllInterfaces.Any(implemented => SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, iAnyType));
    }

    /// <summary>
    ///     Whether <paramref name="invocation" /> is a materializing <c>Generate()</c> call — the single member of
    ///     <c>IAny&lt;T&gt;</c>, and the only thing that turns a recipe into a value.
    /// </summary>
    public static bool IsGenerateCall(IInvocationOperation invocation, INamedTypeSymbol iAnyType) {
        IMethodSymbol method = invocation.TargetMethod;

        return method.Name == GenerateMethodName
            && method.Parameters.IsEmpty
            && IsGenerator(method.ContainingType, iAnyType);
    }

    /// <summary>
    ///     Whether the chain the <c>Generate()</c> call sits on provably starts at a static <c>JustDummies.Any</c>
    ///     factory — that is, whether the value is drawn from the <b>ambient</b> random source that a seed scope pins.
    /// </summary>
    /// <remarks>
    ///     Deliberately conservative: it answers "yes" only for a chain written inline from <c>Any</c>. A generator
    ///     reached through a local, a field or a parameter answers "no" and is not reported, which under-reports rather
    ///     than misfiring on a draw from an isolated <c>AnyContext</c> — that context is unaffected by the ambient
    ///     scope, so reporting it would be plainly wrong. The same reasoning excludes a chain written inline from
    ///     <c>Any.WithSeed(...)</c>: it is a static member of <c>Any</c>, but it hands back such a context, not a
    ///     generator. A derivation the library writes as an extension — <c>As(...)</c>, <c>OrNull()</c>,
    ///     <c>AsNullable()</c> — draws from the generator it is called on, so the walk continues through its receiver
    ///     rather than stopping at it.
    /// </remarks>
    public static bool RootsAtAmbientAny(IInvocationOperation invocation, INamedTypeSymbol anyType, INamedTypeSymbol iAnyType) {
        for (IOperation? current = invocation; current is IInvocationOperation call;) {
            if (call.Instance is not null) {
                current = Unwrap(call.Instance);

                continue;
            }

            IArgumentOperation? receiver = LibraryDerivationReceiver(call, anyType, iAnyType);
            if (receiver is not null) {
                current = Unwrap(receiver.Value);

                continue;
            }

            // A static call: ambient only when it is one of Any's own factories, which all return a generator.
            return SymbolEqualityComparer.Default.Equals(call.TargetMethod.ContainingType, anyType)
                && IsGenerator(call.TargetMethod.ReturnType, iAnyType);
        }

        return false;
    }

    /// <summary>
    ///     The receiver of <paramref name="call" /> when it is one of the library's own derivations written as an
    ///     extension method — declared beside <c>Any</c> and turning a generator into a generator — or <c>null</c>.
    /// </summary>
    /// <remarks>
    ///     Only the library's own extensions are seen through: they keep the source of the generator they wrap. A
    ///     consumer's extension method could draw from anywhere, so the walk stops there and the chain is not
    ///     reported, the conservative side.
    /// </remarks>
    private static IArgumentOperation? LibraryDerivationReceiver(IInvocationOperation call, INamedTypeSymbol anyType, INamedTypeSymbol iAnyType) {
        IMethodSymbol method = call.TargetMethod;
        if (!method.IsExtensionMethod) { return null; }
        if (!SymbolEqualityComparer.Default.Equals(method.ContainingAssembly, anyType.ContainingAssembly)) { return null; }
        if (!IsGenerator(method.ReturnType, iAnyType)) { return null; }

        return call.Arguments.FirstOrDefault(argument => argument.Parameter?.Ordinal == 0);
    }

    /// <summary>
    ///     Whether <paramref name="draw" /> sits in a body that runs later than the member it is written in — a lambda
    ///     or a local function handed on as a value — so the member's lifecycle says nothing about when it draws.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Roslyn attributes an operation inside a lambda or a local function to the enclosing member, so a rule
    ///         judging that member alone reads <c>() =&gt; Any.Int32().Generate()</c> stored in a field as a draw made by
    ///         the field initializer. The delegate draws when it is invoked, which is the caller's business.
    ///     </para>
    ///     <para>
    ///         A body is deferred only when nothing can run it before the member returns; whatever might run it stays the
    ///         member's, since a rule that misses a draw made now is worse than one that reports a draw made later. A
    ///         delegate is provably kept when the member returns it, writes it to a field or an auto-property, or places
    ///         it in a container that is itself kept that way — an array, a <c>List&lt;T&gt;</c> or a <c>TheoryData</c>,
    ///         which store what they are given, a <c>Lazy&lt;T&gt;</c>, which runs its factory on first read. A container
    ///         used on the spot (<c>new Func&lt;int&gt;[] { … }[0]()</c>) keeps nothing. Any other holder may run it: an
    ///         ordinary method (<c>Enumerable.Range(0, 3).Select(_ =&gt; Any.Int32().Generate()).ToArray()</c>), a
    ///         generic one that casts its argument back to a delegate, a constructor that calls its argument, an
    ///         invocation on the spot, a local the member then calls. A local function is deferred when the member never
    ///         calls it and keeps every method group of it in the same way. The walk crosses every enclosing body, so a
    ///         draw nested in a <c>Select</c> inside a stored lambda is deferred by the outer one.
    ///     </para>
    ///     <para>
    ///         A field or an auto-property of the type counts only when no initializer or constructor of the same phase
    ///         reads it back, so <c>_next = () =&gt; …; _value = _next();</c> in a constructor stays reported. A read
    ///         through a method those call is not followed: the rule under-reports there, and its pages say so.
    ///     </para>
    /// </remarks>
    public static bool IsDeferred(IOperation draw) {
        for (IOperation? current = draw.Parent; current is not null; current = current.Parent) {
            switch (current) {
                case IAnonymousFunctionOperation lambda when IsKept(lambda):
                    return true;
                case ILocalFunctionOperation local when IsOnlyKept(local):
                    return true;
            }
        }

        return false;
    }

    private static bool IsKept(IOperation function) {
        IOperation? holder = function.Parent;
        while (holder is IDelegateCreationOperation or IConversionOperation or IParenthesizedOperation) { holder = holder.Parent; }

        return holder switch {
            IReturnOperation @return                                                     => ReturnsFromTheMember(@return),
            IFieldInitializerOperation initializer                                       => initializer.InitializedFields.All(IsUnreadInItsPhase),
            IPropertyInitializerOperation initializer                                    => initializer.InitializedProperties.All(IsUnreadInItsPhase),
            ISimpleAssignmentOperation { Target: IFieldReferenceOperation reference }    => IsOwnMember(reference.Instance) && IsUnreadInItsPhase(reference.Field),
            ISimpleAssignmentOperation { Target: IPropertyReferenceOperation reference } => IsAutoProperty(reference.Property) && IsKeptProperty(reference, holder),
            // A container keeps the delegate only while it is itself kept: an array indexed and invoked on the spot
            // runs it there and then.
            IArrayInitializerOperation { Parent: IArrayCreationOperation array }         => IsKept(array),
            IArgumentOperation argument                                                  => IsKeptByTheCallee(argument),
            _                                                                            => false,
        };
    }

    // Only a return from the member itself hands the delegate out of it. A lambda returning one hands it to whoever
    // runs that lambda, which the walk in IsDeferred judges in turn.
    private static bool ReturnsFromTheMember(IOperation @return) {
        for (IOperation? current = @return.Parent; current is not null; current = current.Parent) {
            if (current is IAnonymousFunctionOperation or ILocalFunctionOperation) { return false; }
        }

        return true;
    }

    // A member of the type being built — this one's, or the type's own static one — rather than another object's.
    private static bool IsOwnMember(IOperation? instance) {
        return instance is null or IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance };
    }

    // An auto-property of the type being built is kept like a field; one set in an object initializer is kept as long
    // as the object it initializes is.
    private static bool IsKeptProperty(IPropertyReferenceOperation reference, IOperation assignment) {
        if (reference.Instance is IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ImplicitReceiver }) {
            return assignment.Parent is IObjectOrCollectionInitializerOperation { Parent: IObjectCreationOperation creation } && IsKept(creation);
        }

        return IsOwnMember(reference.Instance) && IsUnreadInItsPhase(reference.Property);
    }

    // A setter is code that may call its value; only an auto-property's provably stores it.
    private static bool IsAutoProperty(IPropertySymbol property) {
        IPropertySymbol definition = property.OriginalDefinition;

        return definition.ContainingType.GetMembers().OfType<IFieldSymbol>().Any(field => SymbolEqualityComparer.Default.Equals(field.AssociatedSymbol, definition));
    }

    // A field keeps the delegate only if nothing else running in the same phase reads it back: the initializers and
    // constructors of its type, static or instance as the field is. The scan is by name over syntax, across every
    // partial declaration, so a local sharing the name counts as a read too — the conservative side. A read through a
    // method those call is not seen.
    private static bool IsUnreadInItsPhase(ISymbol member) {
        return !member.ContainingType.DeclaringSyntaxReferences
                      .Select(reference => reference.GetSyntax())
                      .OfType<TypeDeclarationSyntax>()
                      .SelectMany(type => type.Members)
                      .Where(candidate => RunsInPhase(candidate, member.IsStatic))
                      .SelectMany(phaseMember => phaseMember.DescendantNodes().OfType<IdentifierNameSyntax>())
                      .Any(name => name.Identifier.ValueText == member.Name && !IsAssignedTo(name));
    }

    private static bool RunsInPhase(MemberDeclarationSyntax member, bool isStatic) {
        if (member.Modifiers.Any(SyntaxKind.StaticKeyword) != isStatic) { return false; }

        return member switch {
            ConstructorDeclarationSyntax                        => true,
            FieldDeclarationSyntax field                        => field.Declaration.Variables.Any(variable => variable.Initializer is not null),
            PropertyDeclarationSyntax { Initializer: not null } => true,
            _                                                   => false,
        };
    }

    // A plain write — `_next = …` or `this._next = …` — reads nothing back.
    private static bool IsAssignedTo(IdentifierNameSyntax name) {
        SyntaxNode target = name.Parent is MemberAccessExpressionSyntax access && access.Name == name ? access : name;

        return target.Parent is AssignmentExpressionSyntax assignment && assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) && assignment.Left == target;
    }

    // Only a callee whose keeping is established: Lazy<T> runs its factory on the first read of Value, never on
    // construction, and List<T> and xUnit's TheoryData store what a collection initializer adds. Any other callee may
    // run its argument — a constructor may call it, and even a method generic over its type can cast it back to a
    // delegate. The container is itself held to the same test: a Lazy<T> read at once runs its factory there and then.
    private static bool IsKeptByTheCallee(IArgumentOperation argument) {
        return argument.Parent switch {
            IObjectCreationOperation creation => IsType(creation.Constructor?.ContainingType, "System", "Lazy") && IsKept(creation),
            IInvocationOperation { IsImplicit: true, TargetMethod: { Name: "Add" } add, Parent: IObjectOrCollectionInitializerOperation { Parent: IObjectCreationOperation collection } }
                => (IsType(add.ContainingType, "System.Collections.Generic", "List") || IsType(add.ContainingType, "Xunit", "TheoryData") || IsType(add.ContainingType, "Xunit", "TheoryDataBase"))
                && IsKept(collection),
            _ => false,
        };
    }

    private static bool IsType(INamedTypeSymbol? type, string containingNamespace, string name) {
        return type is not null && type.Name == name && type.ContainingNamespace.ToDisplayString() == containingNamespace;
    }

    private static bool IsOnlyKept(ILocalFunctionOperation local) {
        IOperation root = local;
        while (root.Parent is not null) { root = root.Parent; }

        return !root.Descendants().Any(use => use switch {
            IInvocationOperation call           => SymbolEqualityComparer.Default.Equals(call.TargetMethod.OriginalDefinition, local.Symbol),
            IMethodReferenceOperation reference => SymbolEqualityComparer.Default.Equals(reference.Method.OriginalDefinition, local.Symbol) && !IsKept(reference),
            _                                   => false,
        });
    }

    /// <summary>
    ///     Strips the implicit conversions Roslyn inserts around a generator when it flows into an <c>object</c> or
    ///     <c>string</c> position, so the rule sees the recipe rather than the conversion wrapping it.
    /// </summary>
    public static IOperation Unwrap(IOperation operation) {
        IOperation current = operation;
        while (current is IConversionOperation { IsImplicit: true } conversion) {
            current = conversion.Operand;
        }

        return current;
    }

}
