# ADR-0094 | Lever un type valeur nullable plutôt que le dériver

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0094-lift-a-nullable-value-type-rather-than-deriving-it.md)

**Statut :** Accepted
**Proposé :** 2026-09-02
**Accepté :** 2026-09-02
**Décideurs :** Reefact

## Contexte

`IAny<out T>` est covariant à travers les conversions de référence : un `IAny<string>` est donc déjà
un `IAny<string?>`. Un type valeur n'a pas cette conversion — un `IAny<int>` n'est pas un
`IAny<int?>` — et le §5.2 du scaffolder écrit le saut explicitement depuis qu'il existe :
`Any.Int32().Positive().As(value => (int?)value)`. Jamais `.OrNull()`, parce qu'un dummy dont le code
testé a besoin n'est jamais absent
([ADR-0064](0064-never-draw-null-for-a-nullable-parameter.fr.md)).

`As` produit un `DerivedAny<T>`, qui porte la source aléatoire et la reproductibilité de ce qu'il
enveloppe, et rien d'autre. C'est délibéré et documenté sur `ICardinalityHint<T>` : un générateur
dérivé n'annonce aucune borne, parce qu'une fabrique quelconque n'a pas d'inverse pour répondre à
l'appartenance, et une collection distincte au-dessus retombe sur un tirage dédupliqué borné
([ADR-0004](0004-gate-distinct-collections-by-cardinality-else-bounded-draw.fr.md)).

La conséquence a été mesurée le 2026-09-02, au premier passage complet du balayage génératif. Un
ensemble d'énums nullables avec un plancher de **un** était refusé : l'ensemble n'avait pas de
plafond, tirait une taille que le domaine de trois membres ne pouvait pas remplir, et épuisait son
budget de retirage. 190 formes du produit se comportaient ainsi — tout ensemble ou dictionnaire
scaffoldé clé par une énum ou un booléen nullable, c'est-à-dire exactement la conversion que le §5.2
écrit pour un élément nullable. Cinquante-cinq ont été convaincues par la règle de distinction du
balayage ; les 135 autres revenaient sous un statut qui se lit comme la bibliothèque se comportant
correctement, et n'ont bougé que quand la cause a bougé.

Rien dans la bibliothèque n'était faux à aucun moment : le repli fait ce qu'il annonce, sur un
générateur qui annonce ne rien savoir. Ce qui était faux, c'était de scaffolder une chaîne dont le
seul dénouement possible est ce repli, sur un domaine qui admet manifestement des valeurs — la
défaillance silencieuse que l'[ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.fr.md)
existe pour empêcher, arrivée par une route que ce record ne couvre pas.

## Décision

La bibliothèque porte un lift de première classe d'`IAny<T>` vers `IAny<T?>` qui ne tire jamais null
et conserve la cardinalité du générateur enveloppé, et le scaffolder l'écrit partout où la
compilation cible le résout.

## Justification

* **Le lift est la seule projection dont l'inverse est connu, donc les deux moitiés de l'indication
  suivent.** `ICardinalityHint<T>` met le compte et le test d'appartenance sur une seule interface
  exprès : une collection a besoin de la taille pour barrer un compte, et de l'appartenance pour
  distinguer une valeur épinglée qui étend le domaine d'une qui y est déjà. Un `As` quelconque ne
  peut répondre ni à l'un ni à l'autre, ce qui est la raison pour laquelle un dérivé n'annonce rien.
  Lever vers `Nullable<T>` est total et injectif, et son inverse est `Value` — donc le compte est
  celui du générateur enveloppé, inchangé, et l'appartenance est « a une valeur, et cette valeur est
  une des siennes ». Ceci n'élargit pas la règle sur les dérivés ; ceci ajoute un générateur qui
  n'est pas une dérivation.
* **Ça fait réussir par construction un cas jusque-là refusé, pas par chance**, ce qui est la forme
  que l'[ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) approuve. Rien
  n'est tiré plus astucieusement et aucune borne n'est élargie en silence : les mêmes valeurs
  sortent, dans le même ordre, sous la même graine. Seul change ce que la collection a le droit d'en
  savoir.
* **L'alternative sans API nouvelle refuse un domaine satisfiable.** Faire bloquer la compilation par
  le scaffolder sur une collection distincte au-dessus d'un type valeur nullable, ce serait le
  « refuser fort au bord » d'ADR-0046 appliqué là où le bord n'est pas l'ambition mais un fait que la
  bibliothèque possédait déjà et perdait en chemin.
* **Un utilisateur en a besoin bien plus souvent que de son jumeau.** `OrNull` est pour une valeur
  qui peut être absente ; un paramètre simplement écrit `int?` doit quand même en recevoir une, et y
  générer une valeur absente exerce une branche que le test n'a jamais demandée. La paire se lit
  comme un choix qu'un développeur fait délibérément, là où l'une des deux options n'avait aucune
  écriture du tout.
* **Le scaffolder demande au lieu de supposer**, comme
  l'[ADR-0059](0059-emit-only-members-resolved-in-the-target-compilation.fr.md) l'exige : un asset
  antérieur au lift reçoit le saut qu'il a toujours reçu. Un consommateur qui met l'outil à jour sans
  mettre le paquet à jour ne voit aucun changement, plutôt qu'un fichier nommant un membre que sa
  compilation ne résout pas.

## Alternatives considérées

### Donner une cardinalité au générateur dérivé

Considérée parce que c'est la plus petite édition — une interface sur `DerivedAny<T>` et toute la
famille en profite d'un coup.

Rejetée parce que la famille est le problème. `DerivedAny<T>` est ce que produisent `As`, `OrNull` et
les sept arités de `Combine`, et un composeur sur huit opérandes n'a aucune cardinalité que
quiconque puisse calculer. Faire suivre la borne d'un opérande serait une sur-estimation exactement
dans le sens qui transforme un tirage différé en refus erroné, et faire suivre l'appartenance est
tout bonnement impossible sans inverse. Les deux membres de l'interface voyagent ensemble pour cette
raison.

### Faire plafonner la taille de la collection par le scaffolder

Considérée parce que le moteur peut lire le nombre de membres d'une énum dans la compilation cible,
et donc émettre un maximum que le domaine satisferait.

Rejetée : ce serait le moteur inventant une borne que personne n'a déclarée, la seule chose que le
§5.2 ne doit pas faire — le fichier émis contraindrait alors une valeur d'une façon que le type du
développeur ne contraint pas, et un lecteur ne pourrait pas distinguer l'invention d'une garde lue.

### Laisser en l'état, et consigner la forme comme un résidu déclaré

Considérée parce que le comportement de la bibliothèque est documenté et que l'échec est bruyant
plutôt que silencieux — une `AnyGenerationException` qui nomme sa graine, pas une valeur fausse.

Rejetée parce que l'échec est bruyant pour *un lecteur de l'exception* et silencieux pour le
développeur qui a lancé `dum` et commité le fichier : il apparaît au premier tirage, dans un
générateur qu'on lui a remis en lui disant qu'il était inféré. C'est la position que l'ADR-0083
refuse.

## Conséquences

### Positives

* Un ensemble ou un dictionnaire scaffoldé clé par une énum ou un booléen nullable tire, là où 190
  formes du produit du balayage ne le pouvaient pas.
* Un utilisateur gagne une écriture pour « type nullable, valeur présente » que la bibliothèque
  n'avait pas.
* La ligne émise est plus courte et dit ce qu'elle veut dire : `Any.Int32().Positive().AsNullable()`
  plutôt qu'une lambda de conversion qu'un lecteur doit décoder.

### Négatives

* Un membre de plus sur la surface publique, et une paire de plus à distinguer. `OrNull` et
  `AsNullable` diffèrent d'un mot et de tout le reste.
* Tout fichier scaffoldé portant un paramètre nullable de type valeur change de forme. Rien ne se
  régénère tout seul (§9), donc un développeur ne voit la nouvelle écriture que là où il re-scaffolde.

### Risques

* **Les deux écritures peuvent être confondues**, et la confusion est asymétrique : prendre `OrNull`
  là où `AsNullable` était voulu donne une valeur absente une fois sur deux environ, ce qui se lit
  comme un test instable plutôt que comme un mauvais choix. La documentation énonce le contraste
  plutôt que de décrire chacune seule.
* **La cardinalité du lift ne vaut que celle du générateur enveloppé.** Il la fait suivre, il ne la
  calcule pas. Un générateur qui sur-estime sa propre borne la sur-estime ici aussi, un saut plus
  loin de l'endroit d'où le nombre vient.

## Actions de suivi

* Relire les comptes du balayage après le premier passage hebdomadaire sur `main` : 190 formes ont
  changé de statut dans une exécution locale, et la référence versionnée est le document qui le dit.

## Références

* [ADR-0004](0004-gate-distinct-collections-by-cardinality-else-bounded-draw.fr.md) — le contrat à
  deux étages que ceci rétablit pour un élément nullable.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — par construction
  plutôt que par chance, et la raison pour laquelle un refus a été pesé d'abord.
* [ADR-0059](0059-emit-only-members-resolved-in-the-target-compilation.fr.md) — pourquoi le
  scaffolder interroge la compilation avant d'écrire le lift.
* [ADR-0064](0064-never-draw-null-for-a-nullable-parameter.fr.md) — pourquoi le saut n'est jamais
  `.OrNull()`.
* [ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.fr.md) — la défaillance
  silencieuse que ceci a contournée par une autre route.
* [`gendummy-sweep.fr.md`](../workflows/gendummy-sweep.fr.md) — le banc qui l'a mesurée, et ce que ses
  comptes disaient avant et après.
