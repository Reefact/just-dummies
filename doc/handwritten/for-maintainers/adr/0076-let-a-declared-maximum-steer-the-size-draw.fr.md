# ADR-0076 | Laisser un maximum déclaré piloter le tirage de taille

🌍 🇬🇧 [English](0076-let-a-declared-maximum-steer-the-size-draw.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-08-18
**Accepted:** 2026-08-18
**Decision Makers:** Reefact

Supersède l'[ADR-0029](0029-let-a-size-maximum-cap-without-steering-the-draw.fr.md).

## Contexte

L'[ADR-0029](0029-let-a-size-maximum-cap-without-steering-the-draw.fr.md) a décidé qu'un maximum de taille
déclaré ne fait jamais que plafonner un tirage sans l'élargir au-delà de l'étendue par défaut, et qu'une
taille que le générateur doit réellement produire — une longueur ou un compte exact ou minimum — est refusée
au-delà de 1 000 000. Il a été accepté le 2026-07-28 et corrigeait quatre pathologies mesurées :
`WithMaxLength(int.MaxValue)` rendant une chaîne de 130 Mo, `WithMaxCount(int.MaxValue)` tournant plusieurs
minutes, et deux exceptions d'argument déroutantes.

Mesuré sur le `main` actuel, le comportement obtenu est :

| déclaration | longueur tirée |
| --- | --- |
| `Dummy.String()` | 0..16 |
| `Dummy.String().WithMaxLength(50)` | 0..16 |
| `Dummy.String().WithMaxLength(100000)` | 0..16 |
| `Dummy.String().WithLengthBetween(1000, 5000)` | 1000..1016 |
| `Dummy.String().WithMinLength(1000).WithMaxLength(5000)` | 1000..1016 |

L'ADR-0029 avait anticipé la réaction à la troisième ligne, dans ses propres conséquences *négatives*, et
avec le même nombre : *« `WithMaxLength(100000)` returning 0-to-16-character strings is the intended new
behaviour, and it will surprise anyone who read the bound as a size hint — the documentation has to state
the rule explicitly rather than let it be inferred. »* Le pari était que la documentation absorberait la
surprise. À l'usage, le mainteneur a atteint la quatrième ligne et l'a jugée non pas surprenante mais
incohérente : deux nombres écrits, 1,6 % de l'intervalle tiré.

Trois autres faits pèsent sur le choix.

**Un maximum déclaré n'est souvent pas écrit à la main.** Le scaffolder `dum` lit les gardes de constructeur
(ADR-0060) et émet la contrainte qu'elles impliquent : une garde `if (value.Length > 255) throw` produit
`Dummy.String().NonEmpty().WithMaxLength(255)`. Ce maximum n'est pas un appelant « qui exprime une limite » :
c'est l'invariant du domaine lui-même, lu sur le type. Le moteur en tire ensuite 1..17, honorant 6 % du
domaine déclaré.

**La règle de l'ADR-0029 admet déjà que quelque chose puisse demander davantage.** Sa justification énonce :
*« a dummy is small unless something explicitly asks for more, and only a minimum, an exact size or a
required fragment can ask. »* Un minimum de 1000 est une telle demande. Ce que le record ne tranche pas,
c'est de combien — et sa réponse, le minimum plus l'étendue par défaut, est la même constante qu'un maximum
ait été écrit à côté ou non.

**L'asymétrie entre les arguments est l'inverse de ce qu'un maximum pilotant exige.** `WithLength` et
`WithMinLength` sont plafonnés à 1 000 000 ; `WithMaxLength` et `WithMaxCount` ne sont validés que sur la
non-négativité, parce que l'ADR-0029 raisonnait qu'*« a maximum is free to honour once it no longer steers
the draw »*. Le paramètre est un `int`, donc un maximum pilotant non plafonné atteindrait 2 147 483 647 —
une chaîne de 4 Go.

Enfin, l'[ADR-0049](0049-replay-a-seed-across-patch-and-minor-versions.fr.md) fait de tout changement de ce
que produit un tirage non contraint une version majeure, garantie par un golden master qui épingle valeurs
et nombres de tirages. L'[ADR-0075](0075-draw-characters-from-the-whole-of-ascii.fr.md) engage déjà ce cycle
sur une majeure.

## Décision

Un tirage de taille est uniforme sur l'intervalle fermé [minimum, maximum], où un maximum non déclaré vaut
le minimum plus l'étendue par défaut de la famille — 1024 pour une longueur de chaîne, inchangée pour un
compte de collection — et tout argument de taille, maxima désormais compris, est refusé au-delà de
1 000 000.

## Justification

**Une borne que l'appelant a écrite devrait gouverner la valeur qu'il reçoit.** Deux nombres écrits et 1,6 %
de l'intervalle tiré n'est pas une politique qu'un lecteur peut retenir ; `WithLengthBetween(1000, 5000)` a
une lecture évidente, et la bibliothèque devrait l'avoir. La décomposition survit intacte, parce que le
maximum pilote sous les deux écritures : `WithLengthBetween(a, b)` et `WithMinLength(a).WithMaxLength(b)`
tirent toujours identiquement, ce que l'ADR-0029 protégeait en refusant de faire de `Between` un cas
particulier. Ce refus était sain et cette décision ne le contredit pas — elle change ce que signifie un
*maximum*, pas ce que signifie `Between`.

**Le scaffolder est l'argument qu'un maximum n'est pas qu'une permission.** La thèse centrale de l'ADR-0029
est qu'« au plus N » énonce ce qu'une valeur ne doit pas dépasser et ne dit rien de la taille voulue. C'est
vrai d'un maximum qu'un développeur tape pour protéger une colonne. Ce ne l'est pas d'un maximum que `dum`
dérive d'une garde de constructeur, qui est la plage déclarée du domaine — et c'est désormais le cas courant
dans le code scaffoldé. Une règle qui lit la même syntaxe de deux façons doit en choisir une, et choisir
« la plage déclarée » est la lecture qui fait qu'un dummy généré exerce le type pour lequel il a été généré.

**Le plafond bouge avec le sens, pas contre lui.** L'ADR-0029 refusait de plafonner les maxima au motif
qu'un maximum est gratuit à honorer ; dès qu'il pilote, il produit, et le motif disparaît. Appliquer un seul
plafond à tout argument de taille n'est donc pas un garde ajouté après coup : c'est la règle que l'ADR-0029
appliquait déjà aux tailles produites, portée à l'ensemble que les tailles produites sont devenues. Cela
rétablit aussi l'uniformité que l'ADR-0029 avait envisagée puis écartée : une phrase, aucune exception à
retenir.

**L'étendue par défaut est relevée parce que l'explicite doit être le chemin facile.** À 16, un dummy de
chaîne est assez court pour qu'aucun code ne rencontre jamais une chaîne longue : un invariant de longueur
n'est donc jamais exercé si un test ne l'énonce pas — et l'énoncer est précisément ce qu'un test se donne
rarement la peine de faire quand le défaut est confortable. À 1024, un `Dummy.String()` non contraint est
assez inconfortable pour que déclarer la vraie borne devienne le geste évident, et la déclaration est alors
honorée au lieu d'être ignorée. Les deux moitiés de cette décision travaillent ensemble : relever l'étendue
sans maximum pilotant ne ferait que grossir les dummies, et un maximum pilotant sans étendue relevée
laisserait l'appel non contraint aussi confortable qu'avant.

**Le remède doit se désigner lui-même, ce qu'une taille ne sait pas faire.** Un défaut inconfortable
n'enseigne que si le lecteur peut deviner quoi écrire à la place, et un mur de caractères dans un message
d'échec ne dit pas `WithMaxLength`. Le jeu d'analyzers est l'instrument de ce dépôt pour exactement cela
(ADR-0038) : une règle informative signalant une chaîne `Dummy.String()` qui ne déclare aucune longueur nomme
le remède au site d'appel, ne coûte ni tirage ni version, et se supprime là où la longueur n'importe
réellement pas. Elle fait partie de cette décision plutôt que d'une autre, car sans elle le défaut relevé
est une punition et non une incitation.

**Le compte de collection garde sa magnitude en adoptant la politique.** Une collection de 1024 éléments
coûte ce que coûte son générateur d'éléments, multiplié — un autre ordre de dépense que 1024 caractères — et
aucun usage n'a été rapporté contre l'étendue actuelle. Le maximum y pilote pour la même raison que sur les
chaînes ; l'étendue reste où elle est jusqu'à ce qu'un cas plaide autrement.

## Alternatives envisagées

### Garder l'ADR-0029 et documenter la règle plus fort

Le pari que l'ADR-0029 avait lui-même fait : énoncer la règle dans la documentation utilisateur et compter
sur le lecteur.

Rejetée parce que l'expérience a eu lieu. Le record a prédit la surprise, choisi de la payer, et le premier
usage soutenu de la bibliothèque a produit la réaction annoncée — de la part de celui qui l'avait accepté.
Un coût accepté qui se révèle plus élevé que son estimation est la raison ordinaire de revisiter une
décision, et la période de préversion existe pour faire remonter exactement cela.

### Relever l'étendue par défaut sans faire piloter le maximum

Envisagée comme la moitié la plus petite : elle rend l'appel non contraint inconfortable, ce qui est
l'essentiel de l'intention, et ne touche aucune borne déclarée.

Rejetée parce qu'elle aggrave le problème. Le lecteur rendu inconfortable par un dummy de 1024 caractères
irait chercher `WithMaxLength(50)` — et obtiendrait 0..16, une valeur dont la taille n'a rien à voir avec ce
qu'il a écrit. L'incitation le pousserait précisément vers le comportement qui se lit comme cassé.

### Faire piloter le maximum sans relever l'étendue par défaut

Envisagée comme l'autre moitié, et la plus conservatrice : elle corrige les lignes incohérentes et laisse le
tirage non contraint tranquille.

Rejetée comme insuffisante plutôt que fausse. C'est une vraie amélioration, et elle tiendrait seule. Mais
elle laisse `Dummy.String()` assez confortable pour être employé non contraint par défaut, ce qui est ce qui
laisse les invariants de longueur inexercés — et ce cycle paie déjà une version majeure, donc le moment de
déplacer le défaut est maintenant plutôt qu'à la suivante.

### Plafonner le pilotage, pour qu'un maximum rétrécisse l'étendue sans jamais l'élargir

Envisagée comme un intermédiaire : tirer sur [min, min + étendue] intersecté avec [min, max], de sorte que
`WithMaxLength(50)` donne 0..50 tandis que `WithMaxLength(1000000)` rende encore la petite chaîne ordinaire,
sans plafond sur les maxima.

Rejetée parce qu'elle répond à deux des trois lignes incohérentes et pas à la troisième.
`WithMinLength(1000).WithMaxLength(5000)` tirerait encore 1000..1016, donc un intervalle écrit resterait
largement inutilisé, et la règle exigerait une phrase expliquant quand un maximum compte et quand il ne
compte pas. La lecture uniforme coûte un plafond et achète une règle sans cas particuliers.

## Conséquences

### Positives

* Un intervalle déclaré est l'intervalle tiré, sous toutes les écritures, et un `WithMaxLength(255)`
  scaffoldé exerce la plage que sa garde de constructeur déclare.
* Une seule règle pour tout argument de taille : refusé au-delà de 1 000 000, aucune exception à retenir.
* Un `Dummy.String()` non contraint est assez inconfortable pour que déclarer la vraie borne soit le chemin
  de moindre résistance, et le nouvel analyzer nomme cette borne au site d'appel.
* Les quatre pathologies mesurées par l'ADR-0029 restent corrigées : le plafond couvre désormais les maxima
  qui en étaient dispensés parce qu'ils ne pilotaient pas.

### Négatives

* Une **version majeure**, à double titre : l'étendue non contrainte bouge, et tout tirage sous un maximum
  déclaré aussi. Combinée à l'ADR-0075, toute la correspondance de graine est remplacée, et le golden master
  avec elle.
* `WithMaxLength(4_000_000)` — un plafond calqué sur une limite de stockage au-dessus du plafond — est
  désormais refusé à la déclaration, là où l'ADR-0029 le gardait délibérément légal. Ce cas doit être
  réécrit autour du plafond, ou plaider pour son relèvement.
* Les suites de tests deviennent plus lentes et plus bruyantes : une chaîne non contrainte est 64 fois plus
  longue qu'avant, et 1024 caractères atterrissent dans chaque message d'échec qui en imprime une.
* Quiconque lisait `WithMaxLength` comme une pure permission — la lecture qu'enseignait l'ADR-0029 — obtient
  désormais des valeurs plus grandes qu'attendu. La documentation doit énoncer la nouvelle règle aussi
  explicitement qu'elle énonçait l'ancienne.

### Risques

* L'étendue relevée peut pousser les consommateurs à déclarer un maximum partout, y compris là où aucun
  invariant n'existe, transformant un vrai vocabulaire de contraintes en boilerplate. Atténué par le
  caractère informatif et supprimable de l'analyzer, et en documentant qu'un maximum est une permission
  qu'un test a le droit de ne pas avoir.
* Le plafond reste une constante sans dérivation, héritée de l'ADR-0029 et désormais appliquée à un ensemble
  plus large. Il est défendable, pas démontrable.
* Deux décisions dans un seul record — le maximum pilotant et l'étendue relevée — ne pourraient être
  séparées plus tard qu'en supersédant les deux. Elles sont consignées ensemble parce que chacune est un
  mauvais compromis sans l'autre.

## Actions de suivi

* Basculer le statut de l'ADR-0029 en *Superseded* avec un lien vers ici, une fois ce record accepté.
* Revisiter l'étendue du compte de collection sur ses propres preuves ; ce record déplace la politique et
  non la magnitude.
* Revisiter l'[ADR-0031](0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.fr.md), où la même
  question s'applique aux nombres : une borne déclarée hors de la magnitude ordinaire ne pilote pas non plus
  le tirage.
* Énoncer la règle dans la documentation utilisateur, anglaise et française, là où les contraintes de taille
  sont décrites — l'action de suivi que l'ADR-0029 avait consignée, portant désormais la règle inverse.

## Références

* [ADR-0029](0029-let-a-size-maximum-cap-without-steering-the-draw.fr.md) — la décision que celui-ci
  supersède, et la conséquence *négative* qui avait prédit cette réaction.
* [ADR-0049](0049-replay-a-seed-across-patch-and-minor-versions.fr.md) — pourquoi c'est une version
  majeure.
* [ADR-0075](0075-draw-characters-from-the-whole-of-ascii.fr.md) — la moitié « alphabet » du même principe,
  et la version majeure que celui-ci partage.
* [ADR-0038](0038-guard-the-recipe-versus-value-boundary-with-analyzers.fr.md) — l'instrument auquel la
  nouvelle règle appartient.
* [ADR-0060](0060-seed-generators-from-constructor-guards.fr.md) — le scaffolder qui dérive un maximum d'un
  invariant de domaine.
