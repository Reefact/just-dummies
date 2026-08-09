# `JustDummies.DiagnosticCatalog`

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./justdummies-diagnosticcatalog.fr.md)

The `JD001`–`JD028` rules, published as constants a `[SuppressMessage]` can name. You only need this
package if you suppress a JustDummies diagnostic and want the compiler to check that the rule you
named exists.

## The problem it solves

A suppression normally names its rule as two string literals:

<!-- jd:declarations -->
```csharp
internal static class LegacyArrangements {

    [SuppressMessage("JustDummies.Usage", "JD006:DiscardedGeneratorResult", Justification = "Measured: the constraint is applied upstream.")]
    internal static void Legacy() {
        // ...
    }

}
```

Nothing verifies either string. Misspell the category and the suppression silently stops working.
Retire or renumber the rule and the attribute lingers, suppressing nothing, with no warning — the
compiler never resolved those strings in the first place, so it cannot notice when they go stale.

## The fix

Reference the catalogue and name the rule through constants the compiler resolves:

<!-- jd:declarations -->
```csharp
internal static class LegacyArrangements {

    [SuppressMessage(JustDummiesRule.JD006.Category, JustDummiesRule.JD006.Id, Justification = "Measured: the constraint is applied upstream.")]
    internal static void Legacy() {
        // ...
    }

}
```

Now a renamed category or a retired rule is a **compile error** at every suppression site, which is
exactly where the decision to suppress was made
([ADR-0050](../../for-maintainers/adr/0050-name-a-suppressed-rule-through-a-catalogue-constant.md)).

## Install

```bash
dotnet add package JustDummies.DiagnosticCatalog
```

It carries no generator and no analyzer of its own — only the identifiers. It is `netstandard2.0`,
like everything else here.

Add the namespace once, in your project file, and every suppression site sees the constants without
an extra `using`:

<!-- jd:skip -->
```xml
<ItemGroup>
  <Using Include="JustDummies.Diagnostics" />
</ItemGroup>
```

## What each rule constant carries

```csharp
string id       = JustDummiesRule.JD006.Id;          // "JD006"
string category = JustDummiesRule.JD006.Category;    // "JustDummies.Usage"
string title    = JustDummiesRule.JD006.Title;
string helpLink = JustDummiesRule.JD006.HelpLinkUri; // the rule's documentation page
```

`Title` and `HelpLinkUri` are there for tooling that reports rules — a build summary, a dashboard, a
custom reporter — so the description and the link come from the same place the analyzer reads rather
than from a second list that drifts.

## The four categories

`JustDummiesCategory` publishes the category strings on their own, for code that groups rules rather
than naming one:

| Constant | Value | Groups |
| --- | --- | --- |
| `JustDummiesCategory.Reproducibility` | `JustDummies.Reproducibility` | seeds, scopes, async bodies |
| `JustDummiesCategory.Usage` | `JustDummies.Usage` | the recipe-versus-value boundary |
| `JustDummiesCategory.Constraints` | `JustDummies.Constraints` | constraint sets decidable at compile time |
| `JustDummiesCategory.Composition` | `JustDummies.Composition` | `Combine` operands, element contracts |

## Do I need it?

**No**, if you never suppress a JustDummies rule — which is the common case. The analyzers ship
inside `JustDummies` itself and work without this package.

**Yes**, if suppressions do appear in your codebase and you would rather they were checked than
trusted. The published catalogue is what makes a suppression a compile-checked statement rather than
a comment that happens to be an attribute
([ADR-0052](../../for-maintainers/adr/0052-publish-the-jd-rules-as-a-first-party-catalogue.md)).

For the rules themselves, see the [analyzer rules index](../analyzers/README.md).

---

[← Packages](./README.md) · [Documentation index](../README.md)
