using UnityEngine;
using UnityEngine.Video;

// World-space joining table only. The live hand is still rendered by PokerView.
[RequireComponent(typeof(SpriteRenderer), typeof(VideoPlayer))]
public sealed class PokerTableVideo : MonoBehaviour
{
    public Area area;
    public VideoClip clip;
    VideoPlayer video;
    SpriteRenderer body;
    Material original, material;
    RenderTexture texture;
    bool preparing, failed;

    void OnEnable()
    {
        body = GetComponent<SpriteRenderer>(); video = GetComponent<VideoPlayer>();
        original = body.sharedMaterial;
        if (original == null) { enabled = false; return; }
        material = new Material(original); body.sharedMaterial = material;
        video.playOnAwake = false; video.isLooping = true; video.waitForFirstFrame = true;
        video.audioOutputMode = VideoAudioOutputMode.None; video.source = VideoSource.VideoClip; video.clip = clip;
        video.renderMode = VideoRenderMode.RenderTexture; video.skipOnDrop = true;
        video.errorReceived += Error;
    }

    void Update()
    {
        if (failed || clip == null || material == null) return;
        bool active = (area == null || Area.Current == area) && Time.timeScale > 0 && !PokerService.BlocksCombat;
        if (!active) { if (video.isPlaying) video.Pause(); return; }
        if (!preparing)
        {
            texture = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32);
            texture.name = "Poker table video"; texture.Create(); video.targetTexture = texture;
            material.SetTexture("_VideoTex", texture); video.Prepare(); preparing = true;
        }
        if (!video.isPrepared) return;
        if (!video.isPlaying) video.Play();
        if (video.frame >= 0) material.SetFloat("_UseVideo", 1);
    }

    void Error(VideoPlayer source, string message)
    {
        failed = true; material.SetFloat("_UseVideo", 0);
        Debug.LogWarning("Pokeripöydän video ei käynnistynyt; ensimmäinen ruutu jää näkyviin. " + message, this);
    }
    void OnDisable()
    {
        if (video != null) { video.errorReceived -= Error; video.Stop(); video.targetTexture = null; }
        if (body != null && original != null) body.sharedMaterial = original;
        if (texture != null) { texture.Release(); Destroy(texture); }
        if (material != null) Destroy(material);
        texture = null; material = null; preparing = false; failed = false;
    }
}
