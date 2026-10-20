using UnityEngine;

/// <summary>Kamera seuraa pelaajaa sivusuunnassa pehmeästi. Korkeus pysyy paikallaan, ellei alue salli pystyseurantaa. Osaa myös tärähtää.</summary>
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    public Transform target;
    public float smoothTime = 0.15f;
    public float minX = -100f;
    public float maxX = 100f;

    float lastTx, speedSm, lead;   // ajonopeuden etuosoitus
    float velocity;
    Vector3 basePos;
    float shakeAmp, shakeTime, shakeDuration;

    float defaultSize = -1f;
    float baseY, riseY, depthMin, depthMax, yVel, offY;

    /// Pystyseuranta (alueittain): pelaajan ollessa seinän vieressä kamera on riseY ylempänä, edessä normaalikorkeudella.
    public void SetVertical(float rise, float minDepth, float maxDepth, float offset = 0f)
    {
        riseY = rise; depthMin = minDepth; depthMax = maxDepth; offY = offset;
        basePos.y = GoalY(); yVel = 0f;   // aluevaihto pimennyksessä: heti oikealle korkeudelle
    }

    float GoalY()
    {
        if (riseY <= 0f || target == null || depthMax <= depthMin) return baseY + offY;
        return baseY + offY + riseY * Mathf.InverseLerp(depthMin, depthMax, target.position.y);
    }

    void Awake()
    {
        Instance = this;
        basePos = transform.position;
        baseY = basePos.y;
        var cam = GetComponent<Camera>();
        if (cam != null) defaultSize = cam.orthographicSize;
    }

    /// Kameran koko alueen mukaan (0 = oletus). Kameran korkeus pysyy, joten ruutu kasvaa ylös ja alas yhtä paljon.
    public void SetSize(float size)
    {
        var cam = GetComponent<Camera>();
        if (cam == null || defaultSize <= 0f) return;
        requestedSize = size > 0f ? size : defaultSize;
        cam.orthographicSize = requestedSize;
    }

    float requestedSize = -1f;   // alueen pyytämä koko (leveällä ruudulla kapea alue zoomataan tätä pienemmäksi)

    /// Leveä ruutu (esim. 21:9): jos alue on kapeampi kuin ruutu, kamera zoomaa sisään niin, ettei taustakuvan reunan yli näy.
    /// Ruudun alareuna pysyy paikallaan (lattia ja hahmojen jalat kuvassa), yläosasta rajautuu.
    float FitWidth(Camera c)
    {
        if (c == null || defaultSize <= 0f) return 0f;
        if (requestedSize <= 0f) requestedSize = c.orthographicSize;
        float designHalf = requestedSize * 16f / 9f;
        float width = (maxX - minX) + 2f * designHalf;   // alueen leveys (rajat on laskettu 16:9-ruudulle): 16:9-ruudulla ei zoomia
        float size = Mathf.Min(requestedSize, width / (2f * Mathf.Max(c.aspect, 0.1f)));
        c.orthographicSize = size;
        return requestedSize - size;   // näin paljon ruudun alareunaa nostetaan takaisin alas
    }

    /// Ruudun tärähdys (amp = voimakkuus yksiköinä, dur = kesto sekunteina).
    public static void Shake(float amp, float dur)
    {
        if (Instance == null || !GameSettings.ScreenShake) return;
        if (amp >= Instance.shakeAmp * (Instance.shakeTime / Mathf.Max(Instance.shakeDuration, 0.001f)))
        {
            Instance.shakeAmp = amp;
            Instance.shakeDuration = dur;
            Instance.shakeTime = dur;
        }
    }

    /// Siirtää kameran heti annettuun kohtaan (esim. oven jälkeen), ilman liukumista.
    public void SnapTo(float x)
    {
        basePos.x = ClampX(x);
        basePos.y = GoalY();
        velocity = 0f; yVel = 0f;
        transform.position = basePos;
    }

    /// Rajat on laskettu 16:9-kuvasuhteelle ja oletuskoolle; leveämmällä tai suuremmalla ruudulla rajoja kavennetaan, ettei kuvan reunan yli näy.
    float ClampX(float x)
    {
        float lo = minX, hi = maxX;
        var c = GetComponent<Camera>();
        float designHalf = (defaultSize > 0f ? defaultSize : (c != null ? c.orthographicSize : 0f)) * 16f / 9f;
        if (c != null && c.orthographicSize * c.aspect > designHalf + 0.01f)
        {
            // rajat on laskettu oletuskoolle ja 16:9-ruudulle: leveämpi tai suurempi ruutu kavennetaan, ettei kuvan päädyn taakse näy
            float extra = c.orthographicSize * c.aspect - designHalf;
            lo += extra; hi -= extra;
            if (lo > hi) lo = hi = (minX + maxX) * 0.5f;
        }
        return Mathf.Clamp(x, lo, hi);
    }

    /// Kaksinpeli: jälkeen jäävä pelaaja ei pääse ruudun ulkopuolelle (kamera seuraa keskikohtaa).
    void KeepPlayersOnScreen()
    {
        if (Players.Count < 2) return;
        var c = GetComponent<Camera>();
        if (c == null || !c.orthographic) return;
        float half = c.orthographicSize * c.aspect - 1.0f;
        float cx = transform.position.x;
        foreach (var p in Players.All)
        {
            if (p == null || p.GameOver) continue;
            var pos = p.transform.position;
            float nx = Mathf.Clamp(pos.x, cx - half, cx + half);
            if (!Mathf.Approximately(nx, pos.x)) p.transform.position = new Vector3(nx, pos.y, pos.z);
        }
    }

    void LateUpdate()
    {
        if (target != null)
        {
            float tx = target.position.x;
            if (Players.Count > 1 && Players.LivingBounds(out float lx, out float hx, out float ay)) tx = (lx + hx) * 0.5f;   // kaksinpeli: keskikohta
            // prätkällä ajaessa kamera menee edelle: pyörä ei karkaa ruudun oikeaan reunaan (vain kun hero ajaa prätkällä)
            float dtl = Time.deltaTime;
            if (dtl > 0.0001f)
            {
                float v = (tx - lastTx) / dtl;
                if (Mathf.Abs(v) > 40f) { speedSm = 0f; lead = 0f; }   // aluevaihto / teleportti
                else speedSm = Mathf.Lerp(speedSm, v, 1f - Mathf.Exp(-6f * dtl));
                var rider = target != null ? target.GetComponent<PlayerController>() : null;
                float wantLead = rider != null && rider.Riding ? Mathf.Clamp(speedSm * 0.3f, -4f, 4f) : 0f;   // vain prätkällä, nopeuden mukaan
                lead = Mathf.Lerp(lead, wantLead, 1f - Mathf.Exp(-3f * dtl));
            }
            lastTx = tx;
            float goal = ClampX(tx + lead);
            basePos.x = Mathf.SmoothDamp(basePos.x, goal, ref velocity, smoothTime);
            basePos.y = Mathf.SmoothDamp(basePos.y, GoalY(), ref yVel, 0.35f);
        }

        Vector3 offset = Vector3.zero;
        if (shakeTime > 0f)
        {
            shakeTime -= Time.unscaledDeltaTime;   // tärisee myös iskun pysähdyksen aikana
            float k = Mathf.Clamp01(shakeTime / Mathf.Max(shakeDuration, 0.001f));
            offset = (Vector3)(Random.insideUnitCircle * shakeAmp * k);
        }
        float zoomDrop = FitWidth(GetComponent<Camera>());
        Vector3 pos = basePos + offset + new Vector3(0f, -zoomDrop, 0f);
        if (target != null) pos.x = ClampX(pos.x);   // tärähdyskään ei vie kuvan päädyn yli
        transform.position = pos;
        KeepPlayersOnScreen();
    }
}
