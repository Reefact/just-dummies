# ADR-0066 | Charger MSBuild depuis le SDK installé, jamais depuis les fichiers de l'outil

🌍 🇬🇧 [English](0066-load-msbuild-from-the-sdk-never-from-the-tool.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-08-11
**Accepted:** 2026-08-11
**Decision Makers:** Reefact

## Contexte

L'outil `dum` ouvre un vrai projet sur disque avant de scaffolder quoi que ce soit, ce qui exige un
workspace Roslyn conscient de MSBuild. ADR-0065 place ce travail dans la CLI : le moteur de
scaffolding reçoit une compilation et ne sait rien de MSBuild, donc toute cette préoccupation vit
dans la coquille qui l'entoure.

MSBuild n'est pas une bibliothèque ordinaire. Il fait partie du SDK .NET installé, et les assemblies
qu'un processus utilise doivent être celles que ce SDK fournit. Un outil qui embarque sa propre
copie charge celle-là, et la divergence apparaît à l'exécution — après que le projet a commencé à
s'ouvrir — sous la forme d'un échec qui ne nomme ni l'outil, ni le SDK, ni la version en désaccord.

`MSBuildLocator` existe pour cela : il trouve le SDK installé au démarrage et y lie MSBuild. Il
refuse aussi, à la compilation, un projet qui déploierait sa propre assembly MSBuild (MSBL001), ce
qui transforme un piège d'exécution en une erreur de build qu'un contributeur ne peut pas manquer.

L'outil est distribué comme outil .NET, et un outil .NET déploie l'ensemble de sa fermeture de
dépendances sous forme de fichiers — le fait de packaging dont ADR-0063 tire déjà ses conclusions
pour la bibliothèque. Rien de ce qui figure dans cette fermeture n'est inerte : c'est un fichier à
côté de l'exécutable, et le chargeur de MSBuild le trouvera.

La couche workspace contre laquelle l'outil compile entraîne des assemblies MSBuild en dépendance
transitive, à la version exacte contre laquelle elle a elle-même été construite. L'outil a besoin de
ces assemblies pour compiler et ne doit pas les déployer : les deux moitiés — la version et le
déploiement — découlent donc de la couche workspace, et non d'une préférence exprimée ici.

L'automatisation des dépendances propose les montées de version paquet par paquet. Elle ne peut pas
voir qu'une version est la conséquence du choix d'un autre paquet : elle lit un numéro dérivé comme
un numéro en retard, et propose de le relever chaque semaine.

## Décision

L'outil compile contre MSBuild et ne le déploie jamais : MSBuild est localisé dans le SDK installé
au démarrage, et la référence de compilation est tenue à la version que résout la couche workspace
plutôt qu'à une version choisie ici.

## Justification

**L'échec évité est de ceux qu'on ne peut pas diagnostiquer depuis leur symptôme.** Une assembly
MSBuild mal appariée n'échoue pas là où elle est fausse ; elle échoue plus tard, dans un chargement
que l'outil n'a pas écrit, avec un message qui nomme un type interne. Choisir la copie du SDK
supprime la classe d'échec au lieu de la rendre moins probable.

**Un outil qui lit le projet d'un développeur doit utiliser le MSBuild qui le construit.** Toute
autre solution répond à une question portant sur un projet qui n'existe pas — le projet tel que le
MSBuild embarqué l'aurait évalué, non tel que le SDK du développeur l'évalue.

**La version est une conséquence : la traiter comme un choix ne peut qu'introduire une erreur.**
Suivre ce que résout la couche workspace maintient par construction l'accord entre la surface de
compilation et les assemblies chargées. Choisir un autre numéro rompt cet accord, et rien ne le
vérifie avant l'exécution.

**Le garde-fou vaut d'être là parce qu'il est vérifiable.** Le refus du locator à la compilation
signifie que l'arrangement est tenu par le build et non par la mémoire, ce qui rend acceptable de
confier l'exclusion de déploiement à une seule référence plutôt qu'à l'attention d'un relecteur.

**Écarter l'automatisation ne coûte rien, car aucune montée de ce paquet seul ne peut être juste.**
Soit la couche workspace a bougé, et sa propre montée amène la nouvelle version ; soit elle n'a pas
bougé, et relever le numéro rompt l'accord sur lequel repose le paragraphe précédent.
L'automatisation ne sait pas distinguer les deux cas, et une re-corrélation humaine est la seule
réponse correcte à l'un comme à l'autre.

Le compromis accepté est la lisibilité : une version dérivée passe pour une version en retard aux
yeux d'un lecteur qui ignore qu'elle est dérivée. La réponse est d'écrire la raison là où la version
vit, non de rendre la version libre.

## Alternatives envisagées

### Livrer MSBuild à côté de l'outil

Envisagé parce que cela supprime la dépendance à un SDK installé, et permettrait à l'outil de
tourner sur une machine qui n'en a pas.

Rejeté parce que c'est le mode d'échec lui-même, non un moyen de le contourner : l'outil chargerait
son propre MSBuild plutôt que celui du développeur, et évaluerait son projet sous un moteur qui ne
le construit jamais. Le locator refuse cet arrangement à la compilation précisément parce que le
symptôme d'exécution est illisible.

### Choisir ici la version de MSBuild, et la monter à son propre rythme

Envisagé parce que cela rend la dépendance explicite et maintenable comme toutes les autres, et
parce qu'une version dérivée se confond aisément avec un oubli.

Rejeté parce que le numéro n'appartient pas à ce projet. Toute valeur autre que celle résolue par la
couche workspace met ce contre quoi l'outil compile en désaccord avec ce qu'il charge, et ce
désaccord n'a aucun symptôme à la compilation : il attend le premier projet qui sollicite la partie
où les deux versions diffèrent.

### Laisser l'automatisation proposer la montée, et compter sur la CI pour rejeter la mauvaise

Envisagé parce que la CI la rejette aujourd'hui, ce qui fait passer le coût pour du bruit plutôt que
pour un risque.

Rejeté parce que cela repose sur un build rouge pour re-établir chaque semaine un fait déjà acquis,
et parce que le cas dangereux est le vert. Le refus actuel vient d'un contrôle à la compilation qui
voit cette forme-là ; une montée qui compilerait proprement tout en divergeant de l'assembly chargée
passerait la même porte.

## Conséquences

### Positives

* L'outil évalue le projet d'un développeur sous le SDK de ce développeur, seule lecture de ce
  projet qui ait un sens.
* Une tentative de déploiement de MSBuild échoue au build et non à l'exécution : l'arrangement est
  tenu là où il est peu coûteux à corriger.
* La version de compilation et les assemblies chargées restent d'accord par construction, sans
  contrôle à écrire ni à oublier.
* Une proposition de montée hebdomadaire qui ne pourrait jamais être acceptée cesse d'être ouverte,
  relue et fermée.

### Négatives

* L'outil exige un SDK .NET installé, et le dit plutôt que de le contourner.
* La version épinglée passe pour dépassée aux yeux de qui n'a pas lu pourquoi elle l'est — raison
  pour laquelle le motif voyage avec elle.
* Une dépendance sort de l'automatisation : elle ne bouge donc que si quelqu'un la bouge.

### Risques

* La couche workspace bouge sans que la version soit re-corrélée, laissant la référence en deçà de
  ce que la couche exige — la première montée qui fera de cette épingle un retour en arrière se
  manifestera par NU1605, mais seulement sur le build qui la tente.
* Un futur contributeur lit l'exclusion d'automatisation comme une licence à exclure d'autres
  dépendances par confort, et non pour un motif de version dérivée.

## Actions de suivi

* Re-corréler à la main la version de compilation de MSBuild chaque fois que la couche workspace
  bouge, et réexaminer l'exclusion d'automatisation au même moment.

## Références

* ADR-0065 — le moteur ne sait rien de MSBuild, raison pour laquelle cette préoccupation
  n'appartient qu'à la CLI.
* ADR-0063 — le fait de packaging dont découle ce raisonnement : un outil .NET livre sa fermeture
  sous forme de fichiers.
* `Directory.Packages.props`, `JustDummies.Cli/JustDummies.Cli.csproj` — où vivent la référence et
  son motif.
* `.github/dependabot.yml` — où vivent l'exclusion d'automatisation et son motif.
* Pull request #60 — la montée proposée sur ce seul paquet, et l'échec de build qui a montré ce
  qu'elle coûte ; pull request #63 — l'exclusion qui l'empêche de revenir.
