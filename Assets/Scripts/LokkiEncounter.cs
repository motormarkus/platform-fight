using UnityEngine;

/// <summary>
/// Lokin ensiesiintyminen juna-asemalla: kun hero kävelee triggerX:n ohi, Lokki juoksee ruutuun OIKEALTA (nopein pomoista), pysähtyy ja sanoo lyhyen repliikin,
/// sitten taistelu: Lokkiin ei osu (LokkiBrain), hero ei kuole (PlayerController.Sparring: energia pysyy vähintään 1). Kun hero on pieksetty (SparDefeated),
/// Lokki sanoo loppurepliikin ja pakenee oikealle; hero saa energiaa takaisin (healFraction) ja peli jatkuu. Tapahtuu vain kerran.
/// </summary>
public class LokkiEncounter : MonoBehaviour
{
    public Area area;
    public Enemy lokki;
    public float triggerX;
    public string[] introLines = { "Sinä siis olet se, josta kaikki puhuvat.", "Odotin enemmän." };
    public string[] outroLines = { "Palaa, kun olet oikeasti valmis." };
    [Tooltip("Hero ajaa Lokin nurkkaan (energia alle retreatFraction): Lokki vetäytyy.")]
    public string[] retreatLines = { "Et ole ihan hukassa. Vielä." };
    [Range(0f, 1f)] public float retreatFraction = 0.25f;
    bool heroWon;
    public float lineTime = 1.7f, speakDistance = 7f, healFraction = 0.25f;

    enum S { Waiting, Entering, Speaking, Fight, Outro, Fleeing, Done }
    S state = S.Waiting;
    float t; int line;
    GameObject bubble; TextMesh bubbleText;
    LokkiBrain brain;
    bool defeated;
    static bool played;   // vain kerran per pelisessio

    void OnEnable() { PlayerController.SparDefeated += OnDefeated; }
    void OnDisable() { PlayerController.SparDefeated -= OnDefeated; if (state == S.Fight || state == S.Outro || state == S.Fleeing) PlayerController.Sparring = false; Enemy.FreezeToIdle = false; }

    void OnDefeated(PlayerController pc) { if (state == S.Fight) defeated = true; }

    float CamRight() { var c = Camera.main; return c != null ? c.transform.position.x + c.orthographicSize * c.aspect : (area != null ? area.camMaxX : 0f); }

    void Update()
    {
        if (lokki == null || area == null) return;
        var hero = Players.Primary;
        switch (state)
        {
            case S.Waiting:
                if (played) { state = S.Done; return; }
                if (Area.Current != area || hero == null) return;
                if (hero.transform.position.x < triggerX) return;
                Begin(hero);
                break;
            case S.Entering:
                if (hero != null && Mathf.Abs(hero.transform.position.x - lokki.transform.position.x) <= speakDistance) StartSpeech(introLines, S.Speaking);
                break;
            case S.Speaking:
                t += Time.deltaTime;
                if (hero != null) lokki.FacingRight = hero.transform.position.x > lokki.transform.position.x;
                if (t >= lineTime) { t = 0f; line++; if (line >= introLines.Length) EndSpeech(); else SetBubble(introLines[line]); }
                break;
            case S.Fight:
                if (defeated) StartOutro();
                else if (lokki.Health <= lokki.maxHealth * retreatFraction) { heroWon = true; StartOutro(); }
                break;
            case S.Outro:
                t += Time.deltaTime;
                if (hero != null) lokki.FacingRight = hero.transform.position.x > lokki.transform.position.x;
                if (t >= lineTime) { t = 0f; line++; if (line >= OutroSet().Length) StartFlee(); else SetBubble(OutroSet()[line]); }
                break;
            case S.Fleeing:
                if (lokki.transform.position.x > CamRight() + 3f || Time.time - t > 6f) Finish();
                break;
        }
    }

    string[] OutroSet() => heroWon ? retreatLines : outroLines;

    void Begin(PlayerController hero)
    {
        played = true;
        brain = lokki.GetComponent<LokkiBrain>();
        var h = hero.transform.position;
        lokki.gameObject.SetActive(true);
        float y = Mathf.Clamp(h.y, area.minDepthY + 0.2f, area.maxDepthY - 0.2f);
        lokki.transform.position = new Vector3(CamRight() + 1.5f, y, 0f);
        lokki.IsAwake = true; lokki.ignoreFreeze = true; lokki.FacingRight = false;
        Enemy.FreezeToIdle = true;
        foreach (var p in Players.All) if (p != null) { p.Scripted = true; p.ScriptedMove = Vector2.zero; }
        state = S.Entering;
    }

    void StartSpeech(string[] lines, S next)
    {
        state = next; t = 0f; line = 0;
        lokki.ignoreFreeze = false;   // FreezeToIdle pysäyttää hänetkin repliikin ajaksi
        SetBubble(lines[0]);
    }

    void SetBubble(string text)
    {
        if (bubble == null)
        {
            bubble = new GameObject("Lokin repliikki");
            bubble.transform.SetParent(lokki.transform, false); bubble.transform.localPosition = new Vector3(0f, 4.2f, 0f);
            bubbleText = bubble.AddComponent<TextMesh>();
            bubbleText.anchor = TextAnchor.MiddleCenter; bubbleText.alignment = TextAlignment.Center;
            bubbleText.fontSize = 64; bubbleText.characterSize = 0.1f; bubbleText.color = new Color(0.95f, 0.97f, 1f);
            bubbleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var mr = bubble.GetComponent<MeshRenderer>(); mr.sharedMaterial = bubbleText.font.material; mr.sortingOrder = 30000;
        }
        bubbleText.text = Loc.T(text);
        bubble.SetActive(true);
    }

    void HideBubble() { if (bubble != null) bubble.SetActive(false); }

    void EndSpeech()
    {
        HideBubble();
        Enemy.FreezeToIdle = false;
        foreach (var p in Players.All) if (p != null) { p.Scripted = false; p.ScriptedMove = Vector2.zero; }
        lokki.ignoreFreeze = true;
        PlayerController.Sparring = true;   // hero ei kuole: energia pysyy vähintään 1, SparDefeated kun se on 1
        state = S.Fight;
    }

    void StartOutro()
    {
        // taistelu ohi: Lokki lopettaa hyökkäykset ja sanoo loppurepliikin; muut pysähtyvät
        state = S.Outro; t = 0f; line = 0;
        if (brain != null) brain.chancePerSec = 0f;
        lokki.attackCooldown = 99f;
        lokki.ignoreFreeze = false; Enemy.FreezeToIdle = true;
        SetBubble(OutroSet()[0]);
    }

    void StartFlee()
    {
        HideBubble();
        Enemy.FreezeToIdle = false;
        lokki.ignoreFreeze = true;
        state = S.Fleeing; t = Time.time;
        if (brain != null) brain.Flee(1f);
    }

    void Finish()
    {
        lokki.gameObject.SetActive(false);
        PlayerController.Sparring = false;
        if (!heroWon)   // hero hävisi: saa energiaa takaisin
            foreach (var p in Players.All)
                if (p != null && p.health < Mathf.RoundToInt(p.maxHealth * healFraction)) p.health = Mathf.RoundToInt(p.maxHealth * healFraction);
        state = S.Done;
    }
}
