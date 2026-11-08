using UnityEngine;

/// <summary>
/// Jakaa yhden alueen kahteen osaan esteen (esim. Muurin keskitorni) kohdalta: alue on yksi, mutta kävely ja kamera pysähtyvät tornin päätyyn kummallakin puolella.
/// Vasen puoli: kävely ja kamera loppuvat tornin päätyyn; oikea puoli alkaa tornin toiselta puolelta. Pelaaja pääsee puolelta toiselle vain ovien kautta.
/// Puoli päätellään pääpelaajan paikasta; kun alue ei ole nykyinen, alkuperäiset rajat palautetaan.
/// </summary>
public class AreaSplit : MonoBehaviour
{
    public Area area;
    public float leftWalkMax, leftCamMax, rightWalkMin, rightCamMin;

    float oMinW, oMaxW, oMinC, oMaxC; bool init, applied;

    void Start() { Init(); }

    void Init()
    {
        if (init || area == null) return;
        oMinW = area.walkMinX; oMaxW = area.walkMaxX; oMinC = area.camMinX; oMaxC = area.camMaxX; init = true;
    }

    void Restore()
    {
        if (!applied) return;
        area.walkMinX = oMinW; area.walkMaxX = oMaxW; area.camMinX = oMinC; area.camMaxX = oMaxC; applied = false;
    }

    void OnDisable() { Restore(); }

    void Update()
    {
        Init();
        if (area == null) return;
        var p = Players.Primary;
        if (Area.Current != area || p == null) { Restore(); return; }
        bool left = p.transform.position.x < (leftWalkMax + rightWalkMin) * 0.5f;
        area.walkMinX = left ? oMinW : rightWalkMin; area.walkMaxX = left ? leftWalkMax : oMaxW;
        area.camMinX = left ? oMinC : rightCamMin; area.camMaxX = left ? leftCamMax : oMaxC;
        applied = true;
    }
}
