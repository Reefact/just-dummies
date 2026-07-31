# Référence d'implémentation des ADR

🌍 🇬🇧 [English](adr-implementation-reference.md) · 🇫🇷 Français (ce fichier)

Ce document porte les détails d'implémentation extraits des Architecture Decision Records. Les ADR restent la source faisant autorité sur **ce qui a été décidé et pourquoi** ; cette référence décrit la réalisation technique actuelle et peut évoluer sans changer ces décisions.

> Récupéré depuis `Reefact/first-class-errors`, où cette référence servait deux produits. Seules les sections dont les décisions sont venues dans ce dépôt ont été gardées — les contrats du Request Binder, le catalogue GenDoc, les fabriques de `FirstClassErrors.Testing` et les surfaces documentaires du binder sont restés avec le code qu'ils décrivent. Les numéros d'ADR ont été remappés sur ceux de ce dépôt, selon l'[ADR-0045](../adr/0045-renumber-the-decision-base.fr.md).

## Plancher de compatibilité de l'analyseur

Décision liée : [ADR-0001](../adr/0001-lock-the-analyzer-roslyn-floor.fr.md).

L'analyseur est compilé contre le plancher Roslyn déclaré par `RoslynFloorVersion` dans `Directory.Build.props`. Le package conserve l'analyseur sous `analyzers/dotnet/cs/`.

En amont, la réalisation reposait sur quatre protections complémentaires. **Deux ne sont pas venues avec l'extraction**, si bien que le plancher est aujourd'hui *déclaré et épinglé* mais pas *prouvé* :

* ✅ la référence de package de l'analyseur est épinglée sur le plancher déclaré (`VersionOverride="$(RoslynFloorVersion)"` dans `JustDummies.Analyzers.csproj`, actuellement 4.8.0) ;
* ✅ Dependabot ignore les mises à jour automatiques des packages Roslyn qui définissent le plancher (`.github/dependabot.yml`) ;
* ❌ `RoslynFloorTests`, qui inspectait les métadonnées d'assembly et refusait les références `Microsoft.CodeAnalysis*` plus récentes — aucun test de ce genre n'existe dans ce dépôt ;
* ❌ le workflow de l'analyseur, qui construisait le vrai package NuGet puis compilait un exemple avec le SDK plancher, vérifiant à la fois le chargement et l'empaquetage — il n'y a pas d'`analyzers.yml` ici.

La conséquence est concrète : une montée transitive au-delà du plancher compilerait, passerait les tests et se packagerait au vert, pour ne se manifester que par un analyseur qui refuse de se charger chez un consommateur sur un compilateur plus ancien. Porter les deux protections manquantes est un travail à faire, pas une décision revenue en arrière — l'ADR-0001 tient tel qu'il est écrit.

Lors d'un changement de plancher, il faut mettre à jour la propriété centrale et l'exigence de compilateur documentée. Le changement architectural lui-même exige un nouvel ADR remplaçant l'ADR-0001.

## Vérification ADR des pull requests

Décision liée : [ADR-0002](../adr/0002-check-every-pull-request-against-the-adr-base.fr.md).

La vérification ADR est une procédure destinée au mainteneur et aux agents, documentée dans `AGENTS.md`, qui compare une modification aux décisions acceptées et détermine si elle enregistre, remplace ou contredit un ADR.

Le workflow GitHub actuel est déclenché manuellement. Il soutient donc la procédure, mais ne garantit pas à lui seul que chaque pull request a été vérifiée. Toute automatisation future de cette obligation relève de la documentation et de la configuration des workflows, pas de l'ADR-0004.

## Contrats de génération de JustDummies

Décisions liées : [ADR-0003](../adr/0003-host-dummies-as-a-standalone-package.fr.md), [ADR-0004](../adr/0004-gate-distinct-collections-by-cardinality-else-bounded-draw.fr.md), [ADR-0005](../adr/0005-cap-any-combine-at-arity-eight.fr.md), [ADR-0006](../adr/0006-materialize-dummies-only-through-generate.fr.md).

JustDummies est livré comme package autonome sans dépendance à un quelconque runtime de gestion d'erreurs ; la frontière est gardée par un test d'architecture. La génération n'est pas seedée par défaut ; la génération reproductible est choisie explicitement et expose la seed nécessaire pour rejouer les échecs.

La génération de collections distinctes compare d'abord le nombre demandé à l'indication de cardinalité du générateur d'éléments, lorsque `ICardinalityHint` sait en fournir une, diminuée des valeurs fixées en dehors de ce domaine via `Containing(...)` et des tirages opaques demandés via `ContainingAny(...)` — les deux élargissent ce que le générateur doit encore fournir lui-même plutôt que de compter contre lui. Une plage flottante ou décimale n'est pas considérée comme dénombrable à bas coût, car énumérer ses valeurs représentables relève d'une arithmétique de bits spécifique au type, disproportionnée pour l'usage « dummy » ; un tel générateur ne participe donc au contrôle anticipé que s'il est fixé sur une liste blanche explicite ou une valeur unique (`OneOf`, `Zero`, `Between(x, x)`), jamais via une plage plus large. Lorsque la cardinalité est inconnue, elle effectue un nombre borné de tirages et échoue explicitement plutôt que de boucler indéfiniment. Cette borne est un mécanisme de sûreté, pas une preuve que tout générateur externe ou biaisé réussira dès lors qu'un nombre suffisant de valeurs distinctes existe théoriquement. `CollectionState` et `ICardinalityHint` unifient la cardinalité et l'appartenance derrière une seule interface, afin qu'un générateur à domaine fini ne puisse pas sortir du périmètre anticipé via un comparateur.

Lorsque la cardinalité est inconnue ou grande, le budget de tirage est dérivé de la demande plutôt que fixé : un domaine connu pour contenir au plus un million de valeurs autorise soixante-quatre tirages par valeur qu'il peut produire, un domaine plus grand ou inconnu en autorise soixante-quatre par élément demandé, et le résultat est relevé à un plancher de dix mille. Un nombre de collisions supérieur à ce budget met fin au remplissage.

La saturation lève `AnyGenerationException`, qui porte la graine sous forme d'entier nullable, accompagnée d'un message nommant le nombre demandé, le générateur qui en a produit trop peu, le nombre de valeurs distinctes atteint, et la façon de rejouer l'exécution. L'indication de rejeu est nuancée plutôt que promise franchement quand le coupable n'est pas pleinement reproductible — un générateur étranger, ou une composition qui en mêle un à un opérande sourcé — car promettre un rejeu complet de ses éléments serait faux. C'est `AnyDerivation` qui en décide : il résout la source derrière un générateur composé, détermine si la composition est reproductible, et détermine si tous les tirages proviennent d'une même source.

`Any.Combine` fournit des surcharges jusqu'à l'arité huit, plus `PairOf` et `TripleOf` pour les formes tuple ; chacune prend un générateur par partie et une fonction de composition. Les arités supérieures sont volontairement exclues de cette surface de confort et doivent utiliser la composition ou une factory spécifique au domaine. Les surcharges d'arité sept et huit portent une suppression localisée sur le nombre de paramètres, dont la justification nomme la décision qui a posé le plafond ; le plafond lui-même est documenté sur la surcharge d'arité huit, là où un appelant qui atteint la limite le lira.

La matérialisation s'effectue uniquement par `Generate()`. Les opérations du builder décrivent la génération et ne produisent pas d'effets de bord cachés.

## Règles de maintenance

* Modifier cette référence lorsque les mécanismes d'implémentation changent mais que les décisions restent valides.
* Écrire un nouvel ADR lorsque le choix architectural, la promesse de compatibilité ou le compromis accepté change.
* Conserver depuis chaque ADR concerné un lien vers la section pertinente de cette référence.
* Ne pas déplacer hors des ADR la justification, les alternatives rejetées ni les conséquences architecturales.
