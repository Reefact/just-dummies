# `dum` — première mesure de terrain

🌍 **Langues :**  
🇫🇷 Français (ce fichier) | 🇬🇧 [English](./2026-09-02-dum-first-field-measurement.md)

**Date :** 2026-09-02
**Révision mesurée :** `f179fa0` (tête de `main`), contre `1c71f74` (son parent) pour la colonne « avant »
**Portée :** le scaffolder `dum` passé sur sept dépôts qui ne sont pas celui-ci.
**Statut :** consultatif, et **instantané** — les chiffres décrivent un jour et un conteneur, pas une règle.

**Pourquoi elle existe.** La spécification disait, jusqu'à cette mesure, qu'*aucun projet extérieur à
ce dépôt n'a encore exercé l'outil*. Deux bancs le tenaient — le corpus nommé et le balayage
génératif — et tous deux vivent dans une seule compilation. C'est la première fois que `dum` est
pointé sur du code que personne n'a écrit pour lui, et il a trouvé dès le premier passage un défaut
qu'aucun des deux bancs ne pouvait atteindre (§« Ce qu'elle a trouvé », 1). Consigner les chiffres
est ce qui permettra à une mesure ultérieure de dire si quelque chose a bougé.

---

## 1. Méthode

Un projet-sonde par dépôt, dans la forme que décrit §3.1 : un projet de test référençant à la fois la
bibliothèque examinée et JustDummies. Rien n'a été modifié dans les dépôts cibles ; les sondes vivent
hors de celui-ci et ne sont pas versionnées.

```
dum generate <every public type> --project <probe>.csproj --dry-run --format json
```

L'inventaire des types publics vient d'un `grep` sur les déclarations de tête de ligne dans les
sources de la bibliothèque, pas de la réflexion — voir §5 pour ce que cela rate. Chaque dépôt a été
mesuré **deux fois**, avec l'outil construit depuis `1c71f74` puis depuis `f179fa0`, de sorte que les
colonnes « avant » et « après » ne diffèrent que d'un commit.

## 2. Le corpus

| dépôt | révision | types | scaffoldés | paramètres | avant `guard`/`unread` | après `guard`/`unread` | `no source` |
|---|---|---:|---:|---:|---:|---:|---:|
| `Reefact/first-class-errors` | `99dc5da` | 31 | 20 | 28 | 0 / 0 | **7 / 11** | 0 |
| `Reefact/luxafor-lighting-device-controller` | `160cf86` | 8 | 4 | 7 | 0 / 0 | 0 / 1 | 0 |
| `tpierrain/Diverse` | `73c98b6` | 11 | 8 | 22 | 0 / 0 | 0 / 0 | 0 |
| `tpierrain/NFluent` | `c6e2aac` | 23 | 8 | 8 | 0 / 0 | 0 / 1 | 0 |
| `nodatime/nodatime` | `67f7885` | 58 | 26 | 73 | 0 / 0 | **0 / 24** | 0 |
| `stryker-mutator/stryker-net` | `4fa9ee7` | 122 | 116 | 154 | 0 / 0 | 0 / 11 | 31 |
| `NuGet/NuGet.Client` (`NuGet.Versioning`) | `e6aaa9a` | 10 | 9 | 22 | 0 / 2 | 0 / 2 | 0 |
| **total** | | **263** | **191** | **314** | **0 / 2** | **7 / 50** | **31** |

**191 types sur 263 scaffoldés — 72,6 %.** Les 72 refus : 55 `NoEligibleConstructor`,
8 `TypeAmbiguous`, 8 `TypeIsGeneric`, 1 `TypeNotFound`. Aucun refus du corpus ne s'est trompé sur le
fait *de* refuser ; §« Ce qu'elle a trouvé », 3 porte sur l'un d'eux qui se trompe sur le *pourquoi*
— et le changement qui porte cette page redistribue ces 72 en 43 `NoEligibleConstructor`,
15 `TypeIsGeneric`, 8 `TypeAmbiguous`, 5 `TypeIsAbstract`, 1 `TypeNotFound`, sans faire entrer ni
sortir un seul type du refus.

## 3. Ce que valait le correctif de `f179fa0`

**Cinquante-cinq paramètres sur 314 — 18 % — sont passés du silence à une contrainte ou à une
marque.** Avant ce commit, un type atteint par une référence de projet perdait ses gardes sans un mot
dès que les deux projets ne liaient pas les mêmes références — c'est-à-dire dès qu'une bibliothèque
en `netstandard2.0` vit sous un projet de test en `net8.0`.

Les deux extrémités de l'écart disent ce que vaut le commit :

* **`first-class-errors`** : 18 paramètres sur 28 ont gagné quelque chose — 7 vraies contraintes
  (`Dummy.String().NotBlank()` là où la ligne de base était tirée), 11 `unread guards` honnêtes.
* **`nodatime`** : 24 paramètres sur 73 ont gagné un `unread guards`. Avant, `dum` aurait remis à un
  utilisateur de NodaTime 26 générateurs dont le récapitulatif annonçait *tout inféré*, au-dessus
  d'invariants qu'il n'avait jamais lus — et `LocalTime`, `LocalDate` et `AnnualDate` rejettent la
  plupart de ce qu'ils auraient tiré.

`Diverse` n'a rien gagné, et c'est le témoin dont le tableau avait besoin : ses types-valeurs ne
déclarent aucune garde de constructeur, il n'y avait donc rien à récupérer. Ses `throw` sont tous
dans des méthodes.

## 4. Ce qu'elle a trouvé

### 1 — Des gardes perdues en silence à travers une référence de projet

Rapporté et corrigé dans `f179fa0` ; la ligne ci-dessus en est la mesure. C'est consigné ici pour
**la façon** dont ça a été trouvé : toutes les formes que tire le balayage génératif vivent dans une
seule compilation, donc aucune quantité d'entre elles ne pouvait l'atteindre. Un banc qui fabrique
ses propres entrées ne peut pas tester l'hypothèse sous laquelle il les fabrique.

### 2 — Un rapport de terrain sur §5.3, au sens propre d'ADR-0085

Les deux idiomes de validation dominants des constructeurs de NodaTime sont hors de ce que le lecteur
de gardes couvre, et tous deux sont correctement marqués plutôt que mal lus :

```csharp
// LocalTime — one || chain spanning four parameters. §9 names a cross-parameter rule as out of reach.
if (hour < 0 || hour > HoursPerDay - 1 ||
    minute < 0 || minute > MinutesPerHour - 1 || ...)
{
    Preconditions.CheckArgumentRange(nameof(hour), hour, 0, HoursPerDay - 1);
    ...
}

// AnnualDate — validation delegated to a helper internal to the project, not to a named guard
// library (ADR-0086).
GregorianYearMonthDayCalculator.ValidateGregorianYearMonthDay(2000, month, day);
```

C'est un rapport venu de l'extérieur de la boucle, exactement ce que
l'[ADR-0085](../adr/0085-change-the-guard-reader-only-against-a-field-report.fr.md) réclame avant que
§5.3 ne bouge. **Et son propre remède, pris dans l'ordre, est le premier : aucun changement.** La
marque `unread guards` répond déjà aux deux, le développeur rencontre une ligne qu'il supprime une
fois, et rien n'est tiré au-dessus d'un invariant que personne n'a honoré. Consigné pour que la
prochaine proposition d'élargir la table parte d'un constructeur réel plutôt que d'un argument.

### 3 — `NoEligibleConstructor` masquant un refus plus précis

`Scaffolder.Scaffold` décide `NoEligibleConstructor` **avant** de demander si le type est générique
ou abstrait. Un type abstrait dont les constructeurs sont `protected` — la forme ordinaire d'un type
abstrait — s'entend donc dire *« `Generate()` a besoin d'un constructeur d'instance public »*, quand
la réponse qu'il devrait recevoir est *« scaffolde un type concret qui en dérive »*.
`ScaffoldStatus.TypeIsAbstract` existe pour le dire et est, en pratique, presque inatteignable : il
n'est interrogé qu'une fois qu'un constructeur public a déjà été trouvé.

**Douze des 55 refus `NoEligibleConstructor` du corpus — 5 abstraits, 7 génériques — masquent ainsi
une raison plus actionnable.** Nommés par l'outil une fois l'ordre corrigé, ce qui est un compte plus
sûr que le `grep` qui l'avait d'abord suggéré : `DiagnosableException` et le `VersionRangeBase` de
`NuGet.Versioning` sont abstraits ; `PublicMessageStage<TError>` et le `MutatorBase<T>` de Stryker
sont génériques — et le second, le `grep` l'avait rangé dans la mauvaise colonne.

La phrase du refus a une seconde lacune, indépendante de l'ordre : elle ne nomme que la voie du
constructeur, jamais la fabrique statique reconnue de §5.1.2. L'auteur d'un value object validant —
un constructeur privé derrière un `Create` public, le public même pour qui cette règle a été écrite —
s'entend conseiller l'inverse de ce que son type veut être.

Ni l'un ni l'autre n'est un changement de contrat : §7 donne à chacun de ces refus le code de sortie
`1`, donc rien de ce qu'un script lit ne bouge.

### 4 — Un renvoi de la spécification qui ne résout plus

§5.1.2 envoie le lecteur vers « une fabrique statique reconnue (§5.4) », et §5.4 a cessé de définir
cela à l'[ADR-0089](../adr/0089-draw-a-composed-parameter-through-the-generator-its-type-owns.fr.md)
— elle ne décrit plus que la façon dont un générateur scaffoldé l'emporte. La règle ne vit plus que
dans le code, et deux des remarques du code portent le même renvoi mort vers §5.4.

## 5. Ce que cette mesure n'établit pas

* **Trois dépôts sont partiellement dégradés.** `luxafor` (`net462`), `NFluent` (`net35`, `net462`) et
  `NuGet.Versioning` (`net472`) visent des versions de .NET Framework dont les assemblies de
  référence ne sont pas installées dans ce conteneur Linux ; MSBuild a signalé l'échec et `dum` l'a
  imprimé. **La ligne `NuGet.Versioning` n'est comptée dans aucun sens** : ses chiffres avant et
  après sont identiques, dans un état qui n'est pas représentatif, et rien n'en est conclu.
* **L'inventaire des types est un `grep`, pas de la réflexion.** Un type public déclaré ailleurs
  qu'en tête de ligne est manqué : 263 est un plancher, pas un recensement.
* **Rien ici n'est un score de mutation.** Rien dans ce dépôt n'en impose (ADR-0025), et cette page
  n'en revendique aucun.
* **Un type scaffoldé n'est pas un type vérifié.** « Scaffoldé » veut dire qu'un fichier a été
  produit ; savoir si son générateur tire des valeurs que le domaine accepte est ce à quoi répondent
  le corpus nommé et l'oracle de tirage graine, et ils n'ont pas été passés sur ces dépôts.

## 6. Quoi en faire

Les deux trouvailles sur lesquelles cette page peut agir — 3 et 4 — sont corrigées dans le changement
qui la porte. La trouvaille 2 est délibérément laissée en l'état, sous le premier remède d'ADR-0085
lui-même. La trouvaille 1 est close.

Une mesure ultérieure devrait rejouer §1 mot pour mot sur les sept mêmes révisions avant d'en ajouter
d'autres : un chiffre qui bouge n'est instructif que si le corpus, lui, n'a pas bougé.
