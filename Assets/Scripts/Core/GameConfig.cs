using UnityEngine;

public enum CardTier { T1 = 1, T2 = 2, T3_Black = 3, T4_White = 4 }
public enum CardColor { Red, Yellow, Blue, Orange, Purple, Green, Black, White }

[CreateAssetMenu(menuName = "RCG/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("Win Condition")]
    public int coinsToWin = 10;   // 先到10币获胜（源自设计文档）

    [Header("Deck Composition")]
    public int countWhite = 4, countBlack = 4;
    public int countRed = 8, countBlue = 8, countYellow = 8;
    public int countPurple = 6, countOrange = 6, countGreen = 6;

    [Header("Economy")]
    public int startingCoinsPerPlayer = 10; // 开局每人10币
}
