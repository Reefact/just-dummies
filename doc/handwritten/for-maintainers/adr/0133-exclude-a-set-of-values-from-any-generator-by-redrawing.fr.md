# ADR-0133 | Exclure un ensemble de valeurs de tout générateur par retirage

🌍 🇬🇧 [English](0133-exclude-a-set-of-values-from-any-generator-by-redrawing.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-11-05
**Accepted:** 2026-11-05
**Decision Makers:** Reefact

## Contexte

L'[ADR-0121](0121-exclude-a-value-from-any-generator-by-redrawing.fr.md) a donné à tout `IAny<T>` un
`DifferentFrom(value)` qui retire tant que la valeur tirée égale celle donnée, un nombre borné de fois, et échoue avec
sa graine quand le budget est épuisé. Les builders typés portent déjà un `Except` sur un ensemble de valeurs, inscrit
dans leur propre spécification et vérifié à la déclaration ; un générateur vu seulement comme un `IAny<T>` — construit
par `As` ou `Combine`, ou scaffoldé — n'a pas de tel membre.

Le troisième essai d'expérience développeur a rencontré le manque dans deux domaines (CORE-7) : un test excluant
plusieurs objets entiers d'un générateur composé ou scaffoldé, les valeurs qu'il avait déjà utilisées. La seule façon
de le dire était une chaîne de `DifferentFrom`, écrite à la main ou dans une boucle sur les valeurs. Cette chaîne
fonctionne et reste bornée : mesurée sur un générateur qui ne produit que des valeurs exclues, elle échoue après
environ mille tirages par valeur exclue, en quelques millisecondes. Son échec ne nomme pourtant que la dernière valeur
exclue, puisque chaque maillon n'en connaît qu'une.

Là où une exclusion porte sur un composant de la valeur, le builder de ce composant l'énonce — l'`Except` d'un builder
typé, ou le raffinement de ce paramètre dans un générateur scaffoldé — et elle est vérifiée à la déclaration.

Une liste d'exclusion vide est l'identité ([ADR-0122](0122-accept-an-empty-exclusion-as-the-identity.fr.md)).

## Décision

Tout `IAny<T>` gagne un `Except` sur un ensemble de valeurs, une liste ou une séquence, qui retire tant que la valeur
tirée en fait partie selon l'égalité par défaut du type, en un seul retirage borné pour tout l'ensemble, et échoue en
nommant chaque valeur exclue ; les builders typés gardent leur propre `Except` constructif, que le type statique du
receveur sélectionne.

## Justification

**Un appel dit ce que la chaîne épelait.** Exclure les valeurs qu'un test a déjà utilisées est une seule intention ; une
chaîne de `DifferentFrom`, ou une boucle qui en construit une, la dit en morceaux et se lit comme un mécanisme plutôt
que comme une contrainte.

**Un retirage et un message pour une intention.** L'ensemble est vérifié en entier à chaque tirage : le budget est
celui du retirage, pas un par maillon, et un échec nomme l'ensemble que le test a déclaré plutôt que la valeur qui se
trouvait être la dernière.

**Il prolonge l'ADR-0121 au lieu de s'en écarter.** La même garantie par retirage, la même borne, la même graine dans
l'échec, le même refus net quand on sait déjà que le générateur ne produit qu'une valeur exclue et rien d'autre, et la
même règle selon laquelle le type statique choisit le membre propre du builder typé. Un lecteur qui connaît
`DifferentFrom` sur `IAny<T>` connaît celui-ci.

**La voie constructive reste celle à préférer.** Une exclusion sur un composant, déclarée sur le builder de ce
composant, est vérifiée à la déclaration et lue par les analyzers. Ce membre sert aux valeurs entières, là où aucun
composant n'énonce l'exclusion.

## Alternatives envisagées

### Garder la chaîne de `DifferentFrom` comme réponse documentée

Envisagée parce qu'elle n'ajoute aucun membre public avant le gel de la 1.0 et que le besoin n'a été rencontré que
dans deux domaines. Rejetée : la chaîne est correcte et bornée, mais c'est une boucle que le lecteur écrit pour une
seule intention, et son échec ne nomme qu'une valeur de l'ensemble ; le membre coûte peu et reste cohérent avec la
forme à une valeur.

### Donner un autre nom à l'exclusion sur `IAny<T>`

Envisagée pour rendre visible dans l'appel la garantie par retirage. Rejetée pour la raison qui l'a fait rejeter
pour `DifferentFrom` dans l'ADR-0121 : la même intention se lirait de deux façons selon la manière dont le générateur
a été obtenu.

## Conséquences

### Positives

* Un test exclut en un appel les valeurs qu'il a déjà utilisées, sur tout générateur, avec un échec qui les nomme
  toutes.
* Une collection distincte construite sur l'exclusion compte les valeurs restantes, comme elle le fait sur
  `DifferentFrom`.

### Négatives

* Un membre public de plus à tenir après la 1.0.
* Comme `DifferentFrom`, l'exclusion compare avec l'égalité par défaut du type : sur un type référence sans égalité de
  valeur, une instance créée à part, aux champs identiques, n'est pas exclue.

### Risques

* Un lecteur peut prendre ce membre là où le builder du composant pourrait énoncer l'exclusion et la vérifier à la
  déclaration. La documentation du membre et le guide de composition indiquent d'abord cette voie.

## Actions de suivi

* Aucune au-delà de l'implémentation et de sa documentation.

## Références

* [ADR-0121](0121-exclude-a-value-from-any-generator-by-redrawing.fr.md) — la décision que celle-ci amende. Elle garde
  son statut ; l'exclusion par retirage qu'elle a donnée à `IAny<T>` prend désormais aussi un ensemble.
* [ADR-0122](0122-accept-an-empty-exclusion-as-the-identity.fr.md) — un ensemble vide n'exclut rien.
* [Essai UX 3, CORE-7](../../../../tools/ux-trial/REPORT-3.fr.md#core-7).
* Issue [#433](https://github.com/Reefact/just-dummies/issues/433).
