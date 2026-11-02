using UnityEngine;

/// <summary>Luolan pimeys: Viidakon luolassa ruutu on tumma ja jokaisen pelaajan ympärillä on pieni valopiiri. Jos soihtu on inventaariossa (Clues "esine_Soihtu"),
/// taukovalikon "Sytytä soihtu" kasvattaa valopiirin suureksi ja lämpimäksi (lepattaa). Luolan alku (sisäänkäynti) ja loppu (kuvat 7–8, valoa kajastaa) ovat valoisammat.
/// Vyöhykkeet annetaan panoraamapikseleinä (CaveX0 + px / ppu); valikko 305 luo komponentin.</summary>
public class CaveDarkness : MonoBehaviour
{
    public const string TorchName = "Soihtu", TorchId = "esine_Soihtu";
    public static bool TorchLit;
    public Material material;
    public string areaName = "Viidakon luola";
    public float caveX0, tileWidth = 29f;
    [Tooltip("Panoraamapikseleinä: pimeä alkaa, täysin pimeä, valoisa alkaa, valoisa täysi.")]
    public Vector4 zonePx = new Vector4(900f, 2100f, 8300f, 9300f);
    public float baseRadius = 2.6f, torchRadius = 9.5f;

    SpriteRenderer quad; Sprite white;
    readonly Vector4[] lights = new Vector4[6];

    public static bool InCave => Area.Current != null && Area.Current.areaName == "Viidakon luola";

    void Awake()
    {
        // TESTAUS: soihtu heti mukaan (poista tämä kun testaus on ohi; muuten se ostetaan tai löydetään)
        if (!Clues.Has(TorchId)) Clues.Add(TorchId, TorchName, "Soihtu. Sytytä se taukovalikosta pimeässä luolassa, niin valo seuraa sinua.", null, false);
    }

    void LateUpdate()
    {
        bool on = material != null && InCave;
        if (!on) { if (quad != null) quad.enabled = false; if (Area.Current != null && !InCave) TorchLit = false; return; }
        var cam = Camera.main; if (cam == null) return;
        if (quad == null || quad.transform.parent != cam.transform)
        {
            if (quad != null) Destroy(quad.gameObject);
            var go = new GameObject("Luolan pimeys"); go.transform.SetParent(cam.transform, false); go.transform.localPosition = new Vector3(0f, 0f, 2f);
            if (white == null)
            {
                var tx = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++) tx.SetPixel(x, y, Color.white);
                tx.Apply(); white = Sprite.Create(tx, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            }
            quad = go.AddComponent<SpriteRenderer>(); quad.sprite = white; quad.sortingOrder = 5000; quad.sharedMaterial = material;
        }
        quad.enabled = true;
        float h = cam.orthographicSize * 2f; quad.transform.localScale = new Vector3(h * cam.aspect * 1.2f, h * 1.2f, 1f);
        float u = tileWidth / 1672f, t = Time.time;
        float flick = 1f + 0.05f * Mathf.Sin(t * 13f) + 0.035f * Mathf.Sin(t * 29f + 1f) + 0.03f * Mathf.Sin(t * 7.3f + 2f);
        for (int i = 0; i < lights.Length; i++) lights[i] = Vector4.zero;
        int n = 0;
        foreach (var p in Players.All)
        {
            if (p == null || !p.enabled || n >= 4) continue;
            float r = TorchLit ? torchRadius * flick : baseRadius;
            lights[n++] = new Vector4(p.transform.position.x, p.transform.position.y + 1.6f, r, 1f);
        }
        foreach (var e in Enemy.All)
        {
            if (e == null || !e.ally || e.IsDead || n >= 6) continue;
            lights[n++] = new Vector4(e.transform.position.x, e.transform.position.y + 1.6f, TorchLit ? 3.2f : 1.8f, 0.9f);
        }
        quad.sharedMaterial.SetVectorArray("_Lights", lights);
        quad.sharedMaterial.SetVector("_Zone", new Vector4(caveX0 + zonePx.x * u, caveX0 + zonePx.y * u, caveX0 + zonePx.z * u, caveX0 + zonePx.w * u));
    }

    void OnDisable() { TorchLit = false; }
}
