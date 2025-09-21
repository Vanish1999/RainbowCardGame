using UnityEngine;

[CreateAssetMenu(menuName = "RCG/CardData")]
public class CardData : ScriptableObject
{
    [Header("Identity")]
    public string cardId;          // 如 "T1_Red_01"
    public string displayName;     // 显示名：Red / Blue / ...

    [Header("Rules")]
    public CardTier tier;          // T1/T2/T3_Black/T4_White
    public CardColor color;        // Red/Yellow/...
    public int value;              // T1=1, T2=2, Black=4, White=5

    [Header("Art (optional)")]
    public Sprite artwork;         // 先留空，后面再换图
}

