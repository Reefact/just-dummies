# ADR-0080 | Admettre une règle JD qui nomme une ambiguïté que la bibliothèque tranche, à côté de l'équivalent plus court

🌍 🇬🇧 [English](0080-admit-a-rule-that-names-a-resolved-ambiguity.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-08-20
**Accepted:** 2026-08-20
**Decision Makers:** Reefact

Supersède l'[ADR-0077](0077-admit-a-rule-that-reports-a-correct-spelling.fr.md).

## Contexte

L'[ADR-0077](0077-admit-a-rule-that-reports-a-correct-spelling.fr.md) a admis, pour la première fois, une règle
JD qui signale un site d'appel correct et délibéré. Il a borné cette admission à un cas : la bibliothèque nomme
elle-même une forme plus courte exactement équivalente par construction, atteignable sans arithmétique sur les
arguments de l'auteur. Sa justification énonce la propriété que le jeu de règles partage réellement — **chacune
porte au site d'appel un fait que l'auteur a peu de chances de détenir** — et sa Décision énumère l'unique
instance de cette propriété qu'il avait en vue. JD031 est cette instance.

L'[ADR-0079](0079-constrain-what-a-dummy-draws-never-the-literals-it-was-given.fr.md) en a produit une seconde,
d'une autre forme. Une famille de caractères, un pool personnalisé, une soustraction et une casse gouvernent les
caractères qu'`Dummy.String()` **tire** ; un littéral ancré n'est pas tiré et est conservé tel qu'écrit. Ainsi
`Dummy.String().AlphaNumeric().StartingWith("ORD-")` est légal et constitue la façon simple d'écrire un séparateur
fixe. Lue seule, cette chaîne dit deux choses de ses caractères : seulement de l'alphanumérique, puis un tiret.
Les déclarations se contredisent ; l'ADR-0079 tranche laquelle gouverne. Rien n'est fautif au site d'appel, et
laquelle des deux lectures s'applique n'est pas visible depuis la chaîne — seulement depuis un enregistrement de
décision, ou en lançant un tirage pour regarder.

Ce site d'appel n'a **aucune** forme plus courte équivalente. La bibliothèque ne nomme pas d'autre façon de
l'écrire. Le critère de l'ADR-0077 l'exclut donc, non parce que la règle serait malsaine, mais parce que la
condition qu'il teste ne s'applique pas du tout à cette forme.

Les deux cas partagent la propriété que nomme la justification de l'ADR-0077, et ils partagent la raison pour
laquelle l'information est la bonne sévérité : tous deux signalent quelque chose que la documentation de la
bibliothèque bénit, donc un avertissement dirait au lecteur que la documentation a tort.

La borne de l'ADR-0077 est dans sa **Décision**, sous la forme « when, and only when ». Un enregistrement
accepté est immuable (ADR-0002) : l'élargir est donc une supersession, pas une retouche.

## Décision

Une règle qui signale une écriture correcte et délibérée est admise dans le jeu JD à la sévérité information
quand, et seulement quand, elle porte un fait que la bibliothèque fixe plutôt qu'un fait qu'un lecteur pourrait
préférer — soit la bibliothèque nomme une forme plus courte exactement équivalente par construction et
atteignable sans arithmétique sur les arguments de l'auteur, soit deux des déclarations de la chaîne elle-même
émettent des affirmations contradictoires sur les mêmes caractères ou les mêmes valeurs et une décision
enregistrée tranche laquelle gouverne.

## Justification

**Cela élargit l'énumération, pas le terrain.** L'ADR-0077 avait déjà identifié ce que le jeu a en commun et
déjà accepté qu'un site d'appel correct puisse mériter d'être signalé ; JD030 en était le précédent et JD031 la
première instance. Ce qu'il ne pouvait pas faire, c'était prévoir une seconde forme, puisqu'elle n'existait pas
avant que l'ADR-0079 ne la crée. Rien dans son raisonnement ne s'oppose au nouveau cas — ses conditions testent
simplement une propriété que ce cas n'a pas, et un critère qui admet une seule instance n'est pas encore un
critère.

**La seconde borne est aussi pointable que la première, et c'est ce qui tient tout ça hors du goût.**
L'ADR-0077 refuse les équivalences « à peu près » parce que la frontière s'argumenterait alors cas par cas sans
fin, et il achète sa vérifiabilité avec l'*exactitude par construction*. Le nouveau membre l'achète de la même
façon, avec deux conditions vérifiables sur la source plutôt que discutables : la contradiction doit opposer
**deux contraintes déclarées** — pas la surprise d'un lecteur, pas une valeur qui a seulement l'air bizarre — et
sa résolution doit être **enregistrée dans une décision**, non déduite par l'auteur de la règle. Une chaîne sur
laquelle personne n'a eu à trancher ne produit aucune règle sous ce membre.

**Se déclencher sur le cas délibéré est ici le but, pas le coût.** Le membre « équivalent plus court » signale
des sites d'appel dont l'auteur ignorait simplement qu'un nom existait. Celui-ci signale des sites d'appel qui
portent une vraie ambiguïté — et l'ambiguïté est portée précisément par les délibérés, puisqu'un séparateur
écrit dans un préfixe est exactement la chaîne qui déclare un alphabet puis en sort. Une règle qui ne se
déclencherait que sur les erreurs devrait deviner lesquelles le sont, et aucune règle ne le peut : le séparateur
délibéré et le préfixe mal frappé ont la même forme pour un compilateur. Signaler le fait et laisser le jugement
à l'auteur, c'est à cela que sert la sévérité information.

**L'information empêche les analyseurs de contredire la documentation d'API**, exactement comme l'ADR-0077
l'argumente. L'exemption n'est pas tolérée mais conçue, documentée sur chaque famille, chaque soustraction et
chaque casse, et c'est elle qui rend un format ordinaire exprimable. Un avertissement dirait que la
documentation a tort.

**Écrire le second membre maintenant est ce qui évite que le prochain candidat soit argumenté à partir de
rien.** L'ADR-0077 tenait cet argument pour lui-même — « writing the criterion down is the decision; the first
rule is only its instance » — et il vaut une forme plus loin. Deux membres vérifiables chacun sur la source d'un
générateur règlent les règles postérieures à JD033 sans une troisième lecture.

## Alternatives envisagées

### Laisser l'ADR-0077 en l'état et refuser la règle

Cela ne coûte rien, laisse intact un enregistrement accepté, et l'exemption fonctionne que quelque chose la
signale ou non.

Rejeté parce que l'ambiguïté n'atteint alors le lecteur que par un enregistrement de décision qu'il n'a aucune
raison d'ouvrir, et parce que la forme fautive — un préfixe en minuscules à côté d'`UpperCase()` — perd le
dernier témoin qui l'aurait fait remonter. L'ADR-0079 a retiré un refus à dessein ; ne rien mettre du tout à sa
place jette un fait que l'auteur a peu de chances de détenir, propriété sur laquelle tout le jeu est bâti.

### Élargir le critère à toute ambiguïté qu'un lecteur pourrait rencontrer

C'est le texte le plus court, et il admettrait les deux cas sans les énumérer.

Rejeté pour la raison que l'ADR-0077 oppose au « à peu près » : un critère reposant sur ce qu'un lecteur
pourrait trouver surprenant met les analyseurs dans le commerce de préférer un programme correct à un autre, et
la frontière bouge avec celui qui argumente. Exiger deux contraintes déclarées et une résolution enregistrée
garde le test mécanique.

### Admettre la règle sur le terrain de l'ADR-0038, comme chercheuse de défauts

La forme fautive est une vraie erreur, et l'ADR-0038 est là où vivent les règles qui cherchent des défauts.

Rejeté parce que la population est à l'envers : sur les suites de ce dépôt, la règle signale neuf chaînes
délibérées pour une fautive. L'appeler chercheuse de défauts décrirait mal ce qu'elle fait neuf fois sur dix, et
inviterait un mainteneur ultérieur à monter sa sévérité sur cette mauvaise description.

## Conséquences

### Positives

* Le jeu de règles gagne un critère qui couvre les deux formes qu'il contient désormais : le prochain candidat
  se vérifie au lieu de s'argumenter.
* JD033 est admise sur un terrain énoncé plutôt que par exception.
* Le premier membre de l'ADR-0077 survit mot pour mot : JD031 n'a besoin d'aucune re-justification.

### Négatives

* Un enregistrement accepté est supersédé le jour même de son acceptation : un lecteur suivant un lien depuis
  JD031 atterrit donc sur un enregistrement dont le successeur porte le texte opérant. L'index et l'en-tête
  portent le renvoi, mais le détour est réel.
* Deux membres se tiennent moins bien en tête qu'un seul, et un futur candidat n'entrant dans aucun des deux
  sera tentant à faire rentrer dans le second, dont le sujet — « affirmations contradictoires » — est moins net
  qu'« exactement équivalent ».

### Risques

* La solidité du second membre repose sur la condition de décision enregistrée. Si une règle y est un jour
  admise en pointant une décision écrite pour l'occasion, le membre devient le « taste engine » contre lequel
  l'ADR-0077 se prémunissait. La condition n'est une contrainte réelle que tant que la décision précède la
  règle.

## Actions de suivi

* Observer la formulation des règles admises sous le second membre. Elles signalent du code délibéré par
  construction : chacune doit donc se lire comme une note sur ce qu'une déclaration signifie, non comme un
  reproche.

## Références

* [ADR-0077](0077-admit-a-rule-that-reports-a-correct-spelling.fr.md) — l'enregistrement supersédé ; son premier
  membre est repris inchangé.
* [ADR-0079](0079-constrain-what-a-dummy-draws-never-the-literals-it-was-given.fr.md) — la décision qui a créé
  la seconde forme.
* [ADR-0038](0038-guard-the-recipe-versus-value-boundary-with-analyzers.fr.md) — le terrain des règles qui
  cherchent des défauts, qu'aucun des deux membres n'emprunte.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — borner la surface, et faire de
  la frontière une chose que l'on peut montrer du doigt.
* [JD031](../../for-users/analyzers/JD031.fr.md), [JD033](../../for-users/analyzers/JD033.fr.md) — une instance
  de chaque membre.
