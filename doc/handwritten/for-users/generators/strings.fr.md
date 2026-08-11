# Chaînes et motifs

🌍 **Langues :**  
🇬🇧 [English](./strings.en.md) | 🇫🇷 Français (ce fichier)

`Any.String()` est le générateur le plus contraint de la bibliothèque, parce que c'est dans les
chaînes que vivent les formats métier. Cette page couvre ses quatre familles de contraintes, la règle
de disposition qui explique comment elles interagissent, `Any.Char()`, et la génération pilotée par
motif avec `Any.StringMatching`.

## À quoi ressemble une chaîne non contrainte

```csharp
string anything = Any.String().Generate();   // 0 à 16 lettres et chiffres ASCII
string nonEmpty = Any.String().NonEmpty().Generate();
```

Un tirage non contraint produit **0 à 16 lettres et chiffres ASCII** : il peut donc être vide.
Chaînez `NonEmpty()` dès que le code environnant exige du contenu — ce qui est le cas la plupart du
temps, et qui est exactement le genre d'invariant qu'une contrainte sert à exprimer.

## Longueur

```csharp
string exact     = Any.String().WithLength(12).Generate();
string ranged    = Any.String().WithLengthBetween(3, 20).Generate();
string atLeast   = Any.String().WithMinLength(8).Generate();
string atMost    = Any.String().WithMaxLength(50).Generate();
string withStuff = Any.String().NonEmpty().Generate();
```

Une longueur supérieure à 1 000 000 est refusée : au-delà, le test voulait un test de charge, pas un
dummy
([ADR-0029](../../for-maintainers/adr/0029-let-a-size-maximum-cap-without-steering-the-draw.fr.md)).

## Alphabet

Six contraintes décident des caractères autorisés :

```csharp
string letters      = Any.String().Alpha().WithLength(10).Generate();          // A-Z a-z
string alphanumeric = Any.String().AlphaNumeric().WithLength(10).Generate();   // A-Z a-z 0-9
string digits       = Any.String().Numeric().WithLength(6).Generate();         // 0-9
string shouting     = Any.String().Alpha().UpperCase().WithLength(4).Generate();
string quiet        = Any.String().Alpha().LowerCase().WithLength(4).Generate();
string custom       = Any.String().WithChars("ACGT").WithLength(20).Generate(); // votre propre vivier
```

`WithChars` est la porte de sortie : fournissez le vivier exact et le tirage n'utilise rien d'autre.
C'est ainsi qu'on exprime un alphabet que les familles fournies ne couvrent pas — une séquence
d'ADN, un alphabet base 32, un ensemble de séparateurs autorisés.

## Forme : préfixes, suffixes, fragments

```csharp
string reference = Any.String().StartingWith("ORD-").WithLength(12).Generate();
string filename  = Any.String().EndingWith(".txt").WithMaxLength(30).Generate();
string path      = Any.String().Alpha().Containing("admin").WithMinLength(20).Generate();
```

## Comment fonctionne la disposition

Les chaînes sont **construites pour satisfaire** les contraintes, et non générées puis filtrées. La
disposition est toujours :

```text
préfixe + remplissage + valeurs contenues + remplissage + suffixe
```

Deux conséquences en découlent, et elles expliquent presque toutes les surprises :

**Les fragments ne se chevauchent jamais.** Le budget de longueur qu'ils réclament est la somme
simple de leurs longueurs. Un préfixe de quatre caractères plus un suffixe de quatre en exige au
moins huit : `WithLength(6)` avec les deux est donc refusé, plutôt que de réutiliser silencieusement
des caractères.

**Un fragment doit appartenir à l'alphabet déclaré.** Déclarer des chiffres seuls puis exiger un
préfixe alphabétique est une contradiction, pas un élargissement. Ces deux cas sont refusés au moment
même de leur déclaration, avec un message nommant les deux côtés :

<!-- jd:allow=JD015,JD006 -->
```csharp
Any.String().WithLength(3).StartingWith("ORD-");  // la longueur ne peut pas contenir le préfixe
Any.String().Numeric().StartingWith("ORD-");      // « ORD- » n'est pas numérique
```

L'analyzer [JD015](../analyzers/JD015.fr.md) signale les deux à la compilation dès que les arguments
sont constants : l'échec arrive donc généralement avant même l'exécution du test.

## Appartenance et exclusion

<!-- jd:allow=JD029 -->
```csharp
string currency = Any.String().OneOf("EUR", "USD", "GBP").Generate();
string status   = Any.String().OneOf(["draft", "sent", "paid"]).Generate();
string notDraft = Any.String().OneOf("draft", "sent", "paid").DifferentFrom("draft").Generate();
string notEmpty = Any.String().WithLengthBetween(1, 5).Except("aaa", "bbb").Generate();
```

`OneOf` est la seule contrainte qui **remplace** la disposition au lieu de la façonner : c'est vous
qui fournissez les valeurs, le tirage est donc un choix uniforme parmi elles, et toute autre
contrainte restreint cet ensemble au lieu de construire une chaîne.

Pour cette raison, déclarez un ensemble de valeurs **en premier**. Les contraintes qui se
contredisent en leurs propres termes sont refusées dès leur déclaration — avant qu'un ensemble de
valeurs ne puisse les réinterpréter comme un filtre.

Les exclusions sont honorées par un retirage **borné** : exclure presque tout ce qu'un petit domaine
peut produire se termine donc par une `AnyGenerationException` explicite, et non par un blocage
([ADR-0012](../../for-maintainers/adr/0012-meet-string-exclusions-with-a-bounded-redraw.fr.md)).

## Caractères

`Any.Char()` porte la famille de l'alphabet et celle de l'appartenance :

```csharp
char letter    = Any.Char().Alpha().Generate();
char upper     = Any.Char().Alpha().UpperCase().Generate();
char digit     = Any.Char().Numeric().Generate();
char separator = Any.Char().OneOf('-', '_', '.').Generate();
char notVowel  = Any.Char().Alpha().LowerCase().Except('a', 'e', 'i', 'o', 'u').Generate();
```

## Motifs

`Any.StringMatching` génère une valeur **à partir** d'un motif au lieu de tester des candidats contre
lui, ce qui lui permet de garantir la correspondance. Une chaîne comme une `Regex` sont acceptées :

```csharp
string sku       = Any.StringMatching(@"[A-Z]{3}-\d{4}").Generate();
string reference = Any.StringMatching(new Regex(@"ORD-\d{8}")).Generate();
string flag      = Any.StringMatching("(true|false)").Generate();
```

### Constructions acceptées

| Construction | Exemple |
| --- | --- |
| littéraux | `abc` |
| n'importe quel caractère | `.` |
| classes de caractères et intervalles | `[A-Z]`, `[aeiou]`, `[^0-9]` |
| classes abrégées | `\d` `\D` `\w` `\W` `\s` `\S` |
| échappements | `\t` `\n` `\r` `\f` `\v` `\a` `\e` |
| quantificateurs | `*` `+` `?` `{3}` `{2,5}` `{2,}` |
| groupements | `(…)`, `(?:…)`, `(?<nom>…)` |
| alternation | `a|b` |
| ancres aux extrémités | `^…$` |

### Constructions refusées

Tout ce qui n'est pas **régulier** ne peut pas être construit par un automate fini : c'est donc
refusé immédiatement par une `UnsupportedRegexException` nommant la construction et sa position —
jamais mal généré :

| Refusé | Pourquoi |
| --- | --- |
| références arrière, groupes d'équilibrage `(?<a-b>…)` | ils exigent la pile de captures |
| anticipation `(?=…)`, `(?!…)` | non régulier |
| rétro-anticipation `(?<=…)`, `(?<!…)` | non régulier |
| groupes atomiques `(?>…)` | non régulier |
| groupes conditionnels `(?(…)…)` | non régulier |
| commentaires en ligne `(?#…)`, options de groupe `(?i…)` | ne font pas partie du langage généré |
| une ancre hors extrémité | `^` et `$` n'ont de sens qu'au début et à la fin du motif, ou d'une branche d'alternation de premier niveau |

Élargir cet ensemble supposerait une dépendance à un automate d'expressions régulières ; la décision
de garder un analyseur maison et de refuser bruyamment est
[ADR-0008](../../for-maintainers/adr/0008-generate-strings-from-a-home-grown-regular-subset.fr.md).

### Ce que l'on peut encore contraindre

Un `AnyPattern` ne porte que `Except` et `DifferentFrom` :

```csharp
string sku = Any.StringMatching(@"[A-Z]{3}-\d{4}").DifferentFrom("ABC-0000").Generate();
```

Les contraintes de longueur, d'alphabet ou de préfixe sont volontairement absentes : les appliquer
reviendrait à construire une valeur dans l'intersection de deux langages réguliers. Mettez plutôt
l'exigence dans le motif — c'est déjà l'endroit le plus précis pour l'énoncer.

Une valeur générée correspond forcément à son motif, grâce à un retirage borné là où la seule
construction ne peut pas le garantir
([ADR-0027](../../for-maintainers/adr/0027-guarantee-a-generated-regex-value-matches-by-bounded-redraw.fr.md)).

---

[← Référence des générateurs](./README.fr.md) · [Sommaire de la documentation](../README.fr.md)
