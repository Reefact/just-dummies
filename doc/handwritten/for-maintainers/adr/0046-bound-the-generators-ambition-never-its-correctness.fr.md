# ADR-0046 | Borner l'ambition du générateur, jamais sa correction

🌍 🇬🇧 [English](0046-bound-the-generators-ambition-never-its-correctness.md) · 🇫🇷 Français (ce fichier)

**Status:** Accepted
**Proposed:** 2026-08-01
**Accepted:** 2026-08-01
**Decision Makers:** Reefact

## Contexte

`JustDummies` produit une valeur arbitraire et valide pour les contraintes déclarées au site
d'appel. Un générateur est une recette fluente : chaque contrainte rétrécit ce qui peut être tiré,
des contraintes contradictoires échouent à la déclaration en nommant les deux côtés, et la valeur
est construite pour satisfaire toute la spécification plutôt que tirée puis filtrée.

Sept décisions acceptées de cette base bornent chacune quelque chose, indépendamment et pour leurs
propres raisons locales :

| Décision | Ce qu'elle borne |
| --- | --- |
| [ADR-0004](0004-gate-distinct-collections-by-cardinality-else-bounded-draw.fr.md) | Une collection distincte utilise un tirage dédupliquant borné et échoue explicitement quand elle ne peut pas atteindre le cardinal demandé. |
| [ADR-0005](0005-cap-any-combine-at-arity-eight.fr.md) | `Any.Combine` fournit les arités deux à huit et s'arrête là. |
| [ADR-0008](0008-generate-strings-from-a-home-grown-regular-subset.fr.md) | `Any.StringMatching` couvre le sous-ensemble régulier avec le parseur maison et refuse un construit non régulier en le nommant, plutôt que de prendre une dépendance vers un automate regex. |
| [ADR-0012](0012-meet-string-exclusions-with-a-bounded-redraw.fr.md) | Une exclusion de chaîne est honorée par un redraw borné. |
| [ADR-0027](0027-guarantee-a-generated-regex-value-matches-by-bounded-redraw.fr.md) | Une valeur regex générée est garantie de matcher par un redraw borné. |
| [ADR-0029](0029-let-a-size-maximum-cap-without-steering-the-draw.fr.md) | Une taille que le générateur doit réellement produire est refusée au-delà d'un million. |
| [ADR-0031](0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.fr.md) | Les tirages flottants et décimaux restent dans une magnitude ordinaire plutôt que de parcourir toute la plage du type. |

**Aucune décision n'énonce la règle qu'elles partagent.** Chacune argumente sa borne depuis zéro, si
bien que toute nouvelle question de couverture est ré-argumentée de zéro — et peut l'être
différemment.

Deux autres faits pèsent. La bibliothèque affiche et garde une identité **zéro dépendance
d'exécution** ([ADR-0003](0003-host-dummies-as-a-standalone-package.fr.md)), vérifiée à la fois par
un test d'architecture sur les assemblies référencées et par une inspection du `.nupkg` produit. Et
toute exécution séquentielle doit se rejouer depuis la graine qu'elle rapporte, ce qui est la
promesse centrale du produit.

Enfin, ce dépôt est développé en grande partie par des sessions d'agent, dont les branches
constituent l'essentiel de son historique récent. Un agent s'oriente à partir du fichier
d'instruction qu'il lit, et ces fichiers dérivent : `CLAUDE.md` et `AGENTS.md` décrivaient encore un
autre produit jusqu'au 2026-07-31.

## Décision

`JustDummies` borne ce qu'il tente — la surface qu'il expose et l'effort qu'il dépense — et refuse à
cette frontière par une erreur nommée de première classe plutôt que de recourir à un mécanisme plus
puissant ; il ne borne jamais la correction d'une valeur qu'il rend.

## Justification

**Le nom est le périmètre.** Un dummy est une valeur qui tient la place d'une vraie dans un test. Sa
valeur vient d'être valide et sans particularité, pas d'être tirée par un procédé idéal. L'effort mis
à élargir ce qui peut être généré est de l'effort retiré aux deux propriétés dont les consommateurs
dépendent réellement : qu'une valeur rendue satisfait toutes les contraintes déclarées, et qu'une
exécution se rejoue.

**Élargir la couverture coûte généralement l'identité zéro dépendance.** Les mécanismes qui
lèveraient ces bornes — un automate regex, un solveur de contraintes, un backend SMT — ne s'écrivent
pas en un après-midi. En prendre un en dépendance serait la première de la bibliothèque,
apparaîtrait dans l'arbre et le SBOM de chaque consommateur, et contredirait une propriété que cette
base a déjà décidé de garder. Le choix est donc rarement « borné ou complet » ; c'est « borné, ou
complet au prix de l'identité ».

**Un mécanisme inexplicable est un risque de reproductibilité.** Une construction bornée se raisonne
depuis ses entrées et sa graine. Une recherche qui réussit par exploration se rejoue plus
difficilement avec confiance, et toute divergence se manifeste par un test qui échoue sans qu'aucun
diff ne l'explique — le pire échec que ce produit puisse infliger à un utilisateur.

**La malice échoue en silence ; le refus échoue utilement.** Un mécanisme assez puissant pour
satisfaire un jeu de contraintes que l'utilisateur n'avait pas l'intention d'écrire fera exactement
cela, masquant une erreur de modélisation que la détection de conflit en fail-fast existe justement
pour révéler. Une erreur nommée à la déclaration — la forme que l'ADR-0008 a déjà choisie — dit à
l'utilisateur quel construit n'est pas supporté et quoi faire à la place.

**C'est le fait d'écrire la règle qui la fait tenir.** Sept instances et aucun parent, cela signifie
que la huitième question est tranchée par le premier qui la tranche — et dans un dépôt écrit en
grande partie par des agents, par celui qui a lu le fichier le plus récent. Un enregistrement de
décision est stable là où un fichier d'instruction ne l'est pas, et c'est là qu'un contributeur
humain va chercher.

**La seconde moitié de la décision n'est pas décorative.** « Borner l'ambition », lu seul, est une
invitation à bâcler. La correction n'est pas négociable : une valeur rendue satisfait toutes les
contraintes déclarées, et le jeu d'analyseurs comme la suite de propriétés sont là pour tenir cette
ligne. La frontière porte sur ce que la bibliothèque *tente*, jamais sur ce qu'elle *garantit* une
fois qu'elle le fait.

## Alternatives considérées

### Laisser le principe implicite dans les sept décisions qui bornent

Elles sont acceptées, elles sont cohérentes, et un lecteur attentif peut inférer la règle. Rejeté :
une inférence n'est pas une décision. La valeur de la règle est précisément de répondre à une
question *avant* qu'on la re-dérive, et sept dérivations indépendantes montrent déjà qu'elle se
re-dérive au lieu d'être citée.

### L'enregistrer seulement dans `CLAUDE.md` et `AGENTS.md`

Ces fichiers sont lus en premier par les agents qui font l'essentiel du travail, donc l'argument de
portée est réel. Rejeté comme suffisant : ils sont opérationnels, mutables, et ils dérivent de façon
démontrée — les deux décrivaient un autre produit jusqu'au 2026-07-31. Un contributeur humain lisant
la base de décisions ne rencontrerait jamais le principe. Ils restent le bon endroit pour
l'instruction opérationnelle courte, qui cite désormais cet ADR.

### L'enregistrer en `ADR-0000`, daté avant l'ADR-0001, pour qu'il se lise en premier

Tentant, car le principe est fondateur et l'on aimerait qu'il soit lu avant les décisions qu'il
gouverne. Rejeté pour deux raisons. L'[ADR-0036](0036-keep-one-dated-line-per-state-an-adr-reached.fr.md)
exige une ligne datée par état que la décision a réellement atteint *dans ce dépôt*, et celui-ci a
été atteint aujourd'hui ; l'antidater casserait la règle qui gouverne les dates pour exprimer un
ordre de lecture. Et l'[ADR-0045](0045-renumber-the-decision-base.fr.md) a établi qu'un numéro est
une poignée stable, pas une position — présenter cet enregistrement en premier est le travail de
l'index, pas celui de la numérotation.

### Viser la complétude à la place

Supporter tous les construits regex, résoudre des jeux de contraintes arbitraires, retirer les
plafonds. Rejeté : cela achète des cas marginaux pour une bibliothèque de support aux tests, au prix
de l'identité zéro dépendance, d'un tirage que personne ne sait expliquer, et d'une surface de
maintenance sans commune mesure avec l'usage.

## Conséquences

### Positives

* Une question de couverture a désormais une réponse par défaut et un seul endroit à citer, au lieu
  de sept précédents à peser.
* La frontière devient une propriété documentée du produit plutôt qu'un manque apparent : « refuse un
  construit non régulier en le nommant » se lit comme une conception, pas comme une fonctionnalité
  inachevée.
* L'identité zéro dépendance gagne un argument qui s'applique avant même qu'une dépendance soit
  évaluée.

### Négatives

* Certaines demandes légitimes sont refusées. Qui veut un lookahead dans `Any.StringMatching` s'entend
  dire non, et la réponse reste non tant que cette décision n'est pas remplacée.
* Contributeurs et agents peuvent lire la première moitié de la décision comme une licence de
  négligence. La seconde moitié est énoncée pour cela, et toute revue doit la tenir.

### Risques

* Le principe pourrait être invoqué pour refuser quelque chose de réellement peu coûteux et utile,
  transformant une frontière délibérée en prétexte. Atténuation : refuser est une décision qui doit
  s'argumenter, exactement comme élargir — cet ADR relève la barre des deux côtés, pas d'un seul.
* L'équilibre bouge avec l'adoption. Sous la 1.0 et sans consommateur, borné et honnête est
  clairement juste ; une version stable avec des utilisateurs demandant un construit peut justifier
  un autre arbitrage. Ce sera une supersession, pas une réinterprétation.

## Actions de suivi

* Aucune. `CLAUDE.md` et `AGENTS.md` portent l'instruction opérationnelle et citent cet
  enregistrement.

## Références

* [ADR-0003](0003-host-dummies-as-a-standalone-package.fr.md) — l'identité zéro dépendance que cette
  décision protège.
* [ADR-0036](0036-keep-one-dated-line-per-state-an-adr-reached.fr.md) — pourquoi cet enregistrement
  est daté d'aujourd'hui plutôt qu'antidaté.
* [ADR-0045](0045-renumber-the-decision-base.fr.md) — pourquoi son numéro ne porte aucun ordre de
  lecture.
* Les sept décisions qui bornent, listées en *Contexte*, que cet enregistrement consolide sans les
  remplacer. Aucune n'est superseded : chacune reste la décision de son propre cas.
