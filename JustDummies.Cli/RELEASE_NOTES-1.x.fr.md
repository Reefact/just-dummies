# Notes de version — dum (JustDummies.Cli), 1.x

Ce qui a changé pour vous, version par version, sur le train `cli`. Pour le registre technique complet — chaque contrainte, chaque cas limite, chaque ADR — voir [CHANGELOG.md](https://github.com/Reefact/just-dummies/blob/main/JustDummies.Cli/CHANGELOG.md).

## 1.1.0-beta.4 — 2 septembre 2026

_Un changement de licence que chaque consommateur devrait lire, un paramètre composé désormais tiré via son propre générateur, et une longue liste de corrections de lecture de gardes — plusieurs d'entre elles closant les limitations connues livrées avec 1.1.0-beta.3._

### ⚠️ Changements majeurs

- **JustDummies est désormais sous licence [PolyForm Internal Use 1.0.0](https://github.com/Reefact/just-dummies/blob/main/LICENSE), et non plus Apache 2.0 — source disponible, pas open source.** Vous pouvez lire, construire, modifier et exécuter l'outil pour vos propres opérations internes ou celles de votre entreprise ; vous ne pouvez pas redistribuer le logiciel. Les versions déjà publiées sur NuGet ne sont pas concernées et conservent la licence sous laquelle elles ont été livrées. Les contributions sont désormais régies par un [Contributor Agreement](https://github.com/Reefact/just-dummies/blob/main/CONTRIBUTOR_AGREEMENT.md).
- **Un paramètre composé est désormais généré comme `new AnyOrderReference()` — le générateur que son propre type possède — au lieu d'une recette dérivée des gardes de sa factory et recopiée à chaque site d'appel.** Là où la compilation cible ne porte pas encore ce générateur, `CS0246` à cette ligne nomme ce qu'il faut générer ([ADR-0089](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0089-draw-a-composed-parameter-through-the-generator-its-type-owns.md)).
- **L'appel pour un paramètre composé va désormais directement dans l'initialiseur du constructeur, et toute factory restante est renommée d'après ce qu'elle retourne** — `AnyValidQuantity()` plutôt que `QuantityFactory()`.
- **Un paramètre de type valeur nullable est désormais généré comme `.AsNullable()`, et non plus `.As(value => (T?)value)`** — nécessite une version de `JustDummies` portant `AsNullable()` ([ADR-0094](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0094-lift-a-nullable-value-type-rather-than-deriving-it.md)) ; un projet sur un package plus ancien garde l'ancien détour.
- **Le mot de provenance `factory`, et `candidates` sur un paramètre, disparaissent de `--format json`** — un paramètre n'est plus jamais laissé ouvert sur une factory ambiguë désormais, si bien que les deux sont toujours vides.

### 🐛 Corrections

- Un type refusé pour être abstrait ou générique le dit désormais (`TypeIsAbstract`/`TypeIsGeneric`) au lieu d'être signalé comme n'ayant pas de constructeur, et le même refus nomme désormais aussi la voie de la factory statique lorsque plusieurs factories éligibles sont à égalité.
- Une garde écrite dans un projet sur un autre framework cible (une bibliothèque `netstandard2.0` sous un projet de test `net8.0`, par exemple) est de nouveau lue au lieu d'être ignorée silencieusement.
- Une collection de collections typées par interface (`List<HashSet<T>>` pour un paramètre `List<ISet<T>>`) n'émet plus un fichier qui ne compile pas.
- Un `readonly struct` derrière un constructeur privé et un `Create` public est désormais généré via sa factory au lieu d'un défaut initialisé à zéro.
- **Chaque orthographe du rejet des espaces blancs se lit désormais `.NotBlank()` au lieu de `.NonEmpty()`** — nécessite la version correspondante de `JustDummies` portant `NotBlank()`.
- Une garde atteinte via un récepteur à accès conditionnel nul, un `throw` à l'intérieur d'une affectation `switch`, ou un appel à une bibliothèque de gardes en position de retour ou dans l'initialiseur d'une déclaration locale, est désormais marquée `unread guards` au lieu d'être passée sous silence.
- Une garde qu'un initialiseur `: this(…)`/`: base(…)`, ou une factory construite sur un constructeur privé gardé, se contente de déléguer est désormais repliée sur le paramètre qui la transmet là — clôturant plusieurs formes que 1.1.0-beta.3 lisait silencieusement de travers ou pas du tout.
- Une transmission `params` en forme normale est de nouveau lue ; seule la forme développée est refusée.
- Une transmission avec suppression de nullabilité (`value!`) replie la garde au lieu de la perdre, lue directement ou via un constructeur délégué.
- Un initialiseur `: this(…)` qui se délègue à lui-même ne fait plus déborder la pile.
- Une garde `.Count`/`.Length` sur un paramètre qui n'est ni une chaîne ni une collection est désormais marquée `unread guards` au lieu d'être lue avec la mauvaise famille.
- Une garde qu'un saut peut sauter depuis l'intérieur d'un bloc `using`, `lock` ou `checked` est désormais marquée `unread guards`, pas seulement en haut du corps.
- Un plancher de distinction sur `char`, `byte`, `sbyte`, `Int16`/`UInt16`, `Half`, ou le domaine d'une énumération, est désormais marqué `unread guards` au-delà de ce que l'élément peut réellement produire, au lieu d'être écrit avec assurance.
- Une énumération sans membre déclaré laisse désormais le paramètre ouvert au lieu de générer un appel que la bibliothèque elle-même refuse.

## 1.1.0-beta.3 — 24 août 2026

_La lecture des gardes devient à la fois plus large et plus stricte — deux bibliothèques de gardes nommées et un type construit par factory sont désormais lus, tandis que trois formes où l'outil se trompait avec assurance sur la portée d'une garde sont refusées au lieu d'être devinées._

### ⚠️ Changements majeurs

- **Une garde que l'outil ne peut pas placer au-dessus de chaque écriture de son paramètre, ou dont il ne peut pas prouver qu'elle s'exécute à chaque construction, est désormais marquée `unread guards`** — un scaffold qui compilait peut donc maintenant bloquer votre build jusqu'à ce que vous confirmiez le générateur. Il émettait auparavant une contrainte que le vrai constructeur ne tient pas, ce qui est la pire des deux défaillances : le fichier compilait, le résumé ne signalait rien à regarder, et le tirage levait dans le constructeur bien plus tard.

### ✨ Nouveautés

- **Les helpers de garde d'Ardalis.GuardClauses et de CommunityToolkit.Diagnostics sont lus, dans leurs deux graphies** — `Name = Guard.Against.NullOrWhiteSpace(name);` ne met plus fin au parcours avant que quoi que ce soit ait été lu, si bien qu'un constructeur gardé dans ce style cesse de produire des générateurs neutres sous un résumé n'affichant aucun doute nulle part ([ADR-0086](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0086-read-the-guard-helpers-of-named-libraries.md)). Une méthode d'une bibliothèque reconnue hors des lignes mesurées vaut `unread guards` plutôt que le silence.
- **Un type sans constructeur accessible se scaffolde désormais via sa propre factory** — l'objet-valeur validant canonique, un constructeur privé derrière un `Create` public, voit `Generate()` appeler la factory et ses gardes lues comme celles d'un constructeur, la ligne de signature du résumé nommant l'appel que le fichier émis effectue (`factory Email.Create(string)`). Un type abstrait doté d'une factory se scaffolde également.

### 🐛 Corrections de bugs

- **Une garde n'est plus lue comme une borne sur une valeur que le constructeur avait déjà remplacée, ou qu'elle n'atteignait jamais** — une écriture sur le paramètre lui-même (`percent = 100 - percent`), un initialiseur `: this(…)` ou `: base(…)` qui s'exécute entier avant le corps, une boucle, un `switch`, un `using` ou un `finally` dont l'outil ne lit pas l'ordre, et un `return` ou un `goto` au-dessus de la garde. Chacun est désormais placé correctement ou refusé.
- **Une garde de signe sur un paramètre non signé ne perd plus sa contrainte** — `if (size <= 0)` sur un `byte` ou un `uint` se lisait `.Positive()`, un membre que les générateurs non signés ne portent pas, si bien qu'elle était perdue en silence et qu'`Any.Byte()` tirait encore `0` ; elle se lit maintenant `.NonZero()`, ce qui est la même contrainte et non une plus lâche.
- **Les throw helpers arithmétiques d'`ArgumentOutOfRangeException` sont lus comme des gardes au lieu de bloquer le build** — `ThrowIfNegative`, `ThrowIfNegativeOrZero`, `ThrowIfZero`, `ThrowIfLessThan`, `ThrowIfGreaterThan`, `ThrowIfLessThanOrEqual` et `ThrowIfGreaterThanOrEqual` correspondent désormais aux mêmes lignes numériques qu'une comparaison construit déjà.
- **Une garde suivie d'un `else`, ou une chaîne `else if` qui lève de bout en bout, est lue au lieu d'être ignorée** — la lecture s'arrête à la première branche qui ne lève pas inconditionnellement, et cette branche, avec tout ce qui la suit, est marquée `unread guards`.
- **Une garde d'exclusion d'énumération se lit `AnyEnum<T>.DifferentFrom`** — `if (status == Status.None) { throw … }`, la garde d'énumération la plus courante qui soit, se lisait `.NonZero()`, un membre qu'`AnyEnum<T>` ne porte pas, et était perdue en silence.
- **Le résumé n'affiche plus `guard` pour une factory dont les gardes n'ont rien resserré** — le mot est calculé à partir des contraintes qui atteignent la chaîne émise, sur le chemin factory comme sur tous les autres.

### 📝 Limites connues

Mesurées après la sortie de cette version, suivies pour `cli-v1.1.0-beta.4` :

- **La dérogation pour les bibliothèques de gardes n'atteint qu'une affectation directe à un champ ou une propriété.** `Guard.Against.NegativeOrZero(total)` lu en position de retour, dans une déclaration locale, ou dans un initialiseur de constructeur, met toujours fin au parcours en silence, sans que rien ne signale la perte.
- **Une garde à une frame de distance de ce que l'outil lit peut encore être perdue.** Un initialiseur `: this(…)`/`: base(…)`, une factory construite sur un constructeur privé gardé, et une factory vers laquelle un constructeur choisi délègue, ne sont encore lus dans aucun des trois cas ; le résumé peut toujours afficher `guard` sur un domaine que le générateur émis ne respecte pas.

## 1.1.0-beta.2 — 22 août 2026

_La lecture des gardes devient nettement plus complète — une garde déléguée à un helper ou écrite dans une graphie moderne, et une garde qui lève dans une forme que l'outil ne savait pas parser avant, sont maintenant lues toutes les deux — et une garde que l'outil ne peut toujours pas garantir bloque la compilation au lieu de compiler en silence._

### ⚠️ Changements majeurs

- **La recette de chaque paramètre vit désormais dans sa propre factory method privée**, plutôt qu'en ligne dans l'initialiseur du constructeur — la forme de chaque fichier émis change, mais `Generate()`, les champs et les méthodes `With…` ne changent pas.
- **Une garde que l'outil ne peut pas garantir bloque désormais la compilation**, comme le fait déjà un paramètre non résolu, au lieu de conserver silencieusement un générateur neutre qui pouvait tirer une valeur que le vrai constructeur rejette ([ADR-0083](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.md)). Le résumé le compte à part — `1 TODO, 1 to verify` — puisqu'un générateur *a* été inféré ici.

### 🙌 Améliorations

- **Une garde déléguée à un helper, ou écrite avec un throw-helper moderne**, est maintenant lue comme son équivalent en `if` — `Ensure.NotBlank(name)` et `ArgumentNullException.ThrowIfNull(name)` ne passent plus en silence, et ne bloquent plus un build qui était déjà correct.
- **Une garde qui lève dans une forme que l'outil ne savait pas parser avant** — une chaîne `else if`, un bloc qui journalise avant de lever — est désormais marquée `unread guards` au lieu de ne rien rapporter du tout.
- **`openParameters` de `--format json` ne compte plus un paramètre qui a seulement besoin d'être vérifié** — il garde son sens publié, et le nouveau `summary.parametersToVerify` porte l'autre compte.
- **Le résumé ne se contredit plus au sujet d'un même paramètre** — une ligne lisant `to verify` est désormais comptée `to verify` dans le pied de page aussi, plus `TODO`.

### 🐛 Corrections de bugs

- **Plusieurs gardes arithmétiques, de taille et inter-paramètres, mal lues, silencieusement perdues, ou faisant planter l'exécution** sont maintenant lues correctement, composées, ou refusées explicitement : une condition sur une valeur dérivée du paramètre, un nom de paramètre inhabituel (`@event`, `_id`), une taille au-delà de ce que la bibliothèque peut produire, une constante de garde au-delà de `decimal`, une borne de taille non `int`, une garde `Enum.IsDefined`, une garde portant sur deux paramètres, et un type que le fichier émis ne pouvait pas construire.

## 1.1.0-beta.1 — 13 août 2026

_Une version mineure, additive de bout en bout : trois nouvelles options, et aucun comportement existant n'a changé. `dum generate Order` écrit toujours exactement ce qu'il écrivait en 1.0.0-beta.1, octet pour octet._

### ✨ Nouveautés

- **`--entry-point`** — un scaffold peut désormais aussi écrire un point d'entrée, pour atteindre un générateur comme ceux de la bibliothèque. `any` émet un membre d'extension C# 14, vous donnant `Any.Order()` à côté de `Any.Int32()` ; `static:<Name>` émet une racine `partial` que vous possédez, vous donnant `Dummies.Order()`, sans aucune exigence de version de langage. Par défaut : `none` ([ADR-0070](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0070-emit-an-entry-point-on-request-as-a-file-of-its-own.md)).
- **`--entry-point-namespace`** — place le fichier du point d'entrée dans un espace de noms qui lui est propre, distinct de celui du générateur.
- **`--format json`** — une exécution se rapporte comme un seul document JSON sur stdout au lieu du résumé console, pour un appelant qui est un script plutôt qu'un lecteur. Porte ce que le code de sortie ne peut pas — `summary.openParameters`, et une ligne par paramètre avec son expression et sa provenance. Les codes de sortie eux-mêmes ne bougent pas ([ADR-0071](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0071-report-a-run-as-data-without-moving-the-exit-codes.md)).
- **`dum.json`** — un fichier optionnel à côté du projet fournit des valeurs par défaut pour `output`, `namespace`, `entryPoint`, `entryPointNamespace` et `format`. La ligne de commande gagne toujours ([ADR-0072](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0072-read-project-defaults-from-a-file-the-command-line-overrides.md)).

### 🙌 Améliorations

- Quand un scaffold écrit deux fichiers, il écrit désormais les deux ou aucun — un `Any{Type}.Entry.cs` déjà présent refuse le scaffold entier, et `--force` couvre les deux.
- Le résumé console nomme maintenant l'appel que le point d'entrée a ouvert.

### 🐛 Corrections de bugs

- **`--namespace ""` et ses quatre équivalents ne pointent plus vers un conseil obsolète** maintenant que `dum.json` peut fixer la même option — le refus pointe désormais vers le fichier.
- **Un type de paramètre hors de tout espace de noms n'émet plus de `using` qui ne compile pas.** Le cas le plus fréquent : un paramètre dont le type n'a pas pu être résolu.

## 1.0.0-beta.1 — 10 août 2026

_Première version publiée — `dum` atteint nuget.org pour la première fois, en implémentant la spécification du scaffolder dans son intégralité. Une **beta**, pas une preview : un outil ne porte aucun socle d'API publique, sa surface étant la ligne de commande plutôt qu'un ensemble de types, et cette surface n'a pas encore été éprouvée par un projet hors de ce dépôt._

### ✨ Nouveautés

- **`dum generate <Type>`** — écrit le générateur de dummy d'un type, une fois, comme du code ordinaire que vous possédez ensuite.
- **Résolution.** Un paramètre de constructeur devient un générateur via la table de base, puis les clauses de garde du constructeur lui-même (`quantity <= 0` → `.Positive()`), puis la composition via une factory ou un `Any{Type}` déjà scaffoldé.
- **Un paramètre non résolu reste ouvert, bruyamment** — émis comme un identifiant qui n'existe pas, pour que votre propre build le signale à la ligne, avec le type en main ([ADR-0060](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0060-seed-generators-from-constructor-guards.md)).
- **Un résumé console** indiquant d'où vient chaque expression — table de base, garde, factory, générateur réutilisé, ou rien.
- **`--project`, `--output`, `--namespace`, `--force`, `--dry-run`**, et rien d'autre.
- **Durcissement du package** — SBOM SPDX embarqué, SourceLink, package de symboles, build déterministe, et une attestation de provenance sur l'artefact publié.

### 🙌 Améliorations

- Nécessite le package `JustDummies` dans le projet analysé. Aucune dépendance vers lui n'est déclarée dans aucun sens — chaque symbole de la bibliothèque est résolu par son nom dans votre compilation, exactement comme le font les analyseurs ([ADR-0063](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0063-give-the-scaffolder-no-dependency-on-the-package.md)), si bien que les versions de l'outil et de la bibliothèque ne peuvent jamais diverger.
