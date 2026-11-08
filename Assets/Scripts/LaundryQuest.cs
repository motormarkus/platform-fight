using UnityEngine;

/// <summary>Pyykkitupa-sivutehtävän tila. Kun Unlocked on päällä, rakennuksen aulan vihreä ovi vie kellarin pyykkituvalle.</summary>
public static class LaundryQuest
{
    public const string Key = "laundry_quest";   // PlayerPrefs: 1 = Dina antoi avaimen
    public static bool Unlocked;

    /// Dina antaa avaimen: pyykkitupa aukeaa (tallentuu).
    public static void GiveKey() { Unlocked = true; PlayerPrefs.SetInt(Key, 1); PlayerPrefs.Save(); }
    public static bool KeyGiven => PlayerPrefs.GetInt(Key, 0) == 1;

    public const string AmuletKey = "laundry_amulet";
    /// Onnenamuletti: isomman rahanipun pudotus on 20 % todennäköisempi (kerroin 1,2).
    public static bool HasAmulet => PlayerPrefs.GetInt(AmuletKey, 0) == 1;
    public static void GiveAmulet() { PlayerPrefs.SetInt(AmuletKey, 1); PlayerPrefs.Save(); }
    public static bool RollBigMoney(float chance) => Random.value < (HasAmulet ? chance * 1.2f : chance);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { Unlocked = PlayerPrefs.GetInt(Key, 0) == 1; }
}

/// <summary>Valitsee, mitkä ovet ovat käytössä: whenOpen-ovet vain kun pyykkitupa on auki (LaundryQuest.Unlocked), whenClosed-ovet vain kun se on kiinni.</summary>
public class ElevatorSwitch : MonoBehaviour
{
    public GameObject[] whenOpen, whenClosed;

    void Update()
    {
        bool open = LaundryQuest.Unlocked;
        if (whenOpen != null) foreach (var g in whenOpen) if (g != null && g.activeSelf != open) g.SetActive(open);
        if (whenClosed != null) foreach (var g in whenClosed) if (g != null && g.activeSelf == open) g.SetActive(!open);
    }
}
