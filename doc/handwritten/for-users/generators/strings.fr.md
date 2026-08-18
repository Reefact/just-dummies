# Chaînes et motifs

🌍 **Langues :**  
🇬🇧 [English](./strings.en.md) | 🇫🇷 Français (ce fichier)

`Any.String()` est le générateur le plus contraint de la bibliothèque, parce que c'est dans les
chaînes que vivent les formats métier. Cette page couvre ses quatre familles de contraintes, la règle
de disposition qui explique comment elles interagissent, `Any.Char()`, et la génération pilotée par
motif avec `Any.StringMatching`.

## À quoi ressemble une chaîne non contrainte

<!-- jd:allow=JD030 -->
```csharp
string anything = Any.String().Generate();   // 0 à 1024 caractères, n'importe où dans l'ASCII
string nonEmpty = Any.String().NonEmpty().Generate();
```

Un tirage non contraint produit **0 à 1024 caractères pris dans tout l'ASCII** — caractères de
contrôle, tabulations et retours à la ligne compris. Il peut donc être vide, long, et plein de choses
que votre code n'aimera peut-être pas.

**C'est le but, et c'est délibéré.** Un dummy est une valeur sur laquelle le code testé n'a pas eu son
mot à dire ; la restreindre d'avance à du texte court et sage retire précisément la preuve que le
tirage existe pour produire. Un test qui passe avec l'une de ces valeurs a montré quelque chose. Un
test qui passe avec `abc123` n'a rien montré de ce qui arrive à 300 caractères, ou quand un `\r` se
présente.

Alors contraignez — avec les invariants que votre code exige réellement :

```csharp
string reference = Any.String().Printable().WithMaxLength(32).NonEmpty().Generate();
```

`NonEmpty()` quand du contenu est requis, `WithMaxLength(...)` pour la longueur que votre colonne ou
votre contrat autorise, `Printable()` quand un caractère de contrôle n'en fait pas partie. Chacun est
un fait sur le code environnant, écrit là où il doit l'être
([ADR-0074](../../for-maintainers/adr/0074-draw-characters-from-the-whole-of-ascii.fr.md),
[ADR-0075](../../for-maintainers/adr/0075-let-a-declared-maximum-steer-the-size-draw.fr.md)).

## Longueur

<!-- jd:allow=JD030 -->
```csharp
string exact     = Any.String().WithLength(12).Generate();
string ranged    = Any.String().WithLengthBetween(3, 20).Generate();
string atLeast   = Any.String().WithMinLength(8).Generate();
string atMost    = Any.String().WithMaxLength(50).Generate();
string withStuff = Any.String().NonEmpty().Generate();
```

`NonEmpty()` est l'intrus de cette liste : il relève le plancher à un et laisse le plafond là où il
était, si bien qu'une chaîne qui ne porte que lui tire encore toute l'étendue. L'analyzer
[JD030](../analyzers/JD030.fr.md) le dit au site d'appel, sur cette ligne comme sur toute autre chaîne
qui ne déclare aucune longueur.

**Une borne déclarée est la borne obtenue.** `WithMaxLength(50)` tire entre 0 et 50, et
`WithLengthBetween(1000, 5000)` tire sur tout l'intervalle — les deux écritures d'une plage se
comportent identiquement. Avec un minimum seul, le tirage atteint l'étendue par défaut au-dessus :
`WithMinLength(1000)` produit 1000 à 2024.

Tout argument de taille est refusé au-delà de 1 000 000, maxima compris : au-delà, le test voulait un
test de charge, pas un dummy
([ADR-0075](../../for-maintainers/adr/0075-let-a-declared-maximum-steer-the-size-draw.fr.md)).

## Alphabet

L'univers est tout l'ASCII, et **chaque contrainte ci-dessous le rétrécit** — sans aucune exception à
cette règle.

| Famille | Tire | Taille |
| --- | --- | --- |
| *(aucune)* | tout caractère ASCII | 128 |
| `Printable()` | 0x20 à 0x7E, espace inclus | 95 |
| `NonPrintable()` | les contrôles C0 et `DEL` | 33 |
| `Alpha()` | `A-Z a-z` | 52 |
| `Numeric()` | `0-9` | 10 |
| `AlphaNumeric()` | `A-Z a-z 0-9` | 62 |
| `Punctuation()` | imprimable, ni lettre, ni chiffre, ni espace | 32 |
| `Whitespaces()` | l'espace et la tabulation | 2 |
| `Hexadecimal()` | `0-9 A-F a-f` | 22 |

```csharp
string letters      = Any.String().Alpha().WithLength(10).Generate();          // A-Z a-z
string alphanumeric = Any.String().AlphaNumeric().WithLength(10).Generate();   // A-Z a-z 0-9
string digits       = Any.String().Numeric().WithLength(6).Generate();         // 0-9
string symbols      = Any.String().Punctuation().WithLength(4).Generate();     // !"#$%&'()*+,-./ etc.
string sha          = Any.String().Hexadecimal().LowerCase().WithLength(40).Generate();
string anyText      = Any.String().Printable().WithLength(20).Generate();      // aucun caractère de contrôle
string shouting     = Any.String().Alpha().UpperCase().WithLength(4).Generate();
string noDigits     = Any.String().Printable().WithoutNumeric().WithLength(8).Generate();
string custom       = Any.String().WithChars("ACGT").WithLength(20).Generate(); // votre propre vivier
```

Une famille occupe **un seul créneau** : en déclarer une seconde contredit la première, et le conflit
nomme les deux côtés. `WithoutAlpha()` et `WithoutNumeric()` sont différentes — elles **soustraient**
et s'accumulent, donc `WithoutAlpha().WithoutNumeric()` laisse la ponctuation, les blancs et les
contrôles.

Deux choses à savoir. `Punctuation()` est le bloc POSIX `[:punct:]`, donc **plus large** que
`char.IsPunctuation` — ce prédicat lit `+`, `<` et `$` comme des symboles : appuyez-vous sur
l'invariant que votre code exige réellement, pas sur lui. Et l'espace n'en fait délibérément pas
partie : c'est le seul caractère qu'un `Trim()` retire en silence, donc un séparateur fiable ne doit
pas en être un. C'est `Whitespaces()` qui le nomme.

**Aucune famille nommée ne va au-delà de l'ASCII**, et cette borne est là où commencerait la
localisation : un vivier suivant la version d'Unicode du runtime tirerait différemment sur deux
frameworks cibles, contre une garantie que cette bibliothèque vérifie octet par octet. `WithChars` est
la porte de sortie — fournissez le vivier exact et le tirage n'utilise rien d'autre. C'est ainsi qu'on
exprime un alphabet que les familles nommées ne couvrent pas : une séquence d'ADN, un alphabet base
32, du texte accentué, un ensemble de séparateurs autorisés.

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

<!-- jd:allow=JD015,JD006,JD030 -->
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
char letter      = Any.Char().Alpha().Generate();
char upper       = Any.Char().Alpha().UpperCase().Generate();
char digit       = Any.Char().Numeric().Generate();
char punctuation = Any.Char().Punctuation().Generate();
char printable   = Any.Char().Printable().Generate();
char control     = Any.Char().NonPrintable().Generate();
char hex         = Any.Char().Hexadecimal().LowerCase().Generate();
char separator   = Any.Char().OneOf('-', '_', '.').Generate();
char notVowel    = Any.Char().Alpha().LowerCase().Except('a', 'e', 'i', 'o', 'u').Generate();
```

Ce sont les familles que `Any.String()` déclare, elles ont ici le même sens, et le défaut aussi :
**un `Any.Char()` non contraint tire n'importe où dans l'ASCII**, il peut donc très bien vous remettre
un retour chariot ou un NUL. `Printable()` est ce que vous déclarez quand ce n'est pas acceptable ;
`Punctuation()` quand le caractère ne doit pas se lire comme alphanumérique ; `NonPrintable()` quand un
caractère de contrôle est précisément le contre-exemple dont votre test a besoin. Là où l'ensemble est
précis — trois séparateurs autorisés, pas les trente-deux — `OneOf` le dit, et le dit exactement.

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
