using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kiinteä este lattialla (esim. biljardipöytä): suorakulmainen jalanjälki (x ja syvyys), jonka läpi pelaaja ja viholliset
/// eivät kävele. Kuva piirretään jalanjäljen keskisyvyyden mukaan: takana olevat jäävät pöydän taakse, edessä olevat eteen.
/// </summary>
public class Obstacle : MonoBehaviour
{
    public static readonly List<Obstacle> All = new List<Obstacle>();
    public SpriteRenderer body;
    [Tooltip("Pöydän päällä olevat (pallot, keppi): piirretään pöydän päälle.")]
    public SpriteRenderer[] onTop;
    [Tooltip("Jalanjäljen puolileveys (yks) ja syvyys etureunasta (transform = etureunan keskikohta).")]
    public float halfWidth = 2.9f, depth = 0.75f;

    [Header("Hajoaminen (vapaaehtoinen): potkut ja lyönnit hajottavat vähitellen")]
    [Tooltip("Vaiheet: 0 ehjä … viimeinen rikki (sama koko ja keskipiste kuin body.sprite).")]
    public Sprite[] damageSprites;
    public int maxHealth = 0;
    public AudioClip[] hitSounds;
    int health = -1;
    public bool Broken { get; private set; }
    public bool CanBeHit => damageSprites != null && damageSprites.Length > 1 && maxHealth > 0 && !Broken;

    /// Lähin kohta jalanjäljestä (iskun ulottuvuuden tarkistukseen).
    public Vector3 NearestPoint(Vector3 p)
    {
        Vector3 o = transform.position;
        return new Vector3(Mathf.Clamp(p.x, o.x - halfWidth, o.x + halfWidth), Mathf.Clamp(p.y, o.y, o.y + depth), 0f);
    }

    /// Isku osui: kestävyys laskee, kuva vaihtuu vaiheittain; lopuksi hajoaa ja päällä olevat (pallot, keppi) valahtavat lattialle.
    public bool TakeHit(int damage)
    {
        if (!CanBeHit) return false;
        if (health < 0) health = maxHealth;
        health -= Mathf.Max(1, damage);
        Rattle(1f);
        int n = damageSprites.Length;
        int stage = health <= 0 ? n - 1 : Mathf.Clamp(Mathf.FloorToInt((1f - health / (float)maxHealth) * (n - 1)), 0, n - 2);
        if (body != null && damageSprites[stage] != null) body.sprite = damageSprites[stage];
        // halkeama keskellä: keskimmäiset pallot valuvat lattialle, alue laajenee vaihe vaiheelta
        if (stage >= 2) DropMiddle(stage >= n - 1 ? 1f : 0.15f + (stage - 2) * 0.2f);
        if (hitSounds != null && hitSounds.Length > 0) HitFx.PlayClip(hitSounds[Random.Range(0, hitSounds.Length)], 0.9f);
        CameraFollow.Shake(health <= 0 ? 0.25f : 0.06f, health <= 0 ? 0.25f : 0.1f);
        if (health <= 0) Break();
        return true;
    }

    /// Pallot, jotka ovat pöydän keskikohdan lähellä (osuus puolileveydestä), valahtavat lattialle.
    void DropMiddle(float frac)
    {
        float cx = transform.position.x, lim = halfWidth * frac;
        foreach (var b in GetComponentsInChildren<Bottle>())
            if (b != null && Mathf.Abs(b.transform.position.x - cx) <= lim) b.FallOff();
    }

    /// Iso vihu lensi pöytään: hajoaa kerralla.
    public void Smash()
    {
        if (!CanBeHit) return;
        if (health < 0) health = maxHealth;
        TakeHit(health);
    }

    /// Ehjä, hajotettava este kohdassa p (jalanjäljen sisällä).
    public static Obstacle BreakableAt(Vector3 p)
    {
        for (int i = 0; i < All.Count; i++) { var o = All[i]; if (o != null && o.isActiveAndEnabled && o.CanBeHit && o.Inside(p)) return o; }
        return null;
    }

    void Break()
    {
        Broken = true;
        foreach (var b in GetComponentsInChildren<Bottle>()) if (b != null) b.FallOff();   // pallot lattialle
        foreach (var c in GetComponentsInChildren<Cue>()) if (c != null) c.FallOff();         // keppi lattialle
        DustPuff.Spawn(transform.position + new Vector3(0f, 0.3f, 0f), Mathf.RoundToInt(-transform.position.y * 100f) + 2, 2f);
    }

    // biljardipallojen kalina: pöytään osuu jotain tai pallot liikahtavat
    static AudioClip rattleClip;
    float nextRattle;
    int isPool = -1;
    bool IsPool
    {
        get
        {
            if (isPool < 0) isPool = GetComponentsInChildren<Bottle>(true).Length > 0 || name.Contains("Biljardi") ? 1 : 0;
            return isPool == 1;
        }
    }

    public void Rattle(float volume = 0.8f)
    {
        if (!IsPool || Time.time < nextRattle) return;
        if (rattleClip == null) rattleClip = Resources.Load<AudioClip>("Sfx/pool_balls");
        if (rattleClip == null) return;
        nextRattle = Time.time + 0.35f;
        HitFx.PlayClip(rattleClip, volume);
    }

    /// Ehjä este, jonka jalanjäljen sisällä p on (törmäyksen tunnistukseen).
    public static Obstacle BlockingAt(Vector3 p)
    {
        for (int i = 0; i < All.Count; i++) { var o = All[i]; if (o != null && o.isActiveAndEnabled && o.Inside(p)) return o; }
        return null;
    }

    /// Pallo nostettiin pöydältä: pöytä, jonka päällä se oli.
    public static void RattleNear(Vector3 p)
    {
        for (int i = 0; i < All.Count; i++)
        {
            var o = All[i]; if (o == null || !o.isActiveAndEnabled) continue;
            Vector3 q = o.NearestPoint(p);
            if (Mathf.Abs(q.x - p.x) < 0.5f && Mathf.Abs(q.y - p.y) < 1.2f) { o.Rattle(0.6f); return; }
        }
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void LateUpdate()
    {
        if (body == null) return;
        body.sortingOrder = Mathf.RoundToInt(-(transform.position.y + depth * 0.5f) * 100f);
        if (onTop != null) for (int i = 0; i < onTop.Length; i++) if (onTop[i] != null) onTop[i].sortingOrder = body.sortingOrder + 1;
    }

    bool Inside(Vector3 p)
    {
        if (Broken) return false;   // hajonneen pöydän kappaleiden yli pääsee
        Vector3 o = transform.position;
        return p.x > o.x - halfWidth && p.x < o.x + halfWidth && p.y > o.y && p.y < o.y + depth;
    }

    /// Uusi paikka p (vanha prev): jos se on esteen sisällä, liike perutaan akseleittain (liukuu reunaa pitkin).
    public static Vector3 Resolve(Vector3 prev, Vector3 p)
    {
        for (int i = 0; i < All.Count; i++)
        {
            var o = All[i];
            if (o == null || !o.isActiveAndEnabled || !o.Inside(p)) continue;
            if (o.Inside(prev)) continue;   // oli jo sisällä (esim. syntyi siihen): ei jumiuteta
            var px = new Vector3(p.x, prev.y, p.z);
            var py = new Vector3(prev.x, p.y, p.z);
            if (!o.Inside(px)) p = px;
            else if (!o.Inside(py)) p = py;
            else p = prev;
        }
        return p;
    }
}
