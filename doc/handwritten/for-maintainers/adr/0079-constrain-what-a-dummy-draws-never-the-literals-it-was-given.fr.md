# ADR-0079 | Contraindre ce qu'un dummy tire, jamais les littéraux qu'on lui a donnés

🌍 🇬🇧 [English](0079-constrain-what-a-dummy-draws-never-the-literals-it-was-given.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-08-20
**Accepted:** 2026-08-20
**Decision Makers:** Reefact

## Contexte

`Any.String()` sans ensemble de valeurs est **constructif** : il agence une valeur en
`préfixe + remplissage + valeurs contenues + remplissage + suffixe` et la rend, sans jamais générer puis
filtrer. Le pool de caractères n'alimente que le **remplissage** — les fragments ancrés sont ajoutés exactement
tels que l'appelant les a écrits. L'`ADR-0075` encadre déjà chaque famille de caractères, ainsi que les
soustractives `WithoutAlpha` / `WithoutNumeric`, comme ne faisant que restreindre l'ensemble dans lequel un
tirage non contraint puise.

Jusqu'ici, la validation croisée à la déclaration allait plus loin que l'agencement : elle tenait aussi chaque
fragment ancré à la famille déclarée, à chaque soustraction et à la casse déclarée, et refusait la chaîne dès
qu'un fragment portait un caractère qu'elles excluaient. `JD015` reflétait ce refus à la compilation dès que
les arguments étaient constants.

La conséquence était qu'un format très ordinaire ne pouvait pas s'exprimer du tout : un préfixe fixe suivi d'un
corps restreint à un alphabet. `AlphaNumeric().StartingWith("ORD-")` était refusé, parce que le séparateur que
l'appelant a écrit n'est pas alphanumérique.

Le contournement disponible consistait à déclarer le séparateur dans un pool personnalisé. Cela le rend
tirable **partout** : les valeurs produites le portent alors dans le corps et en fin de chaîne — l'inverse de
l'invariant que la chaîne devait exprimer. Il coûte une seconde règle par-dessus : un pool personnalisé occupe
l'unique emplacement de famille de caractères et, parce que le pool est toute la définition des caractères,
refuse de se combiner à une casse ; un format à casse unique doit donc cuire sa casse dans un littéral de pool
au lieu de déclarer `UpperCase()`.

Deux mécanismes voisins ne sont concernés par rien de tout cela et restent en l'état. Le **budget de longueur**
est une vérification distincte : les fragments sont juxtaposés, donc leurs longueurs doivent toujours tenir
dans la longueur déclarée. Et dès qu'un ensemble de valeurs est en vigueur, la spécification cesse d'agencer
quoi que ce soit et devient un **filtre** sur les valeurs fournies par l'appelant, où les mêmes contraintes
restreignent le pool fourni au lieu de façonner une chaîne — un mécanisme différent, avec son propre contrat
(`ADR-0054`).

## Décision

Une famille de caractères, un pool personnalisé, une soustraction et une casse gouvernent chaque caractère que
`Any.String()` **tire** et rien d'autre : sur une chaîne façonnée c'est le remplissage seul, de sorte qu'un
littéral fixé par `StartingWith`, `EndingWith` ou `Containing` est conservé exactement tel qu'écrit ; sur un
ensemble de valeurs rien n'est tiré du tout, de sorte que les valeurs fournies leur restent soumises.

## Justification

La règle que le générateur suit déjà, c'est l'agencement, et cet agencement n'a jamais donné au pool de
caractères la moindre prise sur les fragments. La validation retirée refusait donc des chaînes que le chemin de
génération aurait parfaitement honorées : elle faisait respecter une règle dont rien en aval n'avait besoin, et
son seul effet observable était de rendre inécrivable un format légitime.

Cela ne borne aucune correction, qui est la ligne que trace l'`ADR-0046`. Une valeur tirée satisfait toujours
chaque contrainte déclarée — ce qui a changé, c'est ce que la contrainte *déclare*, pas le fait que le
générateur l'honore. L'affirmation plus étroite est d'ailleurs la plus utile : un appelant qui écrit un
séparateur dans un préfixe affirme que ce séparateur est là et nulle part ailleurs, et c'est précisément
l'invariant que la chaîne peut désormais exprimer et que le contournement ne savait pas rendre.

Prendre les trois sortes ensemble plutôt qu'une à une est ce qui rend la règle enseignable. Sur le chemin
constructif, une famille, une soustraction et une casse sont la même sorte de chose — les trois filtres qui
restreignent l'alphabet dans lequel le remplissage est tiré. Une règle exemptant un littéral de deux d'entre
elles mais pas de la troisième devrait être portée comme une exception, et cette exception ne découlerait de
rien dans l'agencement qu'elle prétend décrire. L'uniformité supprime en outre toute une classe de combinaisons
contradictoires au lieu de la réduire, la valeur même que nomme l'`ADR-0008` lorsqu'il refuse de rendre un
motif généré chaînable avec les contraintes de chaîne.

Une seule règle couvre les deux chemins, et c'est la lire comme une exemption pour les littéraux qui les fait
paraître deux. Une contrainte gouverne les caractères tirés, et lesquels le sont est une question d'agencement,
non de savoir qui les a écrits. Sur une chaîne façonnée, les fragments revendiquent leurs propres régions et la
famille revendique le complément : les deux ne se rencontrent jamais et ne peuvent pas se contredire — c'est
pourquoi `AlphaNumeric().StartingWith("ORD-")` se compose. Un ensemble de valeurs revendique au contraire la
chaîne entière : il fournit la valeur tout entière et ne laisse aucun remplissage, donc la région de la famille
est cette même valeur, et deux contraintes sur une même région doivent s'accorder exactement comme
`Alpha().Numeric()` doit s'accorder. `OneOf("ORD-1").AlphaNumeric()` est refusé pour la raison qui fait refuser
deux familles, pas par un principe différent. La lecture inverse — exempter une valeur fournie parce que
l'appelant l'a écrite — laisserait la famille ne gouverner absolument rien sur ce chemin, en faisant un no-op
muet plutôt qu'une contrainte.

`JD015` doit se restreindre avec l'exécution. Un diagnostic qui refuse à la compilation ce que l'exécution
honore est pire que l'un ou l'autre comportement pris isolément, car l'appelant ne peut pas satisfaire les
deux. La règle conserve le budget de longueur, qui reste dépendant de la valeur, reste indécidable par le
système de types, et reste exactement le cas que l'`ADR-0014` désigne comme celui de l'analyseur — c'est pour
cette raison que l'illustration de cet enregistrement a été replacée sur le budget de longueur, sa décision
demeurant intacte.

## Alternatives envisagées

### Conserver le refus et documenter le pool personnalisé comme issue

Cela ne coûte aucun changement, et le contournement produit bien une valeur.

Rejeté parce que la valeur produite viole l'invariant que l'appelant cherchait à énoncer : le séparateur
devient tirable dans le corps et en fin de chaîne. Un contournement documenté qui casse silencieusement la
règle qu'il contourne est pire que le refus qu'il remplace, et il sacrifie de surcroît la casse : deux des
règles du format cessent d'être des appels lisibles pour devenir un littéral de chaîne opaque.

### Un DSL par segment, donnant à chaque zone sa famille et sa longueur

C'est la forme la plus riche, et elle exprime un format multizone exactement plutôt que par exemption.

Rejeté parce que l'`ADR-0008` rejette déjà la même forme, sous « garder le générateur chaînable avec les autres
contraintes de chaîne » : un générateur terminal, portant toute la spécification, « supprime entièrement une
classe de combinaisons contradictoires », et un DSL par segment réintroduirait cette classe entre segments au
lieu de l'avoir entre le motif et la chaîne. `Any.StringMatching(...)` exprime déjà plus compactement un format
réellement multizone, et reste l'outil adapté pour cela.

### Exempter la famille et la soustraction, mais continuer de juger la casse

Une casse se lit comme une propriété de la valeur entière plutôt que d'un alphabet, et conserver la
vérification laisserait un filet sous une faute de frappe évidente — un préfixe en minuscules déclaré à côté
d'`UpperCase()`.

Rejeté parce que la distinction n'a aucun fondement sur le chemin modifié : la casse est l'un des trois filtres
qui bâtissent l'alphabet de remplissage, appliqué caractère par caractère exactement comme la famille et les
soustractions. La conserver préserverait une classe de contradictions que la règle uniforme supprime, et ferait
de la règle deux règles — une pour les alphabets, une pour la casse — là où l'agencement n'en justifie qu'une.

### Une option explicite sur les méthodes de fragment, demandant l'exemption

Elle laisserait coexister les deux lectures sans rien casser.

Rejeté parce qu'elle élargit la surface publique pour offrir un choix dont personne n'a besoin dans l'autre
sens : un appelant qui écrit un littéral a déjà dit ce que sont ces caractères. Une option documenterait une
hésitation plutôt qu'une règle.

## Conséquences

### Positives

* Un préfixe fixe suivi d'un corps contraint devient exprimable, chacune des règles du format restant un appel
  nommé plutôt qu'un littéral de pool bâti à la main.
* Les valeurs honorent l'invariant que la chaîne énonce : le séparateur apparaît dans le préfixe et nulle part
  ailleurs.
* La sémantique des contraintes cesse de dépendre de l'ordre de déclaration, puisqu'aucune combinaison d'une
  contrainte de caractères et d'un fragment ne peut plus échouer.
* `JD015` devient plus petite et plus juste : une seule vérification, exactement alignée sur l'exécution.
* Ce qu'elle cesse de refuser, `JD031` le rapporte en information : le caractère du littéral apparaît là où il a
  été écrit et nulle part ailleurs. La lecture que portait le refus survit sous forme de note, ce qu'elle aurait
  dû être — un format à préfixe fixe est l'usage voulu, et seul son auteur le distingue d'un lapsus.
* C'est un assouplissement, pas une rupture — aucune chaîne qui fonctionne aujourd'hui ne cesse de fonctionner,
  et aucune valeur générée ne change de forme. Seul du code affirmant l'exception retirée est concerné.

### Négatives

* `UpperCase()` et `LowerCase()` ne signifient plus « chaque lettre de la valeur porte cette casse » sur le
  chemin constructif ; elles le disent des lettres que le générateur a tirées. Un littéral en minuscules
  déclaré à côté d'`UpperCase()` est conservé tel quel, et la faute de frappe autrefois signalée passe
  désormais en silence.
* `JD015` perd trois de ses quatre vérifications : une chaîne contredisant sa propre famille n'est plus
  signalée à la compilation — parce que ce n'est plus une contradiction.

### Risques

* Les deux chemins se lisent différemment au site d'appel — `UpperCase().Containing("abc")` conserve désormais
  `"abc"` alors qu'`OneOf("abc").UpperCase()` le rejette toujours — et un lecteur qui apprend la règle comme
  « un littéral que vous avez écrit est exempté » attendra que le second soit accepté aussi et trouvera le
  comportement arbitraire. La règle qui prédit les deux porte sur ce qui est tiré, jamais sur qui l'a écrit ; la
  documentation doit l'enseigner ainsi, car la lecture par l'auteur est l'intuitive et elle est fausse.
* Un littéral portant des caractères que la famille déclarée ne peut pas tirer est accepté : une vraie erreur —
  un préfixe en minuscules à côté d'`UpperCase()`, un séparateur à côté d'une famille choisie par habitude — ne
  fait donc plus échouer quoi que ce soit. `JD031` la met sous les yeux de l'auteur en `Info`, ce qui est une
  note et non un arrêt : une base de code qui éteint la règle perd le dernier témoin de ce cas.

## Actions de suivi

* Observer si `JD031` continue d'apprendre quelque chose. Elle est en `Info` précisément pour que la réponse
  vienne de l'usage plutôt que d'une prédiction : une base de code qui écrit partout des formats à préfixe fixe
  peut raisonnablement l'éteindre, et si cela devient l'issue courante, la règle dit aux lecteurs ce qu'ils
  savent déjà.

## Références

* Issue #94 — le signalement, le contournement mesuré et les critères d'acceptation.
* [ADR-0075](0075-draw-characters-from-the-whole-of-ascii.fr.md) — chaque famille ne fait que restreindre ce
  qu'un tirage tire ; le présent enregistrement fixe ce que « un tirage » recouvre.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — l'ambition est bornée, la
  correction d'une valeur rendue ne l'est jamais.
* [ADR-0008](0008-generate-strings-from-a-home-grown-regular-subset.fr.md) — le rejet auquel se heurte un DSL
  par segment.
* [ADR-0014](0014-enforce-structural-any-conflicts-at-compile-time.fr.md) — le cas dépendant de la valeur que
  porte un analyseur ; son illustration a été replacée sur le budget de longueur par la présente décision.
* [ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.fr.md) — le pool de l'appelant comme
  spécification entière, sur les points d'entrée génériques.
* [JD015](../../for-users/analyzers/JD015.fr.md) — la règle restreinte.
