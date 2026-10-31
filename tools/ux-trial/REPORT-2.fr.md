# Essai d'expérience développeur n° 2 — JustDummies et `any` : rapport

## 1. Synthèse

**Verdict d'ensemble sur la fluidité.** La bibliothèque comme le scaffolder sont assez fluides pour un usage réel.
- Vingt contextes délimités (21 à 40) ont chacun été modélisés une fois et testés par trois profils, soit 60 sessions.
- 58 sessions ont noté la fluidité 4/5. Deux l'ont notée 3/5 : le profil habitué sur 28-satellite-conjunction et le profil découverte sur 33-music-score.
- Toutes les suites sont passées, et toutes sont restées vertes sur des exécutions répétées avec de nouvelles graines (de 5 à 150 exécutions par suite). Dans 32-grid-load-flow, la suite de l'expert a un test volontairement ignoré, parce qu'une valeur tirée a révélé un vrai défaut dans le domaine.

Ce qui a fonctionné :
- Le DSL de base, la composition au travers des générateurs scaffoldés, les paires `With…` d'épingle et d'affinage, et `[Reproducible]` ont fonctionné dans tous les domaines.
- Les dummies tirés ont révélé à plusieurs reprises de vraies dépendances cachées, ce qui est la raison d'être de la bibliothèque. Exemples : un plafond de taux de croissance en 21, un solveur qui diverge pour une accélération ≥ ~1,87 en 32, l'hypothèse d'un seul trayon traitable en 26, et des erreurs d'arithmétique dans plusieurs suites.

Ce qui a ralenti :
- Presque toutes les frictions viennent de la lecture des gardes par le scaffolder et des règles qui lient des paramètres entre eux. Elles ne viennent pas de la surface de la bibliothèque.
- Une garde non lue a bloqué la compilation, comme prévu, à une exception près : une garde écrite au travers d'un helper qui renvoie une valeur passe sous silence, et le générateur semble terminé mais lève une exception sur la plupart des tirages ([CLI-14](#cli-14)).

### Fluidité et nombre de tests

D = découverte, R = habitué, E = expert. Une cellule se lit « fluidité / tests ». Le nombre de tests est celui que chaque session a rapporté (les cas quand la session a donné des cas, sinon les méthodes).

| Contexte | D | R | E |
|---|---|---|---|
| 21-orbital-microbiology | 4 / 40 | 4 / 42 | 4 / 44 |
| 22-pulmonary-nodules | 4 / 31 | 4 / 36 | 4 / 31 |
| 23-captive-breeding | 4 / 38 | 4 / 34 | 4 / 29 |
| 24-radiocarbon-chronology | 4 / 33 | 4 / 39 | 4 / 38 |
| 25-adaptive-video | 4 / 38 | 4 / 37 | 4 / 34 |
| 26-robotic-milking | 4 / 36 | 4 / 44 | 4 / 48 |
| 27-railway-interlocking | 4 / 35 | 4 / 32 | 4 / 36 |
| 28-satellite-conjunction | 4 / 41 | **3** / 40 | 4 / 38 |
| 29-parimutuel-betting | 4 / 32 | 4 / 34 | 4 / 32 |
| 30-liquidity-gap | 4 / 40 | 4 / 40 | 4 / 41 |
| 31-vessel-stowage | 4 / 34 | 4 / 33 | 4 / 38 |
| 32-grid-load-flow | 4 / 39 | 4 / 38 | 4 / 41 |
| 33-music-score | **3** / 34 | 4 / 40 | 4 / 42 |
| 34-manuscript-reconstruction | 4 / 31 | 4 / 35 | 4 / 35 |
| 35-dna-variants | 4 / 34 | 4 / 42 | 4 / 35 |
| 36-knitting-patterns | 4 / 41 | 4 / 37 | 4 / 38 |
| 37-batch-genealogy | 4 / 31 | 4 / 34 | 4 / 36 |
| 38-carbon-ledger | 4 / 30 | 4 / 32 | 4 / 33 |
| 39-geothermal-reservoir | 4 / 41 | 4 / 40 | 4 / 37 |
| 40-catastrophe-losses | 4 / 40 | 4 / 39 | 4 / 35 |
| **Total** | **719** | **748** | **741** |

Soit 2 208 tests répartis sur 60 suites, tous au vert. La fluidité moyenne est de 3,97.

### Les constats qui comptent le plus

1. **[CLI-11](#cli-11) — un vrai bug, de gravité haute.** Avec `--entry-point any`, la classe d'entrée d'un type `Voice` s'appelle `AnyVoiceEntry`. C'est aussi le nom du générateur d'un type de domaine `VoiceEntry`. L'exécution se termine avec le code 0 et le projet ne compile plus.
2. **[CLI-1](#cli-1) — les gardes d'intervalle `x < a || x > b` ne sont jamais lues.** Cela a touché 18 des 20 contextes, à tous les profils. C'est une limite spécifiée et délibérée (spec §5.3, ADR-0060), mais c'est le coût le plus lourd de l'essai. La documentation utilisateur ne le dit jamais ([DOCS-1](#docs-1)). Cet essai est le type de retour de terrain qu'ADR-0085 demande avant de pouvoir élargir le lecteur de gardes.
3. **[CLI-3](#cli-3) — seule la première garde non lue d'un paramètre est nommée.** Une règle croisée entre paramètres qui arrive plus loin reste cachée. Un utilisateur qui ne corrige que ce qui est nommé obtient un générateur qui compile et lève une exception sur certains tirages. C'est documenté (« nomme la première garde »), mais c'est un piège.
4. **[CLI-8](#cli-8) — un paramètre dérivé d'un autre n'a aucune forme prise en charge.** La voie documentée « dériver dans `Generate()` » transforme les épingles `With…` du paramètre dérivé en no-ops silencieux. La modification est en plus perdue avec `--force`. Rencontré dans 15 contextes.
5. **[CLI-9](#cli-9) — affiner une partie composée repart de la base propre à cette partie.** Cela abandonne en silence une règle posée par la base englobante. Un vérificateur l'a classé comme défaut au regard de la spec D13 (29-12). Les autres l'ont classé comme friction, causée par une phrase trompeuse de la documentation, « n'élargit jamais la base ».
6. **[DOCS-2](#docs-2) — l'organisation suggérée par la page de la CLI ne compile pas.** Une racine `Arbitrary` placée dans un espace de noms qui se termine par `.Arbitrary` échoue avec CS0234, et l'outil ne prévient pas.
7. **Des manques du cœur** sont revenus souvent, et tous obligeaient un test à écrire son propre helper :
   - [CORE-1](#core-1) : `Except` avec une liste vide lève une exception.
   - [CORE-4](#core-4) : aucun idiome pour « B diffère de A ».
   - [CORE-5](#core-5) : aucun combinateur pour un tirage qui dépend d'un autre tirage.
   - [CORE-3](#core-3) : pas de `DistinctBy`.

---

## 2. Par profil

### Découverte : ce qui fait trébucher un développeur qui découvre la bibliothèque

- **Le scaffolder ignore des gardes ordinaires, et rien dans la documentation utilisateur n'explique pourquoi.** `x < lo || x > hi`, `is < a or > b` et le préfixe `double.IsNaN(x) || …` reviennent tous en `to verify, unread guards`. La page utilisateur montre un seul exemple de garde lue et ne liste jamais les formes lues ([CLI-1](#cli-1), [CLI-2](#cli-2), [DOCS-1](#docs-1)). Les nouveaux venus ne pouvaient pas savoir si réécrire une garde aiderait. Ce serait le cas : la découper en `if` séparés permet qu'elle soit lue.
- **Seule la première garde non lue est nommée.** Une règle croisée entre champs (min < opt < max, volume de la centrifugeuse, début ≤ fin) ne se découvre donc qu'en lisant le constructeur ([CLI-3](#cli-3)).
- **Des erreurs de compilation que la documentation explique, mais à un ou deux clics de distance :**
  - CS0029 après le passage à `Any.StringMatching` ([CLI-13](#cli-13)).
  - `AnyIRowElement` introuvable, suivi d'un refus de `any generate IRowElement` parce que le type est abstrait ([CLI-10](#cli-10)).
  - Le conflit de nom de la classe d'entrée en 33 ([CLI-11](#cli-11)).
- **Des dates non bornées.** `Any.DateOnly()` et `Any.DateTimeOffset()` couvrent tout le type, et rien ne le dit pour ces deux types ([DOCS-3](#docs-3)).
- **Un doute sur ce qu'est un dummy.** Un identifiant tiré qui n'apparaît dans une assertion que comme poignée est-il encore un dummy ? ([DOCS-5](#docs-5)).
- **Points mineurs :**
  - Le guide de démarrage ne mentionne jamais `any` ([DOCS-8](#docs-8)).
  - Les indications des analyseurs de niveau Info sont invisibles dans `dotnet build` ([AN-1](#an-1)).

### Habitué : ce qui ralentit un utilisateur régulier

- **Du travail manuel sur les bases numériques.** Dans la plupart des domaines, 9 à 23 paramètres ont demandé un `Between` écrit à la main ([CLI-1](#cli-1)).
- **Les règles qui lient des paramètres.** Elles demandent de remanier `Generate()` à la main. Cela rend les épingles du paramètre dérivé dénuées de sens, et `--force` l'efface ([CLI-8](#cli-8)).
- **Des modifications mécaniques répétées :**
  - Retyper `AnyString` en `AnyPattern` pour chaque value object gardé par une regex ([CLI-13](#cli-13)).
  - Des classes de comparateur, faute de `DistinctBy` ([CORE-3](#core-3)).
  - Une condition autour de chaque liste `Except` calculée, parce qu'une liste vide lève une exception ([CORE-1](#core-1)).
- **Des surprises :**
  - Un `With…(lambda)` imbriqué abandonne ce que la base englobante avait posé ([CLI-9](#cli-9)).
  - Un agrégat mutable épinglé est partagé entre les tirages ([CLI-23](#cli-23)).
  - Un paramètre qui n'apparaît que dans un message d'exception est marqué ([CLI-5](#cli-5)).
  - L'organisation `Arbitrary`-dans-`.Arbitrary` tirée de la documentation ne compile pas ([DOCS-2](#docs-2)).
- **Des conventions à apprendre :**
  - Les générateurs atterrissent par défaut dans l'espace de noms du domaine ([CLI-18](#cli-18)).
  - Les bases `AnyValid…()` sont privées ([CLI-17](#cli-17)).
  - Épingler un paramètre nullable à null demande un cast ([CLI-19](#cli-19)).

### Expert : ce qui limite un artisan du code

- **Pas de tirage dépendant.** Il n'y a ni bind ni `SelectMany`. Les grilles dont les cellules dépendent des dimensions tirées, ou les puits qui doivent se trouver dans une grille tirée, ont été réécrits à la main. Un idiome qui fonctionne mais n'est pas documenté existe : `.As(x => inner.Generate())` ([CORE-5](#core-5)).
- **Des primitives de composition manquantes :**
  - Un générateur de constante public ([CORE-6](#core-6)).
  - Un choix parmi plusieurs générateurs ([CORE-12](#core-12)).
  - Une permutation ou un sous-ensemble d'une liste connue ([CORE-7](#core-7)).
  - Des collections ordonnées ([CORE-13](#core-13)).
  - Restreindre un ensemble `WithChars` ou un `AnyPattern` ([CORE-8](#core-8), [CORE-9](#core-9)).
- **La fenêtre de magnitude ordinaire.** Une pression valide jusqu'à 100 MPa est toujours tirée sous 1 MPa, et une perméabilité sur 11 décades tombe presque toujours dans la décade la plus haute ([CORE-14](#core-14)).
- **De petites sources de bruit :**
  - CA1822 sur les scaffolds à un seul paramètre ([CLI-21](#cli-21)).
  - Des indications de niveau Info qu'une compilation en terminal ne montre jamais ([AN-1](#an-1)).
  - JD009 ne voit pas les tirages faits au travers des générateurs scaffoldés ([AN-2](#an-2)).
  - L'attribut de rejeu collé demande un `using` qu'il ne mentionne pas ([XU-1](#xu-1)).
  - Un échec rapporte la graine mais jamais les valeurs tirées ([XU-2](#xu-2)).

---

## 3. Par composant

Abréviations : D/R/E = découverte/habitué/expert. « Rencontré dans » liste chaque contexte avec les profils concernés, puis les identifiants des constats d'origine.

### Core

#### CORE-1 — `Except` avec une liste vide lève une exception au lieu de n'exclure rien
<a id="core-1"></a>
- **Nature / gravité :** friction, moyenne
- **Rencontré dans :** 23 (E), 24 (E), 37 (E), 38 (E). Constats 23-7, 24-3, 37-6, 38-4.
- **Reproduction minimale :**
  ```csharp
  string[] taken = [];
  Any.StringMatching("[A-Z]{2,4}-[0-9]{4,6}").Except(taken).Generate();
  // same on Any.String().Except(...) and Any.Int32().Except(new int[0])
  ```
- **Attendu :** une liste d'exclusion vide laisse le domaine inchangé, de la même façon que `Except(double.NaN)` est un no-op documenté.
- **Constaté :** `System.ArgumentException : At least one value is required. (Parameter 'values')`. Une fixture dont l'ensemble « déjà utilisés » commence vide demande une condition à chaque point d'appel.
- **Verdict :** confirmé en 23. Partiellement en 24, 37 et 38.
  - Le refus est délibéré, uniforme sur tous les `Except`, et documenté dans la doc XML (`<exception … when values is empty>`).
  - JD014 signale bien un `Except()` vide constant.
  - Ce qui reste : la page de JD014 et les pages sur les chaînes et sur les énumérations et choix ne citent que `OneOf()`. Aucun ADR ne justifie de refuser une exclusion vide, qui est l'identité et non un domaine vide. ADR-0033 critique déjà ce message.

#### CORE-2 — Aucun moyen de garder des valeurs distinctes d'un tirage à l'autre ; `Except` n'a pas de surcharge `IEnumerable<T>`
<a id="core-2"></a>
- **Nature / gravité :** manque, basse
- **Rencontré dans :** 37 (E). Constat 37-7.
- **Reproduction minimale :**
  ```csharp
  HashSet<int> used = new();
  Any.Int32().Between(1, 6).Except(used).Generate(); // CS1503
  ```
- **Attendu :** une surcharge `Except(IEnumerable<T>)` (`OneOf` en a déjà une), ou un tirage « pas encore utilisé » limité à une portée.
- **Constaté :** seul `params T[]` existe, si bien que l'ensemble des valeurs utilisées est tenu à la main et passé avec `.ToArray()` à chaque appel.
- **Verdict :** confirmé. La surcharge `IEnumerable<T>` est un petit ajout symétrique. Une portée qui suivrait les valeurs utilisées élargirait l'ambition du générateur et demande une décision au titre d'ADR-0046.

#### CORE-3 — Pas de distinction par clé (`DistinctBy`)
<a id="core-3"></a>
- **Nature / gravité :** friction / manque, basse
- **Rencontré dans :** 22 (R, E), 24 (D), 27 (D), 31 (E), 40 (D). Constats 22-4, 24-8, 27-7, 31-10, 40-8.
- **Reproduction minimale :**
  ```csharp
  Any.ListOf(Any.StratigraphicLayer()).WithCount(2).DistinctBy(l => l.Code); // CS1061
  ```
- **Attendu :** une surcharge avec sélecteur de clé. En 22, elle devrait aussi plafonner le nombre d'éléments selon la cardinalité de la clé, comme le fait `SetOf(Enum)`.
- **Constaté :** seuls `Distinct()` et `Distinct(IEqualityComparer<T>)` existent. En 22, `Distinct(comparer)` sans nombre maximal échoue après les nouveaux tirages bornés : « Could not generate a distinct collection of 9 elements … only 8 distinct value(s) ». L'échec est bruyant et rapporte sa graine.
- **Verdict :**
  - Confirmé en 22, 24, 27 et 40.
  - Partiellement en 31 : la voie du comparateur est documentée (page des collections, page de JD028) et fonctionne, donc ce qui manque relève du confort, pas de la capacité.
  - Avec les types nullables activés, `EqualityComparer<T>.Create` demande `a!`/`b!`.

#### CORE-4 — Deux valeurs du même type qui doivent différer : pas de `DifferentFrom` sur `IAny<T>`, pas de recette documentée
<a id="core-4"></a>
- **Nature / gravité :** manque, moyenne
- **Rencontré dans :** 23 (D, R), 27 (D, R, E), 32 (D, R, E), 33 (R), 34 (R), 35 (D, R, E). Constats 23-5, 27-4, 27-5, 32-6, 33-9, 34-8, 35-3, 35-7.
- **Reproduction minimale :** `any generate Variant` sur un constructeur qui lève une exception quand `reference.Equals(alternate)`. Puis :
  ```csharp
  new AnyAllele().DifferentFrom(a); // CS1061
  // Pitch: Arbitrary.Pitch().DifferentFrom(anyPitch) — CS1061
  ```
- **Attendu :** un moyen de déclarer le lien, comme `DifferentFrom` sur `IAny<T>` ou une paire distincte, ou une recette documentée pour « B diffère de A » qui conserve les deux épingles `With…`.
- **Constaté :** deux tirages indépendants. Ils se percutent sur de rares graines (environ 1 sur 315 pour Pitch, 71 sur 2 000 pour les identifiants de bus à une lettre), si bien que les tests deviennent instables. Les trois utilisateurs de 35 ont écrit à la main une boucle de nouveaux tirages bornée dans le `Generate()` scaffoldé.
- **Verdict :**
  - Confirmé en 23, 27-4, 33, 35-3 et 35-7. La section de documentation « When a guard ties two parameters » couvre l'ordre, la dérivation et les chiffres de contrôle, mais pas l'inégalité. `IAny<T>` n'expose que `Generate()`, `As` et `OrNull`.
  - Partiellement en 27-5, 32 et 34. Des briques documentées font déjà le travail :
    - `new AnyX().WithComponent(p => p.DifferentFrom(first.Component))` dans `Generate()` conserve la base.
    - `Any.ArrayOf(g).WithCount(2).Distinct()` fonctionne aussi, mais ignore les épingles.
  - Aucune page ne montre l'une ou l'autre de ces briques employée dans ce cas.

#### CORE-5 — Aucun combinateur pour un tirage qui dépend d'un autre tirage
<a id="core-5"></a>
- **Nature / gravité :** manque, moyenne
- **Rencontré dans :** 28 (D), 33 (D, R, E), 36 (D, E), 39 (E). Constats 28-4, 33-6, 36-6, 39-4.
- **Reproduction minimale :** tirer une `ReservoirSimulation` dont les puits occupent des cellules distinctes de la grille tirée. Ou tirer une `Measure` dont les voix remplissent la signature rythmique tirée.
- **Attendu :** un opérateur de type bind, par exemple `anyGrid.Then(grid => wellsInside(grid))`, ou un idiome documenté.
- **Constaté :** `Any.Combine` ne prend que des opérandes indépendants. Les utilisateurs ont écrit des classes `IAny<T>` sur mesure, ou des champs `Func<TDep, IAny<T>>` que `--force` efface.
- **Verdict :**
  - Confirmé en 33 et 36 : l'API publique n'a ni `SelectMany`, ni `Bind`, ni `Then`.
  - Partiellement en 28 et 39 :
    - `design-principles.en.md` cite ce cas comme un cas à assembler à la main.
    - `anyGrid.As(grid => inner(grid).Generate())` fonctionne déjà et se rejoue avec la graine (39-4, 200 tirages).
    - La documentation ne montre jamais cet idiome, ne dit jamais qu'un `Generate()` imbriqué dans une lambda est pris en charge, et ne prévient jamais qu'il tire depuis la source ambiante et non depuis un contexte `Any.WithSeed`.

#### CORE-6 — Pas de générateur de constante public
<a id="core-6"></a>
- **Nature / gravité :** manque, basse
- **Rencontré dans :** 28 (D, E), 36 (E), 40 (E). Constats 28-5, 36-8, 40-9.
- **Reproduction minimale :** scaffolder deux types quelconques et les comparer. Chacun porte un `FixedValue<TValue>` privé imbriqué, identique octet pour octet. Les recettes écrites à la main demandent `Any.OneOf<IEnumerable<T>>(array)` avec un argument de type explicite.
- **Attendu :** quelque chose comme `Any.Fixed(value)` / `Any.Just(value)`.
- **Constaté :** pas d'`IAny<T>` constant public. `Any.OneOf(value)` refuse null et consomme un tirage.
- **Verdict :**
  - Confirmé. La spec, §4.2 et §15, note déjà que `Any.Fixed<T>` permettrait à l'émetteur de se passer du helper (reporté, pas rejeté).
  - Partiellement en 28 sur un point : un `Generate()` peut honorer les deux épingles d'une paire enum/bool liée par un nouveau tirage conjoint borné, sans détecter les épingles. Aucune page ne le montre.

#### CORE-7 — Pas de permutation ni de sous-ensemble d'une collection connue
<a id="core-7"></a>
- **Nature / gravité :** manque, basse
- **Rencontré dans :** 25 (E), 26 (E), 29 (E), 38 (E), 40 (E). Constats 25-10, 26-13, 29-10, 38-9, 40-9.
- **Reproduction minimale :**
  ```csharp
  List<UdderQuarter> order = Any.ListOf(Any.ElementOf(all)).WithCount(all.Length).Distinct().Generate();
  ```
- **Attendu :** `Any.PermutationOf(items)`, `Any.SubsetOf(items).WithCount(n)`, ou un idiome documenté.
- **Constaté :** la forme indirecte ci-dessus. Elle fonctionne, parce que les collections sont mélangées en interne (Fisher-Yates) et qu'ADR-0113 contrôle la cardinalité, mais l'intention reste cachée.
- **Verdict :**
  - Confirmé en 26, 29 et 40.
  - Partiellement en 25 et 38 (classé friction dans ces contextes) :
    - `Any.ListOf(Any.ElementOf(rungs)).Distinct()` fonctionne même sans égalité de valeur, parce que ce sont les instances existantes qui sont renvoyées.
    - `Any.SetOf(Any.ElementOf(issued)).WithCount(2)` n'a pas besoin de `ToList()`.
    - Rien de tout cela n'est documenté comme recette.

#### CORE-8 — Un ensemble `WithChars` ne peut pas être restreint, et il n'existe pas de `WithoutChars`
<a id="core-8"></a>
- **Nature / gravité :** manque, moyenne
- **Rencontré dans :** 35 (D, R, E). Constat 35-1. Le manque de documentation associé est [DOCS-4](#docs-4).
- **Reproduction minimale :**
  ```csharp
  Any.String().WithChars("ACGTN").WithLengthBetween(1, 200).WithChars("ACGT").Generate();
  ```
- **Attendu :** les ensembles inclus s'intersectent, ou `WithoutChars("N")` existe, ou l'erreur indique une piste.
- **Constaté :** `ConflictingAnyConstraintException : Cannot apply WithChars("ACGT") because WithChars("ACGTN") is already defined.` Les seules soustractions sont `WithoutAlpha()` et `WithoutNumeric()`.
- **Verdict :** confirmé. Le « déclaré une seule fois » est documenté, mais un ensemble strictement inclus est une pure restriction. La page sur les chaînes dit en outre que l'univers est restreint par « every constraint below … no exception ».

#### CORE-9 — Un `AnyPattern` ne peut pas être restreint à un motif plus petit
<a id="core-9"></a>
- **Nature / gravité :** friction, basse
- **Rencontré dans :** 26 (R), 37 (E). Constats 26-8, 37-8.
- **Reproduction minimale :**
  ```csharp
  Arbitrary.AnimalId().WithValue(v => v.StartingWith("FR")); // CS1061 on AnyPattern
  ```
- **Attendu :** un moyen de restreindre, ou des indications pour ce cas.
- **Constaté :** seuls `OneOf`, `Except` et `DifferentFrom` existent. Le point d'appel se rabat sur `_ => Any.StringMatching("FR[0-9]{12}")`, qui est justement la forme remplaçant la base contre laquelle la page de la CLI met en garde. Sinon, l'utilisateur écrit à la main une méthode qui reformule une partie du motif.
- **Verdict :**
  - Confirmé en 37 et partiellement en 26.
  - La limite est délibérée : strings.en.md, « What you can still constrain », suit ADR-0008 et ADR-0046.
  - Ce qui reste : aucune page ne couvre le cas d'un « sous-langage à un point d'appel ».

#### CORE-10 — Un second `OneOf` est refusé, et le message n'oriente pas vers `Except`
<a id="core-10"></a>
- **Nature / gravité :** friction, basse
- **Rencontré dans :** 31 (E). Constat 31-7.
- **Reproduction minimale :**
  ```csharp
  Any.Int32().OneOf(2,4,…,98).OneOf(2,4,6,8,82,84,86,88)
  ```
- **Attendu :** une intersection, ou un message qui suggère `Except(...)`.
- **Constaté :** `ConflictingAnyConstraintException: Cannot apply OneOf(…) because OneOf(…) is already defined.`
- **Verdict :** confirmé. Le refus est documenté (page de la CLI, JD034). La voie de restriction n'apparaît que sur la page de la CLI, pas dans le message.

#### CORE-11 — Formes d'entiers : une union d'intervalles et les nombres impairs
<a id="core-11"></a>
- **Nature / gravité :** friction, basse
- **Rencontré dans :** 31 (D, R). Constats 31-8, 31-9.
- **Reproduction minimale :** tirer un niveau pair dans 2–20 ou 82–98. Tirer une baie impaire dans 1..99. `Between(1, 99).MultipleOf(2, 1)` donne CS1501.
- **Attendu :** une forme concise d'union et un multiple avec décalage, ou des idiomes documentés pour les deux.
- **Constaté :**
  - Union : `OneOf(Enumerable.Range(2,19).Concat(Enumerable.Range(82,17))).MultipleOf(2)` fonctionne en une ligne, parce que `OneOf(IEnumerable<int>)` existe, mais ce n'est pas documenté pour les intervalles.
  - Les nombres impairs demandent `Between(0, 49).As(n => 2*n+1)`.
- **Verdict :**
  - Partiellement pour l'union : la liste de 30 littéraux annoncée n'était pas nécessaire.
  - Confirmé pour les nombres impairs : le treillis interne a déjà une ancre, mais le `MultipleOf` public est toujours ancré à 0.

#### CORE-12 — Pas de choix parmi plusieurs générateurs
<a id="core-12"></a>
- **Nature / gravité :** manque, basse
- **Rencontré dans :** 36 (D, R, E). Constat 36-5.
- **Reproduction minimale :**
  ```csharp
  Any.Combine(Any.Boolean(), new AnyStitchRun(), new AnyStitchRepeat(),
              (isRepeat, run, repeat) => isRepeat ? (IRowElement)repeat : run);
  ```
- **Attendu :** une union documentée qui ne tire que depuis le générateur choisi.
- **Constaté :** `Combine` tire les deux branches à chaque fois (10 appels sur 10 pour chacune). `OneOf` prend des valeurs, et JD012 met en garde à juste titre contre le fait de lui passer des générateurs, mais aucune page ne nomme d'alternative.
- **Verdict :** confirmé.

#### CORE-13 — Pas de collections ordonnées ni de règles entre éléments
<a id="core-13"></a>
- **Nature / gravité :** manque, basse
- **Rencontré dans :** 25 (R), 40 (D, R). Constats 25-9, 40-3.
- **Reproduction minimale :** quatre doubles distincts dans [0, 100] en ordre croissant. Ou des barreaux d'échelle dont la hauteur ne diminue jamais quand le débit augmente.
- **Attendu :** `.Ascending()` ou une recette documentée.
- **Constaté :** les utilisateurs ont écrit `.As(l => l.Order()…)`, qui perd les méthodes d'affinage du builder, ou un `IAny` sur mesure d'une trentaine de lignes.
- **Verdict :**
  - Confirmé en 40.
  - Partiellement en 25 : une expression `.As(...)` de six lignes fait l'affaire. Ce qui manque, c'est la documentation. L'audit de conception du 2026-07-20 classe un `Sorted()` de premier rang comme reporté.

#### CORE-14 — Les doubles bornés ne couvrent qu'un coin de l'intervalle déclaré
<a id="core-14"></a>
- **Nature / gravité :** friction, moyenne
- **Rencontré dans :** 39 (D, E). Constat 39-5.
- **Reproduction minimale :**
  ```csharp
  Any.Double().GreaterThan(0.0).LessThanOrEqualTo(100e6) // 2000 draws
  Any.Double().Between(1e-20, 1e-9)
  ```
- **Attendu :** un moyen documenté de tirer sur tout l'intervalle déclaré, ou sur une échelle logarithmique.
- **Constaté :**
  - Pression : le maximum sur 2 000 tirages était 999 823, jamais au-dessus de 1e6.
  - Perméabilité : 1 811 tirages sur 2 000 étaient ≥ 1e-10, et aucun n'était ≤ 1e-17.
- **Verdict :** confirmé. Le comportement est voulu et documenté (ADR-0097, page des nombres). Aucun ADR n'envisage une fenêtre proportionnée aux bornes ou une répartition logarithmique, et changer cela demande une décision.

### CLI (`any`)

#### CLI-1 — Les gardes d'intervalle jointes par `||` ne sont jamais lues (préfixes `IsNaN`/`IsFinite` compris)
<a id="cli-1"></a>
- **Nature / gravité :** manque / friction (les vérificateurs sont partagés ; le comportement est spécifié), moyenne. Rapporté haute en 32.
- **Rencontré dans :** 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 34, 36, 38, 39 et 40 (D, R, E à chaque fois), et 33 (R pour 33-3 ; D, R, E pour 33-5). Constats 21-1, 22-2, 23-1, 24-1, 25-1, 25-2, 26-1, 27-1, 28-1, 29-1, 30-3, 31-1, 32-1, 33-3, 33-5, 34-5, 36-1, 38-1, 39-1, 40-1.
- **Reproduction minimale :**
  ```csharp
  public sealed class P { public P(int a) { if (a < 1 || a > 100) throw new ArgumentOutOfRangeException(nameof(a)); } }
  public sealed class Q { public Q(int a) { if (a < 1) throw new ArgumentOutOfRangeException(nameof(a)); if (a > 100) throw new ArgumentOutOfRangeException(nameof(a)); } }
  // any generate P Q --dry-run
  ```
- **Attendu :** `P` est lu comme `Q`. Le préfixe `double.IsNaN(x) ||` est ignoré, puisque NaN n'est jamais tiré. À défaut, la documentation utilisateur dit que cette forme n'est pas lue.
- **Constaté :**
  - `P` : `a int Any.Int32() to verify, unread guards`.
  - `Q` : `Any.Int32().Positive().LessThanOrEqualTo(100) guard`.
  - Il en va de même pour `double`, `decimal` et `int`, avec des bornes littérales ou des constantes nommées.
  - `IsNullOrWhiteSpace(x) || x.Length > n` retombe sur `NonEmpty()`.
  - `Count == 0 || Any(x => x is null)` perd `NonEmpty()`.
  - Impact par domaine : 13 sur 22, 15 sur 17, 23 sur 30, et « chaque paramètre numérique ».
- **Verdict :** partiel dans l'ensemble. Les faits ont été reproduits dans chaque contexte.
  - L'exclusion est spécifiée : la spec, §5.3 ligne 639, dit que la condition d'une garde « contains no `&&` or `||` ». ADR-0060 dit « no boolean composition ». `GuardBoundaryTests.cs:1259` fige ce comportement, et ADR-0085 gèle le lecteur dans l'attente de retours de terrain.
  - Plusieurs vérificateurs l'ont reclassé en friction (27, 28, 29, 31, 33-3, 34, 36, 40).
  - Les helpers ThrowIf et les `if` séparés sont lus, et ce contournement n'est pas documenté pour les utilisateurs.
  - Plusieurs vérificateurs notent qu'un `if (A || B)` qui lève une exception équivaut exactement à `if (A) throw; if (B) throw;`, que le moteur lit déjà. Lire une disjonction portant sur un seul paramètre fonctionnerait donc par construction (ce qui est cohérent avec ADR-0046), mais cela modifie une règle de la spec.
  - Une garde `double.IsNaN(x)` isolée n'est pas lue non plus, parce que l'ensemble fermé a une ligne `!double.IsFinite` mais pas de ligne `IsNaN` ([CLI-15](#cli-15)).
  - Le volet documentation est [DOCS-1](#docs-1).

#### CLI-2 — Les gardes à motif relationnel (`is < a or > b`, `is > n`) ne sont pas lues
<a id="cli-2"></a>
- **Nature / gravité :** manque, moyenne
- **Rencontré dans :** 23 (D, E), 30 (D, R, E), 33 (D, R, E), 34 (D, R, E). Constats 23-1, 30-2, 33-2, 34-4.
- **Reproduction minimale :**
  ```csharp
  public P(int a, int b, int c) {
      if (a is < -2 or > 2) throw …;
      if (b < -2) throw …; if (b > 2) throw …;
      ThrowIfLessThan(c, -2); ThrowIfGreaterThan(c, 2);
  }
  ```
- **Attendu :** `a` → `Any.Int32().Between(-2, 2) guard`.
- **Constaté :** `a int Any.Int32() to verify, unread guards`, alors que `b` et `c` → `Between(-2, 2) guard`. Même un simple `value.Length is > 12` n'est pas lu, alors que `value.Length > 12` est lu comme `.WithMaxLength(12)`.
- **Verdict :**
  - Confirmé en 30 et 33.
  - Partiellement en 34 : un `is 0 or > 12` combiné est l'écriture en motif de l'exclusion documentée de `||`.
  - Le vrai manque : `Guards.TryRecognise` n'accepte un motif `is` que pour `is null`. Ni la spec ni la page utilisateur ne déclarent les motifs relationnels hors périmètre. C'est un candidat pour un retour de terrain au titre d'ADR-0085.

#### CLI-3 — Seule la première garde non lue de chaque paramètre est nommée ; les gardes suivantes et croisées restent cachées
<a id="cli-3"></a>
- **Nature / gravité :** friction (plusieurs déposés comme défaut, reclassés), moyenne
- **Rencontré dans :** 21 (D, R), 22 (R, E), 24 (R, E), 25 (D, R, E), 26 (R, E), 27 (R, E), 28 (E), 29 (R, E), 30 (D, E), 32 (D), 34 (D, E), 37 (D, E), 39 (D, R, E), 40 (D, R, E). Constats 21-2, 22-5, 24-2, 24-11, 25-12, 26-2, 27-2, 28-7, 29-3, 30-1, 32-3, 34-2, 37-2, 39-9, 40-2.
- **Reproduction minimale :**
  ```csharp
  public BehaviouralAssumption(decimal s, int volatileRunOffDays, int coreMaturityMonths) {
      if (volatileRunOffDays is < 1 or > 365) throw …;        // line 16
      if (coreMaturityMonths is < 1 or > 120) throw …;        // line 21
      if (volatileRunOffDays >= coreMaturityMonths * 30) throw …; // line 26
  }
  ```
- **Attendu :** toutes les gardes non lues qui touchent le paramètre sont listées, ou au moins « et N de plus », avec une mention explicite des gardes croisées.
- **Constaté :** seules la ligne 16 et la ligne 21 sont nommées. Une fois ces intervalles corrigés et les lignes TODO supprimées, le générateur compile et lève une exception sur certains tirages :
  - (204 jours, 3 mois) ici.
  - 15 tirages sur 2 000 pour MilkingSettings (26).
  - Environ la moitié des tirages pour CalendarInterval (24).
  - Les gardes de distinction de Route et la regex de WitnessSiglum ne sont jamais mentionnées (27, 34).
- **Verdict :** faits confirmés partout. Le comportement est spécifié et documenté :
  - spec §5.6 et §6.1 : « the earliest such place ».
  - page de la CLI vers la ligne 94 : « names the first guard ».
  - le commentaire lui-même dit « first at ».
  - Chaque paramètre touché reste marqué et la compilation reste bloquée, donc la promesse d'ADR-0083 tient.
  - Ce qui reste : la ligne de récapitulatif elle-même ne dit pas « première », rien ne compte les autres, et l'échec qu'ADR-0083 voulait éviter revient dès que l'utilisateur supprime la ligne TODO après n'avoir vérifié que la garde nommée.

#### CLI-4 — L'extrait de la garde est tronqué, et une condition sur plusieurs lignes est coupée sans points de suspension
<a id="cli-4"></a>
- **Nature / gravité :** friction, basse
- **Rencontré dans :** 22 (R, E), 39 (D), 40 (D). Constats 22-5, 39-6, 40-6.
- **Reproduction minimale :**
  ```csharp
  if (double.IsNaN(k)
      || k < 1.0
      || k > 5.0) throw …;
  // any generate Cell
  ```
- **Attendu :** le texte complet de la garde dans le commentaire du fichier, ou au moins « ... » quand la citation est coupée.
- **Constaté :**
  - Une garde sur plusieurs lignes est citée sous la forme `if (double.IsNaN(k)`, sans aucun marqueur.
  - Une longue garde sur une seule ligne est coupée à 100 caractères avec « ... ». En 40, la coupe est tombée précisément sur la constante que l'utilisateur devait recopier.
- **Verdict :** partiel. C'est spécifié (spec §5.6, « the first line of the statement … shortened past a hundred characters » ; `UnreadGuard.ExcerptLength`). L'incohérence demeure : une coupe de ligne n'est pas marquée alors qu'une coupe de longueur l'est, et le commentaire du fichier partage la limite de largeur de la console.

#### CLI-5 — Un paramètre utilisé seulement dans un message d'exception est marqué comme gardé
<a id="cli-5"></a>
- **Nature / gravité :** défaut, basse
- **Rencontré dans :** 33 (D, R, E), 36 (D, R, E). Constats 33-4, 36-3.
- **Reproduction minimale :**
  ```csharp
  public Row(int number, int? count) {
      if (number < 1) throw …;
      if (count is { } c) { if (c != 3) throw new ArgumentException($"Row {number} is wrong.", nameof(count)); }
  }
  ```
- **Attendu :** `number int Any.Int32().Positive() guard`.
- **Constaté :** `number … to verify, guard, unread guards` et `unread guard at … if (count is { } c)`. Retirer `{number}` du message fait disparaître la marque.
- **Verdict :** confirmé. `Guards.Mentioned` n'exclut que `nameof`, et `MarkIfItRejects` marque tout paramètre mentionné n'importe où dans l'instruction qui lève l'exception. Cela contredit les remarques du moteur lui-même (« a message rejects nothing ») et la spec §5.3.

#### CLI-6 — L'emplacement de la garde non lue pointe vers un en-tête de boucle ou une instruction de repli, pas vers la garde
<a id="cli-6"></a>
- **Nature / gravité :** défaut, basse
- **Rencontré dans :** 32 (R, E), 39 (D, R, E). Constats 32-4, 39-7, 39-9.
- **Reproduction minimale :**
  ```csharp
  public Sim(Box box, IReadOnlyCollection<int> items) {
      foreach (int item in items) {                                  // line 7
          if (!box.Contains(item)) throw …;                          // line 9
      }
  }
  ```
- **Attendu :** `box: unread guard at Sim.cs, line 9: if (!box.Contains(item)) …`
- **Constaté :** `box: unread guard at Sim.cs, line 7: foreach (int item in items)`. En 32, `name`, dont la propre garde a été lue, est renvoyé vers un `foreach` sur `buses` par le repli « tout marquer » d'ADR-0110.
- **Verdict :**
  - Confirmé en 39 : cela contredit la page de la CLI (« so you do not have to search the constructor ») et la spec §5.7.
  - Partiellement en 32 : la citation suit ADR-0110 et la règle du récapitulatif, mais rien n'indique au lecteur que la marque vient du repli.

#### CLI-7 — Une vérification d'élément null marque une liste `to verify` alors que `ListOf` ne produit jamais null
<a id="cli-7"></a>
- **Nature / gravité :** friction, basse
- **Rencontré dans :** 33 (D, R, E), 36 (E). Constats 33-5, 36-4.
- **Reproduction minimale :**
  ```csharp
  if (runs.Count == 0) throw …;
  if (runs.Any(r => r is null)) throw …;
  // any generate StitchRepeat
  ```
- **Attendu :** `Any.ListOf(new AnyRun()).NonEmpty() guard`.
- **Constaté :** `… to verify, guard, unread guards`, qui pointe vers la ligne de l'élément null.
- **Verdict :** confirmé. Aucune ligne de l'ensemble fermé ne couvre ce cas (spec §5.3, §9). Il peut être lu sans risque quand la recette de l'élément est composée. Ajouter la ligne est un élargissement délibéré (ADR-0082, ADR-0085).

#### CLI-8 — Un paramètre dérivé d'un autre n'a aucune forme prise en charge : les épingles deviennent des no-ops et la modification est perdue avec `--force`
<a id="cli-8"></a>
- **Nature / gravité :** manque (documentation) / friction (CLI), moyenne
- **Rencontré dans :** 21 (E), 22 (D, R, E), 24 (D, R, E), 25 (D, R, E), 26 (D, R, E), 28 (R), 29 (D, R), 30 (D, R, E), 31 (D, R, E), 32 (D, R, E), 34 (D, R, E), 37 (D, R), 38 (D, R, E), 39 (D, R), 40 (R, E). Constats 21-4, 22-3, 24-4, 24-5, 25-5, 26-3, 28-6, 29-5, 29-6, 30-5, 31-2, 32-5, 34-1, 37-1, 38-2, 39-3, 40-4, 40-7.
- **Reproduction minimale (34) :**
  ```csharp
  // Witness(…, int datedFrom, int datedTo, …) throws if datedFrom > datedTo
  // Option 1 — derive datedTo from datedFrom in Generate(), as the CLI page suggests:
  Any.Witness().WithDatedTo(1600).Generate().DatedTo; // -269: the pin is ignored
  // Option 2 — disjoint bands (from -800..800, to 800..1600):
  Any.Witness().WithDatedFrom(1500).Generate(); // throws on 173 of 200 seeds
  ```
- **Attendu :** un motif documenté ou un point d'extension scaffoldé pour un paramètre dépendant, qui garde un sens à ses épingles `With…` et survit à `--force`, ou au moins un avertissement disant qu'épingler un seul côté dépend de la graine.
- **Constaté :**
  - Les utilisateurs ont remanié les champs, le constructeur privé, les méthodes `With…` et `Generate()` dans le fichier généré : une quinzaine d'endroits en 37, et de 60 à 230 lignes en 31.
  - Ils ont eu recours à des expédients : des champs nullables « dériver sauf si épinglé », des tests de type comme `_x is AnyInt32`, et des champs `Func<TDep, IAny<T>>`.
  - Un fichier partiel ne peut pas remplacer `Generate()`, donc `--force` efface tout cela.
  - En 22, la dérivation documentée a changé le sens de l'épingle : `WithUncertaintyMm3(5.0)` est devenu une fraction et le constructeur a levé une exception.
- **Verdict :** confirmé en 21, 22, 24, 29, 30, 32, 34 et 39. Partiellement en 25, 26, 28, 31, 37, 38 et 40.
  - L'écrasement par `--force` est une limite documentée et délibérée (ADR-0056 ; la page de la CLI prévient ; l'en-tête du fichier le dit).
  - La forme `Any.Combine` conservée dans votre propre fichier survit bien à `--force`, mais elle abandonne la surface `With…` scaffoldée.
  - En 26, la règle était une garde du constructeur, si bien que `--force` a ramené le TODO bloquant : la perte n'était pas silencieuse.
  - Une simple dérivation locale dans `Generate()` tient en deux lignes. Seul le fait de garder un sens aux épingles demande la grosse modification.
  - Aucune page et aucun ADR ne traite de ce que deviennent les épingles d'un paramètre dérivé. La plupart des vérificateurs attendent une correction de la documentation plutôt qu'un solveur (ADR-0046, ADR-0060).

#### CLI-9 — Affiner une partie composée repart de la base propre à cette partie, et abandonne en silence la règle de la base englobante
<a id="cli-9"></a>
- **Nature / gravité :** friction (un vérificateur : défaut au regard de la spec D13), moyenne. Rapporté haute en 28.
- **Rencontré dans :** 23 (D, R, E), 25 (R), 26 (E), 28 (R), 29 (D, R, E), 30 (R, E), 31 (E), 37 (R). Constats 23-2, 23-9, 25-7, 26-12, 28-3, 29-12, 30-10, 31-3, 37-5.
- **Reproduction minimale (23) :**
  ```csharp
  // AnyKinshipPolicy.AnyValidMaximumPairKinship() => new AnyKinshipCoefficient().WithValue(v => v.LessThanOrEqualTo(0.25));
  new AnyKinshipPolicy().WithMaximumPairKinship(k => k.WithValue(v => v.GreaterThanOrEqualTo(0.125))).Generate();
  ```
- **Attendu :** l'affinage se compose avec la base englobante (un tirage dans [0,125, 0,25]), ou la documentation ou un analyseur dit clairement qu'il repart de la base du type interne.
- **Constaté :** `ArgumentOutOfRangeException: A policy may not tolerate kinship above 0.25` (tirage à 0,72). Taux mesurés ailleurs :
  - 29 : 142 pools sur 2 000 avaient un champ sous le ≥ 4 du parent, et 34 ont levé une exception. Un `LessThan(4)` contradictoire n'a pas été signalé.
  - 25 : 710 tirages sur 2 000 ont levé une exception.
  - 28 : environ 2 exécutions sur 3 ont nommé le mauvais paramètre.
  - Il en va de même avec « le dernier appel gagne » d'une méthode de recette à l'autre (26, 31).
- **Verdict :** partiel dans la plupart des contextes.
  - Le `With…(Func)` généré appelle toujours `configure(AnyValidX())`. C'est documenté : « what you set replaces what the base set for that part », « The last call wins … starts again from the base » (ADR-0109).
  - Mais la puce suivante, « One that builds on its argument never widens the base », est énoncée sans condition. Elle est fausse pour les parties imbriquées.
  - La voie de réutilisation documentée consiste à garder l'affinage sous forme de `Func` et à le composer.
  - Le vérificateur de 29-12 l'a classé comme défaut au regard de la spec D13 (« it never replaces the base »).
  - Aucun analyseur ne prévient, et JD034 ne voit pas l'intérieur des lambdas.

#### CLI-10 — Un paramètre composé typé par une interface devient `new AnyIRowElement()`, que `any` refuse de scaffolder
<a id="cli-10"></a>
- **Nature / gravité :** manque, moyenne
- **Rencontré dans :** 36 (D, R, E). Constat 36-2.
- **Reproduction minimale :**
  ```csharp
  public interface IRowElement { int Produces { get; } }
  public sealed class RowInstruction { public RowInstruction(IReadOnlyList<IRowElement> elements) { … } }
  // any generate RowInstruction
  // dotnet build
  // any generate IRowElement
  ```
- **Attendu :** un TODO qui nomme le type d'élément abstrait, idéalement avec ses implémentations concrètes, ou une note documentée à côté du cas des types génériques.
- **Constaté :** `error CS0246: 'AnyIRowElement' could not be found`. En suivant la « liste de travail » documentée, `any generate IRowElement` échoue ensuite avec : `✗ IRowElement is abstract, so Generate() has nothing to instantiate. Scaffold a concrete type that derives from it instead.` (code de sortie 1).
- **Verdict :** partiel. Le `new Any{T}()` aveugle est voulu (spec §5.4, ADR-0089), et un `AnyIRowElement : IAny<IRowElement>` écrit à la main règle le problème. Ni la page utilisateur ni la spec ne couvrent une interface ou un type abstrait composé, et le refus de l'outil éloigne de la solution.

#### CLI-11 — La classe d'entrée `Any{T}Entry` entre en collision avec le générateur d'un type nommé `{T}Entry`
<a id="cli-11"></a>
- **Nature / gravité :** bug, haute
- **Rencontré dans :** 33 (D, E). Constat 33-1.
- **Reproduction minimale :**
  ```
  # domain: VoiceEntry and Voice(int n, IReadOnlyList<VoiceEntry> e) in one namespace
  any generate VoiceEntry Voice --entry-point any
  dotnet build
  ```
- **Attendu :** un nom de classe d'entrée qui ne peut pas entrer en conflit, ou une détection qui refuse ou avertit en nommant les deux fichiers.
- **Constaté :** code de sortie 0 avec `✓ AnyVoice.Entry.cs — entry point Any.Voice()`. Puis `AnyVoice.Entry.cs(10,21): error CS0260: Missing partial modifier on declaration of type 'AnyVoiceEntry'`, suivi d'une cascade de CS0714, CS0708, CS0710 et CS0722 qui pointent vers le générateur. `--dry-run`, alors que le conflit existe déjà, se termine lui aussi avec le code 0 et sans avertissement.
- **Verdict :** confirmé. `EntryPointEmitter.cs:184` émet `{GeneratorName}Entry` sans vérifier les collisions. Le seul nom réservé est la racine `Any`. Ce n'est pas documenté comme une limite. (La partie du rapport concernant `--force` n'a pas été testée.)

#### CLI-12 — Un nom de type court qui entre en conflit avec un type du framework fait refuser le type et échouer le lot
<a id="cli-12"></a>
- **Nature / gravité :** friction, basse
- **Rencontré dans :** 27 (D, R, E), 28 (E), 31 (D, R, E). Constats 27-3, 28-8, 31-4.
- **Reproduction minimale :**
  ```
  any generate Point TrainId --dry-run
  # domain Point vs System.Drawing.Point; likewise Vector3, Container
  ```
- **Attendu :** les types des projets référencés classés avant les assemblies du framework, et l'échec répété dans un résumé final.
- **Constaté :** `✗ Point: several types answer to that name. … Name the one you mean in full.` Les autres types sont écrits et le lot se termine avec le code 1. Le refus n'apparaît qu'une fois, en haut de plusieurs pages de sortie.
- **Verdict :** confirmé. Cela correspond à la spec §3.2 et §7. `TypeLookup` ne privilégie les types source que pour le projet analysé, si bien qu'un type de domaine situé dans un projet référencé arrive à égalité avec le type de la BCL. Le contournement (le nom complet) figure dans le message, mais la page utilisateur ne mentionne jamais ce refus. Observation annexe (31) : quand la référence de projet ne se résolvait pas, `Container` a scaffoldé en silence `System.ComponentModel.Container`.

#### CLI-13 — Paramètres gardés par une regex : le TODO omet le motif, et passer à `StringMatching` demande de retyper à la main
<a id="cli-13"></a>
- **Nature / gravité :** friction, basse (moyenne en 37)
- **Rencontré dans :** 21 (R), 24 (R, E), 26 (D), 27 (R), 34 (R, E), 37 (D, R), 38 (D, R), 40 (R, E). Constats 21-6, 24-6, 26-7, 27-8, 34-7, 37-3, 37-4, 38-7, 40-5.
- **Reproduction minimale :**
  ```csharp
  // any generate CultureId, then replace the body:
  return Any.StringMatching("CH(0[1-9]|1[0-2])-C[0-9]{4}");
  ```
- **Attendu :** une modification d'une ligne, ou un TODO qui nomme le changement de type et cite le motif du `[GeneratedRegex]`.
- **Constaté :**
  - CS0029, puis CS1503, tant que le type de retour de la fabrique et `Func<AnyString, AnyString>` n'ont pas tous deux été retypés en `AnyPattern`. Pour les bases `.As(...)`, l'erreur est CS0266 vers `IAny<…>`.
  - La note cite `if (!match.Success)` ou `if (!Pattern().IsMatch(x))`, jamais le motif.
  - Cinq fois par domaine en 38. En 40, un rechercher-remplacer aveugle casse une seconde surcharge de chaîne.
- **Verdict :** confirmé ou partiel partout.
  - Ne pas lire la regex est délibéré : la spec §5.3 dit que la plupart des regex de validation sortent du sous-ensemble pris en charge ; les lire est candidat pour la v1.1.
  - La page de la CLI documente exactement le changement de type en deux lignes, et la section vers laquelle renvoie le TODO y mène.
  - « Émettre `AnyPattern` » est réfuté par conception.
  - Deux améliorations restent compatibles avec la spec : citer le motif du `GeneratedRegex` dans le commentaire, et mentionner le changement de type dans le TODO lui-même.

#### CLI-14 — Une garde passant par un helper qui renvoie une valeur est silencieuse et rapportée comme entièrement inférée
<a id="cli-14"></a>
- **Nature / gravité :** friction (déposé comme défaut, haute), moyenne
- **Rencontré dans :** 22 (D, R, E), 21 (D, R, E — le helper `ThrowIfOutside`). Constats 22-1, 21-1.
- **Reproduction minimale :**
  ```csharp
  public SpatialPoint(double xMm) { XMm = Guard(xMm, nameof(xMm)); }
  private static double Guard(double v, string n) { if (Math.Abs(v) > 2000.0) throw new ArgumentOutOfRangeException(n); return v; }
  // any generate SpatialPoint
  ```
- **Attendu :** `to verify` et le TODO qui bloque la compilation, puisqu'un helper qui reçoit le paramètre décide s'il est accepté.
- **Constaté :** `xMm double Any.Double()` sans aucune marque, et « 1 of 1 parameters inferred ». Le fichier compile, et 100 tirages sur 100 lèvent une exception. Un helper void appelé pour son effet est marqué comme prévu.
- **Verdict :** partiel, reclassé en friction.
  - Documenté comme silencieux : page de la CLI, « What the tool still cannot see stays silent » ; spec §9 (un validateur qui renvoie une valeur ne se distingue pas d'une normalisation) ; l'exception d'ADR-0086 ne couvre que des bibliothèques nommées.
  - C'est la garde idiomatique d'un value object, et le résultat est un fichier qui semble terminé mais qui est cassé. C'est un candidat pour un retour de terrain au titre d'ADR-0085, par exemple pour lire un helper privé du même type dont le corps lève visiblement une exception.

#### CLI-15 — Des formes de garde absentes de l'ensemble fermé reconnu
<a id="cli-15"></a>
- **Nature / gravité :** manque / friction, basse à moyenne (selon la forme)
- **Rencontré dans :** 25, 26, 27, 29, 30, 31, 33, 34, 35. Constats 25-2, 26-1, 26-5, 27-6, 29-7, 30-4, 31-6, 33-3, 34-3, 35-5.
- **Reproduction, attendu et constaté pour chaque forme :**

| Forme | Contextes (profils) | Attendu | Constaté | Verdict |
|---|---|---|---|---|
| Bornes de TimeSpan : `ThrowIfLessThan(e, TimeSpan.Zero)`, `t < TimeSpan.FromSeconds(5)` | 26 (D, R, E) | `Any.TimeSpan().GreaterThanOrEqualTo(TimeSpan.Zero)` | `Any.TimeSpan() to verify`, même en gardes séparées | Partiel → manque (moyenne) : les opérandes `TimeSpan` ne sont pas des constantes C#. L'affirmation du constat sur les décimaux est fausse : les gardes décimales séparées sont lues. |
| `ThrowIfGreaterThan(text.Length, 500)` | 34 (D, R, E) | `.WithMaxLength(500)` | `Any.String().NonEmpty() to verify` | Manque confirmé (moyenne) : le helper n'est lu que si son premier argument est le paramètre lui-même. L'écriture `if (p.Length > N)` est lue. |
| `decimal.Round(v, 2) != v` | 29 (E), 30 (E) | `.WithScale(2)` | `to verify, guard, unread guards` | Manque confirmé (basse) : aucune ligne dans l'ensemble fermé. |
| `lockedAt == default` sur `DateTimeOffset` | 27 (R, E) | `.DifferentFrom(default)` | `Any.DateTimeOffset() to verify` | Friction confirmée (basse). |
| `IndexOfAnyExcept("ACGTN") >= 0` | 35 (R) | `.WithChars("ACGTN")` | `NonEmpty() to verify` | Friction confirmée (basse) : aucune inférence d'ensemble de caractères n'existe. |
| `double.IsNaN(x)`, `!(x > 0)`, `double.IsInfinity(x)` isolés | 25 (D, R, E), 31 (R), 33 (R), 40 | aucune contrainte (NaN et les infinis ne sont jamais tirés) / `.Positive()` | non lu | Confirmé / partiel : l'ensemble n'a qu'une ligne `!double.IsFinite`. |
| `quarter is { } q && !Enum.IsDefined(q)` sur `Q?` | 26 (D) | `.AsNullable() guard` | `to verify` | Partiel : exclu par la règle de `&&` (spec §5.3). La spec désigne ce coût du « valider ce qui est présent » comme délibéré. |

- **Verdict (global) :** chaque forme est hors de l'ensemble fermé de la spec §5.3 et est refusée bruyamment, donc aucune n'est un bug. Ajouter des lignes est un élargissement délibéré qui demande une décision (ADR-0082, ADR-0085). Les lignes TimeSpan et helper sur `.Length` sont les plus coûteuses.

#### CLI-16 — Le `NonEmpty()` par défaut écarte une chaîne vide légale, et la ligne est étiquetée `guard`
<a id="cli-16"></a>
- **Nature / gravité :** friction (déposé comme défaut), basse
- **Rencontré dans :** 35 (D, R, E). Constat 35-4.
- **Reproduction minimale :**
  ```csharp
  public Allele(string bases) { ArgumentNullException.ThrowIfNull(bases); if (bases.Length > 10_000) throw …; }
  ```
- **Attendu :** `Any.String().WithMaxLength(10000)`, ou un récapitulatif qui signale `NonEmpty()` comme une valeur par défaut.
- **Constaté :** `bases string Any.String().NonEmpty().WithMaxLength(10000) guard`, sans `to verify`. L'allèle vide, valide et courant, n'est jamais tiré.
- **Verdict :** partiel. `NonEmpty()` est la valeur par défaut spécifiée pour les chaînes (spec §5.2), et l'étiquette `guard` par ligne suit le §6. Le vrai problème d'ergonomie est qu'une étiquette par ligne mélange la valeur par défaut du moteur et ce que la garde a ajouté.

#### CLI-17 — La base valide d'un paramètre, `AnyValid…()`, est privée
<a id="cli-17"></a>
- **Nature / gravité :** friction, basse
- **Rencontré dans :** 21 (R), 24 (D), 29 (E), 30 (R), 40 (D). Constats 21-5, 24-7, 29-9, 30-7, 40-10.
- **Reproduction minimale :**
  ```csharp
  AnyExperimentalConditions.AnyValidTemperatureCelsius().Generate(); // CS0122
  ```
- **Attendu :** une base accessible, ou un idiome documenté.
- **Constaté :** les utilisateurs ont tiré l'agrégat entier pour en lire une propriété, reformulé l'intervalle, ou modifié la visibilité à la main (ce que `--force` annulerait).
- **Verdict :**
  - Confirmé en 21 ; partiel ailleurs.
  - Le caractère privé est spécifié (spec §4.2).
  - La classe est `partial`, donc un wrapper `internal static` d'une ligne dans un fichier voisin fonctionne, survit à `--force`, et a passé un test.
  - Cet usage du point d'extension documenté n'est montré nulle part.

#### CLI-18 — Les générateurs vont par défaut dans l'espace de noms de production du domaine
<a id="cli-18"></a>
- **Nature / gravité :** friction, basse
- **Rencontré dans :** 24 (R), 26 (R), 32 (D), 35 (R), 37 (E), 40 (E). Constats 24-9, 26-10, 32-9, 35-6, 37-10, 40-12.
- **Reproduction minimale :**
  ```
  cd Tests.Regular && any generate Animal --entry-point static:Arbitrary
  ```
- **Attendu :** l'espace de noms du projet de test, ou une indication vers `any.json`.
- **Constaté :** `namespace RoboticMilking.Domain;` dans les deux fichiers.
- **Verdict :**
  - Partiel partout. La valeur par défaut est délibérée (ADR-0062, spec D8) : les points d'appel n'ont besoin d'aucun import supplémentaire, et le moteur ne connaît rien de MSBuild (ADR-0065).
  - Les surcharges sont documentées : `--namespace`, la clé `namespace` d'`any.json`, et `--entry-point-namespace`, qui ne déplace que la racine (26-10).
  - Le seul point valable : le récapitulatif ne nomme jamais l'espace de noms dans lequel il a écrit.

#### CLI-19 — Paramètres nullables : une base jamais nulle, une épingle à null qui demande un cast, un « parfois null » écrit à la main
<a id="cli-19"></a>
- **Nature / gravité :** friction, basse (rapportée moyenne en 23)
- **Rencontré dans :** 23 (D, R, E), 26 (R, E). Constats 23-3, 26-11.
- **Reproduction minimale :**
  ```csharp
  Any.Animal().WithLastMilkedAt(null);               // CS0121
  Any.Animal().WithLastMilkedAt(t => t.OrNull());     // CS0029 / CS1662
  ```
- **Attendu :** une épingle à null lisible (par exemple `WithoutLastMilkedAt()`) et un « parfois null » généré. En 23, une indication dans le TODO que les deux paramètres sont liés en tout-ou-rien.
- **Constaté :** `(DateTimeOffset?)null` est nécessaire. « Parfois null » demande une surcharge écrite à la main dans un fichier partiel. En 23, la valeur par défaut tire toujours les deux parents.
- **Verdict :**
  - Confirmé en 26 ; partiel en 23.
  - Tout cela est délibéré et documenté : ADR-0064 ne tire jamais null ; page de la CLI, « Sometimes null » ; `.WithSireId((StudbookId?)null).WithDamId((StudbookId?)null)` fonctionne sans modification.
  - En 23, la valeur par défaut passe le constructeur ; « non enregistrable » est une règle de Pedigree.

#### CLI-20 — Un générateur préparé ne peut pas être passé à `With…`, et le retour se limite à une erreur brute du compilateur
<a id="cli-20"></a>
- **Nature / gravité :** friction, basse
- **Rencontré dans :** 21 (D, R), 25 (E). Constats 21-3, 25-11.
- **Reproduction minimale :**
  ```csharp
  Arbitrary.Culture().WithConditions(g); // g is an AnyExperimentalConditions → CS1503
  Any.PlaybackSession().WithLadder(_ => Ladder); // compiles, silently replaces the base
  ```
- **Attendu :** une indication dans la doc XML générée ou un diagnostic qui oriente vers la forme `Func<AnyX, AnyX>`, et une façon naturelle de réutiliser une recette nommée.
- **Constaté :** seulement le « cannot convert » du compilateur. Le contournement `_ => recipe` est précisément le remplacement de base qu'ADR-0109 visait à empêcher, sans aucun avertissement.
- **Verdict :** confirmé. Par conception (ADR-0109). La voie `Func` n'est documentée que sur la page de la CLI.

#### CLI-21 — Les scaffolds à un seul paramètre déclenchent CA1822 sur leurs méthodes `With…`
<a id="cli-21"></a>
- **Nature / gravité :** friction, basse
- **Rencontré dans :** 21 (E), 29 (E), 32 (E). Constats 21-7, 29-8, 32-8.
- **Reproduction minimale :**
  ```
  any generate GrowthRate
  dotnet build -p:ErrorLog=out.sarif
  ```
- **Attendu :** aucun bruit d'analyseur sur du code fraîchement scaffoldé.
- **Constaté :** deux notes CA1822 par type, sur `WithX(value)` comme sur `WithX(Func)`.
- **Verdict :** confirmé. Niveau note seulement, donc la console affiche 0 avertissement. L'émetteur n'a ni cas particulier pour un seul paramètre ni suppression.

#### CLI-22 — Le scaffolder copie la valeur d'une constante nommée au lieu de la référencer
<a id="cli-22"></a>
- **Nature / gravité :** friction, basse
- **Rencontré dans :** 38 (R). Constat 38-6.
- **Reproduction minimale :**
  ```csharp
  ThrowIfGreaterThan(projectId, MaxProjectId); // public const int MaxProjectId = 999_999
  ```
- **Attendu :** `.LessThanOrEqualTo(CreditSerial.MaxProjectId)`.
- **Constaté :** `.LessThanOrEqualTo(999999)`. Comme le fichier n'est scaffoldé qu'une fois (D1), la copie devient obsolète quand le domaine modifie la constante.
- **Verdict :** confirmé. Le repli sur la valeur reste nécessaire pour les constantes inaccessibles.

#### CLI-23 — Un agrégat mutable épinglé est partagé entre les appels à `Generate()`
<a id="cli-23"></a>
- **Nature / gravité :** friction, basse
- **Rencontré dans :** 25 (R). Constat 25-8.
- **Reproduction minimale :**
  ```csharp
  AnyPlaybackSession g = Arbitrary.PlaybackSession().WithBuffer(new PlaybackBuffer(10, 60));
  PlaybackSession a = g.Generate(), b = g.Generate();
  a.Buffer.Drain(5); // b.Buffer changes too
  ```
- **Attendu :** une note dans le commentaire de documentation de l'épingle ou sur la page de la CLI.
- **Constaté :** un aliasing silencieux, parce que `FixedValue<T>` renvoie la même instance.
- **Verdict :** confirmé. Le comportement est voulu (spec §4.2). L'aliasing n'est pas documenté.

#### CLI-24 — Accepter une recette `to verify` laisse un commentaire obsolète de cinq lignes par paramètre
<a id="cli-24"></a>
- **Nature / gravité :** friction, basse
- **Rencontré dans :** 25 (D), 33 (D). Constats 25-6, 33-7.
- **Reproduction minimale :**
  ```
  any generate … (9 types)
  ```
  Cela produit de 17 à 21 blocs, chacun composé d'un commentaire de 5 lignes et de `_ = TODO_verify_…;`.
- **Attendu :** un marqueur plus court, ou un moyen d'accepter les recettes en masse.
- **Constaté :** les utilisateurs ont scripté la suppression.
- **Verdict :** partiel. Seule la ligne `_ =` doit disparaître, comme le disent le commentaire, la page de la CLI et la spec §5.6. Le récapitulatif, `--format json` et les erreurs de compilation listent déjà toutes les gardes en suspens. Ce qui reste, c'est le commentaire résiduel.

#### CLI-25 — `--dry-run` affiche un ✓ avant « Nothing written »
<a id="cli-25"></a>
- **Nature / gravité :** friction, basse
- **Rencontré dans :** 27 (E). Constat 27-10.
- **Reproduction minimale :**
  ```
  any generate Interlocking --dry-run
  ```
- **Attendu :** une formulation du type « would write AnyInterlocking.cs ».
- **Constaté :** `✓ AnyInterlocking.cs — 4 of 4 parameters inferred, 3 to verify. … Nothing written: --dry-run.`
- **Verdict :** partiel. La coche rapporte le résultat de l'inférence, et la sortie suit la spec §6.

#### CLI-26 — La recette des éléments d'un paramètre collection ne peut pas être affinée au travers de la lambda
<a id="cli-26"></a>
- **Nature / gravité :** friction, basse
- **Rencontré dans :** 29 (D, R). Constat 29-4.
- **Reproduction minimale :**
  ```csharp
  Any.Stake().WithCombination(c => c.WithHorses(l => /* elements Between(1, fieldSize)? */ l))
  ```
- **Attendu :** l'affinage des éléments, ou un exemple de documentation pour ce cas.
- **Constaté :** il a fallu une surcharge dans un fichier partiel.
- **Verdict :** partiel. Documenté (« When the lambda is not enough », ADR-0109), avec exactement cette voie. Le souhait restant est un exemple de règle d'élément qui dépend d'un autre agrégat.

### Analyseurs

#### AN-1 — Les diagnostics JD de niveau Info n'apparaissent jamais dans la sortie de `dotnet build`
<a id="an-1"></a>
- **Nature / gravité :** friction, basse
- **Rencontré dans :** 21 (E), 26 (E), 33 (E), 34 (D, E), 36 (E), 40 (D). Constats 21-8, 26-14, 33-10, 34-6, 36-9, 40-11. (38-8 a été réfuté pour la même raison.)
- **Reproduction minimale :**
  ```csharp
  Any.String().NotBlank().Generate();
  // dotnet build
  ```
- **Attendu :** une indication visible, ou une documentation qui explique comment la faire apparaître.
- **Constaté :** 0 avertissement dans la console. JD030 ou JD031 n'apparaissent que dans un journal SARIF ou dans un IDE.
- **Verdict :** partiel. C'est la façon dont MSBuild traite le niveau Info, et les sévérités par défaut sont délibérées.
  - `analyzers/README.md` (lignes 19 à 30) et `packages/justdummies.en.md` (lignes 51 à 60) donnent déjà le remède par `.editorconfig`.
  - Ce qui reste, c'est la découvrabilité : le readme du nupkg empaqueté, la page de la CLI (ligne 273, qui cite JD030) et les pages de JD030/JD031 ne mentionnent pas ce remède et n'y renvoient pas.
  - Observation annexe (36) : le message de JD030 dit « 1 to 1025 characters » alors que le README dit de 0 à 1024.

#### AN-2 — JD009 ne voit pas un tirage dans un champ statique fait au travers d'un générateur scaffoldé
<a id="an-2"></a>
- **Nature / gravité :** friction, basse
- **Rencontré dans :** 26 (R). Constat 26-9.
- **Reproduction minimale :**
  ```csharp
  [assembly: Reproducible]
  private static readonly MilkingSettings S = Arbitrary.MilkingSettings().WithCleaningTime(TimeSpan.FromSeconds(10)).Generate(); // nothing
  private static readonly int C = Any.Int32().Generate(); // JD009
  ```
- **Attendu :** un avertissement, ou une mention documentée.
- **Constaté :** aucun diagnostic pour les points d'entrée scaffoldés.
- **Verdict :** partiel. Le constat citait JD007, mais la règle pertinente est JD009. Sa page décrit ce risque et documente en termes généraux que les générateurs « not written inline from Any » ne sont pas signalés. Elle ne nomme pas les générateurs scaffoldés, qui portent l'essentiel des tirages dans un projet `any`.

### Adaptateur xUnit

#### XU-1 — L'attribut de rejeu collé ne compile pas quand `[Reproducible]` est au niveau de l'assembly
<a id="xu-1"></a>
- **Nature / gravité :** friction, basse
- **Rencontré dans :** 28 (E). Constats 28-9 (ainsi que la réserve de 28-10).
- **Reproduction minimale :** `[assembly: Reproducible]` dans `AssemblyInfo.cs`, et un test en échec dans un fichier sans `using JustDummies.Xunit;`. Coller le `[Reproducible(Seed = N)]` rapporté.
- **Attendu :** un extrait qui compile tel quel une fois collé (nom pleinement qualifié), ou qui nomme l'espace de noms.
- **Constaté :** `error CS0246: The type or namespace name 'ReproducibleAttribute' could not be found`.
- **Verdict :** confirmé. `ReplaySnippet` affiche la forme nue, et le package n'ajoute aucun using global. Le quick-fix de l'IDE règle le problème en quelques secondes.

#### XU-2 — Un rapport d'échec donne la graine mais jamais les valeurs tirées
<a id="xu-2"></a>
- **Nature / gravité :** manque, basse
- **Rencontré dans :** 32 (E). Constat 32-7.
- **Reproduction minimale :** un `[Fact, Reproducible]` qui échoue pour certains tirages de `Any.Double().GreaterThanOrEqualTo(1.0).LessThan(2.0)`.
- **Attendu :** un moyen optionnel de voir ce qui a été tiré.
- **Constaté :** seulement la ligne de la graine.
- **Verdict :** confirmé. Aucun ADR ne tranche contre. Le constat selon lequel « un tirage de sonde a décalé la séquence » est le comportement attendu d'un flux séquentiel : journaliser la variable déjà tirée l'évite.

### Documentation

#### DOCS-1 — La documentation utilisateur ne liste pas les formes de garde que `any` lit, et ne dit pas que les conditions `||`/`&&` ne sont pas lues
<a id="docs-1"></a>
- **Nature / gravité :** défaut / manque, basse par élément (haute au total, car elle sous-tend [CLI-1](#cli-1))
- **Rencontré dans :** 23 (D, R), 25 (D, E), 28 (D, E), 29 (D, R, E), 31 (D), 33 (R), 36 (R), 39 (R), plus les volets documentation de 21-1, 24-1 et 32-1. Constats 23-4, 25-3, 28-2, 29-2, 31-5, 33-8, 36-7, 39-2.
- **Reproduction minimale :**
  ```
  grep -nE '\|\||&&' doc/handwritten/for-users/packages/justdummies-cli.en.md   # no match
  ```
- **Attendu :** un court tableau destiné aux utilisateurs, avec les formes lues (et ce que chacune devient) et celles qui ne le sont pas, ainsi que l'astuce consistant à découper une disjonction en `if` séparés.
- **Constaté :**
  - « What the last column means » donne un seul exemple positif (`quantity <= 0` → `.Positive()`) et ne liste que les formes qu'il ne voit pas.
  - Elle renvoie ensuite les lecteurs à la spec §9 (non-objectifs), alors que l'ensemble reconnu se trouve au §5.3.
  - Le TODO émis appelle la section « What the scaffolder reads, and what it cannot ».
- **Verdict :** confirmé dans les huit. Les règles (un seul paramètre, pas de `&&`/`||`, des opérandes constants, avant la première écriture, la table des helpers) n'existent que dans la spec mainteneur, au §5.3.

#### DOCS-2 — L'organisation suggérée par la page de la CLI masque la racine `Arbitrary` derrière son propre espace de noms
<a id="docs-2"></a>
- **Nature / gravité :** défaut, moyenne
- **Rencontré dans :** 25 (R). Constat 25-4.
- **Reproduction minimale :**
  ```
  any.json: { "entryPoint": "static:Arbitrary", "namespace": "AdaptiveVideo.Tests.Regular.Arbitrary" }
  ```
  Puis, dans un test de `AdaptiveVideo.Tests.Regular` : `Arbitrary.Bitrate().Generate()`. L'exemple de la page elle-même est `--entry-point-namespace Shop.Tests.Arbitrary`.
- **Attendu :** `Arbitrary.Bitrate()` se résout, ou la documentation et l'outil détournent de cette organisation.
- **Constaté :** `error CS0234: The type or namespace name 'Bitrate' does not exist in the namespace '…Arbitrary'`, et l'outil ne donne aucun avertissement.
- **Verdict :** confirmé. L'organisation apparaît aux lignes 187 et 559 de la page de la CLI, et la jumelle française a les deux mêmes. La recherche de noms de C# trouve d'abord l'espace de noms enfant. L'outil a déjà un avertissement de masquage et un refus des noms réservés, mais aucun des deux ne couvre ce cas.

#### DOCS-3 — L'étendue par défaut des tirages non contraints de `DateOnly`, `TimeOnly` et `DateTimeOffset` n'est pas documentée
<a id="docs-3"></a>
- **Nature / gravité :** manque / friction, basse (rapportée moyenne en 30 et 38)
- **Rencontré dans :** 23 (D, R), 26 (D), 30 (D, R, E), 37 (D), 38 (D). Constats 23-6, 26-6, 30-6, 37-9, 38-5.
- **Reproduction minimale :**
  ```csharp
  // 100,000 draws
  Any.DateOnly().Generate().AddYears(6);
  ```
- **Attendu :** une phrase par type qui indique l'étendue, plus une indication (sur la page ou dans le récapitulatif du scaffold) invitant à borner les instants du domaine avant de faire de l'arithmétique.
- **Constaté :**
  - `dates-and-times.en.md:89` indique `MinValue..MaxValue` pour `DateTime` uniquement.
  - Taux de dépassement mesurés : `AddYears(6)` a échoué sur 68 tirages sur 100 000, `AddMonths(120)` sur 104 sur 100 000.
  - Le scaffold émet un `Any.DateOnly()` / `Any.DateTimeOffset()` nu, sans indication.
- **Verdict :**
  - Confirmé en 30 et 37 ; partiel en 23, 26 et 38.
  - La phrase sur `DateTime` existe déjà.
  - En 26 et 38, le risque rapporté est exagéré : `AddHours(1)` n'a jamais échoué sur 1 000 000 de tirages. Seuls les décalages plus grands posent problème.

#### DOCS-4 — La liste des contraintes « déclarées une seule fois » omet `WithChars` et `WithCount`
<a id="docs-4"></a>
- **Nature / gravité :** défaut / friction, basse
- **Rencontré dans :** 35 (E), 31 (D). Constats 35-2, 31-11.
- **Reproduction minimale :**
  ```csharp
  Func<AnyString, AnyString> refine = b => b.WithChars("AC");
  refine(Any.String().WithChars("ACGT")); // throws ConflictingAnyConstraintException
  ```
- **Attendu :** `WithChars` (ainsi que les familles d'alphabets, la casse et préfixe/suffixe) et `WithCount` listés avec les autres contraintes déclarées une seule fois.
- **Constaté :** la liste de `justdummies-cli.en.md:343-346` ne cite que `OneOf`, `WithLength`, `MultipleOf` et `WithScale`. La page de JD032 dit bien que `WithCount` se déclare une seule fois.
- **Verdict :**
  - Confirmé en 35.
  - Partiel en 31 : un `WithCount(3)` d'affinage sur une base `WithCountBetween(2, 6)` restreint bien, et c'est documenté. Le manque, c'est la liste incomplète.
  - La correction de la documentation ne devrait pas promettre de restriction à la manière d'`Except` pour les alphabets, car il n'en existe pas ([CORE-8](#core-8)).

#### DOCS-5 — Aucune indication sur les valeurs tirées qui n'atteignent l'assertion que comme poignées d'identité ou comme échos
<a id="docs-5"></a>
- **Nature / gravité :** manque, basse
- **Rencontré dans :** 27 (E), 34 (D), 35 (D), 38 (R, E). Constats 27-9, 34-9, 35-8, 38-3.
- **Reproduction minimale :**
  ```csharp
  Assert.Same(anyBuyer, ledger.HolderOf(unit));
  Assert.Equal([anyJunction.Junction, anyJunction.Main], routeLock.RemainingSections);
  ```
- **Attendu :** les guides disent si une identité tirée, utilisée comme poignée ou renvoyée en écho dans la sortie, peut apparaître dans une assertion.
- **Constaté :** seulement la règle générale (« If an anyXxxx reaches your assertion, it is not a dummy », getting-started:248).
- **Verdict :**
  - Confirmé en 27 et 34.
  - Partiel en 35 et 38 : le critère de core-concepts (« whether the outcome depends on it ») tranche implicitement le cas. En 35, le numéro d'accession renvoyé en écho n'est pas un dummy selon cette règle. En 38, le classement est réellement ambigu.
  - Aucune page ne nomme le cas.

#### DOCS-6 — Aucune indication sur la façon de borner un tirage par une valeur calculée à partir d'autres tirages
<a id="docs-6"></a>
- **Nature / gravité :** manque, basse
- **Rencontré dans :** 21 (D). Constat 21-9.
- **Reproduction minimale :**
  ```csharp
  WithCellsPerMillilitre(c => c.GreaterThanOrEqualTo(1).LessThan(capacity)) // capacity from drawn conditions
  ```
- **Attendu :** la page de composition ou core-concepts montre le motif et dit où placer une borne dépendante.
- **Constaté :** aucune page ne le montre.
- **Verdict :** partiel. La section « When a guard ties two parameters » de la page de la CLI et design-principles disent déjà où placer une règle dépendante. Ce qui manque : savoir si une borne calculée au point d'appel est idiomatique, et un lien croisé depuis les pages de composition.

#### DOCS-7 — La distribution des tirages à virgule flottante à l'intérieur d'un intervalle n'est pas indiquée
<a id="docs-7"></a>
- **Nature / gravité :** manque, basse
- **Rencontré dans :** 21 (D). Constat 21-10.
- **Reproduction minimale :** `Any.Double().Positive().LessThanOrEqualTo(50)` — jusqu'à quel point peut-il s'approcher de 0 ?
- **Attendu :** la page des nombres indique la borne basse, ou que les flottants sont uniformes en valeur dans l'intervalle tronqué.
- **Constaté :** seule la magnitude haute est décrite.
- **Verdict :** partiel. Le risque d'un 1e-300 est réfuté. Les tirages sont uniformes en valeur : le minimum sur 2 000 000 de tirages était 3,55e-05, et aucun n'est tombé sous 1e-6. Le remède consiste à déclarer la borne.

#### DOCS-8 — Le guide de démarrage ne mentionne jamais `any`
<a id="docs-8"></a>
- **Nature / gravité :** friction, basse
- **Rencontré dans :** 39 (D). Constat 39-8.
- **Reproduction minimale :** partir de `guides/getting-started.en.md` et essayer de scaffolder un générateur.
- **Attendu :** un chemin court entre le guide de démarrage et un premier scaffold.
- **Constaté :** aucun lien. La page de la CLI pèse 28,7 Ko (le constat disait ~36 Ko).
- **Verdict :** partiel. Les 40 premières lignes de la page donnent déjà l'installation et une première exécution. Le README utilisateur renvoie à la CLI en étape 3. L'absence de lien depuis le guide de démarrage est réelle.

---

## 4. Ce qui fonctionne bien

Éloges vérifiés, dédoublonnés :

- **Les lignes `TODO_verify` qui bloquent la compilation forment une liste de travail honnête et précise.** Les 20 contextes, tous profils : 21-11, 22-7, 23-8, 24-11, 25-12, 26-16, 27-11, 28-11, 29-11, 30-8, 31-12, 32-11, 33-11, 34-10, 34-11, 35-9, 36-10, 37-11, 38-10, 39-9, 40-13.
  - Chaque garde dont l'outil ne peut pas se porter garant devient une erreur CS0103 sur la fabrique exacte.
  - Chacune s'accompagne d'un commentaire qui cite la garde, son fichier et sa ligne, plus un lien vers la documentation.
  - Les gardes lisibles sont correctement inférées : `ThrowIfNullOrWhiteSpace` → `NotBlank()`, longueur → `WithMaxLength`, `Guid.Empty` → `NonEmpty()`, et les helpers d'intervalle ThrowIf.
  - Aucune session n'a livré un générateur invalide en silence.
  - Réserves : seule la première garde de chaque paramètre est nommée ([CLI-3](#cli-3)) ; une dépendance `AnyX` manquante (CS0246) masque les erreurs TODO tant que cette dépendance n'est pas scaffoldée.
- **Une seule commande scaffolde tout un graphe qui se compose par lui-même.** 22-7, 24-10, 26-16, 30-8, 33-12, 34-10, 34-12, 39-10.
  - Les paramètres composés sont tirés par leurs propres générateurs (`new AnyPorosity()`, `Any.ListOf(new AnyRockCell())`), si bien que chaque invariant a un seul foyer.
  - `--entry-point any` (`extension(Any)` de C# 14) et `static:Arbitrary` se lisent tous deux comme les générateurs intégrés.
  - Réserve : seul `static:Arbitrary` produit une racine partielle partagée (39-10).
- **Les paires `With…` épingler/affiner et les lambdas imbriquées se lisent comme le domaine.** 21-12, 22-8, 24-10, 25-13, 26-17, 27-12, 30-10, 31-14, 33-13, 34-13, 35-10, 36-11, 37-13, 38-11, 39-11, 39-13, 40-14 ; 23-9 et 29-12 tiennent avec des réserves.
  - Les valeurs qui portent le test restent littérales, et tout le reste reste tiré.
  - Un `Between` d'affinage restreint la base scaffoldée. Une contradiction échoue immédiatement avec un message qui nomme les deux côtés.
  - Les affinages sont réutilisables sous forme de `Func<AnyX, AnyX>` ou de groupes de méthodes. `IAny<T>` est covariant. `Any.Combine` compose des agrégats.
  - Réserve : l'affinage imbriqué repart de la base interne ([CLI-9](#cli-9)).
- **`[Reproducible]` rapporte une graine prête à coller et rejoue à l'identique, sans configuration.** 21-13, 22-10, 23-12, 24-13, 25-15, 26-15, 28-10, 29-14, 30-9, 31-15, 32-10, 33-14, 34-14, 36-12, 37-12, 39-12, 40-15.
  - Un attribut d'assembly ou de classe suffit. Chaque cas a sa propre graine. Les exécutions vertes restent silencieuses.
  - Le rejeu tient au travers de graphes scaffoldés imbriqués et de code `Generate()`/`Combine` écrit à la main (36-12, 40-15).
  - Les dummies tirés ont révélé de vraies dépendances cachées, comme le promettent les fondamentaux :
    - le plafond de taux de croissance de 5 h⁻¹ (21) ;
    - une bizarrerie de phase avec un seul trayon traitable (26) ;
    - un vecteur d'écart épinglé parallèle à une vitesse tirée (28) ;
    - un solveur qui diverge pour une accélération ≥ ~1,87, que `SolverSettings` accepte (32) ;
    - une erreur de stabilité dans la mise en place d'un test (39) ;
    - plusieurs erreurs d'assertion et d'arithmétique.
  - Observation annexe tirée de 26-15 : le test commité par l'expert, `Brushing_a_first_teat_starts_the_preparation`, porte l'hypothèse non contrainte d'un seul trayon et échoue environ une exécution sur 4.
- **La démarche de changement de type de la page de la CLI est exacte.** 22-9, 23-10, 24-12, 28-12, 31-13, 32-12, 34-15, 38-12. « When the replacement is another kind of generator » nomme exactement les deux lignes à retyper pour `AnyPattern`, et le cas `IAny<T>` pour `.As(...)`. La recette `StringMatching(...).As(body => body + checkDigit)` a couvert ISO 6346 en une ligne. La documentation avait aussi prévenu à l'avance des tirages dans les constructeurs signalés par JD007/JD009.
- **Les fichiers partiels compagnons survivent à `--force`.** 23-11, 36-11. Les recettes écrites à la main comme `AsFounder` et `WithParents` passent par le constructeur privé et ne sont pas touchées par un nouveau scaffold. Réserve : les modifications à l'intérieur des corps `AnyValid…()` scaffoldés sont annulées.
- **La distinction par construction.** 23-13, 24-14, 29-13, 30-11, 32-13.
  - `SetOf(...).WithCount(n)`, `ArrayOf(...).Distinct()` sur des value objects, et `SetOf(...).As(sort)` pour des collections strictement croissantes fonctionnent sans nouvelle tentative dans le code utilisateur.
  - Les nouveaux tirages internes de la bibliothèque sont bornés et échouent explicitement quand ils sont épuisés.
  - `Enum.DifferentFrom` et `Distinct(comparer)` se comportent comme documenté.
  - Réserve (23-13) : JD028 reste muet sur les générateurs scaffoldés quelle que soit leur égalité, donc son silence ne prouve pas que `Equals` a été vérifié.
- **Le vocabulaire des contraintes se devine.** 28-12, 30-11. `AfterOrEqualTo`, `WithOffset`, `Boolean().False()`, `WithCountBetween`, `NotBlank()`, `WithScale(2)` et `Alpha().InUpperCase().WithLength(3)` ont compilé du premier coup à partir des pages de guide.
- **La documentation sur les paramètres liés et sur « Arbitrary ne veut pas dire dummy » a répondu à de vraies questions.** 25-14. Réserve : les « bases disjointes » étaient une adaptation du développeur lui-même, pas une forme documentée.

---

## 5. Constats réfutés

- **22-6** — « Un `.As` vers une collection typée par une interface demande un type de retour de lambda explicite. » Réfuté : `IAny<out T>` est covariant, et la reproduction exacte compile et s'exécute. L'incompatibilité venait du type de la fabrique `AnyList<T>` scaffoldée, et son changement de type est documenté.
- **26-4** — « La ligne de la garde non lue pointe vers une garde lisible au lieu du helper qui a causé la marque. » Réfuté : `ThrowIfLessThan` sur un `TimeSpan` n'est lui-même pas lu (voir [CLI-15](#cli-15)), et la marque du helper statique ne s'étend pas à ce paramètre. La ligne citée est correcte.
- **38-8** — « Les diagnostics JD de niveau Info sont invisibles, et rien dans la documentation ne le dit. » Réfuté : `analyzers/README.md` (lignes 19 à 29, avec sa jumelle française) et `packages/justdummies.en.md` (lignes 51 à 59) l'indiquent déjà et donnent le remède par `.editorconfig`. Ils avaient été ajoutés avant l'essai.
