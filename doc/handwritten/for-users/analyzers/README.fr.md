# Analyseurs JustDummies

🌍 **Langues :**  
🇬🇧 [English](./README.md) | 🇫🇷 Français (ce fichier)

Le package `JustDummies` embarque 30 règles Roslyn (`JD001`–`JD030`), sous
`analyzers/dotnet/cs`. Tout projet qui référence le package les récupère automatiquement, sans
installation supplémentaire. Elles s'exécutent pendant la compilation et transforment en
diagnostics de build des erreurs que l'exécution signalerait tard — ou jamais.

Elles existent parce que le système de types n'atteint pas l'endroit où ces erreurs vivent : un
générateur est une *recette* immuable et une valeur tirée n'en est pas une, pourtant les deux
satisfont les mêmes signatures ; une graine épinglée hors de sa portée compile quand même ; un
jeu de contraintes qui n'admet aucune valeur est une chaîne parfaitement bien typée. Chaque
règle ferme l'une de ces brèches.

Chaque règle a un identifiant stable. Les erreurs sont des défauts durs ; les avertissements
signalent des erreurs probables ; les règles info sont des conventions, et deux sont opt-in
(voir chaque page pour savoir comment les activer).

## Reproductibilité

Ces règles empêchent un corps de test asynchrone d'avaler silencieusement ses propres échecs.

| Règle | Sévérité | Défaut | Description |
|-------|----------|--------|-------------|
| [JD001 AsyncBodyPassedToReproducibly](JD001.fr.md) | 🔴 Erreur | on | Une lambda async est passée à `Any.Reproducibly(Action)` synchrone ; liée à une Action elle devient async void et ses échecs ne font jamais échouer le test. Utilisez `Any.ReproduciblyAsync` et faites `await`. |
| [JD002 DiscardedReproduciblyAsyncResult](JD002.fr.md) | 🔴 Erreur | on | Le `Task` retourné par `Any.ReproduciblyAsync` est jeté (instruction isolée ou `_ =`) ; les échecs du corps sont perdus. Faites `await`. |
| [JD003 AwaitableBodyPassedToReproducibly](JD003.fr.md) | 🔴 Erreur | on | Une lambda synchrone dont le corps abandonne une tâche, ou un groupe de méthodes `async void`, atteint `Any.Reproducibly` ; la portée retourne avant l'exécution des assertions, et `CS4014` ne se déclenche pas. |
| [JD004 DiscardedSeedingResult](JD004.fr.md) | 🔴 Erreur | on | La poignée retournée par `Any.UseSeed` est jetée, laissant la graine épinglée pour la suite — ou `Any.WithSeed` est appelé pour son effet, alors qu'il n'épingle rien. |
| [JD007 DrawOutsideThePinnedScope](JD007.fr.md) | 🟠 Avertissement | on | Une valeur est tirée pendant la construction d'une classe de test `[Reproducible]`, qu'xUnit exécute avant l'ouverture de la portée de graine ; la graine rapportée ne la rejoue pas. |
| [JD008 ArbitraryValueInTheoryData](JD008.fr.md) | 🟠 Avertissement | on | Le fournisseur de données d'une théorie tire une valeur à la découverte, avant tout épinglage ; tous les cas partagent cette unique valeur. |
| [JD009 DrawInStaticInitializer](JD009.fr.md) | 🟠 Avertissement | on | Un initialiseur statique tire une seule fois pour toute la suite, sous le premier test exécuté, rendant les tests dépendants de l'ordre et rejouables depuis aucune graine. |
| [JD010 ReproducibleOnNonTestMethod](JD010.fr.md) | 🟠 Avertissement | on | `[Reproducible]` sur une méthode qu'xUnit ne traite jamais comme un test ; il n'épingle rien, et ressemble exactement à la forme active. |
| [JD018 NestedReproducibilityScope](JD018.fr.md) | 🟠 Avertissement | on | Une portée de reproductibilité imbriquée dans une autre ; l'interne tire une graine neuve, donc la graine rapportée par l'externe ne rejoue rien. |
| [JD021 BlankReplaySnippet](JD021.fr.md) | 🟠 Avertissement | on | `Any.UseSeed` reçoit un snippet de rejeu vide, que la garde rejette — depuis un hook d'adaptateur, faisant échouer toute la suite. |
| [JD019 CommittedReplaySeed](JD019.fr.md) | 🔵 Info | opt-in | Une graine de rejeu constante est épinglée dans du code committé : le test cesse de varier d'une exécution à l'autre. |
| [JD020 SharedStaticAnyContext](JD020.fr.md) | 🔵 Info | on | Un `AnyContext` tenu dans un champ statique ; les tirages entrelacés ne rendent stables ni la séquence ni le multiensemble. |
| [JD022 ParallelDrawWithoutPerItemSeed](JD022.fr.md) | 🔵 Info | on | Une unité de travail parallèle tire sans sa propre portée de graine : les tirages s'entrelacent et l'exécution ne rejoue rien. |

## Usage

Un générateur est une *recette* immuable, et `Generate()` est la seule chose qui en matérialise une valeur. Ces règles ferment les deux façons dont cette distinction se perd silencieusement.

| Règle | Sévérité | Défaut | Description |
|-------|----------|--------|-------------|
| [JD005 GeneratorRenderedAsText](JD005.fr.md) | 🔴 Erreur | on | Un générateur est interpolé, concaténé ou passé à `ToString()` au lieu d'être généré ; aucun générateur ne surcharge `ToString()`, donc le texte obtenu est le nom de type du constructeur. |
| [JD006 DiscardedGeneratorResult](JD006.fr.md) | 🟠 Avertissement | on | Le générateur retourné par une contrainte est jeté en instruction isolée ; les générateurs étant immuables, l'invariant déclaré est silencieusement perdu. |
| [JD011 GeneratorWhereValueExpected](JD011.fr.md) | 🟠 Avertissement | opt-in | Un générateur atteint une position `object`, `dynamic` ou `params object[]` : c'est la recette qui est stockée, comparée ou assérée, pas la valeur. |
| [JD012 GeneratorPooledAsValue](JD012.fr.md) | 🟠 Avertissement | on | `Any.OneOf` reçoit des générateurs et infère un ensemble de recettes ; y tirer produit une recette plutôt qu'une valeur. |
| [JD013 HeldCollectionPassedToOneOf](JD013.fr.md) | 🟠 Avertissement | on | Une collection tenue passée à `Any.OneOf` lie `T` au type de la collection, formant un ensemble d'un seul élément ; `Any.ElementOf` tire parmi ses éléments. |

## Contraintes

Ces règles anticipent, à la compilation, le sous-ensemble des vérifications de contraintes de la bibliothèque qui est décidable depuis des constantes. Les vérifications d'exécution demeurent : elles couvrent tous les arguments que celles-ci ne peuvent pas voir.

| Règle | Sévérité | Défaut | Description |
|-------|----------|--------|-------------|
| [JD014 RejectedConstantArgument](JD014.fr.md) | 🟠 Avertissement | on | Un argument de contrainte est une constante que la garde du générateur refuse : l'appel lève à chaque exécution. |
| [JD015 StringConstraintsAdmitNoValue](JD015.fr.md) | 🟠 Avertissement | on | Les contraintes constantes d'une chaîne `AnyString` n'admettent aucune valeur — un fragment hors de la famille de caractères ou de la casse déclarée, ou des fragments qui ne peuvent pas tenir dans la longueur déclarée. |
| [JD016 CollectionConstraintsAdmitNoValue](JD016.fr.md) | 🟠 Avertissement | on | Les contraintes de cardinal d'une chaîne de collection ne peuvent pas toutes tenir, ou elle réclame plus d'éléments distincts que son générateur d'éléments ne peut en produire. |
| [JD017 EnumUniverseViolation](JD017.fr.md) | 🟠 Avertissement | on | Une contrainte d'enum sort des membres déclarés — une combinaison de drapeaux sans `AllowingCombinations()`, ou une exclusion qui vide l'univers. |
| [JD023 ScalarChainAdmitsNoValue](JD023.fr.md) | 🟠 Avertissement | on | Les contraintes constantes d'une chaîne entière réduisent le domaine à rien — bornes, treillis ou liste d'autorisation. |
| [JD024 ConstraintWithNoEffect](JD024.fr.md) | 🔵 Info | on | Une contrainte ne rétrécit rien : exclusion d'une valeur que le domaine ne pouvait pas produire, ou borne déjà impliquée. La seule famille de contraintes que l'exécution ne signale jamais. |
| [JD025 DuplicatePoolValue](JD025.fr.md) | 🟠 Avertissement | on | La même constante figure deux fois dans un réservoir ; les doublons sont écrasés, donc le réservoir est plus petit d'une valeur qu'il n'y paraît et le doublon ne pondère rien. |
| [JD026 EmptyRelativeUri](JD026.fr.md) | 🟠 Avertissement | on | Une URI relative à zéro segment, sans requête, fragment ni racine est la référence vide — la seule chaîne dont l'échec atterrit au moment de l'act plutôt que sur la ligne d'arrange. |
| [JD029 PooledValueNeverDraws](JD029.fr.md) | 🔵 Info | on | Une valeur écrite dans un value set de chaîne ou numérique qu'une contrainte de la même chaîne refuse : aucun tirage ne peut la rendre. Le dual de JD024, et elle ne voit que ce qui est écrit à l'appel. |
| [JD030 UndeclaredStringLength](JD030.fr.md) | 🔵 Info | on | Une chaîne `Any.String()` qui ne déclare aucune longueur : elle tire toute l'étendue par défaut — 0 à 1024 caractères. Nomme le remède là où vous pouvez agir. |

## Composition

Ces règles concernent l'assemblage de générateurs en générateurs plus gros — les opérandes de `Combine`, et le contrat d'élément sur lequel s'appuie un générateur de collection. Leur point commun : rien ne va de travers. Le générateur composé se construit, tire et rend une valeur. Ce n'est simplement pas la valeur que le site d'appel décrit.

| Règle | Sévérité | Défaut | Description |
|-------|----------|--------|-------------|
| [JD027 UnusedCombineOperand](JD027.fr.md) | 🟠 Avertissement | on | Un opérande de `Combine` est tiré puis jeté parce que le composeur ne lit jamais son paramètre. Nommer le paramètre `_` pour dire que le tirage est délibéré. |
| [JD028 InertDistinctness](JD028.fr.md) | 🟠 Avertissement | on | La distinction est déclarée sur un type d'élément sans égalité de valeur : elle est satisfaite par construction et la collection peut quand même contenir deux fois la même valeur. |

## Configuration

La sévérité de chaque règle se règle dans `.editorconfig`, par exemple :

```ini
# activer une règle opt-in
dotnet_diagnostic.JD011.severity = warning

# ou faire taire une règle dont vous ne voulez pas
dotnet_diagnostic.JD024.severity = none
```

Le jeu de règles est aussi déclaré dans
[`AnalyzerReleases.Shipped.md`](../../../../JustDummies.Analyzers/AnalyzerReleases.Shipped.md),
que les analyseurs de suivi de version de Roslyn confrontent aux descripteurs.
