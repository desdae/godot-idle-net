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
            new Color(0.08f, 0.11f, 0.08f, 0.14f),
            new Color(0.32f, 0.40f, 0.28f, 0.34f),
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
