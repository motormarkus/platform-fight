using UnityEngine;

/// <summary>Erikokoiset risupallot pyörivät tuulen mukana oikealta vasemmalle.</summary>
public class DesertTumbleweeds : MonoBehaviour
{
    public Area area;
    public Sprite sprite;
    public AudioClip rollSound;
    [Range(0f, 1f)] public float rollVolume = 0.08f;
    [Range(1, 8)] public int count = 4;
    public float minDiameter = 0.65f, maxDiameter = 1.35f;
    public float minSpeed = 2.4f, maxSpeed = 3.8f;
    public float kickSpeed = 13f, kickUpSpeed = 8f, kickGravity = 11f;
    static DesertTumbleweeds instance;

    class Ball
    {
        public Transform ground, body;
        public SpriteRenderer art, shadow;
        public AudioSource audio;
        public float radius, speed, depth, phase, angle, delay;
        public bool rolling;
        public TumbleweedFlight motion;
    }

    Ball[] balls;
    bool entered;

    void Awake() { instance = this; }

    public static bool TryKick(Vector3 playerPosition, float direction, float reach, float depthRange)
    {
        var owner = instance;
        if (owner == null || !owner.isActiveAndEnabled || Area.Current != owner.area || owner.balls == null) return false;
        // Viimeinen pallo on suurin; muut jäävät tuulen mukana vieriviksi koristeiksi.
        var ball = owner.balls[owner.balls.Length - 1];
        if (!ball.rolling || !ball.ground.gameObject.activeInHierarchy || ball.motion.Airborne) return false;
        Vector3 p = ball.ground.position;
        float dx = (p.x - playerPosition.x) * direction;
        if (dx < -0.3f || dx > reach + ball.radius || Mathf.Abs(p.y - playerPosition.y) > depthRange + 0.2f) return false;
        if (!ball.motion.Kick(owner.kickSpeed, owner.kickUpSpeed, owner.kickGravity)) return false;
        HitSpark.Spawn(new Vector3(p.x, p.y + ball.radius, 0f), false, ball.art.sortingOrder + 1);
        return true;
    }

    void Start()
    {
        if (area == null) area = GetComponent<Area>();
        if (area == null || sprite == null) { enabled = false; return; }
        balls = new Ball[Mathf.Clamp(count, 1, 8)];
        float spriteSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        for (int i = 0; i < balls.Length; i++)
        {
            float depth = (i + 0.5f) / balls.Length;
            float diameter = Mathf.Lerp(minDiameter, maxDiameter, depth);
            var ground = new GameObject("Risupallo " + (i + 1)).transform;
            ground.SetParent(transform, false);
            var art = new GameObject("Pyörivä risupallo").AddComponent<SpriteRenderer>();
            art.transform.SetParent(ground, false);
            art.sprite = sprite;
            art.transform.localScale = Vector3.one * (diameter / Mathf.Max(0.01f, spriteSize));
            var shadow = new GameObject("Varjo").AddComponent<SpriteRenderer>();
            shadow.transform.SetParent(ground, false);
            shadow.sprite = PlayerController.CreateShadowSprite();
            shadow.color = new Color(0.25f, 0.15f, 0.08f, 0.24f);
            shadow.transform.localScale = new Vector3(diameter * 0.8f, diameter * 0.18f, 1f);
            var audio = ground.gameObject.AddComponent<AudioSource>();
            audio.clip = rollSound; audio.loop = true; audio.playOnAwake = false;
            audio.spatialBlend = 0f; audio.volume = 0f;
            balls[i] = new Ball { ground = ground, body = art.transform, art = art, shadow = shadow,
                audio = audio,
                radius = diameter * 0.5f, depth = depth, speed = Mathf.Lerp(maxSpeed, minSpeed, depth),
                phase = Random.Range(0f, Mathf.PI * 2f), angle = Random.Range(0f, 360f) };
            ground.position = new Vector3(0f, Mathf.Lerp(area.maxDepthY - 0.25f, area.minDepthY + 0.3f, depth), 0f);
            ground.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        var cam = Camera.main;
        if (balls == null || cam == null) return;
        bool show = Area.Current == area;
        if (!show)
        {
            foreach (var ball in balls) { ball.audio.Stop(); ball.ground.gameObject.SetActive(false); }
            entered = false;
            return;
        }
        float halfWidth = cam.orthographicSize * cam.aspect;
        float right = cam.transform.position.x + halfWidth;
        float left = cam.transform.position.x - halfWidth;
        if (!entered)
        {
            entered = true;
            for (int i = 0; i < balls.Length; i++)
            {
                var ball = balls[i];
                ball.rolling = false;
                ball.delay = i * 2.2f;
            }
        }
        foreach (var ball in balls)
        {
            if (!ball.rolling)
            {
                ball.delay -= Time.deltaTime;
                if (ball.delay > 0f) continue;
                Vector3 start = ball.ground.position;
                start.x = right + ball.radius + 0.3f;
                ball.ground.position = start;
                ball.rolling = true;
                ball.motion = new TumbleweedFlight(ball.speed);
                ball.ground.gameObject.SetActive(true);
                if (rollSound != null)
                {
                    ball.audio.time = Random.Range(0f, rollSound.length * 0.8f);
                    ball.audio.Play();
                }
            }
            float travelled = ball.motion.Step(Time.deltaTime);
            ball.ground.position += Vector3.right * travelled;
            // Vasemmalle vierivä pallo pyörii vastapäivään; pienempi pyörii nopeammin.
            ball.angle = Mathf.Repeat(ball.angle - travelled / ball.radius * Mathf.Rad2Deg, 360f);
            float hop = Mathf.Abs(Mathf.Sin(Time.time * ball.speed * 3f + ball.phase)) * 0.06f;
            ball.body.localPosition = new Vector3(0f, ball.radius + ball.motion.Height + (ball.motion.Airborne ? 0f : hop), 0f);
            ball.body.localRotation = Quaternion.Euler(0f, 0f, ball.angle);
            ball.art.sortingOrder = Mathf.RoundToInt(-ball.ground.position.y * 100f);
            ball.shadow.sortingOrder = ball.art.sortingOrder - 1;
            float screenX = ball.ground.position.x - cam.transform.position.x;
            float audible = Mathf.Clamp01((halfWidth - Mathf.Abs(screenX)) / 2f);
            float loudness = rollVolume * audible * Mathf.Lerp(0.55f, 1f, ball.radius / (maxDiameter * 0.5f));
            ball.audio.volume = Time.deltaTime > 0f && !ball.motion.Airborne ? loudness : 0f;
            ball.audio.panStereo = Mathf.Clamp(screenX / Mathf.Max(1f, halfWidth), -0.75f, 0.75f);
            bool leftScreen = ball.motion.VelocityX < 0f && ball.ground.position.x < left - ball.radius - 1f;
            bool rightScreen = ball.motion.VelocityX > 0f && ball.ground.position.x > right + ball.radius + 1f;
            if (!ball.motion.Airborne && (leftScreen || rightScreen))
            {
                ball.rolling = false;
                ball.delay = Random.Range(3f, 8f);
                ball.speed = Mathf.Lerp(maxSpeed, minSpeed, ball.depth);
                ball.audio.Stop();
                ball.ground.gameObject.SetActive(false);
            }
        }
    }

    void OnDisable()
    {
        if (balls == null) return;
        foreach (var ball in balls) if (ball.audio != null) ball.audio.Stop();
    }

    void OnDestroy() { if (instance == this) instance = null; }
}
