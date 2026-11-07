using System.Windows;
using System.Windows.Media;

namespace OPNX.Samples;

internal static class SampleTheme
{
    public static readonly string[] Names = ["OPNX.V Dark", "Graphite", "Light"];
    public static readonly string[] Accents = ["Blue", "Teal", "Amber"];

    // Independent sample palettes, following V's Common.Brush resource + style approach.
    public static void Apply(string name, string accent)
    {
        string[] colors = name switch
        {
            "Light" => ["#F1F4F9", "#FFFFFF", "#E9EEF6", "#CCD5E2", "#19293D", "#53657C", "#DBE7F7", "#CDE0FC", "#087748", "#C02D45"],
            "Graphite" => ["#15171D", "#20232C", "#292E3A", "#3B4352", "#F0F3F9", "#ABB5C6", "#343D4E", "#3C4C67", "#55D9A2", "#FF7890"],
            _ => ["#0E1621", "#0F1E2D", "#18283A", "#2A4058", "#F1F5FA", "#8FA3B8", "#202E3D", "#26384A", "#47D99A", "#FF7890"]
        };
        string[] keys = ["Background", "Panel.Background", "Control.Background", "Border", "Foreground", "Muted", "Control.MouseOverBackground", "Control.PressedBackground", "Status.Connected", "Status.Error"];
        for (int i = 0; i < keys.Length; i++)
            Set(keys[i], colors[i]);
        Set("Accent", accent switch { "Teal" => "#008878", "Amber" => "#A96000", _ => "#4A6FE3" });
        Set("OnAccent", "#FFFFFF");
        Set("Transparent", "#00000000");
    }

    private static void Set(string name, string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        Application.Current.Resources["Common.Brush." + name] = brush;
    }
}
