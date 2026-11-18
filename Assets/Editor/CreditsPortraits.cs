using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Valikko 427: rakentaa lopputekstien muotokuva-atlaksen (Assets/Resources/Lopputekstit/muotokuvat.png) EndCredits.Casts-listan mukaan:
/// jokaisesta kuvalähteestä ensimmäinen kuva, rajattuna läpinäkyvyyden mukaan ja skaalattuna ruutuun (192 x 240, alareunaan tasattuna).
/// </summary>
public static class CreditsPortraits
{
    [MenuItem("Beat em up/427. Lopputekstit: rakenna muotokuvat", false, 427)]
    static void Menu() { Run(true); }

    public static void RunBatch() { Run(false); }

    static void Run(bool dialog)
    {
        int cw = EndCredits.CellW, ch = EndCredits.CellH, cols = EndCredits.PortraitCols, n = EndCredits.Casts.Length, rows = (n + cols - 1) / cols;
        var atlas = new Texture2D(cols * cw, rows * ch, TextureFormat.RGBA32, false);
        atlas.SetPixels32(new Color32[atlas.width * atlas.height]);   // läpinäkyvä
        var missing = new List<string>();
        for (int i = 0; i < n; i++)
        {
            var c = EndCredits.Casts[i];
            var cell = Portrait(c.src, cw, ch);
            if (cell == null) { missing.Add(c.fi + " (" + c.src + ")"); continue; }
            int col = i % cols, rowFromTop = i / cols, x0 = col * cw, y0 = atlas.height - (rowFromTop + 1) * ch;
            atlas.SetPixels(x0, y0, cw, ch, cell);
        }
        atlas.Apply();
        string dir = "Assets/Resources/Lopputekstit"; Directory.CreateDirectory(dir);
        string path = dir + "/muotokuvat.png";
        File.WriteAllBytes(path, atlas.EncodeToPNG()); Object.DestroyImmediate(atlas);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Default; imp.mipmapEnabled = false; imp.alphaIsTransparency = true; imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed; imp.maxTextureSize = 4096; imp.npotScale = TextureImporterNPOTScale.None; imp.isReadable = false;
            imp.SaveAndReimport();
        }
        string doors = ConfigureExitDoors();
        string msg = doors + " " + "Lopputekstien muotokuvat valmiit: " + (n - missing.Count) + "/" + n + (missing.Count > 0 ? ". Puuttuu: " + string.Join(", ", missing) : ".");
        Debug.Log("[Lopputekstit] " + msg);
        if (dialog) EditorUtility.DisplayDialog("Lopputekstit", msg, "OK");
    }

    /// Rantabaarin kaksi ulko-ovea käynnistävät lopputekstit varmistuksen jälkeen (sama kuin valikko 424 asettaa).
    static string ConfigureExitDoors()
    {
        const string scenePath = "Assets/Scenes/Areas/Linnake.unity";
        if (!File.Exists(scenePath)) return "Linnake-scenea ei löytynyt.";
        var sc = EditorSceneManager.GetSceneByPath(scenePath); bool opened = false;
        if (!sc.IsValid() || !sc.isLoaded) { sc = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive); opened = true; }
        int n = 0;
        try
        {
            foreach (var d in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (d == null || d.gameObject.scene != sc || (d.name != "Baari: ulos vasemmalle" && d.name != "Baari: ulos terassille")) continue;
                d.endCredits = true; d.prompt = "Poistu baarista"; d.confirmText = "Poistutko baarista? Peli päättyy ja lopputekstit alkavat."; EditorUtility.SetDirty(d); n++;
            }
            EditorSceneManager.MarkSceneDirty(sc); EditorSceneManager.SaveScene(sc);
        }
        finally { if (opened && sc.IsValid()) EditorSceneManager.CloseScene(sc, true); }
        return "Baarin ulko-ovet: " + n + "/2 asetettu lopputeksteille.";
    }

    static Color[] Portrait(string rel, int cw, int ch)
    {
        string assetPath = "Assets/" + rel;
        if (!File.Exists(assetPath)) return null;
        // lähdekuva luetaan suoraan PNG:stä (ei vaadi luettavaksi merkittyä tekstuuria)
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!ImageConversion.LoadImage(tex, File.ReadAllBytes(assetPath))) { Object.DestroyImmediate(tex); return null; }
        // ensimmäinen viipaloitu kuva (jos viipalointi on tehty), muuten koko kuva
        var sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().OrderBy(s => NaturalKey(s.name)).ToArray();
        Rect r = sprites.Length > 0 ? sprites[0].rect : new Rect(0, 0, tex.width, tex.height);
        int rx = Mathf.Clamp(Mathf.RoundToInt(r.x), 0, tex.width - 1), ry = Mathf.Clamp(Mathf.RoundToInt(r.y), 0, tex.height - 1);
        int rw = Mathf.Min(Mathf.RoundToInt(r.width), tex.width - rx), rh = Mathf.Min(Mathf.RoundToInt(r.height), tex.height - ry);
        var px = tex.GetPixels(rx, ry, rw, rh);
        // rajaus alfan mukaan
        int minX = rw, minY = rh, maxX = -1, maxY = -1;
        for (int y = 0; y < rh; y++) for (int x = 0; x < rw; x++) if (px[y * rw + x].a > 0.06f) { if (x < minX) minX = x; if (x > maxX) maxX = x; if (y < minY) minY = y; if (y > maxY) maxY = y; }
        if (maxX < 0) { Object.DestroyImmediate(tex); return null; }
        int bw = maxX - minX + 1, bh = maxY - minY + 1;
        float sc = Mathf.Min((cw - 8f) / bw, (ch - 8f) / bh);
        int dw = Mathf.Max(1, Mathf.RoundToInt(bw * sc)), dh = Mathf.Max(1, Mathf.RoundToInt(bh * sc));
        var outPx = new Color[cw * ch];
        int ox = (cw - dw) / 2, oy = 4;   // alareunaan
        for (int y = 0; y < dh; y++)
            for (int x = 0; x < dw; x++)
            {
                // laatikkosuodatus (pienennys): keskiarvo lähdealueelta, esikerrottu alfa
                float sx0 = minX + x / sc, sx1 = minX + (x + 1) / sc, sy0 = minY + y / sc, sy1 = minY + (y + 1) / sc;
                int ix0 = Mathf.FloorToInt(sx0), ix1 = Mathf.Min(maxX, Mathf.Max(ix0, Mathf.CeilToInt(sx1) - 1)), iy0 = Mathf.FloorToInt(sy0), iy1 = Mathf.Min(maxY, Mathf.Max(iy0, Mathf.CeilToInt(sy1) - 1));
                float a = 0f, rr = 0f, gg = 0f, bb = 0f; int cnt = 0;
                for (int yy = iy0; yy <= iy1; yy++) for (int xx = ix0; xx <= ix1; xx++) { var p = px[yy * rw + xx]; a += p.a; rr += p.r * p.a; gg += p.g * p.a; bb += p.b * p.a; cnt++; }
                if (cnt == 0 || a <= 0f) continue;
                outPx[(oy + y) * cw + ox + x] = new Color(rr / a, gg / a, bb / a, a / cnt);
            }
        Object.DestroyImmediate(tex);
        return outPx;
    }

    static string NaturalKey(string s)
    {
        // nimen numerot täytetään nollilla, jotta "x_2" tulee ennen "x_10"
        var sb = new System.Text.StringBuilder(); int i = 0;
        while (i < s.Length)
        {
            if (char.IsDigit(s[i])) { int j = i; while (j < s.Length && char.IsDigit(s[j])) j++; sb.Append(s.Substring(i, j - i).PadLeft(8, '0')); i = j; }
            else sb.Append(s[i++]);
        }
        return sb.ToString();
    }
}
