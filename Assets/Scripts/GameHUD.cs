using UnityEngine;

/// <summary>Yksinkertainen energiapalkki pelaajalle ja viimeksi lyödylle viholliselle (kolikkopelityyliin).</summary>
public class GameHUD : MonoBehaviour
{
    public PlayerController player;
    public string playerName = "PELAAJA";
    [Tooltip("Kuinka kauan vihollisen palkki näkyy osuman jälkeen.")]
    public float enemyBarTime = 3f;

    Texture2D white;
    GUIStyle label;

    void OnGUI()
    {
        if (white == null) { white = new Texture2D(1, 1); white.SetPixel(0, 0, Color.white); white.Apply(); }
        if (label == null)
        {
            label = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height * 0.028f), fontStyle = FontStyle.Bold };
            label.normal.textColor = Color.white;
        }
        if (player == null) player = FindFirstObjectByType<PlayerController>();

        float s = Screen.height / 1080f;
        float x = 40 * s, y = 30 * s, w = 420 * s, h = 26 * s;

        if (player != null)
        {
            GUI.Label(new Rect(x, y, w, h * 1.4f), playerName, label);
            Bar(new Rect(x, y + h * 1.4f, w, h), player.health / (float)Mathf.Max(1, player.maxHealth), new Color(1f, 0.85f, 0.1f));
        }

        var e = Enemy.LastHit;
        if (e != null && Time.time - Enemy.LastHitTime < enemyBarTime)
        {
            float ey = y + h * 3.2f;
            GUI.Label(new Rect(x, ey, w, h * 1.4f), e.displayName.ToUpper(), label);
            Bar(new Rect(x, ey + h * 1.4f, w * 0.8f, h * 0.8f), e.Health / (float)Mathf.Max(1, e.maxHealth), new Color(0.9f, 0.2f, 0.15f));
        }
    }

    void Bar(Rect r, float t, Color c)
    {
        GUI.color = Color.black; GUI.DrawTexture(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), white);
        GUI.color = new Color(0.25f, 0.05f, 0.05f); GUI.DrawTexture(r, white);
        GUI.color = c; GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(t), r.height), white);
        GUI.color = Color.white;
    }
}
