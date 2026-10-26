# ADR-0108 | Nommer le scaffolder d'après ce qu'il tire, non d'après le rôle d'une valeur

🌍 🇬🇧 [English](0108-name-the-scaffolder-after-what-it-draws.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-10-26
**Accepted:** 2026-10-26
**Decision Makers:** Reefact

> Les renvois de section (§N) pointent vers la [spécification de `any`](../specifications/justdummies-tool.fr.md).

## Contexte

La bibliothèque définit un dummy par le test qui l'utilise : une valeur dont le test a besoin et dont
il ne se soucie pas, et dont la valeur n'atteint jamais l'assertion. **Une donnée qui intervient dans
ce que le test cherche à vérifier n'est pas un dummy**, quelle que soit son origine.

Le scaffolder s'appelait `dum` et se décrivait comme écrivant « le générateur de dummy » d'un type. Ce
qu'il écrit est un `AnyX` : une `partial class` implémentant `IAny<X>`, avec une méthode `With…` par
paramètre de constructeur (§4). Un test peut fixer n'importe lequel de ces paramètres à une valeur
qu'il a choisie, et rien ne l'empêche de fixer celui sur lequel porte son assertion. Même sans rien
fixer, l'objet tiré n'est un dummy que si le test ne le regarde pas.

Tout ce que la bibliothèque expose par ailleurs est déjà nommé d'après ce qu'il tire, non d'après le
rôle que joue la valeur : le point d'entrée est `Any`, les générateurs sont `AnyInt32`, `AnyString`,
`IAny<T>`, le moteur de scaffolding est `JustDummies.GenAny`, les types émis sont des `AnyX`, et
l'option de point d'entrée de l'outil lui-même est `--entry-point any`. Le scaffolder était la seule
surface nommée d'après le rôle.

Aucune version du train `cli` n'est sortie de bêta : il en est à `cli-v1.1.0-beta.6`, et ni le nom de
sa commande ni celui de son fichier de défauts de projet (ADR-0072) ne sont encore un contrat stable.

L'issue #272 a signalé la contradiction du point de vue d'un lecteur : la description de l'outil
promet un générateur de dummy, et la définition de la bibliothèque elle-même dit que l'objet qu'il
rend, dès qu'une valeur dont dépend le résultat du test y est fixée, n'est pas un dummy.

## Décision

La commande du scaffolder est `any`, et tous les noms qui lui appartiennent — son fichier de défauts
de projet, ses commentaires émis, son rapport d'exécution — suivent la commande.

## Justification

**Un outil se nomme d'après ce qu'il produit, et ce qu'il produit est un générateur de valeurs
arbitraires.** Qu'une valeur tirée soit un dummy, c'est le test qui la tire qui en décide, une fois
l'outil terminé. Nommer l'outil d'après ce rôle, c'est faire une promesse qu'il ne peut pas tenir, et
la promesse est rompue dès qu'un test se sert de ce qu'il écrit comme prévu — fixer, par une méthode
`With…`, la valeur sur laquelle porte son assertion.

**La bibliothèque garde son nom, parce que ce nom est un périmètre, non la description de chaque
valeur.** *JustDummies* dit à quoi sert la bibliothèque : produire des valeurs que les tests
utiliseront comme dummies, et refuser de devenir un outil de test par propriétés ou de remplissage
d'objets. Cela reste vrai. Le nom de l'outil a une tâche plus étroite : dire ce que fait la commande
quand elle s'exécute, et là `any` est exact.

**Une terminologie uniforme coûte moins qu'une terminologie assortie d'une exception.** Une fois l'outil renommé, le
lecteur rencontre `Any`, `AnyX`, `IAny<T>`, `GenAny` et `any` — un mot pour une idée. Garder `dum`
aurait laissé le seul nom construit sur *dummy* sur la seule surface où la définition ne tient pas.

**C'est le moment où cela ne coûte rien.** Un nom de commande devient un contrat le jour où une
version stable le publie, et il faut alors migrer les scripts et les fichiers `dum.json`. Avant cela,
le coût est un changement dans ce dépôt et une ligne au changelog.

**Le fichier de défauts suit la commande, sans repli.** Un fichier nommé d'après une commande qui
n'existe plus est une énigme pour le lecteur suivant. Lire aussi l'ancien nom garderait deux
orthographes vivantes pour un outil qui n'a jamais publié que des bêtas, et un fichier hérité honoré
en silence est précisément le genre de défaut qu'ADR-0072 refuse de laisser implicite.

## Alternatives considérées

### Garder `dum` et expliquer la nuance dans la documentation

C'était le plus petit changement, et l'issue ne demandait pas plus qu'une explication.

Rejetée : une explication doit être trouvée puis lue, alors qu'un nom de commande se voit à chaque
invocation. Documenter que le « générateur de dummy » ne produit pas toujours des dummies, c'est
documenter un défaut.

### Renommer aussi la bibliothèque

Envisagée par souci de cohérence complète : si l'outil ne doit pas dire *dummy*, pourquoi le paquet
le dirait-il ?

Rejetée : le nom de la bibliothèque énonce son périmètre, qui reste juste, et les
[principes de conception](../../for-users/guides/design-principles.fr.md) sont bâtis dessus. Le nom du
paquet est aussi bien plus largement publié que la commande d'un outil en bêta.

### Une commande plus longue et sans ambiguïté, comme `justdummies` ou `anygen`

Envisagée pour éviter un nom de commande court et générique dans le `PATH` d'un développeur.

Rejetée : aucune commande nommée `any` n'est connue pour entrer en collision sur les plateformes que
l'outil prend en charge, et l'outil s'installe délibérément, comme outil .NET global. Un nom plus long
casserait le vocabulaire en un seul mot que le renommage vient établir.

## Conséquences

### Positives

* La description de l'outil et la définition d'un dummy par la bibliothèque ne se contredisent plus.
* Un seul mot, `any`, nomme tout ce que la bibliothèque et son outil tirent.

### Négatives

* Quiconque a installé une bêta doit taper `any` au lieu de `dum` et renommer son `dum.json` en
  `any.json`. Le changelog le dit.
* Les décisions acceptées et les audits datés continuent de dire `dum` : ils consignent ce qui était
  vrai quand ils ont été écrits, et ne sont pas modifiés.

### Risques

* Un futur outil nommé `any` sur la même machine masquerait celui-ci, ou serait masqué par lui. Le
  risque est le même pour tout nom d'outil global, et n'est pas propre à celui-ci.

## Actions de suivi

* Expliquer dans la documentation utilisateur où se situe un dummy JustDummies dans le vocabulaire de
  *xUnit Test Patterns*, que certains lecteurs connaissent déjà.

## Références

* [ADR-0056](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.fr.md) — scaffolder le générateur une seule fois.
* [ADR-0057](0057-make-the-emitted-generator-a-first-class-iany.fr.md) — le générateur émis est un `IAny<T>` à part entière.
* [ADR-0072](0072-read-project-defaults-from-a-file-the-command-line-overrides.fr.md) — le fichier de défauts de projet.
* Issue [#272](https://github.com/Reefact/just-dummies/issues/272).
