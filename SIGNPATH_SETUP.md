# Mise en place de la signature gratuite (SignPath Foundation)

Ce que tu dois faire toi-même (je ne peux pas le faire à ta place) :

1. **GitHub** : créer un compte, activer la double authentification (obligatoire pour SignPath).
2. **Dépôt public** : créer un dépôt, `kelldev1/kelldev1-color-panel`, et y pousser ce dossier.
3. **Remplir** `CODE_SIGNING_POLICY.md` : remplacer les `TODO` par ton nom d'utilisateur GitHub.
4. **Première release** : créer un tag `v1.0.0` (sans signature au début). SignPath demande au moins une version publiée.
5. **Candidater** : https://signpath.org/apply
   - Licence : MIT (approuvée OSI)
   - Dépôt : l'URL GitHub
   - Page de téléchargement : la page Releases
   - Politique de signature : lien vers `CODE_SIGNING_POLICY.md`
6. **Attendre la réponse** (validation manuelle par SignPath). Pas de délai garanti.
7. **Une fois accepté** :
   - dans SignPath, créer le projet `ColorPanel` et la politique `release-signing`
   - dans GitHub, ajouter la variable `SIGNPATH_ORGANIZATION_ID` et le secret `SIGNPATH_API_TOKEN`
   - pousser un nouveau tag `v1.0.1` : l'exécutable est signé automatiquement.

Sources et conditions à jour : https://signpath.org/ et https://about.signpath.io/product/open-source

## À savoir

- Le certificat est au nom de **SignPath Foundation** : c'est l'éditeur affiché par Windows.
- SmartScreen peut encore avertir les premiers temps : il se base aussi sur la réputation du fichier.
- La signature n'a lieu que sur les versions **compilées par GitHub Actions** à partir de ce dépôt.

