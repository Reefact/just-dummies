# ADR-0067 | Rendre compte d'un pool filtré par une interface implémentée explicitement, et n'avertir de rien

🌍 🇬🇧 [English](0067-report-a-filtered-pool-through-an-explicit-interface.md) · 🇫🇷 Français (ce fichier)

**Status:** Proposed
**Proposed:** 2026-08-11
**Decision Makers:** Reefact

## Contexte

L'[ADR-0033](0033-decide-a-constraint-surface-by-constructive-versus-rejective.fr.md) a rendu un value set
fourni par l'appelant composable avec toutes les autres contraintes d'un générateur : une fois les valeurs
fournies il n'y a plus rien à construire, donc chaque contrainte déclarée devient un test que chaque valeur
passe ou échoue, et le domaine est l'ensemble des valeurs qui passent.

Une valeur que les contraintes rejettent quitte ce domaine **en silence**. Le seul résultat dont la
bibliothèque rend compte est un domaine *vidé*, levé à la déclaration sous forme de conflit nommant les
deux côtés. Entre « toutes les valeurs survivent » et « aucune ne survit », il n'y a aucun signal.

Ce silence a un coût pour un appelant en particulier : celui dont le value set est un **catalogue** — une
liste de prénoms, de codes devise, une table de fixtures — déclaré une fois, réutilisé dans toute une
suite, avec les invariants qui l'entourent déclarés à côté de chaque tirage. Quand une partie de ce
catalogue n'est jamais tirée, il y a exactement deux réparations : élargir l'invariant, ou corriger le
catalogue. Choisir entre les deux suppose de savoir *quelles* valeurs sont tombées et *quelle* contrainte
déclarée a emporté chacune.

La bibliothèque détient déjà ces faits. La spécification conserve côte à côte la liste de l'appelant et la
liste des survivantes, et elle dérive déjà quelles contraintes déclarées rejettent quelles valeurs, parce
que c'est cette dérivation qui permet à un message de conflit de nommer les deux côtés plutôt que d'accuser
le tableau de l'appelant.

Ces faits sont par ailleurs déjà atteignables de l'extérieur, **par accident**. Une collection distincte
arbitre à la déclaration sur la cardinalité survivante
([ADR-0004](0004-gate-distinct-collections-by-cardinality-else-bounded-draw.fr.md)) : sonder cet arbitrage
puis tirer un ensemble distinct de la taille exacte des survivantes reconstitue le pool survivant par la
seule surface publique. Le chemin est déterministe — l'arbitrage est une vérification à la déclaration, pas
un tirage — mais il coûte une déclaration par sondage, il repose sur une interaction conçue pour autre
chose (une valeur épinglée n'étend le domaine que lorsque le générateur n'aurait pas pu la tirer), et rien
ne promet qu'il continuera de fonctionner.

Les générateurs répondent déjà à cette classe de question par une interface qu'ils implémentent
**explicitement** : 25 d'entre eux portent une interface interne de cardinalité et d'appartenance, qui
existe pour que les collections distinctes puissent arbitrer. La forme est donc en place et seule sa
visibilité ne l'est pas — mais cette interface répond par un compte et un oui/non, et ne dit rien du
*pourquoi* une valeur est absente.

Quatre faits supplémentaires cadrent le choix :

* La bibliothèque n'a **aucun canal de compte rendu**. Elle n'écrit qu'à un seul endroit, sur le chemin
  d'échec, par un puits optionnel fourni par l'appelant, avec repli sur la sortie d'erreur standard quand
  il est absent ou qu'il lève.
* Les analyzers ne peuvent pas couvrir le cas. La règle qui analyse une chaîne de contraintes sur `string`
  cesse de raisonner sur le budget de longueur dès qu'un value set est déclaré, et aucune règle de ce type
  ne suit une chaîne d'appels à travers une variable — or un catalogue est une variable par nature.
* L'[ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.fr.md) trace la frontière de ce que la
  bibliothèque juge : un builder typé juge le domaine qu'il représente, et les entrées génériques ne jugent
  rien dans un pool fourni par l'appelant, parce que le type y est opaque et que le pool est toute la
  spécification.
* L'[ADR-0006](0006-materialize-dummies-only-through-generate.fr.md) refuse une conversion implicite vers le
  type généré, au motif qu'elle n'est ni bon marché, ni totale, ni référentiellement transparente, et
  qu'elle laisse un appelant oublier qu'un tirage a lieu.

Rien n'est publié : la base d'API publique ne porte que `#nullable enable`. Une interface publique ajoutée
maintenant ne coûte rien, et serait une version majeure après la `1.0`.

## Décision

Un générateur dont le domaine est un value set fourni par l'appelant rend compte de ce domaine — les
valeurs que les contraintes déclarées ont gardées, et celles qu'elles ont rejetées avec la contrainte qui a
rejeté chacune — par une interface dédiée qu'il implémente explicitement, atteignable seulement par un cast
délibéré et jamais annoncée de la propre initiative du générateur.

## Justification

**La question que ce silence laisse ouverte a exactement deux réponses, et les deux tiennent au même
fait.** Nommer la contrainte qui a emporté chaque valeur, c'est ce qui sépare *le catalogue est faux* de
*l'invariant est faux*. Un compte ne les sépare pas, un test d'appartenance non plus — et c'est
précisément pourquoi l'interface déjà en place répond à la mauvaise question, si commode que serait sa
publication.

**Rendre compte d'un domaine n'est pas matérialiser un dummy, donc le refus de conversion ne l'atteint pas
— et ses critères sont ici satisfaits plutôt que contournés.** L'ADR-0006 a refusé un membre qui n'était ni
bon marché, ni total, ni référentiellement transparent, et dont la pire propriété était de laisser un
appelant oublier qu'un tirage avait lieu. Une inspection ne tire rien : le domaine est fixé au moment où
les contraintes sont déclarées, la même question rend la même réponse à chaque appel et sous n'importe
quelle graine, et un cast explicite est le contraire d'un oubli.

**L'implémentation explicite est ce qui rend une fonctionnalité de diagnostic abordable.** Le coût d'une
telle fonctionnalité n'est pas le code qu'elle prend, c'est ce qu'elle fait à la surface que tous les
autres lisent. La chaîne fluide est la surface d'enseignement de la bibliothèque, et un membre qui répond à
une question de maintenance n'a pas sa place dans la même liste de complétion que les contraintes — sur
chaque générateur, pour chaque utilisateur qui ne la posera jamais. L'implémentation explicite l'en retire
entièrement, donc la fonctionnalité ne coûte exactement rien à qui n'en veut pas. C'est le raisonnement que
l'interface interne incarne déjà ; la décision le prolonge plutôt qu'elle ne l'invente.

**Le cast est la bonne ergonomie pour cette question, pas un contournement de limitation.** Inspecter une
recette, c'est sortir du contrat que le reste de la surface enseigne, à savoir que la sortie d'une recette
est une valeur. Un cast énonce cette intention à l'appel, là où un lecteur la voit.

**Rendre les faits garde la bibliothèque hors d'un jugement qui n'est pas le sien.** Un avertissement
exigerait un canal qu'elle n'a pas, se déclencherait là où personne ne lit — un test qui passe — et
reviendrait à décréter qu'un catalogue rétréci est une erreur. Il ne l'est pas : rétrécir un catalogue
partagé sur un appel précis est exactement ce à quoi la composition d'un value set avec une contrainte
*sert*. L'ADR-0054 place déjà le pool d'un appelant hors de ce que la bibliothèque juge ; rendre compte de
ce que les contraintes lui ont fait respecte cette ligne, l'en avertir non.

**L'information s'échappe déjà par une route que personne n'a dessinée, ce qui plaide pour décider plutôt
que pour laisser en l'état.** Le statu quo n'est pas « le domaine est privé » ; c'est « le domaine est
public par une interaction non conçue, à un coût, sans promesse attachée ». Une décision remplace un
accident.

**La fenêtre est ouverte maintenant et se referme à la première release.** Ajouter une interface publique à
une surface dont la base publiée est vide ne coûte rien aujourd'hui. Après la `1.0`, le même ajout est une
version majeure, et le silence devrait être subi ou payé — l'argument de calendrier même que l'ADR-0033 a
fait valoir en ouvrant le value set de chaîne à la composition.

## Alternatives envisagées

### Publier telle quelle l'interface de cardinalité existante

Envisagée parce qu'elle est déjà implémentée explicitement sur 25 générateurs : la publier coûterait un
changement de visibilité et rien d'autre — la version la moins chère possible de cette décision.

Rejetée parce qu'elle répond à une autre question. Un compte et un test d'appartenance ne permettent de
reconstituer la liste survivante qu'en la sondant valeur par valeur, et ne disent jamais quelle contrainte
a retiré une valeur — le fait auquel les deux réparations tiennent. Elle figerait en outre, dans la surface
publique, une interface dont la forme est due à une collaboration interne avec l'arbitrage des collections
distinctes, et qui devrait dès lors servir deux maîtres à la fois.

### Porter les membres sur les générateurs eux-mêmes

Envisagée parce qu'elle ne demande ni cast ni second type : les membres siégeraient sur les builders
fluides, découvrables par quiconque se demande ce qu'est devenu son pool.

Rejetée parce qu'elle facture à tout le lectorat une fonctionnalité de maintenance. La liste des
contraintes est ce que la surface fluide enseigne, et un membre d'inspection s'y installe sur chaque
générateur, dans chaque liste de complétion, pour chaque utilisateur qui ne posera jamais la question. La
découvrabilité est l'argument pour, et l'argument contre.

### Avertir à l'exécution quand une partie du pool est rejetée

Envisagée parce que c'est la forme que prend le besoin quand il est ressenti pour la première fois :
l'appelant veut qu'on lui *dise* que son catalogue a dérivé, pas avoir à le demander.

Rejetée sur trois points. La bibliothèque n'a qu'une écriture, sur le chemin d'échec, donc un
avertissement exige un canal inventé pour lui. Un avertissement sur un test qui passe est invisible, ce
qui défait la raison même de son ajout. Et il trancherait contre un usage légitime, puisque rétrécir un
catalogue partagé sur un appel précis est ce que la composition existe pour permettre — la bibliothèque
signalerait un défaut là où il y a une fonctionnalité.

### En rendre compte à la compilation, par un analyzer

Envisagée parce qu'un avertissement a vraiment sa place au build, dans l'IDE et en CI, et parce qu'il y a
précédent : plusieurs règles de contraintes avancent déjà au build ce que des arguments constants rendent
décidable.

Rejetée comme insuffisante plutôt que fausse. La règle sur `string` cesse de raisonner dès qu'un value set
est déclaré, et aucune règle de ce type ne suit une chaîne d'appels à travers une variable — or un
catalogue est une variable par nature, donc le cas qui motive cette décision est exactement celui qu'un
analyzer ne peut pas voir. Elle reste un complément utile pour un pool écrit à l'appel, et figure en action de suivi plutôt
qu'en alternative à cette décision.

### Laisser le filtrage à l'appelant

Envisagée parce qu'elle ne demande aucune API : un appelant peut filtrer son propre catalogue contre ses
propres invariants avant de le confier, et comparer lui-même les deux listes.

Rejetée parce qu'elle duplique les prédicats de la bibliothèque dans le code de l'appelant, où ils dérivent
des contraintes qu'ils reflètent — et la dérive est précisément la panne traitée. Elle rend en outre compte
d'une liste vidée par une erreur d'argument sur un tableau que l'appelant n'a jamais écrit, au lieu de
nommer les deux contraintes en jeu, ce qui est la régression que l'ADR-0033 a supprimée.

### Ne rien faire, et laisser le sondage par collection distincte

Envisagée parce qu'elle fonctionne déjà, de façon déterministe, par la surface publique.

Rejetée parce qu'elle coûte une déclaration par sondage, repose sur une interaction conçue dans un autre
but, et constitue une promesse que personne n'a faite : une refonte de l'arbitrage casserait des appelants
qui ignoraient en dépendre, et la panne surgirait loin de sa cause.

## Conséquences

### Positives

* La question de réparation a une réponse de plein droit, et un projet peut en faire un test qui verrouille
  son propre catalogue contre ses propres invariants — la vérification s'exécutant là où vit le catalogue.
* La surface fluide est inchangée, et la fonctionnalité ne coûte rien à qui ne caste pas pour l'obtenir.
* Aucun canal de compte rendu n'est inventé, et la bibliothèque continue de ne rien juger dans le pool d'un
  appelant.
* Le sondage non conçu cesse d'être la seule route vers un fait que la bibliothèque détient déjà.

### Négatives

* Une seconde interface publique à tenir en phase avec les générateurs, et un engagement public à nommer la
  contrainte qui rejette : la dérivation du coupable devient un contrat plutôt qu'un détail de fabrication
  de message.
* Deux niveaux à expliquer au lieu d'un. Un générateur est une recette dont la seule sortie est une valeur
  — sauf que certains répondront aussi à une question sur leur domaine.

### Risques

* **La portée par famille est la vraie décision laissée ouverte.** Toutes les familles portent un value set.
  Sur un seul générateur l'interface est une verrue ; sur toutes c'est un chantier, et les familles
  scalaires atteignent leur domaine par un espace ordinal où *les valeurs survivantes* est bien moins
  immédiat que sur une chaîne. Trancher cela par dérive recréerait l'asymétrie que l'ADR-0033 a supprimée.
* **Nommer le coupable est un jugement lorsque plusieurs contraintes rejettent la même valeur.** Les rendre
  toutes, dans l'ordre de déclaration, est la réponse évidente ; c'est aussi un contrat une fois publié.
* Le nom de l'interface est gelé à la `1.0` avec le reste de la surface.

## Actions de suivi

* Trancher la portée par famille avant d'implémenter — au minimum, si l'interface est optionnelle, avec un
  appelant qui teste sa présence, ou portée par tout générateur qui admet un value set.
* Arrêter le nom de l'interface pendant que la surface est encore libre de changer.
* Envisager le complément par analyzer pour un pool écrit à l'appel, que cette décision laisse non couvert
  plutôt qu'elle ne le refuse.
* Documenter la fonctionnalité pour les utilisateurs, en anglais et en français, si la décision est
  acceptée.

## Références

* [ADR-0033](0033-decide-a-constraint-surface-by-constructive-versus-rejective.fr.md) — la composition dont
  cet enregistrement rend compte.
* [ADR-0004](0004-gate-distinct-collections-by-cardinality-else-bounded-draw.fr.md) — l'arbitrage des
  collections distinctes, dont la vérification à la déclaration rend le domaine observable aujourd'hui.
* [ADR-0006](0006-materialize-dummies-only-through-generate.fr.md) — la frontière de matérialisation que
  cette décision ne franchit pas.
* [ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.fr.md) — la ligne entre ce qu'un builder typé
  juge et ce qu'il ne doit pas juger dans le pool d'un appelant.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — la frontière d'ambition à
  laquelle toute nouvelle capacité est mesurée.
* [ADR-0042](0042-carry-a-declared-constraint-as-a-value-object.fr.md) — le value object dont une contrainte
  déclarée est déjà porteuse, et qu'une rejection rendue nomme.
