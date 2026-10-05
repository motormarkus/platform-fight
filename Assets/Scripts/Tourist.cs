using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Istuva turisti, joka nousee tuolilta, kun pelaaja tulee lähelle: nousuanimaatio, hetki seisten,
/// kävely annettuun kohtaan (esim. baarille) ja seisova idle siellä.
/// Kaikki kuvasarjat samassa koordinaatistossa (pivot ruudun alakeskellä); seisovat kuvat siirretään standOffsetY alemmas.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Tourist : MonoBehaviour
{
    public Sprite[] sitLoop;
    public float sitFrameTime = 0.22f;
    public int sitStartFrame;
    public Sprite[] standUp, idle, walk;
    public float standFrameTime = 0.11f, idleFrameTime = 0.16f, walkFrameTime = 0.065f;
    [Tooltip("Seisovien kuvien siirto alaspäin istumakuviin nähden (yksikköä).")]
    public float standOffsetY;
    [Tooltip("Kuinka läheltä (x) pelaaja herättää turistin.")]
    public float triggerDistance = 3.5f;
    [Tooltip("Mihin x:ään kävellään nousun jälkeen (0 = jää paikalleen).")]
    public float walkToX;
    public float walkSpeed = 1.4f;
    public float idleBeforeWalk = 1.2f;

    [Header("Suudelma (E seisovan turistin vieressä)")]
    [Tooltip("Pariskunnan kuvat (hero + turisti) toistojärjestyksessä ja kunkin kuvan kesto.")]
    public Sprite[] kissFrames;
    public float[] kissTimes;
    [Tooltip("Kuvapisteet (2x tarkkuus, pivot alakeskellä): turistin jalat seisovassa idlessä, pariskuvassa, heron aloitus- ja lopetuskohta.")]
    public Vector2 idleFeetPx = new Vector2(317f, 767f), kissFeetPx = new Vector2(336f, 495f);
    public float kissHeroStartPx = 113f, kissHeroEndPx = 104f;
    public float idleCellH = 768f, kissCellH = 512f;
    public string kissPrompt = "Suutele Auroraa";
    bool near, kissing;
    GUIStyle style;

    enum S { Sit, StandUp, Wait, Walk, Idle }
    S state = S.Sit;
    SpriteRenderer sr;
    float t, sitY;
    PlayerController pc;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sitY = transform.position.y;
        t = sitStartFrame * sitFrameTime;
    }

    static Sprite Loop(Sprite[] s, float t, float ft) => s[(int)(t / ft) % s.Length];

    void Update()
    {
        if (pc == null) pc = FindFirstObjectByType<PlayerController>();
        float dt = Time.deltaTime;
        t += dt;
        switch (state)
        {
            case S.Sit:
                if (sitLoop != null && sitLoop.Length > 0) sr.sprite = Loop(sitLoop, t, sitFrameTime);
                if (pc != null && standUp != null && standUp.Length > 0 && Mathf.Abs(pc.transform.position.x - transform.position.x) < triggerDistance)
                {
                    state = S.StandUp; t = 0f;
                    Vector3 p = transform.position; p.y = sitY - standOffsetY; transform.position = p;
                    sr.sortingOrder = Mathf.RoundToInt(-p.y * 100f);
                }
                break;
            case S.StandUp:
            {
                int f = (int)(t / standFrameTime);
                if (f >= standUp.Length) { state = S.Wait; t = 0f; break; }
                sr.sprite = standUp[f];
                break;
            }
            case S.Wait:
                if (idle != null && idle.Length > 0) sr.sprite = Loop(idle, t, idleFrameTime);
                if (t >= idleBeforeWalk) { state = walkToX != 0f && walk != null && walk.Length > 0 ? S.Walk : S.Idle; t = 0f; }
                break;
            case S.Walk:
            {
                Vector3 p = transform.position;
                float dir = Mathf.Sign(walkToX - p.x);
                sr.flipX = dir < 0f;                     // kuvat katsovat oikealle
                p.x = Mathf.MoveTowards(p.x, walkToX, walkSpeed * dt);
                transform.position = p;
                sr.sprite = Loop(walk, t, walkFrameTime);
                if (Mathf.Abs(p.x - walkToX) < 0.01f) { state = S.Idle; t = 0f; }
                break;
            }
            case S.Idle:
                if (kissing) break;
                if (idle != null && idle.Length > 0) sr.sprite = Loop(idle, t, idleFrameTime);
                near = false;
                if (pc != null && kissFrames != null && kissFrames.Length > 0 && pc.enabled && pc.IsFree && !pc.Riding)
                {
                    Vector3 q = pc.transform.position, me = transform.position;
                    near = Mathf.Abs(q.x - me.x) < 3.2f && Mathf.Abs(q.y - me.y) < 0.8f;
                    if (near) sr.flipX = q.x < me.x;      // kääntyy heroa kohti
                    if (near && UsePressed()) StartCoroutine(Kiss());
                }
                break;
        }
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

    /// Hero kävelee viereen, sitten pariskunnan kuvat (halaus ja suudelma) turistin paikalla; lopuksi kumpikin omiin kuviinsa.
    IEnumerator Kiss()
    {
        kissing = true; near = false;
        float k = transform.localScale.x / 100f;           // kuvapikseli maailmassa (sprite 100 px/yks)
        Vector3 me = transform.position;
        sr.flipX = true;                                   // katsoo vasemmalle, hero tulee vasemmalta
        // pariskuvan paikka: turistin jalat samaan kohtaan kuin seisovassa (peilatussa) idlessä
        float feetX = me.x - (idleFeetPx.x - 256f) * k, feetY = me.y + (idleCellH - idleFeetPx.y) * k;
        Vector3 cpos = new Vector3(feetX - (kissFeetPx.x - 256f) * k, feetY - (kissCellH - kissFeetPx.y) * k, me.z);
        Vector3 heroStart = new Vector3(cpos.x + (kissHeroStartPx - 256f) * k, me.y, 0f);
        Vector3 heroEnd = new Vector3(cpos.x + (kissHeroEndPx - 256f) * k, me.y, 0f);
        // hero kävelee paikalle
        pc.Scripted = true;
        for (float tt = 0f; tt < 4f; tt += Time.deltaTime)
        {
            Vector3 d = heroStart - pc.transform.position;
            if (Mathf.Abs(d.x) < 0.12f && Mathf.Abs(d.y) < 0.08f) break;
            pc.ScriptedMove = new Vector2(Mathf.Abs(d.x) < 0.12f ? 0f : Mathf.Sign(d.x), Mathf.Abs(d.y) < 0.08f ? 0f : Mathf.Clamp(d.y * 3f, -1f, 1f));
            yield return null;
        }
        pc.ScriptedMove = Vector2.zero;
        yield return null;
        pc.enabled = false;
        if (pc.body != null) pc.body.enabled = false;
        if (pc.shadow != null) pc.shadow.enabled = false;
        transform.position = cpos; sr.flipX = false;
        for (int i = 0; i < kissFrames.Length; i++)
        {
            sr.sprite = kissFrames[i];
            yield return new WaitForSeconds(kissTimes != null && i < kissTimes.Length ? kissTimes[i] : 0.12f);
        }
        transform.position = me; sr.flipX = true; t = 0f;
        if (idle != null && idle.Length > 0) sr.sprite = idle[0];
        pc.TeleportTo(heroEnd);
        pc.Face(true);
        if (pc.body != null) pc.body.enabled = true;
        if (pc.shadow != null) pc.shadow.enabled = true;
        pc.enabled = true;
        pc.Scripted = false;
        kissing = false;
    }

    void OnGUI()
    {
        if (!near || kissing) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(Screen.height * 0.032f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = Color.white;
        }
        float w = Screen.height * 0.45f, h = Screen.height * 0.065f;
        GUI.Box(new Rect((Screen.width - w) * 0.5f, Screen.height * 0.82f, w, h), "[E]  " + Loc.T(kissPrompt), style);
    }
}
