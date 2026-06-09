using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Stages/Stage Sprite Catalog")]
public class StageSpriteCatalog : ScriptableObject
{
    [SerializeField] private Entry[] entries = Array.Empty<Entry>();

    public bool TryGetSprite(string imageName, out Sprite sprite)
    {
        sprite = null;
        if (string.IsNullOrWhiteSpace(imageName))
        {
            return false;
        }

        string trimmedName = imageName.Trim();
        for (int i = 0; i < entries.Length; i++)
        {
            Entry entry = entries[i];
            if (string.Equals(entry.ImageName, trimmedName, StringComparison.OrdinalIgnoreCase))
            {
                sprite = entry.Sprite;
                return sprite != null;
            }
        }

        return false;
    }

    [Serializable]
    private struct Entry
    {
        [SerializeField] private string imageName;
        [SerializeField] private Sprite sprite;

        public string ImageName => imageName;
        public Sprite Sprite => sprite;
    }
}
