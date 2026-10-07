# JustDummies — Audit des calques de la documentation française

🌍 **Langues :**  
🇫🇷 Français (ce fichier) | 🇬🇧 [English](./2026-10-07-french-documentation-calque-audit.md)

**Date :** 2026-10-07
**Révision auditée :** `2d99385` (sommet de `main` au moment de l'audit)
**Périmètre :** les 179 pages `*.fr.md` sous `doc/` — environ 251 000 mots répartis sur la
documentation utilisateur, la base de décisions, les pages de workflow, les deux
spécifications, les audits antérieurs et le relevé de migration. Les pages anglaises sont
**hors périmètre** : elles font foi (`.claude/rules/documentation.md`), et cet audit juge le
français en français, non à leur aune.
**Statut :** consultatif. Conformément à la convention du dépôt (ADR-0002), il produit des
recommandations, jamais des bloqueurs. Les choix de terme du §4 appartiennent à `@reefact`,
pas à un auditeur.

**Pourquoi il existe.** Une page française est une traduction, et ce qui la met en défaut
n'est pas la coquille : c'est le mot grammatical, qui ressemble à sa source anglaise, et qui
dit autre chose. Un tel mot survit à la relecture parce que rien n'y cloche. Le changement
qui a précédé cet audit en a trouvé trois, un par un
([ADR-0023](../adr/0023-ship-justdummies-analyzers.fr.md), ADR-0052, ADR-0092 et leurs
voisines), et c'est précisément la méthode que cet audit remplace : **tomber sur un calque
n'est pas une méthode, et le reste demeure.** Le premier constat de cet audit est que la passe
précédente était incomplète — voir le constat 1.

---

## 1. Méthode

Pas une lecture. Le corpus fait un quart de million de mots ; le lire en entier n'est ni
faisable ni ce qui rendrait le résultat fiable : un lecteur qui a déjà accepté un mot vingt
fois cesse de le voir.

L'audit est **piloté par les termes et reproductible**. Une batterie d'expressions
régulières, chacune encodant un calque soupçonné, a été passée sur tous les `*.fr.md` ; chaque
occurrence a ensuite été lue en contexte et classée à la main, face à son original anglais
quand le jugement l'exigeait. La liste des sondes figure au §6, pour qu'un audit ultérieur
puisse la relancer et comparer.

Trois familles de sondes :

* **Les faux amis** — un mot français qui existe, qui est grammatical, et qui ne veut pas dire
  ce que dit son jumeau anglais (`supporter`, `adresser`, `réaliser`, `assumer`, `prétendre`,
  `sensible`, `définitivement`, `supposer`, `délivrer`, `éventuellement`, `initier`,
  `opportunité`, `consistant`, `versatile`, `compléter`).
* **Les mots anglais restés debout dans la prose française** — `record`, *leg* rendu par une
  partie du corps, `gate`, *story*, *in-flight*, et les verbes franglais (`silencier`,
  `wrapper`, `rebuilder`, `skipper`).
* **Les conventions mécaniques** — l'espace devant `:` `;` `!` `?`, l'espace à l'intérieur des
  `« »`, et le caractère apostrophe.

Ce que les sondes ne peuvent pas voir, et que cet audit ne prétend donc pas couvrir : une
phrase idiomatique mot à mot qui argumente tout de même moins clairement que son original
anglais. Le §5 le dit sans détour.

## 2. Ce qui est sain

L'essentiel du corpus. Cela mérite d'être consigné : un audit qui ne rapporte que des défauts
se lit comme si le tout était défectueux.

* **La typographie est quasi irréprochable.** 1 967 deux-points portent leur espace française
  contre **2** qui ne l'ont pas (`0045-renumber-the-decision-base.fr.md`,
  `adr/README.fr.md`). Toutes les paires de guillemets espacent correctement leur contenu.
  Aucun corpus de cette taille ne tient cela par hasard.
* **Les faux amis difficiles sont bien employés.** `prétendre` (11 occurrences), `supposer`
  (13), `réaliser` (4), `définitivement` (4), `sensible` (12) et `assumer` (9) sont les mots
  qu'une traduction automatique rate, et les 53 occurrences sont du bon français — *mesuré,
  pas supposé*, *coût assumé*, *code sensible au décalage*, *prétendre le contraire*. Cinq
  autres sondes (`éventuellement`, `opportunité`, `initier`, `consistant`, `versatile`) ne
  renvoient rien du tout.
* **`adresser` est juste 10 fois sur 11.** L'exception est le constat 8.
* **Le vocabulaire maison est délibéré et cohérent** là où il garde un terme anglais à
  dessein : `run`, `job`, `runner`, `diff`, `commit`, `opt-in`, `quality gate`. Ce ne sont pas
  des calques ; ce sont les mots qu'emploient les développeurs francophones, et le dépôt a
  déjà tranché une fois sur cette famille.

## 3. Constats

Dix défauts, 126 lignes en tout. Chacun est un mot qui veut dire autre chose en français, ou un
mot qui n'est pas français ; chacun a une correction, nommée. Les comptes sont en lignes, pas en
occurrences — une ligne peut en porter deux.

| # | Terme | Lignes | Fichiers | Correction |
|---|---|---|---|---|
| 1 | `patte` pour *leg* | 38 | 4 | `job`, et `cible` une fois |
| 2 | `supporté` pour *supported* | 38 | 20 | `pris en charge` |
| 3 | `ce record` pour *this record* | 30 | 12 | `cet enregistrement` / `cette décision` |
| 4 | `silencier` | 6 | 2 | `supprimer` / `faire taire` |
| 5 | `histoire` pour *story* | 3 | 3 | selon le contexte |
| 6 | `délivrer` pour *deliver* | 4 | 4 | `livrer` / `obtenir` |
| 7 | `en vol` pour *in-flight* | 2 | 2 | `en cours` |
| 8 | `n'adresse` pour *does not address* | 1 | 1 | `ne traite` |
| 9 | `wrappe` | 1 | 1 | `déborde` |
| 10 | `rebuilde`, `skippé`, `floors` | 3 | 3 | `reconstruit`, `sauté`, `planchers` |

### 1 — `patte`, la troisième graphie de *leg*

**Le constat principal, et celui qui justifie d'auditer plutôt que de corriger au fil de
l'eau.** Le changement qui a précédé cet audit a sorti `jambe` du corpus et l'a remplacé,
selon le sens, par `job` (une case de matrice CI) ou par `cible` (un framework cible). Il a
laissé passer que le même mot anglais avait aussi été rendu par **`patte`** — une jambe
d'animal — 38 fois, dans
[l'ADR-0024](../adr/0024-guard-public-and-internal-arguments-against-null.fr.md),
[l'ADR-0028](../adr/0028-drop-the-justdummies-generator-from-the-per-pull-request-mutation-matrix.fr.md)
(l'essentiel) et [`workflows/README.fr.md`](../workflows/README.fr.md).

*Leg* a donc atteint le français par trois chemins : `jambe`, `patte`, `job`. L'un des trois
était un choix ; deux étaient des calques ; et la passe qui a corrigé l'un des calques a
laissé le plus gros debout. Une correction terme par terme ne peut pas trouver cela : elle ne
voit jamais que la graphie qu'elle cherchait.

La correction est celle déjà décidée pour `jambe`, et elle se partage de la même façon : `job`
là où la patte est une case de matrice — 37 des 38 — et `cible` sur la seule ligne où c'est un
framework cible (ADR-0024, *la patte moderne*, que l'anglais appelle *the modern leg*). Ce
partage est tout l'enjeu : une substitution aveugle aurait écrit *le job moderne* à cet
endroit, et dit quelque chose de faux.

### 2 — `supporté`

En français, `supporter` veut dire endurer. Une plateforme ne s'endure pas, elle est **prise
en charge**. La base le sait déjà :
[l'ADR-0001](../adr/0001-lock-the-analyzer-roslyn-floor.fr.md) écrit *un hôte pris en charge*
dans son deuxième paragraphe.

C'est le constat qui porte le plus loin : 20 fichiers, dont quatre relèvent de la
**documentation utilisateur** — `guides/faq.fr.md`, `packages/README.fr.md`,
`packages/justdummies.fr.md`, `generators/enums-and-choices.fr.md`. *Le plancher supporté est
.NET Framework 4.7.2* est lu par des utilisateurs, et dit que le plancher est enduré.

### 3 — `ce record`

En français, un `record` est une performance sportive. Ce n'est pas un écrit consigné, et une
ADR n'en est jamais un. L'index français de la base s'intitule lui-même *Enregistrements de
décisions d'architecture*, et les pages écrivent déjà **`cet enregistrement`** 55 fois et
**`cette décision`** 125 fois. Les 30 lignes qui disent `ce record` sont les intruses,
concentrées de l'ADR-0075 à l'ADR-0097.

À ne pas confondre avec le mot-clé C# `record`, qui est correct et doit rester : la
spécification et `JD028.fr.md` l'emploient à bon escient.

### 4 à 10 — le reste

* **`silencier`** (ADR-0050, ADR-0052) n'est pas un verbe français. Le domaine a déjà le bon :
  un `[SuppressMessage]` **supprime** une règle.
* **`histoire`** rend *story* à trois endroits où l'anglais veut dire un cas ou un exposé, pas
  un récit : *framework-floor story* (ADR-0019), *compatibility story* (ADR-0063), *a clean
  story* (ADR-0093). Le même défaut a été corrigé dans l'ADR-0023 (*analyzer story*) par le
  changement précédent, ce qui montre encore qu'il avait été rencontré et non cherché.
* **`délivrer`** est l'anglais *deliver*. Un signal, en français, est **livré** au mieux, et
  l'*aucun budget abordable ne le délivre* de l'ADR-0093 veut dire *l'obtient*.
* **`en vol`** pour un run de CI en cours est de l'anglais d'aviation ; un run est **en
  cours**.
* **`n'adresse`** (ADR-0093) est *address* au sens de *traiter* : **ne traite**.
* **`wrappe`** (ADR-0031) décrit un débordement d'entier ; en français, l'arithmétique
  **déborde**. Le `new Wrapper(value)` de `JD027.fr.md` est un extrait de code et ne doit pas
  être touché.
* **`rebuilde`** (`justdummies-mutation.fr.md`), **`skippé`** (`genany-sweep.fr.md`) et
  **`floors`** resté en anglais (ADR-0022) font une ligne chacun.

## 4. Les choix de terme, qui appartiennent au mainteneur

Ce ne sont pas des défauts. Chacun est une traduction défendable là où une autre peut être
préférée, et le volume rend la décision plus économique prise une fois que page par page.

* **`barrage` pour *gate*** — 37 lignes, presque toutes dans
  [l'ADR-0022](../adr/0022-gate-pull-requests-on-the-mutation-score-of-the-diff.fr.md). Un
  `barrage` retient ou bloque le passage ; la métaphore tient, et les développeurs
  francophones disent plutôt `gate`. Le dépôt a déjà penché de ce côté une fois, en gardant
  `quality gate` dans le français de l'ADR-0103 au lieu de le traduire. Changer 37 lignes sur
  ce précédent est un arbitrage que cet audit ne rend pas.
* **`vérifié contre` / `mesuré contre`** — 13 lignes. *Verified against* se dit `vérifié face
  à`, `confronté à` ou `par rapport à` ; `contre` se lit comme une opposition. L'audit du
  2026-07-20 de ce dépôt écrit lui-même *vérifiés de façon contradictoire face au code* : la
  meilleure forme est donc déjà dans le corpus. Léger, et compréhensible en l'état.
* **Le caractère apostrophe** — 177 des 179 pages emploient l'apostrophe droite `'`. Deux
  emploient la typographique `’`, 230 fois : `CONTRIBUTING.fr.md` et `SECURITY.fr.md`, toutes
  deux face à l'utilisateur. La typographie française préfère `’` ; l'usage du dépôt est
  massivement `'`. L'une ou l'autre réponse est cohérente ; l'état actuel ne l'est pas.

## 5. Ce que cet audit ne couvre pas

* **La qualité de l'argumentation.** Une page peut être idiomatique phrase après phrase et
  défendre tout de même moins bien son propos que l'anglais qu'elle reflète. C'est une
  relecture page à page face à l'anglais — un exercice différent et bien plus vaste, sur lequel
  les sondes ne disent rien.
* **Les termes auxquels les sondes n'ont pas pensé.** La méthode trouve ce qu'on lui désigne.
  Le constat 1 en est la preuve : `patte` n'a été trouvé que parce que *leg* était déjà un
  défaut connu et que l'audit a demandé en quoi d'autre il avait pu se changer. Un calque que
  personne n'a encore soupçonné est toujours là.
* **Les pages anglaises**, par périmètre, et **tout ce qui est hors de `doc/`** — `.claude/`,
  les changelogs et les fichiers markdown racine n'ont pas été sondés.
* **La parité structurelle**, qui ne relève pas d'un audit :
  `JustDummies.Documentation.UnitTests` la vérifie à chaque build.

## 6. La liste des sondes

Relançable telle quelle sur `doc/**/*.fr.md`, pour qu'un audit ultérieur compare au lieu de
repartir de zéro. Chacune renvoie des candidats, jamais des verdicts — chaque occurrence du §3
a été lue en contexte avant d'être comptée.

```
\b(ce|cet|cette|le|les|du|des|un|une|son|sa) records?\b
\bsupport(e|ent|é|ée|és|ées)\b
\bsilenci(e|es|ent|er|ée?s?)\b
\bpattes?\b
\bjambes?\b
\bhistoires?\b
\bbarrages?\b
\b(rebuild|skipp|trigger|wrapp|logg|shipp|checke|fetche)(e|es|ent|er|é|ée|és|ées)\b
(mesur|vérifi|compar|test|relu|confront|class|calibr|jug|valid|contrôl)\w* contre\b
\b(support|adress|délivr|réalis|assum|prétend|suppos)\w*
\b(éventuel|définitivement|initie|opportunit|consistan|versatil)\w*
en vol\b
\bfloors\b
```

Deux contrôles mécaniques, qu'une expression régulière seule n'exprime pas : le compte des `:`
précédés d'une espace face à ceux qui ne le sont pas, et le compte de `U+2019` face à `U+0027`
par fichier.

## 7. Recommandation

Appliquer le §3 — dix défauts, une correction nommée chacun, aucun jugement qui subsiste.
Trancher le §4 une fois, en trois réponses plutôt qu'en quatre-vingt-dix retouches. Laisser le
§5 comme l'énoncé honnête de ce qui reste non mesuré : le prochain audit de ce corpus devrait
être une relecture face à l'anglais, pas une nouvelle batterie de sondes, car les sondes ont
désormais rendu ce qu'elles pouvaient.
