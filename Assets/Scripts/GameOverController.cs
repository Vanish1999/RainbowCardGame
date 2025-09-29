using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverController : MonoBehaviour
{
    [Header("Overlay UI")]
    [SerializeField] private CanvasGroup overlay;       // 半透明全屏 Panel（可拦截点击）
    [SerializeField] private TMP_Text   label;          // 显示 "YOU WIN / YOU LOSE"

    [Header("Behaviour")]
    [SerializeField] private float minShowSeconds = 0.6f; // 至少显示这么久再允许重开

    private bool showing;
    private float shownAt;
    private Action onRestart; // 可选：自定义重开逻辑（不设则默认重载场景）

    public enum Result { YouWin, YouLose }

    void Awake()
    {
        HideInternal();
    }

    void Update()
    {
        if (!showing) return;

        bool clickOrKey =
            Input.anyKeyDown ||
            Input.GetMouseButtonDown(0) ||
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);

        if (clickOrKey && Time.unscaledTime - shownAt >= minShowSeconds)
        {
            Restart();
        }
    }

    public void Show(Result result, Action customRestart = null)
    {
        onRestart = customRestart;

        label.text = (result == Result.YouWin) ? "YOU WIN" : "YOU LOSE";
        overlay.gameObject.SetActive(true);
        overlay.alpha = 1f;
        overlay.blocksRaycasts = true;
        Time.timeScale = 0f; // 暂停游戏（如不想暂停可去掉）

        showing = true;
        shownAt = Time.unscaledTime;
    }

    private void Restart()
    {
        // 先恢复时间
        Time.timeScale = 1f;
        showing = false;

        if (onRestart != null)
        {
            HideInternal();
            onRestart.Invoke();
        }
        else
        {
            // 默认：直接重载当前场景，最干净
            var idx = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(idx);
        }
    }

    private void HideInternal()
    {
        overlay.alpha = 0f;
        overlay.blocksRaycasts = false;
        overlay.gameObject.SetActive(false);
        label.text = "";
        showing = false;
    }
}

