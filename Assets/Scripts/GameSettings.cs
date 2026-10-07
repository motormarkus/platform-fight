using UnityEngine;

/// <summary>
/// Pelaajan asetukset (muistetaan PlayerPrefsissä): vaikeustaso, äänenvoimakkuudet ja näyttö.
/// Vaikeustaso vaikuttaa vihollisten iskujen voimaan, heron iskujen tehoon vihollisiin ja elämien määrään.
/// </summary>
public static class GameSettings
{
    public enum Level { Helppo, Normaali, Vaikea }

    public static Level Difficulty
    {
        get => (Level)PlayerPrefs.GetInt("vaikeus", (int)Level.Normaali);
        set { PlayerPrefs.SetInt("vaikeus", (int)value); PlayerPrefs.Save(); }
    }

    /// Vihollisen isku heroon kerrotaan tällä.
    public static float DamageToPlayer => Difficulty == Level.Helppo ? 0.6f : Difficulty == Level.Vaikea ? 1.4f : 1f;
    /// Iskut vihollisiin kerrotaan tällä.
    public static float DamageToEnemies => Difficulty == Level.Helppo ? 1.3f : Difficulty == Level.Vaikea ? 0.85f : 1f;
    public static int Lives => Difficulty == Level.Helppo ? 5 : Difficulty == Level.Vaikea ? 2 : 3;

    public static int ScaleToPlayer(int damage) => damage <= 0 ? damage : Mathf.Max(1, Mathf.RoundToInt(damage * DamageToPlayer));
    public static int ScaleToEnemy(int damage) => damage <= 0 ? damage : Mathf.Max(1, Mathf.RoundToInt(damage * DamageToEnemies));

    /// Pelattava hahmo: 0 = Rocco, 1 = Ruby.
    public static int Character
    {
        get => PlayerPrefs.GetInt("hahmo", 0);
        set { PlayerPrefs.SetInt("hahmo", value); PlayerPrefs.Save(); }
    }

    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat("aanet", 1f);
        set { PlayerPrefs.SetFloat("aanet", Mathf.Clamp01(value)); AudioListener.volume = Mathf.Clamp01(value); PlayerPrefs.Save(); }
    }

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat("musiikki", 1f);
        set { PlayerPrefs.SetFloat("musiikki", Mathf.Clamp01(value)); PlayerPrefs.Save(); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplyOnStart() { AudioListener.volume = MasterVolume; }
}
