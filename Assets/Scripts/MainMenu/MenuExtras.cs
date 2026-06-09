using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Накладывает в главном меню панель лучших результатов и номер версии.
/// Чисто рантайм — не требует связывания в сцене (как [Audio]-бутстрап).
/// </summary>
public static class MenuExtras
{
    private const string MenuScene = "MainMenu";
    private const string RootName  = "[MenuExtras]";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (SceneManager.GetActiveScene().name == MenuScene) Build();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == MenuScene) Build();
    }

    private static void Build()
    {
        if (GameObject.Find(RootName) != null) return;

        var root = new GameObject(RootName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        BuildRecords(root.transform);
        BuildVersion(root.transform);
    }

    private static void BuildRecords(Transform parent)
    {
        var panel = CreateText("BestRun", parent,
            anchor: new Vector2(0f, 0f), pivot: new Vector2(0f, 0f),
            pos: new Vector2(28f, 28f), size: new Vector2(560f, 150f));
        panel.alignment = TextAlignmentOptions.BottomLeft;
        panel.fontSize = 26f;

        if (RunRecords.BestScore <= 0)
        {
            panel.text = "<size=120%><b>ЛУЧШИЙ ЗАБЕГ</b></size>\n<alpha=#AA>Рекордов пока нет — начни первый рейд.";
            CyberpunkUI.StyleTMP(panel, new Color(0f, 0.85f, 1f), Color.black, 0.22f);
            return;
        }

        panel.text =
            "<size=120%><b>ЛУЧШИЙ ЗАБЕГ</b></size>\n" +
            $"<color=#FFD23B>Очки: {RunRecords.BestScore}</color>\n" +
            $"Время: {RunRecords.FormatTime(RunRecords.BestTime)}   " +
            $"Волна: {RunRecords.BestWave}   " +
            $"Убийств: {RunRecords.BestKills}   " +
            $"Ур.: {RunRecords.BestLevel}";

        CyberpunkUI.StyleTMP(panel, new Color(0f, 0.85f, 1f), Color.black, 0.22f);
    }

    private static void BuildVersion(Transform parent)
    {
        var ver = CreateText("Version", parent,
            anchor: new Vector2(1f, 0f), pivot: new Vector2(1f, 0f),
            pos: new Vector2(-16f, 14f), size: new Vector2(220f, 24f));
        ver.alignment = TextAlignmentOptions.BottomRight;
        ver.fontSize = 18f;
        ver.text = $"v{Application.version}";
        CyberpunkUI.StyleTMP(ver, new Color(0.7f, 0.75f, 0.85f), Color.black, 0.2f);
        ver.alpha = 0.7f;
    }

    private static TMP_Text CreateText(string objName, Transform parent, Vector2 anchor,
                                       Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(objName, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin        = anchor;
        rt.anchorMax        = anchor;
        rt.pivot            = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        var text = go.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        return text;
    }
}
