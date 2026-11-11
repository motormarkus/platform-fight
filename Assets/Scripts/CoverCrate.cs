using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Iso puulaatikko suojana (Resources/Rekka/laatikko.png): sen taakse (kauemmas kameraan nähden) mennyttä pelaajaa ei näy, koska laatikko piirtyy hänen edelleen.
/// Kontissa ja vankilan sisätiloissa (terminaali, käytävät) sijoitetaan näitä. Vartijoiden näkökenttä kysyy Hides(piste): onko piste jonkin suojan takana.
/// Sijoitus pohjan keskipisteeseen (jalat maassa); Create tekee laatikon koodista, tai lisää komponentin valmiiseen SpriteRenderer-olioon.
/// </summary>
public class CoverCrate : MonoBehaviour
{
    public static readonly List<CoverCrate> All = new List<CoverCrate>();
    [Tooltip("Laatikon korkeus yksikköinä (pelaaja ~2,2).")]
    public float height = 3.4f;
    [Tooltip("Suojan puolileveys: tämän sisällä (x) pelaaja on laatikon takana. Create laskee kuvasta.")]
    public float coverHalfWidth = 1.5f;
    [Tooltip("Kuinka paljon pohjan takana pelaajan pitää olla (y), jotta hän on suojassa.")]
    public float behindMargin = 0.3f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { All.Clear(); }

    void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }

    /// Onko piste (esim. pelaajan jalat) jonkin suojalaatikon takana.
    public static bool Hides(Vector3 p)
    {
        foreach (var c in All)
            if (c != null && Mathf.Abs(p.x - c.transform.position.x) <= c.coverHalfWidth && p.y >= c.transform.position.y + c.behindMargin) return true;
        return false;
    }

    /// Luo laatikon (paikka = pohjan keskipiste). Palauttaa null, jos Resources/Rekka/laatikko.png puuttuu.
    public static CoverCrate Create(Transform parent, Vector3 basePos, float height = 3.4f)
    {
        var tex = Resources.Load<Texture2D>("Rekka/laatikko");
        if (tex == null) return null;
        var go = new GameObject("Suojalaatikko");
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = basePos;
        var sr = go.AddComponent<SpriteRenderer>();
        float ppu = tex.height / height;
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), ppu);
        sr.sortingOrder = Mathf.RoundToInt(-basePos.y * 100f);
        var c = go.AddComponent<CoverCrate>();
        c.height = height;
        c.coverHalfWidth = Mathf.Max(0.5f, 0.5f * tex.width / ppu - 0.25f);
        return c;
    }
}
