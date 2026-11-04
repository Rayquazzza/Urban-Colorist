
/// <summary>
/// Les 8 combinaisons exactes du système Zoeti (pas le poker classique) :
/// chacune a un nombre de cartes attendu précis, pas une évaluation sur
/// une main fixe de 5 cartes. Ordre = force croissante (utile pour
/// ComboIsAtLeastConditionData qui compare via (int)combo).
/// </summary>
public enum PokerComboType
{
    Invalid = -1,      // la sélection ne correspond à aucune combo reconnue
    HighCard = 0,      // Carte seule - 1 carte, n'importe laquelle
    Pair = 1,          // Paire - 2 cartes de même valeur
    TwoPair = 2,       // Double paire - 4 cartes formant deux paires
    MiniFlush = 3,     // Mini-couleur - 3 cartes de la même enseigne
    ThreeOfAKind = 4,  // Brelan - 3 cartes de même valeur
    Straight = 5,      // Quinte - 5 cartes consécutives
    FullHouse = 6,     // Full - un brelan + une paire (5 cartes)
    StraightFlush = 7  // Quinte flush - 5 cartes consécutives, même enseigne
}
