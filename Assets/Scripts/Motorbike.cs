using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Kadun moottoripyörä. Pelaaja menee viereen ja painaa E (ohjaimessa ympyrä): nousuanimaatio, käynnistysääni
/// ja ajo. Ajossa vasen/oikea kiihdyttää ja jarruttaa, ylös/alas vaihtaa kaistaa. Vauhdissa pyörä kaataa
/// viholliset ja lennättää laatikot ja tynnyrit. E pysähtyneenä: pelaaja nousee pois ja pyörä jää siihen.
/// </summary>
public class Motorbike : MonoBehaviour
{
    [Tooltip("Pysäköity pyörä (kadun kuva).")]
    public SpriteRenderer parked;
    public Sprite parkedLeft, parkedRight;
    [Tooltip("Ajokuvat (pratka_ajo.png): kuski pyörän selässä, katse oikealle.")]
    public Sprite[] rideSprites;
    [Tooltip("Nousukuvat (pratka_nousu.png), samassa ruudussa kuin ajokuvat.")]
    public Sprite[] mountSprites;
    public AudioClip startSound;
    [Range(0f, 1f)] public float startVolume = 0.9f;

    [Header("Ajo")]
    public float maxSpeed = 11f;
    public float acceleration = 9f;
    public float braking = 18f;
    public float depthSpeed = 2.6f;
    public float mountFrameTime = 0.11f;
    [Header("Yliajo")]
    public int runOverDamage = 25;
    public float runOverMinSpeed = 3f;
    [Tooltip("Pyörän puolipituus (yksikköä): tämän matkan sisällä keskeltä osuu.")]
    public float halfLength = 2.6f;
    [Header("Käyttö")]
    public float useHalfWidth = 2.2f;
    public float useDepth = 0.8f;
    public string prompt = "Nouse pyörän selkään";

    PlayerController pc;
    AudioSource audioSrc;
    bool near, busy, riding, facingRight;
    float speed, animClock, groundHeight;
    readonly Dictionary<Object, float> lastHit = new Dictionary<Object, float>();
    static Motorbike active;

    void Awake()
    {
        PlayerController.SortByFrameNumber(rideSprites);
        PlayerController.SortByFrameNumber(mountSprites);
        audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.spatialBlend = 0f;
        if (parked != null)
        {
            bool spriteRight = parkedRight != null && parked.sprite == parkedRight;
            facingRight = spriteRight != parked.flipX;
        }
    }

    void Update()
    {
        if (pc == null) pc = FindFirstObjectByType<PlayerController>();
        if (pc == null) return;
        if (riding) { Ride(Time.deltaTime); return; }
        near = false;
        if (busy || active != null || !pc.enabled || !pc.IsFree) return;
        Vector3 p = pc.transform.position, me = transform.position;
        near = Mathf.Abs(p.x - me.x) <= useHalfWidth && Mathf.Abs(p.y - me.y) <= useDepth;
        if (near && UsePressed()) StartCoroutine(Mount());
    }

    static bool UsePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    float Dir => facingRight ? 1f : -1f;

    IEnumerator Mount()
    {
        busy = true; active = this; near = false;
        pc.Riding = true;
        pc.enabled = false;
        Vector3 me = transform.position;
        pc.transform.position = new Vector3(me.x, me.y, 0f);
        groundHeight = GroundAt(me.y);
        if (mountSprites != null)
            foreach (var s in mountSprites) { ShowRider(s); yield return new WaitForSeconds(mountFrameTime); }
        if (startSound != null) audioSrc.PlayOneShot(startSound, startVolume);
        if (parked != null) parked.enabled = false;
        speed = 0f; animClock = 0f;
        riding = true; busy = false;
    }

    IEnumerator Dismount()
    {
        busy = true; riding = false;
        Vector3 p = pc.transform.position;
        // pysäköity pyörä tähän, samaan suuntaan
        transform.position = new Vector3(p.x, p.y, 0f);
        if (parked != null)
        {
            if (parkedRight != null) { parked.sprite = facingRight ? parkedRight : parkedLeft; parked.flipX = false; }
            else parked.flipX = facingRight;
            parked.sortingOrder = Mathf.RoundToInt(-p.y * 100f);
            parked.enabled = true;
        }
        if (mountSprites != null)
            for (int i = mountSprites.Length - 1; i >= 0; i--) { ShowRider(mountSprites[i]); yield return new WaitForSeconds(mountFrameTime * 0.8f); }
        pc.Riding = false;
        pc.enabled = true;
        pc.TeleportTo(new Vector3(p.x - Dir * 1.4f, p.y, 0f));   // seisoo pyörän vieressä
        active = null; busy = false;
    }

    void ShowRider(Sprite s)
    {
        if (pc.body == null) return;
        pc.body.sprite = s;
        pc.body.flipX = !facingRight;
        pc.body.transform.localPosition = new Vector3(0f, groundHeight - 0.1f, 0f);   // ruudussa 10 px tyhjää alla
        pc.body.transform.localRotation = Quaternion.identity;
        pc.body.color = Color.white;
        int order = Mathf.RoundToInt(-pc.transform.position.y * 100f);
        pc.body.sortingOrder = order;
        if (pc.shadow != null)
        {
            pc.shadow.enabled = true;
            pc.shadow.sortingOrder = order - 1;
            pc.shadow.transform.localPosition = new Vector3(0f, groundHeight, 0f);
            pc.shadow.transform.localScale = new Vector3(3.6f, 0.5f, 1f);
        }
    }

    float GroundAt(float y) => pc.useSidewalk && y > pc.curbDepthY ? pc.sidewalkHeight : 0f;

    void Ride(float dt)
    {
        if (busy) return;
        Vector2 input = PlayerController.ReadMoveInput();
        // suunnanvaihto vain lähes pysähdyksissä
        if (Mathf.Abs(speed) < 1f && Mathf.Abs(input.x) > 0.3f && Mathf.Sign(input.x) != Dir) { facingRight = input.x > 0f; speed = 0f; }
        float target = Mathf.Max(0f, input.x * Dir) * maxSpeed;            // eteenpäin kaasu, taaksepäin jarru
        float rate = target > speed ? acceleration : (input.x * Dir < -0.3f ? braking : acceleration * 0.6f);
        speed = Mathf.MoveTowards(speed, target, rate * dt);

        Vector3 p = pc.transform.position;
        p.x += Dir * speed * dt;
        p.y = Mathf.Clamp(p.y + input.y * depthSpeed * dt, pc.minDepthY, pc.maxDepthY);
        var cf = CameraFollow.Instance; var cam = Camera.main;
        if (cf != null && cam != null)
        {
            float halfW = cam.orthographicSize * cam.aspect - halfLength;
            float lo = cf.minX - halfW, hi = cf.maxX + halfW;
            if (p.x < lo || p.x > hi) { p.x = Mathf.Clamp(p.x, lo, hi); speed = 0f; }
        }
        pc.transform.position = p;
        groundHeight = Mathf.MoveTowards(groundHeight, GroundAt(p.y), 3f * dt);   // reunakiven yli

        // moottori käy: kuvat pyörivät hitaasti paikallaan, vauhdissa nopeammin
        animClock += dt * (6f + speed * 1.4f);
        if (rideSprites != null && rideSprites.Length > 0)
            ShowRider(rideSprites[(int)animClock % rideSprites.Length]);

        if (speed >= runOverMinSpeed) RunOver(p);
        if (UsePressed() && speed < 1.5f) StartCoroutine(Dismount());
    }

    void RunOver(Vector3 me)
    {
        float now = Time.time;
        foreach (var e in Enemy.All.ToArray())
        {
            if (e == null || e.IsDead) continue;
            Vector3 q = e.transform.position;
            if (Mathf.Abs(q.x - me.x) > halfLength || Mathf.Abs(q.y - me.y) > 0.45f) continue;
            if (lastHit.TryGetValue(e, out float t) && now - t < 0.8f) continue;
            if (e.TakeHit(runOverDamage, me.x - Dir * halfLength, true))
            {
                lastHit[e] = now;
                HitSpark.Spawn(new Vector3(q.x, q.y + 1.6f, 0f), true, Mathf.RoundToInt(-q.y * 100f) + 5);
                HitFx.OnHit(true);
                speed *= 0.85f;
            }
        }
        foreach (var c in Crate.All.ToArray())
        {
            if (c == null || !c.CanBeHit) continue;
            Vector3 q = c.transform.position;
            if (Mathf.Abs(q.x - me.x) > halfLength || Mathf.Abs(q.y - me.y) > 0.5f) continue;
            if (lastHit.TryGetValue(c, out float t) && now - t < 0.8f) continue;
            lastHit[c] = now;
            c.TakeHit(20, me.x - Dir * halfLength);
            c.TakeHit(20, me.x - Dir * halfLength);
            c.TakeHit(20, me.x - Dir * halfLength);   // laatikko hajoaa kerralla
        }
    }

    GUIStyle style;
    void OnGUI()
    {
        bool show = near || (riding && !busy && speed < 1.5f && pc != null);
        if (!show) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(Screen.height * 0.032f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = Color.white;
        }
        string text = "[E]  " + Loc.T(riding ? "Nouse pyörän selästä" : prompt);
        float w = Screen.height * 0.5f, h = Screen.height * 0.065f;
        GUI.Box(new Rect((Screen.width - w) * 0.5f, Screen.height * 0.82f, w, h), text, style);
    }
}
