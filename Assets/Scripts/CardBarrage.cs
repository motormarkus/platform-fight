using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Taikurin korttisuihku: kun sauva ojentuu iskussa, sauvan kärjestä lentää valtava määrä pelikortteja suunnan dir mukaan hirmuvauhdilla (viuhka lattian eri syvyyksille).
/// Kortit pyörivät, jättävät haamujäljen ja osuvat jokaiseen matkallaan olevaan heroon (ja halutessa muihin vihuihin): läpäisevät, mutta kukin kohde saa vain tietyn määrän osumia.
/// Kuvat: pelin omat pokerikortit (Resources/Poker/DefaultPoker -> cardFaces, 52 kuvaa). Käyttö taikurin aivosta: CardBarrage.Cast(enemy, sauvan kärki, dir).
/// </summary>
public sealed class CardBarrage : MonoBehaviour
{
    [System.Serializable]
    public class Config
    {
        public int cards = 18;
        public float burstTime = 0.3f, speed = 46f, speedJitter = 8f, life = 0.6f, cardHeight = 0.5f, flyHeight = 1.45f;
        public float laneSpread = 1.6f, slope = 0.12f, slowShare = 0.35f, slowSpeed = 0.16f, slowSpread = 2.6f, slowLife = 2.8f;   // slowShare: osuus korteista, jotka lentävät hitaammin ja leviävät laajemmalle       // lattian syvyys (y) ± ja kaltevuus (y-nopeus / x-nopeus)
        public float spinMin = 700f, spinMax = 1500f;
        public int damage = 6, maxHitsPerTarget = 3; public float hitGap = 0.07f;
        public float hitRangeX = 0.8f, hitRangeY = 0.7f;
        public bool hitHeroes = true, hitEnemies = true;     // muut vihut (eivät taikuri itse) saavat osumia, jos hitEnemies
        public AudioClip castSound, hitSound; public float volume = 1f;
    }

    class Card { public Transform t; public SpriteRenderer sr, g1, g2; public Vector3 pos, vel, p1, p2; public float spin, age, spawnAt, life; public bool live; public int hits; public readonly HashSet<Object> hitSet = new HashSet<Object>(); }
    Enemy owner; Config c; float time; readonly List<Card> cards = new List<Card>(); readonly Dictionary<Object, int> perTarget = new Dictionary<Object, int>(); readonly Dictionary<Object, float> lastHit = new Dictionary<Object, float>();
    static Sprite[] faces;
    static Sprite[] Faces()
    {
        if (faces != null) return faces;
        var s = Resources.Load<PokerPresentationSettings>("Poker/DefaultPoker");
        var l = new List<Sprite>();
        if (s != null && s.cardFaces != null) foreach (var f in s.cardFaces) if (f != null) l.Add(f);
        faces = l.ToArray(); return faces;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics() { faces = null; }

    public static CardBarrage Cast(Enemy owner, Vector3 from, float dir, Config config = null)
    {
        var fs = Faces(); if (fs.Length == 0) return null;
        var c = config ?? new Config();
        var go = new GameObject("Korttisuihku"); var b = go.AddComponent<CardBarrage>();
        b.owner = owner; b.c = c;
        float sign = dir >= 0f ? 1f : -1f; var area = Area.Current;
        float lo = from.y - c.laneSpread, hi = from.y + c.laneSpread;
        if (area != null && area.maxDepthY > area.minDepthY) { lo = Mathf.Max(area.minDepthY + 0.2f, lo); hi = Mathf.Min(area.maxDepthY - 0.2f, hi); }
        for (int i = 0; i < c.cards; i++)
        {
            var sp = fs[Random.Range(0, fs.Length)];
            var g = new GameObject("Kortti"); g.transform.SetParent(go.transform, false);
            var sr = g.AddComponent<SpriteRenderer>(); sr.sprite = sp; float sc = c.cardHeight / Mathf.Max(0.01f, sp.bounds.size.y); g.transform.localScale = new Vector3(sc, sc, 1f);
            var g1 = NewGhost(g.transform, sp, .38f, sc); var g2 = NewGhost(g.transform, sp, .18f, sc);
            float lane = Mathf.Lerp(lo, hi, c.cards == 1 ? 0.5f : (i * 0.618f % 1f));                       // jakautuu tasaisesti syvyyksille (kultainen leikkaus)
            float spd = c.speed + Random.Range(-c.speedJitter, c.speedJitter), life = c.life, vy = (lane - from.y) * c.slope * spd / Mathf.Max(1f, hi - lo) * 0.5f;
            if (c.slowShare > 0f && (i * 0.382f % 1f) < c.slowShare)                                        // hidas, laajalle levittyvä kortti
            {
                spd *= Random.Range(c.slowSpeed * 0.8f, c.slowSpeed * 1.25f); life = c.life * c.slowLife;
                float wlo = from.y - c.laneSpread * c.slowSpread, whi = from.y + c.laneSpread * c.slowSpread;
                if (area != null && area.maxDepthY > area.minDepthY) { wlo = Mathf.Max(area.minDepthY + 0.2f, wlo); whi = Mathf.Min(area.maxDepthY - 0.2f, whi); }
                vy = (Random.Range(wlo, whi) - from.y) / (life * 0.85f);
            }
            var card = new Card { t = g.transform, sr = sr, g1 = g1, g2 = g2, pos = new Vector3(from.x, from.y, 0f), vel = new Vector3(sign * spd, vy, 0f), life = life, spin = Random.Range(c.spinMin, c.spinMax) * (Random.value < .5f ? -1f : 1f) * 0.7f, spawnAt = i * (c.burstTime / Mathf.Max(1, c.cards)) };
            card.p1 = card.p2 = card.pos; g.SetActive(false); b.cards.Add(card);
        }
        HitFx.PlayWhoosh();
        if (c.castSound != null) AudioSource.PlayClipAtPoint(c.castSound, Camera.main != null ? Camera.main.transform.position : from, c.volume * GameSettings.SfxVolume);
        return b;
    }
    static SpriteRenderer NewGhost(Transform parent, Sprite sp, float a, float sc)
    {
        var g = new GameObject("Haamu"); g.transform.SetParent(parent, false);
        var sr = g.AddComponent<SpriteRenderer>(); sr.sprite = sp; sr.color = new Color(1f, 1f, 1f, a); g.transform.localScale = Vector3.one; return sr;
    }

    void Update()
    {
        float dt = Time.deltaTime; time += dt; bool any = false;
        foreach (var k in cards)
        {
            if (!k.live)
            {
                if (time < k.spawnAt) { any = true; continue; }
                k.live = true; k.age = 0f; k.t.gameObject.SetActive(true);
            }
            if (k.age >= k.life || k.t == null) { if (k.t != null && k.t.gameObject.activeSelf) k.t.gameObject.SetActive(false); continue; }
            any = true; k.age += dt;
            k.p2 = k.p1; k.p1 = k.pos; k.pos += k.vel * dt;
            float wob = Mathf.Sin(k.age * 38f + k.spawnAt * 20f) * 0.06f;
            // kortti lentää rinnan korkeudella: näkyvä y = maanpinnan y + lentokorkeus
            k.t.position = new Vector3(k.pos.x, k.pos.y + c.flyHeight + wob, 0f); k.t.Rotate(0f, 0f, k.spin * dt);
            k.g1.transform.position = new Vector3(k.p1.x, k.p1.y + c.flyHeight + wob, 0f); k.g2.transform.position = new Vector3(k.p2.x, k.p2.y + c.flyHeight + wob, 0f);
            k.g1.transform.rotation = k.t.rotation; k.g2.transform.rotation = k.t.rotation;
            int order = Mathf.RoundToInt(-k.pos.y * 100f) + 8; k.sr.sortingOrder = order; k.g1.sortingOrder = order - 1; k.g2.sortingOrder = order - 2;
            float fade = Mathf.Clamp01((k.life - k.age) / 0.15f); k.sr.color = new Color(1f, 1f, 1f, fade);
            Hits(k);
        }
        if (!any) Destroy(gameObject);
    }

    void Hits(Card k)
    {
        if (c.hitHeroes)
            foreach (var pl in Players.All)
            {
                if (pl == null || pl.GameOver || k.hitSet.Contains(pl)) continue;
                Vector3 p = pl.transform.position;
                if (Mathf.Abs(p.x - k.pos.x) > c.hitRangeX || Mathf.Abs(p.y - k.pos.y) > c.hitRangeY || pl.AirHeight > 2.2f) continue;
                if (!CanHit(pl)) continue;
                k.hitSet.Add(pl); Register(pl);
                if (pl.TakeHit(c.damage, k.pos.x - Mathf.Sign(k.vel.x) * 0.5f, owner)) Hit(k);
            }
        if (c.hitEnemies)
            foreach (var en in Enemy.All)
            {
                if (en == null || en == owner || !en.isActiveAndEnabled || en.IsDead || k.hitSet.Contains(en)) continue;
                Vector3 p = en.transform.position;
                if (Mathf.Abs(p.x - k.pos.x) > c.hitRangeX || Mathf.Abs(p.y - k.pos.y) > c.hitRangeY) continue;
                if (!CanHit(en)) continue;
                k.hitSet.Add(en); Register(en);
                if (Enemy.EnemyHits(en, c.damage, k.pos.x - Mathf.Sign(k.vel.x) * 0.5f, false)) Hit(k);
            }
    }
    bool CanHit(Object o) => (!perTarget.TryGetValue(o, out int n) || n < c.maxHitsPerTarget) && (!lastHit.TryGetValue(o, out float t) || Time.time - t >= c.hitGap);
    void Register(Object o) { perTarget[o] = (perTarget.TryGetValue(o, out int n) ? n : 0) + 1; lastHit[o] = Time.time; }
    void Hit(Card k)
    {
        if (c.hitSound != null) AudioSource.PlayClipAtPoint(c.hitSound, k.pos, c.volume * GameSettings.SfxVolume * 0.6f);
    }
}
