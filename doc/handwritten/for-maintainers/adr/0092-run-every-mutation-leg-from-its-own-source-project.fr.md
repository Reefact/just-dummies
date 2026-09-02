# ADR-0092 | Lancer chaque jambe de mutation depuis son propre projet source

🌍 🇫🇷 Français (ce fichier) · 🇬🇧 [English](0092-run-every-mutation-leg-from-its-own-source-project.md)

**Statut :** Accepted
**Proposé :** 2026-08-31
**Accepté :** 2026-09-02
**Décideurs :** Reefact

## Contexte

Un verdict de mutation est une réponse à propos d'un oracle : *un test de cette suite échoue-t-il sur
cette réécriture ?* Changez la suite et le même mutant change de verdict ; quels tests jugent n'est
donc pas un réglage de performance — c'est ce que le score signifie.

L'[ADR-0022](0022-gate-pull-requests-on-the-mutation-score-of-the-diff.fr.md) a établi la vérification
et donné à chaque composant une configuration Stryker nommant le projet à muter et les projets de test
qui doivent tuer ses mutants. L'[ADR-0026](0026-measure-justdummies-mutation-against-the-unit-suite-only.fr.md)
a ensuite réduit l'oracle de la bibliothèque à sa suite d'exemples déterministe, en retirant la suite
de propriétés FsCheck pour deux motifs qu'elle énonce : un oracle aléatoire rend un mutant tuable à
une exécution et survivant à la suivante, et cent cas par propriété sont la moitié lente de chaque
mutant.

Ces configurations nomment aussi la solution. Mesuré le 2026-08-31 contre le moteur épinglé (4.16.0),
c'est ce champ qui décide de l'oracle, et non la déclaration : Stryker découvre lui-même les projets
de test — tout projet de la solution référençant l'assembly mutée — et ne lit jamais la liste.
L'exécution de la bibliothèque a rapporté **2 119 tests**, c'est-à-dire toutes les suites du dépôt,
là où sa configuration en nomme une de **790**. Rien n'avertit.

L'ADR-0026 n'a donc jamais été en vigueur. Le commit qui a retiré la suite de propriétés de la liste
a atterri le jour même où la configuration a été créée, sur un fichier nommant déjà la solution, et
n'a rien retiré — la décision n'a jamais été en vigueur pour une seule exécution. La suite de
propriétés juge depuis lors chaque mutant de la bibliothèque, et la dépendance à la graine que cette
décision existe pour supprimer est restée présente tout du long.

Trois mécanismes de restriction ont été mesurés, aucun n'est un remède : la surcharge du projet de
test en ligne de commande laisse le compte inchangé ; le filtre de cas de test est accepté puis
silencieusement ignoré sous le runner MTP, un filtre ne correspondant à aucun test produisant encore
le même score sur les mêmes mutants ; et un fichier de filtre de solution, que MSBuild construit sans
broncher, fait abandonner Stryker. Lancer le moteur depuis le répertoire du projet muté, sans nommer
de solution, est la seule forme sous laquelle la liste déclarée est l'oracle.

Rien aujourd'hui ne barre sur un score. Deux des trois composants ne portent aucun seuil, le balayage
hebdomadaire désactive le sien, et la vérification par pull request est consultative depuis
l'[ADR-0025](0025-make-the-per-pull-request-mutation-gate-advisory.fr.md).

## Décision

Chaque jambe de mutation s'exécute depuis le répertoire du projet qu'elle mute, aucune configuration
Stryker ne nomme de solution, et un test de la suite d'exemples fait échouer la construction si l'une
le fait.

## Justification

* **Une mesure dont l'oracle n'est pas celui déclaré n'est pas une mesure.** Les scores publiés depuis
  l'ADR-0026 répondent à une question que personne n'a posée, et un lecteur n'a aucun moyen de s'en
  apercevoir : la configuration dit une chose, l'exécution en fait une autre, et les deux ne se
  rencontrent jamais. La correction est la propriété que ce dépôt refuse de borner, et un instrument
  qui rapporte faux est le même défaut d'un cran au-dessus.
* **Le corriger ne coûte rien maintenant, et davantage chaque semaine.** Sans seuil à faire tomber,
  les scores peuvent bouger librement ; le jour où une barre sera posée à partir d'un chiffre publié,
  ce chiffre aura été mesuré avec le mauvais oracle et la correction deviendra un changement qui fait
  échouer des pull requests. Entre-temps, chaque balayage hebdomadaire publie une tendance de plus qui
  n'est pas celle qu'on y lit.
* **Le répertoire de travail est contraignable là où la déclaration ne l'est pas.** La liste est inerte
  dans un contexte de solution et aucune option ne la rétablit : honorer la déclaration n'est donc pas
  affaire de l'écrire plus soigneusement, cela demande l'invocation qui la lit. Une convention tenue
  par le seul soin est précisément ce qui a été perdu la première fois, et c'est pourquoi le test fait
  partie de la décision plutôt que de l'accompagner.
* **Elle rétablit l'ADR-0026 plutôt qu'elle ne la remplace.** Cette décision était juste et reste
  intacte : l'oracle est la suite d'exemples déterministe. Ce qui manquait, c'était tout moyen de
  savoir qu'elle n'avait pas pris effet.

## Alternatives considérées

### Corriger la déclaration pour qu'elle décrive ce qui s'exécute

Considérée parce qu'elle est honnête et ne coûte rien : supprimer les listes inertes, et consigner que
l'oracle est toute suite référençant l'assembly mutée. Rejetée parce qu'elle conserve le défaut et se
contente de le documenter. La suite de propriétés continuerait de rendre les verdicts dépendants de la
graine, ce qui est le tort identifié par l'ADR-0026 ; et un dépôt qui répond à un instrument cassé en
réécrivant son étiquette a décidé que ses mesures étaient décoratives.

### Monter le moteur épinglé en espérant qu'un plus récent lise la liste

Considérée parce que le comportement est peut-être corrigé en amont. Rejetée comme remède ici : un
moteur plus récent invente de nouveaux mutants et déplace seul chaque score, il ne peut donc pas être
introduit dans un changement dont tout l'objet est de rendre un chiffre digne de foi. C'est une
décision distincte, et si elle aboutit, le répertoire de travail ne coûte rien à conserver.

### Rendre la déclaration vraie en rétrécissant la solution

Considérée parce qu'un filtre de solution ne nommant que le projet muté et sa suite restreindrait la
découverte à la source, sans toucher à l'invocation. Rejetée sur mesure : Stryker refuse un tel
fichier d'emblée, et abandonne là où MSBuild le construit sans broncher.

## Conséquences

### Positives

* L'ADR-0026 prend effet : les verdicts de la bibliothèque proviennent d'une suite déterministe, donc
  le même commit obtient deux fois le même score.
* Chaque mutant est jugé par une suite deux à trois fois plus petite, ce qui est le coût par mutant.
* Les configurations redeviennent lisibles pour ce qu'elles sont — un composant et la suite qui en
  répond.

### Négatives

* Tout score historique est périmé. Les chiffres publiés avant cette décision ont été mesurés contre
  un autre oracle et ne sont pas comparables à ce qui suit.
* Une jambe s'éloigne d'un cran de l'invocation depuis la racine du dépôt qu'un lecteur pourrait
  attendre, et la raison vit dans l'en-tête du workflow plutôt que dans la commande.

### Risques

* **Un composant dont la suite ne suffit véritablement pas.** Rétrécir l'oracle ne peut que baisser un
  score, et un composant qui comptait sur une suite voisine pour tuer ses mutants montrera des
  survivants qu'il ne montrait pas. C'est l'instrument qui fonctionne : le survivant était déjà là,
  jugé par un test appartenant à un autre composant. Le seul composant portant un vrai seuil n'est pas
  concerné — rien d'autre que sa propre suite ne le référençait — et il a été mesuré au-dessus de cette
  barre après le changement.

## Actions de suivi

* Rouvrir l'[ADR-0028](0028-drop-the-justdummies-generator-from-the-per-pull-request-mutation-matrix.fr.md).
  Elle a retiré la jambe par pull request du générateur sur un coût mesuré, et ce coût l'a été avec
  toutes les suites du dépôt jugeant chaque mutant. L'oracle en est désormais une fraction, la prémisse
  a donc bougé et la jambe est peut-être redevenue abordable. Cette décision ne la rétablit pas : c'est
  à l'ADR-0028 de se rouvrir, sur une mesure fraîche.
* Lire le premier balayage publié après ce changement comme une nouvelle référence, et non comme une
  régression contre les anciens chiffres.

## Références

* ADR-0022 — Gate pull requests on the mutation score of the diff : la décision qui a donné une configuration à chaque composant.
* ADR-0025 — Make the per-pull-request mutation gate advisory : pourquoi aucun score ne barre aujourd'hui.
* ADR-0026 — Measure JustDummies mutation against the deterministic unit suite only : la décision que celle-ci rend effective.
* ADR-0028 — Drop the JustDummies generator from the per-pull-request mutation matrix : le modèle de coût que celle-ci déplace.
* [`workflows/justdummies-mutation.fr.md`](../workflows/justdummies-mutation.fr.md) — le câblage des jambes, et les mesures derrière ce record.
