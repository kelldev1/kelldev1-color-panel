# Color Panel

Contrôle des couleurs de tout l'écran sous Windows, dans n'importe quel jeu, **sans injecter de DLL**.

- Luminosité, contraste, saturation, teinte et balance RGB, en direct sur tout l'écran
- Plus de 100 presets avec aperçu sur de vraies photos
- Résolution et fréquence de chaque écran, avec retour automatique après 15 secondes
- Raccourci global **Ctrl + Alt + C** pour activer ou couper l'effet
- Fonctionne avec NVIDIA, AMD et Intel : tout repose sur l'API Magnification de Windows

## Téléchargement

Récupère `ColorPanel.zip` dans les [Releases](../../releases/latest), décompresse-le et lance `ColorPanel.exe`.
Garde le dossier `photos` à côté de l'exécutable (sans lui, l'application utilise des images générées).

Prérequis : Windows 10 ou 11 (64 bits). Le fond translucide demande Windows 11.

## Limites

- L'application règle l'image finale de l'écran. Elle ne gère pas les effets liés à la profondeur 3D (occlusion ambiante, flou de profondeur) comme ReShade.
- En plein écran exclusif, certains jeux ignorent l'effet : utilise le mode fenêtré sans bordure.
- Pas de résolution personnalisée, ni de DSR ou DLSS : ces fonctions appartiennent au pilote graphique.

## Compiler

Prérequis : [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
dotnet publish ColorPanel.csproj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o publish
```

## Signature du code

Voir [CODE_SIGNING_POLICY.md](CODE_SIGNING_POLICY.md).

## Crédits photos

Les photos d'aperçu viennent de Wikimedia Commons (domaine public, CC0 ou CC BY). Voir [photos/CREDITS.txt](photos/CREDITS.txt).

## Licence

[MIT](LICENSE)
