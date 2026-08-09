# ADR-0064 | Ne jamais tirer null pour un paramètre nullable

🌍 🇬🇧 [English](0064-never-draw-null-for-a-nullable-parameter.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-07-30
**Accepted:** 2026-08-09
**Decision Makers:** Reefact

> Les renvois de section (§N) pointent vers la [spécification de `dum`](../specifications/justdummies-tool.fr.md), le document dont cet enregistrement a été extrait.

## Contexte

La bibliothèque expose `OrNull` sous deux formes — une pour les types valeur, une pour les types
référence annotés — chacune retournant un generator qui produit `null` une partie du temps (§14.4).

Un paramètre de constructeur déclaré `string?` ou `int?` énonce que null est *permis*. Il n'énonce
pas qu'un test particulier a l'intention d'exercer le chemin null.

Le principe affiché de la bibliothèque est que les contraintes expriment les invariants qu'une
valeur doit satisfaire, jamais ce que le test asserte.

Le type émis porte une surcharge `With{Param}(IAny<TParam>)` pour chaque paramètre ([ADR-0057](0057-make-the-emitted-generator-a-first-class-iany.fr.md)), donc un
développeur peut fournir n'importe quel generator, y compris nullable, sur un paramètre choisi dans
un test choisi.

La variance en C# ne franchit pas les types valeur, donc un paramètre nullable de type valeur exige
une conversion explicite quand le generator sous-jacent est utilisé. `OrNull` n'en exigerait
aucune, puisqu'il retourne déjà le type de generator nullable (§5.2).

Un test qui n'échoue que sur certaines exécutions est le mode d'échec que la bibliothèque existe
pour supprimer.

## Décision

L'émetteur n'applique jamais `OrNull`, de sorte qu'un paramètre nullable tire une valeur de son
type sous-jacent et que le développeur consent à null explicitement.

## Justification

La nullabilité dans une signature est une permission, pas une intention. La lire comme une
intention fait décider au tool, à la place du développeur et au hasard, quelles exécutions
exercent le chemin null — si bien qu'un test écrit pour le chemin ordinaire échoue sur les
exécutions qui tirent null, pour une raison étrangère à tout ce qu'il asserte. C'est l'échec
intermittent que [ADR-0060](0060-seed-generators-from-constructor-guards.fr.md) existe pour empêcher, atteint par l'autre bout.

Le consentement est déjà bon marché et précis. La surcharge par generator de [ADR-0057](0057-make-the-emitted-generator-a-first-class-iany.fr.md) permet au
développeur de demander null au paramètre exact et dans le test exact où cela compte, c'est-à-dire
là où cette décision appartient : le test qui veut le chemin null le dit, et aucun autre test n'en
souffre.

Refuser ici applique aussi à un défaut la règle propre à la bibliothèque sur les contraintes.
Émettre `OrNull` encoderait ce qu'un test pourrait asserter plutôt que ce que la valeur doit
satisfaire, ce qui est la distinction sur laquelle la bibliothèque est bâtie.

## Alternatives considérées

##### Émettre `OrNull` pour tout paramètre nullable

Considérée parce que c'est la lecture fidèle du type déclaré, qu'elle ne demande aucun cas
particulier, et que — pour les nullables de type valeur — elle est plus courte que la conversion que
cette décision impose.

Écartée parce que la fidélité à la signature coûte le déterminisme : environ la moitié des valeurs
générées seraient null sans que le test l'ait choisi. L'émission plus courte achète la brièveté au
prix de la propriété que la bibliothèque vend.

##### Émettre `OrNull` seulement là où le constructeur tolère visiblement null

Considérée parce qu'elle réutiliserait la lecture des gardes que [ADR-0060](0060-seed-generators-from-constructor-guards.fr.md) effectue déjà, n'appliquant la
nullabilité que là où le code l'accepte démontrablement.

Écartée parce que l'absence de garde null n'est pas une preuve d'intention — elle est tout aussi
compatible avec un oubli — et parce qu'elle ferait dépendre la stabilité d'un test de l'écriture ou
non d'une garde sans rapport. C'est pire qu'une règle uniforme, dans un sens comme dans l'autre.

## Conséquences

**Positives.** Un generator scaffoldé produit la même forme de valeur à chaque exécution. Rien dans
le défaut émis ne peut rendre un test intermittent par la nullabilité.

**Négatives.** La branche null d'un constructeur, ou du code sous test, n'est jamais exercée par un
generator scaffoldé à moins que le développeur ne le demande. Un paramètre typé `string?` pour une
raison reçoit un generator qui n'explore jamais cette raison.

Visiblement négatif aussi : pour un nullable de type valeur l'émetteur doit convertir explicitement,
donc le §5.2 porte un saut qui se lit comme gratuit tant que cette décision n'est pas connue.

**Risques.** Ce saut est la partie de l'émetteur la plus susceptible d'être « simplifiée » en
défaut — `OrNull` est plus court, retourne exactement le type voulu, et ressemble au nettoyage
évident. Le réintroduire restaurerait l'instabilité en silence. Atténué par cet enregistrement et
par le cas de résolveur nommé ci-dessous.

## Actions de suivi

* Conserver un cas de résolveur pour un paramètre nullable de type valeur assertant la conversion
  explicite, et nommer cet enregistrement là où l'émetteur l'effectue, pour que le saut ne soit pas
  simplifié.

## Références

* §5.2, §14.4 de cette spécification ; [ADR-0057](0057-make-the-emitted-generator-a-first-class-iany.fr.md) et [ADR-0060](0060-seed-generators-from-constructor-guards.fr.md) de cette section.

---
