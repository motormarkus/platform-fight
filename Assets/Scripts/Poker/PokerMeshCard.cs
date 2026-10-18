using UnityEngine;

// A usable adapter for CGTrader models with separate face/back renderers.
public sealed class PokerMeshCard : PokerCardPresenter
{
    public Renderer face, back;
    public Transform visualRoot;
    public Material[] faces = new Material[52];
    public Material backMaterial;
    public override void SetCard(int cardId, bool faceUp)
    {
        if (visualRoot != null) visualRoot.localRotation = Quaternion.Euler(0, faceUp ? 0 : 180, 0);
        if (face != null)
        {
            face.enabled = faceUp;
            if (faceUp && cardId >= 0 && cardId < faces.Length && faces[cardId] != null) face.sharedMaterial = faces[cardId];
        }
        if (back != null) { back.enabled = true; if (backMaterial != null) back.sharedMaterial = backMaterial; }
    }
}
