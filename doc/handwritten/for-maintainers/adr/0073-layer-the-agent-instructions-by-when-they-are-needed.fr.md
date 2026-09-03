# ADR-0073 | Étager les instructions destinées aux agents selon le moment où elles servent

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0073-layer-the-agent-instructions-by-when-they-are-needed.md)

**Statut :** Accepté
**Proposé :** 2026-08-15
**Accepté :** 2026-08-15
**Décideurs :** Reefact

## Contexte

L'[ADR-0035](0035-state-the-coding-rules-where-an-agent-can-act-on-them.fr.md) a déplacé les
règles de codage dans `CLAUDE.md`, parce qu'un pointeur vers un fichier que le lecteur ne peut
pas ouvrir n'est pas une instruction. Son action de suivi était d'ajouter les règles restantes
à mesure qu'elles seraient identifiées. Elles l'ont été : `CLAUDE.md` porte aujourd'hui la
portée du produit, la politique de langue, les commandes de build et de test, la carte des
projets, les consignes de changement, les règles de codage, les conventions de diagnostic et de
documentation, la procédure ADR, les conventions de pull request et la procédure de réponse aux
reviews — 284 lignes, 21 543 octets.

Chacun de ces octets est chargé au début de chaque session, avant même que la tâche soit connue.
Claude Code parcourt l'arborescence au lancement et concatène les fichiers de mémoire qu'il
trouve ; les imports `@chemin` sont développés au même moment, si bien que découper un fichier
en imports déplace des octets sans déplacer le coût. L'éditeur documente une cible de moins de
200 lignes par fichier et indique qu'au-delà, un fichier consomme davantage de contexte *et*
réduit la constance avec laquelle les instructions sont suivies.

Le contenu n'est pas uniformément utile. La procédure ADR sert au moment de finaliser une pull
request, la procédure de release au moment de couper un train, les conventions CLI quand
`JustDummies.Cli` ou `JustDummies.GenAny` est touché, la règle des cinq éléments en phase quand
une règle `JDxxx` bouge. Une modification d'un seul test paie pour toutes.

Une partie du contenu n'a pas à être de la prose du tout, parce qu'un outil refuse déjà l'erreur.
`IDE0008` fait d'un type inféré un avertissement au build en local et une erreur en CI
([ADR-0034](0034-enforce-the-style-rules-the-compiler-can-express.fr.md)). Les analyseurs
`DiagnosticCatalog` câblés dans `Directory.Build.props` lèvent en erreur, sans dérogation
`.editorconfig`, une suppression nommée par littéral ou dépourvue de justification — soit
l'[ADR-0050](0050-name-a-suppressed-rule-through-a-catalogue-constant.fr.md) rendue
incontournable. Un en-tête de commit est vérifié par le même linter dans `.githooks/commit-msg`
et en CI. `ValueObjectConventionTests` tient chaque type `[ValueObject]` à son identité ;
`TranslationParityTests` tient chaque page française à sa jumelle anglaise.

Depuis l'acceptation de l'ADR-0035, Claude Code a acquis deux mécanismes qui n'existaient pas
alors. Un fichier placé sous `.claude/rules/` et portant un champ de front-matter `paths:` est
chargé **quand l'agent lit un fichier correspondant au glob**, ni au lancement ni sur demande.
Une skill placée sous `.claude/skills/` ne précharge que sa description ; son corps se charge
quand l'agent la juge pertinente, ou quand elle est invoquée par son nom. Ni l'un ni l'autre ne
demande à l'agent de décider d'ouvrir un document.

L'histoire de ce dépôt dit ce qui se joue si le sujet est mal traité. La règle des types
explicites a dérivé jusqu'à 203 violations alors qu'une instruction de la suivre était en place.
Une image fournie a été composée en trois variantes toutes pires que le fichier d'origine. Le
GUID d'un projet a été oublié dans la section `NestedProjects` de la solution, et corrigé après
coup, à plusieurs reprises.

## Décision

Chaque instruction destinée à un agent vit à l'étage correspondant au moment où elle sert —
toujours chargée, chargée par chemin, chargée à la demande, ou garantie par un outil au lieu
d'être énoncée — et `CLAUDE.md` ne conserve que ce qu'il vaut la peine de payer sur chaque tâche.

## Justification

L'instruction qui a échoué dans l'ADR-0035 a échoué parce que son lecteur ne pouvait pas
l'appliquer. L'instruction qui échoue ici échoue autrement : elle est présente, elle est lisible,
et elle est sans rapport avec la tâche en cours, si bien qu'elle dilue celles qui ne le sont pas.
Les deux sont des échecs de *livraison*, et les deux se corrigent en plaçant la règle là où son
lecteur la rencontre. L'ADR-0035 a répondu à cette question dans l'espace — écrire la règle là
où l'agent regarde. Celle-ci y répond dans le temps — livrer la règle quand l'agent en a besoin.
La seconde question ne pouvait pas être posée avant, puisque les seules options étaient
« toujours chargée » et « derrière un pointeur ».

C'est pourquoi il ne s'agit pas du document dédié que l'ADR-0035 avait écarté. Cette alternative
avait été rejetée parce qu'un document séparé exige un pointeur depuis `CLAUDE.md` pour être
trouvé, ce qui réintroduit précisément l'indirection qui avait déjà échoué. Une rule à portée de
chemin n'exige aucun pointeur : elle arrive parce que l'agent a ouvert un fichier correspondant,
et c'est le même événement qui rend la règle pertinente. Le mécanisme supprime l'étape qui
rendait l'alternative dangereuse, et l'argument qui lui était opposé ne survit pas à cette
suppression.

Sortir une règle de la prose n'est sûr que là où le mécanisme qui la remplace est au moins aussi
fort. Là où un compilateur, un analyseur, un test ou un job de CI refuse déjà l'erreur, la prose
n'était pas ce qui protégeait le dépôt, et la répéter achète de l'adhérence à ce qui ne dépend
pas de l'adhérence. Là où rien ne la refuse — ne pas reformater ce qu'on n'a pas modifié, la
forme que prend un `[SuppressMessage]`, une propriété qui ne doit pas parcourir une collection,
une image qui s'expédie octet pour octet — la prose est le seul garde-fou, et elle reste, soit
toujours chargée, soit portée aux fichiers qu'elle gouverne. La règle empirique est assez courte
pour être appliquée : ce qu'un outil sait trancher revient à l'outil, ce qui exige un jugement
revient à une instruction, et l'instruction est portée aussi étroitement que son sujet.

Garder petit l'étage toujours chargé est ce qui fait fonctionner le reste. Il porte ce qu'une
modification de n'importe quelle nature peut violer — la portée du produit et le refus qu'elle
implique, les commandes de build et de test, la carte du dépôt, le plancher de plateforme, la
langue, et la poignée d'interdits dont la transgression est bon marché à commettre et coûteuse à
défaire — et il porte le routage, pour que l'agent sache qu'une procédure existe avant d'en
connaître le contenu. Deux règles le tiennent honnête : une entrée n'y gagne sa place que s'il
est raisonnable de la payer en corrigeant un test sans rapport, et aucune entrée n'en sort avant
que son nouveau foyer existe.

L'étagement borne aussi un coût que l'arrangement précédent payait en silence. Une base de 72
décisions ne peut pas être lue à chaque pull request, et la vérification ADR a toujours signifié
*sélectionner les décisions que ce changement peut toucher*, jamais *toutes les lire*. Énoncée en
prose au milieu de 283 autres lignes, cette distinction se perd facilement ; énoncée comme la
procédure qu'exécute une skill, l'étape de sélection est la première chose que fait la procédure.

## Alternatives considérées

### Raccourcir `CLAUDE.md` en supprimant le raisonnement derrière chaque règle

Considérée parce que l'essentiel du poids du fichier est de l'explication, pas de l'instruction,
et que les instructions seules tiendraient dans un tiers de la place.

Rejetée parce que les explications sont la matière dont les règles sont faites. « Ne jamais
altérer une image que je fournis » est suivie différemment par un lecteur qui sait que trois
variantes ont un jour été composées à partir d'une marque fournie ; une règle dont le coût est
invisible est une règle qu'on échange contre une commodité d'apparence plausible. Le raisonnement
n'est pas supprimé par cette décision, seulement relogé là où il est lu.

### Découper `CLAUDE.md` en fichiers thématiques tirés par des imports `@chemin`

Considérée parce que c'est la façon documentée d'organiser un gros fichier de mémoire, et qu'elle
laisserait le contenu adressable et le fichier navigable.

Rejetée parce que les imports sont développés au lancement. Le coût en contexte serait identique
à celui d'aujourd'hui : le changement serait purement organisationnel, tout en ajoutant une
couche d'indirection à chaque règle. Déplacer des octets sans déplacer le coût n'est pas
l'optimisation dont il est question ici.

### Garantir davantage de la prose par des hooks plutôt que la reloger

Considérée parce qu'une vérification qui s'exécute est plus forte qu'une règle qu'on lit, et que
l'ADR-0035 a déjà établi le hook comme la manière qu'a ce dépôt de rendre une règle observable.

Rejetée comme réponse générale, quoique adoptée là où elle s'applique. L'essentiel de ce qui
reste en prose relève du jugement — un changement embarque-t-il une décision durable, un constat
mérite-t-il un label bloquant, un historique se lit-il proprement — et un script qui trancherait
cela prendrait la décision du mainteneur sans aucune de ses informations. Les règles réellement
mécaniques sont déplacées vers le hook par cette décision ; les autres ne peuvent pas l'être, et
prétendre le contraire remplacerait une instruction diluée par une instruction fausse.

### Donner à chaque sous-répertoire son propre `CLAUDE.md` plutôt qu'utiliser `.claude/rules/`

Considérée parce que les fichiers de mémoire imbriqués se chargent eux aussi à la demande, et
qu'ils n'exigent aucun nouveau répertoire.

Rejetée parce que la portée qui compte ici est rarement un répertoire. La règle de documentation
gouverne `doc/**` et les pages racine du dépôt ; les règles C# gouvernent sept projets ; la règle
de build gouverne les `.csproj`, les `Directory.*.props`, `build/`, `tools/` et les workflows. Un
glob énonce cela directement, là où des fichiers imbriqués exigeraient de recopier la même règle
dans plusieurs arborescences et de les tenir en phase à la main.

## Conséquences

### Positives

* Une tâche paie pour les instructions qu'elle peut violer, pas pour toutes celles du dépôt.
* La connaissance qui était diluée est désormais livrée au moment où elle s'applique, qui est
  aussi le moment où elle a le plus de chances d'être suivie.
* Les règles déjà garanties par un compilateur, un analyseur, un test ou la CI cessent d'être
  répétées sous forme de demandes : la prose qui subsiste est celle qui porte.
* La vérification ADR énonce explicitement son étape de sélection, ce qui borne le coût d'une
  base de décisions vouée à croître.
* Deux règles « vérifiées par revue » — la forme d'un `[SuppressMessage]`, un projet absent de la
  section `NestedProjects` de la solution — deviennent observables à l'édition.

### Négatives

* Les instructions vivent désormais à quatre endroits au lieu d'un, et un mainteneur doit savoir
  à quel étage appartient une nouvelle règle avant de l'ajouter.
* Une rule à portée de chemin ne vaut que ce que vaut son glob. Un motif qui manque un fichier
  retient silencieusement la règle dont ce fichier avait besoin.
* Les rules munies de `paths:` ne sont pas réinjectées après un compactage ; elles se rechargent
  à la prochaine lecture d'un fichier correspondant, si bien qu'une longue session peut tourner
  un moment sans une règle qu'elle avait auparavant.

### Risques

* Une règle sortie de l'étage toujours chargé peut être manquée par une tâche qui en avait besoin
  sans jamais ouvrir un fichier correspondant — une demande formulée entièrement en prose, par
  exemple. L'atténuation tient à ce que tout interdit bon marché à transgresser garde un énoncé
  d'une ligne dans `CLAUDE.md`, le raisonnement restant porté.
* L'étagement est un jugement que rien ne vérifie. Rien n'échoue si une règle est classée au
  mauvais étage, et le symptôme — une instruction discrètement non suivie — est celui-là même à
  propos duquel l'ADR-0035 a été écrite.
* Les skills se chargent quand l'agent les juge pertinentes. Une description qui ne correspond
  pas à la formulation d'une demande laisse la procédure non chargée, et l'agent avance sans
  savoir qu'elle existait.

## Actions de suivi

* Guetter une règle non appliquée parce que son glob n'a pas correspondu, et élargir le glob
  plutôt que de remettre la règle en arrière.
* Reconfronter l'étage toujours chargé au test de cette décision chaque fois qu'une règle y est
  ajoutée, et tenir le fichier sous la cible de 200 lignes de l'éditeur.
* Confirmer que chaque skill est bien atteinte par les demandes qu'elle doit servir, et récrire
  sa description plutôt que dupliquer son contenu dans `CLAUDE.md` lorsqu'elle ne l'est pas.

## Références

* [ADR-0035](0035-state-the-coding-rules-where-an-agent-can-act-on-them.fr.md) — la décision que
  celle-ci prolonge : elle a placé les règles là où un agent les lit, celle-ci décide quand
  chacune est livrée. Sa troisième alternative rejetée est réexaminée ici sur la foi d'un
  mécanisme qui n'existait pas à l'époque.
* [ADR-0034](0034-enforce-the-style-rules-the-compiler-can-express.fr.md) — la barrière au build
  qui permet à la règle des types explicites de survivre en une seule ligne.
* [ADR-0050](0050-name-a-suppressed-rule-through-a-catalogue-constant.fr.md) — garantie en erreur
  par les analyseurs, et donc plus répétée sous forme de demande.
* [ADR-0002](0002-check-every-pull-request-against-the-adr-base.fr.md) — la vérification dont
  cette décision rend l'étape de sélection explicite.
