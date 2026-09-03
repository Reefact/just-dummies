# ADR-0055 | Tenir la documentation utilisateur à des contrats que le build vérifie

🌍 🇬🇧 [English](0055-hold-the-user-documentation-to-contracts-the-build-checks.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-08-09
**Accepted:** 2026-08-09
**Decision Makers:** Reefact

## Contexte

Le dépôt publie désormais un ensemble de documentation utilisateur : vingt pages, chacune avec son
jumeau français, sous `doc/handwritten/for-users/`, plus la paire de `README` racine. Ensemble, elles
portent bien plus d'une centaine d'exemples C#.

**Rien dans le build ne lit du Markdown.** Avant cette décision, le seul mécanisme capable de détecter
un exemple faux était un lecteur qui le remarque — et le lecteur qui rencontre un exemple faux en
premier est le débutant qui suit le guide de démarrage, incapable de distinguer un défaut de
documentation d'un défaut de bibliothèque, et qui en conclut que la bibliothèque est cassée.

Le risque n'est pas théorique, pour trois raisons qui sont des faits propres à ce dépôt plutôt que des
observations générales sur la documentation :

* **La surface publique n'est pas figée.** Elle est déclarée dans `PublicAPI.Unshipped.txt`, et la
  bibliothèque est en `1.0.0-preview`. Une contrainte renommée, une fabrique dont le type de retour
  change, une méthode qui migre vers un autre constructeur : chacune casse tous les exemples qui la
  nomment, et aucune ne casse un build.
* **Le produit publie 28 règles d'analyzer, et son propre code y est soumis.**
  `JustDummies.UnitTests` charge les analyzers pour qu'une règle qui se déclenche à tort soit
  rencontrée dans le dépôt avant de l'être par un consommateur
  ([ADR-0038](0038-guard-the-recipe-versus-value-boundary-with-analyzers.fr.md)). La documentation
  restait hors de cette boucle, tout en étant l'endroit d'où les exemples sont copiés.
* **Les jumeaux français sont exigés et vérifiés par rien.** `CLAUDE.md` énonce la règle — changer une
  page, changer son jumeau — et aucune étape de build ne l'a jamais contrôlée.

**Trois défauts ont été mesurés pendant la rédaction des pages**, tous ayant survécu à une relecture :

1. un exemple enchaînant un alphabet de lettres seules avec un suffixe contenant `.`, chaîne que la
   bibliothèque refuse dès la déclaration ;
2. une affirmation selon laquelle `Dummy.OneOf` se lie mal à un tableau, ce qui est faux — l'expansion
   `params` rend ce cas correct, et c'est une `List` conservée en variable qui pose problème ;
3. deux exemples de `[SuppressMessage]` écrits hors de tout type.

Le coût d'outillage est borné par ce que le dépôt porte déjà. `Microsoft.CodeAnalysis.CSharp` est déjà
une version de paquet gérée centralement, et `JustDummies.Analyzers.UnitTests` compile déjà des
extraits en mémoire et y exécute des analyzers.

Deux propriétés du corpus pèsent sur le périmètre. Les 28 pages de règles précèdent cet ensemble
documentaire et répondent à un autre cahier des charges : elles montrent du code `Noncompliant` à
dessein, et leurs exemples nomment des symboles qui n'existent que dans l'imagination du lecteur. Et
la documentation mainteneur porte des conventions de nommage propres — la base ADR ne donne aucun
suffixe de langue à ses pages anglaises — que le contrat de traduction devrait accommoder avant de
pouvoir s'y appliquer.

## Décision

Tout exemple C# de la documentation utilisateur est compilé contre les paquets publiés et inspecté par
les analyzers publiés, et toute page est tenue à une parité structurelle avec son jumeau français
ainsi qu'à des liens qui résolvent, par une suite de tests dont l'échec est un échec de build.

## Justification

La décision convertit la seule classe de défauts que la relecture ne rattrape pas en le seul signal
auquel ce dépôt réagit déjà.

**La compilation est la vérification qui correspond au mode de défaillance.** Un exemple qui nomme une
API disparue n'est pas un problème stylistique qu'un relecteur attentif repérerait ; c'est un fait
relevant du compilateur, et l'interroger est à la fois moins coûteux et plus fiable que de demander à
un humain de garder toute la surface publique en tête. Face à une surface explicitement non figée,
cette vérification doit être mécanique, sinon elle n'aura pas lieu.

**Compiler contre les paquets, et rien d'autre, est ce qui rend la garantie transférable.** Les
exemples se lient à `JustDummies`, à son adaptateur et à son catalogue comme un consommateur les
référence. Un exemple qui compile dans la suite compile donc dans le projet de test d'un lecteur,
seule promesse que fait réellement un extrait de code.

**Exécuter les règles publiées sur les exemples comble un déficit de crédibilité qui serait sinon
structurel.** Une bibliothèque qui publie 28 règles et apprend à ses lecteurs à les enfreindre plaide
contre elle-même, et les exemples sont la partie de la documentation la plus susceptible d'être copiée
telle quelle. Puisque les règles s'exécutent déjà sur le code du dépôt, exempter la documentation
laisserait le code le plus copié comme le moins vérifié.

**Les anti-patrons doivent rester exprimables : le contrat les admet donc par déclaration plutôt que
par exception.** Une page qui ne montre que du code correct ne peut pas apprendre à reconnaître
l'erreur. Un exemple déclare donc les règles qu'il compte enfreindre, ce qui garde l'intention visible
dans la source de la page ; et une déclaration qui cesse de se déclencher échoue également, car une
page affirmant « voici à quoi ressemble un générateur jeté » à côté d'un code qui n'en jette plus a
discrètement cessé d'être un exemple.

**La parité structurelle est vérifiée parce que c'est la moitié qui peut l'être.** Aucun test ne
distingue une traduction fidèle d'une traduction plausible, et prétendre le contraire achèterait une
confiance fausse. Ce qui est comparable, c'est le squelette — titres, blocs de code, marqueurs — et
c'est précisément la moitié qui disparaît : une section ajoutée en anglais et oubliée en français
laisse au lecteur francophone une documentation non pas fausse, seulement incomplète, soit l'échec
qu'aucun relecteur ne remarque.

**Les défauts mesurés tranchent le rapport coût-bénéfice.** Trois erreurs réelles en une seule passe
de rédaction, chacune se lisant comme une prose correcte, prouvent que la relecture n'attrape pas
cette classe. La suite les a toutes attrapées avant publication, au prix d'un harnais dont le dépôt
possédait déjà les pièces.

**Exclure les pages de règles garde cette décision centrée sur le contrat plutôt que sur elles.** Les
tenir au contrat de compilation supposerait de réécrire cinquante-six pages pour inventer les symboles
que leurs exemples nomment — une décision sur la documentation des analyzers, à argumenter pour
elle-même, et non une conséquence de la façon dont la documentation utilisateur est vérifiée. Elles
sont tenues aux contrats de traduction et de liens, qu'elles satisfont déjà.

## Alternatives considérées

### Laisser les exemples à la relecture

Le moins coûteux, et c'est le statu quo partout ailleurs dans l'industrie.

Rejeté parce que c'est exactement ce qui a été mesuré en échec : trois défauts ont survécu à la
relecture durant la passe même qui écrivait les pages, par l'auteur qui avait l'API sous les yeux. La
relecture détecte mal « cet identifiant n'existe plus », et se dégrade encore à mesure que la surface
bouge.

### Extraire chaque exemple dans un projet compilé, et inclure les fichiers dans les pages

Les exemples compileraient par construction, et un IDE les remanierait avec l'API.

Rejeté parce que cela inverse le flux de rédaction et éloigne le texte de la prose qui l'explique :
une page devient une suite d'inclusions, et la phrase introduisant un exemple s'écrit contre un
fichier que l'auteur n'a pas sous les yeux. L'approche résout aussi un problème plus étroit qu'elle ne
coûte — le lecteur lit la page, c'est donc là que le code doit être juste — et ne fait rien pour les
règles, la parité de traduction ou les liens.

### Tout compiler, y compris les 28 pages de règles

Uniforme, sans périmètre qu'un lecteur puisse mal comprendre.

Rejeté pour l'instant, car ces pages montrent délibérément du code non conforme nommant des symboles
imaginaires : le contrat ne pourrait être honoré qu'en les réécrivant. C'est une décision sur la
documentation des analyzers, qui mérite son propre argumentaire ; l'intégrer ici aurait fait porter à
cette ADR un changement que personne n'avait demandé.

### Vérifier le sens de la traduction, pas seulement sa structure

La forme la plus forte de la garantie de parité.

Rejeté parce qu'elle n'est pas atteignable : aucune vérification mécanique ne distingue une bonne
traduction d'une traduction plausible, et une vérification qui semblerait le faire autoriserait une
relecture moins attentive plutôt que davantage. Ne revendiquer que le squelette est ce qui garde le
contrat honnête sur ce qu'il vérifie.

## Conséquences

### Positives

* Un exemple qui se lie dans la suite se lie dans le projet de test d'un consommateur : la promesse que
  fait un extrait de code est celle qu'il tient.
* La documentation ne peut plus dériver silencieusement de l'API, ce qui rend tenable un ensemble
  documentaire de cette taille face à une surface non figée.
* Le code le plus copié du dépôt est désormais tenu aux règles que le produit publie.
* Un anti-patron est déclaré plutôt que fortuit, et ne peut pas se dégrader en exemple périmé.
* Un jumeau français ne peut pas perdre une section, un bloc de code ou une exclusion sans que le build
  le dise.

### Négatives

* Un changement de documentation peut casser le build. C'est le mécanisme qui fonctionne, et cela reste
  un coût : une page n'est plus un fichier que l'on modifie sans exécuter la suite.
* Les exemples doivent être écrits selon un contrat — les modes déclarés, le domaine d'illustration
  partagé, aucune directive d'import dans un exemple — ce qui contraint toute page future.
* Un exemple qui est du C# valide mais que le harnais ne sait pas envelopper exige une exclusion
  explicite : un lecteur de la source de la page rencontre donc une porte de sortie là où aucun défaut
  n'existe.

### Risques

* **La porte de sortie devient l'habitude.** Si s'exclure est plus facile qu'écrire un exemple liable,
  le contrat se vide. Atténué par un plafond sur le nombre d'exemples pouvant s'exclure, qui fait
  échouer la suite au lieu d'avertir.
* **Le domaine d'illustration enfle en second produit.** Des fixtures démontrant leurs propres patrons
  disputeraient l'attention du lecteur aux pages. Atténué en les gardant délibérément ordinaires.
* **Le périmètre est lu plus large qu'il n'est.** « La documentation est vérifiée » est faux pour les
  exemples des pages de règles, et un mainteneur futur qui supposerait le contraire ferait confiance à
  une garantie absente.

## Actions de suivi

* Envisager d'étendre le contrat de compilation aux 28 pages de règles, ce qui suppose une convention
  pour les symboles que leurs exemples nomment.
* Envisager d'étendre les contrats de traduction et de liens à la documentation mainteneur, ce qui
  suppose d'abord de trancher le nommage des pages anglaises de la base ADR.

## Références

* [ADR-0019](0019-split-the-justdummies-test-bed-between-example-and-property-suites.fr.md) — à quelle
  suite appartient un nouveau test.
* [ADR-0034](0034-enforce-the-style-rules-the-compiler-can-express.fr.md) — le précédent d'une règle
  déplacée de l'attention vers le build.
* [ADR-0035](0035-state-the-coding-rules-where-an-agent-can-act-on-them.fr.md) — pourquoi une règle que
  rien ne vérifie dérive.
* [ADR-0038](0038-guard-the-recipe-versus-value-boundary-with-analyzers.fr.md) — les analyzers que ce
  contrat exécute sur les exemples.
* Pull request [#40](https://github.com/Reefact/just-dummies/pull/40) — l'ensemble documentaire et la
  suite que cette décision consigne.
