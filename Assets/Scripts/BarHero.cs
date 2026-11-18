using UnityEngine;

/// <summary>
/// Rantabaarin "toinen sankari": näkyy vain yksinpelissä ja on se sankari, jota pelaaja ei valinnut (Rocco <-> Ruby). Kaksinpelissä molemmat sankarit ovat jo mukana, joten tämä piiloutuu.
/// Seisoo ja hengittää (idle-sarja); koko kopioidaan pelaajan kuvasta.
/// </summary>
public sealed class BarHero : MonoBehaviour
{
    public Sprite[] roccoIdle, rubyIdle;
    public SpriteRenderer body;
    public float frameTime = 0.12f;
    Sprite[] seq; float t;

    void Start()
    {
        if (Players.Count >= 2 || body == null) { gameObject.SetActive(false); return; }
        seq = GameSettings.Character == 1 ? roccoIdle : rubyIdle;   // pelaaja on Ruby -> Rocco baarissa, ja päinvastoin
        if (seq == null || seq.Length == 0) { gameObject.SetActive(false); return; }
        var p = Players.Primary;
        if (p != null && p.BodyRenderer != null)
        {
            var s = p.BodyRenderer.transform.lossyScale; transform.localScale = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), 1f);
        }
        body.sprite = seq[0];
    }

    void Update()
    {
        if (seq == null || seq.Length == 0) return;
        t += Time.deltaTime;
        body.sprite = seq[(int)(t / frameTime) % seq.Length];
    }
}
