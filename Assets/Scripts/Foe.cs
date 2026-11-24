using UnityEngine;

/// <summary>
/// Nyrkkeilijäaivojen (MarcusBoxer, DeaconBoss) vastustaja: hero tai toinen vihu (Enemy.duelFoe, esim. WTF-areenan näytösottelu).
/// Aivot käsittelevät kohdetta tämän kautta, joten sama liikekoodi toimii molempia vastaan.
/// </summary>
public struct Foe
{
    public PlayerController hero;
    public Enemy enemy;

    /// Vihun vastustaja: duelFoe, jos asetettu (ja elossa), muuten hero.
    public static Foe Of(Enemy self)
    {
        if (self != null && self.duelFoe != null && self.duelFoe.isActiveAndEnabled && !self.duelFoe.IsDead) return new Foe { enemy = self.duelFoe };
        return new Foe { hero = self != null ? self.Hero : null };
    }

    public bool Valid => enemy != null || hero != null;
    public bool IsEnemy => enemy != null;
    public Transform transform => enemy != null ? enemy.transform : hero.transform;
    /// Maassa (ei kannata lyödä): heron kaatuminen, vihun kaatuminen/nousu/lento tai sparrauksen KO.
    public bool IsFloored => enemy != null ? enemy.DownOrRising || enemy.SparKO : hero.IsFloored;
    public float AirHeight => enemy != null ? enemy.Height : hero.AirHeight;

    public void TakeHit(int damage, float attackerX, Enemy attacker, bool comboFollow)
    {
        if (enemy != null) Enemy.EnemyHits(enemy, damage, attackerX, false);
        else hero.TakeHit(damage, attackerX, attacker, comboFollow);
    }

    public bool TakeKnockdown(int damage, float attackerX, float speed, float up, Enemy attacker)
    {
        if (enemy != null) return Enemy.EnemyHits(enemy, damage, attackerX, true);
        return hero.TakeKnockdown(damage, attackerX, speed, up, attacker);
    }
}
