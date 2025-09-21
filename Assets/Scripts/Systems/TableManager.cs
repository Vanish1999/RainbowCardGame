using UnityEngine;

public class TableManager : MonoBehaviour
{
    public CardInstance top; // 当前台面顶牌（null 表示本局还没人出）

    public void ClearTable() { top = null; }
}