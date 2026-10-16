using UnityEngine;

/// <summary>Kirkkausasetus: koko ruudun päälle piirrettävä tumma tai vaalea kerros (5 = ei muutosta).</summary>
public class ScreenBrightness : MonoBehaviour
{
    Texture2D white;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindFirstObjectByType<ScreenBrightness>() != null) return;
        var go = new GameObject("ScreenBrightness");
        DontDestroyOnLoad(go);
        go.AddComponent<ScreenBrightness>();
    }

    void OnGUI()
    {
        int b = GameSettings.Brightness;
        if (b == 5) return;
        if (white == null) { white = new Texture2D(1, 1); white.SetPixel(0, 0, Color.white); white.Apply(); }
        GUI.depth = -1000;   // kaikkien päällä, valikotkin
        GUI.color = b < 5 ? new Color(0f, 0f, 0f, (5 - b) * 0.13f) : new Color(1f, 1f, 1f, (b - 5) * 0.07f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), white);
        GUI.color = Color.white;
    }
}
