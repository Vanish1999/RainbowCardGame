public static class RuleValidator
{
    // 是否允许 newCard 跟在 prev 后出（prev 可为 null）
    public static bool CanPlayAfter(CardData prev, CardData next)
    {
        if (next == null) return false;
        if (prev == null) return true; // 首张随意

        switch (prev.tier)
        {
            case CardTier.T1:         // 1 -> 2（且颜色必须能由T1组成）
                if (next.tier != CardTier.T2) return false;
                return IsValidT1ToT2(prev.color, next.color);

            case CardTier.T2:         // 2 -> 3(Black)
                return next.tier == CardTier.T3_Black;

            case CardTier.T3_Black:   // 3 -> 4(White)
                return next.tier == CardTier.T4_White;

            case CardTier.T4_White:   // 4 -> 1（任意 T1）
                return next.tier == CardTier.T1;

            default: return false;
        }
    }

    // T1 -> T2 颜色组成规则
    static bool IsValidT1ToT2(CardColor fromT1, CardColor toT2)
    {
        return (fromT1 == CardColor.Red   && (toT2 == CardColor.Orange || toT2 == CardColor.Purple)) ||
               (fromT1 == CardColor.Blue  && (toT2 == CardColor.Green  || toT2 == CardColor.Purple)) ||
               (fromT1 == CardColor.Yellow&& (toT2 == CardColor.Orange || toT2 == CardColor.Green));
    }
}