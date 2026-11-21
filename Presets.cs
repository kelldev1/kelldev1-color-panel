namespace ColorPanel;

public record Preset(string Category, string Name, string Desc, Settings S);

public static class PresetLibrary
{
    // b = luminosité, c = contraste, s = saturation, h = teinte, r/g/bl = gains RGB
    static Preset P(string cat, string name, string desc,
        double b = 0, double c = 1, double s = 1, double h = 0, double r = 1, double g = 1, double bl = 1)
        => new(cat, name, desc, new Settings
        { Brightness = b, Contrast = c, Saturation = s, Hue = h, Red = r, Green = g, Blue = bl });

    public static readonly Preset[] All =
    {
        // ---------- Essentiels ----------
        P("Essentiels", "Normal", "Aucune modification"),
        P("Essentiels", "Vibrant", "Couleurs éclatantes", c: 1.1, s: 1.5),
        P("Essentiels", "Cinéma", "Contraste doux, teinte chaude", b: -0.03, c: 1.2, s: 0.85, r: 1.05, bl: 0.95),
        P("Essentiels", "Noir & Blanc", "Désature tout", c: 1.15, s: 0),
        P("Essentiels", "Nuit chaude", "Réduit la lumière bleue", b: -0.05, g: 0.92, bl: 0.72),
        P("Essentiels", "FPS compétitif", "Plus de contraste et de clarté", b: 0.06, c: 1.25, s: 1.3),
        P("Essentiels", "Froid", "Ambiance bleutée", r: 0.9, bl: 1.12),
        P("Essentiels", "Rétro", "Saturation basse, ton sépia", c: 1.05, s: 0.6, r: 1.1, bl: 0.8),

        // ---------- Jeux compétitifs ----------
        P("Jeux compétitifs", "Valorant clair", "Couleurs nettes, agents bien visibles", b: 0.04, c: 1.2, s: 1.35),
        P("Jeux compétitifs", "CS2 visibilité", "Silhouettes plus lisibles", b: 0.05, c: 1.3, s: 1.2, g: 1.05),
        P("Jeux compétitifs", "Warzone net", "Image nette, ombres éclaircies", b: 0.03, c: 1.25, s: 1.15),
        P("Jeux compétitifs", "Apex vif", "Couleurs fortes des légendes", c: 1.15, s: 1.45),
        P("Jeux compétitifs", "Fortnite pop", "Palette joyeuse et saturée", b: 0.02, c: 1.1, s: 1.4),
        P("Jeux compétitifs", "Overwatch éclatant", "Héros très colorés", c: 1.15, s: 1.5),
        P("Jeux compétitifs", "Rainbow Six détail", "Détails dans les zones sombres", b: 0.07, c: 1.2, s: 1.1),
        P("Jeux compétitifs", "Anti-ombres", "Éclaircit fortement les coins sombres", b: 0.12, c: 1.1, s: 1.1),
        P("Jeux compétitifs", "Vision nocturne", "Teinte verte, gain de lumière", b: 0.1, c: 1.2, s: 0.6, r: 0.6, g: 1.4, bl: 0.6),
        P("Jeux compétitifs", "Ennemis en évidence", "Contraste et couleurs poussés", b: 0.03, c: 1.35, s: 1.6),
        P("Jeux compétitifs", "Aide deutéranopie", "Aide pour le rouge-vert (vert)", c: 1.05, s: 1.3, h: 25),
        P("Jeux compétitifs", "Aide protanopie", "Aide pour le rouge-vert (rouge)", c: 1.05, s: 1.3, h: -25),
        P("Jeux compétitifs", "Aide tritanopie", "Aide pour le bleu-jaune", c: 1.05, s: 1.25, h: 40, bl: 1.1),
        P("Jeux compétitifs", "Clarté maximale", "Lumineux, contrasté, saturé", b: 0.08, c: 1.4, s: 1.2),

        // ---------- Cinéma ----------
        P("Cinéma", "Teal & Orange", "Look blockbuster classique", c: 1.15, s: 1.1, h: -5, r: 1.08, bl: 0.92),
        P("Cinéma", "Blockbuster", "Fort contraste, couleurs riches", b: -0.02, c: 1.3, s: 1.2),
        P("Cinéma", "Film noir", "Noir et blanc très contrasté", b: -0.05, c: 1.4, s: 0),
        P("Cinéma", "Vintage 70s", "Tons chauds et fanés", c: 1.05, s: 0.7, r: 1.1, bl: 0.85),
        P("Cinéma", "Western poussiéreux", "Sable et poussière", c: 1.1, s: 0.75, r: 1.12, bl: 0.78),
        P("Cinéma", "Cyberpunk", "Néons magenta et cyan", c: 1.2, s: 1.5, h: -30, r: 1.1, bl: 1.15),
        P("Cinéma", "Matrix", "Dominante verte froide", c: 1.2, s: 0.9, r: 0.8, g: 1.2, bl: 0.85),
        P("Cinéma", "Blade Runner", "Ambre sombre et dense", b: -0.04, c: 1.2, s: 1.2, r: 1.12, g: 0.95, bl: 0.9),
        P("Cinéma", "Thriller froid", "Bleu acier désaturé", c: 1.2, s: 0.8, r: 0.92, g: 0.98, bl: 1.1),
        P("Cinéma", "Horreur", "Sombre, pâle, inquiétant", b: -0.08, c: 1.35, s: 0.5, r: 1.05, g: 0.9, bl: 0.9),
        P("Cinéma", "Drame", "Sobre et contrasté", c: 1.1, s: 0.8),
        P("Cinéma", "Science-fiction", "Froid et lumineux", c: 1.15, r: 0.9, bl: 1.1),
        P("Cinéma", "Fantasy", "Couleurs féeriques", c: 1.05, s: 1.3, g: 1.05),
        P("Cinéma", "Documentaire", "Naturel et légèrement doux", c: 1.05, s: 0.9),
        P("Cinéma", "Technicolor", "Couleurs ultra-saturées d'époque", c: 1.15, s: 1.7),

        // ---------- Nature ----------
        P("Nature", "Coucher de soleil", "Chaud, orangé", c: 1.05, s: 1.2, r: 1.15, bl: 0.8),
        P("Nature", "Aube dorée", "Lumière dorée douce", b: 0.03, r: 1.1, g: 1.05, bl: 0.85),
        P("Nature", "Forêt", "Verts profonds", s: 1.25, r: 0.92, g: 1.15),
        P("Nature", "Océan", "Bleus et turquoises", s: 1.2, r: 0.85, g: 1.05, bl: 1.15),
        P("Nature", "Désert", "Sable chaud", s: 1.1, r: 1.12, g: 1.04, bl: 0.82),
        P("Nature", "Neige", "Blanc lumineux, froid", b: 0.06, c: 1.05, s: 0.85, bl: 1.05),
        P("Nature", "Brume", "Voile doux et pâle", b: 0.06, c: 0.8, s: 0.7),
        P("Nature", "Automne", "Roux et cuivres", s: 1.3, r: 1.15, bl: 0.75),
        P("Nature", "Printemps", "Frais et vert tendre", b: 0.04, s: 1.3, g: 1.1),
        P("Nature", "Orage", "Gris bleuté dramatique", b: -0.06, c: 1.3, s: 0.6, bl: 1.05),
        P("Nature", "Tropical", "Couleurs vives et chaudes", s: 1.6, g: 1.1, bl: 1.05),
        P("Nature", "Lueur de bougie", "Éclairage orange tamisé", b: -0.03, r: 1.2, g: 0.95, bl: 0.6),

        // ---------- Confort des yeux ----------
        P("Confort des yeux", "Mode lecture", "Moins de bleu, doux", b: -0.02, s: 0.8, bl: 0.85),
        P("Confort des yeux", "Nuit profonde", "Très sombre et chaud", b: -0.12, g: 0.85, bl: 0.6),
        P("Confort des yeux", "Anti-fatigue", "Réduit légèrement le bleu", b: -0.04, bl: 0.88),
        P("Confort des yeux", "Soirée", "Teinte chaude légère", g: 0.94, bl: 0.8),
        P("Confort des yeux", "Sépia doux", "Ton sépia discret", s: 0.4, r: 1.15, bl: 0.75),
        P("Confort des yeux", "Faible luminosité", "Écran plus sombre", b: -0.15),
        P("Confort des yeux", "Éco yeux", "Contraste et saturation adoucis", b: -0.05, c: 0.9, s: 0.85),
        P("Confort des yeux", "Papier", "Rendu proche d'une page imprimée", c: 0.95, s: 0.6, r: 1.05, g: 1.02, bl: 0.8),
        P("Confort des yeux", "Jour clair", "Lumineux pour pièce éclairée", b: 0.05, c: 1.05),
        P("Confort des yeux", "Contraste doux", "Image plus douce", c: 0.9, s: 0.9),
        P("Confort des yeux", "Soir doux", "Chaud et peu lumineux", b: -0.07, s: 0.9, g: 0.93, bl: 0.7),
        P("Confort des yeux", "Lumière du jour", "Neutre et clair", b: 0.03, c: 1.02, s: 1.05),

        // ---------- Teintes ----------
        P("Teintes", "Teinte +30°", "Décale les couleurs de 30°", h: 30),
        P("Teintes", "Teinte +60°", "Décale les couleurs de 60°", h: 60),
        P("Teintes", "Teinte +90°", "Décale les couleurs de 90°", h: 90),
        P("Teintes", "Teinte +120°", "Décale les couleurs de 120°", h: 120),
        P("Teintes", "Teinte +180°", "Couleurs inversées (complémentaires)", h: 180),
        P("Teintes", "Teinte -30°", "Décale les couleurs de -30°", h: -30),
        P("Teintes", "Teinte -60°", "Décale les couleurs de -60°", h: -60),
        P("Teintes", "Teinte -120°", "Décale les couleurs de -120°", h: -120),
        P("Teintes", "Monochrome bleu", "Tout en nuances de bleu", s: 0, r: 0.7, g: 0.9, bl: 1.4),
        P("Teintes", "Monochrome vert", "Tout en nuances de vert", s: 0, r: 0.7, g: 1.3, bl: 0.7),
        P("Teintes", "Monochrome rouge", "Tout en nuances de rouge", s: 0, r: 1.4, g: 0.7, bl: 0.7),
        P("Teintes", "Monochrome ambre", "Écran ambre type terminal", s: 0, r: 1.35, g: 1.0, bl: 0.5),
        P("Teintes", "Monochrome violet", "Tout en nuances de violet", s: 0, r: 1.2, g: 0.7, bl: 1.4),
        P("Teintes", "Monochrome cyan", "Tout en nuances de cyan", s: 0, r: 0.6, g: 1.2, bl: 1.3),
        P("Teintes", "Monochrome rose", "Tout en nuances de rose", s: 0, r: 1.4, g: 0.8, bl: 1.1),
        P("Teintes", "Monochrome jaune", "Tout en nuances de jaune", s: 0, r: 1.3, g: 1.2, bl: 0.5),

        // ---------- Styles visuels ----------
        P("Styles visuels", "HDR simulé", "Effet HDR contrasté", b: -0.02, c: 1.3, s: 1.4),
        P("Styles visuels", "Pastel", "Couleurs douces et claires", b: 0.08, c: 0.8, s: 0.7),
        P("Styles visuels", "Néon", "Saturation extrême", b: -0.03, c: 1.3, s: 2.2),
        P("Styles visuels", "Délavé", "Look passé et pâle", b: 0.08, c: 0.75, s: 0.5),
        P("Styles visuels", "Cartoon", "Contraste et couleurs de dessin animé", c: 1.4, s: 1.9),
        P("Styles visuels", "Haute définition", "Image plus tranchée", c: 1.2, s: 1.2),
        P("Styles visuels", "Dramatique", "Fort contraste, ombres profondes", b: -0.06, c: 1.5, s: 0.9),
        P("Styles visuels", "Clair et aéré", "Lumineux et léger", b: 0.1, c: 0.9, s: 1.1),
        P("Styles visuels", "Sombre et moody", "Ambiance sombre", b: -0.1, c: 1.3, s: 0.85),
        P("Styles visuels", "Plat (log)", "Image à plat pour un rendu neutre", b: 0.05, c: 0.7, s: 0.8),
        P("Styles visuels", "Ultra contraste", "Noirs profonds, blancs forts", c: 1.8, s: 1.1),
        P("Styles visuels", "Saturation extrême", "Couleurs à fond", c: 1.1, s: 2.8),
        P("Styles visuels", "Argentique", "Grain de film numérique doux", c: 1.1, s: 0.85, r: 1.04, g: 1.02, bl: 0.92),
        P("Styles visuels", "Polaroid", "Photo instantanée chaude", b: 0.05, c: 0.95, s: 0.9, r: 1.08, bl: 0.88),
        P("Styles visuels", "Lomo", "Contraste et vignettage de couleur", b: -0.03, c: 1.35, s: 1.5, r: 1.1, bl: 0.9),
        P("Styles visuels", "Instagram chaud", "Filtre chaud populaire", b: 0.02, c: 1.1, s: 1.2, r: 1.08, bl: 0.9),
        P("Styles visuels", "Instagram froid", "Filtre froid populaire", b: 0.02, c: 1.1, s: 1.1, r: 0.92, bl: 1.08),
        P("Styles visuels", "Moody vert", "Sombre avec dominante verte", b: -0.06, c: 1.25, s: 0.9, r: 0.92, g: 1.08),

        // ---------- Ambiances de jeu ----------
        P("Ambiances de jeu", "Survie", "Désaturé, tendu", c: 1.2, s: 0.7, g: 1.05),
        P("Ambiances de jeu", "Monde ouvert", "Paysages riches", c: 1.1, s: 1.25),
        P("Ambiances de jeu", "Plateformer coloré", "Palette joyeuse", c: 1.1, s: 1.6),
        P("Ambiances de jeu", "Simulation de vol", "Ciel et horizon nets", b: 0.04, c: 1.1, s: 1.1, bl: 1.05),
        P("Ambiances de jeu", "Course nocturne", "Néons et asphalte", b: -0.04, c: 1.3, s: 1.5, bl: 1.1),
        P("Ambiances de jeu", "MOBA", "Sorts bien visibles", c: 1.15, s: 1.3),
        P("Ambiances de jeu", "Stratégie", "Lisibilité de la carte", b: 0.03, c: 1.1, s: 1.1),
        P("Ambiances de jeu", "8-bit", "Couleurs franches rétro", c: 1.4, s: 1.8),
        P("Ambiances de jeu", "Game Boy", "Écran vert d'origine", s: 0, r: 0.6, g: 1.15, bl: 0.5),
        P("Ambiances de jeu", "Nintendo vif", "Couleurs gaies", b: 0.03, s: 1.5),
        P("Ambiances de jeu", "Post-apocalypse", "Poussière et rouille", c: 1.25, s: 0.55, r: 1.1, bl: 0.8),
        P("Ambiances de jeu", "Médiéval", "Tons terre et bois", c: 1.1, s: 0.9, r: 1.08, bl: 0.85),
        P("Ambiances de jeu", "Sci-fi spatial", "Froid, profond et net", b: -0.02, c: 1.25, s: 1.1, r: 0.9, bl: 1.12),
        P("Ambiances de jeu", "Horreur jeu", "Sombre et malsain", b: -0.1, c: 1.3, s: 0.6, g: 0.92, bl: 0.9),
    };
}
