# ADR-0061 | Tirer du contexte ambiant et ne détenir aucun état

🌍 🇬🇧 [English](0061-draw-from-the-ambient-context-and-hold-no-state.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-07-30
**Accepted:** 2026-08-09
**Decision Makers:** Reefact

> Les renvois de section (§N) pointent vers la [spécification de `dum`](../specifications/justdummies-tool.fr.md), le document dont cet enregistrement a été extrait.

## Contexte

La bibliothèque offre deux mécanismes de reproductibilité. Le contexte **ambiant** est épinglé par
un scope (`Dummy.UseSeed`, `Dummy.Reproducibly`) et suit le contexte d'exécution ; le contexte **isolé**
est créé par `Dummy.WithSeed` et porte sa propre source aléatoire fixe, insensible à tout scope.

Chaque fabrique statique `Dummy.*` capture l'objet source ambiant, et cette source résout la frame
`AsyncLocal` courante **au moment du `Generate()`**, pas à la construction du generator (§14.5).

`DummyContext` reflète les points d'entrée primitifs, motif, URI et choix comme méthodes d'instance.
Il ne reflète **pas** les points d'entrée de collection ni de composition (§14.2).

Le type émis porte une surcharge `With{Param}(IDummy<TParam>)` pour chaque paramètre ([ADR-0057](0057-make-the-emitted-generator-a-first-class-iany.fr.md)). Il est
construit une fois et peut être générateur de plusieurs valeurs, possiblement dans des scopes
différents.

Deux analyzers, `JD009` et `JD020`, signalent les tirages depuis un initialiseur statique et les
contextes statiques partagés. Le fichier émis est analysé comme du code écrit à la main ([ADR-0058](0058-leave-the-scaffolded-file-open-to-the-analyzers.fr.md)).

## Décision

Le generator émis construit sa recette à partir de la seule façade statique `Dummy`, sans détenir de
source aléatoire, de seed ni d'état statique propre.

## Justification

La résolution au moment du tirage est ce qui rend cela gratuit. Une recette construite hors d'un
scope de reproductibilité et générée dedans reste épinglée par ce scope : le type émis n'a donc
besoin d'aucune règle de cycle de vie — on le construit là où ça se lit le mieux, on le génère là où
le seed compte. Toute conception capturant une source à la construction devrait spécifier ce cycle
de vie, et dire ce qui arrive quand le generator survit au scope qui l'a vu naître.

Ne détenir aucun état statique est ce qui laisse `JD009` et `JD020` sans rien à signaler. Le fichier
émis étant analysé, un émetteur qui mettrait quoi que ce soit en cache statique serait signalé dans
le build du développeur et non dans le nôtre — le diagnostic serait juste, et le tool serait le
fautif.

Supporter le contexte isolé signifierait un second constructeur et un second chemin de recette à
travers `DummyContext`. Ce chemin ne pourrait pas exprimer toutes les lignes du §5.2, puisque
`DummyContext` ne reflète aucun point d'entrée de collection ni de composition : la surface serait
plus grande *et* moins capable. Le cas est déjà couvert sans rien ajouter : un développeur sur
`WithSeed` passe les generators de ce contexte paramètre par paramètre, via la surcharge que [ADR-0057](0057-make-the-emitted-generator-a-first-class-iany.fr.md)
fournit déjà.

## Alternatives considérées

##### Capturer un seed à la construction

Considérée parce qu'un generator qui possède son seed est autonome et manifestement reproductible,
sans rien d'ambiant à raisonner.

Écartée parce qu'elle duplique un mécanisme que la bibliothèque possède déjà, et parce que deux
generators de ce type dans un même test tireraient de séquences indépendantes — aucun seed unique
rapporté par un test en échec ne pourrait alors rejouer l'exécution dans son ensemble, ce qui est
précisément la propriété que la reproductibilité de la bibliothèque existe pour offrir.

##### Un second constructeur prenant un `DummyContext`

Considérée parce qu'elle referme le manque pour un développeur travaillant avec `Dummy.WithSeed`, qui
est une façon supportée d'utiliser la bibliothèque.

Écartée pour la v1.0 parce que `DummyContext` ne reflète qu'une partie de la façade — le second chemin
ne saurait pas résoudre les paramètres collection ni composés — et parce que la surcharge par
paramètre couvre déjà le cas sans coût de surface. Laissée ouverte au §16.

## Conséquences

**Positives.** Aucune règle de cycle de vie, aucun état statique. La garantie de reproductibilité du
§8.2 vient gratuitement, et les deux analyzers de seeding n'ont rien sur quoi se déclencher.

**Négatives.** Un développeur utilisant `Dummy.WithSeed` ne peut pas confier le contexte entier au
generator et doit fournir les generators paramètre par paramètre, ce qui est verbeux pour un
constructeur large.

**Risques.** Un futur émetteur qui mémoïserait quoi que ce soit — generator en cache, instance
partagée — casserait d'un coup la garantie de reproductibilité et la propreté vis-à-vis des
analyzers. Le test de compilation de la sortie attrape la seconde ; seul un test de reproductibilité
attrape la première, et c'est celui qu'on oublie.

## Actions de suivi

* Conserver un test assertant qu'une recette construite **hors** d'un scope y rejoue dedans. C'est
  la forme exécutable de cette décision ; le §17 consigne l'exécution manuelle qu'il doit remplacer.

## Références

* §8.2, §14.2, §14.5, §16 de cette spécification ; [ADR-0057](0057-make-the-emitted-generator-a-first-class-iany.fr.md) et [ADR-0058](0058-leave-the-scaffolded-file-open-to-the-analyzers.fr.md) de cette section.

---
