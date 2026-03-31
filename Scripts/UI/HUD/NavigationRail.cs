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
        RegisterButton(HudSection.Buildings, "OuterMargin/RootColumn/BuildingsButton", new Color(0.73f, 0.63f, 0.41f));
        RegisterButton(HudSection.Queue, "OuterMargin/RootColumn/QueueButton", new Color(0.79f, 0.67f, 0.37f));
        RegisterButton(HudSection.Town, "OuterMargin/RootColumn/TownButton", new Color(0.80f, 0.59f, 0.36f));
        RegisterButton(HudSection.People, "OuterMargin/RootColumn/PeopleButton", new Color(0.64f, 0.78f, 0.53f));

        AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.08f, 0.11f, 0.08f, 1.0f),
            new Color(0.36f, 0.47f, 0.31f, 0.72f),
            22,
            12,
            10));

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
        button.FocusMode = Control.FocusModeEnum.All;
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
            ? accent.Lerp(new Color(0.92f, 0.84f, 0.58f, 0.98f), 0.30f)
            : new Color(0.15f, 0.18f, 0.14f, 0.90f);
        Color borderColor = active
            ? accent.Lerp(new Color(1.0f, 0.92f, 0.68f, 0.98f), 0.40f)
            : accent.Lerp(new Color(0.46f, 0.57f, 0.40f, 0.72f), 0.38f);

        button.AddThemeColorOverride("font_color", fontColor);
        button.AddThemeColorOverride("font_disabled_color", new Color(0.60f, 0.55f, 0.50f));
        button.AddThemeFontSizeOverride("font_size", active ? 13 : 12);
        button.AddThemeStyleboxOverride("normal", SelectionPanelStyles.CreateInsetStyle(baseColor, borderColor, 16, 12, 9));
        button.AddThemeStyleboxOverride("hover", SelectionPanelStyles.CreateInsetStyle(baseColor.Lightened(0.08f), borderColor.Lightened(0.08f), 16, 12, 9));
        button.AddThemeStyleboxOverride("pressed", SelectionPanelStyles.CreateInsetStyle(baseColor.Darkened(0.08f), borderColor, 16, 12, 9));
        button.AddThemeStyleboxOverride("focus", SelectionPanelStyles.CreateInsetStyle(baseColor.Lightened(0.08f), borderColor.Lightened(0.08f), 16, 12, 9));
        button.Scale = active ? new Vector2(1.01f, 1.01f) : Vector2.One;
    }
}
