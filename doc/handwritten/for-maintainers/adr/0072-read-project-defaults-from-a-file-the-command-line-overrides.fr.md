# ADR-0072 | Lire les défauts de projet dans un fichier que la ligne de commande surcharge

🌍 🇬🇧 [English](0072-read-project-defaults-from-a-file-the-command-line-overrides.md) · 🇫🇷 Français (ce fichier)

**Status:** Proposed
**Proposed:** 2026-08-12
**Decision Makers:** Reefact

> Les renvois de section (§N) pointent vers la [spécification de `dum`](../specifications/justdummies-tool.fr.md).

## Contexte

Le §3 énonçait qu'il n'y a pas de fichier de configuration, et il a été écrit quand chaque option
était une décision par invocation : quel projet, quel type, où va ce fichier-ci.

Deux options ajoutées depuis n'en sont pas. `--entry-point` et `--entry-point-namespace`
([ADR-0070](0070-emit-an-entry-point-on-request-as-a-file-of-its-own.fr.md)) décrivent comment on
atteint les generators d'un projet, et la réponse est la même pour tous ses types — une racine
rassemblée depuis plusieurs namespaces n'est une racine que si chaque scaffold contribue à la même.
`--output` est un fait du même ordre dès qu'une équipe a décidé où vivent ses generators.

Une option qu'il faut retaper à chaque invocation est une option qui finira par être tapée
différemment sur l'une d'elles, et l'outil scaffolde une fois par type (ADR-0056) au fil d'un graphe
d'agrégats : un projet rencontre donc ces options autant de fois qu'il a de types.

Le §16 réservait déjà le fichier : un `dum.json` optionnel à la racine du projet, avec une clé
`naming`, pour les options de nommage de la v1.1. Ces options — `--name`, `--pattern` — ne sont pas
implémentées.

Le moteur ne fait aucune IO et ne connaît ni MSBuild ni le disque
([ADR-0065](0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.fr.md)) ; la CLI localise le
projet et détient son chemin (§11.1).

L'outil distingue un échec de scaffolding (`1`) d'une ligne de commande illisible (`2`), et le §7
maintient les deux séparés.

## Décision

Un `dum.json` optionnel à côté du fichier projet fournit des défauts pour les options qui décrivent le
projet plutôt que l'invocation, la ligne de commande surcharge n'importe laquelle d'entre elles, et une
clé que le fichier ne lit pas est refusée.

## Justification

**Le fichier est justifié par ce que les options sont devenues, pas par le confort.** La phrase du §3
était juste pour les options pour lesquelles elle a été écrite, et a cessé de l'être quand des options
décrivant le projet sont arrivées. Une équipe qui veut une racine unique, rassemblée dans un
namespace, énonce une propriété d'un dépôt ; la lui faire réénoncer à chaque invocation, c'est
ainsi que le onzième scaffold atterrit dans un autre namespace que les dix premiers.

**La précédence n'a besoin d'aucune table, et c'est le design.** Une valeur que le développeur a tapée
est déjà dans les réglages, et rien de ce que le fichier fournit n'en écrase une — toute la règle est
que le fichier remplit les blancs. Un fichier de configuration dont l'interaction avec les options
demande une explication est un fichier auquel personne ne se fie assez pour s'en servir.

**Refuser une clé inconnue est tout l'intérêt d'avoir le fichier.** Une clé silencieusement ignorée
est un défaut que le développeur croit en vigueur et qui ne l'est pas, ce qui est pire que l'absence
de fichier : cela produit la mauvaise disposition et n'en donne aucune raison. C'est le marché que
passe déjà tout le reste de l'outil — refuser fort au bord plutôt que continuer de façon plausible —
et c'est pourquoi la clé `naming` du §16 est refusée ici tant que les options qu'elle configure
n'existent pas.

**Enraciner un chemin relatif dans le projet est ce qui en fait un défaut.** Un chemin tapé sur la
ligne de commande est relatif à l'endroit où il a été tapé, ce qui est juste. Un chemin commité à
côté du `.csproj` doit vouloir dire la même chose depuis n'importe quel répertoire courant, sinon un
développeur qui lance l'outil depuis la racine du dépôt et un autre depuis le projet de test
obtiennent deux dispositions différentes de la même intention commitée.

**Une validation, pas deux.** L'état fusionné repasse par les règles auxquelles répond la ligne de
commande, de sorte qu'une valeur venue du fichier est refusée pour les mêmes raisons et dans les mêmes
mots qu'une valeur tapée. Un second jeu de règles dériverait, et le fichier finirait par accepter ce
que l'option rejette.

**C'est la coquille qui le lit, donc le moteur reste ce qu'il est.** Le fichier est sur le disque, à
côté d'un projet que la CLI a localisé ; le moteur reçoit des options, exactement comme avant, et
continue de ne rien connaître ni de l'un ni de l'autre (ADR-0065).

## Alternatives envisagées

##### Ne pas le faire, et garder intacte la phrase du §3

Envisagé parce que la phrase est porteuse : c'est elle qu'un test structurel oppose à la ligne de
commande, et chaque option non ajoutée est une surface défendue.

Rejeté parce que la phrase défend contre des *options*, et que ceci n'en ajoute aucune — les cinq clés
sont les options qui existent déjà. Ce qu'elle retire, c'est l'obligation de les retaper, ce qui n'est
pas de la surface. Le test qui garde le §3 le garde toujours : ajouter une sixième option devrait
encore être argumenté.

##### Ignorer une clé non reconnue, comme la plupart des formats de configuration

Envisagé parce que c'est indulgent, et parce qu'un fichier qui refuse une clé inconnue ne peut plus
être lu par un outil plus ancien dès qu'un plus récent ajoute une clé.

Rejeté parce que l'indulgence est ici la mauvaise vertu. La défaillance qu'elle pardonne est une
faute de frappe dans le seul fichier dont le métier est d'être cru, et le symptôme — des fichiers qui
atterrissent dans le mauvais namespace — est loin de la cause. Le coût de compatibilité ascendante est
réel et petit : l'outil et le fichier sont commités dans le même dépôt.

##### Mettre les défauts dans le `.csproj`, en propriétés MSBuild

Envisagé parce que le fichier projet est déjà l'endroit où vivent les réglages de build, et qu'aucun
fichier nouveau n'apparaît.

Rejeté parce que cela placerait la configuration de l'outil derrière l'évaluation MSBuild, dont le
moteur ne doit pas avoir besoin (ADR-0065) et que la CLI devrait alors interpréter. Un fichier JSON
plat à côté du projet se lit en une douzaine de lignes par la coquille, et d'un coup d'œil par un
développeur.

##### Chercher en remontant depuis le répertoire courant

Envisagé parce qu'un fichier unique à la racine du dépôt servirait plusieurs projets de test.

Rejeté parce que cela fait dépendre le fichier qui s'applique de l'endroit d'où l'outil a été lancé,
ce qui est précisément la propriété que cette décision existe pour supprimer. À côté du projet est
sans ambiguïté, et un dépôt qui veut un jeu de défauts partagé peut copier quatre lignes.

## Conséquences

**Positives.** Les options qui décrivent un projet sont énoncées une fois, commitées, et relues comme
n'importe quel autre réglage de projet. Une faute de frappe est nommée au lieu d'être absorbée. Rien
ne change pour une invocation sans fichier.

**Négatives.** Le « pas de fichier de configuration » du §3 n'est plus vrai et a dû être réécrit ; la
phrase était commode à pouvoir dire. L'outil a désormais deux endroits d'où une option peut venir,
donc répondre à « pourquoi a-t-il atterri là ? » demande de regarder une chose de plus — atténué par
les refus qui nomment le fichier. Et un dépôt à plusieurs projets de test a besoin du fichier dans
chacun.

**Risques.** Le fichier attirera des clés qui ne sont pas des options — une liste de types à
scaffolder, un `--force` par défaut — chacune l'éloignant de « des défauts pour des options
existantes » et le rapprochant du `--all` et de la régénération que le §16 a abandonnés. La liste des
cinq clés et son refus sont ce qui tient cette ligne.

## Actions de suivi

* Quand `--name` et `--pattern` arriveront (§16), `naming` rejoindra les clés lues et cessera d'être
  refusée.

## Références

* §3, §3.3, §7, §16 de la spécification.
* [ADR-0056](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.fr.md),
  [ADR-0065](0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.fr.md),
  [ADR-0070](0070-emit-an-entry-point-on-request-as-a-file-of-its-own.fr.md).
