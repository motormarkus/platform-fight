using UnityEngine;

// Each bar references its own settings asset; rules and table UI are shared.
[CreateAssetMenu(menuName = "Total Fist/Pokerin asetukset")]
public sealed class PokerPresentationSettings : ScriptableObject
{
    [Min(1)] public int smallBlind = 1, bigBlind = 2, buyIn = 50;
    public Texture2D barBackground;
    [Tooltip("User-supplied 1280x720 empty table + four opponents. White removed by material, source kept intact.")]
    public Texture2D tableAndOpponents;
    public Material whiteKeyMaterial;
    public Texture2D panelAtlas;
    public Texture2D originalLogo;
    public Font headingFont, textFont;
    [Tooltip("Card ID: clubs 2..A, diamonds 2..A, hearts 2..A, spades 2..A. Leave empty for text placeholders.")]
    public Sprite[] cardFaces = new Sprite[52];
    public Sprite cardBack, chip, dealerMarker;
    [Tooltip("3D chip prefabs and their matching integer values, ascending.")]
    public GameObject[] chipPrefabs = new GameObject[0];
    public int[] chipValues = new int[0];
    [Tooltip("Optional prefab implementing PokerCardPresenter for 3D cards. Faces use the same card ID mapping.")]
    public PokerCardPresenter cardPrefab;
    [Tooltip("Optional world camera/render texture display for perspective 3D cards.")]
    public RenderTexture cardRenderTexture;
    public Vector3 cardTableEuler = new Vector3(58, 0, 0);
    [Min(.01f)] public float cardWorldScale = .55f;
    public AudioClip dealSound, chipSound;
}
