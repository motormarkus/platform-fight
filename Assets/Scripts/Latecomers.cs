using UnityEngine;

/// <summary>
/// Tappeluun ovesta myöhemmin tulevat (esim. portsarit): piilossa, kunnes Begin() kutsutaan; sitten yksi kerrallaan
/// sisään oven kohdalta ja kaikkien kimppuun.
/// </summary>
public class Latecomers : MonoBehaviour
{
    public Enemy[] enemies;
    [Tooltip("Mistä tullaan (oven edestä).")]
    public Vector2 entry;
    public float firstDelay = 1.5f, interval = 1.0f;
    [Tooltip("Tulijat tappelevat kaikkia vastaan (portsarit).")]
    public bool fightEveryone = true;

    float t = -1f;
    int next;

    void Start()
    {
        foreach (var e in enemies) if (e != null) e.gameObject.SetActive(false);
    }

    public void Begin()
    {
        if (t < 0f && next == 0) t = 0f;
    }

    void Update()
    {
        if (t < 0f || enemies == null || next >= enemies.Length) return;
        t += Time.deltaTime;
        if (t < firstDelay + next * interval) return;
        var e = enemies[next++];
        if (e == null) return;
        e.transform.position = new Vector3(entry.x, entry.y - 0.25f * (next % 2), 0f);
        if (fightEveryone) e.fightsEveryone = true;
        e.gameObject.SetActive(true);
        e.WakeUp();
    }
}
