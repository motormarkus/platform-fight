using System.Collections;
using UnityEngine;

/// <summary>
/// Pomon voittamisen jälkeen katosta putoavat tikkaat (laivan komentosilta -> katto).
/// Tikkaat ovat piilossa, kunnes pomo kaatuu; sitten ne laskeutuvat lattialle ja ovi (Door) tulee käyttöön.
/// </summary>
public class BossLadder : MonoBehaviour
{
    public Enemy boss;
    public SpriteRenderer ladder;
    public Door door;
    [Tooltip("Aika pomon kaatumisesta tikkaiden putoamiseen.")]
    public float delay = 1.6f;
    public float dropTime = 0.7f;
    [Tooltip("Kuinka korkealta tikkaat putoavat (yksikköä lepoasennon yläpuolelta).")]
    public float dropHeight = 10f;

    bool hadBoss, dropped;
    Vector3 rest;

    void Awake()
    {
        hadBoss = boss != null;
        if (ladder != null) { rest = ladder.transform.localPosition; ladder.enabled = false; }
        if (door != null) door.enabled = false;
    }

    void Update()
    {
        if (dropped || !hadBoss) return;
        if (boss == null || boss.IsDead) { dropped = true; StartCoroutine(Drop()); }
    }

    IEnumerator Drop()
    {
        for (float t = 0f; t < delay; t += Time.deltaTime) yield return null;
        if (ladder == null) yield break;
        var tr = ladder.transform;
        ladder.enabled = true;
        for (float t = 0f; t < dropTime; t += Time.deltaTime)
        {
            float k = t / dropTime;
            tr.localPosition = rest + Vector3.up * dropHeight * (1f - k * k);   // putoaa kiihtyen
            yield return null;
        }
        // pieni kimpoilu perillä
        const float bounce = 0.18f, bounceTime = 0.3f;
        if (CameraFollow.Instance != null) CameraFollow.Shake(0.12f, 0.2f);
        for (float t = 0f; t < bounceTime; t += Time.deltaTime)
        {
            float k = t / bounceTime;
            tr.localPosition = rest + Vector3.up * bounce * 4f * k * (1f - k);
            yield return null;
        }
        tr.localPosition = rest;
        if (door != null) door.enabled = true;
    }
}
