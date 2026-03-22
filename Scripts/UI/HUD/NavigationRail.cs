using System;
using System.Collections.Generic;
using Godot;

namespace IdleNet;

public partial class NavigationRail : PanelContainer
{
    public event Action<HudSection>? SectionSelected;

    private readonly Dictionary<HudSection, Button> _buttons = new();
    private HudSection _activeSection = HudSection.Selection;

    public override void _Ready()
    {
        RegisterButton(HudSection.Selection, "OuterMargin/RootColumn/SelectionButton", new Color(0.52f, 0.78f, 0.45f));
        RegisterButton(HudSection.Queue, "OuterMargin/RootColumn/QueueButton", new Color(0.79f, 0.67f, 0.37f));
        RegisterButton(HudSection.Town, "OuterMargin/RootColumn/TownButton", new Color(0.80f, 0.59f, 0.36f));
        RegisterButton(HudSection.Buildings, "OuterMargin/RootColumn/BuildingsButton", new Color(0.73f, 0.63f, 0.41f));
        RegisterButton(HudSection.People, "OuterMargin/RootColumn/PeopleButton", new Color(0.55f, 0.71f, 0.90f));

        AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.14f, 0.10f, 0.07f, 0.94f),
            new Color(0.48f, 0.36f, 0.20f, 0.88f),
            20,
            8,
            8));

        SetActiveSection(_activeSection);
    }

    public void SetActiveSection(HudSection section)
    {
        _activeSection = section;
        foreach (KeyValuePair<HudSection, Button> pair in _buttons)
        {
            ApplyButtonState(pair.Value, pair.Key == section);
        }
    }

    private void RegisterButton(HudSection section, string path, Color accent)
    {
        Button button = GetNode<Button>(path);
        button.Pressed += () => SectionSelected?.Invoke(section);
        button.Alignment = HorizontalAlignment.Center;
        button.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        button.AddThemeColorOverride("font_hover_color", new Color(0.19f, 0.11f, 0.05f));
        button.AddThemeColorOverride("font_pressed_color", new Color(0.19f, 0.11f, 0.05f));
        button.SetMeta("accent", accent);
        _buttons[section] = button;
    }

    private static void ApplyButtonState(Button button, bool active)
    {
        Color accent = button.HasMeta("accent")
            ? (Color)button.GetMeta("accent")
            : new Color(0.75f, 0.62f, 0.36f);

        Color fontColor = active ? new Color(0.20f, 0.11f, 0.05f) : new Color(0.95f, 0.89f, 0.78f);
        Color baseColor = active
            ? accent.Lerp(new Color(0.92f, 0.80f, 0.52f, 0.98f), 0.32f)
            : new Color(0.27f, 0.19f, 0.12f, 0.92f);
        Color borderColor = active
            ? accent.Lerp(new Color(1.0f, 0.90f, 0.66f, 0.98f), 0.42f)
            : accent.Lerp(new Color(0.64f, 0.52f, 0.30f, 0.76f), 0.50f);

        button.AddThemeColorOverride("font_color", fontColor);
        button.AddThemeColorOverride("font_disabled_color", new Color(0.60f, 0.55f, 0.50f));
        button.AddThemeFontSizeOverride("font_size", active ? 13 : 12);
        button.AddThemeStyleboxOverride("normal", SelectionPanelStyles.CreateInsetStyle(baseColor, borderColor, 14, 6, 6));
        button.AddThemeStyleboxOverride("hover", SelectionPanelStyles.CreateInsetStyle(baseColor.Lightened(0.08f), borderColor.Lightened(0.08f), 14, 6, 6));
        button.AddThemeStyleboxOverride("pressed", SelectionPanelStyles.CreateInsetStyle(baseColor.Darkened(0.08f), borderColor, 14, 6, 6));
        button.AddThemeStyleboxOverride("focus", SelectionPanelStyles.CreateInsetStyle(baseColor.Lightened(0.08f), borderColor.Lightened(0.08f), 14, 6, 6));
        button.Scale = active ? new Vector2(1.03f, 1.03f) : Vector2.One;
    }
}
