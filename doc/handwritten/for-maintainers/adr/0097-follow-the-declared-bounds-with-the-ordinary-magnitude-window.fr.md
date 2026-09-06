# ADR-0097 | Faire suivre les bornes déclarées par la fenêtre de magnitude ordinaire

🌍 🇬🇧 [English](0097-follow-the-declared-bounds-with-the-ordinary-magnitude-window.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-09-05
**Accepted:** 2026-09-06
**Decision Makers:** Reefact

Supersède l'[ADR-0031](0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.fr.md).

## Contexte

L'[ADR-0031](0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.fr.md) a décidé qu'une valeur
flottante ou décimale arbitraire est tirée dans une magnitude ordinaire d'un million, la fenêtre rétrécissant
l'intervalle déclaré et **ne s'effaçant que là où elle le laisserait vide**. Elle a été acceptée le
2026-07-28 sur des mesures : un échantillonnage uniforme sur tout le domaine d'un type plaçait 16,1 % des
doubles positifs là où une seule multiplication déborde vers l'infini, 17,1 % des décimaux là où la même
multiplication lève, et laissait `WithScale(2)` satisfaite par 5 000 tirages sur 5 000 sans en contraindre
aucun.

Ces mesures tiennent toujours. Ce que fait la couture de la règle est une autre affaire. Rétrécir un
intervalle à la fenêtre peut le laisser vide, mais peut aussi ne lui laisser **exactement qu'une valeur** —
ce qui survient lorsqu'une borne déclarée tombe précisément sur le bord de la fenêtre. La règle telle
qu'écrite conserve ce point unique, et un tirage entre une valeur et elle-même n'est pas un tirage. Mesuré
sur `main` :

| déclaré | tiré |
| --- | --- |
| `Any.Double().Between(1_000_000, 5_000_000)` | la constante `1000000`, à chaque fois |
| `Any.Single().Between(1e6f, 2e6f)` | la constante `1000000` |
| `Any.Double().Between(-2e6, -1e6)` | la constante `-1000000` |
| `Any.Decimal().Between(1_000_000m, 5_000_000m).WithScale(2)` | la constante `1000000.00` |
| `Any.Decimal().GreaterThan(1_000_000m)` | `AnyGenerationException`, à chaque fois |
| `Any.Double().GreaterThanOrEqualTo(1e6)` | la constante `1000000` |
| `Any.Double().GreaterThan(1e6)` | des valeurs autour de `1e308` |

L'échec décimal est la même couture vue de l'autre côté : une borne inférieure stricte sur un décimal est
une borne inclusive plus une exclusion ponctuelle, donc l'intervalle effondré sur une valeur, le candidat
tiré est justement la valeur exclue, et le plus petit incrément décimal s'évanouit dans l'arrondi au-delà
d'une magnitude d'environ dix. Le générateur n'a plus aucune valeur à rendre.

Les deux dernières lignes sont distantes d'un ulp dans ce que l'appelant a déclaré et de 300 décades dans ce
qu'il reçoit, parce qu'une borne stricte déplace l'intervalle juste au-delà du bord de la fenêtre et emprunte
donc la branche de l'effacement plutôt que celle de l'effondrement. `Between(1e6, double.MaxValue)` et
`GreaterThanOrEqualTo(1e6)` désignent le même ensemble et sont lus comme deux cas distincts par le test
« vide » de la règle.

Rien de tout cela n'a été détecté. Les propriétés d'atteignabilité de `JustDummies.PropertyTests` énoncent
leur attendu en recalculant la fenêtre avec la formule de l'implémentation elle-même : un intervalle
effondré sur un point effondrait l'attendu avec lui. Et leurs générateurs d'intervalles atteignent une borne
valant exactement un million avec une probabilité de l'ordre de 1e-6, si bien qu'aucun cas n'a jamais
exercé la couture.

Deux faits supplémentaires pèsent sur le correctif. Un million est une constante **choisie**, justifiée dans
l'ADR-0031 par le fait qu'elle laisse à un `double` « environ neuf chiffres significatifs sous la virgule » —
mesuré, une magnitude de `5e6` en laisse exactement 9,0, et toute valeur de `[1e6, 5e6]` survit à la
multiplication et au test `x + 1 != x` dont ce record faisait sa preuve. Et `Half` s'arrête à 65 504 : son
domaine entier tient dans la fenêtre, la couture y est inatteignable.

## Décision

Une valeur flottante ou décimale arbitraire est tirée de son intervalle déclaré rétréci à une magnitude
ordinaire d'un million, et là où ce rétrécissement laisserait moins de deux valeurs, la fenêtre suit les
bornes que l'appelant a écrites au lieu de s'effacer : un intervalle explicitement borné est tiré en entier,
une contrainte unilatérale est tirée sur la largeur propre de la fenêtre reportée à la borne qu'elle déclare,
et une borne unilatérale hors de portée de cette largeur retombe sur le domaine déclaré.

## Justification

* **L'objectif était juste, seul le mécanisme était approximatif.** Toutes les mesures sur lesquelles repose
  l'ADR-0031 restent vraies, et cette décision n'en change aucune : un tirage non contraint, une magnitude
  simplement permise et une contrainte d'échelle se comportent exactement comme ce record le spécifiait. Ce
  qui change, c'est le traitement d'une forme que sa règle ne distinguait pas — un rétrécissement qui laisse
  une valeur plutôt qu'aucune — et cette forme est un défaut en soi : un générateur dont chaque tirage rend
  le même littéral est précisément la constante choisie à la main que cette bibliothèque existe pour
  remplacer.
* **Un dummy constant échoue au même travail qu'une valeur extrême.** L'argument propre de l'ADR-0031 est
  qu'un dummy ne doit pas devenir la cause d'un échec que le test n'a jamais nommé. Une valeur qui ne varie
  jamais est pire que bruyante : elle fait passer un test pour une raison que personne n'a écrite, et elle
  ne peut pas révéler la dépendance erronée que le tirage était censé exposer.
* **Ce qui décide de la branche est la valeur de la borne opposée, jamais le fait qu'une contrainte l'ait
  déclarée.** `Between(1e6, double.MaxValue)` et `GreaterThanOrEqualTo(1e6)` désignent le même ensemble et
  doivent donc tirer pareil ; lire la contrainte plutôt que la valeur séparerait deux orthographes d'une
  même exigence de 300 décades, ce qui est le défaut et non son correctif. Une borne en deçà du bord propre
  du type est une borne que l'appelant a écrite et possède ; une borne posée exactement sur ce bord est le
  domaine qui transparaît, et permettre une magnitude n'est toujours pas la demander.
* **Reporter la fenêtre est ce qui garde ensemble les deux orthographes d'une borne.** L'abandonner là où
  elle ne peut pas rétrécir envoie une contrainte unilatérale au bout du domaine — l'issue que l'ADR-0031
  existe pour empêcher — tandis que la conserver effondre le tirage. Reporter sa largeur à la borne que
  l'appelant a bel et bien écrite est la seule des trois options qui honore la borne déclarée tout en restant
  ordinaire, et elle referme la discontinuité d'un ulp par conséquence plutôt que par rustine séparée.
* **Un intervalle explicitement borné appartient à l'appelant.** Ses deux bornes ont été écrites : les
  traiter comme intentionnelles est la règle la plus simple et celle qu'un lecteur peut prédire. Qui voulait
  « au moins un million, mais ordinaire par ailleurs » dispose d'une façon de le dire exactement, et reçoit
  la fenêtre reportée ; tronquer un intervalle bilatéral ne protégerait de rien, puisque les magnitudes qu'un
  tel intervalle atteint sont celles que le critère propre de l'ADR-0031 qualifie déjà d'ordinaires.
* **« Deux valeurs » signifie deux valeurs sur lesquelles un tirage peut tomber.** Sur `decimal`, c'est un
  treillis d'échelle déclaré qui en décide, non les bornes : un intervalle à cheval sur un seul point de grille
  ne rend qu'une valeur, si large qu'il paraisse — le rétrécissement se juge donc contre la grille. Lire les
  bornes à la place rejoint le singleton même que cette décision existe pour supprimer :
  `Between(999_999.5m, 1_000_001m).WithScale(0)` se rétrécit sur une portée dont l'unique point de grille est
  `1 000 000`, et le `1 000 001` que l'appelant a déclaré devient inatteignable. Les types binaires n'ont pas de
  tel treillis : leurs valeurs tirables sont celles que le type représente. Ni l'une ni l'autre lecture ne
  dispense de l'échelle de valeurs propre à la ligne, et les deux moteurs l'ont appris à leurs dépens. Une
  *borne* déclarée arrive représentable ; une extrémité que la règle *calcule* ne l'est pas forcément, si bien
  qu'une tranche reportée peut bouger dans l'arithmétique tout en ne tenant qu'une valeur de la ligne — à
  `2^45f`, un ulp de `float` est plus large que la tranche entière. Une *échelle* déclarée est ce sur quoi un
  candidat est aligné, non un incrément que la représentation peut toujours faire — à une magnitude de 1e6, un
  `decimal` porte 22 décimales, si bien que `WithScale(28)` nomme un pas qui ne déplace rien. C'est l'échelle de
  valeurs sur laquelle la ligne tire réellement qui tranche, sur l'un comme sur l'autre moteur.
* **La règle reste prédictible au point d'appel.** Chacun des invariants ci-dessus pousse vers plus de
  machinerie, et une règle de fenêtrage n'est jamais loin d'une recherche.
  L'[ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) s'applique à cette décision
  autant qu'aux générateurs qu'elle gouverne : une règle énoncée en trois clauses et légèrement grossière
  vaut mieux qu'une règle optimale que personne ne peut prédire depuis les contraintes qu'il a écrites.
  C'est pourquoi aucune branche supplémentaire n'est ajoutée pour les cas qui restent imparfaits.

## Alternatives envisagées

### S'effacer dès que le rétrécissement laisse moins de deux valeurs

Envisagée parce que c'est le plus petit changement possible — une comparaison — et qu'elle corrige bel et
bien l'effondrement bilatéral et l'échec décimal.

Rejetée parce qu'elle les corrige en généralisant l'autre moitié du défaut. Une borne unilatérale posée sur
le bord de la fenêtre tirerait alors sur tout le domaine restant, uniforme par valeur, donc à quelques
décades du maximum du type : `GreaterThanOrEqualTo(1e6)` passerait d'une constante à `1e308` et y rejoindrait
`GreaterThan(1e6)` au lieu de le retrouver au milieu. Échanger une constante contre la classe même de
valeurs que l'ADR-0031 a été écrite pour éliminer n'est pas un correctif.

### Reporter la fenêtre à la borne déclarée dans tous les cas, intervalles bilatéraux compris

Envisagée comme la version uniforme de la règle retenue : une branche au lieu de deux, sans distinction
entre une contrainte unilatérale et un intervalle borné.

Rejetée parce que la troncature qu'elle impose à un intervalle bilatéral ne protège de rien.
`Between(1e6, 5e6)` ne serait tiré que sur ses deux premiers millions de largeur, alors que son intégralité
satisfait déjà le critère dont l'ADR-0031 se sert pour justifier la constante : neuf chiffres significatifs
sous la virgule, une magnitude quelconque, une arithmétique à des centaines de décades du débordement. Elle
coûterait à l'appelant l'essentiel de la plage qu'il a écrite en échange d'une garantie qu'il avait déjà.

### Remplacer la fenêtre fixe par une frontière dérivée

Envisagée parce qu'un million est explicitement une constante choisie et non mesurée, et que la couture est
un symptôme d'une fenêtre absolue ancrée sur zéro ; une frontière dérivée par type de la magnitude où son
arithmétique se dégrade n'aurait pour ainsi dire pas de couture.

Rejetée comme une décision plus large que le défaut ne le justifie, et sans preuve derrière elle à ce jour.
Les mesures disponibles décrivent où les types se comportent mal, non où une frontière dérivée devrait se
poser, et une dérivation par type ferait dépendre la magnitude reçue d'un calcul plutôt que d'une constante
lisible. Elle reste un candidat légitime si la constante venait à mordre là où la couture ne l'explique pas.

## Conséquences

### Positives

* Un intervalle déclaré contenant plus d'une valeur produit plus d'une valeur, sur tous les types continus.
* `Any.Decimal().GreaterThan(1_000_000m)` et son miroir génèrent au lieu d'échouer à chaque tirage.
* Les deux orthographes d'une borne — inclusive et exclusive, unilatérale et écrite contre le bord du
  domaine — tirent à la même magnitude : la surface ne récompense plus le fait de deviner laquelle évite la
  couture.
* La règle s'énonce en trois clauses qu'un lecteur peut appliquer à son propre appel sans la simuler.

### Négatives

* `Any.Double().GreaterThan(1e6)` et ses semblables changent de magnitude, d'environ `1e308` à juste
  au-dessus de la borne déclarée. C'était le comportement documenté de la branche d'effacement, et qui s'y
  fiait pour obtenir des valeurs extrêmes doit désormais nommer l'intervalle voulu.
* La règle a trois clauses là où elle en avait une, et celle du milieu repose sur une comparaison au bord
  propre du type plutôt que sur le seul jeu de contraintes.

### Risques

* La largeur de la fenêtre est réutilisée comme largeur reportée : un choix défendable et non dérivé, du
  même statut que la constante elle-même.
* Une borne unilatérale assez grande pour que la largeur reportée ne la déplace pas tire toujours sur le
  domaine déclaré. C'est le comportement préexistant, conservé délibérément, et le seul cas où la magnitude
  reçue par un appelant n'est pas ordinaire.

## Actions de suivi

* Garder l'attendu des propriétés d'atteignabilité consommateur de la règle plutôt qu'énonciateur, et garder
  la couture atteignable par leurs générateurs d'intervalles ; la règle elle-même est épinglée cas par cas
  en exemples ([ADR-0019](0019-split-the-justdummies-test-bed-between-example-and-property-suites.fr.md)).
* Énoncer la règle dans la documentation du paquet là où la surface de contraintes est décrite, en
  remplacement de la formulation de l'ADR-0031.
* L'évanouissement du plus petit incrément décimal au-delà d'une magnitude d'environ dix est un défaut
  distinct, atteignable sans cette couture, et n'est pas traité ici.

## Références

* [ADR-0031](0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.fr.md) — la décision supersédée : ses
  preuves, sa constante et son intention sont reprises telles quelles.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — pourquoi la règle s'arrête à
  trois clauses au lieu d'optimiser les cas qui restent.
* [ADR-0076](0076-let-a-declared-maximum-steer-the-size-draw.fr.md) — le même geste « une borne écrite par
  l'appelant est une borne qu'il obtient », appliqué aux tailles.
* [ADR-0091](0091-draw-a-half-from-the-values-it-can-represent.fr.md) — pourquoi `Half` est insensible à ce
  que fait cette fenêtre.
* Issue [#178](https://github.com/Reefact/just-dummies/issues/178) — les mesures ci-dessus, et les
  invariants contre lesquels cette décision a été choisie.
