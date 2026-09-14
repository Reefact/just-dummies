# ADR-0101 | Accepter l'exclusion d'une valeur non finie sur un générateur flottant comme une opération sans effet

🌍 🇬🇧 [English](0101-accept-a-non-finite-exclusion-as-a-no-op.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-09-14
**Accepted:** 2026-09-14
**Decision Makers:** Reefact

Restreint l'[ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.fr.md) sur un seul point : la règle
applicable aux arguments des exclusions flottantes. Tout le reste de ce qu'a décidé l'ADR-0054 reste en
vigueur.

## Contexte

L'[ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.fr.md) a décidé qu'un générateur typé *tire, et
accepte comme argument, uniquement des valeurs valides du domaine qu'il représente*. Sur les générateurs
flottants (`Any.Double()`, `Any.Single()`, `Any.Half()`), cette règle reposait sur une garde unique appliquée à
tous les arguments indistinctement : une borne, une valeur passée à `OneOf`, une valeur passée à `Except` ou à
`DifferentFrom` étaient toutes rejetées lorsqu'elles n'étaient pas finies, avec la même phrase. Parmi les
alternatives que l'ADR-0054 a examinées puis écartées figurait *« Limiter la règle aux tirages, pas aux
arguments »* — accepter une valeur non finie *comme borne ou comme exclusion* — au motif qu'*« une borne
impossible à comparer ne peut pas participer à un modèle ordonné, de sorte que la garde devrait être réintroduite
plus bas, là où l'échec se manifesterait par une valeur incorrecte plutôt que par un argument rejeté »*, et
qu'un `NaN` parvenant jusqu'à une règle de distinction fondée sur un comparateur serait dédoublonné alors que le
`==` de l'appelant y verrait deux valeurs distinctes.

Les deux sortes d'arguments n'entrent pas dans le moteur de la même manière. Une borne et une valeur du vivier
**définissent** le domaine : le moteur d'intervalle continu compare les bornes, échantillonne entre elles et
parcourt l'échelle des valeurs représentables à partir d'elles, et une valeur du vivier est tirée telle qu'elle a
été fournie. Une exclusion ne fait que **retrancher** d'un domaine défini ailleurs : elle est honorée par un test
d'égalité sur chaque candidat, et ses seuls autres usages — le contrôle de satisfiabilité et le message
d'épuisement — ne prennent une exclusion en compte que lorsqu'elle se situe à l'intérieur des bornes. Toute
comparaison ordonnée avec `NaN` est fausse, et aucune valeur tirée n'est jamais non finie : une exclusion non
finie ne peut donc correspondre à aucun candidat, ne se situe à l'intérieur d'aucune borne, et n'est jamais ni
comptée ni nommée. Qu'elle soit conservée ou rejetée, les valeurs produites par le générateur sont les mêmes.

La bibliothèque dispose déjà d'une règle pour un argument qui demande ce qui est déjà garanti. *« Redéclarer la
MÊME contrainte n'est pas une contradiction : c'est une opération sans effet plutôt qu'un conflit, car la seconde
déclaration demande exactement ce que la première garantit déjà »* est écrit à dix endroits dans les moteurs,
vaut pour chaque contrainte déclarée une seule fois, et a été étendu aux fragments de chaîne répétés par le
changement qui a clos [#181](https://github.com/Reefact/just-dummies/issues/181). Exclure une valeur que le
générateur ne tire jamais relève de la même logique : c'est déclarer ce qui est déjà vrai.

`DifferentFrom` est documenté comme prenant *« typiquement une valeur existante que le test détient déjà »*.
Une telle valeur est souvent issue d'un calcul plutôt qu'écrite en toutes lettres, et un calcul peut fort bien
produire un infini ; avec la garde uniforme, une exclusion pourtant sans effet se transformait alors en
`ArgumentException` au moment où le test met en place ses données.

Le message de rejet indiquait la porte de sortie — *« utilisez un vivier explicite : `Any.OneOf(...)` »* — pour
tous les arguments indistinctement. Émis depuis `OneOf`, ce conseil désigne l'appel que le lecteur vient
d'écrire ; émis depuis une exclusion, il propose un moyen d'*obtenir* une valeur que l'appelant cherchait
précisément à *exclure*. Ces deux défauts ont été signalés dans
[#180](https://github.com/Reefact/just-dummies/issues/180), ouverte par le mainteneur lors d'une revue des cas
limites de toute la bibliothèque, cinq semaines après l'acceptation de l'ADR-0054 ; la question des exclusions
y était explicitement laissée ouverte, comme une décision à prendre.

La porte de sortie prévue par l'ADR-0054 n'est ni modifiée ni remise en cause ici : un test dont le domaine
contient réellement une valeur non finie la tire du `Any.OneOf(...)` générique, qui n'impose aucune règle de
finitude, par construction.

La bibliothèque n'a pas atteint la version 1.0, et un changement de comportement y est permis
([`CONTRIBUTING.md`](../../for-users/CONTRIBUTING.fr.md), « Base de référence de l'API publique »).

## Décision

Un générateur flottant accepte une valeur non finie passée à `Except` ou à `DifferentFrom` sans lui donner
d'effet, tandis qu'une borne ou une valeur du vivier de `OneOf` non finie reste rejetée, comme l'a décidé
l'ADR-0054.

## Justification

**Exclure une valeur qui ne peut pas être tirée revient à redéclarer une garantie que le générateur offre déjà,
et la règle de la bibliothèque pour une garantie redéclarée est qu'elle n'a aucun effet.** Le principe qui fait
qu'un second `Between(0, 100)` n'a aucun effet, et qu'un second `Containing("ab")` n'a aucun effet, fait
qu'`Except(double.NaN)` n'a aucun effet : l'appelant demande exactement ce qui est déjà vrai. Le rejeter était le
seul endroit où la bibliothèque traitait une garantie redéclarée comme une erreur.

**La distinction tient en une phrase, et elle relève d'un principe, non d'une exemption.** Un argument qui
viderait la contrainte de son sens — une borne impossible à comparer, une valeur du vivier qui serait tirée —
est rejeté ; un argument qui ne fait que redéclarer une garantie déjà tenue n'a aucun effet. Cette phrase couvre
chaque point d'entrée flottant sans avoir à les énumérer, et c'est celle dont un lecteur a besoin pour voir dans
les deux comportements une seule règle plutôt qu'une incohérence.

**L'argument de l'ADR-0054 fondé sur le moteur vaut pour les bornes et n'atteint pas les exclusions.** *« Une
borne impossible à comparer ne peut pas participer à un modèle ordonné »* est vrai, et la présente décision en
conserve toutes les conséquences : une borne non finie reste rejetée. Une exclusion, elle, n'entre jamais dans le
modèle ordonné. La seconde inquiétude de l'ADR-0054 — un `NaN` dédoublonné par un comparateur — suppose qu'un
`NaN` parvienne jusqu'à un tirage ou jusqu'à un vivier de distinction, ce qu'une exclusion sans effet ne produit
jamais. L'alternative écartée a rangé les exclusions avec les bornes par généralisation, sans argument propre
aux exclusions ; la présente décision les sépare et laisse le verdict sur les bornes là où il était.

**Un rejet mérite sa place lorsqu'il protège quelque chose.** Le *« refuser bruyamment à la frontière »* de
l'ADR-0046 est la bonne réponse lorsque honorer la demande exigerait un mécanisme dont personne ne peut rendre
compte, ou laisserait passer une valeur incorrecte. Ici, rien n'est en jeu : les valeurs produites sont
identiques dans les deux cas. Ce que le rejet faisait, c'était bloquer une mise en place légitime —
`DifferentFrom(existant)` sur une valeur qu'un calcul a rendue infinie — sans rien apprendre au lecteur qu'il
puisse corriger. Une friction qui ne protège rien n'est pas une sécurité.

**Exclure une valeur non finie, ce n'est pas la rechercher.** La doctrine de l'ADR-0054 — un appelant qui
réclame `NaN` réclame le cas à tester, lequel a sa place au point d'appel sous forme de littéral — porte sur
l'*obtention* de la valeur. Un appelant qui l'exclut manifeste son indifférence à son égard, soit l'intention
inverse ; la doctrine ne s'applique pas.

## Alternatives envisagées

### Continuer à rejeter l'exclusion, et ne corriger que le message

L'autre voie ouverte par #180 : conserver la garde, et lui faire dire qu'une valeur non finie n'est jamais
tirée, de sorte que l'exclusion n'a rien à retrancher. Envisagée parce qu'elle conserve une règle uniforme sur
les arguments et n'exige aucune décision. Écartée parce que la règle uniforme est la mauvaise règle pour une
exclusion — c'est le seul endroit où une garantie redéclarée est traitée comme une erreur — et parce qu'un
`DifferentFrom` sur une valeur calculée continue d'en faire les frais. La correction du message est conservée
quoi qu'il en soit, pour les bornes et pour `OneOf`.

### Abandonner entièrement la règle applicable aux arguments

Accepter également une borne et une valeur du vivier non finies — l'alternative que l'ADR-0054 a écartée dans
son intégralité. Écartée pour les motifs mêmes de l'ADR-0054, que la présente décision laisse intacts : une borne
impossible à comparer n'a pas sa place dans un modèle ordonné, et une valeur du vivier non finie serait tirée.

### Remplacer l'ADR-0054 dans son ensemble

Écartée parce que presque tout ce qu'elle décide reste valable : la règle sur les tirages, les verdicts sur les
bornes et sur le vivier, l'exemption des points d'entrée génériques et la porte de sortie. Une décision de
remplacement qui reformulerait tout cela pour n'en changer qu'une clause noierait le changement ; restreindre la
clause en question, à côté de la décision qu'elle restreint, garde les deux lisibles.

## Conséquences

### Positives

* `DifferentFrom(existant)` est sûr quel que soit le résultat d'un calcul, ce que sa documentation promettait.
* Le partage entre arguments rejetés et arguments acceptés s'explique en une phrase qu'un lecteur peut retenir.
* Le message de rejet ne propose plus un vivier à un appelant qui excluait une valeur, et ne conseille plus
  `OneOf` à un appelant qui vient de l'écrire.

### Négatives

* Deux comportements à documenter sur un même générateur au lieu d'un seul ; le readme du package énonce la
  règle.
* Un appelant qui se servait du rejet comme signal d'alerte pour un `NaN` produit en amont perd ce signal. La
  bibliothèque ne l'a jamais promis, et le véritable échec du test se manifeste là où le `NaN` est utilisé.

### Risques

* L'absence d'effet repose sur un fait propre au moteur — une exclusion n'est jamais soumise à une comparaison
  ordonnée, et aucun tirage n'est jamais non fini. Une évolution future du moteur qui comparerait les exclusions,
  ou tirerait une valeur non finie, devrait explicitement préserver la présente décision ; la suite de tests par
  propriétés verrouille ce comportement, de sorte qu'une telle évolution ferait échouer un test plutôt qu'un
  utilisateur.
* « Déjà garanti » doit s'entendre strictement : cela vaut pour une valeur exclue dont on peut démontrer qu'elle
  n'est jamais tirée. Ce n'est pas une licence pour accepter n'importe quel argument invalide qui se trouverait
  être inoffensif sur un générateur donné.

## Actions de suivi

* La recette *NaN et les infinis* du readme du package et la page consacrée aux nombres, en anglais et en
  français, énoncent la règle restreinte dans le changement même qui livre le comportement.
* Que le statut de l'ADR-0054 devienne *Superseded by ADR-0101* ou reste *Accepted* à côté de la présente
  restriction relève du mainteneur au moment de l'acceptation ; la présente décision n'y touche pas.

## Références

* [ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.fr.md) — la règle que la présente décision
  restreint, et l'alternative écartée qu'elle rouvre pour les seules exclusions.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — quand un rejet est la réponse
  honnête.
* [ADR-0097](0097-follow-the-declared-bounds-with-the-ordinary-magnitude-window.fr.md) — quelles valeurs finies
  sont tirées, la question voisine.
* [Issue #180](https://github.com/Reefact/just-dummies/issues/180) — les deux messages qui manquent leur cible, et
  la question ouverte que la présente décision tranche.
* [Issue #31](https://github.com/Reefact/just-dummies/issues/31) — la recette du readme qui a nommé la porte de
  sortie en premier.
