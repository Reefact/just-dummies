# JustDummies.DiagnosticCatalog

The **JustDummies analyzer rules** — `JD001` to `JD033` — as compile-checked constants, so a
`[SuppressMessage]` names a rule the compiler resolves instead of a string nobody verifies.

```csharp
using JustDummies.Diagnostics;

[SuppressMessage(JustDummiesRule.JD006.Category, JustDummiesRule.JD006.Id,
                 Justification = "The drawn value is the subject of the assertion below.")]
public void TheGeneratorResultIsDeliberatelyDiscarded() { }
```

Referencing this package also switches on the `DCAT` analyzers, which report a literal suppression a
catalogue could describe and one carrying no justification, and offer a fix for the first.

## Why a constant rather than the literal

Not typo protection — a mistyped id leaves the rule active, so the diagnostic still reports and you
find out. What it protects against is the **silent** case: a rule that is retired or recategorised
leaves an attribute that keeps compiling, silences nothing, and no build says so.

Because this catalogue is **first-party**, it goes one step further than a mirror can. The
`DiagnosticDescriptor` that JustDummies' analyzer reports with reads the *same constants* this package
publishes, so the category you write is exact by construction rather than by diligence.

## Install

```
dotnet add package JustDummies.DiagnosticCatalog
```

You do not need it to use JustDummies — the analyzers ship inside the
[`JustDummies`](https://www.nuget.org/packages/JustDummies) package and report with or without this
one. Reference this only if you write suppressions of `JD` rules and want them checked.

To keep the constants and decline the analysis, set `EnableDiagnosticCatalogAnalyzers` to `false` in
your project.

## What a rule carries

`Id`, `Category`, `Title` and `HelpLinkUri`. Hovering the constant in your IDE shows the rule's own
title, which is where the prose goes once the suppression stops carrying it.

## Compatibility

`netstandard2.0`, which reaches every consumer JustDummies itself supports, down to the .NET Framework
4.7.2 floor.

## Stability

A rule is never removed and a member is never renamed. These are `const`, so they are inlined into
**your** assembly at your compile time: deleting one would not deprecate it, it would break your build
with a message naming nothing you wrote. A rule retired from the product is carried forward as
`[Obsolete]` instead, so an upgrade tells you what happened.

## The rest of the product

* [`JustDummies`](https://www.nuget.org/packages/JustDummies) — the library, with these analyzers
  bundled in.
* [`JustDummies.Xunit`](https://www.nuget.org/packages/JustDummies.Xunit) — the xUnit v3 adapter.

Built on [`DiagnosticCatalog`](https://www.nuget.org/packages/DiagnosticCatalog), which also publishes
ready-made catalogues for SonarAnalyzer, the .NET `CA` rules, StyleCop, xUnit and others.

## Links

* [Repository](https://github.com/Reefact/just-dummies)
* [The analyzer rules, one page each](https://github.com/Reefact/just-dummies/tree/main/doc/handwritten/for-users/analyzers)
* [Changelog](https://github.com/Reefact/just-dummies/blob/main/JustDummies.DiagnosticCatalog/CHANGELOG.md)
* [License](https://github.com/Reefact/just-dummies/blob/main/LICENSE) — Apache-2.0

## Credits

The package icon is a crash-test dummy by **Magnific**, from
[Flaticon](https://www.flaticon.com/fr/icones-gratuites/crash).
