using System.Collections.Generic;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     The representative shapes §12 asks the emitter to be pinned against, as plans written by hand.
/// </summary>
/// <remarks>
///     Written by hand on purpose: resolution (§5) is what will build these, and until it does — and after it
///     does — the emitter has to be checkable without it. Every expression below is the one §4.1 and §5.2
///     specify, so an approved file can be read against the specification rather than against another program.
/// </remarks>
internal static class Shapes {

    private static readonly IReadOnlyList<string> LibraryOnly = ["JustDummies"];

    /// <summary>The §4.1 example, every parameter inferred, with <c>AnyCustomer</c> already scaffolded.</summary>
    internal static ScaffoldPlan Order() {
        return new ScaffoldPlan(new TargetType("Order", "Shop.Domain", NamespaceStyle.FileScoped),
                                "AnyOrder",
                                ["System", "System.Collections.Generic", "JustDummies"],
                                OrderParameters(customer: ScaffoldedParameter.DrawnFrom("customer", "Customer", "new AnyCustomer()")));
    }

    /// <summary>The same, before <c>Customer</c> was scaffolded: the one open parameter of §5.5.</summary>
    internal static ScaffoldPlan OrderWithTodo() {
        return new ScaffoldPlan(new TargetType("Order", "Shop.Domain", NamespaceStyle.FileScoped),
                                "AnyOrder",
                                ["System", "System.Collections.Generic", "JustDummies"],
                                OrderParameters(customer: ScaffoldedParameter.Unresolved("customer", "Customer")));
    }

    /// <summary>
    ///     The same, with one parameter's guard read but not vouched for: the doubt of §5.5, distinct from the
    ///     open parameter above — here a generator was inferred, and stays as the factory's working base.
    /// </summary>
    internal static ScaffoldPlan OrderRequiringVerification() {
        return new ScaffoldPlan(new TargetType("Order", "Shop.Domain", NamespaceStyle.FileScoped),
                                "AnyOrder",
                                ["System", "System.Collections.Generic", "JustDummies"],
                                OrderParameters(customer: ScaffoldedParameter.DrawnFrom("customer", "Customer", "new AnyCustomer()",
                                                                                        Provenance.UnreadGuards)));
    }

    /// <summary>One parameter, and no <c>System</c> using — the group separator has nothing to separate.</summary>
    internal static ScaffoldPlan Money() {
        return new ScaffoldPlan(new TargetType("Money", "Shop.Domain", NamespaceStyle.FileScoped),
                                "AnyMoney",
                                LibraryOnly,
                                [ScaffoldedParameter.DrawnFrom("amount", "decimal", "Any.Decimal()")]);
    }

    /// <summary>
    ///     The degenerate shape of §4.2 — no parameters — in the global namespace, so both paths that change
    ///     the file's skeleton are pinned by one file.
    /// </summary>
    internal static ScaffoldPlan Session() {
        return new ScaffoldPlan(new TargetType("Session", @namespace: null, NamespaceStyle.None),
                                "AnySession",
                                LibraryOnly,
                                []);
    }

    /// <summary>
    ///     A domain type whose generator name collides with the library's own <c>AnyPattern</c> (§7), declared
    ///     in a block namespace, which the emitted file copies (§4.4).
    /// </summary>
    /// <remarks>
    ///     The collision changes nothing here: it is a warning on the console, not a different file. Pinning it
    ///     is how that stays true — an emitter that started renaming, or commenting, would be caught.
    /// </remarks>
    internal static ScaffoldPlan Pattern() {
        return new ScaffoldPlan(new TargetType("Pattern", "Shop.Legacy", NamespaceStyle.Block),
                                "AnyPattern",
                                LibraryOnly,
                                [ScaffoldedParameter.DrawnFrom("text", "string", "Any.String().NonEmpty()")]);
    }

    /// <summary>A positional record: its primary constructor is an ordinary one, and needs no special handling.</summary>
    internal static ScaffoldPlan Address() {
        return new ScaffoldPlan(new TargetType("Address", "Shop.Domain", NamespaceStyle.FileScoped),
                                "AnyAddress",
                                LibraryOnly,
                                [ScaffoldedParameter.DrawnFrom("street", "string", "Any.String().NonEmpty()"),
                                 ScaffoldedParameter.DrawnFrom("city", "string", "Any.String().NonEmpty()")]);
    }

    /// <summary>A type with no accessible constructor: <c>Generate()</c> calls the static factory (§5.1).</summary>
    internal static ScaffoldPlan Email() {
        return new ScaffoldPlan(new TargetType("Email", "Shop.Domain", NamespaceStyle.FileScoped),
                                "AnyEmail",
                                LibraryOnly,
                                [ScaffoldedParameter.DrawnFrom("value", "string", "Any.String().NonEmpty()")],
                                factory: "Email.Create");
    }

    private static IReadOnlyList<ScaffoldedParameter> OrderParameters(ScaffoldedParameter customer) {
        return [
            ScaffoldedParameter.DrawnFrom("reference", "OrderReference", "Any.String().NonEmpty().As(OrderReference.Create)"),
            customer,
            ScaffoldedParameter.DrawnFrom("quantity", "int", "Any.Int32().Positive()"),
            ScaffoldedParameter.DrawnFrom("status", "OrderStatus", "Any.Enum<OrderStatus>()"),
            ScaffoldedParameter.DrawnFrom("tags", "IReadOnlyList<string>", "Any.ListOf(Any.String().NonEmpty())"),
            ScaffoldedParameter.DrawnFrom("placedAt", "DateTime", "Any.DateTime()")
        ];
    }

}
