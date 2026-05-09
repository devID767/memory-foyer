using UnityEngine;

namespace MemoryFoyer.Presentation.Foyer
{
    [CreateAssetMenu(menuName = "MemoryFoyer/Art Palette Config", fileName = "ArtPaletteConfig")]
    public sealed class ArtPaletteConfig : ScriptableObject
    {
        [field: SerializeField, ColorUsage(false)] public Color Accent { get; private set; } = new(0.690f, 0.353f, 0.165f, 1f);
        [field: SerializeField, ColorUsage(false)] public Color Paper { get; private set; } = new(0.949f, 0.906f, 0.816f, 1f);
        [field: SerializeField, ColorUsage(false)] public Color Rest { get; private set; } = new(0.431f, 0.400f, 0.341f, 1f);
        [field: SerializeField, ColorUsage(false)] public Color PaperRested { get; private set; } = new(0.867f, 0.824f, 0.733f, 1f); // #DDD2BB
        [field: SerializeField, ColorUsage(false)] public Color Ink { get; private set; } = new(0.196f, 0.176f, 0.157f, 1f); // #322D28 — neutral dark for body text
    }
}
