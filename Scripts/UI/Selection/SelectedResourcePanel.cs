using System;
using Godot;

namespace IdleNet;

public partial class SelectedResourcePanel : PanelContainer
{
    public event Action? PrimaryActionRequested;
    public event Action? SecondaryActionRequested;
    public event Action? CancelRequested;

    [Export]
    public PackedScene? StatChipScene { get; set; }

    private PanelContainer? _iconMedallion;
    private Label? _iconLabel;
    private Label? _titleLabel;
    private PanelContainer? _tagPanel;
    private Label? _tagLabel;
    private Label? _subtitleLabel;
    private PanelContainer? _headerDivider;
    private GridContainer? _statsGrid;
    private PanelContainer? _progressionPanel;
    private Label? _progressionIcon;
    private Label? _progressionLabel;
    private PanelContainer? _actionNotePanel;
    private Label? _actionNoteLabel;
    private Button? _primaryButton;
    private Button? _secondaryButton;
    private Button? _cancelButton;
    private Tween? _panelTween;
    private Tween? _primaryPulseTween;

    public override void _Ready()
    {
        _iconMedallion = GetNode<PanelContainer>("OuterMargin/RootColumn/HeaderRow/IconMedallion");
        _iconLabel = GetNode<Label>("OuterMargin/RootColumn/HeaderRow/IconMedallion/IconLabel");
        _titleLabel = GetNode<Label>("OuterMargin/RootColumn/HeaderRow/HeaderText/TitleRow/TitleLabel");
        _tagPanel = GetNode<PanelContainer>("OuterMargin/RootColumn/HeaderRow/HeaderText/TitleRow/TagPanel");
        _tagLabel = GetNode<Label>("OuterMargin/RootColumn/HeaderRow/HeaderText/TitleRow/TagPanel/TagLabel");
        _subtitleLabel = GetNode<Label>("OuterMargin/RootColumn/HeaderRow/HeaderText/SubtitleLabel");
        _headerDivider = GetNode<PanelContainer>("OuterMargin/RootColumn/HeaderDivider");
        _statsGrid = GetNode<GridContainer>("OuterMargin/RootColumn/StatsGrid");
        _progressionPanel = GetNode<PanelContainer>("OuterMargin/RootColumn/ProgressionPanel");
        _progressionIcon = GetNode<Label>("OuterMargin/RootColumn/ProgressionPanel/ProgressionMargin/ProgressionRow/ProgressionIcon");
        _progressionLabel = GetNode<Label>("OuterMargin/RootColumn/ProgressionPanel/ProgressionMargin/ProgressionRow/ProgressionLabel");
        _actionNotePanel = GetNode<PanelContainer>("OuterMargin/RootColumn/ActionNotePanel");
        _actionNoteLabel = GetNode<Label>("OuterMargin/RootColumn/ActionNotePanel/ActionNoteMargin/ActionNoteLabel");
        _primaryButton = GetNode<Button>("OuterMargin/RootColumn/ActionButtons/PrimaryButton");
        _secondaryButton = GetNode<Button>("OuterMargin/RootColumn/ActionButtons/SecondaryButton");
        _cancelButton = GetNode<Button>("OuterMargin/RootColumn/ActionButtons/CancelButton");

        MouseFilter = MouseFilterEnum.Stop;

        _primaryButton.Pressed += () => PrimaryActionRequested?.Invoke();
        _secondaryButton.Pressed += () => SecondaryActionRequested?.Invoke();
        _cancelButton.Pressed += () => CancelRequested?.Invoke();
    }

    public void SetData(SelectedResourcePanelViewData data)
    {
        if (_iconMedallion is null || _iconLabel is null || _titleLabel is null || _tagPanel is null || _tagLabel is null ||
            _subtitleLabel is null || _headerDivider is null || _statsGrid is null || _progressionPanel is null || _progressionIcon is null ||
            _progressionLabel is null || _actionNotePanel is null || _actionNoteLabel is null || _primaryButton is null || _secondaryButton is null ||
            _cancelButton is null || StatChipScene is null)
        {
            if (!IsInsideTree())
            {
                return;
            }

            _Ready();
        }

        PanelContainer iconMedallion = _iconMedallion!;
        Label iconLabel = _iconLabel!;
        Label titleLabel = _titleLabel!;
        PanelContainer tagPanel = _tagPanel!;
        Label tagLabel = _tagLabel!;
        Label subtitleLabel = _subtitleLabel!;
        PanelContainer headerDivider = _headerDivider!;
        GridContainer statsGrid = _statsGrid!;
        PanelContainer progressionPanel = _progressionPanel!;
        Label progressionIcon = _progressionIcon!;
        Label progressionLabel = _progressionLabel!;
        PanelContainer actionNotePanel = _actionNotePanel!;
        Label actionNoteLabel = _actionNoteLabel!;
        Button primaryButton = _primaryButton!;
        Button secondaryButton = _secondaryButton!;
        Button cancelButton = _cancelButton!;
        PackedScene statChipScene = StatChipScene!;

        AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateFrameStyle(data.AccentColor));

        iconMedallion.AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateMedallionStyle(data.AccentColor));
        iconLabel.Text = data.IconGlyph;
        iconLabel.AddThemeColorOverride("font_color", new Color(0.19f, 0.11f, 0.05f));
        iconLabel.AddThemeFontSizeOverride("font_size", 22);

        titleLabel.Text = data.Title;
        titleLabel.AddThemeColorOverride("font_color", new Color(0.98f, 0.92f, 0.79f));
        titleLabel.AddThemeColorOverride("font_shadow_color", new Color(0.08f, 0.05f, 0.03f, 0.84f));
        titleLabel.AddThemeConstantOverride("shadow_offset_x", 1);
        titleLabel.AddThemeConstantOverride("shadow_offset_y", 1);
        titleLabel.AddThemeFontSizeOverride("font_size", 20);

        tagPanel.Visible = data.ShowTag && !string.IsNullOrWhiteSpace(data.TagText);
        tagPanel.AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateBadgeStyle(data.AccentColor));
        tagLabel.Text = data.TagText;
        tagLabel.AddThemeColorOverride("font_color", new Color(0.20f, 0.11f, 0.05f));
        tagLabel.AddThemeFontSizeOverride("font_size", 10);

        subtitleLabel.Text = data.Subtitle;
        subtitleLabel.AddThemeColorOverride("font_color", new Color(0.88f, 0.82f, 0.74f));
        subtitleLabel.AddThemeFontSizeOverride("font_size", 12);
        subtitleLabel.TooltipText = data.Subtitle;
        headerDivider.AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateDividerStyle());

        foreach (Node child in statsGrid.GetChildren())
        {
            child.QueueFree();
        }

        foreach (SelectionStatChipViewData stat in data.Stats)
        {
            SelectionStatChip chip = statChipScene.Instantiate<SelectionStatChip>();
            statsGrid.AddChild(chip);
            chip.SetData(stat);
        }

        progressionPanel.AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateCalloutStyle(data.AccentColor, data.EmphasizeProgression));
        progressionIcon.Text = data.EmphasizeProgression ? "!" : "+";
        progressionIcon.AddThemeColorOverride("font_color", data.EmphasizeProgression
            ? new Color(0.23f, 0.12f, 0.05f)
            : new Color(0.91f, 0.82f, 0.61f));
        progressionIcon.AddThemeFontSizeOverride("font_size", 12);
        progressionLabel.Text = data.ProgressionText;
        progressionLabel.TooltipText = string.IsNullOrWhiteSpace(data.ProgressionTooltipText) ? data.ProgressionText : data.ProgressionTooltipText;
        progressionLabel.AddThemeColorOverride("font_color", data.EmphasizeProgression
            ? new Color(0.21f, 0.11f, 0.05f)
            : new Color(0.93f, 0.86f, 0.74f));
        progressionLabel.AddThemeFontSizeOverride("font_size", 11);

        actionNotePanel.Visible = !string.IsNullOrWhiteSpace(data.ActionNoteText);
        actionNotePanel.AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.22f, 0.16f, 0.10f, 0.90f),
            new Color(0.58f, 0.44f, 0.24f, 0.62f),
            10,
            5,
            4));
        actionNoteLabel.Text = data.ActionNoteText;
        actionNoteLabel.TooltipText = data.ActionNoteText;
        actionNoteLabel.AddThemeColorOverride("font_color", new Color(0.96f, 0.89f, 0.68f));
        actionNoteLabel.AddThemeFontSizeOverride("font_size", 11);

        ConfigureButton(primaryButton, data.PrimaryAction, data.AccentColor, true, new Vector2(0.0f, 40.0f), 16);
        ConfigureButton(secondaryButton, data.SecondaryAction, data.AccentColor.Darkened(0.10f), false, new Vector2(0.0f, 30.0f), 13);

        cancelButton.Visible = data.ShowCancelAction;
        cancelButton.Text = data.CancelActionText;
        cancelButton.Disabled = false;
        SelectionPanelStyles.ApplyActionButtonStyle(cancelButton, new Color(0.63f, 0.48f, 0.28f), false, new Vector2(0.0f, 24.0f), 11);

        Visible = true;
        AnimateShow();
    }

    public void HidePanel()
    {
        Visible = false;
    }

    private void ConfigureButton(Button button, SelectedResourcePanelActionViewData action, Color accent, bool primary, Vector2 minimumSize, int fontSize)
    {
        button.Visible = action.Visible;
        button.Text = action.Text;
        button.TooltipText = action.TooltipText;
        button.Disabled = !action.Enabled;
        SelectionPanelStyles.ApplyActionButtonStyle(button, accent, primary, minimumSize, fontSize);
    }

    private void AnimateShow()
    {
        _panelTween?.Kill();
        _primaryPulseTween?.Kill();

        Modulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        Scale = new Vector2(0.98f, 0.98f);
        _panelTween = CreateTween();
        _panelTween.SetParallel(true);
        _panelTween.TweenProperty(this, "modulate", Colors.White, 0.16);
        _panelTween.TweenProperty(this, "scale", Vector2.One, 0.18);

        if (_primaryButton is null || _primaryButton.Disabled)
        {
            return;
        }

        _primaryButton.Scale = new Vector2(0.98f, 0.98f);
        _primaryPulseTween = CreateTween();
        _primaryPulseTween.TweenProperty(_primaryButton, "scale", new Vector2(1.02f, 1.02f), 0.10);
        _primaryPulseTween.TweenProperty(_primaryButton, "scale", Vector2.One, 0.12);
    }
}
