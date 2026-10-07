using UnityEngine;

/// <summary>
/// Taustakuvan päälle piirretty avautuva ovi (esim. laivan hyttiovi): kuvat 0 = kiinni … viimeinen = auki.
/// Open() avaa, Close() sulkee; seilorit juoksevat ulos avoimesta ovesta (BouncerSquad.door).
/// </summary>
public class AnimatedDoor : MonoBehaviour
{
    public SpriteRenderer body;
    public Sprite[] frames;
    public float frameTime = 0.05f;
    public AudioClip openSound, closeSound;
    [Tooltip("Avautuminen tasaisella tahdilla ilman pehmennystä (videosta otettu sarja, jonka ääni on kuvien tahdissa).")]
    public bool linear;
    float pos;          // 0 = kiinni, 1 = auki
    int dir;            // 1 avautuu, -1 sulkeutuu

    public bool IsOpen => pos >= 1f && dir >= 0;

    [ContextMenu("Avaa")] void TestOpen() { Open(); }     // testaus Play-tilassa: Inspector ⋮ → Avaa / Sulje
    [ContextMenu("Sulje")] void TestClose() { Close(); }

    void Start() { Apply(); }

    public void Open()
    {
        if (dir == 1 || pos >= 1f) return;
        dir = 1;
        if (openSound != null) HitFx.PlayClip(openSound, 0.8f);
    }

    public void Close()
    {
        if (dir == -1 || pos <= 0f) return;
        dir = -1;
        if (closeSound != null) HitFx.PlayClip(closeSound, 0.8f);
    }

    void Update()
    {
        if (dir == 0 || frames == null || frames.Length < 2) return;
        // avautuu kiihtyen (lukko aukeaa, sitten ovi heilahtaa), sulkeutuu tasaisesti
        float total = frameTime * (frames.Length - 1);
        pos = Mathf.Clamp01(pos + dir * Time.deltaTime / total);
        if (pos <= 0f || pos >= 1f) dir = 0;
        Apply();
    }

    void Apply()
    {
        if (body == null || frames == null || frames.Length == 0) return;
        float k = dir >= 0 && !linear ? pos * pos * (3f - 2f * pos) : pos;
        body.sprite = frames[Mathf.Clamp(Mathf.RoundToInt(k * (frames.Length - 1)), 0, frames.Length - 1)];
    }
}
