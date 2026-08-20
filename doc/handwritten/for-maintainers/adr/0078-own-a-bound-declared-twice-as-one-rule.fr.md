# ADR-0078 | Confier à une seule règle la borne déclarée deux fois, et en retirer JD024

🌍 🇬🇧 [English](0078-own-a-bound-declared-twice-as-one-rule.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-08-20
**Accepted:** 2026-08-20
**Decision Makers:** Reefact

## Contexte

JD024 signale une contrainte qui ne rétrécit rien, dans la catégorie `Constraints` et en sévérité
information. Sa raison consignée est qu'il s'agit de la seule famille de contraintes que l'exécution ne
rapporte jamais — toute autre contradiction finit par lever, bruyamment — et que le cas pour lequel elle
existe est l'exclusion d'une sentinelle que le générateur ne pourrait jamais tirer, silencieuse aujourd'hui
et qui se met à compter le jour où quelqu'un élargit l'intervalle. C'est cette lecture qui la place en
information plutôt qu'en avertissement : exclure une valeur que le domaine actuel ne peut pas produire est
un acte défensif légitime.

JD024 est levée par un seul analyzer, gardé sur une fabrique entière et raisonnant sur un domaine entier.
Les chaînes de caractères et les chaînes de collections ne la reçoivent jamais, et l'analyzer qui lit les
longueurs d'une chaîne ne lit que le maximum et la longueur exacte, pour une autre question.

Les bornes se replient silencieusement et de façon monotone dans toutes les familles. Un minimum garde la
plus grande des deux valeurs, un maximum la plus petite ; l'appel perdant renvoie le générateur inchangé.
Rien ne lève, et aucun rapport d'exécution ne le mentionne. Sur une chaîne qui déclare deux fois la même
borne, exactement l'un des deux appels est donc mort — toujours le plus lâche — quel que soit l'ordre
d'écriture. Dans l'ordre relâchant, le second appel est inerte ; dans l'ordre resserrant, le premier est
effacé par le second.

Des quatre combinaisons que cette forme peut prendre — deux ordres d'écriture croisés avec la famille
entière et la famille chaîne-ou-collection — une seule est rapportée aujourd'hui. Sur un scalaire entier
écrit de la borne la plus serrée vers la plus lâche, le second appel laisse le domaine inchangé et JD024 se
déclenche, disant que la contrainte est déjà impliquée par les contraintes déclarées avant elle. Les trois
autres sont muettes, et ce silence découle de l'ordre dans lequel les analyzers ont été écrits, non d'une
décision.

Déclarer séparément les bornes d'un intervalle est une fonctionnalité documentée de la bibliothèque : un
helper partagé peut poser un plancher et un site d'appel ajouter un plafond, ce qui est précisément ce qui
garde un intervalle décomposable. Les deux appels de la forme considérée ici tiennent dans une seule chaîne
fluente.

La bibliothèque livre aussi des alias exacts d'une borne unique — `NonEmpty` est une longueur minimale de
un, `Positive` un minimum de un — de sorte qu'une chaîne peut déclarer deux fois la même borne sous deux
noms différents.

Le README des règles énonce la taxonomie de sévérité : les erreurs sont des défauts durs, les avertissements
signalent des erreurs probables, les règles d'information sont des conventions. Ce dépôt traite un
changement de sémantique d'un identifiant de diagnostic comme une décision d'architecture et non comme un
correctif, et l'ADR-0077 vient de trancher quelles règles d'écriture le jeu JD admet, et à quelle sévérité.

## Décision

Une borne déclarée deux fois sur une même chaîne fluente est signalée par une règle qui lui est propre, en
sévérité avertissement, dans toutes les familles de générateurs, appariée sur le nom de la contrainte ;
JD024 ne signale plus cette forme et conserve la contrainte qui ne rétrécit rien.

## Justification

**Un phénomène mérite un identifiant.** L'appel mort est la borne la plus lâche dans les deux ordres
d'écriture, et l'erreur qui est derrière est la même — une borne écrite deux fois, le plus souvent par un
copier-coller ou une résolution de merge. La scinder en deux identifiants selon l'ordre d'écriture
donnerait à l'auteur un diagnostic différent, sur une autre page de catégorie, pour une différence qu'il n'a
pas faite exprès. Que les deux ordres soient rapportés aujourd'hui par des mécanismes différents est un
accident d'ordre d'implémentation, pas une distinction que quiconque a choisi de tracer.

**La raison qui place JD024 en information ne se transporte pas, donc cette règle n'hérite pas de sa
sévérité.** JD024 est en information parce qu'une contrainte inerte a une lecture défendable : l'auteur a
exclu une sentinelle avant que l'intervalle capable de la produire n'existe. Une borne écrite deux fois dans
une même chaîne n'a pas cette lecture. Les deux appels sont dans une seule expression, devant le même
lecteur, et le plus serré efface simplement le plus lâche — il n'existe aucun futur où l'appel effacé se
mettrait à compter. Selon la taxonomie qu'énonce le README des règles, c'est une erreur probable et non une
convention, ce qui est le rang de l'avertissement.

**La sévérité suit le mode de défaillance, pas la catégorie.** L'ADR-0038 a déjà tranché ce principe pour ce
jeu de règles en plaçant un vert silencieux en erreur et un vert probabiliste en avertissement. Tenir cette
règle à l'information par symétrie avec l'autre membre de sa famille serait une cohérence du mauvais genre.

**Élargir JD024 rendrait son propre message faux.** JD024 dit qu'une contrainte ne change rien. Dans l'ordre
resserrant, c'est exactement l'inverse : la contrainte écrite en second change le domaine, et l'appel mort
est celui écrit en premier. Un identifiant dont le message doit se lire comme faux sur la moitié des cas
qu'il couvre a cessé d'être une poignée stable vers une règle.

**JD024 se retire pour qu'une erreur ne tire qu'un diagnostic.** Dans l'ordre relâchant sur un scalaire
entier, les deux règles décriraient sinon le même appel, et deux diagnostics sur une expression pour une
seule erreur sont un bruit qui apprend au lecteur à désactiver les deux. Rétrécir JD024 au cas d'exclusion
pour lequel elle a été écrite la laisse dire exactement ce que son message dit.

**Étendre la règle à toutes les familles est une correction, pas une extension.** Le repliement est le même
partout et l'exécution est muette partout ; seule la couverture des analyzers diffère. Signaler une erreur
sur un entier et pas sur une chaîne, pour la même erreur avec la même conséquence, est le genre
d'incohérence qu'un utilisateur lit comme un défaut de l'outil.

**Apparier sur le nom plutôt que sur l'effet garde la règle sans faux positif, et met le cas des alias là où
il appartient.** Une chaîne qui atteint la même borne par deux noms différents écrit bien la borne deux fois
en effet, mais l'alias est un choix de lisibilité avec une lecture défendable — il dit sur l'intention
quelque chose que la borne explicite ne dit pas. Cela en fait une question d'écriture, dont l'ADR-0077 vient
de décider le traitement, et non une question d'appel mort. L'appariement sur le nom trace la ligne
exactement là, et il la trace là où un lecteur peut la voir.

**Rien d'autre ne le signalera.** L'exécution ne le peut pas : le repliement est voulu, et lever dessus
casserait la décomposabilité que la bibliothèque maintient exprès. JD024 ne le fait pas, hors d'un cas sur
quatre. Un auteur qui supprime plus tard l'appel survivant en le croyant redondant change le domaine dans
lequel le test tire, et aucun mécanisme du produit ne le lui aurait dit.

## Alternatives envisagées

### Élargir JD024 à la forme, dans toutes les familles

L'ordre relâchant est déjà exactement ce que JD024 décrit, et l'analyzer qui la lève parcourt déjà la chaîne
dont elle aurait besoin. Réutiliser l'identifiant ne coûterait aucune page de documentation, aucune
constante de catalogue et aucune ligne de table.

Rejetée parce que le message de JD024 est faux dans l'ordre resserrant, où le second appel est celui qui
rétrécit et le premier celui qui meurt. L'élargir déplacerait par ailleurs le sens d'un identifiant livré,
ce que ce dépôt traite comme une décision à consigner et non comme un changement à faire — et le record
devrait plaider pour un message qui ne correspond plus à la règle.

### Signaler en sévérité information, par cohérence avec JD024

Les deux règles portent sur une contrainte qui finit par ne rien faire, elles siégeraient dans la même
catégorie, et un lecteur parcourant la table des règles y trouverait une famille cohérente.

Rejetée : la taxonomie du README des règles porte sur le mode de défaillance, pas sur l'appartenance à une
catégorie. La lecture défensive qui vaut à JD024 sa sévérité information n'existe pas pour deux bornes dans
une même chaîne, donc la symétrie ne serait que visuelle, et elle sous-estimerait une erreur que rien
d'autre ne signale.

### Apparier sur l'effet, pour attraper aussi les alias

`NonEmpty().WithMinLength(8)` atteint deux fois un minimum et le premier est mort, exactement pour les mêmes
raisons que la paire explicite. Une règle raisonnant sur la borne plutôt que sur le nom de méthode le
couvrirait.

Rejetée parce que l'alias n'est pas le même acte. Choisir `NonEmpty()` dit quelque chose que la borne
explicite ne dit pas, et un avertissement dessus serait brutal. La question qu'il soulève est celle du choix
entre deux écritures correctes, que l'ADR-0077 admet en sévérité information sous ses propres conditions —
une autre règle, et une règle qu'on peut ajouter plus tard sans déranger celle-ci.

### Laisser la forme muette

Le domaine est bien défini dans tous les cas, les valeurs tirées satisfont toute contrainte déclarée, et
aucun test n'échoue à cause de cela.

Rejetée parce que « bien défini » n'est pas le critère auquel le jeu est tenu — JD024 existe pour un cas
tout aussi bien défini. Ce que l'auteur croit de la borne et ce que le générateur applique réellement
divergent, rien dans le produit ne les réconcilie, et l'écart n'apparaît que le jour où quelqu'un modifie la
chaîne.

## Conséquences

### Positives

* Une erreur tire un diagnostic, avec un seul message, dans toutes les familles de générateurs.
* Trois des quatre cas muets se ferment, et le silence restant est une décision et non un accident.
* JD024 garde exactement la portée que son message décrit, ce qui rend sa page plus simple à écrire et sa
  suppression plus simple à raisonner.
* La question des alias est garée là où l'ADR-0077 peut y répondre, au lieu d'être tranchée implicitement
  ici.

### Négatives

* La portée documentée de JD024 se rétrécit : ses deux pages et les deux tables de règles doivent le dire,
  et une suppression écrite contre JD024 pour une borne doublement déclarée cesse de correspondre.
* Un nouvel identifiant coûte une page anglaise et une française, une ligne dans chaque table de règles, une
  constante de catalogue sur le train de release `catalog`, et une mise à jour de chaque décompte des règles.
* Une chaîne qui redéclare une borne par un alias reste muette, ce que certains lecteurs s'attendront à voir
  se déclencher.

### Risques

* La règle se déclenche sur une forme qu'un générateur de code ou un helper très paramétré pourrait produire
  légitimement à l'intérieur d'une chaîne ; en sévérité avertissement, cela coûte une suppression et non un
  build. Rien dans le dépôt ni dans la documentation n'écrit cette forme aujourd'hui.
* Rétrécir un identifiant livré n'est sûr que tant que les règles restent non publiées au sens du suivi de
  release, ce qui est le cas en dessous de 1.0 ; le même geste après le gel de la surface serait un
  changement cassant.

## Actions de suivi

* Ouvrir l'issue qui spécifie la règle — les quatre vocabulaires, les deux ordres, et le cas des alias sur
  lequel elle reste muette.
* Réénoncer la portée de JD024 sur ses pages anglaise et française une fois la nouvelle règle livrée.
* Reprendre le cas des alias contre le critère de l'ADR-0077 s'il devait être voulu comme règle à part
  entière.

## Références

* [ADR-0038](0038-guard-the-recipe-versus-value-boundary-with-analyzers.fr.md) — la sévérité suit le mode de défaillance.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.fr.md) — borner la surface, et refuser à une frontière désignable.
* [ADR-0052](0052-publish-the-jd-rules-as-a-first-party-catalogue.fr.md) — le catalogue par lequel chaque nouvel identifiant est publié.
* [ADR-0077](0077-admit-a-rule-that-reports-a-correct-spelling.fr.md) — là où le cas des alias appartient.
* [Issue #95](https://github.com/Reefact/just-dummies/issues/95) — la discussion d'où cette décision est sortie.
