using UnityEngine;

/// Implement this adapter on a supplied 3D card prefab. ID -1 = face down.
/// Its camera should render only the card layer into settings.cardRenderTexture.
public abstract class PokerCardPresenter : MonoBehaviour
{
    public abstract void SetCard(int cardId, bool faceUp);
    /// Himmentää kortin (ei-voittava kortti loppunäytöksessä).
    public virtual void SetDim(bool dim) { }
}
