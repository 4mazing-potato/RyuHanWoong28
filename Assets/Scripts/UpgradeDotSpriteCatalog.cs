using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/Upgrade Dot Sprite Catalog")]
public class UpgradeDotSpriteCatalog : ScriptableObject
{
    [SerializeField] private Sprite inactiveSprite;
    [SerializeField] private Sprite activeSprite;

    public Sprite InactiveSprite => inactiveSprite;
    public Sprite ActiveSprite => activeSprite;
}
