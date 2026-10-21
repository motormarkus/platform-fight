using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Merimieskäniin (PlayerController.IsDrunk) liittyvät tehosteet: kuva väreilee (linssivääristymä + värihäiriö aaltoina), kaikki äänet matalammiksi
/// (-8 puolisävelaskelta) ja aika hidastuu. Tehoste tulee ja menee pehmeästi (viimeiset 2 s). Luo itsensä pelin alussa.
/// </summary>
public class DrunkFx : MonoBehaviour
{
    public const float PitchSemitones = -8f;
    public const float SlowScale = 0.6f;

    static DrunkFx instance;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (instance != null) return;
        var go = new GameObject("DrunkFx");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<DrunkFx>();
    }

    PlayerController[] heroes = new PlayerController[0];
    float nextFind, nextAudioFind, weight;
    AudioSource[] sources = new AudioSource[0];
    readonly Dictionary<AudioSource, Vector2> pitchOf = new Dictionary<AudioSource, Vector2>();   // x = lähde-pitch, y = meidän asettama
    float lastScaleSet = -1f;
    Volume volume; VolumeProfile profile; LensDistortion lens; ChromaticAberration chroma;

    void Update()
    {
        if (Time.unscaledTime >= nextFind) { nextFind = Time.unscaledTime + 0.5f; heroes = FindObjectsByType<PlayerController>(FindObjectsSortMode.None); }
        float target = 0f;
        foreach (var h in heroes)
            if (h != null && h.IsDrunk) target = Mathf.Max(target, Mathf.Clamp01(h.DrunkLeft / 2f));
        weight = Mathf.MoveTowards(weight, target, Time.unscaledDeltaTime * 1.2f);
        UpdateSlowMotion();
        UpdateVisual();
    }

    void LateUpdate() { UpdatePitch(); }

    void UpdateSlowMotion()
    {
        float want = Mathf.Lerp(1f, SlowScale, weight);
        float cur = Time.timeScale;
        bool ours = lastScaleSet >= 0f && Mathf.Abs(cur - lastScaleSet) < 0.001f;
        if (weight > 0.001f)
        {
            // jos joku muu (valikko, pysäytys, osumapysäytys) säätää aikaa, ei sotketa; palautuksessa vanha arvo on meidän
            if (ours || cur >= 0.99f) { Time.timeScale = want; lastScaleSet = want; }
        }
        else if (ours) { Time.timeScale = 1f; lastScaleSet = -1f; }
    }

    void UpdateVisual()
    {
        if (weight <= 0.001f && volume == null) return;
        if (volume == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            lens = profile.Add<LensDistortion>(true);
            lens.intensity.overrideState = true; lens.scale.overrideState = true; lens.xMultiplier.overrideState = true; lens.yMultiplier.overrideState = true; lens.center.overrideState = true;
            chroma = profile.Add<ChromaticAberration>(true);
            chroma.intensity.overrideState = true;
            volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true; volume.priority = 100f; volume.sharedProfile = profile; volume.weight = 0f;
        }
        volume.weight = weight;
        float t = Time.unscaledTime;
        lens.intensity.value = Mathf.Sin(t * 1.7f) * 0.55f + Mathf.Sin(t * 0.63f) * 0.2f;       // pullistuu ja supistuu aaltoina
        lens.scale.value = 1f - 0.08f * Mathf.Abs(Mathf.Sin(t * 1.1f));
        lens.xMultiplier.value = 0.7f + 0.3f * Mathf.Sin(t * 2.3f);
        lens.yMultiplier.value = 0.7f + 0.3f * Mathf.Cos(t * 1.9f);
        lens.center.value = new Vector2(0.5f + 0.12f * Mathf.Sin(t * 1.3f), 0.5f + 0.1f * Mathf.Cos(t * 0.9f));
        chroma.intensity.value = 0.45f + 0.35f * Mathf.Sin(t * 2.6f);
        var cam = Camera.main;
        if (cam != null && Time.unscaledTime - lastCamCheck > 1f)
        {
            lastCamCheck = Time.unscaledTime;
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null && !data.renderPostProcessing) data.renderPostProcessing = true;   // väreily vaatii jälkikäsittelyn
        }
        if (weight <= 0.001f) { volume.weight = 0f; }
    }
    float lastCamCheck = -10f;

    void UpdatePitch()
    {
        bool on = weight > 0.001f;
        if (!on && pitchOf.Count == 0) return;
        if (Time.unscaledTime >= nextAudioFind) { nextAudioFind = Time.unscaledTime + 0.4f; sources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None); }
        float mul = Mathf.Lerp(1f, Mathf.Pow(2f, PitchSemitones / 12f), weight);
        foreach (var s in sources)
        {
            if (s == null) continue;
            if (on)
            {
                Vector2 rec;
                // jos koodi on asettanut pitchin uudelleen (satunnaisvaihtelu), se on uusi lähdearvo
                if (!pitchOf.TryGetValue(s, out rec) || Mathf.Abs(s.pitch - rec.y) > 0.0005f) rec.x = s.pitch;
                rec.y = rec.x * mul; s.pitch = rec.y; pitchOf[s] = rec;
            }
            else if (pitchOf.TryGetValue(s, out var r))
            {
                if (Mathf.Abs(s.pitch - r.y) < 0.0005f) s.pitch = r.x;
                pitchOf.Remove(s);
            }
        }
        if (!on)
        {
            // jäljellä olevat (poistuneet) pois
            pitchOf.Clear();
        }
    }

    void OnDestroy()
    {
        if (lastScaleSet >= 0f && Mathf.Abs(Time.timeScale - lastScaleSet) < 0.001f) Time.timeScale = 1f;
        foreach (var kv in pitchOf) if (kv.Key != null) kv.Key.pitch = kv.Value.x;
    }
}
