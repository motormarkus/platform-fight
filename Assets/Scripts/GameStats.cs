using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Pelin tilastot lopputekstejä varten: pelattu aika, kaadetut viholliset ja voitetut pomot (istunnon ajan; nollautuvat pelin käynnistyessä).</summary>
public static class GameStats
{
    public static float PlaySeconds;
    public static int Kills;
    public static readonly HashSet<string> Bosses = new HashSet<string>();

    public static void BossDown(string name) { if (!string.IsNullOrEmpty(name)) Bosses.Add(name); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        PlaySeconds = 0f; Kills = 0; Bosses.Clear();
        var go = new GameObject("Tilastot"); Object.DontDestroyOnLoad(go); go.hideFlags = HideFlags.HideInHierarchy; go.AddComponent<Ticker>();
    }
    sealed class Ticker : MonoBehaviour
    {
        void Update()
        {
            PlaySeconds += Time.deltaTime;
            // editorissa F7 käynnistää lopputekstit esikatseluun
            if (Application.isEditor && Keyboard.current != null && Keyboard.current.f7Key.wasPressedThisFrame) EndCredits.Begin();
        }
    }

    public static string TimeText() { int s = Mathf.FloorToInt(PlaySeconds); return (s / 3600) + ":" + ((s / 60) % 60).ToString("00") + ":" + (s % 60).ToString("00"); }
}
