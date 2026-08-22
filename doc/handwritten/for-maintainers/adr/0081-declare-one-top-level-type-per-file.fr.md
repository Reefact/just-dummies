# ADR-0081 | Déclarer un seul type de premier niveau par fichier, via un analyseur de style tiers

🌍 🇬🇧 [English](0081-declare-one-top-level-type-per-file.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-08-22
**Accepted:** 2026-08-22
**Decision Makers:** Reefact

## Contexte

Les règles de style de ce dépôt vivent à deux endroits. Celles que Roslyn sait exprimer sont
réécrites dans `.editorconfig` et appliquées par le build ; `JustDummies.sln.DotSettings` reste
la source de vérité pour les autres
([ADR-0034](0034-enforce-the-style-rules-the-compiler-can-express.fr.md)). Ce partage existe
parce qu'une règle tenue uniquement dans le DotSettings n'est lue que par Rider : la règle du
type explicite a dérivé jusqu'à 203 violations sur 17 fichiers alors qu'elle était nominalement
en sévérité erreur, sans que rien n'en signale jamais une.

« Un fichier déclare un seul type de premier niveau » tombe du côté non appliqué de ce partage.
Aucune règle Roslyn intégrée ne l'exprime, donc `.editorconfig` ne peut pas la porter. Les
analyseurs `JD` du dépôt ne le peuvent pas davantage : ils sont publiés à l'intérieur du package
([ADR-0023](0023-ship-justdummies-analyzers.fr.md)), où une règle sur l'organisation des
fichiers de ce dépôt atteindrait chaque consommateur de JustDummies et gouvernerait du code qui
ne la regarde pas.

L'arbre ne suit pas la règle aujourd'hui. Mesuré sur la solution, 21 fichiers portent plus d'un
type de premier niveau, dont 11 dans les projets publiés et 10 dans les projets de test. Trois
fichiers en concentrent l'essentiel : une hiérarchie de nœuds d'expression régulière, un fichier
de source aléatoire qui a accumulé des helpers sans rapport, et le rapport d'exécution du CLI.

`StyleCop.Analyzers` porte la règle sous `SA1402`, son corollaire sur le nom de fichier sous
`SA1649`, et une règle d'accessibilité des champs sous `SA1401`. Sa dernière version stable est
la 1.1.118, publiée en 2019 — avant les `record` et avant les namespaces à portée de fichier,
que ce code utilise tous les deux. Sa ligne active est une préversion, la 1.2.0-beta.556, qui est
ce que l'écosystème .NET consomme en pratique.

La surface du package a été mesurée sur ce code plutôt qu'estimée. Activée en entier, elle
signale 24 380 avertissements. Décliner les familles qui gouvernent l'espacement, la mise en
page, le préfixe `this.`, les régions, les en-têtes de fichier, les champs préfixés d'un
souligné et la documentation XML — terrain que le DotSettings détient ou que les conventions
maison contredisent — en laisse 1 074. Ne garder que les trois règles ci-dessus en laisse 152,
sans aucun effet sur les surfaces Sonar, analyseurs .NET et `IDE*` existantes. Décliner la règle
dans les projets de test en laisse 72, sur les 11 fichiers publiés. Une règle, `SA0001`, survit
au déclin de sa catégorie et demande un déclin nommé.

Deux comportements ont été mesurés plutôt que supposés. `SA1402` n'exempte **pas** les types qui
ne diffèrent que par l'arité générique : `Toto` et `Toto<T>` dans un même fichier sont signalés.
`SA1649` accepte aussi bien `Toto.cs` que `Toto{T}.cs` comme fichier de `Toto<T>`, et refuse
`TotoOfT.cs` ainsi que l'écriture métadonnées. L'arbre ne contient aujourd'hui aucune paire
`Toto` / `Toto<T>` : 16 types génériques et 337 non génériques, sans aucun nom dans les deux
ensembles.

Trois dispositifs existants pèsent sur la façon d'adopter cela. `SonarAnalyzer.CSharp` est déjà
référencé comme actif de build uniquement, ce qui est précisément ce qui tient un analyseur hors
du graphe de dépendances de chaque package publié
([ADR-0003](0003-host-dummies-as-a-standalone-package.fr.md)). `EnforceCodeStyleInBuild` n'est
délibérément pas restreint à la CI, donc une règle configurée se signale au moment où le code
est compilé plutôt qu'une fois la pull request ouverte. Et le cliquet de la CI promeut chaque
avertissement en erreur, donc une règle laissée en sévérité avertissement bloque à l'entrée.

Une suppression dans ce dépôt nomme sa règle par une constante de catalogue plutôt que par un
littéral, et les analyseurs `DCAT` signalent un littéral comme une erreur
([ADR-0050](0050-name-a-suppressed-rule-through-a-catalogue-constant.fr.md)). Un catalogue
décrivant les règles StyleCop est publié sous `DiagnosticCatalog.StyleCop`.

## Décision

Un fichier déclare un seul type de premier niveau, appliqué au build par un analyseur de style
tiers pris comme actif de build uniquement, toutes les autres règles de cet analyseur étant
déclinées et la règle elle-même déclinée dans les projets de test.

## Justification

**Le dispositif non appliqué est celui qui a déjà échoué ici.** L'ADR-0034 existe parce qu'une
règle de style dont le seul foyer était le DotSettings a dérivé jusqu'à 203 violations sans que
personne puisse les voir. Écrire « un type par fichier » dans les instructions d'agent et s'en
remettre à la revue reconstruirait exactement ce dispositif, avec le même angle mort : un
contributeur qui n'ouvre pas Rider — un humain sans ReSharper, ou un agent, qui ne peut analyser
ce fichier en aucune circonstance — ne se voit rien signaler.

**L'application appartient au build, pas à la pull request.** Le dépôt a déjà fait ce choix une
fois, en refusant de restreindre `EnforceCodeStyleInBuild` à la CI : le but est d'atteindre
celui qui écrit le code, pendant qu'il l'écrit. Un analyseur hérite gratuitement de cette
propriété. Un contrôle qui ne tourne qu'en CI ne parlerait qu'une fois la branche poussée, soit
plus tard que la règle ne le mérite.

**La règle existe déjà, correctement, et ce dépôt ne peut pas en héberger sa propre copie.** Les
analyseurs `JD` sont publiés dans le package, donc une convention sur l'organisation des
fichiers de ce dépôt n'y a pas sa place ; un projet d'analyseur privé signifierait écrire une
règle Roslyn qui existe déjà, et la porter. La règle existante traite en outre ce qu'une
heuristique manquerait — types partiels, types imbriqués, différence entre une déclaration de
premier niveau et du C# cité dans une fixture de test — et apporte avec elle le corollaire sur
le nom de fichier.

**Décliner le reste est ce qui préserve l'ADR-0034, non ce qui la sape.** La mesure montre que
la grande majorité des règles du package gouvernent l'espacement, les accolades, les lignes
vides et l'alignement en colonnes : précisément le terrain que l'ADR-0034 laisse au DotSettings,
et précisément ce que ce dépôt ne sait reproduire avec aucun outil qu'un agent puisse lancer.
Plusieurs autres contredisent des conventions que ce code tient exprès — champs préfixés d'un
souligné, régions. Les adopter n'étendrait pas l'ensemble appliqué, cela dresserait deux sources
de vérité l'une contre l'autre. La frontière se déplace d'une bande délibérée, pas de deux cents
règles.

**Les projets de test sont exclus parce que leur regroupement est l'unité que le lecteur
cherche.** Un fichier nommé d'après les diagnostics qu'il couvre, portant une classe par
diagnostic, annonce son contenu dans son propre nom ; le découper produirait des fichiers vers
lesquels personne ne navigue et perdrait le regroupement que le nom promet. La section de
`.editorconfig` déjà réservée aux tests porte des déclins de cette forme.

**Le cas de l'arité générique n'appelle aucune exception, et c'est pourquoi aucune n'est
accordée.** La préoccupation qui a ouvert cette question — `Toto` et `Toto<T>` seraient-ils
séparés de force — trouve sa réponse dans le corollaire sur le nom de fichier plutôt que dans
une dérogation : la paire se répartit en deux fichiers qui satisfont tous deux la règle, et
aucun n'a besoin d'une suppression. Accorder une exception que le mécanisme ne sait pas
exprimer, pour un cas que le nommage résout déjà, aurait ajouté une règle sans travail à faire.

**Les coûts acceptés sont une dépendance en préversion et une migration bornée.** La version
stable précède des fonctionnalités du langage que ce code utilise et se signalerait sur ses
propres lacunes, donc la ligne de préversion est la seule utilisable ; c'est aussi celle que
l'écosystème fait tourner. La migration porte sur 11 fichiers et 36 types extraits, connus
plutôt qu'estimés, et se paie une fois.

## Alternatives envisagées

### Laisser la convention non appliquée, dans les instructions d'agent et en revue

Envisagée parce qu'elle ne coûte rien à adopter, ne demande aucune dépendance, et laisse chaque
jugement — y compris sur les trois fichiers où le regroupement se défend — au lecteur.

Rejetée parce que l'ADR-0034 est le procès-verbal de ce dispositif exact échouant dans ce dépôt
exact. Une règle que seul un lecteur applique n'est appliquée que sur les fichiers que ce
lecteur ouvre, et les contributeurs qui auraient le plus besoin qu'on la leur signale sont
justement ceux à qui on ne peut pas.

### Un script de contrôle sous `tools/`, lancé par la CI

Envisagé parce qu'il ne prend aucune dépendance, et parce que `tools/` héberge déjà des
contrôles de cette forme que la CI lance et qu'on tient délibérément hors de la solution.

Rejeté sur deux points. Il ne parlerait qu'une fois la pull request ouverte, abandonnant la
visibilité au build que le dépôt a choisie délibérément ailleurs. Et compter les types de
premier niveau depuis l'extérieur du compilateur revient à réimplémenter, en heuristique, ce que
le langage définit : types partiels et imbriqués, et le C# écrit dans les littéraux de chaîne
des fixtures de test. Mesurée, une telle heuristique divergeait déjà de la réponse du
compilateur sur cet arbre.

### Un analyseur de première partie, privé à ce dépôt

Envisagé parce que le dépôt construit, teste et publie déjà des analyseurs Roslyn : l'outillage
— plancher Roslyn épinglé, projet de tests, conventions établies — existe.

Rejeté parce qu'il réimplémenterait une règle qui existe déjà et qui est déjà correcte, et parce
qu'une règle `JD` porte un entretien que cette convention ne justifie pas : un identifiant, un
message, une entrée de suivi de version et une page de documentation dans chaque langue. Le
catalogue publié est fait pour les règles dont les utilisateurs de cette bibliothèque ont
besoin, pas pour l'organisation des fichiers de ce dépôt.

### Adopter l'ensemble des règles de l'analyseur

Envisagé parce qu'une dépendance prise pour trois règles est un mauvais échange, et parce que le
package porte des règles réellement utiles au-delà d'elles — l'ordre des membres avant tout.

Rejeté tel que posé, parce que la mesure montre ce que « l'ensemble » signifie ici : les plus
grandes familles gouvernent la mise en page que le DotSettings détient, et les adopter mettrait
deux configurations en conflit sur les mêmes lignes. L'ordre des membres est la seule bande qui
mérite d'être reconsidérée, à 606 sites et en conflit probable avec les régions de ce code ;
c'est une décision distincte, et la fondre ici la dissimulerait.

## Conséquences

### Positives

* La règle se signale là où le code s'écrit — dans l'IDE, dans `dotnet build`, et à un agent qui
  ne sait pas lire le DotSettings — au lieu d'un commentaire de revue ou de rien du tout.
* Le corollaire sur le nom de fichier vient avec elle, donc le nom d'un fichier et son type
  restent accordés.
* L'analyseur est un actif de build uniquement, donc aucun graphe de dépendances de package
  publié ne change.
* L'exception dont partait la question disparaît : `Toto` et `Toto<T>` ont chacun leur fichier,
  et la convention de nommage du générique est fixée plutôt que laissée au goût.

### Négatives

* Une migration de 11 fichiers publiés et 36 types extraits précède l'application de la règle, et
  touche trois fichiers dont le regroupement était délibéré.
* Le dépôt prend une dépendance sur un package en préversion, et toute la surface de règles de ce
  package doit être déclinée explicitement puis maintenue déclinée.
* Une règle survivant au déclin de sa catégorie a montré que décliner par famille ne suffit pas
  seul ; la configuration doit être vérifiée contre un build plutôt que raisonnée.

### Risques

* La ligne de préversion est la stable de fait de l'écosystème depuis des années, mais elle ne
  porte aucun engagement de version ; une version future peut ajouter des règles dans des
  familles déjà déclinées. Décliner par famille plutôt que règle par règle est ce qui borne cela.
* Découper les trois fichiers cohésifs échange une unité lisible contre plusieurs petites. La
  hiérarchie d'expressions régulières laisse en particulier une base abstraite de sept lignes
  dans un fichier à elle seule, qui ne porte pas grand-chose isolément.

## Actions de suivi

* Découper les 11 fichiers publiés, extraire le type qui n'appartient pas à la hiérarchie de
  nœuds, et donner à chaque type générique l'écriture à accolades de son nom de fichier.
* Écrire les déclins avec leur raison, dans la forme qu'emploient les entrées existantes de
  `.editorconfig`, plutôt qu'en liste nue.
* Décider l'ordre des membres séparément, face à ses 606 sites mesurés et aux conventions de
  régions qu'il rencontrerait.

## Références

* [ADR-0034](0034-enforce-the-style-rules-the-compiler-can-express.fr.md) — le partage que cette
  décision déplace d'une bande, et la dérive qui l'a justifié.
* [ADR-0035](0035-state-the-coding-rules-where-an-agent-can-act-on-them.fr.md) — où une règle
  doit vivre pour qu'un agent puisse agir dessus.
* [ADR-0039](0039-derive-the-build-rule-set-from-the-quality-profile.fr.md) — la forme que ce
  dépôt donne à un jeu de règles appliqué : appartenance énoncée, exceptions écrites avec leur
  raison.
* [ADR-0003](0003-host-dummies-as-a-standalone-package.fr.md) — pourquoi un analyseur est pris
  comme actif de build uniquement.
* [ADR-0050](0050-name-a-suppressed-rule-through-a-catalogue-constant.fr.md) — comment une
  suppression nomme sa règle, si jamais une devenait nécessaire ici.
* [ADR-0023](0023-ship-justdummies-analyzers.fr.md) — pourquoi les analyseurs de première partie
  ne peuvent pas héberger une convention interne au dépôt.
