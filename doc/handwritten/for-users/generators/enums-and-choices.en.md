# Enums and choices

🌍 **Languages:**  
🇬🇧 English (this file) | 🇫🇷 [Français](./enums-and-choices.fr.md)

Four generators cover the case where the value comes from a **known set** rather than a range:
enumerations, explicit pools, elements of an existing collection, and booleans.

## Enumerations

`Any.Enum<TEnum>()` draws one of the members declared on the type:

```csharp
OrderStatus status    = Any.Enum<OrderStatus>().Generate();
OrderStatus notDraft  = Any.Enum<OrderStatus>().DifferentFrom(OrderStatus.Draft).Generate();
OrderStatus openState = Any.Enum<OrderStatus>().Except(OrderStatus.Shipped, OrderStatus.Cancelled).Generate();
OrderStatus terminal  = Any.Enum<OrderStatus>().OneOf(OrderStatus.Shipped, OrderStatus.Cancelled).Generate();
```

The draw stays inside the declared members. It never invents an undeclared numeric value, even
though the CLR would allow one — a dummy that did would be testing your `switch` against a state your
domain does not have.

Exclusions that empty the universe are refused by name, and the analyzer
[JD017](../analyzers/JD017.en.md) reports the constant cases at build time.

## Flags enumerations

For a `[Flags]` enum, a plain draw still yields **one declared member**. Widening that draw is opt-in:

```csharp
// One declared member: None, Read, Write or Delete.
Permissions single = Any.Enum<Permissions>().Generate();

// Any combination of them: Read | Delete, Read | Write | Delete, ...
Permissions combined = Any.Enum<Permissions>().AllowingCombinations().Generate();

// One of two combinations you name: an allow-list is the pool itself, so it needs no opt-in.
Permissions writable = Any.Enum<Permissions>()
                          .OneOf(Permissions.Read | Permissions.Write, Permissions.Write | Permissions.Delete)
                          .Generate();
```

The opt-in is deliberate
([ADR-0020](../../for-maintainers/adr/0020-draw-flag-enum-combinations-behind-an-opt-in.md)). A
`[Flags]` attribute says the members *may* combine, not that every value in your domain does; and a
generator that combined automatically would silently change what existing tests draw the day someone
adds the attribute. Asking for combinations is one call, and it says at the call site that
combinations are part of what this test covers.

What the opt-in settles is what a *plain* draw ranges over, and that is all it settles: naming a
combination in `OneOf` needs none of it, since an allow-list is the pool itself and writing
`Read | Write` there is asking for that exact value. A value no OR of declared members produces —
`(Permissions)16` on the enum above — is refused either way, and reported by
[JD017](../analyzers/JD017.en.md).

## Explicit pools

`Any.OneOf` draws uniformly from values you list:

```csharp
string  currency = Any.OneOf("EUR", "USD", "GBP").Generate();
int     httpPort = Any.OneOf(80, 443, 8080).Generate();
decimal vatRate  = Any.OneOf(0.055m, 0.10m, 0.20m).Generate();

// Pools narrow like anything else.
string notEuro = Any.OneOf("EUR", "USD", "GBP").DifferentFrom("EUR").Generate();
```

Two mistakes are common enough to have their own diagnostics.

**Listing the same constant twice** collapses the duplicate, so the pool is smaller than it reads and
the repeated value weighs nothing extra — [JD025](../analyzers/JD025.en.md).

**Passing generators instead of values** infers a pool of *recipes*, so the draw returns a generator
rather than a value — [JD012](../analyzers/JD012.en.md). Use `Any.Combine` when you meant to compose.

## Elements of an existing collection

`Any.OneOf` takes `params T[]`, so handing it an **array** expands as usual and does what you expect.
Handing it any other collection does not: `T` binds to the collection type itself, and the pool
becomes a single element — that collection:

<!-- jd:allow=JD013 -->
```csharp
List<string> currencies = ["EUR", "USD", "GBP"];

// JD013: a pool of one, whose single element is the list.
IAny<List<string>> wrong = Any.OneOf(currencies);
```

`Any.ElementOf` is the one that draws *from* the collection, whatever its type:

```csharp
List<string> currencies = ["EUR", "USD", "GBP"];

string currency = Any.ElementOf(currencies).Generate();
```

Two overloads exist, for `IReadOnlyList<T>` and `IEnumerable<T>`. The compiler picks the more
specific one whenever the type allows, because a list can be indexed and a general sequence must be
walked; both are supported so a `yield`-returning helper or a LINQ query works without a
`.ToList()` at the call site.

```csharp
List<OrderStatus>       open      = [OrderStatus.Draft, OrderStatus.Submitted];
IEnumerable<OrderStatus> lazyOpen = open.Where(status => status != OrderStatus.Draft);

OrderStatus fromList     = Any.ElementOf(open).Generate();
OrderStatus fromSequence = Any.ElementOf(lazyOpen).Generate();
```

An empty pool admits no value and is refused rather than returning a default.

## Booleans

```csharp
bool flag       = Any.Boolean().Generate();
bool always     = Any.Boolean().True().Generate();
bool never      = Any.Boolean().False().Generate();
bool notTheSame = Any.Boolean().DifferentFrom(true).Generate();
```

`True()` and `False()` exist so a call site that pins the flag still reads like the ones that do not,
which matters in a test where three of four dummies vary and one does not.

`Any.Boolean().Except(true, false)` would empty the domain, and is refused with a message naming
exactly that.

---

[← Generator reference](./README.md) · [Documentation index](../README.md)
