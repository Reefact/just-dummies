# ADR-0077 | Admettre une règle JD qui signale une écriture correcte, bornée par un équivalent nommé exact

🌍 🇬🇧 [English](0077-admit-a-rule-that-reports-a-correct-spelling.md) · 🇫🇷 Français (ce fichier)

**Status:** Superseded by [ADR-0080](0080-admit-a-rule-that-names-a-resolved-ambiguity.fr.md)
**Proposed:** 2026-08-20
**Accepted:** 2026-08-20
**Decision Makers:** Reefact

## Contexte

Le package `JustDummies` embarque trente règles Roslyn (ADR-0023), et le terrain sur lequel elles ont été
admises est que le système de types ne peut pas atteindre l'endroit où vit l'erreur (ADR-0038) : une recette
et une valeur tirée satisfont les mêmes signatures, une graine épinglée hors de sa portée compile quand
même, un jeu de contraintes qui n'admet aucune valeur est une chaîne parfaitement bien typée. Le README des
règles énonce la taxonomie de sévérité qui en découle — les erreurs sont des défauts durs, les
avertissements signalent des erreurs probables, les règles d'information sont des conventions.

Toutes les règles livrées à ce jour signalent quelque chose que l'auteur n'a probablement pas voulu, y
compris les trois règles d'information de la catégorie `Constraints`. JD024 signale une contrainte qui ne
rétrécit rien, JD029 une valeur écrite dans un pool qu'aucun tirage ne peut rendre, JD030 une chaîne de
caractères qui ne déclare aucune longueur et tire donc sur toute l'étendue par défaut. JD030 est la plus
proche de la frontière : ce qu'elle signale est légal, délibéré sur certains sites d'appel, et vrai — la
chaîne tire bien cette étendue — et elle le signale parce que l'auteur ne le sait probablement pas.

La bibliothèque livre des paires d'écritures équivalentes **par construction**, non par coïncidence.
`DummyString.WithLengthBetween` est implémentée comme les deux bornes qu'elle remplace, et sa documentation
énonce que les deux formes se comportent à l'identique, ce qui est précisément ce qui garde l'intervalle
décomposable. La même paire existe pour les générateurs de collections, et dix-neuf surcharges de `Between`
couvrent les numériques, `TimeSpan` et les temporels. À côté d'elles, la bibliothèque livre des alias exacts
d'une borne unique : `NonEmpty` est une longueur minimale de un, `Positive` un minimum de un, `Zero` une
paire de bornes.

L'écriture décomposée est légale, documentée et délibérément décomposable — un helper partagé pose un
plancher et un site d'appel ajoute un plafond — donc les deux formes doivent rester disponibles. Là où elles
diffèrent est ailleurs : la forme intervalle enregistre un seul appel de contrainte partagé par les deux
bornes, si bien qu'un conflit levé plus tard nomme l'intervalle, tandis que la forme décomposée en
enregistre deux et nomme la borne qu'il a heurtée.

L'issue #95 rapporte ce que cela laisse ouvert. Un lecteur qui écrit les bornes séparément n'apprend jamais
que la forme intervalle existe ; découvrir `WithLengthBetween` a demandé une lecture de la documentation.
Elle propose une règle pour cela, et sa propre argumentation montre où une telle règle cesse d'être juste :
`GreaterThan(5).LessThan(10)` sur un type entier est l'intervalle six à neuf et non cinq à dix, et sur un
type flottant il n'a pas de forme intervalle du tout — une règle qui le signalerait réécrirait donc les
nombres que l'auteur a écrits, ou proposerait une contrainte qui n'existe pas.

L'ADR-0046 est la règle que cette base partage déjà pour les questions de cette forme : borner ce que la
bibliothèque tente, et refuser à la frontière plutôt que d'aller chercher un mécanisme plus capable.

## Décision

Une règle qui signale une écriture correcte et délibérée est admise dans le jeu JD en sévérité information
quand, et seulement quand, la bibliothèque nomme elle-même une forme plus courte exactement équivalente par
construction et atteignable sans arithmétique sur les arguments de l'auteur.

## Justification

**Le terrain qu'énonce l'ADR-0038 ne porte pas cette règle, et la vraie propriété commune du jeu la
porte.** Rien n'est faux sur un tel site d'appel, donc « le système de types ne peut pas l'atteindre » n'est
pas l'argument — il n'y a rien qu'un système de types aurait pu attraper. Ce que les trente règles partagent
réellement est plus étroit que la chasse au défaut et plus large que cette formulation : chacune porte au
site d'appel un fait que l'auteur ne détient probablement pas. JD030 est le précédent déjà accepté, et elle
signale quelque chose de légal et de vrai exactement pour cette raison. Un équivalent plus court dont
l'auteur ignore l'existence est un fait de même nature, et l'admettre qualifie le terrain de l'ADR-0038
plutôt qu'il ne s'en écarte.

**La sévérité information est ce qui empêche les analyzers de contredire la documentation de l'API.** La
forme décomposée n'est pas seulement tolérée, elle est bénie en toutes lettres et sa décomposabilité est une
propriété que la bibliothèque maintient exprès. Un avertissement dirait au lecteur que la documentation a
tort. L'information dit ce que le README des règles dit qu'information veut dire : une convention, un fait à
peser, jamais un verdict.

**L'exactitude par construction est ce qui empêche la règle de devenir un moteur à goûts.** Un critère qui
admettrait des équivalences « à peu près » mettrait les analyzers dans le métier de préférer un programme
correct à un autre, et la frontière entre les deux se plaiderait au cas par cas pour toujours. Exiger que la
forme courte soit implémentée comme la longue rend l'équivalence vérifiable plutôt que discutable, et c'est
le geste que l'ADR-0046 fait partout ailleurs dans cette base : borner la surface, et faire de la frontière
une chose que l'on peut désigner.

**Exiger que la bibliothèque ait nommé la forme fait de ceci de la découvrabilité et non du style.** Le nom
existe déjà et a déjà été choisi ; la règle ne fait que le porter là où il aurait servi. Une règle qui
proposerait une forme que la bibliothèque ne livre pas serait une proposition de conception déguisée en
analyzer.

**Interdire l'arithmétique sur les arguments est ce qui garde la règle juste.** Une suggestion qui change
les nombres est une autre contrainte portant le même nom, et sur un type sans valeur suivante il n'y a
aucune suggestion à faire. C'est la condition qui tranche le cas des bornes strictes, et elle le tranche de
la même façon pour tous les types au lieu de type par type.

**La forme courte n'est pas seulement plus courte, et c'est ce qui vaut une règle plutôt qu'un guide de
style.** Parce que la forme intervalle enregistre un seul appel de contrainte, un conflit levé plus tard
contre elle nomme l'intervalle que l'auteur a écrit plutôt qu'une de ses moitiés. La règle désigne donc une
écriture dont la conséquence est observable, propriété que possède toute autre règle d'information de cette
catégorie.

**Écrire le critère est la décision ; la première règle n'en est que l'instance.** Trancher #95 seul
laisserait le candidat suivant à qui l'argumenterait le mieux. Trois conditions vérifiables contre les
sources d'un générateur règlent JD032 et tout ce qui suivra sans seconde lecture — ce qui est la raison même
pour laquelle l'ADR-0046 existe au lieu de sept décisions de bornage séparées.

**Le critère admet délibérément davantage que le cas qui l'a provoqué.**
`Dummy.String().WithMinLength(1)` possède une forme nommée plus courte implémentée exactement comme cette
contrainte : elle satisfait les trois conditions. Un critère assez étroit pour n'admettre qu'une seule règle
ne serait pas un critère, et le coût des candidats supplémentaires est borné — chacun doit encore mériter
une issue, un identifiant et deux pages de documentation.

## Alternatives envisagées

### Combler le manque dans la documentation XML

Le manque que rapporte #95 est un manque de documentation, et le remède le plus proche est une prose sur les
deux bornes pointant vers la forme intervalle, qu'IntelliSense afficherait pendant que l'auteur écrit.

Rejetée parce qu'elle atteint le mauvais lecteur. La documentation d'un membre est lue par quelqu'un qui
hésite sur ce membre ; un auteur en train d'écrire la seconde borne a déjà décidé quoi écrire et n'hésite
pas. La population pour laquelle cette règle existe est précisément celle qui n'ouvre jamais l'infobulle —
c'est d'ailleurs en lisant la documentation comme un document, non comme une infobulle, que l'auteur de #95
a trouvé la méthode.

### Livrer un refactoring Roslyn plutôt qu'un diagnostic

« Il existe une écriture équivalente plus courte » a la forme d'un refactoring : une ampoule propose la
réécriture, rien n'est souligné, et aucune sortie de build ne change. Cela ne coûterait ni identifiant de
diagnostic ni surface de catalogue.

Rejetée parce qu'un refactoring n'est jamais découvert que par quelqu'un qui pose le curseur sur le code et
demande ce qu'on pourrait en faire, et personne ne le demande d'un code dont il est satisfait. C'est un bon
instrument pour *effectuer* une réécriture et un instrument inutile pour *annoncer* qu'une réécriture
existe — or annoncer est tout l'enjeu ici. Le suivi que #95 décrit déjà — un code fix à côté de la règle —
est le bon foyer pour la moitié qui effectue.

### Signaler en sévérité avertissement

Cohérent avec le reste de la catégorie `Constraints` en nombre, et cela rendrait la suggestion plus
difficile à ignorer.

Rejetée : la forme décomposée est documentée comme correcte et sa décomposabilité est maintenue exprès, donc
un avertissement dresserait les analyzers contre la documentation de l'API. La taxonomie qu'énonce le README
des règles place les avertissements sur les erreurs probables, et il ne s'agit pas de l'une d'elles.

### Tenir le jeu aux seuls défauts et refuser la règle

La frontière la plus simple disponible, ne demandant aucun critère et aucune discussion sur l'endroit où
commence le style.

Rejetée parce que le jeu ne se tient pas actuellement sur cette frontière. JD030 signale déjà quelque chose
de légal, de vrai et de délibéré sur certains sites d'appel, et refuser ici la laisserait en anomalie
qu'aucune règle n'explique. Le choix n'est pas de savoir si le jeu signale des faits en plus des défauts —
il le fait déjà — mais si la condition sous laquelle il le peut est écrite ou improvisée.

## Conséquences

### Positives

* Le candidat suivant se tranche en lisant trois conditions contre les sources d'un générateur, au lieu de
  rejouer le débat de frontière.
* L'écriture décomposée reste légale, documentée et décomposable ; rien ne change dans les générateurs.
* Le manque de découvrabilité se comble au site d'appel, là où l'auteur peut agir.
* Les conditions excluent les cas non justes — bornes strictes, paires mixtes, types flottants — par
  construction plutôt qu'au cas par cas.

### Négatives

* Le jeu contient désormais des règles de deux natures, et la phrase de cadrage du README des règles doit le
  dire.
* Chaque règle admise coûte un identifiant de diagnostic, une page anglaise et une française, une ligne dans
  chaque table de règles, une constante de catalogue publiée sur son propre train de release (ADR-0052), et
  une mise à jour de chaque décompte des règles.
* Un auteur ayant écrit la forme décomposée exprès voit un diagnostic d'information disant que rien n'est
  faux, petite taxe payée sur chacun de ces sites d'appel.

### Risques

* Le critère admet plus de candidats que personne n'en a énumérés, et une vague de règles d'alias serait du
  bruit même en sévérité information. L'atténuation est que chacune passe encore par une issue, où la
  question est celle du gain contre le coût et non de l'admissibilité.
* Un lecteur futur pourrait prendre « écriture correcte » pour un blanc-seing sur les règles de style en
  général. Les trois conditions sont la réponse, et c'est la raison pour laquelle ce record énonce un critère
  plutôt qu'un verdict.

## Actions de suivi

* Implémenter JD031 sous ce critère (#95).
* Énoncer le critère sur les pages JD031, pour qu'un lecteur qui tombe sur un cas muet comprenne pourquoi il
  l'est.
* Reprendre la phrase de cadrage du README des analyzers, qui énonce aujourd'hui que chaque règle comble un
  manque que le système de types ne peut pas atteindre.

## Références

* [ADR-0023](0023-ship-justdummies-analyzers.fr.md) — les règles sont embarquées dans le package.
* [ADR-0038](0038-guard-the-recipe-versus-value-boundary-with-analyzers.fr.md) — le terrain que ce record qualifie.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — borner la surface, et rendre la frontière désignable.
* [ADR-0052](0052-publish-the-jd-rules-as-a-first-party-catalogue.fr.md) — le catalogue par lequel chaque nouvel identifiant est publié.
* [Issue #95](https://github.com/Reefact/just-dummies/issues/95) — la règle que ce critère admet en premier.
