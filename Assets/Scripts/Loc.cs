using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pelin tekstit ja kielivalinta. Tekstit kirjoitetaan koodiin ja sceneen suomeksi;
/// tästä taulukosta haetaan englanninkielinen vastine, kun kieli on English.
/// Jos käännöstä ei ole, näytetään suomenkielinen teksti.
/// Uusi teksti: kirjoita se suomeksi ja lisää rivi { "suomeksi", "in English" } alle.
/// </summary>
public static class Loc
{
    public enum Lang { Suomi, English }

    static Lang? current;
    public static Lang Current
    {
        get
        {
            if (current == null) current = (Lang)PlayerPrefs.GetInt("kieli", 0);
            return current.Value;
        }
        set { current = value; PlayerPrefs.SetInt("kieli", (int)value); PlayerPrefs.Save(); }
    }

    public static void Toggle() => Current = Current == Lang.Suomi ? Lang.English : Lang.Suomi;

    /// Palauttaa tekstin valitulla kielellä.
    public static string T(string fi)
    {
        if (string.IsNullOrEmpty(fi) || Current == Lang.Suomi) return fi;
        return En.TryGetValue(fi, out string en) ? en : fi;
    }

    /// Kuten T, mutta täyttää {0}, {1}... -kohdat.
    public static string F(string fi, params object[] args) => string.Format(T(fi), args);

    static readonly Dictionary<string, string> En = new Dictionary<string, string>
    {
        { "Suutele Auroraa", "Kiss Aurora" },
        { "Lopeta tanssi", "Stop dancing" },
        // --- HUD ---
        { "PELAAJA", "PLAYER" },
        { "Enter = uusi peli", "Press Enter to play again" },
        { "Kieli: Suomi", "Language: English" },

        // --- Viholliset ---
        { "Kovis", "Kovis" },
        { "Punkkari", "Punk" },

        // --- Ovet ---
        { "Mene sisään", "Go inside" },
        { "Mene S-Clubiin", "Enter S-Club" },
        { "Ulos kadulle", "Back to the street" },
        { "Kiipeä katolle", "Climb to the roof" },
        { "Nouse pyörän selkään", "Get on the bike" },
        { "Laskeudu kujalle", "Climb down to the alley" },
        { "Valtatie", "Highway" },
        { "Nouse pyörän selästä", "Get off the bike" },
        { "Laskeudu kadulle", "Climb down to the street" },

        // --- Baaritiski ---
        { "S-CLUB  BAARI", "S-CLUB  BAR" },
        { "Baaritiski", "Bar counter" },
        { "Puhu Sohville", "Talk to Sohvi" },
        { "Mitä saisi olla?", "What'll it be?" },
        { "Energia on jo täynnä.", "You're already at full health." },
        { "Ei riitä markat!", "Not enough markka!" },
        { "{0}: +{1} energiaa. {2}", "{0}: +{1} health. {2}" },
        { "{0}: +{1} energiaa, +{3} staminaa. {2}", "{0}: +{1} health, +{3} stamina. {2}" },
        { "Rahaa: {0} mk      Energia: {1} / {2}", "Money: {0} mk      Health: {1} / {2}" },
        { "Rahaa: {0} mk      Energia: {1} / {2}      Stamina: {3} / {4}", "Money: {0} mk      Health: {1} / {2}      Stamina: {3} / {4}" },
        { "W/S valitse   ·   E osta   ·   Esc poistu", "W/S select   ·   E buy   ·   Esc leave" },
        { "TÄYSI", "FULL" },

        // --- Tuotteet ---
        { "Sipsipussi", "Chips" },
        { "Rapsakka.", "Crunchy." },
        { "Grillimakkara", "Makkara (grilled sausage)" },
        { "Sinapilla.", "With mustard." },
        { "Lonkero", "Lonkero (long drink)" },
        { "Kylmä ja kirpeä.", "Cold and fizzy." },
        { "Makkaraperunat", "Makkaraperunat (sausage & fries)" },
        { "Kunnon annos.", "A proper meal." },
        { "Tuoppi", "Pint of beer" },
        { "Hanasta.", "Straight from the tap." },
        { "Kossupaukku", "Kossu shot (Finnish vodka)" },
        { "Täydet voimat!", "Full power!" },
    };
}
