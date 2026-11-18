using UnityEngine;

/// <summary>
/// Pelastuskapseli (aluksen hytti): kun Lokki on kaatunut, hero pääsee kapseliin vain 10 sekunnin ajan (ShipFinale.Arm). Aktiivisena kapseli hehkuu
/// ja lähellä näkyy kehote; Pick (E / ohjaimen nappi) -> hero sisään. Ennen sitä kapseli on vain koriste hytissä.
/// </summary>
public sealed class EscapePod : MonoBehaviour
{
    public SpriteRenderer sr;
    public float useRange = 4.5f;
    public bool Armed { get; private set; }
    public bool Entered { get; private set; }
    GUIStyle st;
    readonly System.Collections.Generic.List<SpriteRenderer> hidden = new System.Collections.Generic.List<SpriteRenderer>();
    PlayerController who; bool released;

    public void Arm() { Armed = true; }
    public void Disarm() { Armed = false; }

    /// Hero ulos kapselista (rannalle): kuvat takaisin, ohjaus vapaaksi.
    public PlayerController Release()
    {
        released = true; if (who != null) who.BullHidden = false;
        foreach (var r in hidden) if (r != null) r.enabled = true;
        hidden.Clear();
        if (who != null) { who.Scripted = false; who.ScriptedMove = Vector2.zero; who.GrantInvulnerable(1.5f); }
        return who;
    }

    PlayerController Near()
    {
        foreach (var pc in Players.All)
        {
            if (pc == null || pc.GameOver) continue;
            Vector3 p = pc.transform.position, q = transform.position;
            if (Mathf.Abs(p.x - q.x) <= useRange && Mathf.Abs(p.y - q.y) <= 6f) return pc;
        }
        return null;
    }

    void Update()
    {
        if (sr != null) sr.color = Armed && !Entered ? Color.Lerp(Color.white, new Color(1f, 0.95f, 0.5f), 0.5f + 0.5f * Mathf.Sin(Time.time * 8f)) : Color.white;
        if (Entered && who != null && !released)
        {   // hero pysyy piilossa: vilkkuva loukkaamattomuus kytkee kuvat takaisin päälle joka kierroksella
            foreach (var r in who.GetComponentsInChildren<SpriteRenderer>(true)) if (r.enabled) { if (!hidden.Contains(r)) hidden.Add(r); r.enabled = false; }
            return;
        }
        if (!Armed || Entered) return;
        var pc = Near();
        if (pc != null && CoopInput.Pick(pc.playerIndex))
        {
            Entered = true;
            who = pc; foreach (var r in pc.GetComponentsInChildren<SpriteRenderer>(true)) if (r.enabled) { hidden.Add(r); r.enabled = false; }   // hero sisään kapseliin
            pc.Scripted = true; pc.ScriptedMove = Vector2.zero; pc.GrantInvulnerable(30f); pc.BullHidden = true;   // BullHidden: hero pysyy piilossa vilkkuvasta loukkaamattomuudesta huolimatta
            if (sr != null) sr.color = Color.white;
        }
    }

    void OnGUI()
    {
        if (!Armed || Entered) return;
        var pc = Near(); if (pc == null) return;
        if (st == null) st = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        st.fontSize = Mathf.RoundToInt(Screen.height * 0.04f); st.normal.textColor = new Color(1f, 0.92f, 0.3f);
        GUI.Label(new Rect(0, Screen.height * 0.62f, Screen.width, Screen.height * 0.08f), "[E]  " + Loc.T("Mene pelastuskapseliin"), st);
    }
}
