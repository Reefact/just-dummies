# ADR-0088 | Énoncer la garde de blancheur avec un membre à elle

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0088-state-the-whitespace-guard-with-a-member-of-its-own.md)

**Status:** Accepted
**Proposed:** 2026-08-24
**Accepted:** 2026-08-24
**Decision Makers:** Reefact

## Context

`string.IsNullOrWhiteSpace` est la façon la plus répandue, en .NET, dont un constructeur rejette une chaîne sans
contenu. Le scaffolder lit cette garde, et émettait jusqu'ici `.NonEmpty()` pour elle — un plancher d'un
caractère. Les deux ne sont pas la même chose : une valeur d'un seul espace satisfait le plancher et la garde
la rejette.

Le repli reposait sur une prémisse inscrite dans la spécification : qu'un `Any.String()` non contraint ne tire
que des lettres et des chiffres ASCII, ce qui rend un tirage tout-blanc impossible.
[ADR-0075](0075-draw-characters-from-the-whole-of-ascii.fr.md) l'a falsifiée — le remplissage est tout l'ASCII,
blancs compris — et [ADR-0076](0076-let-a-declared-maximum-steer-the-size-draw.fr.md) a fait qu'un maximum
déclaré pilote le tirage, si bien qu'un plafond court rend les chaînes courtes ordinaires. Aucun des deux records
n'est revenu sur la ligne, et elle se lit toujours comme une justification.

La conséquence est mesurable plutôt que théorique. Sous un plafond de quatre caractères, environ un tirage sur
quatre-vingts est entièrement blanc. Face à un domaine qui garde avec `IsNullOrWhiteSpace`, un générateur
scaffoldé compile donc, ne lève aucune règle, rapporte le paramètre comme inféré, et se fait rejeter par le
constructeur pour lequel il a été écrit — le mode de défaillance que
[ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.fr.md) existe pour empêcher, atteint ici en lisant une
garde plutôt qu'en échouant à en lire une.

La bibliothèque n'a aucun membre qui énonce la garde. Ses voisins les plus proches manquent chacun leur cible :

* `NonEmpty()` est le plancher que le repli utilisait déjà, et celui auquel la garde survit.
* `AlphaNumeric()` rejette les blancs, et aussi la ponctuation que la garde admet — il contraint un domaine dont
  la garde n'a jamais parlé.
* `WithoutAlpha()` et la paire soustractive retirent une famille de toutes les positions, là où la garde demande
  seulement qu'une position ne soit pas blanche.

Deux faits supplémentaires pèsent sur la forme de tout membre ajouté ici.

**La bibliothèque porte déjà une notion plus étroite du blanc.** La famille `Whitespaces` est l'espace et la
tabulation — la paire lisible, choisie pour qu'un test puisse compter voir un séparateur, et reprise par le `\s`
du sous-ensemble régulier. Le `char.IsWhiteSpace` de la BCL, en fonction duquel `IsNullOrWhiteSpace` est défini,
accepte six caractères ASCII : la paire, et les quatre sauts de ligne et de page. Mesuré sur des tirages non
contraints sous un plafond court, deux valeurs blanches sur trois ne le sont que par un caractère que la famille
ne nomme pas.

**`ADR-0086` interdit l'approximation qui éviterait tout cela.** Un helper de garde dont la table de contraintes
ne peut porter la sémantique est laissé non lu plutôt que rapproché de quelque chose d'approchant, et les deux
orthographes de bibliothèque du rejet de blancheur sont non lues aujourd'hui pour exactement cette raison, avec
un commentaire nommant le membre manquant.

## Decision

`Any.String()` gagne `NotBlank()`, une contrainte constructive exigeant au moins un caractère que le
`char.IsWhiteSpace` de la BCL rejette, et le scaffolder lit chaque orthographe de la garde de blancheur comme ce
membre plutôt que comme `NonEmpty()`.

## Rationale

**L'alternative à un membre est un refus permanent, et la garde est trop répandue pour être refusée.** La règle
d'ADR-0086 laisse non lu un helper non mappable, et c'est la bonne réponse tant que rien n'énonce la sémantique
— c'est ce que font les deux lignes de bibliothèque aujourd'hui. Mais `IsNullOrWhiteSpace` n'est pas un recoin de
la table d'idiomes : c'est la manière ordinaire dont un domaine .NET dit qu'une chaîne doit porter du contenu.
Répondre à la garde la plus courante du langage par un blocage de compilation, définitivement, dépense l'utilité
du scaffolder pour protéger un manque que la bibliothèque pourrait simplement combler. Ajouter le membre est ce
qui transforme un refus permanent en une lecture exacte, et c'est la seule route qui le fasse sans approximer.

**La correction n'est pas ce que cette bibliothèque borne.**
[ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) borne ce que le générateur *tente* et
jamais ce qu'il garantit une fois qu'il le fait. Une valeur tirée satisfaisant chaque contrainte déclarée est la
garantie, et un tirage rejeté par le domaine depuis un récap propre est une rupture de celle-ci. Le membre n'est
donc pas un cran d'ambition à peser contre la borne ; c'est la réparation d'un défaut de correction que la borne
n'a jamais couvert.

**Il énonce la garde exactement, ce qui est tout l'enjeu.** Le prédicat est celui de la BCL, donc la contrainte
ne rétrécit pas un domaine que la garde laissait ouvert — les blancs intérieurs restent légaux, la ponctuation
reste légale — et ne laisse pas ouvert un domaine que la garde ferme. Cette exactitude est ce qui lui vaut une
ligne dans la table fermée sous la règle propre à ADR-0086 : mesurée, pas approximée. Un membre bâti sur le
prédicat de famille plus étroit aurait satisfait la lettre de la même règle en laissant deux tirages blancs sur
trois atteindre encore le domaine, et c'est pourquoi le prédicat plus large fait partie de la décision plutôt que
d'un détail d'implémentation.

**Deux notions du blanc sont l'issue honnête, et les nommer coûte moins cher que les unifier.** Élargir la
famille `Whitespaces` aux six de la BCL déplacerait toutes les graines qui y puisent, contre la promesse de
rejeu d'[ADR-0049](0049-replay-a-seed-across-patch-and-minor-versions.fr.md), et coûterait à la famille la
lisibilité pour laquelle ADR-0075 l'a choisie. Les deux servent des rôles différents — l'une est un alphabet
auquel un tirage est restreint, l'autre un test qu'une valeur doit passer — et un lecteur rencontre la différence
au seul endroit où elle compte, là où déclarer les deux se contredit et où le message nomme chaque côté.

**Ce n'est pas une famille de caractères, donc la file qu'ADR-0075 a fermée le reste.** Ce record n'admet un
alphabet nommé que là où une norme publiée le définit, et renvoie vers `WithChars` tout alphabet qu'un projet
invente. Ce membre ne nomme aucun alphabet : il contraint la valeur assemblée, sur le même axe qu'une longueur
plutôt que sur l'axe de l'alphabet, et chaque caractère d'un tirage reste libre. La liste des familles est
intacte, et la règle voulant que chaque famille rétrécisse l'ASCII aussi.

**Constructive plutôt que rejective, parce qu'[ADR-0033](0033-decide-a-constraint-surface-by-constructive-versus-rejective.fr.md)
a déjà décidé comment trancher.** La contrainte décrit une valeur que le générateur doit bâtir, donc elle est
bâtie — jamais tirée puis retentée. Sur le chemin de l'ensemble de valeurs, la même déclaration filtre le pool
fourni, ce qui est ce que ce record entend par offrir une contrainte là où le générateur peut la satisfaire.

**Un littéral ancré répond pour lui-même.** Un préfixe, un suffixe ou une valeur contenue portant déjà un
caractère non blanc satisfait la garantie, et la contrainte n'exige alors rien du remplissage et ne juge aucun
alphabet. Cela garde intact
[ADR-0079](0079-constrain-what-a-dummy-draws-never-the-literals-it-was-given.fr.md) : le littéral est lu pour
décider de ce que le tirage doit fournir, jamais rejeté pour ce qu'il contient.

## Alternatives Considered

### Laisser les deux orthographes non lues, comme ADR-0086 le fait déjà pour les bibliothèques de gardes

La réponse par défaut, et celle qui ne demande aucune surface publique nouvelle : le scaffolder bloque la
compilation avec la marque de vérification et le développeur écrit la contrainte lui-même.

Rejetée sur ce que cela coûte, à une garde aussi répandue. La marque est la bonne réponse pour un idiome que la
table ne peut porter, et `IsNullOrWhiteSpace` en est un que la table *peut* porter dès qu'un membre existe —
choisir la marque ici, c'est donc choisir de laisser ouvert pour toujours un manque refermable. Chaque scaffold
d'un type chaîne validé porterait un blocage que le développeur résout à la main, ce qui est précisément l'issue
que l'outil existe pour éviter, et la base devrait encore expliquer à un lecteur pourquoi la garde la plus
ordinaire de .NET est celle qu'elle ne sait pas lire.

### Mapper la garde vers `AlphaNumeric()`

Disponible aujourd'hui, rejette les blancs, et ne demande aucun membre nouveau.

Rejetée parce qu'elle énonce un invariant que le domaine n'a jamais déclaré. Une garde sur la blancheur ne dit
rien de la ponctuation, et un générateur qui ne tire jamais de tiret pour un paramètre dont le domaine en admet
un certifie moins que ce que le test laisse croire. C'est l'approximation qu'ADR-0086 nomme et refuse, et elle
échangerait une valeur fausse contre un domaine silencieusement rétréci plutôt que de réparer quoi que ce soit.

### Bâtir le membre sur le prédicat de famille `Whitespaces` existant

La lecture plus étroite, et celle qui laisserait à la bibliothèque une notion unique du blanc.

Rejetée sur la mesure. La famille est l'espace et la tabulation ; la garde rejette quatre caractères de plus, et
ces quatre-là comptent pour deux tirages blancs sur trois sous un plafond court. Un membre bâti ainsi énoncerait
la garde de façon inexacte dans le sens permissif — le seul qui compte — et le défaut qu'il existe pour refermer
lui survivrait dans la majorité des cas.

### Élargir la famille `Whitespaces` aux six de la BCL, puis bâtir dessus

Unifie les deux notions, et supprime la divergence qu'un lecteur doit apprendre.

Rejetée sur le coût et sur l'intention. Cela déplace toutes les graines qui puisent dans la famille, ce
qu'ADR-0049 rend majeur, et cela retire la lisibilité qu'ADR-0075 a délibérément choisie — une famille dont le
rôle est « un séparateur sur lequel je peux compter » ne devrait pas rendre un saut de page. La divergence est
réelle mais elle sépare un alphabet d'un test, deux choses différentes qui partagent un mot.

## Consequences

### Positive

* La garde de chaîne la plus courante de .NET se lit exactement, là où elle se lisait faux, et les deux
  orthographes de bibliothèque cessent de bloquer la compilation.
* Une classe de défauts se referme à sa source : la valeur que le scaffolder émet satisfait le domaine qui la
  jugera.
* La prémisse falsifiée de la spécification est retirée plutôt que laissée debout à côté des records qui l'ont
  falsifiée.
* Les appelants qui écrivent leurs générateurs à la main gagnent la contrainte aussi — la garde était
  inénonçable pour eux également.

### Negative

* Un membre de plus sur la surface chaîne, avec sa ligne de baseline sur les deux frameworks cibles, trois bras
  d'analyzer, un jumeau de documentation et sa place dans la table de parité.
* La bibliothèque porte deux notions du blanc, et la documentation est la seule chose qui les tient distinctes.
* `NotBlank()` n'a pas de contrepartie sur `Any.Char()`, donc les deux surfaces ne sont plus symétriques —
  délibérément, puisqu'un caractère unique est blanc ou ne l'est pas, ce que les familles existantes disent déjà.

### Risks

* Un appelant peut lire `NotBlank()` comme interdisant les blancs intérieurs et l'employer là où il voulait une
  famille ou un motif. Atténué sur la documentation du membre lui-même, là où il le rencontre.
* Les analyzers tiennent leur propre copie de ce que chaque contrainte admet, et rien ne vérifie que les deux
  s'accordent — le risque qu'ADR-0075 avait déjà consigné, avec un membre de plus dessus.

## Follow-up Actions

* Revoir si la table de base de la spécification doit émettre ce membre pour un paramètre `string` sans aucune
  garde. C'est une autre question — rien n'y a été lu — et ce record ne la tranche pas.
* Le sous-ensemble régulier tire `\s` de la paire lisible, donc un motif et cette contrainte divergent sur le
  blanc de la même manière que la famille. Signalé plutôt que tranché, à côté de la divergence qu'ADR-0075 a déjà
  laissée ouverte pour les positions libres d'un motif.

## References

* [ADR-0086](0086-read-the-guard-helpers-of-named-libraries.fr.md) — la règle « mesuré, ou pas dans la table », et
  les deux lignes dont les commentaires nommaient le membre que ce record ajoute.
* [ADR-0075](0075-draw-characters-from-the-whole-of-ascii.fr.md) — la règle des familles à laquelle ce membre
  n'est pas soumis, et le défaut élargi qui a falsifié la prémisse du repli.
* [ADR-0033](0033-decide-a-constraint-surface-by-constructive-versus-rejective.fr.md) — pourquoi la contrainte est
  bâtie plutôt que filtrée, et pourquoi elle atteint le chemin de l'ensemble de valeurs.
* [ADR-0079](0079-constrain-what-a-dummy-draws-never-the-literals-it-was-given.fr.md) — l'exemption que garde un
  littéral ancré.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — la borne hors de laquelle se situe
  cette réparation.
