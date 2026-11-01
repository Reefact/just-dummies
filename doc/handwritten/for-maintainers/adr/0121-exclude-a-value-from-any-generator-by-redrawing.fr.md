# ADR-0121 | Exclure une valeur de tout générateur en retirant

🌍 🇬🇧 [English](0121-exclude-a-value-from-any-generator-by-redrawing.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-11-01
**Accepted:** 2026-11-01
**Decision Makers:** Reefact

## Contexte

Chaque builder typé porte `DifferentFrom(value)`, une exclusion dans la spécification propre du builder : elle est
vérifiée à la déclaration, `Any.Int32().OneOf(5).DifferentFrom(5)` est refusé tout de suite en nommant les deux
contraintes, et les analyzers qui raisonnent sur les exclusions (JD023, JD034) la voient à la compilation. Les
contraintes de ce genre sont constructives ; l'ADR-0033 admet aussi des contraintes rejectives, un nouveau tirage
borné qui échoue bruyamment plutôt que de boucler.

Un générateur construit par `As` ou `Combine`, un générateur scaffoldé, ou un générateur écrit par un utilisateur
n'est vu que comme un `IAny<T>`, et n'a pas de `DifferentFrom`. Le second essai d'expérience développeur a rencontré
six contextes où un constructeur refuse deux paramètres égaux ou un test a besoin d'une seconde valeur différente d'une
première. Deux tirages indépendants entraient en collision sur des graines rares — une sur 315 pour un type, 71 sur
2 000 pour un autre — et des utilisateurs ont écrit à la main des boucles de nouveau tirage dans des générateurs
scaffoldés.

C# résout un appel vers un membre d'instance avant une extension du même nom, à partir du type statique du receveur.

## Décision

Tout `IAny<T>` reçoit une extension `DifferentFrom(value)` qui retire tant que la valeur tirée égale celle donnée selon
l'égalité par défaut du type, un nombre borné de fois, et échoue avec sa graine quand le budget est épuisé ; les
builders typés gardent leur `DifferentFrom` constructif, que choisit le type statique du receveur, et les analyzers ne
raisonnent que sur celui-là.

## Justification

**Le besoin de l'essai tient en un appel, et seule une exclusion rejective peut servir tout générateur.** Un
générateur opaque n'expose aucune spécification à restreindre ; la seule exclusion qu'il admette est de regarder ce
qu'il a tiré. Un nouveau tirage borné est la forme que l'ADR-0033 accepte déjà, et il échoue bruyamment avec la graine
qui le rejoue, ce que faisaient les boucles écrites à la main, sans la borne.

**Deux garanties sous un même nom, écrites.** L'exclusion constructive refuse une contradiction à la déclaration ; la
rejective ne voit rien avant de tirer. Leur donner des noms différents ferait lire le générateur scaffoldé autrement que
le builder qu'il enveloppe ; garder un nom et consigner la différence garde le site d'appel uniforme et la garantie
connaissable, depuis le type statique que le lecteur voit.

**Les analyzers restent sur un terrain sûr.** Une règle qui raisonnerait sur l'exclusion rejective devrait prédire
l'issue d'un nouveau tirage borné ; ne lire que la constructive garde leurs verdicts exacts, et leurs pages disent
laquelle elles lisent.

**Bornée, donc un générateur épinglé échoue tout de suite.** Un générateur qui ne produit que la valeur exclue épuise
le budget dès son premier appel et le signale, plutôt que de boucler.

## Alternatives envisagées

### Laisser `any` lire une garde `a.Equals(b)` et émettre lui-même le nouveau tirage

Envisagé parce que le scaffolder lit déjà les gardes. Rejeté : une fois que le cœur le dit en un appel, élargir le
lecteur de gardes pour ce cas ajoute un mécanisme dont personne n'a besoin.

### Un nom différent pour l'exclusion rejective

Envisagé parce qu'il rendrait la garantie visible dans l'appel. Rejeté : une même intention se lirait de deux façons
selon la manière dont le générateur a été obtenu, ce qui est l'incohérence que l'essai a signalée.

## Conséquences

### Positives

* Tout générateur, scaffoldé ou écrit à la main, peut se voir dire de différer d'une valeur en un appel.
* Une boucle de nouveau tirage n'est plus à écrire, et un générateur qui ne produit rien d'autre échoue avec sa graine.

### Négatives

* `IAny<int> g = Any.Int32(); g.DifferentFrom(5)` prend l'exclusion rejective, sans rien au site d'appel pour le dire.

### Risques

* Un générateur dont le domaine tient la valeur exclue la plupart du temps peut épuiser le budget alors qu'une autre
  valeur existe ; le message énonce un budget épuisé, pas un domaine vide.

## Actions de suivi

* Les pages de JD023 et JD034 disent qu'elles ne lisent que le `DifferentFrom` constructif.

## Références

* [ADR-0033](0033-decide-a-constraint-surface-by-constructive-versus-rejective.fr.md) — contraintes constructives et
  rejectives.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — des nouveaux tirages bornés qui échouent
  explicitement.
* Issue #395 ; CORE-4 du second essai d'expérience développeur.
