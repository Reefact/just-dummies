# ADR-0105 | Accepter l'exclusion d'une valeur non déclarée sur un enum comme une opération sans effet

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0105-accept-an-undeclared-enum-exclusion-as-a-no-op.md)

**Statut :** Accepted
**Proposé :** 2026-10-11
**Accepté :** 2026-10-14
**Décideurs :** Reefact

Restreint l'[ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.fr.md) sur un seul point : la règle
applicable aux arguments des exclusions d'enum, comme
l'[ADR-0101](0101-accept-a-non-finite-exclusion-as-a-no-op.fr.md) l'a fait pour les exclusions flottantes. Tout le
reste de ce qu'a décidé l'ADR-0054 reste en vigueur.

## Contexte

L'[ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.fr.md) a décidé qu'*« un générateur typé ne tire,
et n'accepte en argument, que des valeurs valides du domaine qu'il représente »*. `Any.Enum<T>()` est un tel
générateur : il tire un membre que son type déclare ou, sur un enum `[Flags]`, une combinaison de membres déclarés
là où l'appelant en demande une ([ADR-0020](0020-draw-flag-enum-combinations-behind-an-opt-in.fr.md)).

Le générateur applique cette règle à `OneOf` et à rien d'autre. `OneOf` refuse une valeur que le type ne définit
pas par une `ArgumentException`. `Except` et `DifferentFrom` prennent la valeur telle quelle : une valeur non
déclarée ne peut correspondre à aucun candidat que le générateur tirerait, donc les valeurs produites sont les
mêmes, qu'elle soit portée ou non. L'exécution accepte une telle exclusion comme une opération sans effet.

`JD017` jugeait les trois contraintes de la même façon. Il signalait `Any.Enum<Day>().Except((Day)99)`, et
`Any.Enum<Status>().DifferentFrom(default)` sur un enum sans membre zéro, comme des valeurs hors de l'univers — un
avertissement, une erreur sous une compilation qui traite les avertissements comme des erreurs. L'analyseur
refusait à la compilation une chaîne que l'exécution prend en charge quelle que soit la graine aléatoire
([#228](https://github.com/Reefact/just-dummies/issues/228)). La suite qui garde la règle et le générateur
d'accord n'interrogeait le générateur que par `OneOf` : rien ne comparait les deux sur une exclusion.

Une valeur d'enum non déclarée s'obtient par une conversion numérique ou par `default`, le zéro que tout enum
possède, qu'il le déclare ou non. Un nom de membre mal orthographié ne compile pas. `DifferentFrom` est documenté
comme prenant *« typiquement une valeur existante, déjà présente dans le test »*, et une valeur d'enum déjà
présente dans un test peut être `default`.

L'[ADR-0101](0101-accept-a-non-finite-exclusion-as-a-no-op.fr.md) a tranché la même question pour les générateurs
flottants. Il a gardé refusées une borne et une valeur de `OneOf` non finies, et accepté une valeur non finie dans
`Except` et `DifferentFrom` comme une opération sans effet, au motif qu'exclure une valeur que le générateur ne
tire jamais énonce une propriété déjà garantie. La bibliothèque traite partout ailleurs une propriété déjà
garantie comme une opération sans effet.

La bibliothèque est en deçà de la 1.0, et un changement de comportement y est permis
([`CONTRIBUTING.md`](../../for-users/CONTRIBUTING.fr.md), « Base de référence de l'API publique »).

## Décision

`Any.Enum<T>()` accepte une valeur que son type ne définit pas dans `Except` et `DifferentFrom` comme une
opération sans effet, et `JD017` ne la signale pas, tandis qu'une valeur non déclarée dans `OneOf` reste refusée
comme l'a décidé l'ADR-0054.

## Justification

**Exclure une valeur qui n'est jamais tirée énonce une propriété déjà garantie, et la règle de la bibliothèque pour
une propriété déjà garantie est l'absence d'effet.** L'ADR-0101 a tracé la frontière en une phrase : un argument qui
rendrait la contrainte vide de sens est refusé, un argument qui énonce une propriété déjà garantie est accepté. Une
valeur d'enum non déclarée dans `OneOf` serait tirée, donc elle est refusée ; la même valeur dans `Except` ne retire
rien, donc elle est acceptée. Le générateur d'enum se lit désormais exactement comme les générateurs flottants, et
le lecteur apprend une règle au lieu de deux.

**L'analyseur ne doit pas refuser ce que la bibliothèque accepte.** Une règle de compilation se justifie en
attrapant ce qui échouerait plus tard. Ici, rien n'échoue plus tard : la chaîne tire un membre déclaré quelle que
soit la graine aléatoire et se rejoue depuis sa graine. Un avertissement sur elle est un avertissement inutile et,
sous une compilation qui traite les avertissements comme des erreurs, il arrête une compilation correcte. Les deux
côtés étaient en désaccord, et c'est l'exécution qui suit la règle de la bibliothèque pour les propriétés déjà
garanties.

**La valeur s'écrit délibérément, donc le silence ne perd aucune faute de frappe.** Ce qui justifie d'ordinaire de
signaler un argument sans effet, c'est l'erreur d'écriture. Un nom de membre mal orthographié ne compile jamais :
une valeur d'enum non déclarée s'écrit donc par une conversion ou par `default`, une écriture délibérée.
`DifferentFrom(default)` sur une valeur déjà présente dans le test est un cas d'utilisation courant, et c'est celui
que cette règle cassait.

**Refuser plutôt à l'exécution casserait le même cas d'utilisation, plus bruyamment.** Faire lever `Except` sur une
valeur non déclarée alignerait l'exécution sur l'ancienne règle, mais transformerait `DifferentFrom(default)` en
exception dès la préparation du test et rouvrirait, pour les enums, l'asymétrie que l'ADR-0101 a refermée pour
les valeurs flottantes. Le *« refuser bruyamment à la frontière »* de l'ADR-0046 vaut pour une demande que la
bibliothèque ne sait pas prendre en charge ; celle-ci, elle la prend déjà en charge.

## Alternatives considérées

### Refuser l'exclusion non déclarée à l'exécution

La seconde voie de l'issue : `Except` et `DifferentFrom` valident leur argument contre l'univers déclaré, comme
`OneOf`, et `JD017` continue de signaler. Envisagée parce qu'elle garde la phrase de l'ADR-0054 à la lettre et ne
demande aucune modification de l'analyseur. Écartée parce qu'elle fait d'une propriété déjà garantie une erreur, ce
que l'ADR-0101 a justement décidé que la bibliothèque ne fait pas, et parce que `DifferentFrom(default)` sur un
enum sans membre zéro lèverait là où il fonctionne aujourd'hui.

### Continuer à la signaler, au niveau information

Signaler une exclusion non déclarée comme un fait à peser plutôt que comme un avertissement, à la manière de
`JD024` pour une sentinelle entière que le domaine ne produit jamais. Envisagée parce qu'elle garde un signal pour
l'appelant qui s'est trompé de nombre en convertissant. Écartée par le mainteneur au profit du silence : la valeur
s'écrit toujours délibérément, par une conversion ou par `default`, et un diagnostic d'information sur
`DifferentFrom(default)` se déclencherait précisément sur le cas d'utilisation que cette décision rend légal.

### Remplacer l'ADR-0054 en entier

Écartée pour la raison qu'a donnée l'ADR-0101 : presque tout tient — la règle côté tirage, le verdict sur
`OneOf`, l'exemption générique — et un successeur qui le reformulerait en entier pour changer une clause
enfouirait le changement.

## Conséquences

### Positives

* `JD017` et l'exécution s'accordent sur chaque exclusion, et la suite d'accord interroge désormais le générateur
  par `Except` et `DifferentFrom` autant que par `OneOf` : les deux ne peuvent plus diverger sans qu'on le voie.
* `DifferentFrom(default)` compile proprement sous une compilation qui traite les avertissements comme des
  erreurs, que l'enum déclare un zéro ou non.
* Les générateurs d'enum et flottants suivent une seule règle d'argument.

### Négatives

* L'appelant qui voulait exclure une valeur déclarée et s'est trompé de nombre en convertissant n'a aucun signal
  à la compilation. L'exclusion est sans effet : l'échec réel du test, s'il y en a un, se manifeste là où la valeur
  tirée est utilisée.

### Risques

* L'absence d'effet repose sur un fait du générateur : une valeur non déclarée n'est jamais tirée. Sur un enum
  `[Flags]`, c'est vrai d'une valeur qu'aucun OU de membres déclarés n'atteint ; une combinaison atteignable est
  tirée dès qu'`AllowingCombinations()` est déclaré, et `JD017` s'abstient déjà sur cette chaîne. Un changement
  futur de ce que tire le générateur devrait conserver cette décision explicitement.

## Actions de suivi

* Les pages de `JD017`, en anglais et en français, énoncent la règle restreinte dans le même changement qui
  l'introduit.
* Que le statut de l'ADR-0054 change à côté de sa seconde restriction relève du mainteneur au moment de
  l'acceptation ; cette décision ne le modifie pas.

## Références

* [ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.fr.md) — la règle que cette décision restreint.
* [ADR-0101](0101-accept-a-non-finite-exclusion-as-a-no-op.fr.md) — la même restriction pour les générateurs
  flottants, et le principe que cette décision applique.
* [ADR-0020](0020-draw-flag-enum-combinations-behind-an-opt-in.fr.md) — quelles combinaisons tire un enum
  `[Flags]`.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — quand un refus est la réponse
  honnête.
* [Issue #228](https://github.com/Reefact/just-dummies/issues/228) — le désaccord entre `JD017` et l'exécution,
  et la question ouverte que cette décision tranche.
