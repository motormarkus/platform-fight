using System;
using UnityEngine;

/// <summary>Komentoriviajo Vegasin rakennusvalikoille: Unity.exe -batchmode -executeMethod BeatEmUpSetup.RunVegasBatch.
/// Ajaa 376, 378, 379, 380, 382, 383, 384, 385, 386, 387 ja 381 oikeassa järjestyksessä; ilmoitukset menevät lokiin.</summary>
public static partial class BeatEmUpSetup
{
    public static void RunVegasBatch()
    {
        batch = true;
        try
        {
            Step("376 Vegas", AddVegas);
            Step("378 katu", AddVegasStreet);
            Step("379 baari", AddVegasBar);
            Step("380 Tony", AddVegasTony);
            Step("382 Melvin", AddVegasMelvin);
            Step("383 Don", AddVegasDon);
            Step("384 vihut", AddVegasCrowd);
            Step("385 lava", AddVegasStage);
            Step("386 Violetti", AddVegasViolet);
            Step("387 bäkkäri", AddVegasBackroom);
            Step("381 vessat", AddVegasWc);
            Step("389 kappeli ja pantti", AddVegasChapelPawn);
        }
        finally { batch = false; }
    }

    /// <summary>Showbaarin ruletti-pöytä ja Sohvi (410): vaatii 376, 378, 379 ja 406.</summary>
    public static void RunShowbarExtrasBatch()
    {
        batch = true;
        try { Step("410 showbaarin ruletti ja Sohvi", AddShowbarExtras); }
        finally { batch = false; }
    }

    /// <summary>Vankilan ruokalan Sohvi ja kauppa (414): vaatii vankilan kirjaston kanttiinin.</summary>
    public static void RunPrisonCafeShopBatch()
    {
        batch = true;
        try { Step("414 vankilan ruokala", AddPrisonCafeShop); }
        finally { batch = false; }
    }

    /// <summary>Showroomin esitys ja yleisö (415): vaatii 376, 406, 409 ja 386.</summary>
    public static void RunShowroomShowBatch()
    {
        batch = true;
        try { Step("415 showroomin esitys", AddShowroomShow); }
        finally { batch = false; }
    }

    /// <summary>Showroomin esitys (415) ja Khan (416) kerralla.</summary>
    public static void RunShowroomKhanBatch()
    {
        batch = true;
        try { Step("415 showroomin esitys", AddShowroomShow); Step("416 Khan", AddKhan); }
        finally { batch = false; }
    }

    /// <summary>Linnakkeen ensimmäinen pomohuone (351) ja Kenraali (352).</summary>
    public static void RunFortressBossBatch()
    {
        batch = true;
        try { Step("351 pomohuone", AddFortressBossRoom1); Step("352 Kenraali", AddKenraali); Step("417 katto", AddFortressRoof); }
        finally { batch = false; }
    }

    /// <summary>Aluksen sisätilat (418): vaatii 417.</summary>
    public static void RunShipBatch()
    {
        batch = true;
        try { Step("418 aluksen sisätilat", AddFortressShip); Step("419 aluksen vihut", AddFortressShipEnemies); }
        finally { batch = false; }
    }

    /// <summary>Katto, alus ja vihut uusiksi (417-419) + aloitus katolta (420).</summary>
    public static void RunRoofAllBatch()
    {
        batch = true;
        try { UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", UnityEditor.SceneManagement.OpenSceneMode.Single); Step("417 katto", AddFortressRoof); Step("418 alus", AddFortressShip); Step("419 vihut", AddFortressShipEnemies); Step("420 aloitus", StartAtFortressRoof); }
        finally { batch = false; }
    }

    /// <summary>Aluksen vihut + tankkauspiste (419) ja aloitus katolta (420).</summary>
    public static void RunShipEnemiesBatch()
    {
        batch = true;
        try { Step("419 vihut", AddFortressShipEnemies); Step("420 aloitus", StartAtFortressRoof); }
        finally { batch = false; }
    }

    /// <summary>Aloitus linnakkeen katolta (420).</summary>
    public static void RunRoofStartBatch()
    {
        batch = true;
        try { UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", UnityEditor.SceneManagement.OpenSceneMode.Single); Step("420 aloitus katolta", StartAtFortressRoof); }
        finally { batch = false; }
    }

    /// <summary>Taikuri (409): vaatii 376, 378, 379 ja 406.</summary>
    public static void RunMagicianBatch()
    {
        batch = true;
        try { Step("409 taikuri", AddVegasMagician); }
        finally { batch = false; }
    }

    /// <summary>Kasinon showbaari (406), aula (407) ja ruletti (408). Vaatii, että Vegas (376, 378, 379) on jo ajettu.</summary>
    public static void RunVegasLobbyBatch()
    {
        batch = true;
        try
        {
            Step("406 showbaari", AddVegasShowbar);
            Step("407 aula", AddVegasLobby);
            Step("408 ruletti", AddVegasRoulette);
        }
        finally { batch = false; }
    }

    /// <summary>Vain katu (hääkappelin julkisivu) ja kappelin/panttilainaamon sisätilat.</summary>
    public static void RunVegasChapelBatch()
    {
        batch = true;
        try
        {
            Step("378 katu", AddVegasStreet);
            Step("389 kappeli ja pantti", AddVegasChapelPawn);
            Step("386 Violetti", AddVegasViolet);
        }
        finally { batch = false; }
    }

    /// <summary>Tony, Melvin, Don (ilmalennon silmukat) ja niiden kopiot kasinolla ja kappelissa.</summary>
    public static void RunVegasAirBatch()
    {
        batch = true;
        try
        {
            Step("380 Tony", AddVegasTony);
            Step("382 Melvin", AddVegasMelvin);
            Step("383 Don", AddVegasDon);
            Step("384 vihut", AddVegasCrowd);
            Step("389 kappeli ja pantti", AddVegasChapelPawn);
        }
        finally { batch = false; }
    }

    /// <summary>Rakennustyömaa (valikko 392).</summary>
    public static void RunVegasSiteBatch()
    {
        batch = true;
        try { Step("392 työmaa", AddVegasSite); }
        finally { batch = false; }
    }

    /// <summary>Työmaakonttori (valikko 393) työmaan jälkeen.</summary>
    public static void RunVegasOfficeBatch()
    {
        batch = true;
        try { Step("393 konttori", AddVegasOffice); }
        finally { batch = false; }
    }

    /// <summary>Vain Violetti (valikko 386: baari + kasinon tehtävä-Violetti).</summary>
    public static void RunVegasVioletBatch()
    {
        batch = true;
        try { Step("386 Violetti", AddVegasViolet); }
        finally { batch = false; }
    }

    /// <summary>Uusimmat valikot oikeassa järjestyksessä: työmaa (392), konttori (393), kanttiini + toimisto (394), Nokkamies (395), kanttiinin väki (396),
    /// Lokki (397), juna-aseman NPC:t (398) ja tiski + Sohvi (399).</summary>
    public static void RunLatestBatch()
    {
        batch = true;
        try
        {
            Step("392 työmaa", AddVegasSite);
            Step("393 konttori", AddVegasOffice);
            Step("394 kanttiini ja toimisto", AddVegasCanteen);
            Step("395 Nokkamies", AddNokkamies);
            Step("396 kanttiinin väki", AddVegasCanteenCrowd);
            Step("397 Lokki", AddLokki);
            Step("398 aseman NPC:t", AddStationNpcs);
            Step("399 aseman tiski", AddStationKiosk);
        }
        finally { batch = false; }
    }

    static void Step(string name, Action a)
    {
        Debug.Log("[VegasBatch] >>> " + name);
        try { a(); }
        catch (Exception e) { Debug.LogError("[VegasBatch] " + name + " kaatui: " + e); }
        Debug.Log("[VegasBatch] <<< " + name);
    }
}
