# ADR-0112 | Signaler un test qui vérifie un refus comme tout autre code, et le laisser faire taire la règle explicitement

🌍 🇬🇧 [English](0112-report-a-test-asserting-a-refusal-like-any-other-code.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-10-29
**Accepted:** 2026-10-29
**Decision Makers:** Reefact

## Contexte

L'ADR-0038 a borné le coût des règles JD notamment en les limitant de sorte qu'« un test négatif vérifiant un
conflit » reste silencieux, pour qu'elles restent utilisables dans une suite qui teste le comportement d'échec de la
bibliothèque elle-même. Vingt règles appliquaient cette limite par une même heuristique : un appel qui constitue tout le
corps d'une lambda passée en argument, comme dans `Check.ThatCode(() => Any.Int32().Between(10, 5))`, était traité
comme un test vérifiant une exception, et n'était pas signalé.

L'heuristique lit la forme du code, pas son intention, et elle se trompe dans les deux sens. `Task.Run(() =>
chain.Generate())` ou une lambda dans un tuple ont la même forme et sont du code de préparation, pourtant silencieux ;
jusqu'à #346, `Select(_ => chain.Generate())` l'était aussi. Le `generator.Invoking(g => g.Between(10, 5))` de
FluentAssertions est un test négatif, pourtant signalé depuis que #346 a restreint la forme aux lambdas sans paramètre.
Chaque correction a déplacé la frontière sans la rendre juste.

Les tests qui vérifient un refus de JustDummies sont, de très loin, ceux de ce dépôt : retirer l'heuristique fait
apparaître 455 diagnostics, tous dans `JustDummies.UnitTests` et `JustDummies.PropertyTests`, tous sur des chaînes
écrites pour être refusées. Un utilisateur teste son propre code, où une chaîne impossible est une erreur. Ces chaînes
lèvent à chaque exécution : un diagnostic manqué se voit au premier lancement du test, et un diagnostic émis sur un
test négatif voulu coûte une suppression justifiée. Les analyzers .NET courants — les règles `CA` de la plateforme,
celles de xUnit et de NUnit — ne cherchent pas non plus à reconnaître les tests négatifs ; un projet fait taire une
règle là où il le veut, localement ou par configuration.

Ce dépôt traite un changement de sémantique d'un identifiant de diagnostic comme une décision d'architecture.

## Décision

Aucune règle JD ne cherche à reconnaître un test qui vérifie un refus : un tel test est signalé comme tout autre code,
et fait taire la règle sur la méthode de test avec un `[SuppressMessage]` justifié.

## Justification

**L'intention est écrite, pas devinée.** Un `[SuppressMessage]` sur la méthode de test dit, dans le test, que le refus
est le sujet, et sa justification dit pourquoi. Une heuristique ne dit rien qu'un lecteur puisse voir, et se trompe dans
les deux sens ; chaque raffinement jusqu'ici a échangé un résidu contre un autre.

**Le coût revient à qui écrit ces tests.** Presque tous les tests négatifs d'un refus de JustDummies vivent dans ce
dépôt. Les payer par des suppressions ici garde les règles exactes pour les utilisateurs, qui payaient jusqu'ici les
silences à tort de l'heuristique.

**Se tromper coûte peu dans ce sens.** Les chaînes que ces règles signalent lèvent à chaque exécution. Un diagnostic sur
un test négatif voulu se fait taire en une ligne ; un diagnostic manqué sur du code de préparation était un test rouge
découvert plus tard. L'argument que l'ADR-0038 faisait valoir pour des suppressions bon marché vaut ici aussi.

**C'est le comportement habituel des analyzers.** Un lecteur qui connaît les règles de la plateforme s'attend à ce
qu'un diagnostic veuille dire la même chose dans un test qu'ailleurs, et à le faire taire de la même façon.

## Alternatives considérées

### Continuer à affiner la forme

Envisagée parce que chaque affinement a corrigé un vrai cas : une lambda sans paramètre, une chaîne remontée jusqu'à
l'appel le plus externe.

Rejetée parce qu'aucune forme syntaxique ne porte l'intention. Chaque restriction a laissé un résidu d'un côté, et
chaque élargissement de l'autre.

### Désactiver les règles par projet de test, par configuration

Envisagée parce qu'elle tient en quelques lignes au lieu d'un attribut par méthode de test.

Rejetée parce qu'elle éteint les règles partout dans ces projets, y compris sur du code de préparation qu'elles
attraperaient, alors qu'une suppression sur la méthode fait taire exactement le test qui le veut.

## Conséquences

### Positives

* Vingt règles perdent leur exemption, et l'heuristique partagée disparaît.
* Un diagnostic veut dire la même chose partout ; une page n'a plus à décrire quelles formes de lambda se taisent.

### Négatives

* Les suites de ce dépôt portent un `[SuppressMessage]` par méthode de test qui vérifie un refus : 261 au moment de
  cette décision.
* Un utilisateur qui écrit un test négatif d'un refus de JustDummies voit désormais le diagnostic, et doit le faire
  taire.

### Risques

* Un utilisateur dont la compilation traite les avertissements comme des erreurs et qui a de tels tests passe au rouge
  à la mise à jour. Le changelog le dit, et nomme la suppression.

## Actions de suivi

* Aucune.

## Références

* [ADR-0038](0038-guard-the-recipe-versus-value-boundary-with-analyzers.fr.md) — la limite que cette décision révise.
* [ADR-0050](0050-name-a-suppressed-rule-through-a-catalogue-constant.fr.md) — comment une suppression nomme sa règle.
* Pull requests [#345](https://github.com/Reefact/just-dummies/pull/345) et [#346](https://github.com/Reefact/just-dummies/pull/346), dont les revues ont relevé les résidus de l'heuristique.
