# ADR-0106 | Tester les analyseurs contre l'exécution dans une suite sœur

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0106-test-the-analyzers-against-the-run-time-in-a-sibling-suite.md)

**Statut :** Accepted
**Proposé :** 2026-10-24
**Accepté :** 2026-10-24
**Décideurs :** Reefact

## Contexte

`JustDummies.Analyzers` est testé par un seul projet, `JustDummies.Analyzers.UnitTests` : une suite par règle, dont
chacune compile des extraits écrits à la main et vérifie ce que la règle y signale. Un extrait ne couvre que les
écritures auxquelles quelqu'un a pensé.

Plusieurs règles affirment qu'une chaîne qu'elles signalent lève une exception à l'exécution, et qu'une chaîne
qu'elles ne signalent pas n'en lève pas. `JD015` l'affirme d'une chaîne `Any.String()` : ses constantes « n'admettent
aucune valeur : la chaîne lève une `ConflictingAnyConstraintException` dès l'exécution de la ligne d'arrangement ».
Cette affirmation se vérifie indépendamment de la règle : exécuter la chaîne montre si elle lève une exception, quoi
qu'en dise la règle.

La revue adversariale de septembre 2026 a ouvert vingt et une issues. Dix étaient des angles morts des analyseurs :
des chaînes qui levaient une exception à l'exécution sans qu'aucune règle ne les signale. Chacune a été trouvée de la
même manière, en exécutant la chaîne pour voir ce qui se passe réellement, puis en confrontant les règles au résultat
(issue #232). Cette vérification a été faite à la main, et une seule fois.

La bibliothèque sépare déjà ses propres tests de cette façon.
L'[ADR-0019](0019-split-the-justdummies-test-bed-between-example-and-property-suites.fr.md) a donné à `JustDummies`
deux suites sœurs : `JustDummies.PropertyTests` pour les invariants quantifiés sur des arguments générés, avec
FsCheck, et `JustDummies.UnitTests` pour les cas nommés. Les analyseurs n'ont pas ce partage, et leur unique projet
de test n'embarque aucun outil de test par propriétés.

Une chaîne générée doit être compilée avant qu'une règle puisse l'analyser. Chaque cas coûte donc bien plus cher que
dans une propriété d'exécution : une telle suite exécute des centaines de cas là où une propriété d'exécution en
exécute des milliers.

## Décision

Les analyseurs se dotent d'une suite de tests sœur, `JustDummies.Analyzers.PropertyTests` : elle génère les chaînes
qu'une règle prétend juger, les exécute pour obtenir le résultat attendu et exige que la règle signale une chaîne
exactement quand l'exécution la refuse, tandis que `JustDummies.Analyzers.UnitTests` garde les cas nommés.

## Justification

**L'exécution est ce qu'une règle prétend prédire, et l'interroger coûte peu.** Une règle qui dit « cette chaîne lève
une exception » se vérifie en exécutant la chaîne. Comparer les deux sur des chaînes générées transforme l'audit
manuel et ponctuel de la revue en filet de sécurité permanent : une chaîne que la règle ne reconnaît pas fait échouer
la suite dès qu'elle est générée, et non à la revue suivante. Ses premières exécutions ont trouvé trois angles morts
de `JD015` que la revue avait manqués, et une catégorie de refus qu'aucune règle ne couvre.

**Un désaccord dans un sens comme dans l'autre est une découverte.** Une chaîne refusée qui n'est pas signalée est
l'angle mort que la revue n'a cessé de trouver. Un signalement sur une chaîne que l'exécution accepte est un faux
positif, et il coûte plus cher : quand les avertissements sont traités comme des erreurs, il casse une compilation
correcte. Seule une comparaison avec l'exécution attrape les deux.

**Un projet frère, pour les raisons qu'a données l'ADR-0019 et une de plus.** Les cas nommés et les cas générés
n'affirment pas le même genre de chose, et le projet d'où vient un échec indique déjà quel genre a cassé ; FsCheck
reste en outre hors de la suite qui n'en a pas besoin. La raison de plus est le coût : une suite qui compile un
extrait par cas a besoin de son propre budget de temps, et la fondre dans la suite des cas nommés ralentirait chaque
exécution rapide pendant qu'on modifie une règle.

**Chaque règle est vérifiée contre son propre périmètre documenté, pas contre tout ce que l'exécution refuse.** Les
chaînes générées couvrent ce que la règle prétend juger, et rien de plus. Un refus que la règle ne prétend pas
couvrir, comme la déclaration d'une seconde famille là où une seule est permise, est exclu des chaînes générées et
documenté avec l'issue qui le suit, au lieu de compter comme un échec de la règle. La suite attrape alors une règle
qui manque à sa propre promesse, et une question de périmètre va vers une décision plutôt que vers un test.

## Alternatives considérées

### Ajouter les tests générés à `JustDummies.Analyzers.UnitTests`

Envisagée parce qu'elle ne demande aucun nouveau projet et réutilise le harnais existant. Écartée parce qu'elle
mélange les deux genres de tests que l'ADR-0019 tient délibérément séparés, qu'elle ajoute FsCheck à une suite qui ne
s'en sert pas, et qu'elle lie chaque exécution rapide des cas nommés d'une règle à la suite la plus lente du dépôt.

### Garder la comparaison comme un audit manuel périodique

Envisagée parce que la revue qui l'a motivée a fonctionné. Écartée parce qu'un audit manuel ne trouve que ce qui
existe le jour où il est mené. Chaque écriture ajoutée ensuite reste non vérifiée jusqu'à l'audit suivant, et la
revue a montré que c'est là qu'apparaissent ces défauts.

### Un modèle symbolique plutôt que l'exécution

Envisagée parce que l'issue #231 en construit un pour le côté exécution. Écartée pour cette suite, parce que la
question ici est de savoir si une règle est d'accord avec la bibliothèque, et que la bibliothèque est elle-même
l'autorité en la matière. Un modèle ajouterait un tiers dont les propres erreurs pourraient masquer ou inventer un
désaccord.

## Conséquences

### Positives

* Une règle qui prétend prédire un refus à l'exécution est vérifiée contre cette affirmation sur des chaînes
  générées, dans les deux sens, à chaque exécution.
* Un désaccord est rapporté sous la forme de la plus courte chaîne qui le montre encore.
* Les lacunes de périmètre sont consignées dans le générateur avec l'issue qui les suit : ce que la suite ne couvre
  pas est documenté.

### Négatives

* Un projet de test de plus, avec son entrée dans la solution et sa part du temps de CI.
* Compiler un extrait par cas maintient le nombre de cas dans les centaines : un désaccord rare peut demander
  plusieurs exécutions pour apparaître.

### Risques

* Si les chaînes générées couvrent moins que le périmètre réel d'une règle, la suite peut passer alors que la règle
  manque des chaînes hors de cette couverture. Le générateur doit grandir avec la règle, comme ses cas nommés.

## Actions de suivi

* Étendre la suite de `JD015` aux autres règles qui prédisent un refus (`JD016`, `JD017`, `JD023`), à mesure que
  chacune reçoit son propre générateur.
* Retirer du générateur de `JD015` le filtre des contraintes déclarées une seule fois, une fois l'issue #274
  tranchée.

## Références

* [ADR-0019](0019-split-the-justdummies-test-bed-between-example-and-property-suites.fr.md) — le même partage pour
  la bibliothèque, et l'essentiel du raisonnement repris ici.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — pourquoi c'est le périmètre d'une
  règle, et non tout ce que fait l'exécution, qui borne ce qui est comparé.
* [Issue #232](https://github.com/Reefact/just-dummies/issues/232) — la demande et la revue dont elle vient.
* [Issue #274](https://github.com/Reefact/just-dummies/issues/274) — les déclarations conflictuelles qu'aucune règle ne
  signale encore.
