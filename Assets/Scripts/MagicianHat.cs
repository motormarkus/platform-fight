using UnityEngine;

/// <summary>
/// Taikurin silinteri: kaatuessa hattu ponnahtaa päästä korkealle (näyttävä kaari ja pyöriminen), tömähtää maahan ja jää lepäämään; ylösnousun aikana se lentää korkeaa kaarta
/// takaisin ja laskeutuu itsestään päähän juuri kun taikuri on noussut. Kaatuminen- ja ylösnousukuvat ovat ilman hattua (Tools/MagicianAssets/make_kaatuminen.py).
/// Hattu on oma spriteolio: kaatumisen alussa se lähtee taikurin pään kohdalta, ja ylösnousun lopussa piilotetaan (idle-kuvassa hattu on päässä).
/// Asetukset: hatSprite, headHeight (pään korkeus maasta, yksikköä), maailmayksikkö per kuvan 1 px (0,75 / 108 = 0,00694).
/// </summary>
[RequireComponent(typeof(Enemy))]
public sealed class MagicianHat : MonoBehaviour
{
    public Sprite hatSprite;
    public float headHeight = 3.1f, apexRise = 2.3f, flightTime = 0.95f, restDistance = 3.4f, returnPeak = 2.2f, spinTurns = 2.5f;
    Enemy e; SpriteRenderer hat;
    enum Mode { Off, Fly, Rest, Return }
    Mode mode = Mode.Off; float t; Vector3 anchor, restPos; float dir = 1f, restAngle; bool prevAir, prevGetUp, prevDown;

    void Awake() { e = GetComponent<Enemy>(); }
    void OnDisable() { Hide(); }
    void Hide() { mode = Mode.Off; if (hat != null) hat.enabled = false; }

    void EnsureHat()
    {
        if (hat != null) return;
        var go = new GameObject("Taikurin hattu"); hat = go.AddComponent<SpriteRenderer>(); hat.sprite = hatSprite;
        go.transform.localScale = Vector3.one * Mathf.Max(0.01f, e.body != null ? e.body.transform.lossyScale.x : 1f);
    }

    void LateUpdate()
    {
        if (e == null || hatSprite == null) return;
        bool air = e.StateAirborne, down = e.IsDownNow, getUp = e.StateGetUp;
        bool lyingChain = air || down || getUp;
        if (!lyingChain && !e.IsDead && mode != Mode.Off && mode != Mode.Return) Hide();   // jokin muu tila (kuollut: hattu jää maahan): hattu pois, idle-kuvassa se on päässä
        // alkaa: kaatuminen
        if (air && !prevAir && !prevDown && !prevGetUp && mode == Mode.Off)
        {
            EnsureHat(); dir = e.FacingRight ? 1f : -1f;      // taikuri kaatuu selälleen: hattu lentää kasvojen suuntaa vastaan
            anchor = transform.position; t = 0f; mode = Mode.Fly; hat.enabled = true;
        }
        if (getUp && !prevGetUp && mode == Mode.Rest) { t = 0f; mode = Mode.Return; }
        else if (getUp && !prevGetUp && mode == Mode.Fly) { restPos = HatPos(1f); t = 0f; mode = Mode.Return; }
        if (mode == Mode.Off) { prevAir = air; prevDown = down; prevGetUp = getUp; return; }
        t += Time.deltaTime;
        Vector3 p; float ang; var body = e.body;
        switch (mode)
        {
            case Mode.Fly:
                {
                    float u = Mathf.Clamp01(t / flightTime); p = HatPos(u); ang = FlyAngle(u);
                    if (u >= 1f) { restPos = p; restAngle = ang; mode = Mode.Rest; }
                    break;
                }
            case Mode.Rest:
                p = restPos; ang = restAngle; break;
            default:
                {
                    float total = Mathf.Max(0.3f, e.getUpTime);
                    float u = Mathf.Clamp01(e.StateGetUp ? e.StateTimeNow / total : 1f);
                    Vector3 head = transform.position + new Vector3(0f, headHeight + 0.2f, 0f);
                    float k = u * u * (3f - 2f * u);
                    p = Vector3.Lerp(restPos, head, k) + new Vector3(0f, returnPeak * 4f * u * (1f - u), 0f);
                    ang = Mathf.Lerp(restAngle, 360f * 2f * dir, k);
                    if (u >= 1f && !e.StateGetUp) { Hide(); prevAir = air; prevDown = down; prevGetUp = getUp; return; }
                    break;
                }
        }
        hat.transform.position = p; hat.transform.rotation = Quaternion.Euler(0f, 0f, ang);
        hat.sortingOrder = (body != null ? body.sortingOrder : 0) + 3; hat.flipX = false;
        if (mode == Mode.Return && !e.StateGetUp && e.BrainIdleOrChase) Hide();   // nousu valmis
        prevAir = air; prevDown = down; prevGetUp = getUp;
    }

    // lentorata: nousu hidastuen (apexRise), lasku maahan kiihtyen, pieni pomppu; hajoaa x suuntaan -dir (selän taakse)
    Vector3 HatPos(float u)
    {
        float upT = 0.38f; float groundY = anchor.y;
        float h;
        if (u < upT) { float k = u / upT; h = headHeight + apexRise * (1f - (1f - k) * (1f - k)); }
        else { float k = (u - upT) / (1f - upT); h = Mathf.Lerp(headHeight + apexRise, 0.2f, k * k); }
        float x = anchor.x - dir * restDistance * u;
        return new Vector3(x, groundY + h, anchor.z);
    }
    float FlyAngle(float u) { return -dir * 360f * spinTurns * u; }
}
