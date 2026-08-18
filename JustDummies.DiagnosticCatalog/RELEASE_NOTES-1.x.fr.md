# Notes de version — JustDummies.DiagnosticCatalog, 1.x

Ce qui a changé pour vous, version par version, sur le train `catalog`. La version d'un catalogue lui est propre — elle ne décrit pas `JustDummies` au même numéro. Pour le registre technique complet, voir [CHANGELOG.md](https://github.com/Reefact/just-dummies/blob/main/JustDummies.DiagnosticCatalog/CHANGELOG.md).

## 1.0.0-preview.3 — 18 août 2026

_Le catalogue rattrape le jeu de règles que `JustDummies 1.0.0-preview.2` a livré : deux règles rejoignent les constantes, `JD029` et `JD030`._

### ✨ Nouveautés

- **`JustDummiesRule.JD029`** — *Une valeur écrite dans un pool qu'une contrainte déclarée refuse*, catégorie `JustDummies.Constraints`.
- **`JustDummiesRule.JD030`** — *Un dummy de chaîne qui ne déclare aucune longueur*, catégorie `JustDummies.Constraints`.

## 1.0.0-preview.2 — 7 août 2026

_Première version publiée — le catalogue atteint nuget.org pour la première fois, au numéro du jeu de règles que JustDummies 1.0 embarque. Il n'existe pas de `1.0.0-preview.1` : l'exécution de release de ce tag a échoué avant de rien publier, et le numéro est sauté plutôt que réutilisé._

### ✨ Nouveautés

- **`JustDummiesRule`** — les 28 règles d'analyse, de `JD001` à `JD028`, chacune portant `Id`, `Category`, `Title` et `HelpLinkUri` comme constantes de compilation qu'un `[SuppressMessage]` peut nommer.
- **`JustDummiesCategory`** — les quatre catégories qui regroupent ces règles.
- **Une activation cantonnée à votre propre projet** — `build/JustDummies.DiagnosticCatalog.props` n'active les contrôles que pour le projet qui référence ce catalogue, jamais pour celui qui en dépend seulement indirectement.

### 🙌 Améliorations

- **Une règle n'est jamais supprimée, un membre jamais renommé.** Une règle retirée du produit est conservée en `[Obsolete]` à la place, pour qu'une montée de version ne casse jamais un build sur un identifiant de diagnostic.
