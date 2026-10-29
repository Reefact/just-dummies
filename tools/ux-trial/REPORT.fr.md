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

> **État.** La milestone 7 est close. CLI-B1 à CLI-B4 sont corrigés dans [#331](https://github.com/Reefact/just-dummies/pull/331). Les suites le sont
> aussi : la récursion mutuelle ([#332](https://github.com/Reefact/just-dummies/issues/332)), et le lecteur de gardes marque désormais tout paramètre
> dont un `throw` peut dépendre ([#333](https://github.com/Reefact/just-dummies/issues/333), ADR-0110). Toutes les formes de constructeur réunies pendant
> la revue de #331 sont marquées ou lues comme il se doit. La milestone 8 (CORE-B1, CORE-B2, CORE-D1 à CORE-D3)
> est fusionnée elle aussi, de même que [#341](https://github.com/Reefact/just-dummies/pull/341) : un conflit décimal sur une
> grille `WithScale` ne cite plus la contrainte refusée comme sa propre cause ([#340](https://github.com/Reefact/just-dummies/issues/340)). La milestone 9 (CLI-B5 à CLI-B7, CLI-D1,
> CLI-D3 à CLI-D5) est fusionnée elle aussi, chaque élément confirmé en relançant `any` sur sa reproduction, de même que la milestone 10 (AN-D1 à
> AN-D3, AN-F1, AN-F2), chaque élément confirmé en recompilant sa reproduction, avec ses suites [#346](https://github.com/Reefact/just-dummies/pull/346) et [#347](https://github.com/Reefact/just-dummies/pull/347). Sur cet état, l'essai passe 1099 tests sur 1101, les 2 rouges étant les tests `Bug_`
> de [#310](https://github.com/Reefact/just-dummies/issues/310). Les autres éléments ci-dessous restent ouverts dans leurs issues.

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

**Corrigé dans [#331](https://github.com/Reefact/just-dummies/pull/331)** (suivi dans [#297](https://github.com/Reefact/just-dummies/issues/297)).

<a id="cli-b2"></a>
#### CLI-B2 — `any generate <Enum>` annonce ✓ et émet `return new TEnum();` (élevée)

**Corrigé dans [#331](https://github.com/Reefact/just-dummies/pull/331)** (suivi dans [#298](https://github.com/Reefact/just-dummies/issues/298)).

<a id="cli-b3"></a>
#### CLI-B3 — Un type autoréférent est scaffoldé en une récursion non bornée (élevée)

**Corrigé dans [#331](https://github.com/Reefact/just-dummies/pull/331)** (suivi dans [#299](https://github.com/Reefact/just-dummies/issues/299)).

<a id="cli-b4"></a>
#### CLI-B4 — Une garde atteinte via une variable locale dérivée est silencieuse alors que le récapitulatif dit « inferred » (élevée)

**Corrigé dans [#331](https://github.com/Reefact/just-dummies/pull/331)** (suivi dans [#300](https://github.com/Reefact/just-dummies/issues/300)).

<a id="cli-b5"></a>
#### CLI-B5 — `--format json` rapporte une écriture refusée comme `Scaffolded` (moyenne)

**Corrigé dans la milestone 9** (suivi dans [#301](https://github.com/Reefact/just-dummies/issues/301)) : `--format json` rapporte désormais l'écriture refusée comme `FileExists`, `written: false`, `failed: 1`, code de sortie 1.

<a id="cli-b6"></a>
#### CLI-B6 — La recherche par nom court liste des candidats que C# ne pourrait jamais lier, et l'en-tête nomme une commande qui échoue (moyenne)

**Corrigé dans la milestone 9** (suivi dans [#302](https://github.com/Reefact/just-dummies/issues/302)) : `Measurement` se résout vers le type du projet (l'arité et l'accessibilité filtrent les candidats) ; l'en-tête donne le nom complet quand le nom court est ambigu.

<a id="cli-b7"></a>
#### CLI-B7 — `any generate` sur un projet non restauré prétend que le projet ne référence pas JustDummies (déjà ouvert)

**Corrigé dans la milestone 9** (ouvert sous [#296](https://github.com/Reefact/just-dummies/issues/296)) : un projet non restauré est désormais refusé avec « this project has not
been restored, so its package references cannot be read », qui indique `dotnet restore`, code de sortie 1. Tous les domaines
ont lancé `dotnet restore` d'abord à cause de lui.

### Défauts (messages, formulation, récapitulatif/JSON)

<a id="cli-d1"></a>
**CLI-D1 — « to verify » ne dit jamais quelle garde n'a pas été lue, et cite le « §9 » de la spécification de maintenance (moyenne).**
**Corrigé dans la milestone 9** (suivi dans [#303](https://github.com/Reefact/just-dummies/issues/303)) : le TODO, le récapitulatif et le JSON nomment la première garde non lue avec son fichier, sa ligne et un extrait, et renvoient vers la page utilisateur du CLI au lieu de « §9 ».

<a id="cli-d2"></a>
**CLI-D2 — « Delete that one line, and you are done » est faux quand la bonne base a un autre type de builder (moyenne).**
Suivi dans [#304](https://github.com/Reefact/just-dummies/issues/304).

<a id="cli-d3"></a>
**CLI-D3 — Un paramètre de type référence nullable perd son `?` dans la méthode qui fixe sa valeur, le récapitulatif et le JSON (moyenne).**
**Corrigé dans la milestone 9** (suivi dans [#305](https://github.com/Reefact/just-dummies/issues/305)) : le pin, le champ et le récapitulatif gardent `Bic?`/`string?` ; `.WithAgent((Bic?)null)` compile sans avertissement sous `-warnaserror`.

<a id="cli-d4"></a>
**CLI-D4 — `--namespace` réécrit le nom du type analysé dans le récapitulatif et le JSON (faible).**
**Corrigé dans la milestone 9** (suivi dans [#306](https://github.com/Reefact/just-dummies/issues/306)) : le récapitulatif et le JSON gardent `Repro.Domain.Sku` ; seul le fichier est dans `Foo.Bar`.

<a id="cli-d5"></a>
**CLI-D5 — `p >= N` / `p <= N` avec N ≠ 0 ne sont pas lus, alors que `ThrowIfGreaterThanOrEqual(p, N)` l'est (faible).**
**Corrigé dans la milestone 9** (suivi dans [#307](https://github.com/Reefact/just-dummies/issues/307)) : `value >= 180.0` donne `.LessThan(180d)` et `value <= 10` donne `.GreaterThan(10)`, tous deux `guard`.

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

Suivi dans [#308](https://github.com/Reefact/just-dummies/issues/308).

<a id="core-b2"></a>
#### CORE-B2 — Les messages d'échec de `As(...)`/`Combine(...)` affichent les chaînes tirées brutes, contrairement à l'ADR-0075 (moyenne)

Suivi dans [#309](https://github.com/Reefact/just-dummies/issues/309).

<a id="core-b3"></a>
#### CORE-B3 — `Any.Double().Between(-double.Epsilon, double.Epsilon)` ne tire jamais que 0 (faible)

Suivi dans [#310](https://github.com/Reefact/just-dummies/issues/310).

### Défauts (messages, formulation, documentation d'un contrat)

<a id="core-d1"></a>
**CORE-D1 — Les messages de conflit décimaux énoncent mal les bornes strictes (moyenne).**
Suivi dans [#311](https://github.com/Reefact/just-dummies/issues/311).

<a id="core-d2"></a>
**CORE-D2 — Une borne décimale stricte qui vide une grille `WithScale` ne nomme qu'un côté (moyenne).**
Suivi dans [#312](https://github.com/Reefact/just-dummies/issues/312).

<a id="core-d3"></a>
**CORE-D3 — L'épuisement de `Distinct` sur un `IAny<T>` étranger affirme comme un fait que ses valeurs « are not reproducible » (moyenne).**
Suivi dans [#313](https://github.com/Reefact/just-dummies/issues/313).

<a id="core-d4"></a>
**CORE-D4 — Les messages de conflit en virgule flottante reformulent une borne stricte comme la valeur représentable suivante (faible).**
Suivi dans [#314](https://github.com/Reefact/just-dummies/issues/314).

<a id="core-d5"></a>
**CORE-D5 — Un décalage refusé près de `DateTimeOffset.MaxValue`/`MinValue` n'énonce pas la limite qu'il implique (faible).**
Suivi dans [#315](https://github.com/Reefact/just-dummies/issues/315).

<a id="core-d6"></a>
**CORE-D6 — Le refus de cardinalité de `SetOf` ne nomme aucun nombre d'éléments (faible).**
Suivi dans [#316](https://github.com/Reefact/just-dummies/issues/316).

<a id="core-d7"></a>
**CORE-D7 — Les tirages `DateTime` sont toujours `Kind = Utc` et se comparent en ticks : décidé dans la documentation XML, absent de la page utilisateur (moyenne, documentation).**
Suivi dans [#317](https://github.com/Reefact/just-dummies/issues/317).

<a id="core-d8"></a>
**CORE-D8 — La page utilisateur de `WithScale` dit qu'il « fixes the number of decimal places » ; l'ADR-0015 décide une grille de valeurs (moyenne, documentation).**
Suivi dans [#318](https://github.com/Reefact/just-dummies/issues/318).

<a id="core-d9"></a>
**CORE-D9 — `DateTimeOffset.WithGranularity` aligne l'instant UTC sur la grille, pas l'horloge locale ; la documentation utilisateur ne le dit jamais (moyenne, documentation).**
Suivi dans [#319](https://github.com/Reefact/just-dummies/issues/319).

<a id="core-d10"></a>
**CORE-D10 — Le refus de cardinalité distincte a migré vers `Generate()` sans trace écrite ; l'ADR-0004, JD016 et la page des collections promettent toujours un refus à la déclaration (moyenne, documentation + ADR).**
Suivi dans [#320](https://github.com/Reefact/just-dummies/issues/320).

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
**Corrigé dans la milestone 10** (suivi dans [#321](https://github.com/Reefact/just-dummies/issues/321)) : chaque valeur d'enum non déclarée dans `OneOf(...)` est signalée une seule fois, par JD017 seul.

<a id="an-d2"></a>
**AN-D2 — JD023 ne nomme que la contrainte qui vide le domaine, sans ses arguments (faible).**
**Corrigé dans la milestone 10** (suivi dans [#322](https://github.com/Reefact/just-dummies/issues/322)) : JD023 nomme la contrainte précédente avec ses arguments : « cannot apply GreaterThan(10) because Between(1, 6) already requires values less than or equal to 6 ».

<a id="an-d3"></a>
**AN-D3 — JD014 et JD023 neutralisent des formes de test négatif différentes (faible).**
**Corrigé dans la milestone 10** (suivi dans [#323](https://github.com/Reefact/just-dummies/issues/323)) : dans `Assert.ThrowsAny(() => chain.Generate())`, JD014 et JD023 sont désormais d'accord. Depuis [#347](https://github.com/Reefact/just-dummies/pull/347) (ADR-0112), aucune règle ne se tait plus sur un test négatif : tous deux le signalent (lignes 48 à 51 de la reproduction), et un test négatif volontaire porte un `[SuppressMessage]` justifié. [#346](https://github.com/Reefact/just-dummies/pull/346) avait d'abord corrigé une régression de #345, où `Select(_ => chain.Generate())` ne déclenchait plus rien, et rendu les nombres de JD023 indépendants de la culture.

<a id="an-d4"></a>
**AN-D4 — La page JD014 prétend refléter les gardes « exactly », et un test range un argument refusé parmi les « legal » (faible).**
Suivi dans [#324](https://github.com/Reefact/just-dummies/issues/324).

<a id="an-d5"></a>
**AN-D5 — Les règles Info (JD020, JD022, JD024, JD029, JD030, JD031, JD033) n'apparaissent jamais dans la sortie de `dotnet build`, et aucune page utilisateur ne le dit (moyenne, documentation).**
Suivi dans [#325](https://github.com/Reefact/just-dummies/issues/325).

### Faux négatifs au regard de la portée documentée d'une règle

<a id="an-f1"></a>
**AN-F1 — JD016 ignore un ensemble `OneOf` numérique typé (faible).**
**Corrigé dans la milestone 10** (suivi dans [#326](https://github.com/Reefact/just-dummies/issues/326)) : les pools typés (`Any.Double().OneOf(1.0, 2.0)`, `Any.Int32().OneOf(1, 2)`) sont comptés, donc `WithCount(3).Distinct()` dessus lève JD016.

<a id="an-f2"></a>
**AN-F2 — JD014 manque les bornes constantes non finies, et `Half` dans `OneOf` (faible).**
**Corrigé dans la milestone 10** (suivi dans [#327](https://github.com/Reefact/just-dummies/issues/327)) : JD014 signale les bornes NaN et infinies (`Between`, `LessThan`, `LessThanOrEqualTo`) et un `Half` non fini dans `OneOf(...)`.

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
Suivi dans [#328](https://github.com/Reefact/just-dummies/issues/328).

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
| <a id="doc-1"></a>DOC-1 | `generators/enums-and-choices:157`, `guides/errors-and-conflicts:63` | `Any.Boolean().Except(true, false)` n'existe pas dans l'API publique | `core` | [#329](https://github.com/Reefact/just-dummies/issues/329) |
| <a id="doc-2"></a>DOC-2 | `generators/strings:56`, `analyzers/JD030:24` | « `NonEmpty()` leaves the ceiling where it was » ; il donne 1 à 1025 (la propre phrase de JD030 le dit) | `core` | [#329](https://github.com/Reefact/just-dummies/issues/329) |
| <a id="doc-3"></a>DOC-3 | `generators/collections:50`, `guides/design-principles:72` | Citent l'ADR-0029 pour la limite du million ; remplacée par l'ADR-0076 | `core` | [#329](https://github.com/Reefact/just-dummies/issues/329) |
| <a id="doc-4"></a>DOC-4 | `generators/strings` (tableaux regex) | `\xNN`/`\uNNNN` fonctionnent mais ne sont pas listés ; `\b`, `\B`, `\G`, `\p{…}`, `\k<…>` et la soustraction de classes sont refusés mais pas listés ; `IgnorePatternWhitespace` lève `ArgumentException`, sans mention ; la limite de 65 536 caractères est absente ; chaque refus se termine par la même liste figée | `core` | [#329](https://github.com/Reefact/just-dummies/issues/329) |
| <a id="doc-5"></a>DOC-5 | `packages/justdummies-cli:254` | « A lambda never widens the base » ; `_ => Any.Int32()` la remplace | `cli` | [#330](https://github.com/Reefact/just-dummies/issues/330) |
| <a id="doc-6"></a>DOC-6 | `guides/errors-and-conflicts:101-108` | `Seed` documenté « inside a reproducible scope » seulement ; il est aussi renseigné en dehors | `core` | [#308](https://github.com/Reefact/just-dummies/issues/308) (avec [CORE-B1](#core-b1)) |
| <a id="doc-7"></a>DOC-7 | `packages/justdummies-cli` | Muette sur le fait que les gardes par regex ne sont jamais lues (le §5.3 de la spécification le décide) | `cli` | [#330](https://github.com/Reefact/just-dummies/issues/330) |
| <a id="doc-8"></a>DOC-8 | `packages/justdummies-cli` | Aucune section sur l'endroit où placer un invariant entre paramètres | `cli` | [#330](https://github.com/Reefact/just-dummies/issues/330) |
| <a id="doc-9"></a>DOC-9 | `packages/justdummies-cli:243-247` (affinage) | Ne dit pas qu'une contrainte déclarable une seule fois (`OneOf`, familles, `WithLength`, `MultipleOf`, `WithScale`) ne peut pas être redéclarée dans la lambda ; que `Except`/`DifferentFrom` restreignent un ensemble ; que JD034 ne voit pas dans la lambda | `cli` | [#330](https://github.com/Reefact/just-dummies/issues/330) |
| <a id="doc-10"></a>DOC-10 | `packages/justdummies-cli` (affinage) | Pour réutiliser un affinage préparé, conserver un `Func<AnyX, AnyX>`, pas un `IAny<T>` construit (ADR-0109 ; CS0266 ne l'enseigne pas) | `cli` | [#330](https://github.com/Reefact/just-dummies/issues/330) |
| <a id="doc-11"></a>DOC-11 | `packages/justdummies-cli` | Une ligne de chaîne scaffoldée déclenche JD030 (Info) par conception | `cli` | [#330](https://github.com/Reefact/just-dummies/issues/330) |
| <a id="doc-12"></a>DOC-12 | `guides/errors-and-conflicts` | Un appel unique aux bornes inversées (`Between(10, 1)`) est une erreur d'argument, pas un conflit | `core` | [#329](https://github.com/Reefact/just-dummies/issues/329) |
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
mesure. Les nombres de tests proviennent d'une exécution mesurée, refaite après la fusion de #331 : les
tests `Bug_` du `cli` et les générateurs d'enum scaffoldés que ce correctif a rendus obsolètes ont été
retirés (voir la section « After #331 » du NOTES.md de chaque domaine).

| Domaine | Modèle | Tests | Aisance | Commentaire | Notes |
|---|---|---|---|---|---|
| 01 payments | Virement SEPA : IBAN mod-97, BIC, Money, date en jour ouvré, lot pain.001 | 55 / 55 | 4 | Côté tests très fluide ; les chiffres de contrôle et les règles entre champs ont demandé le travail manuel | [NOTES](domains/01-payments/NOTES.md) |
| 02 trading | Carnet d'ordres : ticker, prix limite/stop, pas de cotation, séance | 59 / 59 | 4 | Les littéraux et les affinages par lambda disent ce qu'ils veulent dire ; la grille de pas et les liens entre prix demandent des retouches manuelles | [NOTES](domains/02-trading/NOTES.md) |
| 03 insurance | Police auto : période de couverture, franchise, bonus-malus, sinistres | 48 / 48 | 4 | Affinages imbriqués agréables ; les deux règles entre paramètres corrigées à la main | [NOTES](domains/03-insurance/NOTES.md) |
| 04 manufacturing | Ordre de fabrication MES : code de lot, cadence, tolérances, états machine | 65 / 65 | 4 | Rejeu trivial, messages excellents ; une recette silencieuse et le scaffolding d'énumération ont coûté de la confiance | [NOTES](domains/04-manufacturing/NOTES.md) |
| 05 CAD drawing | Ajustements ISO 286, références de plans, nomenclature récursive | 57 / 57 | 3 | Bon DSL scalaire et de motifs ; le scaffolding récursif a fait planter l'hôte | [NOTES](domains/05-cad-drawing/NOTES.md) |
| 06 warehouse | Réception de palettes : SSCC, EAN-13, emplacement, quantités | 63 / 63 | 4 | Le DSL de base se lit comme le domaine ; deux manques silencieux de la CLI | [NOTES](domains/06-warehouse/NOTES.md) |
| 07 freight | Expédition : poids, volume, ISO 6346, ETA entre fuseaux | 62 / 62 | 4 | Fluide ; 3 générateurs sur 7 restructurés ; une garde silencieuse | [NOTES](domains/07-freight/NOTES.md) |
| 08 patient record | Prescription : NIR, molécule, posologie, voie, date de naissance | 55 / 55 | 4 | Messages et rejeu excellents ; somme de contrôle et règles entre champs réécrites | [NOTES](domains/08-patient-record/NOTES.md) |
| 09 laboratory | Résultats de laboratoire, plages de référence, unités, heures de prélèvement | 67 / 67 | 4 | Contraintes fluides ; invariants entre champs dans un fichier que `--force` écrase | [NOTES](domains/09-laboratory/NOTES.md) |
| 10 pharma serialisation | Boîtes : GTIN, lot, péremption, unicité des numéros de série | 57 / 57 | 4 | Chaque nom deviné existait ; le chiffre de contrôle GTIN a imposé des retouches de signature | [NOTES](domains/10-pharma-serialisation/NOTES.md) |
| 11 telecom | Ligne mobile : MSISDN, IMEI (Luhn), ICCID, forfait, consommation | 49 / 49 | 4 | Les blocs Arrange montrent le choisi face à l'arbitraire ; résoudre « to verify » réécrit plus que ce qui est documenté | [NOTES](domains/11-telecom/NOTES.md) |
| 12 smart grid | Index de compteur, point de livraison, courbe de charge à la demi-heure | 39 / 39 | 3 | Affinages imbriqués exacts ; séries ordonnées et règles entre champs ont réécrit 4 fichiers sur 8 | [NOTES](domains/12-smart-grid/NOTES.md) |
| 13 payroll | Bulletin de paie : contrat, heures, primes, retenues | 44 / 44 | 4 | Se lit comme le domaine ; la confusion entre valeur fixée et lambda a causé une exécution rouge | [NOTES](domains/13-payroll/NOTES.md) |
| 14 e-commerce | Panier et commande : SKU, quantité, remise, adresse | 47 / 47 | 4 | Naturel ; un manque silencieux ; les règles entre éléments ont perdu des cas | [NOTES](domains/14-ecommerce/NOTES.md) |
| 15 hotel booking | Séjour, chambre, clients, conditions d'annulation | 48 / 48 | 4 | Fluide ; l'intervalle du séjour a exigé des générateurs restructurés à la main | [NOTES](domains/15-hotel-booking/NOTES.md) |
| 16 aviation | Vol : codes IATA/OACI, immatriculation, créneau | 66 / 66 | 4 | Le vocabulaire temporel convient aux créneaux ; une recette silencieuse ; gardes temporelles non lues | [NOTES](domains/16-aviation/NOTES.md) |
| 17 real estate | Cadastre, diagnostic énergétique, annonces | 50 / 50 | 3 | Les objets valeur se lisent bien ; chaque agrégat avait une règle entre champs | [NOTES](domains/17-real-estate/NOTES.md) |
| 18 education | Inscription, notes au demi-point, semestres | 51 / 51 | 4 | Contraintes et graines fluides ; la note au demi-point exige un ensemble de 41 valeurs | [NOTES](domains/18-education/NOTES.md) |
| 19 IAM | Jeton d'accès, rôles, scopes, expiration, URI de redirection | 61 / 61 | 4 | `Any.Uri()` est excellent ; un récapitulatif « guard » silencieux et le scaffolding d'énumération | [NOTES](domains/19-iam/NOTES.md) |
| 20 agritech IoT | Capteurs de terrain : GPS, humidité, batterie en `Half`, lots de 500k | 56 / 58 (2 `Bug_`) | 4 | Le vocabulaire numérique colle à la géodésie ; un grand plafond rend le dummy par défaut énorme | [NOTES](domains/20-agritech-iot/NOTES.md) |
| **Total** | | **1099 / 1101** (2 rouges, les deux tests `Bug_` de [#310](https://github.com/Reefact/just-dummies/issues/310)) | | | |

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

1. **Issues.** Ouvertes avec l'accord du mainteneur, une par cause racine ; les sections ci-dessus y
   renvoient.
   * `cli` : [#297](https://github.com/Reefact/just-dummies/issues/297)–[#307](https://github.com/Reefact/just-dummies/issues/307), plus [#296](https://github.com/Reefact/just-dummies/issues/296) (CLI-B7, ouverte pendant le pilote).
   * `core` : [#308](https://github.com/Reefact/just-dummies/issues/308)–[#320](https://github.com/Reefact/just-dummies/issues/320).
   * `analyzers` : [#321](https://github.com/Reefact/just-dummies/issues/321)–[#327](https://github.com/Reefact/just-dummies/issues/327).
   * `xunit` : [#328](https://github.com/Reefact/just-dummies/issues/328).
   * Documentation : [#329](https://github.com/Reefact/just-dummies/issues/329) (pages des générateurs et des erreurs : DOC-1–4, DOC-12) et [#330](https://github.com/Reefact/just-dummies/issues/330)
     (page du CLI : DOC-5, DOC-7–11) ; DOC-6 suit [#308](https://github.com/Reefact/just-dummies/issues/308).
   * Non ouvertes, parce que chacune attend une décision : les candidats du §8, et DOC-13 (dépend
     d'AN-C3). Elles vont au mainteneur comme demandes de décision, pas comme bogues.
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
