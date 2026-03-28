using TheRandomizer.Application.Enumerators;

namespace TheRandomizer.Maui.Utilities;

public static class TextSizeMap
{
    public static (Double Body, Double Title, Double Small) Get(FontSizes fontSize)
        => fontSize switch
        {
            FontSizes.Small => (12, 18, 10),
            FontSizes.Large => (16, 22, 14),
            FontSizes.ExtraLarge => (18, 24, 16),
            _ => (14, 20, 12)
        };
}

