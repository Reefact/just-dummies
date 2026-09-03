# Notes de version — JustDummies, 1.x

Ce qui a changé pour vous, version par version, sur le train `lib`. Pour le registre technique complet — chaque contrainte, chaque cas limite, chaque ADR — voir [CHANGELOG.md](https://github.com/Reefact/just-dummies/blob/main/JustDummies/CHANGELOG.md). Précédemment : [0.x](https://github.com/Reefact/just-dummies/blob/main/JustDummies/RELEASE_NOTES-0.x.fr.md).

## 1.0.0-preview.6 — 2 septembre 2026

_Un changement de licence que chaque consommateur devrait lire, un générateur qui élargit vers le nullable, et un tirage plus équitable pour `Half`._

### ⚠️ Changements cassants

- **JustDummies est désormais sous licence [PolyForm Internal Use 1.0.0](https://github.com/Reefact/just-dummies/blob/main/LICENSE), et non plus Apache 2.0 — source disponible, pas open source.** Vous pouvez lire, construire, modifier et exécuter la bibliothèque (et les analyseurs qu'elle embarque) pour vos propres opérations internes ou celles de votre entreprise ; vous ne pouvez pas redistribuer le logiciel. Les versions déjà publiées sur NuGet ne sont pas concernées et conservent la licence sous laquelle elles ont été livrées. Les contributions sont désormais régies par un [Contributor Agreement](https://github.com/Reefact/just-dummies/blob/main/CONTRIBUTOR_AGREEMENT.md).

### ✨ Nouveautés

- Nouveau `generator.AsNullable()` — élargit le type d'un générateur vers le nullable sans jamais tirer une valeur absente, l'opposé de `.OrNull()`. Il conserve le nombre de valeurs connu du générateur enveloppé, si bien que `Any.SetOf(Any.Enum<T>().AsNullable())` dimensionne correctement par rapport aux membres de l'énumération ([ADR-0094](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0094-lift-a-nullable-value-type-rather-than-deriving-it.md)).

### 🙌 Améliorations

- **`Any.Half()` tire désormais uniformément parmi les valeurs qu'un half peut réellement représenter**, plutôt que parmi les réels avec arrondi — ce qui ne produisait presque rien en dessous de 1. Un test à seed fixe tirant un `Half` rejouera une valeur différente d'avant ([ADR-0091](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0091-draw-a-half-from-the-values-it-can-represent.md)).

### 🐛 Corrections

- `Any.Half()` indique désormais combien de valeurs distinctes il détient, si bien qu'`Any.SetOf(Any.Half())` au-delà de ce compte est refusé plutôt que d'épuiser un budget de nouveaux tirages.
- `JD016` prouve désormais exactement plusieurs domaines d'éléments supplémentaires (`Char`, `Byte`/`SByte`, `Int16`/`UInt16`, `Half`, et les valeurs distinctes d'une énumération), et compte exactement un ensemble `Any.Char().OneOf(...)` fourni par l'appelant.
- `JD015` pèse désormais un ensemble de valeurs face à toutes les contraintes déclarées ensemble, si bien qu'une chaîne refusée seulement par l'intersection de plusieurs contraintes est signalée.
- Un générateur d'élément qui n'admet plus rien nomme désormais sa propre contrainte responsable, même à l'intérieur d'une collection distincte comme `Any.SetOf(...)`.

## 1.0.0-preview.5 — 25 août 2026

_Une garde contre les chaînes vierges, et quatre corrections d'ordre de chaîne pour qu'une spécification se lise à l'identique quel que soit l'ordre dans lequel elle a été écrite._

### ✨ Nouveautés

- Nouveau `NotBlank()` sur `Any.String()` — exige au moins un caractère non blanc, la garde que `NonEmpty()` seul ne couvrait jamais puisqu'une chaîne entièrement blanche n'est pas vide ([ADR-0088](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0088-state-the-whitespace-guard-with-a-member-of-its-own.md)).

### 🐛 Corrections

- `JD015` mesure désormais le budget de longueur d'une chaîne comme le générateur la construit réellement, si bien qu'un préfixe redéclaré ou une position de remplissage de `NotBlank()` n'est plus compté deux fois ni oublié.
- Un ensemble `Any.Enum<T>()` vidé par `Except(...)` honore désormais `AllowingCombinations()` quel que soit l'appel écrit en premier.
- Une combinaison de drapeaux nommée dans `OneOf` est désormais acceptée où qu'elle soit écrite, sans besoin d'`AllowingCombinations()` du tout.
- Une collection distincte (`Any.SetOf(...)` et semblables) est désormais jugée sur la chaîne entière et finie, si bien que `Containing`, `ContainingAny` et un `Distinct(comparer)` plus fin sont honorés quelle que soit la place de `WithCount` dans la chaîne.
- `JD030` compte désormais chaque fragment ancré (`StartingWith`, `EndingWith`, `Containing`) lorsqu'il signale l'intervalle qu'une chaîne tire.

## 1.0.0-preview.4 — 24 août 2026

_Un renommage qui se lit mieux, et un plafond de taille sur lequel l'analyseur et la bibliothèque s'accordent enfin._

### ⚠️ Changements cassants

- `AnyChar` et `AnyString` renomment `LowerCase()`/`UpperCase()` en `InLowerCase()`/`InUpperCase()` — les noms nus se lisaient comme un changement d'état plutôt qu'une qualité de la valeur tirée. Aucun changement de comportement ; seuls les deux noms changent.

### 🐛 Corrections

- JD014 signale désormais un plafond de taille au-dessus de la limite production : `WithMaxLength` et `WithMaxCount` étaient déclarés sans plafond alors que la bibliothèque les plafonne, si bien qu'un appel béni par l'analyseur pouvait être refusé à l'exécution sans rien pour le dire entre les deux.

## 1.0.0-preview.3 — 21 août 2026

_Un format à préfixe fixe et alphabet restreint — `ORD-` suivi d'alphanumériques — est enfin une seule chaîne plutôt qu'un contournement._

### ✨ Nouveautés

- Une famille de caractères, une soustraction ou une casse ne gouverne désormais que les caractères que le générateur tire — jamais un préfixe, un suffixe ou une valeur contenue que vous avez écrits. `Any.String().StartingWith("ORD-").AlphaNumeric()` ne lève plus d'exception à la déclaration, et produit `ORD-` suivi uniquement d'alphanumériques ([ADR-0079](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0079-constrain-what-a-dummy-draws-never-the-literals-it-was-given.md)). Aucune chaîne qui fonctionnait avant ne cesse de fonctionner, et aucune valeur générée ne change de forme.
- Nouvelle règle JD033 — nomme un fragment ancré que les caractères déclarés ne peuvent pas tirer, à l'endroit de l'appel, sans refuser la chaîne. Information uniquement, active par défaut.
- Nouvelle règle JD031 — pointe une chaîne déclarant les deux bornes inclusives séparément (`WithMinLength(8).WithMaxLength(20)`) vers la forme d'intervalle que le même générateur expose (`WithLengthBetween(8, 20)`). Information uniquement.
- Nouvelle règle JD032 — avertit lorsqu'une borne est déclarée deux fois et que l'appel le plus lâche perd silencieusement, quel que soit l'ordre d'écriture des deux.

### 🙌 Améliorations

- JD015 signale désormais, en avertissement, un ensemble de valeurs que chaque contrainte déclarée vide — jusqu'ici cela n'apparaissait que sous forme de deux notes JD029 au niveau information.
- JD024 et JD015 se resserrent pour rester en phase avec les changements ci-dessus : JD024 ne signale plus une borne que JD032 possède désormais, et JD015 ne garde que sa vérification du budget de longueur, si bien qu'aucune des deux ne refuse à la compilation ce que la bibliothèque honore désormais à l'exécution.

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
