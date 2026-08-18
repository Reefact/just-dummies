# Notes de version — JustDummies, 1.x

Ce qui a changé pour vous, version par version, sur le train `lib`. Pour le registre technique complet — chaque contrainte, chaque cas limite, chaque ADR — voir [CHANGELOG.md](https://github.com/Reefact/just-dummies/blob/main/JustDummies/CHANGELOG.md). Précédemment : [0.x](https://github.com/Reefact/just-dummies/blob/main/JustDummies/RELEASE_NOTES-0.x.fr.md).

## 1.0.0-preview.1 — 7 août 2026

_Pas une surface plus large que la 0.1.0 — la même, offerte pour la première fois à un consommateur extérieur, avec une nouvelle promesse : votre seed._

### ✨ Nouveautés

- **Une seed rejoue désormais à l'identique à travers les versions patch et mineures.** Épinglez-en une dans un test, et elle continue de tirer les mêmes valeurs à chaque montée de version au sein de `1.x` ([ADR-0049](https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-maintainers/adr/0049-replay-a-seed-across-patch-and-minor-versions.md)).

### 🙌 Améliorations

- Le package embarque désormais une icône, partagée par tous les packages publiés depuis ce dépôt.
- Les liens du readme embarqué pointent maintenant vers ce dépôt plutôt que celui dont JustDummies a été extrait.
