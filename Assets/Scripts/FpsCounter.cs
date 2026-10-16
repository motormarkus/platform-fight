using UnityEngine;

/// <summary>
/// FPS-laskuri ja kehysaikakuvaaja (F3 tai Asetukset → Video): näyttää nopeuden, pahimman kehyksen ja viimeisen 3 sekunnin kehysajat pylväinä,
/// joten nykiminen näkyy heti (punainen pylväs = kehys yli 33 ms).
/// </summary>
public class FpsCounter : MonoBehaviour
{
    const int N = 180;
    readonly float[] ft = new float[N];
    int head, count;
    float avgFps, worstMs, shownAt;
    Texture2D white;
    GUIStyle style;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindFirstObjectByType<FpsCounter>() != null) return;
        var go = new GameObject("FpsCounter");
        DontDestroyOnLoad(go);
        go.AddComponent<FpsCounter>();
    }

    void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.f3Key.wasPressedThisFrame) GameSettings.ShowFps = !GameSettings.ShowFps;
        float dt = Time.unscaledDeltaTime;
        ft[head] = dt; head = (head + 1) % N; count = Mathf.Min(count + 1, N);
        if (Time.unscaledTime - shownAt > 0.25f)
        {
            shownAt = Time.unscaledTime;
            float sum = 0f, worst = 0f;
            int m = Mathf.Min(count, 60);   // viimeinen ~sekunti
            for (int i = 0; i < m; i++) { float v = ft[(head - 1 - i + N) % N]; sum += v; if (v > worst) worst = v; }
            avgFps = m > 0 && sum > 0f ? m / sum : 0f;
            worstMs = worst * 1000f;
        }
    }

    void OnGUI()
    {
        if (!GameSettings.ShowFps) return;
        if (white == null) { white = new Texture2D(1, 1); white.SetPixel(0, 0, Color.white); white.Apply(); }
        float u = Screen.height / 1080f;
        if (style == null) style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        style.fontSize = Mathf.Max(10, Mathf.RoundToInt(24 * u));
        GUI.depth = -2000;
        float w = 360 * u, h = 130 * u, x = Screen.width - w - 14 * u, y = 14 * u;
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(new Rect(x, y, w, h), white);
        GUI.color = Color.white;
        style.normal.textColor = avgFps >= 55f ? new Color(0.5f, 1f, 0.5f) : avgFps >= 30f ? new Color(1f, 0.9f, 0.3f) : new Color(1f, 0.4f, 0.4f);
        GUI.Label(new Rect(x + 8 * u, y + 2 * u, w, 32 * u), "FPS " + Mathf.RoundToInt(avgFps) + "   " + (avgFps > 0f ? (1000f / avgFps).ToString("0.0") : "-") + " ms   pahin " + worstMs.ToString("0") + " ms", style);
        // kehysaikapylväät (korkeus 0–50 ms), vanhimmasta uusimpaan
        float bw = (w - 16 * u) / N, gh = h - 40 * u, gy = y + h - 6 * u;
        for (int i = 0; i < count; i++)
        {
            float v = ft[(head - count + i + N) % N] * 1000f;
            float bh = Mathf.Clamp(v / 50f, 0.02f, 1f) * gh;
            GUI.color = v <= 17.5f ? new Color(0.4f, 0.9f, 0.4f) : v <= 33.4f ? new Color(1f, 0.85f, 0.25f) : new Color(1f, 0.3f, 0.3f);
            GUI.DrawTexture(new Rect(x + 8 * u + i * bw, gy - bh, Mathf.Max(1f, bw), bh), white);
        }
        GUI.color = Color.white;
    }
}
