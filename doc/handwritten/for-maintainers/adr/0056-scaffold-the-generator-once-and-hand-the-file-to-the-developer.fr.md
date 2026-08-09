# ADR-0056 | Scaffolder le generator une fois et confier le fichier au développeur

🌍 🇬🇧 [English](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-07-30
**Accepted:** 2026-08-09
**Decision Makers:** Reefact

> Les renvois de section (§N) pointent vers la [spécification de `dum`](../specifications/justdummies-tool.fr.md), le document dont cet enregistrement a été extrait.

## Contexte

Le tool écrit un fichier C#, contenant un generator pour un type du code du développeur, dans le
projet du développeur lui-même. Trois formes existent pour un tel outil, toutes utilisées par des
outils réels : un source generator Roslyn produisant le fichier dans la sortie intermédiaire du
build ; un fichier écrit une fois dans l'arbre des sources ; et un fichier écrit dans l'arbre des
sources accompagné d'une commande de vérification qui échoue quand il ne correspond plus à ce que
l'outil produirait aujourd'hui.

Un fichier dans l'arbre des sources peut se désynchroniser, silencieusement, du type dont il a été
dérivé quand le constructeur de ce type change.

La bibliothèque que le tool sert affiche l'absence de magie dans son positionnement : pas de
réflexion, pas de remplissage de graphe d'objets, et sa propre description est « small,
deterministic, explicit ».

Le tool ne peut pas inférer tous les paramètres de constructeur. Certains portent des invariants
exprimés d'une façon qu'aucun ensemble clos de règles ne peut lire (§9), donc un fichier scaffoldé
est censé être incomplet pour certains types.

La sortie d'un source generator n'est pas éditable par le développeur et n'apparaît pas en revue de
code. Un fichier dans l'arbre des sources est les deux.

## Décision

Le tool écrit chaque fichier de generator une fois et en transfère la propriété au développeur, qui
peut l'éditer librement et à qui il n'est jamais demandé de le régénérer.

## Justification

La dérive est la seule objection sérieuse à l'écriture dans l'arbre des sources, et elle n'existe
que tant que le tool revendique la propriété du fichier. Une fois la propriété transférée, « le
fichier ne correspond plus à ce que le tool produirait » cesse d'être un défaut et devient l'état
attendu d'un fichier que le développeur a édité — ce que le tool lui demande précisément de faire.
L'objection se dissout au lieu d'être atténuée.

Ce transfert est aussi ce qui rend un fichier incomplet acceptable. Un outil qui possède sa sortie
doit produire quelque chose de complet ou échouer ; un outil qui remet un squelette peut s'arrêter
où sa connaissance s'arrête et le dire, ce qui est la position honnête étant donné que certains
invariants sont illisibles. [ADR-0060](0060-seed-generators-from-constructor-guards.fr.md) et [ADR-0060](0060-seed-generators-from-constructor-guards.fr.md) dépendent de ce point réglé d'abord.

L'éditabilité et la visibilité en revue servent une bibliothèque dont l'argument de vente est que
rien ne se passe dans le dos du développeur. Un generator qu'il peut lire, parcourir au débogueur
et modifier est cohérent avec ce positionnement ; un generator matérialisé par le compilateur ne
l'est pas.

Retirer la propriété retire avec elle toute une classe de machinerie : pas de verbe de
vérification, pas de protocole de régénération, pas de détection de dérive, pas de règles sur les
régions éditables à la main. Pour un outil dont la première règle de conception est d'être trivial
à adopter, la machinerie non construite vaut plus que les garanties qu'elle aurait offertes.

## Alternatives considérées

##### Un source generator Roslyn

Considéré parce qu'il rend la dérive structurellement impossible : il rejoue à chaque build, donc
sa sortie ne peut pas retarder sur le type.

Écarté parce qu'il abandonne tout ce que l'existence réelle du fichier apporte. Le développeur ne
peut pas l'éditer, ne peut pas compléter les paramètres que le tool n'a pas su inférer, et les
relecteurs ne le voient jamais. Il n'a par ailleurs aucun moyen utile de laisser du travail
inachevé, donc le cas du paramètre non résolu devrait faire échouer le build sans offrir au
développeur d'endroit où agir.

##### Un fichier écrit plus un verbe de vérification

Considéré parce que c'est la réponse standard à la dérive pour les artefacts générés commités, et
qu'elle s'intègre proprement en intégration continue.

Écarté parce que vérification et édition s'excluent. Une commande qui échoue dès que le fichier
diffère d'une génération fraîche interdit exactement l'édition que ce tool existe pour inviter.
Garder les deux supposerait d'encoder quelles régions appartiennent au tool et lesquelles au
développeur — plus de machinerie que la fonctionnalité entière n'en vaut.

## Conséquences

**Positives.** Le tool a un verbe et aucun protocole. Le fichier scaffoldé est du code ordinaire :
relisible, débogable, éditable. Le chemin du paramètre non résolu de [ADR-0060](0060-seed-generators-from-constructor-guards.fr.md) devient disponible.

**Négatives.** Un generator peut retarder sur son type. Ajouter un paramètre de constructeur casse
la compilation du generator, ce qui fait remonter le problème ; changer l'invariant d'un paramètre,
non — le generator continue de produire des valeurs que le constructeur rejette désormais, et seul
un test en échec le révèle.

**Risques.** Un développeur peut s'attendre à ce que la régénération préserve ses éditions. Atténué
par l'en-tête émis, qui indique que la régénération écrase et que le type est `partial` donc que
les fichiers voisins survivent, et par `--force` exigé pour écraser tout court.

## Actions de suivi

* Énoncer la position « ce fichier est le tien » en évidence dans la documentation utilisateur du
  tool : elle inverse l'attente installée par la plupart des outils de scaffolding.

## Références

* §1, §3, §4.3 de cette spécification.

---
