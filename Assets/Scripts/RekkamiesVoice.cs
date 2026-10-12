using UnityEngine;

/// <summary>
/// Rekkamiehen puheet: satunnaisia repliikkejä, tanssilattialla Elainen kanssa oma repliikki ja Shanicen lähellä omat.
/// Tappelun alettua (BarBrawl) hän huutelee silloin tällöin. Ääni vain, kun hero on samalla alueella.
/// </summary>
public class RekkamiesVoice : MonoBehaviour
{
    public Enemy enemy;
    public Area area;
    public BarBrawl brawl;
    public BarElaine elaine, shanice;
    public AudioClip[] speak, speakShanice, danceTalk;
    public float volume = 0.9f;
    public Vector2 speakInterval = new Vector2(12f, 26f);
    public float hearRange = 16f;
    public float partnerRange = 3.5f;
    float speakT = 6f, danceT = 4f, shaniceT = 10f;
    PlayerController hero;
    float busyUntil;   // milloin edellinen repliikki loppuu

    void Update()
    {
        if (enemy == null || enemy.IsDead) return;
        if (area != null && Area.Current != area) return;
        if (hero == null) hero = FindFirstObjectByType<PlayerController>();
        if (hero == null) return;
        if (Time.time < busyUntil) return;   // edellinen repliikki on vielä kesken: ei aloiteta uutta
        float dt = Time.deltaTime;
        Vector3 me = transform.position;
        bool fighting = brawl != null && brawl.Started;
        if (Mathf.Abs(hero.transform.position.x - me.x) > hearRange) return;

        if (!fighting)
        {
            // tanssii Elainen kanssa tanssilattialla: oma repliikki silloin tällöin
            danceT -= dt;
            if (danceT <= 0f && elaine != null && Mathf.Abs(elaine.transform.position.x - me.x) <= partnerRange)
            {
                danceT = Random.Range(18f, 32f);
                Play(danceTalk);
                return;
            }
            // Shanice tulee juttelemaan: vastaa hänelle
            shaniceT -= dt;
            if (shaniceT <= 0f && shanice != null && Mathf.Abs(shanice.transform.position.x - me.x) <= partnerRange)
            {
                shaniceT = Random.Range(16f, 28f);
                Play(speakShanice);
                return;
            }
        }
        speakT -= dt;
        if (speakT <= 0f)
        {
            speakT = Random.Range(speakInterval.x, speakInterval.y);
            Play(speak);
        }
    }

    void Play(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        var c = clips[Random.Range(0, clips.Length)];
        if (c == null) return;
        HitFx.PlayClip(c, volume);
        busyUntil = Time.time + c.length + 0.3f;
    }
}
