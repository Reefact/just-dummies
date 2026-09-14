# ADR-0100 | Identifier un ensemble de valeurs déclaré par ses valeurs, non par l'appel qui l'a déclaré

🌍 🇬🇧 [English](0100-identify-a-value-set-by-its-values.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-09-14
**Accepted:** 2026-09-14
**Decision Makers:** Reefact

## Contexte

L'[ADR-0042](0042-carry-a-declared-constraint-as-a-value-object.fr.md) a fait de la contrainte
déclarée un objet-valeur qui se rend lui-même, et lui a donné une égalité de façon délibérée et non
par commodité : une vingtaine de comparaisons, réparties dans les spécifications, décident si une
seconde déclaration est une redéclaration inoffensive — qui renvoie le générateur inchangé — ou un
vrai conflit. Cette égalité porte sur le texte, comparé de façon ordinale : deux contraintes sont
égales quand elles se lisent pareil.

Pour une contrainte dont les arguments sont positionnels et scalaires, se lire pareil et être
identiques sont une seule et même question. `Between(0, 100)` écrit deux fois ne rend qu'une chaîne ;
`Between(0, 100)` à côté de `Between(5, 50)` en rend deux. Décider l'identité sur le texte ne perd
rien.

`OneOf` n'est pas une contrainte de cette sorte, et sa propre documentation le dit : les doublons
sont ignorés, et rien n'est promis quant à l'ordre. `OneOf("a", "b")`, `OneOf("b", "a")` et
`OneOf("a", "b", "b")` déclarent donc un seul domaine et rendent trois chaînes différentes.

D'autres faits encadrent le choix :

* Neuf générateurs acceptent un ensemble de valeurs fourni par l'appelant — `AnyChar`,
  `AnyEnum<TEnum>`, `AnyGuid`, `AnyPattern`, la spécification de chaînes et les quatre moteurs
  d'intervalle. Jusqu'à l'[issue #185](https://github.com/Reefact/just-dummies/issues/185), les neuf
  comparaient l'appel rendu, si bien que deux de ces trois écritures étaient refusées comme un
  second ensemble contradictoire. `AnyPattern` a été corrigé là où l'audit l'a trouvé ; pas les huit
  autres, ce qui laissait la surface en désaccord avec elle-même sur ce qu'est un domaine.
* Le conflit produit par une telle comparaison est un conflit sur lequel l'appelant ne peut rien.
  « Cannot apply `OneOf("b", "a")` because `OneOf("a", "b")` is already defined » nomme deux
  contraintes qui demandent la même chose, et il n'y a rien à desserrer dans l'une ni dans l'autre.
* Chacun des neuf conserve déjà l'ensemble **tel que déclaré**, dédupliqué, à côté de l'ensemble qui
  survit aux autres contraintes — l'un alimente les diagnostics, l'autre le tirage. L'identité dont
  cette décision a besoin est donc déjà stockée et n'exige aucun état nouveau.
* L'appel rendu reste nécessaire, pour une raison que la question de l'égalité ne touche pas : un
  conflit cite à l'appelant ses propres mots, ce qui participe de ce qui fait lire une contradiction
  dans l'`Arrange` d'un test comme un défaut de ce test.
* `ConstraintCall.OfElided` existe pour un vivier dont le type d'élément est opaque à la
  bibliothèque, et dont elle ne doit pas rendre les arguments. Une telle contrainte n'a aucun
  argument rendu à comparer : le texte n'allait donc pas non plus être la réponse générale à
  l'identité d'un ensemble.

## Décision

Une contrainte dont l'argument est un ensemble de valeurs est identifiée, à la redéclaration, par cet
ensemble et non par le texte de l'appel qui l'a déclarée.

## Justification

**Le contrat le disait déjà, seul le code le contredisait.** `OneOf` documente les doublons comme
ignorés et ne promet rien sur l'ordre, ce qui revient à énoncer que la contrainte est l'ensemble et
non l'écriture. Une comparaison qui refuse une permutation ne fait pas respecter une règle que la
bibliothèque possède : elle en contredit une que la bibliothèque a publiée.

**Un conflit sur lequel on ne peut rien vaut moins que pas de conflit du tout.** Toutes les autres
contradictions que cette bibliothèque lève nomment deux contraintes entre lesquelles l'appelant peut
choisir. Un conflit de redéclaration portant sur un seul domaine nomme deux appels qui s'accordent, et
envoie le lecteur chercher un désaccord qui n'existe pas. Le message est bien formé et le diagnostic
qu'il invite à poser est faux, ce qui est précisément le mode de défaillance que toute la lignée
« erreurs de première classe » de la base cherche à éviter.

**C'est un écart par rapport à la forme de l'ADR-0042, et c'est l'écart étroit.** Ce record a déplacé
l'égalité dans le type justement pour que les sites de comparaison cessent de porter du comportement,
et la présente décision rend sa comparaison à une classe de contraintes. Si elle ne rouvre pas le cas
que l'ADR-0042 a tranché, c'est que les deux répondent à des questions différentes : l'ADR-0042 traite
de l'endroit où vivent *le rendu et son égalité*, et il les y garde ; ici il s'agit d'une contrainte
dont l'identité n'a jamais été son rendu. Les neuf sites ne réimplémentent pas une égalité — ils
comparent les ensembles que les générateurs détiennent déjà, et continuent d'utiliser `ConstraintCall`
pour chaque contrainte scalaire qu'ils déclarent.

**Trancher cela dans `ConstraintCall` rapporterait moins que le coût.** Porter les valeurs à côté du
texte rendu mettrait la règle en un seul endroit, ce qui est la forme que défend l'ADR-0042, mais cela
boxerait chaque élément de chaque ensemble pour une comparaison qui n'a lieu qu'une fois par
déclaration, donnerait au type deux sortes d'égalité à expliquer, et laisserait quand même `OfElided`
sans réponse. La règle mérite d'être énoncée une fois ; elle ne mérite pas une seconde identité sur le
type par lequel passe chaque contrainte de la bibliothèque.

**Une règle qui mérite d'être énoncée mérite une garde.** Neuf copies d'un commentaire, c'est
exactement ainsi que la dérive s'est installée : l'audit a trouvé un site, et les huit autres ont gardé
le défaut parce que rien ne les comparait. Une garde par réflexion, qui tire sa matière des
constructeurs eux-mêmes, tient la règle sur les générateurs existants comme sur ceux qui ne sont pas
encore écrits — c'est le mouvement qu'a fait l'[ADR-0098](0098-offer-every-oneof-in-both-shapes.fr.md)
pour la forme de `OneOf`.

## Alternatives envisagées

### Porter l'identité de l'ensemble dans `ConstraintCall`

Envisagée en premier, parce que c'est ce vers quoi pointe l'argument même de l'ADR-0042 : l'égalité
appartient au type plutôt qu'à chaque site de comparaison, et un ensemble de valeurs est justement le
cas où le type répond aujourd'hui de travers.

Rejetée sur le coût, non sur le principe. La contrainte devrait porter ses valeurs sous forme
d'objets à côté du texte qu'elle rend, en boxant chaque élément de chaque ensemble déclaré ; le type
par lequel passe chaque contrainte de la bibliothèque gagnerait une seconde notion d'égalité, qu'il
faudrait expliquer à chaque site qui ne l'utilise *pas* ; et la contrainte à vivier opaque n'aurait
toujours aucune valeur à comparer. Les neuf sites qui ont besoin de la règle détiennent déjà
l'ensemble : la centralisation déplacerait le code sans supprimer le concept.

### Normaliser le rendu — trier et dédupliquer avant de rendre

Envisagée parce qu'elle n'exigerait aucune comparaison nouvelle : si le texte était canonique,
l'égalité textuelle serait déjà l'égalité d'ensembles, et chaque site existant deviendrait correct
sans être touché.

Rejetée parce qu'elle rompt le contrat pour lequel le rendu existe. Un message de conflit cite à
l'appelant ses propres mots ; un rendu trié et dédupliqué lui cite des mots qu'il n'a pas écrits, et
le fait précisément dans le message qui lui demande d'aller retrouver la ligne. Ce serait en outre une
modification silencieuse de chaque diagnostic nommant un vivier.

### Documenter que l'écriture fait partie de la déclaration

Envisagée comme la réponse la moins chère, et la seule qui n'exige aucun code : énoncer qu'un ensemble
de valeurs doit être redéclaré à l'identique, et la comparaison existante devient correcte par
définition.

Rejetée parce qu'elle publierait une règle pour protéger un détail d'implémentation. L'appelant n'y
gagne rien, elle contredit ce que `OneOf` documente déjà des doublons et de l'ordre, et elle ferait du
`Distinct` que la bibliothèque applique elle-même aux valeurs — dans chacun des neuf sites — une étape
dont le résultat ne correspondrait plus à ce que la contrainte prétend être.

## Conséquences

### Positives

* Redéclarer un domaine est le no-op que la surface promet, sur tout générateur prenant un ensemble
  de valeurs, et non sur le seul qu'un audit a eu l'occasion d'atteindre.
* Un conflit portant sur un ensemble de valeurs signifie désormais que les ensembles diffèrent
  réellement : le message nomme donc quelque chose sur quoi l'appelant peut agir.
* La règle est tenue par une garde qui construit les générateurs et tire sa propre matière : un
  générateur ajouté plus tard est couvert sans que la garde soit modifiée.

### Négatives

* Neuf sites comparent un ensemble là où l'ADR-0042 n'avait laissé qu'un opérateur de comparaison :
  le lecteur de l'un d'eux rencontre donc une règle locale avant de rencontrer ce record.
* La comparaison alloue un ensemble par déclaration, là où la précédente comparait deux chaînes. Elle
  s'exécute une fois par contrainte déclarée, jamais à chaque tirage.

### Risques

* **Un générateur pourrait détenir un ensemble rétréci et comparer la mauvaise chose.** La règle a
  besoin de l'ensemble *tel que déclaré* ; comparer l'ensemble ayant survécu aux autres contraintes
  refuserait une redéclaration légitime faite après un rétrécissement. Tous les générateurs stockent
  aujourd'hui les deux, et la seule méthode qui rétrécirait sur place l'ensemble déclaré —
  `OrdinalIntervalSpec.NarrowingAllowed` — n'a aucun appelant. La garde ne couvre pas ce cas,
  puisqu'elle déclare l'ensemble avant tout rétrécissement.

## Actions de suivi

* Aucune. Les huit sites restants, la garde et l'entrée de changelog atterrissent ensemble.

## Références

* [ADR-0042](0042-carry-a-declared-constraint-as-a-value-object.fr.md) — le record dont celui-ci
  s'écarte, et la raison pour laquelle l'écart est étroit.
* [ADR-0098](0098-offer-every-oneof-in-both-shapes.fr.md) — la règle jumelle sur la forme de `OneOf`,
  tenue par le même genre de garde.
* [ADR-0099](0099-offer-oneof-wherever-the-generator-would-otherwise-build.fr.md) — où un ensemble de
  valeurs est proposé.
* [Issue #185](https://github.com/Reefact/just-dummies/issues/185) — l'élément d'audit dont la revue a
  trouvé le défaut sur `AnyPattern`.
