public enum DuelState
{
    Idle,       // Main près de l'étui, en attente
    Feinting,   // En train de feinter (bloqué temporairement)
    Drawing,    // 1ère Touche : Viser / Dégainer
    Cocked,     // 2ème Touche : Le chien est armé
    Fired,      // 3ème Touche : Le coup est parti
    Dead        // Le joueur a perdu
}