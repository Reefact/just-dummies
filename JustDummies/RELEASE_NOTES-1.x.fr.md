# Notes de version — JustDummies, 1.x

Ce qui a changé pour vous, version par version, sur le train `lib`. Pour le registre technique complet — chaque contrainte, chaque cas limite, chaque ADR — voir [CHANGELOG.md](https://github.com/Reefact/just-dummies/blob/main/JustDummies/CHANGELOG.md). Précédemment : [0.x](https://github.com/Reefact/just-dummies/blob/main/JustDummies/RELEASE_NOTES-0.x.fr.md).

## 1.0.0-preview.2 — 18 août 2026

_Deux défauts qui cassent la compatibilité, tous deux au service de la même idée : un dummy non contraint devrait certifier quelque chose, pas seulement paraître inoffensif._

### ⚠️ Changements cassants

- `Any.String()` et `Any.Char()` tirent désormais dans l'ensemble de l'ASCII, caractères de contrôle compris, et une chaîne non contrainte s'étend de 0 à 1024 caractères — restreignez avec `NonEmpty()`, `WithMaxLength(n)` ou `Printable()` ([ADR-0075](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0075-draw-characters-from-the-whole-of-ascii.md)).
- Un maximum déclaré — `WithMaxLength`, `WithLengthBetween`, `WithMaxCount` — pilote désormais le tirage au lieu de se composer avec l'ancien étalement restreint, et un maximum au-delà de 1 000 000 est refusé ([ADR-0076](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0076-let-a-declared-maximum-steer-the-size-draw.md)).

### ✨ Nouveautés

- Cinq nouvelles familles de caractères — `Punctuation()`, `Printable()`, `NonPrintable()`, `Whitespaces()` et `Hexadecimal()` — plus `WithoutAlpha()`/`WithoutNumeric()` pour soustraire plutôt qu'épingler, sur `Any.String()` comme sur `Any.Char()`.
- **`IPoolInspection<T>` révèle ce que vos propres contraintes ont laissé d'un pool que vous avez fourni** — `GetSurvivors()`, `GetRejections()` — sur tout générateur qui accepte un ensemble de valeurs ([ADR-0067](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0067-report-a-filtered-pool-through-an-explicit-interface.md), [ADR-0068](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0068-carry-the-pool-inspection-wherever-a-caller-supplies-the-values.md)).
- JD029 signale, à la compilation, une valeur écrite dans un pool que vos propres contraintes ne peuvent jamais tirer.
- JD030 signale une chaîne `Any.String()` qui ne fixe aucune longueur, en information.

### 🙌 Améliorations

- Le readme embarqué documente désormais comment obtenir un `NaN` volontairement, puisque `Any.Double()`, `Any.Single()` et `Any.Half()` refusent aussi cette valeur en argument.
- JD015 valide désormais aussi les nouvelles familles de caractères, et chaque valeur que la bibliothèque affiche est échappée contre les caractères de contrôle.

### 🐛 Corrections

- Une exclusion decimal déclarée deux fois ne vide plus une grille pourtant satisfiable.
- Un conflit sur `Any.Enum<T>()` ou `Any.Guid()` nomme désormais l'exclusion qui en est la cause.
- Un pool `DateTimeOffset` portant deux horloges pour un même instant atteint désormais un seul verdict, quel que soit l'ordre de déclaration.
- JD023 et JD024 lisent désormais les constantes `UInt16`/`UInt32`/`UInt64` quel que soit le suffixe du littéral.
- JD015, JD023, JD024 et JD029 reconnaissent désormais une chaîne seedée écrite en une seule expression.

## 1.0.0-preview.1 — 7 août 2026

_Pas une surface plus large que la 0.1.0 — la même, offerte pour la première fois à un consommateur extérieur, avec une nouvelle promesse : votre seed._

### ✨ Nouveautés

- **Une seed rejoue désormais à l'identique à travers les versions patch et mineures.** Épinglez-en une dans un test, et elle continue de tirer les mêmes valeurs à chaque montée de version au sein de `1.x` ([ADR-0049](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0049-replay-a-seed-across-patch-and-minor-versions.md)).

### 🙌 Améliorations

- Le package embarque désormais une icône, partagée par tous les packages publiés depuis ce dépôt.
- Les liens du readme embarqué pointent maintenant vers ce dépôt plutôt que celui dont JustDummies a été extrait.
