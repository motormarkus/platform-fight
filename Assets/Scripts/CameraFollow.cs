using UnityEngine;

/// <summary>Kamera seuraa pelaajaa sivusuunnassa pehmeästi. Korkeus pysyy paikallaan. Osaa myös tärähtää.</summary>
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

    void Awake()
    {
        Instance = this;
        basePos = transform.position;
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
        basePos.x = Mathf.Clamp(x, minX, maxX);
        velocity = 0f;
        transform.position = basePos;
    }

    void LateUpdate()
    {
        if (target != null)
        {
            float goal = Mathf.Clamp(target.position.x, minX, maxX);
            basePos.x = Mathf.SmoothDamp(basePos.x, goal, ref velocity, smoothTime);
        }

        Vector3 offset = Vector3.zero;
        if (shakeTime > 0f)
        {
            shakeTime -= Time.unscaledDeltaTime;   // tärisee myös iskun pysähdyksen aikana
            float k = Mathf.Clamp01(shakeTime / Mathf.Max(shakeDuration, 0.001f));
            offset = (Vector3)(Random.insideUnitCircle * shakeAmp * k);
        }
        transform.position = basePos + offset;
    }
}
