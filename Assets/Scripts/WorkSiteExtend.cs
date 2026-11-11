using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Työmaan bussipysäkin tie jatkuu oikealle (tyhjää tietä rekkaa varten). Ajonaikainen jatko, ettei viidakon valikkoa tarvitse ajaa uudelleen:
/// ottaa pysäkkikuvan (Työmaa 3) tyhjän vasemman osan ja liittää sen peilikuvana kuvan perään, jolloin pysäkki ei toistu.
/// Alueen kävely- ja kamerarajat leventyvät, taka-aita jatkuu. Jatko tehdään vain kerran per scene.
/// </summary>
public static class WorkSiteExtend
{
    const string ExtName = "Työmaan tien jatko";
    const float CropPx = 820f;   // pysäkin vasemman reunan (noin 870 px) vasemmalla puolella oleva tyhjä tie

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init() { SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded; Apply(); }
    static void OnLoaded(Scene s, LoadSceneMode m) { Apply(); }

    static void Apply()
    {
        foreach (var a in Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (a == null || a.areaName != "Työmaa") continue;
            Scene sc = a.gameObject.scene;
            GameObject root = null, last = null;
            foreach (var g in sc.GetRootGameObjects())
            {
                if (g.name == "Työmaa") root = g;
                if (g.name == ExtName) { root = null; last = g; break; }
            }
            if (last != null || root == null) continue;   // jo tehty tai työmaan kuvia ei löydy
            var tile3 = root.transform.Find("Työmaa 3");
            var sr3 = tile3 != null ? tile3.GetComponent<SpriteRenderer>() : null;
            if (sr3 == null || sr3.sprite == null) continue;

            var src = sr3.sprite;
            float ppu = src.pixelsPerUnit, w3 = src.bounds.size.x;
            Rect full = src.rect;
            float cropPx = Mathf.Min(CropPx, full.width * 0.5f);
            // pala = kuvan vasen tyhjä osa; sama texture, vain alirajaus (ei uutta kuvaa)
            var piece = Sprite.Create(src.texture, new Rect(full.x, full.y, cropPx, full.height), new Vector2(0f, 0.5f), ppu);
            float pieceW = cropPx / ppu * tile3.localScale.x;

            var ext = new GameObject(ExtName);
            SceneManager.MoveGameObjectToScene(ext, sc);
            float rightEdge = tile3.position.x + w3 * tile3.localScale.x * 0.5f;   // kuvan 3 oikea reuna
            // ensimmäinen pala peilattuna (reuna, jossa kuva päättyi tyhjään tiehen -> sama reuna), toinen suorana; flipX peilaa pivotin mukaan oikealta vasemmalle
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject("Tiepala " + (i + 1));
                go.transform.SetParent(ext.transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = piece; sr.sortingOrder = sr3.sortingOrder; sr.sortingLayerID = sr3.sortingLayerID; sr.flipX = i == 0;
                sr.color = sr3.color; sr.sharedMaterial = sr3.sharedMaterial;
                // flipX: pivotti (0, 0.5) kääntyy oikealle puolelle, joten peilattu pala sijoitetaan vasen reuna = rightEdge + pieceW
                float left = rightEdge + pieceW * i;
                go.transform.position = new Vector3(i == 0 ? left + pieceW : left, tile3.position.y, 0f);
                go.transform.localScale = new Vector3(tile3.localScale.x, tile3.localScale.y, 1f);
            }

            float add = pieceW * 2f;
            a.walkMaxX += add; a.camMaxX += add;

            // taka-aita jatkuu: kopioidaan viimeistä palaa samalla välillä uuden reunan yli
            var fence = root.transform.Find("Aita (taka)");
            if (fence != null && fence.childCount >= 2)
            {
                var lastP = fence.GetChild(fence.childCount - 1); var prevP = fence.GetChild(fence.childCount - 2);
                float step = lastP.position.x - prevP.position.x;
                if (step > 0.5f)
                    for (float x = lastP.position.x + step; x < rightEdge + add + step * 0.5f; x += step)
                    {
                        var c = Object.Instantiate(lastP.gameObject, fence, false);
                        c.transform.position = new Vector3(x, lastP.position.y, lastP.position.z);
                    }
            }
            // taustakerrosten näkyvyys pitenee uuden kameran oikean reunan mukaan
            foreach (var p in root.GetComponentsInChildren<ParallaxLayer>(true)) if (p != null && p.maxX < a.camMaxX + 2f) p.maxX = a.camMaxX + 2f;
            // rekka pysäkin kyltin oikealle puolelle jatketulle tielle (kuvan 3 pysäkin kyltin oikea reuna on noin 1400 px)
            float signX = tile3.position.x + (1400f - full.width * 0.5f) * tile3.localScale.x / ppu;
            TruckRide.Create(sc, signX + 2.5f, a.maxDepthY - 0.4f, ppu, a);
        }
    }
}
