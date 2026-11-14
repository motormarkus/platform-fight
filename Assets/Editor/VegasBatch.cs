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

    /// <summary>Vain Violetti (valikko 386: baari + kasinon tehtävä-Violetti).</summary>
    public static void RunVegasVioletBatch()
    {
        batch = true;
        try { Step("386 Violetti", AddVegasViolet); }
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
