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
    public static int Lives => Difficulty == Level.Helppo ? 5 : Difficulty == Level.Vaikea ? 2 : 3;   // ei käytössä (elämät poistettu)
    /// Alkuenergia (palkin pituus pelin alussa).
    public static int StartHealth => Difficulty == Level.Helppo ? 250 : Difficulty == Level.Vaikea ? 150 : 200;

    public static int ScaleToPlayer(int damage) => damage <= 0 ? damage : Mathf.Max(1, Mathf.RoundToInt(damage * DamageToPlayer));
    /// Kaksinpelissä viholliset kestävät enemmän (iskut tekevät vastaavasti vähemmän): tavalliset 1,5-kertaisesti, pomot 1,8-kertaisesti.
    public const float CoopEnemyHealth = 1.5f, CoopBossHealth = 1.8f;
    public static float CoopFactor(bool boss) => Players.Count > 1 ? (boss ? CoopBossHealth : CoopEnemyHealth) : 1f;

    public static int ScaleToEnemy(int damage, bool boss = false) => damage <= 0 ? damage : Mathf.Max(1, Mathf.RoundToInt(damage * DamageToEnemies / CoopFactor(boss)));

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

    // ---------------- Tehosteet, kirkkaus, tärinä, näyttö ----------------

    static float sfxCache = -1f;
    /// Tehosteiden voimakkuus 0–1 (musiikki ja pääääni erikseen). Välimuistissa, koska luetaan usein.
    public static float SfxVolume
    {
        get { if (sfxCache < 0f) sfxCache = PlayerPrefs.GetFloat("sfx", 1f); return sfxCache; }
        set { sfxCache = Mathf.Clamp01(value); PlayerPrefs.SetFloat("sfx", sfxCache); PlayerPrefs.Save(); }
    }

    /// Kirkkaus 0–10, 5 = ei muutosta.
    public static int Brightness
    {
        get => PlayerPrefs.GetInt("kirkkaus", 5);
        set { PlayerPrefs.SetInt("kirkkaus", Mathf.Clamp(value, 0, 10)); PlayerPrefs.Save(); }
    }

    static int shakeCache = -1;
    /// Ruudun tärinä päällä.
    public static bool ScreenShake
    {
        get { if (shakeCache < 0) shakeCache = PlayerPrefs.GetInt("tarina", 1); return shakeCache != 0; }
        set { shakeCache = value ? 1 : 0; PlayerPrefs.SetInt("tarina", shakeCache); PlayerPrefs.Save(); }
    }

    /// Näyttötila: 0 = koko näyttö, 1 = reunaton ikkuna, 2 = ikkuna.
    public static int ScreenMode
    {
        get
        {
            switch (Screen.fullScreenMode)
            {
                case FullScreenMode.ExclusiveFullScreen: return 0;
                case FullScreenMode.Windowed: return 2;
                default: return 1;
            }
        }
        set
        {
            value = ((value % 3) + 3) % 3;
            Screen.fullScreenMode = value == 0 ? FullScreenMode.ExclusiveFullScreen : value == 1 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            PlayerPrefs.SetInt("naytto", value); PlayerPrefs.Save();
        }
    }

    public static bool VSync
    {
        get => QualitySettings.vSyncCount > 0;
        set { QualitySettings.vSyncCount = value ? 1 : 0; PlayerPrefs.SetInt("vsync", value ? 1 : 0); PlayerPrefs.Save(); ApplyFpsCap(); }
    }

    /// FPS-raja: 0 = 60, 1 = 120, 2 = rajaton (vsync päällä ruudun päivitys määrää tahdin).
    public static int FpsCapIndex
    {
        get => PlayerPrefs.GetInt("fps", 2);
        set { PlayerPrefs.SetInt("fps", ((value % 3) + 3) % 3); PlayerPrefs.Save(); ApplyFpsCap(); }
    }

    static void ApplyFpsCap()
    {
        int i = FpsCapIndex;
        Application.targetFrameRate = i == 0 ? 60 : i == 1 ? 120 : -1;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplyOnStart()
    {
        AudioListener.volume = MasterVolume;
        // tallennetut näyttöasetukset käyttöön (vain jos pelaaja on muuttanut niitä)
        if (PlayerPrefs.HasKey("naytto")) ScreenMode = PlayerPrefs.GetInt("naytto");
        if (PlayerPrefs.HasKey("vsync")) QualitySettings.vSyncCount = PlayerPrefs.GetInt("vsync");
        if (PlayerPrefs.HasKey("fps")) ApplyFpsCap();
    }
}
