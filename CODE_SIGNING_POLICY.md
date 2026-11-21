# Code signing policy

Free code signing provided by [SignPath.io](https://about.signpath.io/), certificate by [SignPath Foundation](https://signpath.org/).

## Ce qui est signé

Seul l'exécutable `ColorPanel.exe`, compilé à partir du code source de ce dépôt par GitHub Actions
(voir `.github/workflows/build.yml`). Aucun binaire compilé à la main n'est signé.

## Rôles

| Rôle | Personne |
|---|---|
| Auteur / Committer | [kelldev1](https://github.com/kelldev1) |
| Relecteur | [kelldev1](https://github.com/kelldev1) |
| Approbateur des signatures | [kelldev1](https://github.com/kelldev1) |

L'authentification à deux facteurs est obligatoire sur GitHub et SignPath pour tous les membres.

## Vie privée

Color Panel ne collecte aucune donnée et n'envoie aucune information à un serveur.
Il écrit seulement, en local, un journal d'erreurs dans `%LocalAppData%\ColorPanel\error.log`.

## Intégrité

Les versions signées sont publiées uniquement sur la page [Releases](../../releases) de ce dépôt.
L'empreinte SHA-256 de chaque archive est indiquée dans les notes de version.

