# Analyseurs JustDummies

🌍 **Langues :**  
🇬🇧 [English](./README.md) | 🇫🇷 Français (ce fichier)

Le package `JustDummies` embarque 35 règles Roslyn (`JD001`–`JD035`), sous
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

**Les diagnostics Info n'apparaissent pas dans la sortie de `dotnet build`.** MSBuild les garde hors de la console :
un IDE les affiche, un journal d'erreurs SARIF aussi, mais dans un terminal ou un journal de CI une règle comme
[JD030](JD030.fr.md) ne dit rien, et le remède qu'elle enseigne ne vous parvient jamais. Les sévérités par défaut
sont voulues et ne changent pas ; pour voir une règle Info dans la sortie du build, relevez-la dans `.editorconfig` :

```ini
# voir une règle Info dans la sortie du build
dotnet_diagnostic.JD030.severity = warning

# ou toutes les règles de la catégorie Constraints
dotnet_analyzer_diagnostic.category-JustDummies.Constraints.severity = warning
```

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
| [JD015 StringConstraintsAdmitNoValue](JD015.fr.md) | 🟠 Avertissement | on | Les contraintes constantes d'une chaîne `AnyString` n'admettent aucune valeur — deux longueurs déclarées qui se contredisent, des caractères déclarés qui ne laissent rien à tirer à une chaîne façonnée, une longueur déclarée dans laquelle la forme ne peut pas tenir, ou des contraintes qui, ensemble, n'admettent aucune valeur d'un value set. |
| [JD016 CollectionConstraintsAdmitNoValue](JD016.fr.md) | 🟠 Avertissement | on | Les contraintes de cardinal d'une chaîne de collection ne peuvent pas toutes tenir, ou elle réclame plus d'éléments distincts que son générateur d'éléments ne peut en produire. |
| [JD017 EnumUniverseViolation](JD017.fr.md) | 🟠 Avertissement | on | Une contrainte d'enum nomme une valeur que le type ne définit pas — une valeur numérique non déclarée, ou une exclusion qui vide l'univers. |
| [JD023 ScalarChainAdmitsNoValue](JD023.fr.md) | 🟠 Avertissement | on | Les contraintes constantes d'une chaîne entière réduisent le domaine à rien — bornes, treillis ou liste d'autorisation. |
| [JD024 ConstraintWithNoEffect](JD024.fr.md) | 🔵 Info | on | Une contrainte ne rétrécit rien : exclusion d'une valeur que le domaine ne pouvait pas produire, ou borne déjà impliquée. La seule famille de contraintes que l'exécution ne signale jamais. |
| [JD025 DuplicatePoolValue](JD025.fr.md) | 🟠 Avertissement | on | La même constante figure deux fois dans un réservoir — un ensemble de valeurs `OneOf` ou `ElementOf`, ou un réservoir de caractères `WithChars` ; les doublons sont éliminés, donc le réservoir est plus petit d'une valeur qu'il n'y paraît et le doublon ne pondère rien. |
| [JD026 EmptyRelativeUri](JD026.fr.md) | 🟠 Avertissement | on | Une URI relative à zéro segment, sans requête, fragment ni racine est la référence vide — la seule chaîne dont l'échec atterrit au moment de l'act plutôt que sur la ligne d'arrange. |
| [JD029 PooledValueNeverDraws](JD029.fr.md) | 🔵 Info | on | Une valeur écrite dans un value set de chaîne ou numérique qu'une contrainte de la même chaîne refuse : aucun tirage ne peut la rendre. Le dual de JD024, et elle ne voit que ce qui est écrit à l'appel. |
| [JD030 UndeclaredStringLength](JD030.fr.md) | 🔵 Info | on | Une chaîne `Any.String()` qui ne déclare aucune longueur : elle tire toute l'étendue par défaut — 0 à 1024 caractères. Nomme le remède là où vous pouvez agir. |
| [JD031 PairedBoundsHaveARangeForm](JD031.fr.md) | 🔵 Info | on | Une chaîne déclare séparément les deux bornes inclusives d'un intervalle, alors que le même générateur nomme cet intervalle en un seul appel. Rien n'est faux — elle comble un manque de découvrabilité. Paires inclusives uniquement : une paire stricte n'a pas de forme intervalle exacte. |
| [JD032 BoundDeclaredTwice](JD032.fr.md) | 🟠 Warning | on | Une chaîne déclare deux fois la même borne ; les bornes se replient silencieusement, donc seule la plus serrée survit et l'appel le plus lâche est mort. Appariée sur le nom, donc les alias restent muets, et une borne détenue sous un nom n'est jamais suivie. |
| [JD033 AnchoredLiteralOutsideCharacterFamily](JD033.fr.md) | 🔵 Info | on | Un littéral ancré porte un caractère que la famille, la soustraction ou la casse déclarée ne peut pas tirer. Légal et délibéré dans un format à préfixe fixe : la règle rapporte donc la conséquence — ce caractère n'apparaît que là où vous l'avez écrit. |
| [JD034 ConstraintAlreadyDefined](JD034.fr.md) | 🟠 Avertissement | on | Une chaîne redéfinit une contrainte qui ne peut être déclarée qu'une seule fois (longueur exacte, préfixe ou suffixe d'une chaîne, famille de caractères, casse, ensemble de valeurs, multiple, échelle, granularité, décalage horaire, nombre exact d'éléments d'une collection, schéma, hôte, port, informations utilisateur ou chemin d'une URI), ou combine `WithChars` avec une contrainte de casse. JD034 signale la seconde déclaration incompatible ; une répétition identique relève de JD035. |
| [JD035 ConstraintRepeated](JD035.fr.md) | 🟠 Avertissement | on | Une chaîne déclare de nouveau une contrainte exactement comme elle l'a déjà déclarée — une contrainte à déclaration unique avec les mêmes arguments, ou une exclusion, une soustraction, un fragment de chaîne ou un interrupteur écrits deux fois, y compris une valeur listée deux fois dans un même `Except`. La bibliothèque ignore la seconde déclaration, qui ne change donc rien ; les bornes restent à JD032. |

## Composition

Ces règles concernent l'assemblage de générateurs en générateurs plus gros — les opérandes de `Combine`, et le contrat d'élément sur lequel s'appuie un générateur de collection. Leur point commun : rien ne va de travers. Le générateur composé se construit, tire et rend une valeur. Ce n'est simplement pas la valeur que le site d'appel décrit.

| Règle | Sévérité | Défaut | Description |
|-------|----------|--------|-------------|
| [JD027 UnusedCombineOperand](JD027.fr.md) | 🟠 Avertissement | on | Un opérande de `Combine` est tiré puis jeté parce que le composeur ne lit jamais son paramètre. Nommer le paramètre `_` pour dire que le tirage est délibéré. |
| [JD028 InertDistinctness](JD028.fr.md) | 🟠 Avertissement | on | La distinction est déclarée sur un type d'élément sans égalité de valeur : elle est satisfaite par construction et la collection peut quand même contenir deux fois la même valeur. |

## Un test qui vérifie un refus

Chaque règle signale ce qu'elle voit, code de test compris : aucune règle ne cherche à deviner quels appels un
test écrit exprès. Un test dont le sujet est le refus lui-même, par exemple un test qui vérifie que des bornes
transposées sont rejetées, fait taire la règle là où c'est voulu, avec un `[SuppressMessage]` justifié qui
nomme la règle par le [catalogue](../packages/justdummies-diagnosticcatalog.fr.md) :

<!-- jd:declarations -->
```csharp
public sealed class BoundsTests {

    [Fact]
    [SuppressMessage(JustDummiesRule.JD014.Category, JustDummiesRule.JD014.Id, Justification = "Le refus EST le sujet : le test vérifie que des bornes transposées sont rejetées.")]
    public void Transposed_bounds_are_rejected() {
        Assert.Throws<ArgumentException>(() => Any.Int32().Between(10, 5));
    }

}
```

Partout ailleurs, le même diagnostic signale une erreur : la chaîne lève une exception à chaque exécution.

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
