using Godot;

namespace IdleNet;

public partial class MapPanel : Control
{
    private MapToolbar? _mapToolbar;

    public MapToolbar? MapToolbar => _mapToolbar;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _mapToolbar = GetNode<MapToolbar>("MapToolbar");

        PanelContainer backdrop = GetNode<PanelContainer>("Backdrop");
        backdrop.MouseFilter = MouseFilterEnum.Ignore;
        backdrop.AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.03f, 0.07f, 0.05f, 0.82f),
            new Color(0.20f, 0.31f, 0.23f, 0.50f),
            24,
            0,
            0));

        ApplyMousePassThrough(_mapToolbar);
    }

    private static void ApplyMousePassThrough(Control? root)
    {
        if (root is null)
        {
            return;
        }

        root.MouseFilter = MouseFilterEnum.Ignore;
        foreach (Node child in root.GetChildren())
        {
            if (child is Control control)
            {
                ApplyMousePassThrough(control);
            }
        }
    }
}
