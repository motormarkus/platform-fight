using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Linnakkeen juhlasali: kamera noin 15 % lähemmäs (hahmot ~15 % isompia). Ajonaikainen säätö, ettei valikkoa tarvitse ajaa uudelleen.
/// Kameran koko pienenee, joten kameran x-rajat leventyvät (näkyvä leveys pienenee), pystysuunnassa kamera siirretään alas niin, että lattia pysyy ruudun alareunassa.
/// </summary>
public static class HallZoomFix
{
    public static float Zoom = 1.15f;
    static readonly HashSet<int> done = new HashSet<int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { done.Clear(); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init() { SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded; Apply(); }
    static void OnLoaded(Scene s, LoadSceneMode m) { Apply(); }

    static void Apply()
    {
        foreach (var a in Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (a == null || a.areaName != "Muurin juhlahalli" || !done.Add(a.GetInstanceID())) continue;
            float f = 1f / Zoom;
            float halfOld = a.camSize * (16f / 9f), shrink = halfOld * (1f - f);   // näkyvän leveyden puolikas (suunnittelukuvasuhde 16:9) pienenee
            float oldMin = a.camMinX;
            a.camMinX -= shrink; a.camMaxX += shrink;
            a.camOffsetY -= a.camSize * (1f - f);   // lattia pysyy alareunassa, katto rajautuu
            a.camSize *= f;
            // ikkunoiden avaruusparallaksi ja muut kerrokset seuraavat uusia kamerarajoja
            foreach (var pl in a.gameObject.scene.GetRootGameObjects())
                foreach (var p in pl.GetComponentsInChildren<ParallaxLayer>(true))
                    if (p != null && Mathf.Abs(p.startCamX - oldMin) < 0.05f) { p.startCamX = a.camMinX; p.minX = a.camMinX - 2f; p.maxX = a.camMaxX + 2f; }
        }
    }
}
