using UnityEngine;

/// <summary>
/// Rex, vankilan pelastettava kaveri (E13). Aluksi sellissä paikallaan (Enemy nukkuu); kun hero on lähellä ja painaa E, Rex vapautuu:
/// hänestä tulee heron liittolainen (Enemy.ally), joka taistelee vihuja vastaan heron rinnalla (Enemy hoitaa kohteen valinnan, seuraamisen ja iskut).
/// Seuraa heroa alueelta ja scenestä toiseen. Elämäpalkki näkyy ruudulla heron palkkien alla. Jos Rex kaatuu, hän on poissa (Enemy kuolee).
/// Lisätään Rex-oliolle valikosta 365.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class RexAlly : MonoBehaviour
{
    public float recruitRange = 2.8f;
    public bool rescued;
    public string label = "REX";

    Enemy e;
    float otherFloorTime;
    float noticeUntil;
    bool near;
    static GUIStyle box;
    static Texture2D px;

    public static bool HasRex
    {
        get { foreach (var r in FindObjectsByType<RexAlly>(FindObjectsSortMode.None)) if (r != null && r.rescued && r.e != null && !r.e.IsDead) return true; return false; }
    }

    void Awake() { e = GetComponent<Enemy>(); if (e != null) { e.ally = true; e.fightsEveryone = false; e.huntsBrawlers = false; e.brawlsWithOthers = false; e.joinsFightWhenSquadComes = false; } }

    void Update()
    {
        if (e == null || e.IsDead) { near = false; return; }
        if (!rescued)
        {
            e.IsAwake = false; e.wakeDistance = 0f;   // sellissä odottamassa: ei lähde heron perään
            near = false;
            var p = Players.Nearest(transform.position);
            if (p == null || p.GameOver || p.Scripted) return;
            Vector3 hp = p.transform.position, me = transform.position;
            if (Mathf.Abs(hp.x - me.x) > recruitRange || Mathf.Abs(hp.y - me.y) > e.depthTolerance + 0.9f) return;
            near = true;
            e.FacingRight = hp.x > me.x;
            if (CoopInput.Pick(p.playerIndex)) Rescue(p);
            return;
        }
        // vapautettu: seuraa heroa alueelta toiselle (myös toiseen sceneen); kaukana oleva hyppää heron viereen
        var hero = e.Hero;
        if (hero == null) return;
        if (Mathf.Abs(hero.transform.position.x - transform.position.x) > 28f || gameObject.scene != hero.gameObject.scene)
        {
            if (gameObject.scene != hero.gameObject.scene) { transform.SetParent(null, true); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gameObject, hero.gameObject.scene); }
            transform.position = new Vector3(hero.transform.position.x - 2.2f, hero.transform.position.y, 0f);
            otherFloorTime = 0f;
        }
        // kaksikerroksisessa käytävässä (D/E-osasto): jos Rex jää eri kerrokseen kuin hero yli 8 s (varmistus, jos portaiden käyttö ei onnistu), hän hyppää heron viereen
        var tf = TwoFloorCorridor.For(Area.Current);
        if (tf != null && Area.Current != null && tf.area == Area.Current)
        {
            float hy = hero.transform.position.y, my = transform.position.y;
            bool heroStable = hy <= tf.lowerMax + 0.05f || hy >= tf.upperMin - 0.05f, meStable = my <= tf.lowerMax + 0.05f || my >= tf.upperMin - 0.05f;
            if (heroStable && meStable && tf.IsUpper(hy) != tf.IsUpper(my)) otherFloorTime += Time.deltaTime; else otherFloorTime = 0f;
            if (otherFloorTime > 8f) { transform.position = new Vector3(hero.transform.position.x - 1.6f, hy, 0f); otherFloorTime = 0f; }
        }
    }

    void Rescue(PlayerController p)
    {
        rescued = true; near = false;
        transform.SetParent(null, true);   // juuriolioksi, jotta seuraaminen toiseen sceneen onnistuu
        if (gameObject.scene != p.gameObject.scene) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gameObject, p.gameObject.scene);
        e.ally = true; e.fightsEveryone = false; e.huntsBrawlers = false; e.brawlsWithOthers = false; e.joinsFightWhenSquadComes = false;
        e.IsAwake = true; e.wakeDistance = 60f;
        e.FacingRight = p.transform.position.x > transform.position.x;
        HitFx.PlayPickup(false);
        noticeUntil = Time.time + 3f;
    }

    void OnGUI()
    {
        if (e == null || e.IsDead) return;
        float H = Screen.height, W = Screen.width;
        if (box == null) { box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold }; box.normal.textColor = Color.white; }
        box.fontSize = Mathf.RoundToInt(H * 0.032f);
        bool notice = Time.time < noticeUntil;
        if (!rescued && near || notice)
        {
            float w = H * 0.8f, h = H * 0.065f;
            GUI.Box(new Rect((W - w) * 0.5f, notice ? H * 0.74f : H * 0.82f, w, h), notice ? Loc.T("Rex liittyi seuraasi!") : "[E]  " + Loc.T("Vapauta Rex"), box);
        }
        if (!rescued) return;
        // elämäpalkki heron palkkien alla (amatsonin palkin alapuolella, jos molemmat mukana)
        if (px == null) { px = new Texture2D(1, 1); px.SetPixel(0, 0, Color.white); px.Apply(); }
        float x = W * 0.045f, y = H * 0.25f, bw = W * 0.2f, bh = H * 0.022f, f = e.HealthFraction;
        var lbl = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(H * 0.03f), fontStyle = FontStyle.Bold };
        lbl.normal.textColor = Color.black; GUI.Label(new Rect(x + 2, y - H * 0.042f + 2, bw, H * 0.04f), label, lbl);
        lbl.normal.textColor = Color.white; GUI.Label(new Rect(x, y - H * 0.042f, bw, H * 0.04f), label, lbl);
        GUI.color = Color.black; GUI.DrawTexture(new Rect(x - 3, y - 3, bw + 6, bh + 6), px);
        GUI.color = new Color(0.35f, 0.05f, 0.05f); GUI.DrawTexture(new Rect(x, y, bw, bh), px);
        GUI.color = Color.Lerp(new Color(0.95f, 0.15f, 0.1f), new Color(0.3f, 0.85f, 0.25f), f); GUI.DrawTexture(new Rect(x, y, bw * f, bh), px);
        GUI.color = Color.white;
    }
}
