using UnityEngine;

/// <summary>
/// Pysyvä musta pimennys ja siirtymien isäntä. Ovet ajavat siirtymänsä täällä, koska ovi voi olla scenessä, joka puretaan kesken siirtymän
/// (silloin ovi ja sen rutiini tuhoutuisivat ja ruutu jäisi mustaksi).
/// </summary>
public class ScreenFader : MonoBehaviour
{
    /// 0 = näkyvä, 1 = musta.
    public static float Alpha;
    static ScreenFader instance;
    static Texture2D black;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { instance = null; Alpha = 0f; }

    public static ScreenFader Host
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("ScreenFader");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<ScreenFader>();
            }
            return instance;
        }
    }

    void OnGUI()
    {
        if (Alpha <= 0f) return;
        if (black == null) { black = new Texture2D(1, 1); black.SetPixel(0, 0, Color.black); black.Apply(); }
        GUI.depth = -100;
        GUI.color = new Color(1f, 1f, 1f, Alpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), black);
        GUI.color = Color.white;
    }
}
