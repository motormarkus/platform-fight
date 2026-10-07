using UnityEngine;

/// <summary>Kamera seuraa pelaajaa sivusuunnassa pehmeästi. Korkeus pysyy paikallaan, ellei alue salli pystyseurantaa. Osaa myös tärähtää.</summary>
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    public Transform target;
    public float smoothTime = 0.15f;
    public float minX = -100f;
    public float maxX = 100f;

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
        cam.orthographicSize = size > 0f ? size : defaultSize;
    }

    /// Ruudun tärähdys (amp = voimakkuus yksiköinä, dur = kesto sekunteina).
    public static void Shake(float amp, float dur)
    {
        if (Instance == null) return;
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

    void LateUpdate()
    {
        if (target != null)
        {
            float goal = ClampX(target.position.x);
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
        Vector3 pos = basePos + offset;
        if (target != null) pos.x = ClampX(pos.x);   // tärähdyskään ei vie kuvan päädyn yli
        transform.position = pos;
    }
}
