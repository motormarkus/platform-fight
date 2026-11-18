using UnityEngine;

/// <summary>Kutsutun liittolaisluurangon elinaika: heräät, kun maastanousu on valmis, ja murenee kappaleiksi ajan päätyttyä.</summary>
public sealed class SkeletonAllyLife : MonoBehaviour
{
    public float life = 25f;
    Enemy e; float t; bool woke;

    void Awake() { e = GetComponent<Enemy>(); }

    void Update()
    {
        if (e == null || e.IsDead) { Destroy(this); return; }
        if (!e.enabled) return;                      // vielä nousemassa maasta
        if (!woke) { woke = true; e.IsAwake = true; }
        t += Time.deltaTime;
        if (t >= life) { Enemy.EnemyHits(e, 99999, transform.position.x, true); Destroy(this); }
    }
}
