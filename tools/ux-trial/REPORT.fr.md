# Essai d'expérience développeur — rapport final

🌍 **Langues :** [English](./REPORT.md) | 🇫🇷 Français (ce fichier)

**Périmètre.** Cet essai a mis JustDummies et son scaffolder `any` à l'épreuve comme les découvre un
premier utilisateur. Vingt domaines métier inventés ont chacun été modélisés avec des gardes de
constructeur ordinaires, scaffoldés avec `any`, puis testés avec xUnit. Les domaines sont listés au
[§11](#11-résultats-par-domaine). Les packages empaquetés `1.0.0-uxtrial.1` ont été consommés comme
ceux d'un vrai utilisateur, depuis un flux local, en dehors de `JustDummies.sln` (voir
[`README.md`](README.md)). Les participants ont appris l'API uniquement à partir de la documentation
utilisateur et de la référence PublicAPI livrée. Ils ont lancé `any generate` sur chaque type du
domaine, conservé les récapitulatifs tels quels dans leur `NOTES.md`, et laissé des sondes délibérées
et des tests `Bug_…`. Chaque constat a ensuite été dédoublonné en une cause racine. Chaque cause racine
est passée par un vérificateur contradicteur, chargé de la réfuter : la confronter aux ADR, à la
spécification et à la documentation utilisateur, et laisser une reproduction minimale sous
[`repros/`](repros/).

**Limites.** Participants et vérificateurs sont des agents, pas des développeurs humains. Les scores
d'aisance sont un signal relatif entre domaines, pas une mesure. Chaque cause racine a eu exactement un
vérificateur, et pour le domaine 20, deux éléments triés ont été vérifiés par lecture plutôt que par
une nouvelle exécution (ils sont signalés). Chaque élément ci-dessous est jugé à l'aune des deux
fondamentaux et de « The name is the scope » dans [`CLAUDE.md`](../../CLAUDE.md), ainsi que des
principes convenus avec le mainteneur ci-dessous. Là où ces principes l'emportent sur le verdict d'un
vérificateur, l'élément le dit.

> **Principes convenus avec le mainteneur**
>
> * **La ligne d'honnêteté de le scaffolder.** `any` produit une *base de travail* que le développeur
>   possède et modifie ; elle peut être incomplète. C'est acceptable tant que la CLI dit où — `to verify`,
>   un TODO qui ne compile pas. Un récapitulatif qui dit « inferred » ou « guard », ou un ✓, sur une
>   recette fausse est un bogue. Une lacune signalée honnêtement n'en est pas un.
> * **Les invariants entre paramètres** signalés `to verify` sont le comportement attendu de
>   le scaffolder (ADR-0060), pas un défaut. Les améliorations ne sont bienvenues que si elles guident
>   la retouche manuelle sans rendre la génération plus astucieuse.
> * **Un refus n'est une frontière assumée que lorsqu'une ADR ou la documentation le décide.** Un refus
>   clair mais non décidé dont un vrai domaine a besoin est un candidat à une décision, jamais un bogue.

Les identifiants sont stables pour que les issues puissent les citer : `CLI-`, `CORE-`, `AN-`, `XU-`,
`DOC-`, suivis de `B` (bogue), `D` (défaut), `G` (lacune d'inférence, signalée honnêtement), `F` (faux
négatif d'un analyseur) ou `C` (candidat à une décision).

---

## 1. Synthèse

| | |
|---|---|
| Domaines | 20 |
| Sessions `any generate` enregistrées | 143 (plusieurs couvrent plus d'un type) |
| Constats bruts remontés par les participants | 203 |
| Causes racines vérifiées (verdicts des vérificateurs) | 29, plus 7 éléments triés pour le domaine 20 et 11 vérifications factuelles d'affirmations qui n'étaient pas des bogues |
| **Valeurs tirées ayant violé une contrainte déclarée** | **0** |
| Bogues | 9 nouveaux (6 `cli`, 3 `core`), plus 1 déjà ouvert (#296) |
| Défauts (messages, rapports, documentation) | 5 `cli`, 10 `core`, 5 `analyzers` (plus 2 faux négatifs), 1 `xunit`, 13 lignes de documentation transverses |
| Candidats à une décision | 21 (jamais comptés comme bogues) |
| Score d'aisance moyen | 3,85 / 5 (dix-sept domaines à 4, trois à 3) |

La bibliothèque n'a jamais franchi sa ligne de correction. Sur 20 domaines, aucune valeur tirée n'a
violé une contrainte déclarée. Les contraintes contradictoires ont échoué immédiatement. Chaque
rejeu synchrone depuis la graine qui a été tentée a reproduit son exécution. Les problèmes
sérieux se situent dans le scaffolder. Dans quelques formes précises, `any` se porte garant d'un code
qu'il n'a pas vérifié : il annonce « inferred », ou ✓, sur un fichier qui compile puis produit des
valeurs rejetées, une constante ou un dépassement de pile. Cela rompt la ligne d'honnêteté fixée par
le mainteneur (voir les principes ci-dessus). C'est aussi l'échec que l'ADR-0083 classe comme le plus
coûteux.

### À corriger en premier

| Rang | ID | Portée | Gravité | Pourquoi |
|---|---|---|---|---|
| 1 | [CLI-B1](#cli-b1) | `cli` | élevée | Une garde qui lève une exception sous la première écriture d'état n'est ni lue ni signalée. Le récapitulatif dit « inferred » (ou « guard » avec une borne perdue), le fichier compile, et chaque tirage est rejeté. Rencontré dans 4 domaines. |
| 2 | [CLI-B2](#cli-b2) | `cli` | élevée | `any generate <Enum>` affiche ✓ et émet `return new TEnum();` : une constante, et une valeur non déclarée quand l'énumération n'a pas de membre zéro. Rencontré dans 6 domaines. |
| 3 | [CLI-B3](#cli-b3) | `cli` | élevée | Un agrégat autoréférent est scaffoldé en `new AnyT()` dans le propre constructeur de `AnyT`. Le récapitulatif l'annonce « inferred », et la première instanciation fait déborder la pile et tue l'hôte de test. |
| 4 | [CLI-B4](#cli-b4) | `cli` | élevée | Une garde atteinte via une variable locale dérivée du paramètre est silencieuse. Le récapitulatif dit « inferred » alors que 200 tirages sur 200 sont rejetés. La spécification de maintenance la liste comme un résidu, et c'est là le défaut. |
| 5 | [CORE-B1](#core-b1) | `core` | moyenne | Hors d'une portée, un premier tirage dans un helper attendu par `await` perd sa graine. Le message d'échec nomme alors une graine qui ne rejoue pas l'exécution. |
| 6 | [CORE-B2](#core-b2) | `core` | moyenne | Les messages d'échec de `As(...)`/`Combine(...)` affichent les chaînes tirées brutes, NUL et ESC compris, contrairement à l'ADR-0075. |
| 7 | [CLI-B5](#cli-b5) | `cli` | moyenne | `--format json` rapporte `Scaffolded` avec `failed: 0` pour une écriture refusée, alors que l'exécution se termine avec le code 1. Cela rompt l'ADR-0071. |
| 8 | [CORE-D1](#core-d1) | `core` | moyenne | Les messages de conflit décimaux énoncent des contre-vérités : ils présentent une borne stricte comme inclusive, et nomment « the only value left » un point qu'aucune des deux bornes n'admet. |
| 9 | [XU-D1](#xu-d1) | `xunit` | moyenne | Aucune page utilisateur ne montre `using JustDummies.Xunit;`, si bien que 13 domaines sur 20 ont raté leur premier build. La cause est un prélude caché dans le compilateur d'exemples de la documentation. |
| 10 | [AN-D1](#an-d1) | `analyzers` | moyenne | JD014 et JD017 signalent la même valeur d'énumération non déclarée sur la même étendue, contrairement au « one phenomenon, one id » de l'ADR-0078. |

Ensuite : [CLI-B6](#cli-b6) (l'en-tête nomme une commande de régénération qui échoue) et
[CORE-B3](#core-b3) (`Between(-ε, ε)` ne tire jamais que 0).

---

## 2. `cli` — le scaffolder `any` (JustDummies.Cli + JustDummies.GenAny)

### Bogues

<a id="cli-b1"></a>
#### CLI-B1 — Une garde qui lève une exception sous la première écriture d'état n'est ni lue ni marquée (élevée)

Le lecteur de gardes s'arrête à la première affectation d'un champ ou d'une propriété. Seule cette
instruction est examinée à la recherche d'un `throw`. Aucune des instructions suivantes n'est visitée,
si bien qu'une garde écrite là ne reçoit pas la marque `unread guards`. L'ADR-0083 et le §5.6 de la
spécification exigent précisément cette marque (« a statement that throws is a guard whatever its
shape »). Le §9 de la spécification liste les formes connues pour échapper au lecteur, et une garde
sous une écriture d'état n'en fait pas partie.

```csharp
public FreightRate(decimal rate, string currency) {
    Rate = rate;
    if (currency.Length != 3) { throw new ArgumentException("ISO 4217", nameof(currency)); }
    Currency = currency;
}
// any: currency  string  Any.String().NonEmpty()        — "2 of 2 parameters inferred", compiles
// ThrowIfNegativeOrZero(rack); Aisle = aisle; ThrowIfGreaterThan(rack, 99);
// any: rack  int  Any.Int32().Positive()  guard         — the <= 99 bound is lost under "guard"
```

Le vérificateur a tiré 200 valeurs par scaffolding. Chaque variante a été rejetée 200 fois sur 200.
Une expression `throw` ou une expression `switch` est correctement marquée lorsqu'elle *est* la
première écriture d'état. Elle est manquée dès qu'une affectation antérieure la précède, ce qu'ont
rencontré les domaines 06 et 14.

| | |
|---|---|
| Domaines | 06 (`BinLocation.level`, expression `switch` après deux écritures), 07 (`FreightRate.currency`), 14 (`PromoCode.maxUses`, expression `throw` après cinq écritures), 19 (`RoleSet`, vérification sur la copie dans le champ) |
| Repro | [`repros/cli-silent-guard-miss`](repros/cli-silent-guard-miss) (variantes a1, a2, d) |
| Source | `JustDummies.GenAny/Guards.cs:162-169` (le `break` à la première affectation d'état), `JustDummies.GenAny.UnitTests/GuardBoundaryTests.cs:216-236` (fige l'expression mais ne vérifie jamais la provenance, et sa justification est fausse) |
| Test rouge | `JustDummies.GenAny.UnitTests`, à côté de `AThrowingCheckAfterTheFirstAssignmentIsNotAGuard`. Scaffolder `kept = value; if (value <= 0) { throw …; }` et vérifier `Provenance.HasFlag(Provenance.UnreadGuards)`. Ajouter un second cas, `ThrowIfNegativeOrZero(v); Kept = v; ThrowIfGreaterThan(v, 99);`, et vérifier `UnreadGuards` plutôt qu'un simple `Guard`. |
| Correctif | À la première écriture d'état qui n'est pas une garde, cesser de *lire* les gardes mais continuer à *parcourir* le corps. Marquer `UnreadGuards` chaque paramètre qu'une instruction ultérieure qui lève une exception (ou un appel dont le résultat est ignoré) mentionne, pour que la ligne bloquante existante de l'ADR-0083 se déclenche. Cela refuse plus bruyamment sans rien inférer de nouveau, ce qui reste dans le cadre de l'ADR-0046. |
| Tests `Bug_` de l'essai | `06 Bug_Scaffolded_level_recipe_for_BinLocation_ignores_its_guard`, `07 Bug_Scaffolded_currency_recipe_ignores_a_guard_written_below_a_property_assignment`, `14 Bug_Scaffolded_AnyPromoCode_misses_the_throw_expression_guard_on_maxUses`, `19 Bug_Scaffolded_AnyRoleSet_ignores_a_distinctness_guard_written_against_a_field`. **Ils ne passeront pas au vert d'eux-mêmes.** 06 et 07 rejouent la recette émise figée. 14 et 19 utilisent le fichier tel qu'émis. Après le correctif, confirmer en relançant le scaffolding : le récapitulatif doit afficher `to verify, unread guards` et le build doit s'arrêter sur la ligne TODO. Retirer ou réécrire ensuite ces tests. |

<a id="cli-b2"></a>
#### CLI-B2 — `any generate <Enum>` annonce ✓ et émet `return new TEnum();` (élevée)

Roslyn synthétise un constructeur sans paramètre pour une énumération. L'exclusion des constructeurs
synthétisés ne teste que `TypeKind.Struct`, si bien qu'une énumération est « scaffoldée » en
`new TEnum()`. Cela renvoie toujours `(TEnum)0`. La valeur est non déclarée quand l'énumération n'a
pas de membre zéro, et constante quand elle en a un. Le récapitulatif affiche
`✓ AnyCurrency.cs — no constructor parameters to infer.`, et l'exécution sort avec le code 0. Le §5.1.3
de la spécification promet que le chemin sans paramètre produit une valeur *valide*, et aucune ligne
de la spécification ni aucune ADR ne traite une énumération comme une cible.

```csharp
public enum Currency { Eur = 1, Usd = 2, Gbp = 3 }
// AnyCurrency.Generate() { return new Currency(); }  → 200 draws, 1 distinct value, Enum.IsDefined == false
```

| | |
|---|---|
| Domaines | 01, 03, 04, 05, 06, 19 |
| Repro | [`repros/cli-enum-scaffold`](repros/cli-enum-scaffold) (avec son propre `Bug_scaffolded_enum_generator_draws_an_undeclared_constant`) |
| Source | `JustDummies.GenAny/Scaffolder.cs:283` (`ChosenConstructor`) et `:251-252` (`EligibleFactories`) : l'exclusion du constructeur implicite se limite à `TypeKind.Struct` |
| Test rouge | `JustDummies.GenAny.UnitTests/ConstructorChoiceTests.cs` : ajouter `public enum Subject { One = 1, Two = 2 }` à `ATypeTheEmittedFileCouldNotConstructIsRefused`, en vérifiant un statut de refus et `outcome.File == null`. Aujourd'hui, le statut est `Scaffolded`. |
| Correctif | Refuser `TypeKind.Enum` avant le choix du constructeur, avec un message qui nomme `Any.Enum<TEnum>()` (sortie 1, rien d'écrit). À défaut, émettre un générateur qui délègue à `Any.Enum<TEnum>()`. Choisir entre les deux est une petite décision de spécification pour les §5.1/§7, plus le jumeau français. Au minimum, étendre l'exclusion des structures aux énumérations. |
| Tests `Bug_` de l'essai | `01 Bug_Scaffolded_AnyTransferStatus_never_varies`, `01 Bug_Scaffolded_AnyChargeBearer_draws_an_undeclared_enum_value`, `03 Bug_Scaffolded_AnyResponsibility_draws_more_than_the_zero_member`, `04 Bug_Scaffolded_AnyShiftKind_draws_an_undeclared_member`, `05 Bug_Scaffolded_AnyProjectionMethod_draws_an_undeclared_enum_value`, `06 Bug_Scaffolded_AnyPalletType_draws_an_undeclared_member`, `06 Bug_Scaffolded_AnyReceiptStatus_never_varies`, `19 Bug_Scaffolded_AnyRole_for_an_enum_only_ever_draws_the_default_member`. Ils passent au vert après un nouvel scaffolding avec `--force` seulement si l'option de délégation est retenue. Si c'est l'option du refus, confirmer sur le refus et réécrire ces tests avec `Any.Enum<T>()`. |

<a id="cli-b3"></a>
#### CLI-B3 — Un type autoréférent est scaffoldé en une récursion non bornée (élevée)

```csharp
public Node(string name, IReadOnlyList<Node> children) { … }
// any:  children  IReadOnlyList<Node>  Any.ListOf(new AnyNode())     ✓ AnyNode.cs — 2 of 2 parameters inferred
// public AnyNode() : this(name: AnyValidName(), children: AnyValidChildren()) { }
// dotnet test → "Stack overflow." exit 134, zero tests ran
```

La récursion est immédiate : le constructeur public lui-même est récursif, si bien que même
`new AnyNode().WithChildren(...)` ne peut y échapper. Un `Employee(string name, Employee? manager)`
direct donne le même résultat, et le récapitulatif perd en plus le `?`. La détection de cycle de
`GeneratorFor` ne contient jamais le type cible. Les remarques de `GeneratorFor.cs:38-40` disent que
cette garde « belongs with §5.4 », mais elle n'a jamais été implémentée. Ni la spécification ni
aucune ADR ne mentionnent l'autoréférence.

| | |
|---|---|
| Domaines | 05 (nomenclature récursive) |
| Repro | [`repros/cli-recursive-aggregate`](repros/cli-recursive-aggregate) |
| Source | `JustDummies.GenAny/GeneratorFor.cs:270` (`Draw` racine avec un `underway` vide), `:366` (détection de cycle), `:431-450` (`Composed` écrit `new AnyT()` inconditionnellement) |
| Test rouge | `JustDummies.GenAny.UnitTests/CompositionTests.cs` : scaffolder `Node(string name, IReadOnlyList<Node> children)` et vérifier que `children` n'est pas résolu en une expression contenant `new AnyNode()`, et qu'il est rapporté comme non résolu ou à vérifier. Ajouter un cas jumeau pour `Employee(string, Employee?)`. |
| Correctif | Amorcer la détection de cycle avec la cible, sous la forme `Draw(type, MaximumDepth, [target])`. Un paramètre qui atteint la cible directement, via un nullable ou via une collection devient un TODO explicite, avec pour raison dans le récapitulatif « self-referencing: recursion needs a depth bound you write ». Émettre à la place une liste vide, un null ou un paramètre de profondeur serait un générateur plus astucieux, et demanderait une ADR ou un amendement du §5.4 plutôt qu'un correctif. |
| Tests `Bug_` de l'essai | Aucun : le domaine 05 a écrit `WithDepth` à la main. Confirmer sur la repro : scaffolder `Node` à nouveau et vérifier le TODO. |

<a id="cli-b4"></a>
#### CLI-B4 — Une garde atteinte via une variable locale dérivée est silencieuse alors que le récapitulatif dit « inferred » (élevée)

```csharp
public AircraftRegistration(string mark) {
    bool valid = mark switch { null => false, { Length: > 7 } => false, _ => Hyphenated.IsMatch(mark) };
    if (!valid) { throw new ArgumentException(…, nameof(mark)); }
    Mark = mark!;
}
// any: "1 of 1 parameters inferred", no "to verify"; the file compiles; most draws are rejected
```

`MarkIfItRejects` ne marque que les paramètres que l'instruction qui lève nomme syntaxiquement, et
`if (!valid)` n'en nomme aucun. Le vérificateur a reproduit quatre formes, avec 197 à 200 tirages
rejetés sur 200 (l'une lève `FormatException` à la place) : `Match m = Regex.Match(value, …)`,
`bool ok = value.StartsWith(…)`, `int parsed = int.Parse(week)` et un `switch` vers `bool`.

**Note de classement.** Le vérificateur a classé ce cas comme le résidu documenté du §9 (« a local copy
of the parameter »), que `GuardBoundaryTests.cs:167` fige. Selon la ligne d'honnêteté de
le scaffolder, un récapitulatif qui dit « inferred » alors que la recette est fausse est un bogue, quoi
qu'en dise une page. C'est donc la liste du §9 de la spécification qui est le défaut. De plus, le
correctif *marque* la garde sans la *lire*, ce qui reste dans le cadre de l'ADR-0046.

| | |
|---|---|
| Domaines | 04 (`BatchCode`, un `Match` sur une regex), 16 (`AircraftRegistration`) |
| Repro | [`repros/cli-silent-guard-miss`](repros/cli-silent-guard-miss) (variantes b1–b4) |
| Source | `JustDummies.GenAny/Guards.cs:512-515` (`MarkIfItRejects` ne voit que les paramètres mentionnés syntaxiquement) ; spécification `justdummies-tool.md` §9 (liste le résidu) ; `JustDummies.GenAny.UnitTests/GuardBoundaryTests.cs:167` |
| Test rouge | `JustDummies.GenAny.UnitTests/GuardBoundaryTests.cs` : scaffolder `bool ok = value.StartsWith("LOT-"); if (!ok) { throw …; }` sans écriture d'état qui le précède, et vérifier `UnreadGuards` sur `value`. |
| Correctif | Quand la condition d'une instruction qui lève atteint un paramètre via une variable locale initialisée à partir de ce paramètre (un flux de données d'une seule étape), marquer ce paramètre `UnreadGuards`. Retirer le résidu du §9 de la spécification (EN/FR). La page utilisateur de la CLI doit nommer le résidu qui subsiste. |
| Tests `Bug_` de l'essai | `04 Bug_Scaffolded_AnyBatchCode_recipe_misses_the_format_guard_silently` (rejoue la recette figée, il reste donc rouge par construction), `16 Bug_Scaffolded_AnyAircraftRegistration_as_emitted_draws_marks_the_constructor_rejects` (utilise `AsScaffolded/`, également figé). Confirmer en relançant le scaffolding, puis retirer les deux. |

<a id="cli-b5"></a>
#### CLI-B5 — `--format json` rapporte une écriture refusée comme `Scaffolded` (moyenne)

Quand le fichier de sortie existe déjà et que `--force` n'est pas fourni, stderr affiche
`✗ … already exists. Re-run with --force`, et l'exécution se termine avec le code 1. Stdout, en revanche, porte
`{"status":"Scaffolded","files":[],…,"summary":{"scaffolded":1,"failed":0}}`. Le shell recopie le
statut du *moteur* dans le rapport. L'ADR-0071 et le §6.1 de la spécification exigent que le résumé
soit « checkable against the rows rather than believed », avec des codes de sortie qui ne bougent pas.

| | |
|---|---|
| Domaines | 01, 19 |
| Repro | [`repros/cli-json-status`](repros/cli-json-status) |
| Source | `JustDummies.Cli/GenerateCommand.cs:216-221`, `JustDummies.Cli/ScaffoldReport.cs:25-27`, `JustDummies.Cli/RunReport.cs:44-45` |
| Test rouge | `JustDummies.Cli.UnitTests/GenerateCommandTests.cs`, `JsonRecordsAnExistingFileAsAFailure` : écrire `AnyOrder.cs` au préalable, lancer avec `Format = "json"`, et vérifier le code de sortie 1, `status != "Scaffolded"`, `summary.scaffolded == 0`, `summary.failed == 1`, et que le fichier n'a pas été touché. |
| Correctif | Donner à la branche d'écriture refusée un statut propre au shell (par ex. `"FileExists"`). Conserver le chemin dans `files` avec `written: false`. Documenter le statut aux §6.1/§7 de la spécification (EN/FR). |
| Tests `Bug_` de l'essai | Aucun. Confirmer avec la seconde exécution de la repro. |

<a id="cli-b6"></a>
#### CLI-B6 — La recherche par nom court liste des candidats que C# ne pourrait jamais lier, et l'en-tête nomme une commande qui échoue (moyenne)

`any generate Measurement` refuse pour ambiguïté à cause de `System.Diagnostics.Metrics.Measurement<T>`,
dont l'arité est 1. `any generate Scope` liste `JustDummies.Xunit.ReproducibleAttribute.Scope`, un type
`internal`, et une `ref struct`. Après un `any generate Ambig.Domain.Meter` réussi, la ligne 2 du
fichier généré dit ``// `any generate Meter --force` overwrites it.``, mais cette commande échoue sur
le même refus pour ambiguïté. Refuser une ambiguïté *réelle* est spécifié (§3.2) et le message de
refus est clair. Ni l'ensemble de candidats trop large ni l'en-tête erroné ne sont spécifiés.

| | |
|---|---|
| Domaines | 03 (`Claim`), 09 (`Measurement`), 12 (en-tête), 17 (en-tête, packages transitifs), 19 (types inaccessibles) |
| Repro | [`repros/cli-ambiguous-names`](repros/cli-ambiguous-names) |
| Source | `JustDummies.GenAny/TypeLookup.cs` (`Matching`/`Named`), `Scaffolder.cs:53-62`, `GeneratorEmitter.cs:64`, `EntryPointEmitter.cs:83` |
| Test rouge | `JustDummies.GenAny.UnitTests/TypeLookupTests.cs`. (1) Source `Shop.Measurement`, plus des métadonnées avec un `Other.Measurement` interne et un `Other.Measurement<T>` public : `"Measurement"` se résout en `Shop.Measurement`. (2) Pour un nom court ambigu donné en entier, la ligne 2 de l'en-tête nomme une commande que `TypeLookup.Find` résout exactement vers ce type. |
| Correctif | Filtrer les candidats par construction : arité 0 en l'absence de liste d'arguments de type, et accessibles depuis `compilation.Assembly`. Écrire dans l'en-tête une graphie qui se résout (le nom complet quand le nom court est ambigu), et mettre à jour les fichiers de référence (golden files). La question de privilégier les types du projet sur ceux du framework relève de [CLI-C3](#cli-c3). |
| Tests `Bug_` de l'essai | Aucun. |

<a id="cli-b7"></a>
#### CLI-B7 — `any generate` sur un projet non restauré prétend que le projet ne référence pas JustDummies (déjà ouvert)

Trouvé pendant le pilote et ouvert sous
[#296](https://github.com/Reefact/just-dummies/issues/296). Il est listé ici pour que le train `cli`
le porte. Tous les domaines ont lancé `dotnet restore` d'abord à cause de lui.

### Défauts (messages, formulation, récapitulatif/JSON)

<a id="cli-d1"></a>
**CLI-D1 — « to verify » ne dit jamais quelle garde n'a pas été lue, et cite le « §9 » de la spécification de maintenance (moyenne).**
Le commentaire émis dit `// TODO(any): 'string value' may be guarded by something the scaffolder
could not read (§9).` Quand un paramètre a trois gardes, rien n'indique laquelle n'a pas été lue. Le
récapitulatif et la provenance JSON n'en disent pas plus, et « §9 » renvoie à « Non-goals for v1.0 »
dans une page de maintenance vers laquelle aucune page utilisateur ne renvoie. Domaines : 02, 09, 11, 12. Repro :
[`repros/cli-todo-wording`](repros/cli-todo-wording). Source : `GeneratorEmitter.cs:263-267`
(`WriteVerify`), `GuardReading.cs:54` (ne stocke que des noms, jamais le nœud), `Cli/Recap.cs:132,179`.
Test rouge (`JustDummies.GenAny.UnitTests`) : un paramètre avec une garde `Length > 7` lue et une garde
`Regex.IsMatch` non lue. Vérifier que le TODO contient le texte ou la ligne de la garde non lue et ne
contient pas « §9 ». Ajouter un jumeau `Recap` dans `JustDummies.Cli.UnitTests`. Correctif : conserver
l'emplacement de la première garde non lue et un extrait d'une ligne par paramètre, l'afficher dans le
TODO, le récapitulatif et le JSON, et remplacer « §9 » par un lien vers la page utilisateur de la CLI.
Le texte du commentaire est spécifié au §5.6, donc la spécification change avec lui. C'est aussi
l'idée d'amélioration (b) de [CLI-E1](#cli-e1).

<a id="cli-d2"></a>
**CLI-D2 — « Delete that one line, and you are done » est faux quand la bonne base a un autre type de builder (moyenne).**
Remplacer `Any.String()…` par `Any.StringMatching(...)` (qui renvoie `AnyPattern`), ou par `.As(...)`
(qui renvoie `IAny<T>`), échoue avec `CS0029`. Le type de retour de `AnyValidX()` et la surcharge
`WithX(Func<TBuilder, TBuilder>)` doivent être retypés à la main. C'est la friction la plus répandue :
16 domaines (01, 02, 03, 04, 05, 06, 07, 10, 11, 13, 14, 16, 17, 18, 19, 20). Le scaffolder a raison de
laisser au développeur une base à modifier (voir les principes en tête). Le défaut, c'est la page et le
§5.6 de la spécification qui promettent une résolution en une ligne. Source :
`doc/handwritten/for-users/packages/justdummies-cli.en.md:82-91` et le §5.6 de la spécification.
Correctif : dire ce qu'il faut retyper d'autre quand le remplacement a un autre type, avec un exemple
compilé dans `JustDummies.Documentation.UnitTests`. À défaut, émettre la fabrique en `IAny<T>` là où
aucune surcharge d'affinage n'en dépend, ce qui est une décision de spécification.

<a id="cli-d3"></a>
**CLI-D3 — Un paramètre de type référence nullable perd son `?` dans la méthode qui fixe sa valeur, le récapitulatif et le JSON (moyenne).**
Pour `Party(Bic? agent, string? note, int? priority)`, le scaffolding émet `WithAgent(Bic value)`. La
voie documentée « toujours null », `.WithAgent((Bic?)null)`, déclenche alors l'avertissement `CS8625`,
qui casse le build sous `TreatWarningsAsErrors`. Le récapitulatif affiche
`constructor Party(string, Bic, string, int?)`, et le JSON rapporte `"type": "Bic"`. `int?` est
correctement conservé. L'ADR-0064 exclut de tirer null *par défaut*. Elle n'exclut pas que la méthode
qui fixe la valeur accepte null, et c'est précisément là que l'ADR dit qu'un test fait ce choix
explicitement. Domaines : 01, 02, 05, 15 (et l'`Employee?` du 05, voir CLI-B3). Repro :
[`repros/cli-nullable-parameter`](repros/cli-nullable-parameter). Source : `TypeNames.cs:66-74`
(perd `NullableAnnotation`), `GeneratorEmitter.cs:307-309`. Test rouge (`JustDummies.GenAny.UnitTests`,
à côté de `BaseTableTests`) : un paramètre `Customer? agent` émet `WithAgent(Customer? value)`, et un
appelant qui passe `(Customer?)null` compile sans `CS8625`. Correctif : typer `T?` la méthode qui fixe
la valeur, le champ et le paramètre du constructeur privé. La covariance permet toujours d'affecter la base, qui n'est jamais nulle. Afficher `T?` dans le récapitulatif et le JSON, et documenter le cast en type référence à
côté de `(decimal?)null`.

<a id="cli-d4"></a>
**CLI-D4 — `--namespace` réécrit le nom du type analysé dans le récapitulatif et le JSON (faible).**
Avec `--namespace Foo.Bar`, le récapitulatif affiche `Analyzing Foo.Bar.Sku` et le JSON
`"type": "Foo.Bar.Sku"`. Ce type n'existe pas. Le fichier émis est correct. Domaines : 14, 16.
Repro : [`repros/cli-namespace-recap`](repros/cli-namespace-recap). Source : `Scaffolder.cs:127,140`
(remplit `TargetType.Namespace`, documenté comme « the namespace the type is declared in », avec la
valeur forcée), `Cli/Recap.cs:60,212`, `Cli/ScaffoldReport.cs:45,58`. Test rouge
(`JustDummies.Cli.UnitTests/RecapTests.cs` plus un jumeau JSON) : avec une valeur forcée, la première
ligne reste `Analyzing Shop.Domain.Order`. Correctif : conserver l'espace de noms déclaré sur
`TargetType`, et porter séparément l'espace de noms du fichier.

<a id="cli-d5"></a>
**CLI-D5 — `p >= N` / `p <= N` avec N ≠ 0 ne sont pas lus, alors que `ThrowIfGreaterThanOrEqual(p, N)` l'est (faible).**
`if (longitude >= 180.0) throw` reste `to verify, unread guards`. La forme helper donne
`.LessThan(180d)`. La ligne 928 de la spécification dit que les helpers « map to the same numeric rows
a comparison would », mais la ligne correspondant à la comparaison manque. C'est la seule lacune du
tableau ci-dessous qui soit une incohérence plutôt qu'une frontière. Elle échoue bruyamment, d'où sa
faible gravité. Domaine : 20. Repro : [`repros/20-3`](repros/20-3). Source : `Guards.cs:1172-1184`
(`Numeric` : les lignes `>=`/`<=` seulement `when value == 0`) face à `Guards.cs:882-892`. Test rouge
(`JustDummies.GenAny.UnitTests`) : une garde `value >= 180.0` donne au scaffolding `.LessThan(180d)` avec le
statut `guard`. Correctif : ajouter les deux lignes à `Numeric` et au tableau du §5.3 de la
spécification (EN/FR), plus une ligne `GuardCorpus` et un fichier de référence. Garder les lignes à zéro
comme correspondance plus spécifique.

### Lacunes d'inférence signalées honnêtement (pas des bogues)

Aucune de ces formes n'est lue, et dans tous les cas observés le récapitulatif a marqué le paramètre
`to verify, unread guards`, et le build s'est arrêté sur la ligne TODO. C'est l'ADR-0060 et l'ADR-0083
qui fonctionnent comme prévu. L'élargissement du lecteur relève de l'ADR-0085 (« change the guard
reader only against a field report »). La colonne des domaines constitue ce retour de terrain.

| ID | Forme de garde non lue | Signalée ? | Domaines |
|---|---|---|---|
| CLI-G1 | Motifs relationnels/`or` : `x is < 1 or > 12`, `{ Length: < 8 or > 20 }` | oui | 02, 03, 04, 09, 13, 14 |
| CLI-G2 | Conditions combinées : `x < a \|\| x > b`, `!(x > 0)`, `IsNaN(v) \|\| v < Min` | oui | 09, 13, 15, 20 |
| CLI-G3 | Gardes par regex (`Regex.IsMatch`, `[GeneratedRegex]`, champs `Regex` statiques) — *délibérément* non lues, spécification §5.3 (ligne 827) ; voir §9 | oui | 02, 03, 05, 06, 07, 11, 12, 15 |
| CLI-G4 | Helpers privés `Validate(...)` délégués | oui | 01, 02, 03, 05, 15 |
| CLI-G5 | Gardes temporelles (bornes `TimeSpan`/`DateTime`/`DateOnly` ; les bornes `static readonly` ne sont pas des constantes de compilation) | oui | 05, 13, 16, 17, 19 |
| CLI-G6 | Longueur via des helpers : `ThrowIfGreaterThan(s.Length, N)`, `ThrowIfZero(s.Length)` | oui | 01, 19, 20 |
| CLI-G7 | Gardes d'échelle : `decimal.Round(v, n) != v`, `v.Scale > n` | oui | 08, 12, 13, 18 |
| CLI-G8 | Certaines formes de comptage (`Distinct().Count()`) ; `Count > N` et `Count == 0` *sont* lus | oui | 18, 19 |
| CLI-G9 | Arithmétique et helpers sur la valeur : `Math.Abs(v) > 90`, `v < (Half)0` | oui | 20 |
| CLI-G10 | Chiffres de contrôle et sommes de contrôle (Luhn, mod 97, GS1) | oui | 01, 06, 08, 10, 11 |
| — | `p >= N` / `p <= N` avec N ≠ 0 | oui, mais de façon incohérente avec la forme helper : [CLI-D5](#cli-d5) | 20 |

Une constante nommée est remplacée par son littéral (`ThrowIfGreaterThan(kg, Weight.MaxGrossKilograms)`
donne `.LessThanOrEqualTo(32500d)`, domaine 07). La valeur est correcte : c'est purement cosmétique.

### Candidats à une décision

Détaillés au [§8](#8-candidats-à-une-décision) : [CLI-C1](#cli-c1) (les grands plafonds de comptage
orientent les agrégats scaffoldés), [CLI-C2](#cli-c2) (les plafonds de comptage au-delà de la limite du
million rapportés comme non lus), [CLI-C3](#cli-c3) (privilégier les types des projets référencés dans
la recherche par nom court), [CLI-C4](#cli-c4) (lire les gardes d'intervalle en motif `or` de
CLI-G1/G2), [CLI-C5](#cli-c5) (transformer une garde par regex littérale en `Any.StringMatching`).

### Comportement attendu

<a id="cli-e1"></a>
**CLI-E1 — Les invariants entre paramètres sont signalés « to verify » (ADR-0060).**
`start < end`, des sinistres compris dans la période d'une police, `stop < limit`, une péremption
postérieure à la fabrication : chacun des 20 domaines avait au moins un invariant de ce genre.
Le scaffolder a marqué chacun d'eux `to verify` et bloqué le build, comme le décident l'ADR-0060 et le
§16 de la spécification. Ce n'est ni un défaut ni une friction majeure. Le générateur est une base de
travail que le développeur possède. Les participants ont résolu ces invariants avec des fenêtres
disjointes, un `Generate()` écrit à la main, ou `Any.Combine`.
[`repros/02-trading-1`](repros/02-trading-1) en montre la forme. Trois idées d'amélioration
conviennent, et aucune ne rend la génération plus astucieuse :

* **(a) Documentation.** Ajouter à la page de la CLI une section « Quand une garde lie deux
  paramètres », avec des exemples compilés pour :
  * la reparamétrisation début + durée, qui tire `start` et une `duration` positive plutôt que deux
    bornes ;
  * la dérivation d'un tirage à partir d'un autre dans `Generate()` ;
  * la forme en une ligne du chiffre de contrôle, `.As(body => body + Luhn(body))`. Le domaine 11 l'a
    trouvée propre, et elle répond aussi à CLI-G10.
* **(b) Un commentaire TODO qui cite la garde non lue**, et qui le dit quand la garde lit deux
  paramètres. C'est [CLI-D1](#cli-d1).
* **(c) Seulement comme éventuelle ADR ultérieure : un point d'extension `partial` qui survive à un
  nouvel scaffolding pour l'assemblage final**, afin que `--force` n'efface pas une règle entre
  paramètres écrite à la main. Les domaines 09, 10 et 12 ont perdu de telles retouches, ou ont craint de
  les perdre. L'écrasement actuel est annoncé dans le message de refus et dans l'en-tête : ce n'est donc
  pas un défaut aujourd'hui.

---

## 3. `core` — JustDummies (générateurs, contraintes, messages, reproductibilité)

**Aucune valeur tirée n'a violé une contrainte déclarée, dans aucun des 20 domaines.** Les éléments
ci-dessous concernent les messages, la documentation et un chemin de rejeu.

- *WithScale.* Le vérificateur a contrôlé `WithScale` exactement, sur `decimal.GetBits` avec
  `BigInteger`, pour des magnitudes de 0 à `decimal.MaxValue`. Chaque valeur est restée sur la grille
  de 10⁻ⁿ que décide l'ADR-0015 ([CORE-D8](#core-d8)).
- *DateTime Kind.* Le comportement de `Kind` est classé comme défaut de documentation
  ([CORE-D7](#core-d7)), mais sa conséquence est surprenante et y est énoncée clairement.
- *Graine hors d'une portée.* Seul le cas du flux asynchrone est un vrai défaut ([CORE-B1](#core-b1)).
  Le reste est voulu.

### Bogues

<a id="core-b1"></a>
#### CORE-B1 — Hors d'une portée, une graine choisie dans un helper attendu par `await` ne remonte pas, si bien que la graine rapportée ne rejoue pas l'exécution (moyenne)

```csharp
static async Task<int> DrawAsync() { await Task.Yield(); return Any.Int32().Generate(); }
int a = await DrawAsync();          // first draw: lazy seed S1, set in the helper's flow, then lost
int b = Any.Int32().Generate();     // the caller picks a second lazy seed, S2
// a later failure says "reproduce this run with Any.Reproducibly(S2, ...)";
// replaying under ReproduciblyAsync(S2, …) gives every value shifted by one
```

La graine ambiante paresseuse est affectée à un `AsyncLocal` dans le contexte d'exécution qui tire en
premier, quel qu'il soit. Une valeur affectée dans une méthode attendue par `await` ne remonte pas vers
l'appelant. La documentation XML de l'exception promet « the seed that replays the run », et CLAUDE.md
dit « any sequential run replays from the seed it reports ». Un helper attendu par `await` est une
exécution séquentielle.

L'essentiel de ce qui a été remonté ici est voulu (variantes V1, V2 et V4 du vérificateur) :

- la même graine est rapportée pour chaque échec d'un test ;
- le rejeu redémarre au premier tirage du test ;
- chaque test séquentiel reçoit sa propre graine.

La page des erreurs décrit aussi ce comportement de façon incomplète. Elle ne mentionne `Seed` qu'« à
l'intérieur d'une portée reproductible », alors que `Seed` est aussi renseigné en dehors (domaine 08).

| | |
|---|---|
| Domaines | 05 (a remonté la partie voulue), 08 (la partie documentation) ; la variante asynchrone a été trouvée par le vérificateur |
| Repro | [`repros/core-seed-outside-scope`](repros/core-seed-outside-scope) (V3 et V3b échouent) |
| Source | `JustDummies/AmbientRandomSource.cs:38-46` (graine `AsyncLocal` paresseuse), `:51`, `:65` (indications de rejeu) ; `doc/handwritten/for-users/guides/errors-and-conflicts.en.md:101-108` |
| Test rouge | `JustDummies.UnitTests`, à côté de `AmbientSeedScopeTests.cs`. Hors de toute portée, tirer `a` dans un helper attendu par `await` puis `b`, déclencher une `AnyGenerationException`, rejouer sous `Any.ReproduciblyAsync(ex.Seed!.Value, …)`, et vérifier `(ra, rb) == (a, b)`. |
| Correctif | Soit conserver la graine paresseuse dans un conteneur que le flux extérieur possède déjà, soit cesser de promettre le rejeu hors d'une portée (« this flow drew from seed S; wrap the body in `Reproducibly`/`ReproduciblyAsync` to make it replayable »). Dans les deux cas, corriger la page des erreurs (EN/FR). |
| Tests `Bug_` de l'essai | Aucun. |

<a id="core-b2"></a>
#### CORE-B2 — Les messages d'échec de `As(...)`/`Combine(...)` affichent les chaînes tirées brutes, contrairement à l'ADR-0075 (moyenne)

`AnyDerivation.Display` entoure une chaîne de guillemets sans l'échapper, si bien que NUL, CR, FF et
ESC atteignent `AnyGenerationException.Message`. Résultat : les consoles perdent des caractères, `grep`
traite la sortie de `dotnet test` comme un fichier binaire, et un ESC peut ouvrir une séquence ANSI.
L'ADR-0075 exige que la bibliothèque « escape control characters wherever it renders a drawn value ».
La bibliothèque dispose déjà de l'échappeur, `CharacterPools.Escape`, et `AnyChar` l'utilise. Le
`Any.String()` par défaut couvre tout l'ASCII, si bien que ce message est fréquemment rencontré.

| | |
|---|---|
| Domaines | 04 |
| Repro | [`repros/core-raw-control-chars`](repros/core-raw-control-chars) (2 sur 2 échouent) |
| Source | `JustDummies/AnyDerivation.cs:120-133` (`Display`) ; appelants `AnyExtensions.cs:46`, `Any.Combine.cs:46-302` |
| Test rouge | `JustDummies.UnitTests` : `Any.String().WithChars("\0\r\u001b").WithLength(3).As<string, object>(v => throw new ArgumentException()).Generate()` lève une exception dont le message contient `\0`, `\r` et `\x1b` sous forme d'échappements et aucun caractère inférieur à 0x20. Ajouter un jumeau pour `Combine`. |
| Correctif | Rendre chaînes et caractères via `CharacterPools.Escape`, et échapper aussi le repli `ToString()`. |
| Tests `Bug_` de l'essai | Aucun. |

<a id="core-b3"></a>
#### CORE-B3 — `Any.Double().Between(-double.Epsilon, double.Epsilon)` ne tire jamais que 0 (faible)

L'intervalle contient trois doubles, mais dans `CandidateIn` la demi-étendue `upper/2 - lower/2`
s'annule par dépassement de capacité par le bas (underflow), si bien que chaque tirage est le point milieu. La valeur tirée est valide. Ce qui
casse, c'est que la demande réalisable `ArrayOf(...).WithCount(2).Distinct()` est refusée. L'ADR-0097
énonce « two values the type represents must be two values a draw can land on ». C'est un recoin
infime, mais c'est un défaut de correction (le refus d'une demande réalisable), pas une borne sur l'ambition.

| | |
|---|---|
| Domaines | 20 |
| Repro | [`repros/20-1`](repros/20-1) |
| Source | `JustDummies/ContinuousIntervalSpec.cs:438-446` (`CandidateIn`) |
| Test rouge | `JustDummies.UnitTests` : une boucle avec graine sur `Any.Double().Between(-double.Epsilon, double.Epsilon)` atteint chacune des valeurs {-ε, 0, ε}, ou `ArrayOf(...).WithCount(3).Distinct()` réussit. Un invariant `JustDummies.PropertyTests` sur des étendues de quelques ulp en est la suite naturelle. |
| Correctif | Quand `half == 0` mais `lower < upper`, choisir par index parmi les valeurs représentables énumérables. Consommer exactement un échantillon unitaire, comme l'exige l'ADR-0049. |
| Tests `Bug_` de l'essai | `20 Bug_Double_between_minus_and_plus_epsilon_only_ever_draws_zero`, `20 Bug_Two_distinct_doubles_from_minus_to_plus_epsilon_are_refused`. Les deux devraient passer au vert après un nouvel empaquetage. |

### Défauts (messages, formulation, documentation d'un contrat)

<a id="core-d1"></a>
**CORE-D1 — Les messages de conflit décimaux énoncent mal les bornes strictes (moyenne).**
`Any.Decimal().GreaterThan(100m).LessThan(10m)` donne « …GreaterThan(100) already requires values
greater than **or equal to 100** ». `GreaterThan(36m).LessThan(36m)` donne « …forbids 36, **the only
value** GreaterThan(36) and LessThan(36) leave ». `Positive().Negative()` dit la même chose de 0. Ce
sont des contre-vérités, là où `Int32` et `Double` sont formulés correctement. La cause : une borne
décimale stricte est stockée comme inclusive plus une exclusion ponctuelle. Domaines : 01, 02, 12, 13.
Repro : [`repros/core-decimal-conflict-messages`](repros/core-decimal-conflict-messages). Source :
`JustDummies/DecimalIntervalSpec.cs:132-157`, `:740-750` (`PinningClause`) ;
`ConflictingAnyConstraintException.cs:182-191`. Test rouge (`JustDummies.UnitTests`, tests de conflits
décimaux) : une théorie sur les trois chaînes vérifiant le message exact, par exemple « …GreaterThan(100)
already requires values greater than 100. » Correctif : mémoriser pour chaque borne si elle est stricte,
ajouter des fabriques « greater/less than {bound} », et traiter deux bornes strictes au même point
comme un intervalle vide avant le contrôle d'exclusion. Tests `Bug_` de l'essai :
`12 Bug_Decimal_crossed_bounds_message_misstates_what_GreaterThan_requires`,
`12 Bug_Decimal_empty_open_interval_message_claims_a_value_is_left`,
`13 Bug_Decimal_sign_conflict_message_claims_zero_is_left`.

<a id="core-d2"></a>
**CORE-D2 — Une borne décimale stricte qui vide une grille `WithScale` ne nomme qu'un côté (moyenne).**
`Any.Decimal().Between(0m, 5000m).WithScale(2).Negative()` donne « Cannot apply Negative() because it
forbids every WithScale(2) value between 0 and 0. » `Between` n'est jamais nommé. `Int32` donne la forme
propre à deux côtés. Une seconde variante nomme la contrainte appliquée des deux côtés du « because »
(domaine 10). Domaines : 03, 10. Repro : [`repros/core-scaled-range-message`](repros/core-scaled-range-message).
Source : `DecimalIntervalSpec.cs:148-159`, `:669-672`, `:734`. Test rouge
(`JustDummies.UnitTests/ConflictMessageProvenanceTests.cs`) : le message contient « Between(0, 5000) ».
Correctif : refuser la borne avant de la scinder en une borne et une exclusion, comme le fait la
spécification ordinale, et donner la clause d'épinglage à la branche d'épuisement avec échelle. Test
`Bug_` de l'essai : `03 Bug_Negative_on_a_scaled_deductible_range_names_neither_Between_nor_the_real_conflict`.

<a id="core-d3"></a>
**CORE-D3 — L'épuisement de `Distinct` sur un `IAny<T>` étranger affirme comme un fait que ses valeurs « are not reproducible » (moyenne).**
Sur n'importe quel générateur scaffoldé par `any`, le message dit que certaines valeurs « come from a
generator that does not draw from this source, so they are not reproducible from this seed alone ». Le
rapporteur `Reproducibly` de la même exécution affiche une ligne de rejeu complète, et le générateur
scaffoldé tire bien depuis la source ambiante, et rejoue exactement. L'ADR-0102 décide la *réserve*
prudente, parce que la bibliothèque ne peut pas voir à l'intérieur d'un générateur étranger. Rien ne
décide d'affirmer comme un fait ce qui est inconnaissable. Domaines : 06, 18. Repro :
[`repros/core-distinct-not-reproducible-msg`](repros/core-distinct-not-reproducible-msg). Source :
`AmbientRandomSource.cs:54-56`, `FixedRandomSource.cs:22-24`, sélectionnés via
`CollectionState.cs:378-388` et `AnyDerivation.cs:27-33`. Test rouge (`JustDummies.UnitTests`) : sur un
`IAny<int>` écrit à la main qui tire via `Any`, le message ne contient pas « does not draw from this
source » et énonce bien le rejeu conditionnel. Correctif : reformuler en « its values replay from
this seed only if it draws through `Any` (as a generator scaffolded by `any` does) ». Un moyen public
pour un générateur de déclarer qu'il tire depuis la source ambiante relève de [CORE-C10](#core-c10).

<a id="core-d4"></a>
**CORE-D4 — Les messages de conflit en virgule flottante reformulent une borne stricte comme la valeur représentable suivante (faible).**
`Any.Double().GreaterThan(10).LessThan(5)` donne « …greater than or equal to 10.000000000000002 », et
`Positive()` donne « ≥ 5E-324 ». Le message est littéralement vrai, et sa véracité est garantie par
`ConflictMessageTruthfulnessProperties`, mais il est formulé dans des termes du moteur que
l'utilisateur n'a jamais écrits. Domaines : 05, 07, 17. Repro :
[`repros/core-double-conflict-wording`](repros/core-double-conflict-wording). Source :
`ContinuousIntervalSpec.cs:160-198`. Test rouge
(`JustDummies.UnitTests/ConflictMessageProvenanceTests`) : le message contient « GreaterThan(10)
already requires values greater than 10 » et non « 10.000000000000002 ». Correctif : conserver la
valeur déclarée et un indicateur de stricte inégalité par borne, et partager les fabriques strictes
avec CORE-D1.

<a id="core-d5"></a>
**CORE-D5 — Un décalage refusé près de `DateTimeOffset.MaxValue`/`MinValue` n'énonce pas la limite qu'il implique (faible).**
`After(MaxValue.AddHours(-1)).WithOffset(14h)` est correctement refusé et nomme les deux contraintes. Il
ne donne jamais la limite d'instant dérivée (UTC ≤ MaxValue − 14 h) ni la raison (l'heure locale
sortirait de 0001..9999). La formulation varie aussi selon l'ordre de déclaration. Domaine : 19. Repro :
[`repros/core-dto-maxvalue-offset`](repros/core-dto-maxvalue-offset). Source :
`AnyDateTimeOffset.cs:386-409` (`WithOffsetRange`). Test rouge
(`JustDummies.UnitTests/AnyDateTimeOffsetOffsetTests.cs`) : le message contient
« 9999-12-31T09:59:59.9999999+00:00 », ou dit que l'heure locale sortirait de l'intervalle. Correctif :
donner au conflit une fabrique dédiée qui nomme la limite dérivée et la raison, dans les deux ordres.

<a id="core-d6"></a>
**CORE-D6 — Le refus de cardinalité de `SetOf` ne nomme aucun nombre d'éléments (faible).**
« The generator can produce only 10 distinct value(s), not enough for the 11 elements… » ne nomme ni
`WithCount(11)` ni la chaîne des éléments. CLAUDE.md demande un message qui nomme les deux côtés.
Domaines : 05. Source : `ConflictingAnyConstraintException.cs:152-154`. Test rouge
(`JustDummies.UnitTests/ConflictMessageProvenanceTests`) : le message pour `SetOf(Any.Boolean()).WithCount(5)`
contient « WithCount(5) ». Correctif : passer l'appel de comptage à `SetElementsExceedCardinality` et à
`DictionaryKeysExceedCardinality`.

<a id="core-d7"></a>
**CORE-D7 — Les tirages `DateTime` sont toujours `Kind = Utc` et se comparent en ticks : décidé dans la documentation XML, absent de la page utilisateur (moyenne, documentation).**
Les remarques XML de `AnyDateTime` (lignes 18-20) le décident : les valeurs générées portent
`DateTimeKind.Utc`, les bornes se comparent en `Ticks` et ignorent leur `Kind` (comme le font les
opérateurs de `DateTime` lui-même), et `OneOf` conserve le `Kind` des valeurs qu'on lui passe. La page
utilisateur `dates-and-times` (EN/FR) n'en mentionne rien. **La conséquence est surprenante et doit
être énoncée clairement :** `Between(Local, Local)` peut renvoyer une valeur `Utc` dont l'*instant* se
situe hors des instants des bornes. Sous `TZ=Europe/Paris`, des bornes à 00:00:00 et 00:00:01 en heure
locale donnent une valeur `Utc` dont les ticks sont entre les deux, ce qui, en tant qu'instant, est une
heure *plus tard*. La valeur satisfait `start <= value && value <= end` selon la comparaison que
ferait le propre `<=` d'un test, donc aucune contrainte déclarée n'est violée. **Le mainteneur devrait
juger si le contrat documenté est le bon.** L'alternative est [CORE-C7](#core-c7). Domaines : 02, 04,
06, 11, 16, 17. Repro : [`repros/core-datetime-kind`](repros/core-datetime-kind). Test rouge
(`JustDummies.Documentation.UnitTests`) : un exemple compilé sur `dates-and-times` montrant un résultat
`Utc` entre des bornes `Local` au sens des ticks. Correctif : ajouter un paragraphe « DateTime et Kind »
(EN/FR) qui énonce aussi l'intervalle sans contrainte (`MinValue`..`MaxValue`), et conseiller des
bornes `Utc`, ou `DateTimeOffset` quand l'instant compte.

<a id="core-d8"></a>
**CORE-D8 — La page utilisateur de `WithScale` dit qu'il « fixes the number of decimal places » ; l'ADR-0015 décide une grille de valeurs (moyenne, documentation).**
Chaque tirage est un multiple de 10⁻ⁿ (vérifié exactement, 500 tirages × 5 magnitudes). Une valeur
`OneOf` ou un intervalle réduit à un point revient tel qu'écrit (`20`, pas `20.0`). Près de 7,9e27,
29 chiffres significatifs ne laissent pas de place pour deux chiffres décimaux, et pourtant la valeur
reste sur la grille. L'ADR-0015 rejette explicitement un contrat de `decimal.Scale` complété par des
zéros, et exige que la documentation du builder « state it as a value lattice ». Domaines : 03, 08, 11,
12, 13, 15, 18. Repro : [`repros/core-withscale`](repros/core-withscale). Source :
`doc/handwritten/for-users/generators/numbers.en.md:106`, `numbers.fr.md:111`. Correctif : dire
« garde la valeur exprimable avec au plus n décimales (un multiple de 10⁻ⁿ) ; `decimal.Scale` n'est pas
complété par des zéros », et lier l'ADR-0015. Un invariant `JustDummies.PropertyTests`
`decimal.Round(v, n) == v` jusqu'à `decimal.MaxValue` figerait le contrat. Il passe aujourd'hui.

<a id="core-d9"></a>
**CORE-D9 — `DateTimeOffset.WithGranularity` aligne l'instant UTC sur la grille, pas l'horloge locale ; la documentation utilisateur ne le dit jamais (moyenne, documentation).**
`WithGranularity(FromDays(1)).WithOffset(FromHours(2))` donne 02:00 en heure locale, et avec `+05:30`
les « heures pleines » tombent à :30. La documentation XML dit « the instant », ce qui est respecté,
mais ses exemples (« a whole day », « a quarter-hour ») se lisent comme de l'heure locale. Ni la section
sur la granularité ni celle sur le décalage de la page utilisateur ne disent comment les deux
interagissent. Domaines : 07, 09, 19. Repro :
[`repros/core-dto-granularity`](repros/core-dto-granularity). Source : `AnyDateTimeOffset.cs:201-207`,
`:334-340` ; `dates-and-times.en.md:42-88`. Correctif : documenter le comportement dès maintenant
(EN/FR plus le résumé XML). Faut-il changer la sémantique ? C'est [CORE-C8](#core-c8).

<a id="core-d10"></a>
**CORE-D10 — Le refus de cardinalité distincte a migré vers `Generate()` sans trace écrite ; l'ADR-0004, JD016 et la page des collections promettent toujours un refus à la déclaration (moyenne, documentation + ADR).**
`CollectionState.cs:166` effectue le contrôle quand la spécification est complète, délibérément :
contrôler au milieu d'une chaîne refusait un ordre de déclaration et en acceptait un autre. Le
comportement est sain, et il échoue toujours avant tout tirage. Les écrits le contredisent :

- l'ADR-0004 (« rejects … immediately » ; elle rejette même « always fail at generation ») ;
- les sections de contexte de l'ADR-0009 et de l'ADR-0011 ;
- `JD016.en.md:14` (« Both throw at declaration time ») ;
- `collections.en.md:93-95` (« refused immediately »).

Domaines : 01, 04, 05, 10. Repro : [`repros/core-cardinality-gate`](repros/core-cardinality-gate).
Correctif : consigner ce déplacement dans une ADR qui amende l'ADR-0004, puis mettre à jour les pages
(EN/FR). Le test rouge va dans `JustDummies.UnitTests` : il vérifie que `SetOf(Boolean()).WithCount(5)`
ne lève pas à la déclaration et lève `ConflictingAnyConstraintException` à `Generate()`. Le repli par
tirage borné des chaînes et des motifs est la frontière décidée par l'ADR-0004 et reste en l'état (voir
§9).

### Lacunes d'inférence

Sans objet : la bibliothèque n'infère rien. Les tirages bornés qu'elle refuse sont listés au §9.

### Candidats à une décision

Détaillés au [§8](#8-candidats-à-une-décision) : CORE-C1 (TimeOnly à cheval sur minuit), CORE-C2
(tirage dépendant), CORE-C3 (jour de semaine / jour ouvré), CORE-C4 (pas décimal), CORE-C5 (Guid v4),
CORE-C6 (séries ordonnées), CORE-C7 (héritage du Kind de DateTime), CORE-C8 (granularité sur
l'horloge locale), CORE-C9 (un `OneOf` sous-ensemble qui intersecte), CORE-C10 (une déclaration
publique « tire depuis la source ambiante »).

### Comportement attendu

- Un appel unique aux bornes inversées tel que `Any.Int32().Between(10, 1)` lève `ArgumentException`,
  pas un conflit. C'est documenté et détecté par JD014 (§9).
- Sur un corps synchrone, la graine rapportée hors d'une portée rejoue depuis le premier tirage du
  test, et la même graine est rapportée pour chaque échec de ce test.
- Un second `OneOf` dans une lambda `With…` est refusé comme conflit de déclaration (ADR-0100, JD034).
  Ce qui manque, c'est la documentation ([DOC-9](#doc-9)).

---

## 4. `analyzers` — JDxxx

### Défauts

<a id="an-d1"></a>
**AN-D1 — JD014 et JD017 signalent tous deux une même valeur d'énumération non déclarée dans `Any.Enum<T>().OneOf(...)` (moyenne).**
`OneOf((Currency)42)`, une non-combinaison `[Flags]`, et `(Currency)0` dans un ensemble mixte
produisent chacun deux avertissements à la même ligne et à la même colonne. Le commit `37ff3ff` a
ajouté l'appartenance à l'énumération à JD014 sans faire s'effacer l'une ou l'autre règle. ADR-0078 :
« two diagnostics on one expression for one mistake is noise that trains a reader to disable both. »
Domaines : 01, 04, 12. Repro :
[`repros/analyzers-jd014-jd017-double`](repros/analyzers-jd014-jd017-double). Source :
`RejectedConstantArgumentAnalyzer.cs` (~l.117, `case "OneOf"`), `ArgumentGuardFacts.cs:109, 217-234`,
en recouvrement avec `EnumUniverseViolationAnalyzer.cs`. Test rouge (`JustDummies.Analyzers.UnitTests`) :
exécuter les deux analyseurs sur `Any.Enum<Status>().OneOf((Status)99)` et vérifier exactement un
diagnostic sur l'étendue. Correctif : donner un seul propriétaire à l'appartenance. JD017 est suggéré,
parce que son message enseigne l'erreur de catégorie. Garder le refus dans le parcours que partagent
JD034/JD035, mettre à jour JD014 (EN/FR), et écrire une courte note de décision dans l'esprit de
l'ADR-0078.

<a id="an-d2"></a>
**AN-D2 — JD023 ne nomme que la contrainte qui vide le domaine, sans ses arguments (faible).**
L'analyseur signale « No value satisfies this chain once GreaterThan is applied ». L'exécution dit
« Cannot apply GreaterThan(10) because Between(1, 6) already requires values less than or equal to 6 ».
L'analyseur dispose déjà de l'état antérieur et des arguments repliés. Domaines : 08, 12. Repro :
[`repros/analyzers-jd023-message`](repros/analyzers-jd023-message). Source : `Descriptors.cs:242`,
`ScalarChainAdmitsNoValueAnalyzer.cs:86-87`. Test rouge (`Jd023Jd024ScalarChainTests.cs`) : le message
contient à la fois « GreaterThan(10) » et « Between(1, 6) ». Correctif : utiliser un message à deux
côtés qui reflète la formulation de l'exécution, et le montrer sur les pages JD023.

<a id="an-d3"></a>
**AN-D3 — JD014 et JD023 neutralisent des formes de test négatif différentes (faible).**
Dans `Assert.Throws(() => chain.Generate())`, JD014 avertit encore parce qu'il examine l'appel gardé
lui-même, alors que JD023 se tait parce qu'il ne juge que l'invocation la plus externe. Le silence
dans `Assert.Throws` est documenté. L'incohérence ne l'est pas. Domaines : 02, 04 (remonté), vérifié
dans [`repros/analyzers-constant-misses`](repros/analyzers-constant-misses) (V8b face à la ligne 50).
Source : `RejectedConstantArgumentAnalyzer.cs:59`, `ScalarChainAdmitsNoValueAnalyzer.cs:44-47`.
Test rouge (`JustDummies.Analyzers.UnitTests`) : JD023 rend le même verdict que JD014 sur
`Assert.ThrowsAny<Exception>(() => <conflicting chain>.Generate())`. Correctif : aligner les deux
règles, et énoncer la forme neutralisée sur les deux pages.

<a id="an-d4"></a>
**AN-D4 — La page JD014 prétend refléter les gardes « exactly », et un test range un argument refusé parmi les « legal » (faible).**
La section Scope dit « the table mirrors SizeGuard and the per-generator guards exactly ». C'est faux :
voir AN-F2 et AN-C4. `Jd014RejectedConstantArgumentTests.cs:209-210` liste
`Any.Double().Between(double.NaN, 1.0)` sous `Does_not_report_a_legal_argument`, alors que le
commentaire à côté dit que la garde le refuse. Correctif : lister ce que la règle couvre réellement
(EN/FR), et déplacer cette ligne vers un cas « non couvert » correctement nommé.

<a id="an-d5"></a>
**AN-D5 — Les règles Info (JD020, JD022, JD024, JD029, JD030, JD031, JD033) n'apparaissent jamais dans la sortie de `dotnet build`, et aucune page utilisateur ne le dit (moyenne, documentation).**
Les règles se déclenchent : elles apparaissent dans un journal d'erreurs SARIF, et s'affichent une fois
élevées à `warning` dans `.editorconfig`. Masquer les diagnostics Info dans la console est le
comportement standard de MSBuild, et l'ADR-0077 a décidé la gravité. Les pages utilisateur (« active
on your next build », « at the call site, which is the one place you can act on it ») supposent un
lecteur qui utilise un IDE. Dans un flux de travail en terminal seul ou en CI, le remède que JD030 est
censé enseigner n'atteint jamais l'utilisateur. Domaines : 02, 12, 17 (le 20 le note aussi). Repro :
[`repros/analyzers-info-invisible`](repros/analyzers-info-invisible). Correctif : ajouter une phrase
plus un extrait `.editorconfig` à `analyzers/README.md` et à `packages/justdummies` (EN/FR), par exemple
`dotnet_analyzer_diagnostic.category-JustDummies.Constraints.severity = warning`. Ne pas changer les
gravités par défaut. Test rouge (`JustDummies.Documentation.UnitTests`) : les deux pages l'énoncent et
portent l'extrait.

### Faux négatifs au regard de la portée documentée d'une règle

<a id="an-f1"></a>
**AN-F1 — JD016 ignore un ensemble `OneOf` numérique typé (faible).**
`Any.SetOf(Any.Int32().OneOf(1, 2)).WithCount(3)` et `Any.ArrayOf(Any.Double().OneOf(1.0, 2.0)).WithCount(3).Distinct()`
compilent sans rien dire et lèvent à l'exécution. Le `Any.OneOf(...)` générique et `Any.Char().OneOf(...)`
sont signalés. La page liste « a OneOf/ElementOf pool of constants », et le commentaire à
`CollectionConstraintsAdmitNoValueAnalyzer.cs:205-209` prétend que le cas est traité, ce qui est faux.
Domaine : 20. Repro : [`repros/20-6`](repros/20-6). Source : même fichier, `:200-215, 269`. Test rouge
(`JustDummies.Analyzers.UnitTests`, JD016) : l'ensemble ci-dessus déclenche JD016, et un cas négatif
conserve la forme que #188 a rendue légale (élargie par `Containing`). Correctif : compter le premier
`OneOf` typé rencontré en descendant, et faire passer un élargissement ultérieur par le point
d'extension `mayExtend` existant.

<a id="an-f2"></a>
**AN-F2 — JD014 manque les bornes constantes non finies, et `Half` dans `OneOf` (faible).**
`Between(double.NaN, 90.0)`, `LessThan(double.PositiveInfinity)`, `LessThanOrEqualTo(double.NaN)` et
`Any.Half().OneOf((Half)1, Half.NaN)` lèvent toujours à la déclaration, et ne reçoivent aucun
diagnostic. `Single().OneOf(1f, float.NaN)` est signalé. La page ne liste les `OneOf` non finis que pour
`Double` et `Single`, si bien qu'en toute rigueur cela se situe à la lisière de la portée documentée.
C'est classé ici parce que la page prétend aussi refléter les gardes exactement (AN-D4). Domaines : 09,
20. Repro : [`repros/20-5`](repros/20-5). Source : `ArgumentGuardFacts.cs:109` (`AnyHalf` manquant),
`RejectedConstantArgumentAnalyzer.cs` (aucun cas de finitude des bornes). Test rouge (suite JD014) :
JD014 à la fois sur la borne `NaN` et sur l'élément `Half.NaN`. Correctif : ajouter les deux contrôles,
mettre à jour les pages JD014 (EN/FR), et étendre les chaînes des tests de propriétés.

### Candidats (hors de la portée documentée de toute règle)

Détaillés au [§8](#8-candidats-à-une-décision) : AN-C1 (une valeur tirée qui atteint une assertion),
AN-C2 (JD023 au-delà des entiers), AN-C3 (membres `--entry-point any` invisibles pour JD007/JD009), AN-C4
(les manques restants sur les constantes), AN-C5 (constructions regex non prises en charge, au build),
AN-C6 (une lambda d'affinage qui ignore son argument).

### Règles Info invisibles dans `dotnet build`

C'est [AN-D5](#an-d5) : un défaut de documentation, pas un défaut de code. Il change la façon dont
chaque règle Info est vécue hors d'un IDE.

---

## 5. `xunit` — l'adaptateur

<a id="xu-d1"></a>
**XU-D1 — Aucune page utilisateur ne montre `using JustDummies.Xunit;` (moyenne ; 13 domaines sur 20).**
Un test `[Fact, Reproducible]` copié depuis `packages/justdummies-xunit.en.md` échoue avec `CS0246` sur
`Reproducible`. Aucune page utilisateur ne montre la *moindre* directive `using JustDummies…`, pas plus
que le readme NuGet. **Cause racine :** `JustDummies.Documentation.UnitTests/DocumentationSampleCompiler.cs:31-48`
ajoute silencieusement `using JustDummies; using JustDummies.Diagnostics; using JustDummies.Xunit; using Xunit;`
à chaque exemple. Les exemples compilent donc en CI, et la propre remarque du compilateur (« if it
binds, it binds in a consumer's test project too ») ne tient pas pour les espaces de noms.

| | |
|---|---|
| Domaines | 03, 04, 05, 06, 07, 09, 11, 12, 13, 15, 16, 18, 19 |
| Repro | [`repros/docs-xunit-namespace`](repros/docs-xunit-namespace) |
| Test rouge | `JustDummies.Documentation.UnitTests` : compiler le premier exemple complet de `justdummies-xunit.{en,fr}.md` avec un prélude qui n'injecte pas `JustDummies.Xunit`, et n'attendre aucun `CS0246`. |
| Correctif | Montrer les trois lignes `using` dans le premier exemple de la page xunit (EN/FR), dans `JustDummies.Xunit/README.nuget.md` et sur la page de prise en main. Nommer l'espace de noms sous « The whole surface ». Empêcher le prélude d'injecter `JustDummies.Xunit`, pour que la documentation ne puisse plus dériver. Un `<Using>` global en `buildTransitive` serait une décision d'empaquetage, pas ce correctif. |

Aucun défaut n'a été trouvé dans le comportement propre de l'adaptateur. `[Reproducible]` a affiché un
`[Reproducible(Seed = …)]` prêt à copier-coller en cas d'échec dans chaque domaine qui l'a utilisé, et
fixer cette graine a reproduit la valeur fautive à chaque essai.

---

## 6. `catalog`

Rien n'a été trouvé. Aucun domaine n'a neutralisé une règle JDxxx via `JustDummies.DiagnosticCatalog` :
cet essai ne dit donc rien, dans un sens ou dans l'autre, sur ce package.

---

## 7. Documentation (transverse)

Les lignes qui portent un identifiant de composant sont décrites dans leur section. Chaque correctif
s'applique à la page anglaise **et** à son jumeau français.

| ID | Page | Défaut | Composant | Correctif |
|---|---|---|---|---|
| [XU-D1](#xu-d1) | `packages/justdummies-xunit`, `README.nuget.md`, prise en main | `using JustDummies.Xunit;` jamais montré ; prélude d'exemples caché | `xunit` | Montrer les `using` ; supprimer l'injection du prélude |
| [CLI-D2](#cli-d2) | `packages/justdummies-cli` (§« to verify »), spécification §5.6 | « delete that one line » est faux quand le type de la base change | `cli` | Dire quoi retyper ; exemple compilé |
| [CORE-D7](#core-d7) | `generators/dates-and-times` | Le `Kind` de `DateTime` est toujours `Utc`, comparaison en ticks, intervalle par défaut | `core` | Paragraphe « DateTime et Kind » |
| [CORE-D8](#core-d8) | `generators/numbers` | `WithScale` décrit comme fixant le nombre de décimales | `core` | Formulation en grille de valeurs, lien vers l'ADR-0015 |
| [CORE-D9](#core-d9) | `generators/dates-and-times` | Sous un décalage, la granularité aligne l'instant UTC | `core` | L'énoncer ; résumé XML aussi |
| [CORE-D10](#core-d10) | ADR-0004, ADR-0009/0011, `analyzers/JD016`, `generators/collections` | Refus de cardinalité documenté à la déclaration ; il s'exécute à `Generate()` | `core` | ADR d'amendement, puis les pages |
| [AN-D5](#an-d5) | `analyzers/README`, `packages/justdummies` | Règles Info invisibles dans les builds en console | `analyzers` | Une phrase plus un extrait `.editorconfig` |
| <a id="doc-1"></a>DOC-1 | `generators/enums-and-choices:157`, `guides/errors-and-conflicts:63` | `Any.Boolean().Except(true, false)` n'existe pas dans l'API publique | `core` | Utiliser un membre existant, par ex. `Any.Boolean().True().DifferentFrom(true)` |
| <a id="doc-2"></a>DOC-2 | `generators/strings:56`, `analyzers/JD030:24` | « `NonEmpty()` leaves the ceiling where it was » ; il donne 1 à 1025 (la propre phrase de JD030 le dit) | `core` | « relève le plancher et le plafond d'une unité » |
| <a id="doc-3"></a>DOC-3 | `generators/collections:50`, `guides/design-principles:72` | Citent l'ADR-0029 pour la limite du million ; remplacée par l'ADR-0076 | `core` | Pointer vers l'ADR-0076 ; ajouter un contrôle de liens vers les ADR remplacées |
| <a id="doc-4"></a>DOC-4 | `generators/strings` (tableaux regex) | `\xNN`/`\uNNNN` fonctionnent mais ne sont pas listés ; `\b`, `\B`, `\G`, `\p{…}`, `\k<…>` et la soustraction de classes sont refusés mais pas listés ; `IgnorePatternWhitespace` lève `ArgumentException`, sans mention ; la limite de 65 536 caractères est absente ; chaque refus se termine par la même liste figée | `core` | Compléter les deux tableaux, ajouter les deux notes ; éventuellement rendre la fin du message propre à la construction (`UnsupportedRegexException.cs:22`) |
| <a id="doc-5"></a>DOC-5 | `packages/justdummies-cli:254` | « A lambda never widens the base » ; `_ => Any.Int32()` la remplace | `cli` | « Une lambda qui s'appuie sur son argument ne l'élargit jamais ; une lambda qui l'ignore remplace la base » |
| <a id="doc-6"></a>DOC-6 | `guides/errors-and-conflicts:101-108` | `Seed` documenté « inside a reproducible scope » seulement ; il est aussi renseigné en dehors | `core` | Avec [CORE-B1](#core-b1) |
| <a id="doc-7"></a>DOC-7 | `packages/justdummies-cli` | Muette sur le fait que les gardes par regex ne sont jamais lues (le §5.3 de la spécification le décide) | `cli` | Le dire ; renvoyer vers `Any.StringMatching` et le sous-ensemble régulier |
| <a id="doc-8"></a>DOC-8 | `packages/justdummies-cli` | Aucune section sur l'endroit où placer un invariant entre paramètres | `cli` | Idée (a) de [CLI-E1](#cli-e1), y compris la ligne unique Luhn |
| <a id="doc-9"></a>DOC-9 | `packages/justdummies-cli:243-247` (affinage) | Ne dit pas qu'une contrainte déclarable une seule fois (`OneOf`, familles, `WithLength`, `MultipleOf`, `WithScale`) ne peut pas être redéclarée dans la lambda ; que `Except`/`DifferentFrom` restreignent un ensemble ; que JD034 ne voit pas dans la lambda | `cli` | Une puce plus un exemple compilé `z => z.Except(...)` |
| <a id="doc-10"></a>DOC-10 | `packages/justdummies-cli` (affinage) | Pour réutiliser un affinage préparé, conserver un `Func<AnyX, AnyX>`, pas un `IAny<T>` construit (ADR-0109 ; CS0266 ne l'enseigne pas) | `cli` | Une ligne |
| <a id="doc-11"></a>DOC-11 | `packages/justdummies-cli` | Une ligne de chaîne scaffoldée déclenche JD030 (Info) par conception | `cli` | Une ligne invitant le développeur à déclarer la longueur |
| <a id="doc-12"></a>DOC-12 | `guides/errors-and-conflicts` | Un appel unique aux bornes inversées (`Between(10, 1)`) est une erreur d'argument, pas un conflit | `core` | Une ligne sous « bounds that cross » |
| <a id="doc-13"></a>DOC-13 | `analyzers/JD007`, `JD009` | « Written inline from `Any` » laisse entendre que `Any.X()` issu de `--entry-point any` est vu ; il ne l'est pas | `analyzers` | Dépend de [AN-C3](#an-c3) |

Garde-fous suggérés pour `JustDummies.Documentation.UnitTests` : résoudre les extraits en ligne
`Any.<Factory>().<Member>(` contre la référence PublicAPI (rouge sur DOC-1), et échouer sur un lien vers
une ADR remplacée (rouge sur DOC-3). L'issue #296 ([CLI-B7](#cli-b7)) est un défaut de message de la
CLI, pas un défaut de page : elle n'a donc pas de ligne ici.

---

## 8. Candidats à une décision

Aucun de ces éléments n'est un bogue. Dans chaque cas, la bibliothèque soit refuse clairement, soit n'a
pas de membre, et l'essai fournit le retour de terrain dont une ADR aurait besoin. Les options sont
données en une ligne chacune.

<a id="core-c1"></a>
**CORE-C1 — Intervalles `TimeOnly` qui franchissent minuit.**
*Qui :* 02, 03, 04, 09, 12, 13, 15, 16 (postes de nuit, couvre-feux, séances de cotation, fenêtres de
nuit). *Aujourd'hui :* `Any.TimeOnly().Between(new TimeOnly(22, 0), new TimeOnly(2, 0))` lève
`ArgumentException` (« The start … must be at or before the end »). *Clair ?* Oui, mais la page des
dates ne nomme ni le refus ni un contournement. Aucune ADR ne décide d'une fenêtre qui boucle : ce n'est
donc pas une frontière assumée. Le `TimeOnly.IsBetween` de la BCL boucle. *Options :*
**(A)** S'aligner sur `IsBetween` : des bornes inversées bouclent. Cela change le sens d'un appel
existant, et JD014 ne peut plus détecter un `Between` inversé sur `TimeOnly`.
**(B)** Ajouter un membre explicite (`BetweenAcrossMidnight`, ou une forme `Wrapping`) et conserver le
refus, avec un message de refus qui renvoie vers le nouveau membre.
Le responsable de l'essai penche pour **B**, qui préserve le sens de chaque appel existant et la
détection de la faute de frappe, et refuse bruyamment en offrant une issue. C'est au mainteneur de
trancher. Dans les deux cas, la page des dates devrait nommer le refus actuel et le contournement par
un `OneOf` de deux `Between`.

<a id="core-c2"></a>
**CORE-C2 — Tirage dépendant (tirer une valeur, puis construire une recette à partir d'elle).**
*Qui :* 01, 02, 07, 17. *Aujourd'hui :* il n'y a ni `Bind` ni `SelectMany`. Les participants ont imbriqué
`Generate()` dans `As`/`Combine`. Leurs sondes montrent que cela rejoue sous une graine ambiante, mais
rien ne le documente, et sous un `AnyContext` le tirage imbriqué viendrait de la source ambiante (le cas
de rejeu partiel de l'ADR-0102). *Clair ?* Il n'y a pas de refus : la lacune est silencieuse.
*Options :* documenter le `Generate()` imbriqué comme un idiome pris en charge, avec ses limites de
rejeu / ajouter un combinateur de liaison (bind) / faire les deux. Un premier pas plus modeste est
l'idée (a) de CLI-E1.

<a id="core-c3"></a>
**CORE-C3 — Contrainte de jour de semaine / jour ouvré.**
*Qui :* 01, 02. *Aujourd'hui :* aucun membre. Les participants ont utilisé un ensemble `OneOf`
précalculé (01) ou un `Except` sur environ 1 148 dates de week-end (02). *Clair ?* Ni refus ni idiome
documenté. *Options :* un filtre `DayOfWeek`, constructif sur l'espace ordinal / documenter l'idiome de
l'ensemble pour les calendriers de jours fériés, qui sont des données du domaine et sortent sans doute
du périmètre de l'ADR-0046.

<a id="core-c4"></a>
**CORE-C4 — Un pas décimal / `MultipleOf` sur `AnyDecimal`.**
*Qui :* 02 (pas de cotation), 18 (notes au demi-point, un ensemble de 41 valeurs). Le domaine 17 a
demandé une grille sur `AnyDouble`, ce que l'ADR-0015 refuse délibérément (« 0.1 is not
representable »). *Aujourd'hui :* `WithScale` uniquement, et il ne se déclare qu'une fois (ADR-0015).
*Clair ?* L'absence est claire, et le contournement est `Any.Int32().Between(..).As(k => k * 0.25m)`.
*Options :* `AnyDecimal.MultipleOf(decimal)` sur la mécanique de grille existante / documenter le
contournement par `As`. La propre condition de réexamen de l'ADR-0015 s'applique.

<a id="core-c5"></a>
**CORE-C5 — `Guid` de version 4 selon la RFC 9562.**
*Qui :* 19. *Aujourd'hui :* `Any.Guid()` tire 128 bits aléatoires, si bien que le quartet de version est
arbitraire, et un code qui vérifie `Guid.Version == 4` rejette la plupart des tirages. *Clair ?* Ni
membre ni mention dans la documentation. *Options :* `Version4()`/`WithVersion(4)`, qui fixe 6 bits de
façon constructive / documenter le masque via `.As(...)`.

<a id="core-c6"></a>
**CORE-C6 — Séries ordonnées (monotones).**
*Qui :* 12 (une courbe de charge à la demi-heure). *Aujourd'hui :* aucun membre. Le contournement
consiste à trier un tirage distinct dans `As`, ou à cumuler des incréments positifs. *Options :* une
contrainte de collection `Ordered()`/`StrictlyIncreasing()`, constructive / documenter le contournement.

<a id="core-c7"></a>
**CORE-C7 — Un tirage `DateTime` doit-il hériter du `Kind` que partagent ses bornes ?**
Découle de [CORE-D7](#core-d7). *Options :* garder `Utc` et le documenter / hériter du `Kind` quand
toutes les bornes partagent un `Kind` autre que `Utc` / refuser les bornes mixtes ou `Local`. Chaque
option change le contrat, et demande donc une ADR.

<a id="core-c8"></a>
**CORE-C8 — Granularité sur l'horloge locale sous un décalage déclaré.**
Découle de [CORE-D9](#core-d9). *Qui :* 07, 09, 19. *Options :* garder la grille UTC et la documenter /
ancrer la grille à `MinValue − offset` quand le décalage est fixe (constructif) / refuser
`WithGranularity` combiné à `WithOffsetBetween`.

<a id="core-c9"></a>
**CORE-C9 — Laisser un second `OneOf`, sous-ensemble du premier, intersecter avec lui.**
*Qui :* 05. *Aujourd'hui :* refusé avec « is already defined » (ADR-0100, JD034). *Options :* le garder
et documenter `Except` ([DOC-9](#doc-9)) / rouvrir l'ADR-0100 pour qu'un sous-ensemble intersecte, comme
le fait une borne.

<a id="core-c10"></a>
**CORE-C10 — Un moyen public pour un générateur utilisateur ou scaffoldé de déclarer qu'il tire depuis la source ambiante.**
*Qui :* 06, 18. *Aujourd'hui :* `IHasRandomSource` et `IReproducibilityHint` sont internes, si bien que
l'indication de rejeu complet ne peut jamais s'appliquer à un générateur `any`. *Options :* un
marqueur public émis par le scaffolder / garder seulement la réserve ([CORE-D3](#core-d3)).

<a id="cli-c1"></a>
**CLI-C1 — Les grands plafonds de comptage orientent les agrégats scaffoldés.**
*Qui :* 20. *Aujourd'hui :* la garde `readings.Count > 500_000` devient `.WithMaxCount(500000)`, et sous
l'ADR-0076 un maximum oriente le tirage. Un `AnyReadingBatch` par défaut a tiré 406 641 relevés en
environ 2 s, et il a fallu tuer le banc d'essai. Le récapitulatif dit seulement `guard`. Restreindre
`AnyValidReadings()` fait ensuite entrer en conflit des affinages tels que
`WithReadings(r => r.WithCount(100_000))`, ce qui est correct sous l'ADR-0109. Restreindre seulement
l'argument du constructeur par défaut a fonctionné. *Clair ?* Aucun avertissement. *Options :* le
récapitulatif signale un plafond issu d'une garde au-delà d'un seuil et suggère de restreindre le
constructeur par défaut / émettre une fenêtre de taille par défaut à l'intérieur de toute la base /
réexaminer la clause de l'ADR-0076 sur les collections pour les éléments composites. Non réexécuté par le
vérificateur : le mécanisme est montré dans [`repros/20-4`](repros/20-4).

<a id="cli-c2"></a>
**CLI-C2 — Les plafonds de comptage au-delà de la limite du million sont rapportés comme non lus.**
*Qui :* 20. *Aujourd'hui :* `if (values.Count > 2_000_000) throw` est marqué `to verify, unread guards`,
et bloque le build. Les tailles sont plafonnées à 1 000 000 (ADR-0076), donc la garde est respectée par
construction. Le §5.3 de la spécification décide le refus, mais la raison qu'il donne (« would throw
inside the emitted constructor ») explique pourquoi rien n'est émis, pas pourquoi la garde est dite non
lue. *Options :* lire la garde comme satisfaite par construction (`guard`, plus une note dans le
récapitulatif « above the size cap ») / garder le refus et corriger la justification de la
spécification et le texte du récapitulatif. Repro : [`repros/20-4`](repros/20-4).

<a id="cli-c3"></a>
**CLI-C3 — Privilégier les types du projet sur les types du framework de même nom.**
*Qui :* 03 (`Claim`), 04 (`Machine`), 12. *Aujourd'hui :* chaque type de bonne arité et de bonne
visibilité concourt à égalité, et le refus est clair et liste les candidats (§3.2). *Options :*
privilégier les types déclarés dans le source des projets référencés avant d'élargir aux métadonnées /
garder le comportement et s'en remettre au nom complet. Cela demande une décision au §3.2 ou une ADR.
Le filtre d'arité et d'accessibilité est un bogue ([CLI-B6](#cli-b6)).

<a id="cli-c4"></a>
**CLI-C4 — Lire les gardes d'intervalle en motif `or` et en `||`.**
*Qui :* voir CLI-G1/G2 (10 domaines). *Aujourd'hui :* ces gardes sont signalées honnêtement. Un motif
`or` de deux motifs relationnels constants sur un même paramètre correspond exactement à `Between`.
*Options :* élargir le lecteur pour cette seule forme, ce que l'ADR-0085 permet face à un retour de
terrain / garder le comportement actuel.

<a id="cli-c5"></a>
**CLI-C5 — Transformer une garde par regex littérale en `Any.StringMatching`.**
*Qui :* 02, 07, 15. *Aujourd'hui :* non lue, par conception (spécification §5.3). La spécification en
fait un candidat pour la v1.1, et 4 motifs réalistes sur 5 sortent du sous-ensemble régulier
(ADR-0008). *Options :* émettre `StringMatching` seulement quand le motif s'analyse dans le
sous-ensemble, sinon garder le TODO / garder le comportement actuel, avec [DOC-7](#doc-7).

<a id="an-c1"></a>
**AN-C1 — Une règle pour une valeur tirée qui atteint une assertion.**
*Qui :* 02, 03, 05, 06, 07, 10, 16, 19. Cela fait huit domaines, et tous citent le corollaire du premier
fondamental : « the value the assertion is about is not a dummy ». *Aujourd'hui :* aucune règle.
*Clair ?* Silencieux. *Options :* une règle de niveau Info pour une variable locale nommée `any…`, ou une
valeur renvoyée par `Generate()`, qui aboutit dans `Assert.*` / une position purement documentaire.
L'analyse de flux de données à travers les helpers serait heuristique, donc la portée doit être bornée
d'emblée.

<a id="an-c2"></a>
**AN-C2 — JD023 au-delà des entiers.**
*Qui :* 02, 09, 13, 16, 17, 18 (bornes constantes croisées en décimal, en double et en `TimeSpan`, une
faute de frappe typique sur une plage de référence). *Aujourd'hui :* « Integer generators only » est
documenté. *Options :* étendre aux ordinaux décimaux et temporels, qui se replient exactement / garder
les entiers seulement, et laisser la virgule flottante à l'écart.

<a id="an-c3"></a>
**AN-C3 — Les membres scaffoldés `--entry-point any` sont invisibles pour JD007/JD009.**
*Qui :* 03, 07, 11, 14, 16. *Aujourd'hui :* `Any.Leg()` se compile en un membre de `AnyLegEntry`, et le
parcours s'arrête aux extensions du consommateur par conception (`GeneratorFacts.cs:71, 87-94`).
`new AnyX()` et les variables locales sont des limites documentées, et un `Any.Combine` en ligne *est*
vu. *Options :* faire confiance à un membre C# 14 `extension(Any)` dont le corps renvoie un nouvel
`IAny<T>`, ou qui porte un marqueur de scaffolding / documenter la limitation ([DOC-13](#doc-13)). Repro :
[`repros/analyzers-scaffolded-iany-blind`](repros/analyzers-scaffolded-iany-blind).

<a id="an-c4"></a>
**AN-C4 — Les manques restants sur les constantes.**
*Qui :* 01, 05, 09, 10, 12, 13, 15, 19. Chaque manque sort de la portée documentée de sa règle
(ADR-0106 : « a question of scope goes to a decision rather than to a test ») :

- JD016 ne compte pas les familles `AnyChar` (`Numeric()` = 10) ;
- JD014 ne vérifie pas un `Between` inversé sur `DateOnly`, `DateTime`, `TimeOnly`,
  `DateTimeOffset` ou `TimeSpan` ;
- JD034 n'a pas de paire `Empty()`/`NonEmpty()` pour `Guid`.

*Options :* accepter chacun comme une petite extension saine / documenter la portée. Repro :
[`repros/analyzers-constant-misses`](repros/analyzers-constant-misses).

<a id="an-c5"></a>
**AN-C5 — Constructions regex non prises en charge, au build.**
*Qui :* 08. *Aujourd'hui :* `UnsupportedRegexException` au premier appel. C'est clair, et cela nomme la
construction et sa position. *Options :* une règle qui analyse les motifs constants avec le propre
analyseur syntaxique de la bibliothèque / rien.

<a id="an-c6"></a>
**AN-C6 — Une lambda d'affinage qui ignore son argument.**
*Qui :* 18. *Aujourd'hui :* `WithCredits(_ => Any.Int32())` remplace la base, et seule la garde du
domaine le détecte. *Options :* une règle Info sur une lambda `With…(Func<…>)` dont le paramètre est
inutilisé / [DOC-5](#doc-5) seulement.

---

## 9. Frontières assumées rencontrées

Chaque frontière ci-dessous est décidée par une ADR, la spécification ou la documentation utilisateur.
La dernière colonne juge le message de refus que les participants ont effectivement vu.

| Frontière | Décidée par | Domaines | Refus clair ? |
|---|---|---|---|
| Constructions regex hors du sous-ensemble régulier (lookarounds, `\b`, `\p{…}`, références arrière, soustraction de classes, groupes atomiques, `(?i:)` à portée) | ADR-0008 | sondé dans les 20 | **Oui** : construction et position. La liste finale est figée plutôt que spécifique ([DOC-4](#doc-4)). |
| `RegexOptions.IgnorePatternWhitespace` | `Any.Pattern.cs:71` | 03, 06, 08, 10, 11, 13 | Oui, en `ArgumentException`. Absent de la page ([DOC-4](#doc-4)). |
| Un élément chaîne ou motif sans cardinalité annoncée échoue via le tirage borné | ADR-0004, ADR-0009, ADR-0027 | 01, 04, 10, 19 | Oui, en `AnyGenerationException` avec la graine et un extrait de rejeu. |
| Les fabriques statiques à deux arguments ne sont pas reconnues | Spécification §5.1 règle 2, ADR-0089 | 10 (`ExpiryMonth.Of(year, month)`) | Oui, nomme la règle. L'ADR-0085 est l'écrit à rouvrir face à ce rapport. |
| Les gardes par regex ne sont pas lues | Spécification §5.3 (ligne 827), candidat v1.1 | 02, 03, 05, 06, 07, 11, 12, 15 | Signalées `to verify`. La page utilisateur est muette ([DOC-7](#doc-7)). |
| Règles entre paramètres | ADR-0060, ADR-0083, spécification §16 | les 20 | Signalées et bloquantes. Voir [CLI-E1](#cli-e1). |
| `Any.Int32().Between(10, 1)` lève `ArgumentException`, pas un conflit | `errors-and-conflicts` (« Ordinary argument mistakes »), page JD014 | 11 | Oui, et JD014 le signale au build ([DOC-12](#doc-12) ajoute une ligne). |
| Tailles au-delà de 1 000 000 | ADR-0076 | 17, 20 | Oui, à la déclaration ; JD014 signale les constantes. |
| Tirages en virgule flottante dans une magnitude ordinaire (`Between(-MaxValue, MaxValue)` reste en deçà d'environ 1e6) | ADR-0031 | 05, 20 | Pas un refus ; documenté. |
| `WithScale` déclaré une seule fois ; pas de grille en virgule flottante binaire | ADR-0015 | 02, 17 | Oui. |
| Un ensemble de valeurs (`OneOf`) déclaré une seule fois par générateur | ADR-0100, JD034 | 05 | « Is already defined » : clair sur la règle, mais la page de la CLI ne prépare pas le lecteur ([DOC-9](#doc-9)). |
| La surcharge d'affinage ne prend pas d'`IAny<T>` | ADR-0109 | 20 | `CS0266` ne l'enseigne pas ([DOC-10](#doc-10)). |
| Un `IAny<T>` étranger compte comme une source distincte pour le rejeu | ADR-0102 | 06, 18 | La réserve est voulue ; sa formulation en dit trop ([CORE-D3](#core-d3)). |
| La recette par défaut d'un paramètre nullable ne tire jamais null | ADR-0064 | 01, 02, 05, 15 | Respecté. Le type de la méthode qui fixe la valeur relève de [CLI-D3](#cli-d3). |
| Une ligne de chaîne scaffoldée déclenche JD030 (Info) | Spécification §5.2 et §17 (les tests qui compilent la sortie excusent JD030 nommément) | 04 | Voulu ([DOC-11](#doc-11)). |
| JD028 est muet sur les générateurs scaffoldés ou conservés ; JD007/JD009 sont muets sur `new AnyX()` et les variables locales | Pages JD028/JD007/JD009, « What it does not flag » | 01, 06, 11, 14, 15, 17, 18 | Documenté. La graphie `Any.X()` relève de [AN-C3](#an-c3). |
| JD032 ne couvre pas `Between` | Page JD032, ADR-0078 | 14 | Documenté (« still covered by JD024 »). |
| Les règles de conflit sur constantes sont muettes dans `Assert.Throws` | Pages JD014/JD016/JD023 | 04, 15 | Documenté. L'incohérence JD014/JD023 relève de [AN-D3](#an-d3). |
| JD023 ne couvre que les générateurs entiers | Page JD023 | 02, 09, 13, 17, 18 | Documenté ([AN-C2](#an-c2)). |
| `--force` écrase le fichier généré | Message de refus et en-tête | 09, 10, 12 | Oui, annoncé. Idée (c) de [CLI-E1](#cli-e1). |

`TimeOnly` à cheval sur minuit n'est **pas** listé : aucune ADR ne le décide ([CORE-C1](#core-c1)).

---

## 10. Ce qui fonctionne bien

Chaque point ci-dessous s'appuie sur les notes et les sondes d'au moins un domaine.

* **Les messages de conflit nomment les deux côtés, y compris entre une base scaffoldée et un affinage
  de test.** Par exemple : « Cannot apply GreaterThan(400) because Between(1, 365) already requires
  values less than or equal to 365. » (03), et « Cannot apply Before(1890-01-01) because
  Between(1900-01-01, 2025-12-31) already requires… » (08). Les 20 domaines l'ont salué. Les exceptions
  sont les éléments de formulation décimale/double du §3.
* **Les refus de regex nomment la construction et sa position :** « uses a negative lookahead '(?!…)'
  at position 8 » (18). Chaque format dont les domaines avaient réellement besoin a été généré
  correctement. Exemples : IBAN et BIC (01), chaînes GS1 avec `\x1D` (10), E.164 (11), SSCC avec groupes
  nommés et `\A..\z` (06), et ISIN et horodatages FIX (02). `\d` ne tire que des chiffres ASCII.
* **Le rejeu depuis la graine fonctionne de bout en bout.** `[Reproducible]` affiche un
  `[Reproducible(Seed = …)]` prêt à copier-coller. Le fixer a reproduit la valeur fautive exacte dans
  01, 04, 06, 07, 08, 11, 13, 14, 16, 17 et 19. `Any.UseSeed`, `WithSeed` et `Reproducibly` ont rejoué des
  graphes scaffoldés entiers : une nomenclature de profondeur 3 (05), un `PaymentBatch` avec des tirages
  imbriqués (01), et un `Flight` (16). Un lot de 100 000 relevés a été rejoué champ par champ (20).
* **L'inspection des ensembles explique pourquoi une valeur n'est jamais tirée :** par exemple
  « 376012345678 rejected by WithLength(13) » (06), « N12345 by StringMatching(…) » (16), et « 12.025
  rejected by WithScale » (13).
* **Les récapitulatifs sont honnêtes dans la grande majorité des cas.** Quatorze domaines n'ont
  enregistré aucune garde manquée silencieusement : tous les domaines sauf 04, 06, 07, 14, 16 et 19 (le
  domaine 20 a sondé huit formes délibérément). Les gardes lues ont été respectées dans des bancs
  d'essai de 500 tirages (02 : 7 × 500 tirages, 08 : 7 × 500). La ligne `TODO_verify` qui ne compile pas
  transforme la sortie du build en une liste de travail exacte (04, 10, 16). Les cas silencieux se
  limitent à [CLI-B1](#cli-b1)–[CLI-B4](#cli-b4).
* **La conception `With…` à lambda d'affinage (ADR-0109)** se lit comme la règle métier :
  `Arbitrary.CreditTransfer().WithDebtor(p => p.InCountry("DE"))` (01),
  `WithDriver(d => d.WithYearsLicensed(y => y.GreaterThanOrEqualTo(3)))` (03),
  `WithRoute(route => route.WithDistanceKm(5_800))` (16). Un affinage qui contredit la base échoue en
  nommant les deux côtés (07, 10, 14). Le domaine 02 l'a qualifiée de « a real improvement over
  replacing a generator ».
* **La composition via `AnyX` (ADR-0089)** a fonctionné du premier coup à travers des agrégats imbriqués
  (14), et un constructeur à dix paramètres a été scaffoldé sans heurter le plafond d'arité (15).
* **Le traitement des cas limites des nombres et du temps est exact.** Le zéro signé n'est jamais tiré
  par `NonZero`/`Negative`. Les intervalles d'un ulp, les intervalles sous-normaux, les échelles de
  `Half`, `DateOnly`/`DateTime` à `Min`/`MaxValue`, `Between(d, d)`, et les grilles de granularité sans
  aucun point dans l'intervalle se comportent tous correctement, et le dernier cas est refusé en
  nommant les deux côtés (05, 07, 09, 12, 15, 16, 20).
* **Le rapport `--format json` est propre et vérifiable par lui-même**, hormis [CLI-B5](#cli-b5) (03,
  05, 09, 12, 18). Les réexécutions avec `--force` sont identiques à l'octet près (15).
* **Les analyseurs se déclenchent avec précision sur les erreurs de constantes**, sans faux positif sur
  le code scaffoldé ou réel. Parmi les règles qui se sont déclenchées : JD004, JD006, JD013–JD018,
  JD023, JD025–JD027, JD034 et JD035. JD018 a détecté un `Any.Reproducibly` imbriqué sous
  `[Reproducible]` (08, 13, 17). JD014 a dit « the two arguments look transposed » (13).
* **Des valeurs arbitraires ont révélé de vrais défauts dans les propres domaines des participants**,
  ce qui est le fondamental 1 en action. Dans le domaine 17, `AddYears(10)` a débordé près de
  `DateOnly.MaxValue`. Dans le domaine 16, le banc d'essai de 500 tirages a détecté un motif
  d'immatriculation erroné écrit à la main.

---

## 11. Résultats par domaine

L'aisance est le score que le participant s'attribue lui-même, de 1 à 5 : un signal relatif, pas une
mesure. Les nombres de tests proviennent d'une exécution mesurée.

| Domaine | Modèle | Tests | Aisance | Commentaire | Notes |
|---|---|---|---|---|---|
| 01 payments | Virement SEPA : IBAN mod-97, BIC, Money, date en jour ouvré, lot pain.001 | 55 / 57 (2 `Bug_`) | 4 | Côté tests très fluide ; les chiffres de contrôle et les règles entre champs ont demandé le travail manuel | [NOTES](domains/01-payments/NOTES.md) |
| 02 trading | Carnet d'ordres : ticker, prix limite/stop, pas de cotation, séance | 59 / 59 | 4 | Les littéraux et les affinages par lambda disent ce qu'ils veulent dire ; la grille de pas et les liens entre prix demandent des retouches manuelles | [NOTES](domains/02-trading/NOTES.md) |
| 03 insurance | Police auto : période de couverture, franchise, bonus-malus, sinistres | 47 / 49 (2 `Bug_`) | 4 | Affinages imbriqués agréables ; les deux règles entre paramètres corrigées à la main | [NOTES](domains/03-insurance/NOTES.md) |
| 04 manufacturing | Ordre de fabrication MES : code de lot, cadence, tolérances, états machine | 66 / 68 (2 `Bug_`) | 4 | Rejeu trivial, messages excellents ; une recette silencieuse et le scaffolding d'énumération ont coûté de la confiance | [NOTES](domains/04-manufacturing/NOTES.md) |
| 05 CAD drawing | Ajustements ISO 286, références de plans, nomenclature récursive | 57 / 58 (1 `Bug_`) | 3 | Bon DSL scalaire et de motifs ; le scaffolding récursif a fait planter l'hôte | [NOTES](domains/05-cad-drawing/NOTES.md) |
| 06 warehouse | Réception de palettes : SSCC, EAN-13, emplacement, quantités | 64 / 67 (3 `Bug_`) | 4 | Le DSL de base se lit comme le domaine ; deux manques silencieux de la CLI | [NOTES](domains/06-warehouse/NOTES.md) |
| 07 freight | Expédition : poids, volume, ISO 6346, ETA entre fuseaux | 62 / 63 (1 `Bug_`) | 4 | Fluide ; 3 générateurs sur 7 restructurés ; une garde silencieuse | [NOTES](domains/07-freight/NOTES.md) |
| 08 patient record | Prescription : NIR, molécule, posologie, voie, date de naissance | 55 / 55 | 4 | Messages et rejeu excellents ; somme de contrôle et règles entre champs réécrites | [NOTES](domains/08-patient-record/NOTES.md) |
| 09 laboratory | Résultats de laboratoire, plages de référence, unités, heures de prélèvement | 67 / 67 | 4 | Contraintes fluides ; invariants entre champs dans un fichier que `--force` écrase | [NOTES](domains/09-laboratory/NOTES.md) |
| 10 pharma serialisation | Boîtes : GTIN, lot, péremption, unicité des numéros de série | 57 / 57 | 4 | Chaque nom deviné existait ; le chiffre de contrôle GTIN a imposé des retouches de signature | [NOTES](domains/10-pharma-serialisation/NOTES.md) |
| 11 telecom | Ligne mobile : MSISDN, IMEI (Luhn), ICCID, forfait, consommation | 49 / 49 | 4 | Les blocs Arrange montrent le choisi face à l'arbitraire ; résoudre « to verify » réécrit plus que ce qui est documenté | [NOTES](domains/11-telecom/NOTES.md) |
| 12 smart grid | Index de compteur, point de livraison, courbe de charge à la demi-heure | 37 / 39 (2 `Bug_`) | 3 | Affinages imbriqués exacts ; séries ordonnées et règles entre champs ont réécrit 4 fichiers sur 8 | [NOTES](domains/12-smart-grid/NOTES.md) |
| 13 payroll | Bulletin de paie : contrat, heures, primes, retenues | 43 / 44 (1 `Bug_`) | 4 | Se lit comme le domaine ; la confusion entre valeur fixée et lambda a causé une exécution rouge | [NOTES](domains/13-payroll/NOTES.md) |
| 14 e-commerce | Panier et commande : SKU, quantité, remise, adresse | 47 / 48 (1 `Bug_`) | 4 | Naturel ; un manque silencieux ; les règles entre éléments ont perdu des cas | [NOTES](domains/14-ecommerce/NOTES.md) |
| 15 hotel booking | Séjour, chambre, clients, conditions d'annulation | 48 / 48 | 4 | Fluide ; l'intervalle du séjour a exigé des générateurs restructurés à la main | [NOTES](domains/15-hotel-booking/NOTES.md) |
| 16 aviation | Vol : codes IATA/OACI, immatriculation, créneau | 66 / 67 (1 `Bug_`) | 4 | Le vocabulaire temporel convient aux créneaux ; une recette silencieuse ; gardes temporelles non lues | [NOTES](domains/16-aviation/NOTES.md) |
| 17 real estate | Cadastre, diagnostic énergétique, annonces | 50 / 50 | 3 | Les objets valeur se lisent bien ; chaque agrégat avait une règle entre champs | [NOTES](domains/17-real-estate/NOTES.md) |
| 18 education | Inscription, notes au demi-point, semestres | 51 / 51 | 4 | Contraintes et graines fluides ; la note au demi-point exige un ensemble de 41 valeurs | [NOTES](domains/18-education/NOTES.md) |
| 19 IAM | Jeton d'accès, rôles, scopes, expiration, URI de redirection | 61 / 63 (2 `Bug_`) | 4 | `Any.Uri()` est excellent ; un récapitulatif « guard » silencieux et le scaffolding d'énumération | [NOTES](domains/19-iam/NOTES.md) |
| 20 agritech IoT | Capteurs de terrain : GPS, humidité, batterie en `Half`, lots de 500k | 56 / 58 (2 `Bug_`) | 4 | Le vocabulaire numérique colle à la géodésie ; un grand plafond rend le dummy par défaut énorme | [NOTES](domains/20-agritech-iot/NOTES.md) |
| **Total** | | **1097 / 1117** (20 rouges, tous `Bug_`) | | | |

---

## 12. Constats rejetés ou déclassés

| Remonté comme | Verdict | Pourquoi |
|---|---|---|
| `WithScale(2)` au-delà d'environ 7,92e26 renvoie une échelle inférieure, « bogue de gravité élevée » (03) | Déclassé en défaut de documentation ([CORE-D8](#core-d8)) | Chaque valeur est sur la grille de 10⁻ⁿ que décide l'ADR-0015 ; le complément de `decimal.Scale` par des zéros y a été rejeté. Le test de l'essai qui vérifiait `decimal.Scale` a été réécrit en contrôle de grille (`WithScale_2_near_decimal_MaxValue_stays_on_the_lattice_without_trailing_zeros`), et passe. |
| `WithScale` ignoré quand l'intervalle ne contient qu'une valeur (18) | Idem | `Between(20m, 20m).WithScale(1)` renvoie `20`, qui est sur la grille de 0,1. Le test de l'essai a été réécrit de même (`WithScale_on_a_single_value_range_keeps_the_value_on_the_lattice`), et passe. |
| Le scaffolder émet une chaîne que son propre analyseur signale, JD030 (04) | Voulu | Spécification §5.2/§17. Inventer une longueur reviendrait à revendiquer un invariant que le domaine n'a jamais déclaré ([DOC-11](#doc-11)). |
| `Between(10, 1)` devrait lever un conflit, pas `ArgumentException` (11) | Voulu | Erreur d'argument documentée, détectée par JD014. |
| `PublicAPI.Shipped.txt` est vide (05, 17) | Pas un défaut visible de l'utilisateur | Un artefact pré-1.0 du banc d'essai : la surface vit dans `Unshipped` jusqu'à la première version publiée. |
| JD028 muet sur les générateurs scaffoldés ou conservés (01, 06, 14, 15, 17, 18) | Voulu | Documenté : « only a chain that provably builds a new value here qualifies ». L'utilisateur possède le fichier et peut renvoyer des instances partagées. |
| `Any.Combine` invisible pour JD007/JD009/JD028 (07) | Réfuté pour l'usage en ligne | Un `Combine` en ligne déclenche les trois. Un générateur conservé dans une variable locale est une limite documentée. |
| JD032 ne couvre pas `Between` (14) | Voulu | Page JD032 et ADR-0078 : « still covered by JD024 ». |
| `Double().Between(0, 100).Between(50, 200)` « échoue toujours » | Réfuté | Il se replie en [50, 100]. |
| Le silence des règles de conflit sur constantes dans `Assert.Throws` n'est pas documenté (04) | Réfuté | Il est documenté. L'incohérence restante relève de [AN-D3](#an-d3). |
| Le contrôle de cardinalité n'est pas uniforme entre char et string (10) | Voulu | L'ADR-0004 décide le repli par tirage borné. Seul le moment du contrôle est un défaut ([CORE-D10](#core-d10)). |
| `AnyPattern` « ne peut pas être affiné » (18) | Exagéré | `OneOf`, `Except` et `DifferentFrom` existent ; on restreint la regex en la modifiant. |
| Une garde entre champs qui lit deux paramètres n'en signale qu'un (17) | Déclassé | Cela ne se produit que via une variable locale dérivée (le résidu du §9). Le build reste bloqué par le paramètre signalé. Couvert par l'amélioration (b) de [CLI-E1](#cli-e1). |
| Hors d'une portée, la graine rapportée « ne peut pas rejouer le tirage fautif » (05) | Voulu, sauf le cas asynchrone | Un corps synchrone rejoue depuis son premier tirage. La variante asynchrone est [CORE-B1](#core-b1). |
| « Les gardes de comptage ne sont pas lues » (18) | Exagéré | `Count > N` → `WithMaxCount`, `Count == N` → `WithCount` et `Count == 0` sont lus (06, 12, 15). |
| Une constante nommée est remplacée par un littéral (07) | Cosmétique | La valeur repliée est correcte. |
| `With…(Func<AnyX, AnyX>)` ne peut pas prendre un `IAny<T>` préparé (20) | Voulu (ADR-0109) | Documentation seulement : [DOC-10](#doc-10). |
| `WithScale` ne se déclare qu'une fois et ne peut être restreint ; pas de grille sur `AnyDouble` (02, 17) | Voulu (ADR-0015) | Un pas décimal relève de [CORE-C4](#core-c4). |
| Des bornes constantes décimales/double/date croisées compilent sans rien dire (nombreux) | Hors de la portée documentée | JD023 est réservé aux entiers selon sa page : [AN-C2](#an-c2). |
| `--force` écrase les bases résolues à la main (10) | Comportement annoncé | Idée (c) de [CLI-E1](#cli-e1). |

---

## 13. Prochaines étapes

1. **Issues.** Ouvrir une issue par cause racine, regroupées par portée, en citant ces identifiants. Ne
   le faire **qu'avec l'accord du mainteneur**.
   * `cli` : CLI-B1–B6 (B7 est #296), CLI-D1–D5.
   * `core` : CORE-B1–B3, CORE-D1–D10.
   * `analyzers` : AN-D1–D5, AN-F1–F2.
   * `xunit` : XU-D1.
   * Documentation : DOC-1–13. Chaque ligne relève du train que nomme son composant.
   * Les candidats (§8) vont au mainteneur comme demandes de décision, pas comme bogues. La plupart
     aboutiraient à une ADR ou à un refus documenté.
2. **Bogues, du rouge au vert.** Pour chaque bogue, écrire le test rouge nommé dans sa ligne, dans la
   suite qui y est nommée (l'ADR-0019 décide entre `JustDummies.UnitTests` et `.PropertyTests`). Placer
   le test et le correctif dans le même commit `fix(<scope>)`, avec `Refs: #NN`. Le texte de
   spécification ou d'ADR que modifie un correctif (spécification §5.1/§5.3/§5.6/§6.1/§7/§9, ADR-0004)
   change avec lui, ainsi que son jumeau français.
3. **Confirmer sur l'essai.** Réempaqueter (`tools/ux-trial/prepare.sh`) et relancer les domaines
   concernés.
   * Les tests `Bug_` listés pour CORE-B3, CORE-D1 et CORE-D2 devraient passer au vert en l'état.
   * Pour CLI-B1, CLI-B2 et CLI-B4, les tests `Bug_` rejouent des recettes ou des fichiers émis figés.
     Confirmer plutôt sur un nouvel scaffolding avec `--force` : le récapitulatif doit marquer le
     paramètre, ou refuser l'énumération. Retirer ou réécrire ensuite ces tests.
   * Les deux tests `WithScale` (03, 18) qui vérifiaient un contrat rejeté ont déjà été réécrits en
     contrôles de grille (§12).
4. **Les décisions d'abord, là où un correctif en dépend :**
   * le choix refuser-ou-déléguer pour une cible énumération ([CLI-B2](#cli-b2)) ;
   * le texte du commentaire au §5.6 de la spécification ([CLI-D1](#cli-d1)) ;
   * le propriétaire de l'appartenance à une énumération ([AN-D1](#an-d1)) ;
   * l'ADR qui amende l'ADR-0004 ([CORE-D10](#core-d10)) ;
   * `TimeOnly` à cheval sur minuit ([CORE-C1](#core-c1)).
