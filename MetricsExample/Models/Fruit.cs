namespace MetricsExample.Models;

public record Fruit(string Name, Guid Guid)
{
    public static string[] KnownFruits =
        { "Apple", "Pear", "Banana", "Pineapple", "Lemon", "Kiwi", "Dragonfruit", "Watermelon" };
};
