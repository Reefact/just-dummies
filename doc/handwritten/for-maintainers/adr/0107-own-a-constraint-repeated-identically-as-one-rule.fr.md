# ADR-0107 | Confier à une seule règle la contrainte répétée à l'identique, et en retirer JD024

🌍 🇬🇧 [English](0107-own-a-constraint-repeated-identically-as-one-rule.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-10-25
**Accepted:** 2026-10-25
**Decision Makers:** Reefact

## Contexte

La bibliothèque n'accepte certaines contraintes qu'une seule fois par générateur — une famille de caractères, une
longueur exacte, un préfixe, un ensemble de valeurs, un multiple, une échelle, une granularité, un décalage, un
nombre exact d'éléments, les composantes d'une URI — et en cumule d'autres : les exclusions, les soustractions, les
fragments contenus d'une chaîne, et quelques interrupteurs comme `NotBlank` ou `Distinct()`. Une seconde déclaration
*différente* d'une contrainte à déclaration unique provoque une exception à l'appel, et JD034 la signale en
sévérité avertissement.

Une contrainte déclarée de nouveau *à l'identique* ne fait rien. La bibliothèque conserve la déclaration qu'elle
détient déjà, ou retire de nouveau ce qui a déjà disparu, et le générateur tire exactement ce qu'il tirait. Rien ne
provoque d'exception et aucun rapport d'exécution ne le mentionne. C'est vrai pour toute contrainte à déclaration
unique et pour toute contrainte cumulative citée plus haut ; on l'a vérifié en comparant les tirages, sous
plusieurs graines, avec et sans le second appel. Ce ne l'est pas pour l'élément contenu d'une collection : écrit
deux fois, il est exigé deux fois, et une liste en tire deux.

Avant cette décision, aucune règle ne signalait cette forme sur les chaînes, les caractères, les numériques non
entiers, les types temporels, les collections ni les URI. Sur les générateurs d'entiers, JD024 en signalait un cas —
une exclusion écrite deux fois, comme une exclusion qui ne retire rien — en sévérité information. JD024 relève de
l'information parce qu'une exclusion inerte peut documenter une intention qu'un futur élargissement de l'intervalle
rendra réelle. JD032 signale une borne déclarée deux fois, en sévérité avertissement, appariée sur le nom de la
contrainte et dans toutes les familles ; une borne atteinte par un alias est une question d'écriture, laissée à
l'ADR-0077 et à l'ADR-0080.

L'issue #279 laissait trois questions ouvertes : quelle règle, quelle sévérité, quelle portée. Le mainteneur y a
répondu : une règle à part entière, en sévérité avertissement, pour tous les générateurs, contraintes cumulatives
comprises, les bornes et leurs alias restant où ils sont. JD034 lit désormais tous les générateurs : la table par
générateur des contraintes à déclaration unique, et la lecture de « la même déclaration » dont la règle a besoin,
existent donc déjà.

Ce dépôt traite le changement de la sémantique d'un identifiant de diagnostic comme une décision d'architecture.

## Décision

Une contrainte déclarée de nouveau à l'identique sur une même chaîne fluide est signalée par une règle à part
entière, en sévérité avertissement, dans toutes les familles de générateurs, avec la lecture que fait JD034 de ce
qu'est la même déclaration ; JD024 ne signale plus une exclusion écrite deux fois à l'identique.

## Justification

**Une répétition n'a pas de lecture défendable.** Les deux appels sont dans une même expression, sous les yeux d'un
même lecteur, et le second n'énonce rien que le premier ne disait déjà. C'est un copier-coller ou une fusion — la
lecture que l'ADR-0078 a donnée à une borne écrite deux fois en plaçant JD032 en avertissement. La raison qui place
JD024 en information, une intention qu'un futur élargissement rendra réelle, n'existe pas ici : aucun avenir ne
donne de l'importance au second appel.

**Elle va contre ce à quoi sert la déclaration d'un dummy.** Une déclaration dit ce que la valeur doit satisfaire,
pour que ce qui est arbitraire se voie arbitraire. Un appel qui ne change rien se lit comme s'il avait été choisi
pour une raison, et le lecteur qui le croit le laisse en place — exactement le mensonge sur ce qui compte que la
bibliothèque existe pour supprimer.

**JD034 et cette règle sont les deux issues d'une même comparaison.** Une seconde déclaration d'une contrainte à
déclaration unique est soit identique à la première, et ignorée, soit différente, et refusée. Lire « la même » une
seule fois, pour les deux règles, garantit qu'exactement l'une d'elles s'exprime sur un appel donné, et qu'un
ensemble de valeurs, une granularité ou un décalage est le même pour les deux selon la même mesure (ADR-0100).

**Une erreur, un diagnostic, dans toutes les familles.** Sur une chaîne d'entiers, une exclusion écrite deux fois
déclencherait sinon JD024 et cette règle, et sur toutes les autres chaînes cette seule règle. JD024 se retire sur
cette seule forme, comme il s'est retiré pour JD032, et garde l'exclusion rendue inerte par un appel *différent*,
qui est le cas pour lequel sa sévérité information est argumentée.

**Les bornes restent en dehors parce qu'elles se replient au lieu de se répéter.** Une borne écrite de nouveau peut
changer le domaine — la plus stricte l'emporte — et JD032 détient déjà le nom écrit deux fois, à la même sévérité.
La signaler ici aussi serait le double diagnostic que cette décision évite. Un alias est une question d'écriture,
qui a son propre enregistrement.

**Toutes les familles, d'un coup.** L'exécution ignore une répétition de la même manière sur tous les générateurs,
et la table dont la règle a besoin existe depuis que JD034 lit tous les générateurs. Échelonner par famille
signalerait la même erreur sur certains générateurs et pas sur d'autres, ce qu'un utilisateur lit comme un défaut
de l'outil.

## Alternatives considérées

### Élargir JD024 au-delà des générateurs d'entiers

Son message — la contrainte ne change rien — est littéralement vrai d'une répétition, et il signale déjà une
exclusion écrite deux fois sur une chaîne d'entiers.

Écartée parce que la sévérité de JD024 repose sur une lecture défendable qu'une répétition n'a pas : l'erreur
serait sous-estimée dans toutes les familles. JD024 est en outre levée par un analyseur qui raisonne sur un domaine
entier ; l'élargir déplacerait le sens d'un identifiant publié vers une forme que sa justification ne décrit pas.

### Élargir JD032 à toute contrainte écrite deux fois

JD032 signale déjà une borne écrite deux fois, le même geste sur une autre sorte de contrainte, à la sévérité
retenue ici.

Écartée parce que l'identifiant de JD032 signifie une borne écrite deux fois, comme l'a décidé l'ADR-0078, et que
son message — seule la plus stricte s'applique — est faux pour une répétition, où les deux appels sont égaux et
aucun n'est plus strict. Le changer imposerait de remplacer l'ADR-0078 pour le seul gain d'un identifiant.

### Signaler en sévérité information

Une répétition ne nuit pas à la valeur tirée, et une règle d'information ne peut pas casser une compilation qui
traite les avertissements comme des erreurs.

Écartée pour les raisons ci-dessus : la taxonomie des sévérités suit le mode de défaillance, et un appel qui
n'énonce rien devant un lecteur qui le croira porteur de sens est une erreur probable, pas une convention.

### Les chaînes et les caractères d'abord

L'issue #279 proposait cet ordre tant que la table par générateur n'existait que pour les chaînes.

Écartée dès que cette table a couvert tous les générateurs : il ne restait rien à échelonner, et l'échelonnement
aurait laissé la même erreur signalée sur certaines familles seulement.

## Conséquences

### Positives

* Une répétition est signalée une seule fois, avec un seul message, où qu'elle soit écrite.
* JD034 et la nouvelle règle ne peuvent pas diverger sur ce qui compte comme la même déclaration, ni sur l'endroit où
  une chaîne s'arrête.
* JD024 garde exactement la forme pour laquelle sa sévérité est argumentée.

### Négatives

* Un nouvel identifiant coûte une page anglaise et une page française, une ligne dans chaque table des règles, une
  constante du catalogue sur le train `catalog`, et la mise à jour de chaque décompte des règles.
* La portée documentée de JD024 se réduit : une suppression écrite contre JD024 pour une exclusion écrite deux fois
  cesse de s'appliquer.
* Une borne ou un alias écrits deux fois à l'identique sur un générateur non entier restent muets, puisque ni
  l'appariement par nom de JD032 ni le domaine de JD024 ne les atteignent.

### Risques

* Un générateur de code ou un assistant très paramétré pourrait produire légitimement une répétition dans une même
  chaîne ; en sévérité avertissement, cela coûte une suppression plutôt qu'une compilation.
* La liste des contraintes cumulatives dont la répétition ne change rien est tenue à la main. Une nouvelle que la
  bibliothèque ajoute reste muette tant qu'elle n'y figure pas, et une dont la répétition se mettrait à compter
  serait signalée à tort ; la propriété différentielle confronte la liste à l'exécution.

## Actions de suivi

* Reformuler la portée de JD024 sur ses pages anglaise et française.
* Relier la nouvelle règle depuis JD024, JD032 et JD034.

## Références

* Issue #279 — les questions ouvertes et les réponses du mainteneur.
* [ADR-0078](0078-own-a-bound-declared-twice-as-one-rule.fr.md) — le même geste pour une borne écrite deux fois.
* [ADR-0100](0100-identify-a-value-set-by-its-values.fr.md) — ce qui rend deux ensembles de valeurs identiques.
* [ADR-0080](0080-admit-a-rule-that-names-a-resolved-ambiguity.fr.md) — où se trouve la question des alias.
* [ADR-0106](0106-test-the-analyzers-against-the-run-time-in-a-sibling-suite.fr.md) — la suite différentielle qui
  confronte la règle à l'exécution.
