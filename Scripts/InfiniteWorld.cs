using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Godot;

namespace IdleNet;

public partial class InfiniteWorld : Node2D
{
	private enum WorkerPhase
	{
		Idle,
		TravelingToResource,
		Gathering,
		ReturningToTown,
		ReturningToResource,
	}

	private static GameRulesDefinition Rules => GameCatalog.Rules;

	private static readonly Vector2I TownCell = new(0, 0);
	private static readonly Vector2I PlayerStartCell = new(0, 1);

	private static readonly Color GrassA = new(0.44f, 0.69f, 0.33f);
	private static readonly Color GrassB = new(0.39f, 0.63f, 0.29f);
	private static readonly Color GrassAccent = new(0.64f, 0.84f, 0.47f, 0.18f);
	private static readonly Color GridLine = new(0.10f, 0.14f, 0.10f, 0.18f);
	private static readonly Color TreeLeaf = new(0.20f, 0.45f, 0.19f);
	private static readonly Color TreeLeafLight = new(0.30f, 0.58f, 0.25f);
	private static readonly Color TreeTrunk = new(0.45f, 0.29f, 0.14f);
	private static readonly Color BerryFruit = new(0.80f, 0.20f, 0.40f);
	private static readonly Color BerryLeaf = new(0.28f, 0.58f, 0.25f);
	private static readonly Color StoneDark = new(0.50f, 0.55f, 0.59f);
	private static readonly Color StoneLight = new(0.69f, 0.73f, 0.77f);
	private static readonly Color TownWall = new(0.72f, 0.62f, 0.45f);
	private static readonly Color TownRoof = new(0.63f, 0.24f, 0.19f);
	private static readonly Color TownDoor = new(0.35f, 0.21f, 0.11f);
	private static readonly Color Highlight = new(1.00f, 0.97f, 0.63f, 0.38f);
	private static readonly Color FogColor = new(0.06f, 0.09f, 0.06f, 0.92f);
	private static readonly Color FrontierFogColor = new(0.11f, 0.17f, 0.11f, 0.74f);
	private static readonly Color FrontierOutline = new(0.91f, 0.87f, 0.52f, 0.45f);
	private static readonly Color QueuedExploreOverlay = new(0.44f, 0.78f, 0.97f, 0.28f);
	private static readonly Color QueuedExploreOutline = new(0.62f, 0.88f, 1.00f, 0.55f);

	private readonly TownState _townState = new(GameCatalog.Items, GameCatalog.Buildings, GameCatalog.Rules.StockpileCapacity);
	private readonly CharacterState _characterState = new(GameCatalog.Items, GameCatalog.Skills, GameCatalog.Rules.BagCapacity);
	private readonly HashSet<Vector2I> _exploredCells = new();

	private PlayerController? _player;
	private Label? _coordsLabel;
	private Label? _statusLabel;
	private Label? _hintLabel;
	private LeftSidebar? _leftSidebar;
	private SelectedResourcePanel? _selectedResourcePanel;
	private QueuePanel? _queuePanel;
	private TownUI? _townUi;
	private PanelContainer? _townPanel;
	private Label? _townBody;
	private Label? _townGoldLabel;
	private ScrollContainer? _townInfoScroll;
	private PanelContainer? _characterPanel;
	private Label? _characterSummaryLabel;
	private VBoxContainer? _characterSkillsList;
	private readonly Dictionary<string, Label> _skillLevelLabels = new();
	private readonly Dictionary<string, Label> _skillXpLabels = new();
	private readonly Dictionary<string, ProgressBar> _skillProgressBars = new();
	private ProgressBar? _stockpileProgressBar;
	private Label? _stockpileProgressLabel;
	private Label? _stockpileUpgradeLabel;
	private Button? _stockpileUpgradeButton;
	private ItemList? _sellItemList;
	private Label? _sellSelectionLabel;
	private HSlider? _sellPercentSlider;
	private Label? _sellPercentLabel;
	private Button? _sellButton;
	private string? _selectedSellItemId;
	private int _selectedSellPercent;
	private TownBuildingFilter _activeBuildingFilter = TownBuildingFilter.All;

	private Vector2I _selectedCell = PlayerStartCell;
	private Vector2I _actionCell = PlayerStartCell;
	private string? _actionResourceId;
	private string? _actionPrimaryResourceActionId;
	private string? _actionSecondaryResourceActionId;
	private WorkKind _actionWorkKind = WorkKind.Gather;
	private GatherCommand? _activeGatherCommand;
	private readonly List<GatherCommand> _queuedCommands = new();
	private bool _activeExploreRequirementPaid;
	private bool _activeTownUpgradePaid;
	private bool _queuePanelDirty = true;
	private double _queueSummaryRefreshSeconds;
	private double _gatherProgressSeconds;
	private double _runningXpAccumulator;
	private double _exploringXpAccumulator;
	private double _buildingXpAccumulator;
	private WorkerPhase _workerPhase = WorkerPhase.Idle;

	public override void _Ready()
	{
		_player = GetNode<PlayerController>("Player");
		_coordsLabel = GetNode<Label>("Hud/CoordsLabel");
		_hintLabel = GetNode<Label>("Hud/HintLabel");
		_townUi = GetNode<TownUI>("Hud/TownUI");

		_player.Initialize(PlayerStartCell, Rules.TileSize);
		InitializeExploration();
		_hintLabel.Text = $"Click the character, town, or a resource tile.\nMovement and gathering take 2 seconds. Bag: {_characterState.BagCapacity}. Stockpile: {_townState.StockpileCapacity}.";

		CreateHudPanels();
		ConnectTownUi();
		UpdateQueuePanel();
		UpdateTownPanel();
		UpdateStatus("Starter town at (0, 0). Click a nearby resource to begin.");
		RenderingServer.SetDefaultClearColor(new Color(0.13f, 0.19f, 0.13f));
	}

	public override void _Process(double delta)
	{
		UpdateRunningSkill(delta);
		UpdateCoordsLabel();
		ProcessGatherCommand(delta);
		UpdatePlayerProgressBar();

		if (_townUi?.Visible == true)
		{
			UpdateTownPanel();
		}

		if (_characterPanel?.Visible == true)
		{
			UpdateCharacterPanel();
		}

		if (_queuePanel?.Visible == true)
		{
			_queueSummaryRefreshSeconds += delta;
			if (_queuePanelDirty || _queueSummaryRefreshSeconds >= 0.25)
			{
				UpdateQueuePanel();
			}
		}

		QueueRedraw();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouseButton ||
			mouseButton.ButtonIndex != MouseButton.Left ||
			!mouseButton.Pressed)
		{
			return;
		}

		Vector2 worldPosition = ScreenToWorld(mouseButton.Position);
		Vector2I clickedCell = WorldToCell(worldPosition);
		_selectedCell = clickedCell;

		if (IsClickOnCharacter(worldPosition))
		{
			HideActionPanel();
			HideTownPanel();
			ShowCharacterPanel();
			UpdateStatus("Viewing character details.");
			return;
		}

		if (clickedCell == TownCell)
		{
			HideActionPanel();
			HideCharacterPanel();
			ShowTownPanel();
			UpdateStatus("Viewing starter town details.");
			return;
		}

		if (!IsExplored(clickedCell))
		{
			if (CanQueueExploreCell(clickedCell))
			{
				HideTownPanel();
				HideCharacterPanel();
				ShowExploreActionPanel(clickedCell);
				return;
			}

			HideActionPanel();
			HideTownPanel();
			HideCharacterPanel();
			UpdateStatus("That tile is still hidden. Explore adjacent frontier tiles first.");
			return;
		}

		string? resourceId = GetResourceId(clickedCell);
		if (resourceId is not null)
		{
			HideTownPanel();
			HideCharacterPanel();
			ShowActionPanel(clickedCell, GameCatalog.GetResource(resourceId));
			return;
		}

		HideActionPanel();
		HideTownPanel();
		HideCharacterPanel();
		UpdateStatus($"Tile ({clickedCell.X}, {clickedCell.Y}) is plain grass.");
	}

	public override void _Draw()
	{
		if (_player is null)
		{
			return;
		}

		Vector2 padding = new(Rules.TileSize * 2, Rules.TileSize * 2);
		Rect2 visibleWorldRect = GetVisibleWorldRect();
		Vector2 minWorld = visibleWorldRect.Position - padding;
		Vector2 maxWorld = visibleWorldRect.End + padding;

		int minX = Mathf.FloorToInt(minWorld.X / Rules.TileSize);
		int maxX = Mathf.CeilToInt(maxWorld.X / Rules.TileSize);
		int minY = Mathf.FloorToInt(minWorld.Y / Rules.TileSize);
		int maxY = Mathf.CeilToInt(maxWorld.Y / Rules.TileSize);

		for (int y = minY; y <= maxY; y++)
		{
			for (int x = minX; x <= maxX; x++)
			{
				Vector2I cell = new(x, y);
				Vector2 tileOrigin = new(x * Rules.TileSize, y * Rules.TileSize);

				DrawGrassTile(tileOrigin, cell);

				if (cell == _selectedCell)
				{
					DrawRect(new Rect2(tileOrigin, new Vector2(Rules.TileSize, Rules.TileSize)), Highlight);
				}

				if (!IsExplored(cell))
				{
					DrawUnexploredTile(tileOrigin, cell);
					continue;
				}

				if (cell == TownCell)
				{
					DrawTown(tileOrigin);
					continue;
				}

				string? resourceId = GetResourceId(cell);
				if (resourceId is not null)
				{
					DrawResource(tileOrigin, resourceId);
				}
			}
		}
	}

	public void OpenTownUi()
	{
		ShowTownPanel();
	}

	public void CloseTownUi()
	{
		HideTownPanel();
	}

	public bool IsTownUiOpen()
	{
		return _townUi?.Visible == true;
	}

	public bool OpenSelectionPanelForCell(Vector2I cell)
	{
		_selectedCell = cell;

		if (cell == TownCell)
		{
			HideActionPanel();
			HideCharacterPanel();
			ShowTownPanel();
			return true;
		}

		if (!IsExplored(cell))
		{
			if (!CanQueueExploreCell(cell))
			{
				return false;
			}

			HideTownPanel();
			HideCharacterPanel();
			ShowExploreActionPanel(cell);
			return true;
		}

		string? resourceId = GetResourceId(cell);
		if (resourceId is null)
		{
			return false;
		}

		HideTownPanel();
		HideCharacterPanel();
		ShowActionPanel(cell, GameCatalog.GetResource(resourceId));
		return true;
	}

	public bool IsSelectionPanelOpen()
	{
		return _selectedResourcePanel?.Visible == true;
	}

	private void CreateHudPanels()
	{
		CanvasLayer hud = GetNode<CanvasLayer>("Hud");

		_statusLabel = new Label
		{
			OffsetLeft = 404.0f,
			OffsetTop = 660.0f,
			OffsetRight = 1180.0f,
			OffsetBottom = 710.0f,
			Text = string.Empty,
		};
		_statusLabel.AddThemeColorOverride("font_color", new Color(0.96f, 0.97f, 0.91f));
		_statusLabel.AddThemeColorOverride("font_shadow_color", new Color(0.07f, 0.09f, 0.06f, 0.9f));
		_statusLabel.AddThemeConstantOverride("shadow_offset_x", 2);
		_statusLabel.AddThemeConstantOverride("shadow_offset_y", 2);
		hud.AddChild(_statusLabel);

		PackedScene leftSidebarScene = ResourceLoader.Load<PackedScene>("res://UI/HUD/LeftSidebar.tscn");
		_leftSidebar = leftSidebarScene.Instantiate<LeftSidebar>();
		_leftSidebar.OffsetLeft = 16.0f;
		_leftSidebar.OffsetTop = 92.0f;
		_leftSidebar.OffsetRight = 388.0f;
		_leftSidebar.OffsetBottom = 710.0f;
		_leftSidebar.QueueToggleRequested += ToggleQueuePanel;
		hud.AddChild(_leftSidebar);

		_selectedResourcePanel = _leftSidebar.SelectedResourcePanel;
		_queuePanel = _leftSidebar.QueuePanel;

		if (_selectedResourcePanel is not null)
		{
			_selectedResourcePanel.Visible = false;
			_selectedResourcePanel.PrimaryActionRequested += OnActionButtonPressed;
			_selectedResourcePanel.SecondaryActionRequested += OnSecondaryActionButtonPressed;
			_selectedResourcePanel.CancelRequested += ClearGatherCommand;
		}

		if (_queuePanel is not null)
		{
			_queuePanel.Visible = false;
			_queuePanel.ClearRequested += ClearGatherCommand;
		}

		_townPanel = new PanelContainer
		{
			Visible = false,
			OffsetLeft = 860.0f,
			OffsetTop = 72.0f,
			OffsetRight = 1260.0f,
			OffsetBottom = 706.0f,
		};
		_townPanel.AddThemeStyleboxOverride("panel", CreatePanelStyle(
			new Color(0.19f, 0.13f, 0.09f, 0.96f),
			new Color(0.54f, 0.40f, 0.22f, 0.95f),
			18,
			2,
			new Color(0.06f, 0.04f, 0.03f, 0.65f)));

		VBoxContainer townBox = new();
		townBox.AddThemeConstantOverride("separation", 10);
		townBox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		townBox.OffsetLeft = 16.0f;
		townBox.OffsetTop = 16.0f;
		townBox.OffsetRight = -16.0f;
		townBox.OffsetBottom = -16.0f;

		PanelContainer townHeaderPanel = new();
		townHeaderPanel.AddThemeStyleboxOverride("panel", CreateInsetPanelStyle(
			new Color(0.29f, 0.18f, 0.12f, 0.88f),
			new Color(0.62f, 0.49f, 0.28f, 0.90f)));

		HBoxContainer townHeader = new();
		townHeader.AddThemeConstantOverride("separation", 10);
		townHeader.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		townHeader.OffsetLeft = 14.0f;
		townHeader.OffsetTop = 10.0f;
		townHeader.OffsetRight = -14.0f;
		townHeader.OffsetBottom = -10.0f;

		Label townTitle = new()
		{
			Text = "Starter Town",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			VerticalAlignment = VerticalAlignment.Center,
		};
		ApplyTownTitleStyle(townTitle);

		_townGoldLabel = new Label
		{
			Text = "Gold 0",
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center,
			CustomMinimumSize = new Vector2(100.0f, 0.0f),
		};
		ApplyTownGoldStyle(_townGoldLabel);

		townHeader.AddChild(townTitle);
		townHeader.AddChild(_townGoldLabel);
		townHeaderPanel.AddChild(townHeader);

		PanelContainer stockpilePanel = new();
		stockpilePanel.AddThemeStyleboxOverride("panel", CreateInsetPanelStyle(
			new Color(0.23f, 0.16f, 0.11f, 0.84f),
			new Color(0.56f, 0.42f, 0.24f, 0.82f)));

		VBoxContainer stockpileBox = new();
		stockpileBox.AddThemeConstantOverride("separation", 8);
		stockpileBox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		stockpileBox.OffsetLeft = 14.0f;
		stockpileBox.OffsetTop = 12.0f;
		stockpileBox.OffsetRight = -14.0f;
		stockpileBox.OffsetBottom = -12.0f;

		_stockpileProgressLabel = new Label
		{
			Text = "Stockpile 0/100",
			HorizontalAlignment = HorizontalAlignment.Left,
		};
		ApplySectionTitleStyle(_stockpileProgressLabel);

		_stockpileProgressBar = new ProgressBar
		{
			MinValue = 0,
			MaxValue = _townState.StockpileCapacity,
			Value = 0,
			ShowPercentage = false,
			CustomMinimumSize = new Vector2(0.0f, 22.0f),
		};
		_stockpileProgressBar.AddThemeStyleboxOverride("background", CreateBarBackgroundStyle());
		_stockpileProgressBar.AddThemeStyleboxOverride("fill", CreateBarFillStyle());

		_stockpileUpgradeLabel = new Label
		{
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Text = "Stockpile upgrades are available in town.",
		};
		ApplyBodyLabelStyle(_stockpileUpgradeLabel);

		_stockpileUpgradeButton = new Button { Text = "Queue Stockpile Upgrade" };
		_stockpileUpgradeButton.Pressed += OnStockpileUpgradePressed;
		ApplyTownButtonStyle(_stockpileUpgradeButton, true);

		stockpileBox.AddChild(_stockpileProgressLabel);
		stockpileBox.AddChild(_stockpileProgressBar);
		stockpileBox.AddChild(_stockpileUpgradeLabel);
		stockpileBox.AddChild(_stockpileUpgradeButton);
		stockpilePanel.AddChild(stockpileBox);

		PanelContainer sellPanel = new();
		sellPanel.AddThemeStyleboxOverride("panel", CreateInsetPanelStyle(
			new Color(0.22f, 0.15f, 0.10f, 0.82f),
			new Color(0.51f, 0.39f, 0.21f, 0.70f)));

		VBoxContainer sellBox = new();
		sellBox.AddThemeConstantOverride("separation", 8);
		sellBox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		sellBox.OffsetLeft = 14.0f;
		sellBox.OffsetTop = 12.0f;
		sellBox.OffsetRight = -14.0f;
		sellBox.OffsetBottom = -12.0f;

		Label sellSectionTitle = new()
		{
			Text = "Market Board",
		};
		ApplySectionTitleStyle(sellSectionTitle);

		_sellItemList = new ItemList
		{
			CustomMinimumSize = new Vector2(0.0f, 132.0f),
			SelectMode = ItemList.SelectModeEnum.Single,
		};
		_sellItemList.ItemSelected += OnSellItemSelected;
		_sellItemList.AddThemeStyleboxOverride("panel", CreatePanelStyle(
			new Color(0.12f, 0.09f, 0.06f, 0.88f),
			new Color(0.42f, 0.31f, 0.17f, 0.75f),
			12,
			1));
		_sellItemList.AddThemeColorOverride("font_color", new Color(0.93f, 0.88f, 0.77f));
		_sellItemList.AddThemeColorOverride("font_selected_color", new Color(0.18f, 0.10f, 0.05f));
		_sellItemList.AddThemeColorOverride("guide_color", new Color(0.54f, 0.42f, 0.24f, 0.35f));
		_sellItemList.AddThemeColorOverride("cursor_color", new Color(0.88f, 0.73f, 0.41f));
		_sellItemList.AddThemeStyleboxOverride("cursor", CreatePanelStyle(
			new Color(0.90f, 0.79f, 0.53f, 0.86f),
			new Color(0.98f, 0.88f, 0.66f, 0.95f),
			8,
			1));

		_sellSelectionLabel = new Label
		{
			Text = "Select a resource to sell",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
		};
		ApplyBodyLabelStyle(_sellSelectionLabel);

		HBoxContainer sellControls = new();
		sellControls.AddThemeConstantOverride("separation", 10);

		_sellPercentSlider = new HSlider
		{
			MinValue = 0,
			MaxValue = 100,
			Step = 5,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		_sellPercentSlider.ValueChanged += OnSellPercentChanged;
		_sellPercentSlider.AddThemeStyleboxOverride("slider", CreateSliderTrackStyle());
		_sellPercentSlider.AddThemeStyleboxOverride("grabber_area", CreateSliderFillStyle());
		_sellPercentSlider.AddThemeStyleboxOverride("grabber_area_highlight", CreateSliderFillStyle());
		_sellPercentSlider.AddThemeIconOverride("grabber", CreateSliderKnobTexture(new Color(0.88f, 0.78f, 0.48f)));
		_sellPercentSlider.AddThemeIconOverride("grabber_highlight", CreateSliderKnobTexture(new Color(0.98f, 0.87f, 0.62f)));

		_sellPercentLabel = new Label
		{
			Text = "0%",
			CustomMinimumSize = new Vector2(52.0f, 0.0f),
			HorizontalAlignment = HorizontalAlignment.Right,
		};
		ApplySectionTitleStyle(_sellPercentLabel);

		sellControls.AddChild(_sellPercentSlider);
		sellControls.AddChild(_sellPercentLabel);

		_sellButton = new Button { Text = "Sell Goods" };
		_sellButton.Pressed += SellSelectedResources;
		ApplyTownButtonStyle(_sellButton, false);

		sellBox.AddChild(sellSectionTitle);
		sellBox.AddChild(_sellItemList);
		sellBox.AddChild(_sellSelectionLabel);
		sellBox.AddChild(sellControls);
		sellBox.AddChild(_sellButton);
		sellPanel.AddChild(sellBox);

		PanelContainer infoPanel = new();
		infoPanel.AddThemeStyleboxOverride("panel", CreateInsetPanelStyle(
			new Color(0.21f, 0.15f, 0.10f, 0.80f),
			new Color(0.48f, 0.36f, 0.19f, 0.64f)));

		VBoxContainer infoBox = new();
		infoBox.AddThemeConstantOverride("separation", 8);
		infoBox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		infoBox.OffsetLeft = 14.0f;
		infoBox.OffsetTop = 12.0f;
		infoBox.OffsetRight = -14.0f;
		infoBox.OffsetBottom = -12.0f;

		Label infoSectionTitle = new()
		{
			Text = "Town Hall Ledger",
		};
		ApplySectionTitleStyle(infoSectionTitle);

		_townInfoScroll = new ScrollContainer
		{
			CustomMinimumSize = new Vector2(0.0f, 230.0f),
			VerticalScrollMode = ScrollContainer.ScrollMode.ShowAlways,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
		};
		_townInfoScroll.AddThemeStyleboxOverride("panel", CreatePanelStyle(
			new Color(0.11f, 0.08f, 0.05f, 0.55f),
			new Color(0.39f, 0.29f, 0.15f, 0.30f),
			10,
			1));

		_townBody = new Label
		{
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
		};
		ApplyBodyLabelStyle(_townBody);
		_townBody.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_townBody.SizeFlagsVertical = (Control.SizeFlags)0;
		_townBody.CustomMinimumSize = new Vector2(320.0f, 0.0f);
		_townInfoScroll.AddChild(_townBody);

		infoBox.AddChild(infoSectionTitle);
		infoBox.AddChild(_townInfoScroll);
		infoPanel.AddChild(infoBox);

		Button townCloseButton = new() { Text = "Close Ledger" };
		townCloseButton.Pressed += HideTownPanel;
		ApplyTownButtonStyle(townCloseButton, true);

		townBox.AddChild(townHeaderPanel);
		townBox.AddChild(stockpilePanel);
		townBox.AddChild(sellPanel);
		townBox.AddChild(infoPanel);
		townBox.AddChild(townCloseButton);
		_townPanel.AddChild(townBox);
		hud.AddChild(_townPanel);

		_characterPanel = new PanelContainer
		{
			Visible = false,
			OffsetLeft = 16.0f,
			OffsetTop = 88.0f,
			OffsetRight = 360.0f,
			OffsetBottom = 420.0f,
		};

		VBoxContainer characterBox = new();
		characterBox.AddThemeConstantOverride("separation", 10);
		characterBox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		characterBox.OffsetLeft = 14.0f;
		characterBox.OffsetTop = 14.0f;
		characterBox.OffsetRight = -14.0f;
		characterBox.OffsetBottom = -14.0f;

		Label characterTitle = new()
		{
			Text = "Character",
			HorizontalAlignment = HorizontalAlignment.Center,
		};

		_characterSummaryLabel = new Label
		{
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			VerticalAlignment = VerticalAlignment.Top,
		};
		_characterSummaryLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

		ScrollContainer characterSkillsScroll = new()
		{
			CustomMinimumSize = new Vector2(0.0f, 180.0f),
			VerticalScrollMode = ScrollContainer.ScrollMode.ShowAlways,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
		};

		_characterSkillsList = new VBoxContainer();
		_characterSkillsList.AddThemeConstantOverride("separation", 4);
		characterSkillsScroll.AddChild(_characterSkillsList);

		foreach (SkillDefinition skill in GameCatalog.Skills)
		{
			AddCharacterSkillRow(skill);
		}

		Button characterCloseButton = new() { Text = "Close" };
		characterCloseButton.Pressed += HideCharacterPanel;

		characterBox.AddChild(characterTitle);
		characterBox.AddChild(_characterSummaryLabel);
		characterBox.AddChild(characterSkillsScroll);
		characterBox.AddChild(characterCloseButton);
		_characterPanel.AddChild(characterBox);
		hud.AddChild(_characterPanel);
	}

	private void AddCharacterSkillRow(SkillDefinition skill)
	{
		if (_characterSkillsList is null)
		{
			return;
		}

		HBoxContainer row = new();
		row.AddThemeConstantOverride("separation", 6);

		Label iconLabel = new()
		{
			Text = skill.IconGlyph,
			CustomMinimumSize = new Vector2(16.0f, 16.0f),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
		};
		iconLabel.AddThemeColorOverride("font_color", skill.IconColor);

		VBoxContainer details = new();
		details.AddThemeConstantOverride("separation", 1);
		details.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

		HBoxContainer topRow = new();
		topRow.AddThemeConstantOverride("separation", 4);

		Label nameLabel = new()
		{
			Text = skill.DisplayName,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};

		Label levelLabel = new()
		{
			Text = "Lv.1",
			HorizontalAlignment = HorizontalAlignment.Right,
			CustomMinimumSize = new Vector2(50.0f, 0.0f),
		};

		Label xpLabel = new()
		{
			Text = "0/5",
			HorizontalAlignment = HorizontalAlignment.Right,
			CustomMinimumSize = new Vector2(34.0f, 0.0f),
		};
		xpLabel.AddThemeColorOverride("font_color", new Color(0.82f, 0.85f, 0.80f));

		ProgressBar progressBar = new()
		{
			MinValue = 0,
			MaxValue = 5,
			Value = 0,
			ShowPercentage = false,
			CustomMinimumSize = new Vector2(0.0f, 4.0f),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};

		topRow.AddChild(nameLabel);
		topRow.AddChild(levelLabel);
		topRow.AddChild(xpLabel);
		details.AddChild(topRow);
		details.AddChild(progressBar);

		row.AddChild(iconLabel);
		row.AddChild(details);
		_characterSkillsList.AddChild(row);

		_skillLevelLabels[skill.Id] = levelLabel;
		_skillXpLabels[skill.Id] = xpLabel;
		_skillProgressBars[skill.Id] = progressBar;
	}

	private void ConnectTownUi()
	{
		if (_townUi is null)
		{
			return;
		}

		_townUi.CloseRequested += HideTownPanel;
		_townUi.StockpileUpgradeRequested += OnStockpileUpgradePressed;
		_townUi.SellResourceSelected += OnTownSellResourceSelected;
		_townUi.SellPercentChanged += OnTownSellPercentChanged;
		_townUi.SellRequested += SellSelectedResources;
		_townUi.BuildRequested += OnTownBuildRequested;
		_townUi.UpgradeRequested += OnTownUpgradeRequested;
		_townUi.FilterChanged += OnTownFilterChanged;
	}

	private static StyleBoxFlat CreatePanelStyle(Color backgroundColor, Color borderColor, int cornerRadius, int borderWidth, Color? shadowColor = null)
	{
		StyleBoxFlat style = new()
		{
			BgColor = backgroundColor,
			BorderColor = borderColor,
			BorderWidthLeft = borderWidth,
			BorderWidthTop = borderWidth,
			BorderWidthRight = borderWidth,
			BorderWidthBottom = borderWidth,
			CornerRadiusTopLeft = cornerRadius,
			CornerRadiusTopRight = cornerRadius,
			CornerRadiusBottomRight = cornerRadius,
			CornerRadiusBottomLeft = cornerRadius,
			ContentMarginLeft = 0.0f,
			ContentMarginTop = 0.0f,
			ContentMarginRight = 0.0f,
			ContentMarginBottom = 0.0f,
		};

		if (shadowColor is not null)
		{
			style.ShadowColor = shadowColor.Value;
			style.ShadowSize = 10;
			style.ShadowOffset = new Vector2(0.0f, 4.0f);
		}

		return style;
	}

	private static StyleBoxFlat CreateInsetPanelStyle(Color backgroundColor, Color borderColor)
	{
		StyleBoxFlat style = CreatePanelStyle(backgroundColor, borderColor, 14, 1);
		style.ContentMarginLeft = 12.0f;
		style.ContentMarginTop = 10.0f;
		style.ContentMarginRight = 12.0f;
		style.ContentMarginBottom = 10.0f;
		return style;
	}

	private static StyleBoxFlat CreateBarBackgroundStyle()
	{
		StyleBoxFlat style = CreatePanelStyle(
			new Color(0.12f, 0.09f, 0.06f, 0.90f),
			new Color(0.47f, 0.35f, 0.18f, 0.84f),
			10,
			1);
		style.ContentMarginLeft = 2.0f;
		style.ContentMarginTop = 2.0f;
		style.ContentMarginRight = 2.0f;
		style.ContentMarginBottom = 2.0f;
		return style;
	}

	private static StyleBoxFlat CreateBarFillStyle()
	{
		return CreatePanelStyle(
			new Color(0.22f, 0.56f, 0.39f, 1.0f),
			new Color(0.68f, 0.82f, 0.55f, 0.95f),
			8,
			1);
	}

	private static StyleBoxFlat CreateSliderTrackStyle()
	{
		return CreatePanelStyle(
			new Color(0.13f, 0.10f, 0.07f, 0.88f),
			new Color(0.44f, 0.32f, 0.18f, 0.80f),
			8,
			1);
	}

	private static StyleBoxFlat CreateSliderFillStyle()
	{
		return CreatePanelStyle(
			new Color(0.49f, 0.66f, 0.36f, 0.75f),
			new Color(0.78f, 0.87f, 0.63f, 0.82f),
			8,
			1);
	}

	private static Texture2D CreateSliderKnobTexture(Color color)
	{
		Image image = Image.CreateEmpty(18, 18, false, Image.Format.Rgba8);
		image.Fill(Colors.Transparent);

		Vector2 center = new(9.0f, 9.0f);
		for (int y = 0; y < 18; y++)
		{
			for (int x = 0; x < 18; x++)
			{
				Vector2 point = new(x + 0.5f, y + 0.5f);
				float distance = point.DistanceTo(center);
				if (distance <= 7.5f)
				{
					image.SetPixel(x, y, color);
				}
				else if (distance <= 8.5f)
				{
					image.SetPixel(x, y, new Color(0.28f, 0.18f, 0.07f, 0.95f));
				}
			}
		}

		return ImageTexture.CreateFromImage(image);
	}

	private static void ApplyTownTitleStyle(Label label)
	{
		label.AddThemeColorOverride("font_color", new Color(0.97f, 0.90f, 0.76f));
		label.AddThemeColorOverride("font_shadow_color", new Color(0.11f, 0.07f, 0.05f, 0.92f));
		label.AddThemeConstantOverride("shadow_offset_x", 2);
		label.AddThemeConstantOverride("shadow_offset_y", 2);
		label.AddThemeFontSizeOverride("font_size", 26);
	}

	private static void ApplyTownGoldStyle(Label label)
	{
		label.AddThemeColorOverride("font_color", new Color(0.96f, 0.81f, 0.42f));
		label.AddThemeColorOverride("font_shadow_color", new Color(0.14f, 0.08f, 0.05f, 0.92f));
		label.AddThemeConstantOverride("shadow_offset_x", 2);
		label.AddThemeConstantOverride("shadow_offset_y", 2);
		label.AddThemeFontSizeOverride("font_size", 18);
	}

	private static void ApplySectionTitleStyle(Label label)
	{
		label.AddThemeColorOverride("font_color", new Color(0.95f, 0.88f, 0.73f));
		label.AddThemeColorOverride("font_shadow_color", new Color(0.12f, 0.07f, 0.04f, 0.88f));
		label.AddThemeConstantOverride("shadow_offset_x", 1);
		label.AddThemeConstantOverride("shadow_offset_y", 1);
		label.AddThemeFontSizeOverride("font_size", 18);
	}

	private static void ApplyBodyLabelStyle(Label label)
	{
		label.AddThemeColorOverride("font_color", new Color(0.89f, 0.83f, 0.73f));
		label.AddThemeColorOverride("font_shadow_color", new Color(0.08f, 0.05f, 0.03f, 0.72f));
		label.AddThemeConstantOverride("shadow_offset_x", 1);
		label.AddThemeConstantOverride("shadow_offset_y", 1);
		label.AddThemeFontSizeOverride("font_size", 16);
	}

	private static void ApplyTownButtonStyle(Button button, bool secondary)
	{
		Color textColor = secondary ? new Color(0.93f, 0.88f, 0.79f) : new Color(0.22f, 0.12f, 0.06f);
		button.AddThemeColorOverride("font_color", textColor);
		button.AddThemeColorOverride("font_hover_color", textColor);
		button.AddThemeColorOverride("font_pressed_color", textColor);
		button.AddThemeColorOverride("font_focus_color", textColor);
		button.AddThemeColorOverride("font_disabled_color", new Color(0.60f, 0.54f, 0.46f));
		button.AddThemeFontSizeOverride("font_size", secondary ? 17 : 19);

		StyleBoxFlat normal = secondary
			? CreatePanelStyle(new Color(0.26f, 0.18f, 0.12f, 0.92f), new Color(0.63f, 0.49f, 0.29f, 0.84f), 14, 1)
			: CreatePanelStyle(new Color(0.86f, 0.71f, 0.42f, 0.92f), new Color(0.98f, 0.86f, 0.58f, 0.96f), 14, 1);
		StyleBoxFlat hover = secondary
			? CreatePanelStyle(new Color(0.31f, 0.22f, 0.14f, 0.96f), new Color(0.78f, 0.63f, 0.37f, 0.90f), 14, 1)
			: CreatePanelStyle(new Color(0.95f, 0.79f, 0.49f, 0.96f), new Color(1.0f, 0.91f, 0.67f, 1.0f), 14, 1);
		StyleBoxFlat pressed = secondary
			? CreatePanelStyle(new Color(0.21f, 0.15f, 0.10f, 0.98f), new Color(0.56f, 0.44f, 0.27f, 0.88f), 14, 1)
			: CreatePanelStyle(new Color(0.74f, 0.58f, 0.31f, 0.98f), new Color(0.94f, 0.80f, 0.48f, 0.96f), 14, 1);

		button.AddThemeStyleboxOverride("normal", normal);
		button.AddThemeStyleboxOverride("hover", hover);
		button.AddThemeStyleboxOverride("pressed", pressed);
		button.AddThemeStyleboxOverride("focus", hover);
		button.CustomMinimumSize = new Vector2(0.0f, secondary ? 40.0f : 46.0f);
	}

	private static ResourceActionDefinition GetPrimaryResourceAction(ResourceDefinition resource)
	{
		return new ResourceActionDefinition
		{
			Id = "default",
			ButtonText = resource.GatherButtonText,
			Verb = resource.GatherVerb,
			ItemId = resource.ItemId,
			SkillId = resource.SkillId,
			MinSkillLevel = 1,
		};
	}

	private static List<ResourceActionDefinition> GetResourceActions(ResourceDefinition resource)
	{
		List<ResourceActionDefinition> actions = new() { GetPrimaryResourceAction(resource) };
		actions.AddRange(resource.AlternateActions);
		return actions;
	}

	private static ResourceActionDefinition GetResourceAction(ResourceDefinition resource, string? actionId)
	{
		foreach (ResourceActionDefinition action in GetResourceActions(resource))
		{
			if (action.Id == (actionId ?? "default"))
			{
				return action;
			}
		}

		return GetPrimaryResourceAction(resource);
	}

	private bool IsResourceActionUnlocked(ResourceActionDefinition action)
	{
		return _characterState.GetSkillLevel(action.SkillId) >= action.MinSkillLevel;
	}

	private static TownUpgradeDefinition StockpileUpgrade => GameCatalog.StockpileUpgrade;

	private double GetStockpileUpgradeDurationSeconds()
	{
		return StockpileUpgrade.DurationSeconds;
	}

	private int GetNextStockpileCapacity()
	{
		return _townState.PreviewNextStockpileCapacity(StockpileUpgrade.CapacityMultiplier, StockpileUpgrade.CapacityRoundTo);
	}

	private string BuildStockpileUpgradeCostSummary()
	{
		StringBuilder builder = new();
		for (int index = 0; index < StockpileUpgrade.Costs.Count; index++)
		{
			BuildingCostDefinition cost = StockpileUpgrade.Costs[index];
			if (index > 0)
			{
				builder.Append("   |   ");
			}

			ItemDefinition item = GameCatalog.GetItem(cost.ItemId);
			builder.Append(cost.Amount)
				.Append(' ')
				.Append(item.DisplayName.ToLowerInvariant());
		}

		return builder.ToString();
	}

	private void ProcessGatherCommand(double delta)
	{
		if (_player is null)
		{
			return;
		}

		if (_activeGatherCommand is null && !TryStartNextQueuedCommand())
		{
			_workerPhase = WorkerPhase.Idle;
			return;
		}

		if (_activeGatherCommand is null)
		{
			_workerPhase = WorkerPhase.Idle;
			return;
		}

		if (_activeGatherCommand.Kind == WorkKind.Explore)
		{
			ProcessExploreCommand(delta);
			return;
		}

		if (_activeGatherCommand.Kind == WorkKind.TownUpgrade)
		{
			ProcessTownUpgradeCommand(delta);
			return;
		}

		if (_activeGatherCommand.Kind == WorkKind.BuildingConstruction)
		{
			ProcessBuildingConstructionCommand(delta);
			return;
		}

		ResourceDefinition resource = GameCatalog.GetResource(_activeGatherCommand.ResourceId!);
		ResourceActionDefinition action = GetResourceAction(resource, _activeGatherCommand.ResourceActionId);

		if (_player.CurrentCell == TownCell && _characterState.GetBagCount() > 0 && _townState.HasStorageSpace())
		{
			_workerPhase = WorkerPhase.ReturningToTown;
			ProcessReturnToTown();
			return;
		}

		if (_activeGatherCommand.RemainingAmount <= 0)
		{
			FinishGatherCommandIfReady(resource);
			return;
		}

		if (_workerPhase != WorkerPhase.ReturningToTown && _characterState.IsBagFull())
		{
			_workerPhase = WorkerPhase.ReturningToTown;
			_gatherProgressSeconds = 0.0;
		}

		if (_workerPhase == WorkerPhase.ReturningToTown)
		{
			ProcessReturnToTown();
			return;
		}

		if (_workerPhase == WorkerPhase.ReturningToResource)
		{
			ProcessReturnToResource(resource);
			return;
		}

		if (_player.IsMoving)
		{
			_workerPhase = WorkerPhase.TravelingToResource;
			UpdateStatus($"Moving to {_activeGatherCommand.Cell.X}, {_activeGatherCommand.Cell.Y}...");
			return;
		}

		if (_player.CurrentCell != _activeGatherCommand.Cell)
		{
			Vector2I nextCell = GetNextStep(_player.CurrentCell, _activeGatherCommand.Cell);
			_player.BeginStep(nextCell, GetCurrentStepDurationSeconds());
			_workerPhase = WorkerPhase.TravelingToResource;
			UpdateStatus($"Walking to {_activeGatherCommand.Cell.X}, {_activeGatherCommand.Cell.Y}...");
			return;
		}

		if (_characterState.IsBagFull())
		{
			_workerPhase = WorkerPhase.ReturningToTown;
			_gatherProgressSeconds = 0.0;
			UpdateStatus("Bag is full. Returning to town.");
			return;
		}

		_workerPhase = WorkerPhase.Gathering;
		_gatherProgressSeconds += delta;
		double secondsLeft = Mathf.Max(0.0, (float)(Rules.GatherDurationSeconds - _gatherProgressSeconds));
		UpdateStatus($"{action.Verb} at {_activeGatherCommand.Cell.X}, {_activeGatherCommand.Cell.Y}... {secondsLeft:0.0}s  Bag: {_characterState.GetBagCount()}/{_characterState.BagCapacity}");

		if (_gatherProgressSeconds < Rules.GatherDurationSeconds)
		{
			return;
		}

		_gatherProgressSeconds -= Rules.GatherDurationSeconds;
		CompleteGatherCycle(resource, action);
	}

	private void ProcessExploreCommand(double delta)
	{
		if (_player is null || _activeGatherCommand is null)
		{
			return;
		}

		if (IsExplored(_activeGatherCommand.Cell))
		{
			CompleteCurrentCommand($"Tile ({_activeGatherCommand.Cell.X}, {_activeGatherCommand.Cell.Y}) is already explored.");
			return;
		}

		if (!IsFrontierCell(_activeGatherCommand.Cell))
		{
			RequeueExploreCommandForRequirements(_activeGatherCommand);
			return;
		}

		if (_player.IsMoving)
		{
			_workerPhase = WorkerPhase.TravelingToResource;
			UpdateStatus($"Moving to explore {_activeGatherCommand.Cell.X}, {_activeGatherCommand.Cell.Y}...");
			return;
		}

		if (_player.CurrentCell != _activeGatherCommand.Cell)
		{
			Vector2I nextCell = GetNextStep(_player.CurrentCell, _activeGatherCommand.Cell);
			_player.BeginStep(nextCell, GetCurrentStepDurationSeconds());
			_workerPhase = WorkerPhase.TravelingToResource;
			UpdateStatus($"Walking to explore {_activeGatherCommand.Cell.X}, {_activeGatherCommand.Cell.Y}...");
			return;
		}

		if (!_activeExploreRequirementPaid)
		{
			ItemDefinition requirementItem = GameCatalog.GetItem(Rules.ExploreRequirementItemId);
			if (!_townState.TryConsumeStored(requirementItem.Id, Rules.ExploreRequirementAmount))
			{
				RequeueExploreCommandForRequirements(_activeGatherCommand);
				return;
			}

			_activeExploreRequirementPaid = true;
			UpdateTownPanel();
		}

		_workerPhase = WorkerPhase.Gathering;
		UpdateExploringSkill(delta);
		_gatherProgressSeconds += delta;
		double exploreDuration = GetCurrentExploreDurationSeconds();
		double secondsLeft = Mathf.Max(0.0, (float)(exploreDuration - _gatherProgressSeconds));
		UpdateStatus($"Exploring tile {_activeGatherCommand.Cell.X}, {_activeGatherCommand.Cell.Y}... {secondsLeft:0.0}s");

		if (_gatherProgressSeconds < exploreDuration)
		{
			return;
		}

		_gatherProgressSeconds -= exploreDuration;
		CompleteExploreCycle(_activeGatherCommand.Cell);
	}

	private void ProcessTownUpgradeCommand(double delta)
	{
		if (_player is null || _activeGatherCommand is null)
		{
			return;
		}

		if (_player.IsMoving)
		{
			_workerPhase = WorkerPhase.TravelingToResource;
			UpdateStatus("Moving to town to upgrade the stockpile...");
			return;
		}

		if (_player.CurrentCell != TownCell)
		{
			Vector2I nextCell = GetNextStep(_player.CurrentCell, TownCell);
			_player.BeginStep(nextCell, GetCurrentStepDurationSeconds());
			_workerPhase = WorkerPhase.TravelingToResource;
			UpdateStatus("Walking to town to upgrade the stockpile...");
			return;
		}

		if (!_activeTownUpgradePaid)
		{
			if (!_townState.CanAfford(StockpileUpgrade.Costs))
			{
				RequeueStockpileUpgradeForRequirements();
				UpdateTownPanel();
				return;
			}

			_townState.TryConsumeCosts(StockpileUpgrade.Costs);
			_activeTownUpgradePaid = true;
			UpdateTownPanel();
		}

		_workerPhase = WorkerPhase.Gathering;
		UpdateBuildingSkill(delta);
		_gatherProgressSeconds += delta;
		double duration = GetStockpileUpgradeDurationSeconds();
		double secondsLeft = Mathf.Max(0.0, (float)(duration - _gatherProgressSeconds));
		UpdateStatus($"Upgrading stockpile in town... {secondsLeft:0.0}s");

		if (_gatherProgressSeconds < duration)
		{
			return;
		}

		int nextCapacity = _townState.UpgradeStockpile(StockpileUpgrade.CapacityMultiplier, StockpileUpgrade.CapacityRoundTo);
		_gatherProgressSeconds -= duration;
		UpdateTownPanel();
		CompleteCurrentCommand($"Stockpile upgraded to Lv.{_townState.StockpileLevel}. Capacity is now {nextCapacity}.");
	}

	private void ProcessBuildingConstructionCommand(double delta)
	{
		if (_player is null || _activeGatherCommand is null || string.IsNullOrEmpty(_activeGatherCommand.BuildingId))
		{
			return;
		}

		BuildingDefinition definition = GameCatalog.GetBuilding(_activeGatherCommand.BuildingId);
		BuildingState state = _townState.GetBuildingState(definition.Id);
		BuildingLevelDefinition level = definition.GetLevelDefinition(_activeGatherCommand.TargetLevel);

		if (_player.IsMoving)
		{
			_workerPhase = WorkerPhase.TravelingToResource;
			UpdateStatus($"Moving to town to work on {definition.DisplayName}...");
			return;
		}

		if (_player.CurrentCell != TownCell)
		{
			Vector2I nextCell = GetNextStep(_player.CurrentCell, TownCell);
			_player.BeginStep(nextCell, GetCurrentStepDurationSeconds());
			_workerPhase = WorkerPhase.TravelingToResource;
			UpdateStatus($"Walking to town to work on {definition.DisplayName}...");
			return;
		}

		if (!state.IsUnderConstruction)
		{
			if (!_townState.TryConsumeCosts(level.Costs))
			{
				CompleteCurrentCommand($"{definition.DisplayName} cannot start. The town is missing required materials.");
				UpdateTownPanel();
				return;
			}

			state.BeginConstruction(_activeGatherCommand.TargetLevel, level.DurationSeconds);
			UpdateTownPanel();
		}

		_workerPhase = WorkerPhase.Gathering;
		UpdateBuildingSkill(delta);
		state.Advance(delta);
		_gatherProgressSeconds = state.ProgressSeconds;

		double secondsLeft = Mathf.Max(0.0, (float)(state.TotalSeconds - state.ProgressSeconds));
		UpdateStatus($"Constructing {definition.DisplayName}... {secondsLeft:0.0}s");
		UpdateTownPanel();

		if (state.ProgressSeconds < state.TotalSeconds)
		{
			return;
		}

		state.CompleteConstruction();
		_gatherProgressSeconds = 0.0;
		UpdateTownPanel();
		CompleteCurrentCommand($"{definition.DisplayName} reached level {state.CurrentLevel}.");
	}

	private void ProcessReturnToTown()
	{
		if (_player is null)
		{
			return;
		}

		if (_player.IsMoving)
		{
			UpdateStatus($"Returning to town with bag {_characterState.GetBagCount()}/{_characterState.BagCapacity}...");
			return;
		}

		if (_player.CurrentCell != TownCell)
		{
			Vector2I nextCell = GetNextStep(_player.CurrentCell, TownCell);
			_player.BeginStep(nextCell, GetCurrentStepDurationSeconds());
			UpdateStatus($"Returning to town with bag {_characterState.GetBagCount()}/{_characterState.BagCapacity}...");
			return;
		}

		int moved = _characterState.UnloadToTown(_townState);
		UpdateTownPanel();

		if (_characterState.GetBagCount() > 0 && !_townState.HasStorageSpace())
		{
			_workerPhase = WorkerPhase.Idle;
			UpdateStatus($"Stockpile full. Stored {moved} item(s), but {_characterState.GetBagCount()} remain in the bag. Sell resources to free space.");
			return;
		}

		if (_activeGatherCommand is not null &&
			_activeGatherCommand.Kind == WorkKind.Gather &&
			_activeGatherCommand.RemainingAmount <= 0 &&
			_characterState.GetBagCount() == 0)
		{
			ResourceDefinition finishedResource = GameCatalog.GetResource(_activeGatherCommand.ResourceId!);
			ResourceActionDefinition finishedAction = GetResourceAction(finishedResource, _activeGatherCommand.ResourceActionId);
			CompleteCurrentCommand($"{finishedAction.Verb} queue complete at ({_activeGatherCommand.Cell.X}, {_activeGatherCommand.Cell.Y}).");
			return;
		}

		_workerPhase = _activeGatherCommand is null ? WorkerPhase.Idle : WorkerPhase.ReturningToResource;

		if (_activeGatherCommand is null)
		{
			UpdateStatus($"Bag unloaded at town. Stockpile {_townState.GetStoredCountTotal()}/{_townState.StockpileCapacity}.");
			return;
		}

		UpdateStatus($"Bag unloaded. Returning to ({_activeGatherCommand.Cell.X}, {_activeGatherCommand.Cell.Y}).");
	}

	private void ProcessReturnToResource(ResourceDefinition resource)
	{
		if (_player is null || _activeGatherCommand is null)
		{
			return;
		}

		ResourceActionDefinition action = GetResourceAction(resource, _activeGatherCommand.ResourceActionId);

		if (_player.IsMoving)
		{
			UpdateStatus($"Returning to resource at ({_activeGatherCommand.Cell.X}, {_activeGatherCommand.Cell.Y})...");
			return;
		}

		if (_player.CurrentCell != _activeGatherCommand.Cell)
		{
			Vector2I nextCell = GetNextStep(_player.CurrentCell, _activeGatherCommand.Cell);
			_player.BeginStep(nextCell, GetCurrentStepDurationSeconds());
			UpdateStatus($"Returning to resource at ({_activeGatherCommand.Cell.X}, {_activeGatherCommand.Cell.Y})...");
			return;
		}

		_workerPhase = WorkerPhase.Gathering;
		_gatherProgressSeconds = 0.0;
		UpdateStatus($"Back at resource. Resuming {action.Verb.ToLowerInvariant()}.");
	}

	private void FinishGatherCommandIfReady(ResourceDefinition resource)
	{
		ResourceActionDefinition action = GetResourceAction(resource, _activeGatherCommand?.ResourceActionId);
		if (_characterState.GetBagCount() > 0)
		{
			_workerPhase = WorkerPhase.ReturningToTown;
			_gatherProgressSeconds = 0.0;
			ProcessReturnToTown();
			return;
		}

		CompleteCurrentCommand($"{action.Verb} queue complete at ({_activeGatherCommand!.Cell.X}, {_activeGatherCommand.Cell.Y}).");
	}

	private void CompleteGatherCycle(ResourceDefinition resource, ResourceActionDefinition action)
	{
		SkillDefinition skill = GameCatalog.GetSkill(action.SkillId);
		ItemDefinition item = GameCatalog.GetItem(action.ItemId);
		int previousLevel = _characterState.GetSkillLevel(skill.Id);

		_characterState.AddToBag(item.Id);
		_characterState.AddSkillXp(skill.Id);

		if (_activeGatherCommand is not null && _activeGatherCommand.RemainingAmount > 0)
		{
			_activeGatherCommand.RemainingAmount--;
		}

		int newLevel = _characterState.GetSkillLevel(skill.Id);
		string result = $"{action.Verb} complete. +1 {item.DisplayName.ToLowerInvariant()}. Bag: {_characterState.GetBagCount()}/{_characterState.BagCapacity}.";
		if (newLevel > previousLevel)
		{
			result += $" {skill.DisplayName} reached level {newLevel}.";
		}

		if (_characterState.IsBagFull())
		{
			_workerPhase = WorkerPhase.ReturningToTown;
			result += " Bag full, returning to town.";
		}
		else if (_activeGatherCommand is not null && _activeGatherCommand.RemainingAmount <= 0)
		{
			_workerPhase = WorkerPhase.ReturningToTown;
			result += " Batch complete, returning to town.";
		}

		UpdateStatus(result);
		UpdateTownPanel();
		UpdateCharacterPanel();
		_queuePanelDirty = true;
		UpdateQueuePanel();
	}

	private void CompleteExploreCycle(Vector2I cell)
	{
		_exploredCells.Add(cell);
		_characterState.AddSkillXp(GameCatalog.Exploring.Id);
		CompleteCurrentCommand($"Exploration complete at ({cell.X}, {cell.Y}).");
		UpdateCharacterPanel();
	}

	private void UpdatePlayerProgressBar()
	{
		if (_player is null)
		{
			return;
		}

		if (_workerPhase != WorkerPhase.Gathering)
		{
			_player.SetProgressBar(false, 0.0f);
			return;
		}

		double duration = _activeGatherCommand?.Kind switch
		{
			WorkKind.Explore => GetCurrentExploreDurationSeconds(),
			WorkKind.TownUpgrade => GetStockpileUpgradeDurationSeconds(),
			WorkKind.BuildingConstruction when _activeGatherCommand.BuildingId is not null => _townState.GetBuildingState(_activeGatherCommand.BuildingId).TotalSeconds,
			_ => Rules.GatherDurationSeconds,
		};
		float ratio = (float)(_gatherProgressSeconds / duration);
		_player.SetProgressBar(true, ratio);
	}

	private bool TryStartNextQueuedCommand()
	{
		if (_activeGatherCommand is not null || _queuedCommands.Count == 0)
		{
			return false;
		}

		GatherCommand nextCommand = _queuedCommands[0];
		_queuedCommands.RemoveAt(0);
		ActivateCommand(nextCommand);
		UpdateStatus($"Starting: {nextCommand.Description}.");
		return true;
	}

	private void ActivateCommand(GatherCommand command)
	{
		_activeGatherCommand = command;
		_gatherProgressSeconds = 0.0;
		_activeExploreRequirementPaid = false;
		_activeTownUpgradePaid = false;
		_workerPhase = command.Kind == WorkKind.Gather && _characterState.IsBagFull()
			? WorkerPhase.ReturningToTown
			: WorkerPhase.TravelingToResource;
		_queuePanelDirty = true;
		UpdateQueuePanel();
	}

	private void CompleteCurrentCommand(string completedStatus)
	{
		CancelActiveConstructionVisualState();
		_activeGatherCommand = null;
		_gatherProgressSeconds = 0.0;
		_activeExploreRequirementPaid = false;
		_activeTownUpgradePaid = false;
		_workerPhase = WorkerPhase.Idle;
		_queuePanelDirty = true;
		UpdateQueuePanel();

		if (TryStartNextQueuedCommand() && _activeGatherCommand is not null)
		{
			UpdateStatus($"{completedStatus} Next: {_activeGatherCommand.Description}.");
			return;
		}

		UpdateStatus(completedStatus);
	}

	private void EnqueueCommand(GatherCommand command, bool prioritize = false)
	{
		if (prioritize)
		{
			_queuedCommands.Insert(0, command);
		}
		else
		{
			_queuedCommands.Add(command);
		}

		_queuePanelDirty = true;
		UpdateQueuePanel();
		if (_activeGatherCommand is null)
		{
			TryStartNextQueuedCommand();
		}
	}

	private bool QueueExploreCommandWithRequirements(Vector2I cell, bool prioritize, out int queuedGatherAmount, out string statusMessage)
	{
		queuedGatherAmount = 0;
		statusMessage = string.Empty;

		if (!CanQueueExploreCell(cell))
		{
			statusMessage = "That tile is not reachable for exploration yet.";
			return false;
		}

		if (IsExploreAlreadyPlanned(cell))
		{
			statusMessage = $"Tile ({cell.X}, {cell.Y}) is already in the queue.";
			return false;
		}

		int requiredAmount = Rules.ExploreRequirementAmount;
		int availableAtExecution = GetProjectedTownItemCount(Rules.ExploreRequirementItemId);
		int shortfall = System.Math.Max(0, requiredAmount - availableAtExecution);
		List<GatherCommand> plannedCommands = new();

		if (shortfall > 0)
		{
			GatherCommand? gatherCommand = BuildGatherCommandForItem(Rules.ExploreRequirementItemId, shortfall);
			if (gatherCommand is null)
			{
				ItemDefinition requirementItem = GameCatalog.GetItem(Rules.ExploreRequirementItemId);
				statusMessage = $"No explored source is available to gather {requirementItem.DisplayName.ToLowerInvariant()}.";
				return false;
			}

			plannedCommands.Add(gatherCommand);
			queuedGatherAmount = shortfall;
		}

		plannedCommands.Add(CreateExploreCommand(cell));

		if (prioritize)
		{
			_queuedCommands.InsertRange(0, plannedCommands);
		}
		else
		{
			_queuedCommands.AddRange(plannedCommands);
		}

		_queuePanelDirty = true;
		UpdateQueuePanel();
		if (_activeGatherCommand is null)
		{
			TryStartNextQueuedCommand();
		}

		statusMessage = shortfall > 0
			? $"Queued {shortfall} {GameCatalog.GetItem(Rules.ExploreRequirementItemId).DisplayName.ToLowerInvariant()} first, then explore ({cell.X}, {cell.Y})."
			: $"Queued exploration for tile ({cell.X}, {cell.Y}).";
		return true;
	}

	private bool QueueStockpileUpgradeWithRequirements(bool prioritize, out string statusMessage)
	{
		statusMessage = string.Empty;

		List<GatherCommand> plannedCommands = new();
		List<string> queuedRequirements = new();

		foreach (BuildingCostDefinition cost in StockpileUpgrade.Costs)
		{
			int shortfall = System.Math.Max(0, cost.Amount - GetProjectedTownItemCount(cost.ItemId));
			if (shortfall <= 0)
			{
				continue;
			}

			GatherCommand? gatherCommand = BuildGatherCommandForItem(cost.ItemId, shortfall);
			if (gatherCommand is null)
			{
				ItemDefinition item = GameCatalog.GetItem(cost.ItemId);
				statusMessage = $"No explored source is available to gather {item.DisplayName.ToLowerInvariant()}.";
				return false;
			}

			plannedCommands.Add(gatherCommand);
			queuedRequirements.Add($"{shortfall} {GameCatalog.GetItem(cost.ItemId).DisplayName.ToLowerInvariant()}");
		}

		int projectedLevel = _townState.StockpileLevel + GetQueuedStockpileUpgradeCount() + (_activeGatherCommand?.Kind == WorkKind.TownUpgrade ? 1 : 0);
		plannedCommands.Add(new GatherCommand
		{
			Kind = WorkKind.TownUpgrade,
			TownUpgradeId = "stockpile",
			Cell = TownCell,
			TotalAmount = 1,
			RemainingAmount = 1,
			Description = $"Upgrade stockpile to Lv.{projectedLevel + 1}",
		});

		if (prioritize)
		{
			_queuedCommands.InsertRange(0, plannedCommands);
		}
		else
		{
			_queuedCommands.AddRange(plannedCommands);
		}

		_queuePanelDirty = true;
		UpdateQueuePanel();
		if (_activeGatherCommand is null)
		{
			TryStartNextQueuedCommand();
		}

		statusMessage = queuedRequirements.Count > 0
			? $"Queued {FormatRequirementList(queuedRequirements)} first, then upgrade stockpile to Lv.{projectedLevel + 1}."
			: $"Queued stockpile upgrade to Lv.{projectedLevel + 1}.";
		return true;
	}

	private void RequeueExploreCommandForRequirements(GatherCommand exploreCommand)
	{
		_activeGatherCommand = null;
		_gatherProgressSeconds = 0.0;
		_activeExploreRequirementPaid = false;
		_workerPhase = WorkerPhase.Idle;
		_queuePanelDirty = true;

		if (QueueExploreCommandWithRequirements(exploreCommand.Cell, true, out _, out string statusMessage))
		{
			UpdateStatus(statusMessage);
			return;
		}

		UpdateStatus($"Stopped explore at ({exploreCommand.Cell.X}, {exploreCommand.Cell.Y}). {statusMessage}");
		TryStartNextQueuedCommand();
	}

	private void RequeueStockpileUpgradeForRequirements()
	{
		_activeGatherCommand = null;
		_gatherProgressSeconds = 0.0;
		_activeTownUpgradePaid = false;
		_workerPhase = WorkerPhase.Idle;
		_queuePanelDirty = true;

		if (QueueStockpileUpgradeWithRequirements(true, out string statusMessage))
		{
			UpdateStatus(statusMessage);
			return;
		}

		UpdateStatus($"Stopped stockpile upgrade. {statusMessage}");
		TryStartNextQueuedCommand();
	}

	private GatherCommand CreateGatherCommand(string resourceId, string actionId, Vector2I cell, int amount)
	{
		ResourceDefinition resource = GameCatalog.GetResource(resourceId);
		ResourceActionDefinition action = GetResourceAction(resource, actionId);
		return new GatherCommand
		{
			Cell = cell,
			Kind = WorkKind.Gather,
			ResourceId = resourceId,
			ResourceActionId = action.Id,
			TotalAmount = amount,
			RemainingAmount = amount,
			Description = $"{resource.DisplayName} {action.ButtonText.ToLowerInvariant()} x{amount} at ({cell.X}, {cell.Y})",
		};
	}

	private GatherCommand CreateExploreCommand(Vector2I cell)
	{
		return new GatherCommand
		{
			Cell = cell,
			Kind = WorkKind.Explore,
			TotalAmount = 1,
			RemainingAmount = 1,
			Description = $"Explore ({cell.X}, {cell.Y})",
		};
	}

	private GatherCommand? BuildGatherCommandForItem(string itemId, int amount)
	{
		if (amount <= 0)
		{
			return null;
		}

		foreach (ResourceDefinition resource in GameCatalog.Resources)
		{
			foreach (ResourceActionDefinition action in GetResourceActions(resource))
			{
				if (action.ItemId != itemId || !IsResourceActionUnlocked(action))
				{
					continue;
				}

				Vector2I? sourceCell = FindNearestExploredResourceCell(resource.Id);
				if (sourceCell is null)
				{
					continue;
				}

				return CreateGatherCommand(resource.Id, action.Id, sourceCell.Value, amount);
			}
		}

		return null;
	}

	private Vector2I? FindNearestExploredResourceCell(string resourceId)
	{
		Vector2I? bestCell = null;
		int bestDistance = int.MaxValue;

		foreach (Vector2I cell in _exploredCells)
		{
			if (GetResourceId(cell) != resourceId)
			{
				continue;
			}

			int distance = GetStepDistance(TownCell, cell);
			if (distance < bestDistance)
			{
				bestDistance = distance;
				bestCell = cell;
			}
		}

		return bestCell;
	}

	private bool IsExploreAlreadyPlanned(Vector2I cell)
	{
		if (_activeGatherCommand?.Kind == WorkKind.Explore && _activeGatherCommand.Cell == cell)
		{
			return true;
		}

		foreach (GatherCommand command in _queuedCommands)
		{
			if (command.Kind == WorkKind.Explore && command.Cell == cell)
			{
				return true;
			}
		}

		return false;
	}

	private int GetProjectedTownItemCount(string itemId)
	{
		int projectedCount = _townState.GetStoredCount(itemId);

		if (_activeGatherCommand is not null)
		{
			bool activeCostAlreadyPaid = _activeGatherCommand.Kind switch
			{
				WorkKind.Explore => _activeExploreRequirementPaid,
				WorkKind.TownUpgrade => _activeTownUpgradePaid,
				WorkKind.BuildingConstruction when _activeGatherCommand.BuildingId is not null => _townState.GetBuildingState(_activeGatherCommand.BuildingId).IsUnderConstruction,
				_ => false,
			};
			projectedCount = ApplyProjectedTownDelta(projectedCount, itemId, _activeGatherCommand, activeCostAlreadyPaid);
		}

		foreach (GatherCommand command in _queuedCommands)
		{
			projectedCount = ApplyProjectedTownDelta(projectedCount, itemId, command, false);
		}

		return projectedCount;
	}

	private int ApplyProjectedTownDelta(int currentAmount, string itemId, GatherCommand command, bool requirementAlreadyPaid)
	{
		if (command.Kind == WorkKind.Gather && command.ResourceId is not null)
		{
			ResourceDefinition resource = GameCatalog.GetResource(command.ResourceId);
			ResourceActionDefinition action = GetResourceAction(resource, command.ResourceActionId);
			if (action.ItemId == itemId)
			{
				return currentAmount + System.Math.Max(0, command.RemainingAmount);
			}
		}

		if (command.Kind == WorkKind.Explore && Rules.ExploreRequirementItemId == itemId && !requirementAlreadyPaid)
		{
			return currentAmount - Rules.ExploreRequirementAmount;
		}

		if (command.Kind == WorkKind.TownUpgrade && !requirementAlreadyPaid)
		{
			foreach (BuildingCostDefinition cost in StockpileUpgrade.Costs)
			{
				if (cost.ItemId == itemId)
				{
					return currentAmount - cost.Amount;
				}
			}
		}

		if (command.Kind == WorkKind.BuildingConstruction && !requirementAlreadyPaid && !string.IsNullOrEmpty(command.BuildingId))
		{
			BuildingLevelDefinition level = GameCatalog.GetBuilding(command.BuildingId).GetLevelDefinition(command.TargetLevel);
			foreach (BuildingCostDefinition cost in level.Costs)
			{
				if (cost.ItemId == itemId)
				{
					return currentAmount - cost.Amount;
				}
			}
		}

		return currentAmount;
	}

	private bool CanQueueExploreCell(Vector2I cell)
	{
		if (IsExplored(cell))
		{
			return false;
		}

		HashSet<Vector2I> reachableCells = new(_exploredCells);
		if (_activeGatherCommand?.Kind == WorkKind.Explore)
		{
			reachableCells.Add(_activeGatherCommand.Cell);
		}

		foreach (GatherCommand command in _queuedCommands)
		{
			if (command.Kind == WorkKind.Explore)
			{
				reachableCells.Add(command.Cell);
			}
		}

		for (int y = -1; y <= 1; y++)
		{
			for (int x = -1; x <= 1; x++)
			{
				if (x == 0 && y == 0)
				{
					continue;
				}

				if (reachableCells.Contains(cell + new Vector2I(x, y)))
				{
					return true;
				}
			}
		}

		return false;
	}

	private bool IsQueuedForExplore(Vector2I cell)
	{
		if (_activeGatherCommand?.Kind == WorkKind.Explore && _activeGatherCommand.Cell == cell)
		{
			return true;
		}

		foreach (GatherCommand command in _queuedCommands)
		{
			if (command.Kind == WorkKind.Explore && command.Cell == cell)
			{
				return true;
			}
		}

		return false;
	}

	private void ToggleQueuePanel()
	{
		if (_leftSidebar is null || _queuePanel is null)
		{
			return;
		}

		_leftSidebar.SetQueueVisible(!_queuePanel.Visible);

		if (_queuePanel.Visible)
		{
			_queuePanelDirty = true;
			UpdateQueuePanel();
		}

		RefreshActionPanel();
	}

	private void UpdateQueuePanel()
	{
		if (_queuePanel is null)
		{
			return;
		}

		_queueSummaryRefreshSeconds = 0.0;

		List<GatherCommand> visibleCommands = new();
		if (_activeGatherCommand is not null)
		{
			visibleCommands.Add(_activeGatherCommand);
		}

		visibleCommands.AddRange(_queuedCommands);

		if (visibleCommands.Count == 0)
		{
			_queuePanel.SetData("No queued actions.", System.Array.Empty<string>(), false);
			_queuePanelDirty = false;
			return;
		}

		double totalSeconds = EstimateQueueDurationSeconds();
		string summary = $"Queued: {visibleCommands.Count}  Total: {FormatDuration(totalSeconds)}";

		List<string> entries = new();
		if (_activeGatherCommand is not null)
		{
			entries.Add($"Now: {FormatCommandEntry(_activeGatherCommand)}");
		}

		for (int index = 0; index < _queuedCommands.Count; index++)
		{
			entries.Add($"{index + 1}. {FormatCommandEntry(_queuedCommands[index])}");
		}

		_queuePanel.SetData(summary, entries, true);

		_queuePanelDirty = false;
	}

	private string FormatCommandEntry(GatherCommand command)
	{
		if (command.Kind == WorkKind.BuildingConstruction)
		{
			return command.Description;
		}

		if (command.Kind != WorkKind.Gather || command.TotalAmount <= 0)
		{
			return command.Description;
		}

		int completedAmount = command.TotalAmount - System.Math.Max(0, command.RemainingAmount);
		return $"{command.Description} [{completedAmount}/{command.TotalAmount}]";
	}

	private double EstimateQueueDurationSeconds()
	{
		if (_player is null)
		{
			return 0.0;
		}

		Vector2I simulatedCell = _player.CurrentDisplayCell;
		int simulatedBagTotal = _characterState.GetBagCount();
		int simulatedBagRequirementCount = _characterState.GetBagCount(Rules.ExploreRequirementItemId);
		int simulatedTownRequirementCount = _townState.GetStoredCount(Rules.ExploreRequirementItemId);
		double totalSeconds = 0.0;

		if (_activeGatherCommand is not null)
		{
			totalSeconds += EstimateCommandSeconds(
				_activeGatherCommand,
				true,
				ref simulatedCell,
				ref simulatedBagTotal,
				ref simulatedBagRequirementCount,
				ref simulatedTownRequirementCount);
		}

		foreach (GatherCommand command in _queuedCommands)
		{
			totalSeconds += EstimateCommandSeconds(
				command,
				false,
				ref simulatedCell,
				ref simulatedBagTotal,
				ref simulatedBagRequirementCount,
				ref simulatedTownRequirementCount);
		}

		return totalSeconds;
	}

	private double EstimateCommandSeconds(
		GatherCommand command,
		bool isActiveCommand,
		ref Vector2I simulatedCell,
		ref int simulatedBagTotal,
		ref int simulatedBagRequirementCount,
		ref int simulatedTownRequirementCount)
	{
		double totalSeconds = 0.0;

		if (command.Kind == WorkKind.Gather && command.ResourceId is not null)
		{
			ResourceDefinition resource = GameCatalog.GetResource(command.ResourceId);
			ResourceActionDefinition action = GetResourceAction(resource, command.ResourceActionId);
			int remainingAmount = System.Math.Max(0, command.RemainingAmount);
			bool appliedActiveGatherProgress = false;

			while (remainingAmount > 0)
			{
				if (simulatedBagTotal >= _characterState.BagCapacity)
				{
					totalSeconds += GetTravelSeconds(simulatedCell, TownCell);
					simulatedCell = TownCell;
					simulatedTownRequirementCount += simulatedBagRequirementCount;
					simulatedBagTotal = 0;
					simulatedBagRequirementCount = 0;
				}

				totalSeconds += GetTravelSeconds(simulatedCell, command.Cell);
				simulatedCell = command.Cell;

				int freeSlots = _characterState.BagCapacity - simulatedBagTotal;
				int gatheredThisTrip = System.Math.Min(remainingAmount, freeSlots);
				double gatherSeconds = gatheredThisTrip * Rules.GatherDurationSeconds;

				if (isActiveCommand && !appliedActiveGatherProgress && _workerPhase == WorkerPhase.Gathering)
				{
					gatherSeconds = System.Math.Max(0.0, gatherSeconds - _gatherProgressSeconds);
					appliedActiveGatherProgress = true;
				}

				totalSeconds += gatherSeconds;
				simulatedBagTotal += gatheredThisTrip;
				if (action.ItemId == Rules.ExploreRequirementItemId)
				{
					simulatedBagRequirementCount += gatheredThisTrip;
				}

				remainingAmount -= gatheredThisTrip;

				totalSeconds += GetTravelSeconds(simulatedCell, TownCell);
				simulatedCell = TownCell;
				simulatedTownRequirementCount += simulatedBagRequirementCount;
				simulatedBagTotal = 0;
				simulatedBagRequirementCount = 0;
			}
		}
		else if (command.Kind == WorkKind.Explore)
		{
			totalSeconds += GetTravelSeconds(simulatedCell, command.Cell);
			simulatedCell = command.Cell;

			double exploreSeconds = GetCurrentExploreDurationSeconds();
			if (isActiveCommand && _workerPhase == WorkerPhase.Gathering)
			{
				exploreSeconds = System.Math.Max(0.0, exploreSeconds - _gatherProgressSeconds);
			}

			totalSeconds += exploreSeconds;
			simulatedTownRequirementCount = System.Math.Max(0, simulatedTownRequirementCount - Rules.ExploreRequirementAmount);
		}
		else if (command.Kind == WorkKind.TownUpgrade)
		{
			totalSeconds += GetTravelSeconds(simulatedCell, TownCell);
			simulatedCell = TownCell;

			double upgradeSeconds = GetStockpileUpgradeDurationSeconds();
			if (isActiveCommand && _workerPhase == WorkerPhase.Gathering)
			{
				upgradeSeconds = System.Math.Max(0.0, upgradeSeconds - _gatherProgressSeconds);
			}

			totalSeconds += upgradeSeconds;
			simulatedTownRequirementCount = System.Math.Max(0, simulatedTownRequirementCount);
		}
		else if (command.Kind == WorkKind.BuildingConstruction && !string.IsNullOrEmpty(command.BuildingId))
		{
			BuildingDefinition definition = GameCatalog.GetBuilding(command.BuildingId);
			BuildingLevelDefinition level = definition.GetLevelDefinition(command.TargetLevel);

			totalSeconds += GetTravelSeconds(simulatedCell, TownCell);
			simulatedCell = TownCell;

			double buildSeconds = level.DurationSeconds;
			if (isActiveCommand && _workerPhase == WorkerPhase.Gathering)
			{
				buildSeconds = System.Math.Max(0.0, buildSeconds - _gatherProgressSeconds);
			}

			totalSeconds += buildSeconds;
		}

		return totalSeconds;
	}

	private double GetTravelSeconds(Vector2I from, Vector2I to)
	{
		return GetStepDistance(from, to) * GetCurrentStepDurationSeconds();
	}

	private string FormatDuration(double totalSeconds)
	{
		int roundedSeconds = Mathf.RoundToInt((float)totalSeconds);
		int minutes = roundedSeconds / 60;
		int seconds = roundedSeconds % 60;
		return minutes > 0 ? $"{minutes}m {seconds}s" : $"{seconds}s";
	}

	private void ShowActionPanel(Vector2I cell, ResourceDefinition resource)
	{
		if (_selectedResourcePanel is null)
		{
			return;
		}

		_actionCell = cell;
		_actionResourceId = resource.Id;
		_actionWorkKind = WorkKind.Gather;
		List<ResourceActionDefinition> actions = GetResourceActions(resource);
		ResourceActionDefinition primaryAction = actions[0];
		SkillDefinition skill = GameCatalog.GetSkill(primaryAction.SkillId);
		_actionPrimaryResourceActionId = primaryAction.Id;
		_actionSecondaryResourceActionId = actions.Count > 1 ? actions[1].Id : null;

		ItemDefinition item = GameCatalog.GetItem(primaryAction.ItemId);
		Color accent = skill.IconColor;
		System.Collections.Generic.List<SelectionStatChipViewData> stats = new()
		{
			new()
			{
				IconGlyph = "Q",
				Label = "Queue",
				Value = $"x{Rules.DefaultQueuedGatherAmount}",
				TooltipText = $"One click adds {Rules.DefaultQueuedGatherAmount} runs to the action queue.",
				AccentColor = accent,
			},
			new()
			{
				IconGlyph = GameCatalog.Running.IconGlyph,
				Label = "Stride",
				Value = FormatFixedSeconds(GetCurrentStepDurationSeconds(), 2),
				TooltipText = "Current travel time for one tile step.",
				AccentColor = GameCatalog.Running.IconColor,
			},
			new()
			{
				IconGlyph = "T",
				Label = "Gather",
				Value = FormatFixedSeconds(Rules.GatherDurationSeconds, 1),
				TooltipText = "Time needed to complete each gather action.",
				AccentColor = new Color(0.91f, 0.77f, 0.42f),
			},
			new()
			{
				IconGlyph = "B",
				Label = "Bag",
				Value = $"{_characterState.GetBagCount()}/{_characterState.BagCapacity}",
				TooltipText = "Current carried load and total bag capacity.",
				AccentColor = new Color(0.82f, 0.64f, 0.38f),
			},
			new()
			{
				IconGlyph = skill.IconGlyph,
				Label = skill.DisplayName,
				Value = $"Lv.{_characterState.GetSkillLevel(skill.Id)}",
				TooltipText = $"{skill.DisplayName} level grows while this work is performed.",
				AccentColor = accent,
			},
			new()
			{
				IconGlyph = GetItemGlyph(item.Id),
				Label = "Value",
				Value = $"{item.SellPriceCoins}g each",
				TooltipText = $"{item.DisplayName} currently sells for {item.SellPriceCoins} gold each in town.",
				AccentColor = new Color(0.97f, 0.84f, 0.47f),
			},
		};

		string progressionText = $"{skill.DisplayName} rises with each run of {primaryAction.ButtonText.ToLowerInvariant()}.";
		string progressionTooltip = $"{primaryAction.Verb} here trains {skill.DisplayName}.";
		bool emphasizeProgression = false;
		SelectedResourcePanelActionViewData secondaryActionViewData = new() { Visible = false };

		if (actions.Count > 1)
		{
			ResourceActionDefinition secondaryAction = actions[1];
			SkillDefinition secondarySkill = GameCatalog.GetSkill(secondaryAction.SkillId);
			ItemDefinition secondaryItem = GameCatalog.GetItem(secondaryAction.ItemId);
			bool unlocked = IsResourceActionUnlocked(secondaryAction);
			progressionText = unlocked
				? $"{secondaryItem.DisplayName} ready. {secondaryAction.ButtonText} now yields {secondaryItem.SellPriceCoins}g goods."
				: $"{secondaryItem.DisplayName} unlock at {secondarySkill.DisplayName} Lv.{secondaryAction.MinSkillLevel}.";
			progressionTooltip = unlocked
				? $"{secondaryAction.ButtonText} is available and will gather {secondaryItem.DisplayName.ToLowerInvariant()}."
				: $"{secondaryAction.ButtonText} becomes available once {secondarySkill.DisplayName} reaches level {secondaryAction.MinSkillLevel}.";
			emphasizeProgression = unlocked;
			secondaryActionViewData = new SelectedResourcePanelActionViewData
			{
				Visible = true,
				Enabled = unlocked,
				Text = unlocked
					? $"{secondaryAction.ButtonText} x{Rules.DefaultQueuedGatherAmount}"
					: $"{secondaryAction.ButtonText} (Lv.{secondaryAction.MinSkillLevel})",
				TooltipText = unlocked
					? $"Queue {Rules.DefaultQueuedGatherAmount} runs of {secondaryAction.ButtonText.ToLowerInvariant()} at ({cell.X}, {cell.Y})."
					: $"{secondaryAction.ButtonText} needs {secondarySkill.DisplayName} level {secondaryAction.MinSkillLevel}.",
			};
		}

		_selectedResourcePanel.SetData(new SelectedResourcePanelViewData
		{
			Title = resource.DisplayName,
			Subtitle = string.IsNullOrWhiteSpace(resource.PanelSubtitle)
				? $"{primaryAction.Verb} here to deepen {skill.DisplayName}."
				: resource.PanelSubtitle,
			TagText = resource.CategoryTag,
			ShowTag = !string.IsNullOrWhiteSpace(resource.CategoryTag),
			IconGlyph = resource.IconGlyph,
			AccentColor = accent,
			Stats = stats,
			ProgressionText = progressionText,
			ProgressionTooltipText = progressionTooltip,
			EmphasizeProgression = emphasizeProgression,
			ActionNoteText = $"Yield: {item.DisplayName}  •  {item.SellPriceCoins}g each in town.",
			ShowCancelAction = ShouldShowSelectionCancelAction(),
			PrimaryAction = new SelectedResourcePanelActionViewData
			{
				Text = $"{primaryAction.ButtonText} x{Rules.DefaultQueuedGatherAmount}",
				Enabled = true,
				TooltipText = $"Queue {Rules.DefaultQueuedGatherAmount} runs of {primaryAction.ButtonText.ToLowerInvariant()} at ({cell.X}, {cell.Y}).",
			},
			SecondaryAction = secondaryActionViewData,
		});

		UpdateStatus($"Resource selected at ({cell.X}, {cell.Y}).");
	}

	private void ShowExploreActionPanel(Vector2I cell)
	{
		if (_selectedResourcePanel is null)
		{
			return;
		}

		_actionCell = cell;
		_actionResourceId = null;
		_actionPrimaryResourceActionId = null;
		_actionSecondaryResourceActionId = null;
		_actionWorkKind = WorkKind.Explore;

		ItemDefinition requirementItem = GameCatalog.GetItem(Rules.ExploreRequirementItemId);
		int availableRequirement = _townState.GetStoredCount(requirementItem.Id);
		int projectedRequirement = GetProjectedTownItemCount(requirementItem.Id);
		int missingRequirement = System.Math.Max(0, Rules.ExploreRequirementAmount - projectedRequirement);
		SkillDefinition exploringSkill = GameCatalog.Exploring;

		_selectedResourcePanel.SetData(new SelectedResourcePanelViewData
		{
			Title = "Frontier Tile",
			Subtitle = "Survey the edge of the wild and reveal fresh ground for the town.",
			TagText = "FRONTIER",
			IconGlyph = exploringSkill.IconGlyph,
			AccentColor = exploringSkill.IconColor,
			Stats = new System.Collections.Generic.List<SelectionStatChipViewData>
			{
				new()
				{
					IconGlyph = GetItemGlyph(requirementItem.Id),
					Label = "Cost",
					Value = $"{Rules.ExploreRequirementAmount} {requirementItem.DisplayName}",
					TooltipText = $"Exploration spends {Rules.ExploreRequirementAmount} {requirementItem.DisplayName.ToLowerInvariant()} from town storage.",
					AccentColor = GameCatalog.Foraging.IconColor,
				},
				new()
				{
					IconGlyph = GameCatalog.Running.IconGlyph,
					Label = "March",
					Value = FormatFixedSeconds(GetCurrentStepDurationSeconds(), 2),
					TooltipText = "Current travel time for each step toward the frontier.",
					AccentColor = GameCatalog.Running.IconColor,
				},
				new()
				{
					IconGlyph = "E",
					Label = "Survey",
					Value = FormatDuration(GetCurrentExploreDurationSeconds()),
					TooltipText = "Time needed to chart and reveal this tile.",
					AccentColor = exploringSkill.IconColor,
				},
				new()
				{
					IconGlyph = GetItemGlyph(requirementItem.Id),
					Label = "Town",
					Value = availableRequirement.ToString(),
					TooltipText = $"{requirementItem.DisplayName} currently stored in town.",
					AccentColor = GameCatalog.Foraging.IconColor,
				},
				new()
				{
					IconGlyph = exploringSkill.IconGlyph,
					Label = exploringSkill.DisplayName,
					Value = $"Lv.{_characterState.GetSkillLevel(exploringSkill.Id)}",
					TooltipText = "Exploring gains experience while time is spent surveying.",
					AccentColor = exploringSkill.IconColor,
				},
				new()
				{
					IconGlyph = "Q",
					Label = "Missing",
					Value = missingRequirement > 0 ? missingRequirement.ToString() : "Ready",
					TooltipText = missingRequirement > 0
						? $"{missingRequirement} more {requirementItem.DisplayName.ToLowerInvariant()} will be queued automatically first."
						: "Town stores already cover the exploration cost.",
					AccentColor = missingRequirement > 0 ? GameCatalog.Foraging.IconColor : exploringSkill.IconColor,
				},
			},
			ProgressionText = missingRequirement > 0
				? $"Missing {requirementItem.DisplayName.ToLowerInvariant()} will be gathered first, then the survey begins."
				: "Town stockpile is ready. Queue the survey whenever you wish.",
			ProgressionTooltipText = "Exploring gains 1 XP per second and trims explore time multiplicatively as it levels.",
			EmphasizeProgression = missingRequirement > 0,
			ActionNoteText = $"Surveying grants {Rules.ExploringXpPerSecond:0} XP per second and spends town stores.",
			ShowCancelAction = ShouldShowSelectionCancelAction(),
			PrimaryAction = new SelectedResourcePanelActionViewData
			{
				Text = "Queue Explore",
				Enabled = true,
				TooltipText = $"Queue exploration for tile ({cell.X}, {cell.Y}).",
			},
		});

		UpdateStatus($"Frontier tile selected at ({cell.X}, {cell.Y}).");
	}

	private void HideActionPanel()
	{
		_actionPrimaryResourceActionId = null;
		_actionSecondaryResourceActionId = null;
		if (_selectedResourcePanel is not null)
		{
			_selectedResourcePanel.HidePanel();
		}
	}

	private void RefreshActionPanel()
	{
		if (_selectedResourcePanel is null || !_selectedResourcePanel.Visible)
		{
			return;
		}

		if (_actionWorkKind == WorkKind.Explore)
		{
			ShowExploreActionPanel(_actionCell);
			return;
		}

		if (!string.IsNullOrWhiteSpace(_actionResourceId))
		{
			ShowActionPanel(_actionCell, GameCatalog.GetResource(_actionResourceId));
		}
	}

	private bool HasQueuedOrActiveWork()
	{
		return _activeGatherCommand is not null || _queuedCommands.Count > 0;
	}

	private bool ShouldShowSelectionCancelAction()
	{
		return HasQueuedOrActiveWork() && (_leftSidebar?.IsQueueVisible != true);
	}

	private static string FormatRequirementList(IReadOnlyList<string> requirements)
	{
		switch (requirements.Count)
		{
			case 0:
				return string.Empty;
			case 1:
				return requirements[0];
			case 2:
				return $"{requirements[0]} and {requirements[1]}";
		}

		StringBuilder builder = new();
		for (int index = 0; index < requirements.Count; index++)
		{
			if (index > 0)
			{
				builder.Append(index == requirements.Count - 1 ? " and " : ", ");
			}

			builder.Append(requirements[index]);
		}

		return builder.ToString();
	}

	private static string FormatFixedSeconds(double seconds, int decimals)
	{
		string format = decimals <= 1 ? "0.0" : "0.00";
		return $"{seconds.ToString(format, CultureInfo.InvariantCulture)}s";
	}

	private void ShowTownPanel()
	{
		UpdateTownPanel();
		if (_townUi is not null)
		{
			_townUi.Visible = true;
		}
	}

	private void HideTownPanel()
	{
		if (_townUi is not null)
		{
			_townUi.Visible = false;
		}
	}

	private void ShowCharacterPanel()
	{
		UpdateCharacterPanel();
		if (_characterPanel is not null)
		{
			_characterPanel.Visible = true;
		}
	}

	private void HideCharacterPanel()
	{
		if (_characterPanel is not null)
		{
			_characterPanel.Visible = false;
		}
	}

	private void UpdateTownPanel()
	{
		if (_townUi is null)
		{
			return;
		}

		_townUi.SetData(BuildTownViewData());
	}

	private TownViewData BuildTownViewData()
	{
		List<TownResourceViewData> resources = new();
		foreach (ItemDefinition item in GameCatalog.Items)
		{
			resources.Add(new TownResourceViewData
			{
				ItemId = item.Id,
				DisplayName = item.DisplayName,
				IconGlyph = GetItemGlyph(item.Id),
				Amount = _townState.GetStoredCount(item.Id),
				SellValue = item.SellPriceCoins,
				Selected = _selectedSellItemId == item.Id,
			});
		}

		List<BuildingCardViewData> buildings = new();
		foreach (BuildingDefinition definition in GameCatalog.Buildings)
		{
			BuildingCardViewData card = BuildBuildingCardViewData(definition);
			if (ShouldIncludeBuildingInFilter(card))
			{
				buildings.Add(card);
			}
		}

		return new TownViewData
		{
			SettlementTitle = "Starter Town",
			Gold = _townState.Gold,
			StockpileCurrent = _townState.GetStoredCountTotal(),
			StockpileCapacity = _townState.StockpileCapacity,
			StockpileSummary = $"Stockpile {_townState.GetStoredCountTotal()}/{_townState.StockpileCapacity}   Lv.{_townState.StockpileLevel}",
			StockpileUpgradeSummary =
				$"Next: {GetNextStockpileCapacity()} cap   |   {BuildStockpileUpgradeCostSummary()}   |   {GetStockpileUpgradeDurationSeconds():0}s",
			CanUpgradeStockpile = CanQueueStockpileUpgrade(),
			Resources = resources,
			SellPrompt = BuildSellPrompt(),
			SellPercent = _selectedSellPercent,
			SellAmountText = BuildSellAmountText(),
			CanSell = CanSellSelectedResources(),
			Buildings = buildings,
			ActiveFilter = _activeBuildingFilter,
			LedgerText =
				$"Town Hall  •  Stockpile Lv.{_townState.StockpileLevel}  •  Campfire  •  Builder Lv.{_characterState.GetSkillLevel(GameCatalog.Building.Id)}",
		};
	}

	private BuildingCardViewData BuildBuildingCardViewData(BuildingDefinition definition)
	{
		BuildingState state = _townState.GetBuildingState(definition.Id);
		bool isBuilt = state.IsBuilt;
		bool isMaxLevel = state.IsMaxLevel(definition);
		int targetLevel = isBuilt ? state.CurrentLevel + 1 : 1;
		targetLevel = Mathf.Clamp(targetLevel, 1, definition.MaxLevel);
		BuildingLevelDefinition level = definition.GetLevelDefinition(targetLevel);
		bool hasSkillRequirement = _characterState.GetSkillLevel(definition.RequiredSkillId) >= definition.RequiredSkillLevel;
		bool isUnlocked = hasSkillRequirement;
		bool canAfford = _townState.CanAfford(level.Costs);
		bool isQueued = HasQueuedBuildingCommand(definition.Id);
		bool canAct = isUnlocked && !state.IsUnderConstruction && !isMaxLevel && canAfford && !isQueued;

		List<TownCostViewData> costs = new();
		foreach (BuildingCostDefinition cost in level.Costs)
		{
			ItemDefinition item = GameCatalog.GetItem(cost.ItemId);
			costs.Add(new TownCostViewData
			{
				ItemId = cost.ItemId,
				ItemName = item.DisplayName,
				IconGlyph = GetItemGlyph(cost.ItemId),
				Amount = cost.Amount,
				Affordable = _townState.GetStoredCount(cost.ItemId) >= cost.Amount,
			});
		}

		string status = state.IsUnderConstruction
			? $"Constructing Lv.{state.TargetLevel}"
			: isQueued
				? "Queued"
				: isBuilt
					? "Idle"
					: "Not built";
		string requirementSkillName = GameCatalog.GetSkill(definition.RequiredSkillId).DisplayName;
		string requirementText = isUnlocked
			? $"Requires {requirementSkillName} Lv.{definition.RequiredSkillLevel}"
			: $"Locked: needs {requirementSkillName} Lv.{definition.RequiredSkillLevel}";
		string actionText = isMaxLevel
			? "Max Level"
			: isBuilt
				? "Upgrade"
				: "Build";
		string actionHint = isMaxLevel
			? "This structure has reached its finest form."
			: state.IsUnderConstruction
				? $"In progress. {Mathf.CeilToInt((float)(state.TotalSeconds - state.ProgressSeconds))}s remaining."
				: isQueued
					? "Queued in the worker command list."
					: canAfford
						? "Affordable now."
						: "Missing some materials.";

		return new BuildingCardViewData
		{
			BuildingId = definition.Id,
			DisplayName = definition.DisplayName,
			Description = definition.Description,
			IconGlyph = definition.IconGlyph,
			AccentColor = definition.AccentColor,
			CurrentLevel = state.CurrentLevel,
			MaxLevel = definition.MaxLevel,
			IsBuilt = isBuilt,
			IsUnlocked = isUnlocked,
			IsUnderConstruction = state.IsUnderConstruction,
			IsMaxLevel = isMaxLevel,
			CanAfford = canAfford,
			CanAct = canAct,
			StatusText = status,
			RequirementText = requirementText,
			TimeText = $"{(isBuilt ? "Upgrade" : "Build")} {FormatDuration(level.DurationSeconds)}",
			OutputSummary = level.OutputSummary,
			BenefitSummary = level.BenefitSummary,
			PrimaryButtonText = actionText,
			LevelText = $"Lv.{state.CurrentLevel}/{definition.MaxLevel}",
			ActionHintText = actionHint,
			Progress = (float)state.ConstructionProgress,
			ProgressText = state.IsUnderConstruction ? $"{Mathf.RoundToInt((float)(state.ConstructionProgress * 100.0))}% complete" : string.Empty,
			IsUpgradeAction = isBuilt,
			Illustration = definition.Illustration,
			Costs = costs,
		};
	}

	private bool ShouldIncludeBuildingInFilter(BuildingCardViewData card)
	{
		return _activeBuildingFilter switch
		{
			TownBuildingFilter.Available => !card.IsBuilt,
			TownBuildingFilter.Existing => card.IsBuilt,
			_ => true,
		};
	}

	private bool HasQueuedBuildingCommand(string buildingId)
	{
		if (_activeGatherCommand?.Kind == WorkKind.BuildingConstruction && _activeGatherCommand.BuildingId == buildingId)
		{
			return true;
		}

		foreach (GatherCommand command in _queuedCommands)
		{
			if (command.Kind == WorkKind.BuildingConstruction && command.BuildingId == buildingId)
			{
				return true;
			}
		}

		return false;
	}

	private string BuildSellPrompt()
	{
		if (_selectedSellItemId is null)
		{
			return "Select a resource";
		}

		ItemDefinition item = GameCatalog.GetItem(_selectedSellItemId);
		return $"Sell {item.DisplayName.ToLowerInvariant()}";
	}

	private string BuildSellAmountText()
	{
		if (_selectedSellItemId is null)
		{
			return "0%";
		}

		ItemDefinition item = GameCatalog.GetItem(_selectedSellItemId);
		int stored = _townState.GetStoredCount(item.Id);
		int amount = (stored * _selectedSellPercent) / 100;
		int gold = amount * item.SellPriceCoins;
		return $"{_selectedSellPercent}%  •  {amount} goods  •  +{gold}g";
	}

	private bool CanSellSelectedResources()
	{
		if (_selectedSellItemId is null || _selectedSellPercent <= 0)
		{
			return false;
		}

		return _townState.GetStoredCount(_selectedSellItemId) > 0;
	}

	private static string GetItemGlyph(string itemId)
	{
		return itemId switch
		{
			"sticks" => "/",
			"stones" => "O",
			"berries" => "*",
			"logs" => "=",
			_ => "+",
		};
	}

	private bool CanQueueStockpileUpgrade()
	{
		foreach (BuildingCostDefinition cost in StockpileUpgrade.Costs)
		{
			int shortfall = System.Math.Max(0, cost.Amount - GetProjectedTownItemCount(cost.ItemId));
			if (shortfall > 0 && BuildGatherCommandForItem(cost.ItemId, shortfall) is null)
			{
				return false;
			}
		}

		return true;
	}

	private void OnStockpileUpgradePressed()
	{
		if (!QueueStockpileUpgradeWithRequirements(false, out string statusMessage))
		{
			UpdateStatus(statusMessage);
			return;
		}
		HideActionPanel();
		UpdateTownPanel();
		UpdateStatus(statusMessage);
	}

	private int GetQueuedStockpileUpgradeCount()
	{
		int count = 0;
		foreach (GatherCommand command in _queuedCommands)
		{
			if (command.Kind == WorkKind.TownUpgrade && command.TownUpgradeId == "stockpile")
			{
				count++;
			}
		}

		return count;
	}

	private void QueueBuildingConstruction(string buildingId, bool upgrade)
	{
		BuildingDefinition definition = GameCatalog.GetBuilding(buildingId);
		BuildingState state = _townState.GetBuildingState(buildingId);
		if (state.IsUnderConstruction)
		{
			UpdateStatus($"{definition.DisplayName} is already under construction.");
			return;
		}

		if (HasQueuedBuildingCommand(buildingId))
		{
			UpdateStatus($"{definition.DisplayName} is already queued.");
			return;
		}

		if (_characterState.GetSkillLevel(definition.RequiredSkillId) < definition.RequiredSkillLevel)
		{
			SkillDefinition requiredSkill = GameCatalog.GetSkill(definition.RequiredSkillId);
			UpdateStatus($"{definition.DisplayName} needs {requiredSkill.DisplayName} Lv.{definition.RequiredSkillLevel}.");
			return;
		}

		if (upgrade && !state.CanUpgrade(definition))
		{
			UpdateStatus($"{definition.DisplayName} cannot be upgraded further right now.");
			return;
		}

		if (!upgrade && state.IsBuilt)
		{
			UpdateStatus($"{definition.DisplayName} is already built.");
			return;
		}

		int targetLevel = upgrade ? state.CurrentLevel + 1 : 1;
		BuildingLevelDefinition level = definition.GetLevelDefinition(targetLevel);
		if (!CanAffordProjected(level.Costs))
		{
			UpdateStatus($"{definition.DisplayName} needs more materials before it can start.");
			return;
		}

		EnqueueCommand(new GatherCommand
		{
			Kind = WorkKind.BuildingConstruction,
			BuildingId = definition.Id,
			TargetLevel = targetLevel,
			Cell = TownCell,
			TotalAmount = 1,
			RemainingAmount = 1,
			Description = $"{(upgrade ? "Upgrade" : "Build")} {definition.DisplayName} to Lv.{targetLevel}",
		});
		UpdateTownPanel();
		UpdateStatus($"Queued {(upgrade ? "upgrade" : "build")} for {definition.DisplayName}.");
	}

	private bool CanAffordProjected(IEnumerable<BuildingCostDefinition> costs)
	{
		foreach (BuildingCostDefinition cost in costs)
		{
			if (GetProjectedTownItemCount(cost.ItemId) < cost.Amount)
			{
				return false;
			}
		}

		return true;
	}

	private void UpdateCharacterPanel()
	{
		if (_characterSummaryLabel is null || _player is null)
		{
			return;
		}

		StringBuilder builder = new();
		builder.AppendLine($"Position: {_player.CurrentDisplayCell.X}, {_player.CurrentDisplayCell.Y}");
		builder.AppendLine($"Bag: {_characterState.GetBagCount()}/{_characterState.BagCapacity}");
		builder.Append("Items: ");
		for (int index = 0; index < GameCatalog.Items.Count; index++)
		{
			ItemDefinition item = GameCatalog.Items[index];
			if (index > 0)
			{
				builder.Append("  ");
			}

			builder.Append($"{item.DisplayName} {_characterState.GetBagCount(item.Id)}");
		}

		_characterSummaryLabel.Text = builder.ToString().TrimEnd();

		foreach (SkillDefinition skill in GameCatalog.Skills)
		{
			if (_skillLevelLabels.TryGetValue(skill.Id, out Label? levelLabel))
			{
				levelLabel.Text = $"Lv.{_characterState.GetSkillLevel(skill.Id)}";
			}

			if (_skillXpLabels.TryGetValue(skill.Id, out Label? xpLabel))
			{
				xpLabel.Text = $"{_characterState.GetSkillXpIntoCurrentLevel(skill.Id)}/{_characterState.GetSkillXpForNextLevel(skill.Id)}";
			}

			if (_skillProgressBars.TryGetValue(skill.Id, out ProgressBar? progressBar))
			{
				progressBar.MaxValue = _characterState.GetSkillXpForNextLevel(skill.Id);
				progressBar.Value = _characterState.GetSkillXpIntoCurrentLevel(skill.Id);
			}
		}
	}

	private void SellSelectedResources()
	{
		if (_selectedSellItemId is null)
		{
			UpdateStatus("Select a resource to sell first.");
			return;
		}

		ItemDefinition item = GameCatalog.GetItem(_selectedSellItemId);
		int percent = _selectedSellPercent;
		if (percent <= 0)
		{
			UpdateStatus($"Choose a sell percentage for {item.DisplayName} first.");
			return;
		}

		int storedCount = _townState.GetStoredCount(item.Id);
		int amountToSell = (storedCount * percent) / 100;
		if (amountToSell <= 0)
		{
			UpdateStatus($"Not enough {item.DisplayName.ToLowerInvariant()} stored to sell {percent}%.");
			return;
		}

		int soldAmount = _townState.SellStored(item.Id, amountToSell, item.SellPriceCoins);
		int goldEarned = soldAmount * item.SellPriceCoins;
		_selectedSellPercent = 0;
		if (_sellPercentSlider is not null)
		{
			_sellPercentSlider.Value = 0;
		}

		UpdateTownPanel();
		UpdateStatus($"Sold {soldAmount} {item.DisplayName.ToLowerInvariant()} for {goldEarned} gold. Stockpile {_townState.GetStoredCountTotal()}/{_townState.StockpileCapacity}.");
	}

	private void OnTownSellResourceSelected(string itemId)
	{
		_selectedSellItemId = itemId;
		UpdateTownPanel();
	}

	private void OnTownSellPercentChanged(int percent)
	{
		_selectedSellPercent = percent;
		UpdateTownPanel();
	}

	private void OnTownFilterChanged(TownBuildingFilter filter)
	{
		_activeBuildingFilter = filter;
		UpdateTownPanel();
	}

	private void OnTownBuildRequested(string buildingId)
	{
		QueueBuildingConstruction(buildingId, false);
	}

	private void OnTownUpgradeRequested(string buildingId)
	{
		QueueBuildingConstruction(buildingId, true);
	}

	private void RefreshSellList()
	{
		if (_sellItemList is null)
		{
			return;
		}

		_sellItemList.Clear();

		int selectedIndex = -1;
		for (int index = 0; index < GameCatalog.Items.Count; index++)
		{
			ItemDefinition item = GameCatalog.Items[index];
			_sellItemList.AddItem($"{item.DisplayName,-7} {_townState.GetStoredCount(item.Id),3}   ({item.SellPriceCoins}g each)");

			if (item.Id == _selectedSellItemId)
			{
				selectedIndex = index;
			}
		}

		if (selectedIndex >= 0)
		{
			_sellItemList.Select(selectedIndex);
		}

		UpdateSellSelectionLabel();
	}

	private void UpdateStockpileBar()
	{
		if (_stockpileProgressBar is not null)
		{
			_stockpileProgressBar.MaxValue = _townState.StockpileCapacity;
			_stockpileProgressBar.Value = _townState.GetStoredCountTotal();
		}

		if (_stockpileProgressLabel is not null)
		{
			_stockpileProgressLabel.Text = $"Stockpile {_townState.GetStoredCountTotal()}/{_townState.StockpileCapacity}";
		}
	}

	private void OnSellItemSelected(long index)
	{
		if (index < 0 || index >= GameCatalog.Items.Count)
		{
			return;
		}

		_selectedSellItemId = GameCatalog.Items[(int)index].Id;
		UpdateSellSelectionLabel();
	}

	private void OnSellPercentChanged(double value)
	{
		_selectedSellPercent = Mathf.RoundToInt((float)value);
		if (_sellPercentLabel is not null)
		{
			_sellPercentLabel.Text = $"{value:0}%";
		}

		UpdateSellSelectionLabel();
	}

	private void UpdateSellSelectionLabel()
	{
		if (_sellSelectionLabel is null)
		{
			return;
		}

		if (_selectedSellItemId is null)
		{
			_sellSelectionLabel.Text = "Select a resource to sell";
			return;
		}

		ItemDefinition item = GameCatalog.GetItem(_selectedSellItemId);
		int percent = _sellPercentSlider is null ? 0 : Mathf.RoundToInt((float)_sellPercentSlider.Value);
		int storedCount = _townState.GetStoredCount(item.Id);
		int amountToSell = (storedCount * percent) / 100;
		int goldValue = amountToSell * item.SellPriceCoins;

		_sellSelectionLabel.Text = $"Selected: {item.DisplayName}  Sell: {amountToSell}  Gold: {goldValue}";
	}

	private void OnActionButtonPressed()
	{
		QueueSelectedAction(_actionPrimaryResourceActionId);
	}

	private void OnSecondaryActionButtonPressed()
	{
		QueueSelectedAction(_actionSecondaryResourceActionId);
	}

	private void QueueSelectedAction(string? actionId)
	{
		if (_actionWorkKind == WorkKind.Explore)
		{
			if (!CanQueueExploreCell(_actionCell))
			{
				UpdateStatus("That tile is not reachable for exploration yet.");
				return;
			}

			if (!QueueExploreCommandWithRequirements(_actionCell, false, out _, out string statusMessage))
			{
				UpdateStatus(statusMessage);
				return;
			}

			UpdateTownPanel();
			UpdateQueuePanel();
			RefreshActionPanel();
			UpdateStatus(statusMessage);
			return;
		}

		if (_actionResourceId is null)
		{
			return;
		}

		ResourceDefinition resource = GameCatalog.GetResource(_actionResourceId);
		ResourceActionDefinition action = GetResourceAction(resource, actionId);
		if (!IsResourceActionUnlocked(action))
		{
			SkillDefinition requiredSkill = GameCatalog.GetSkill(action.SkillId);
			UpdateStatus($"{action.ButtonText} needs {requiredSkill.DisplayName} Lv.{action.MinSkillLevel}.");
			return;
		}

		GatherCommand command = CreateGatherCommand(_actionResourceId, action.Id, _actionCell, Rules.DefaultQueuedGatherAmount);
		EnqueueCommand(command);
		UpdateTownPanel();
		UpdateQueuePanel();
		RefreshActionPanel();

		UpdateStatus($"Queued {action.ButtonText.ToLowerInvariant()} x{Rules.DefaultQueuedGatherAmount} at ({_actionCell.X}, {_actionCell.Y}).");
	}

	private void ClearGatherCommand()
	{
		CancelActiveConstructionVisualState();
		_activeGatherCommand = null;
		_queuedCommands.Clear();
		_gatherProgressSeconds = 0.0;
		_activeExploreRequirementPaid = false;
		_activeTownUpgradePaid = false;
		_workerPhase = WorkerPhase.Idle;
		_queuePanelDirty = true;
		HideActionPanel();
		UpdateTownPanel();
		UpdateQueuePanel();
		UpdateStatus("Queue cleared.");
	}

	private void CancelActiveConstructionVisualState()
	{
		if (_activeGatherCommand?.Kind != WorkKind.BuildingConstruction || string.IsNullOrEmpty(_activeGatherCommand.BuildingId))
		{
			return;
		}

		BuildingState state = _townState.GetBuildingState(_activeGatherCommand.BuildingId);
		if (state.IsUnderConstruction)
		{
			state.CancelConstruction();
		}
	}

	private void UpdateCoordsLabel()
	{
		if (_coordsLabel is null || _player is null)
		{
			return;
		}

		Vector2I cell = _player.CurrentDisplayCell;
		_coordsLabel.Text = $"X: {cell.X}  Y: {cell.Y}";
	}

	private void UpdateStatus(string text)
	{
		if (_statusLabel is not null)
		{
			_statusLabel.Text = text;
		}
	}

	private void UpdateRunningSkill(double delta)
	{
		if (_player is null || !_player.IsMoving)
		{
			return;
		}

		_runningXpAccumulator += delta * Rules.RunningXpPerSecond;
		while (_runningXpAccumulator >= 1.0)
		{
			_runningXpAccumulator -= 1.0;
			_characterState.AddSkillXp(GameCatalog.Running.Id);
		}
	}

	private void UpdateExploringSkill(double delta)
	{
		_exploringXpAccumulator += delta * Rules.ExploringXpPerSecond;
		while (_exploringXpAccumulator >= 1.0)
		{
			_exploringXpAccumulator -= 1.0;
			_characterState.AddSkillXp(GameCatalog.Exploring.Id);
		}
	}

	private void UpdateBuildingSkill(double delta)
	{
		_buildingXpAccumulator += delta * Rules.BuildingXpPerSecond;
		while (_buildingXpAccumulator >= 1.0)
		{
			_buildingXpAccumulator -= 1.0;
			_characterState.AddSkillXp(GameCatalog.Building.Id);
		}
	}

	private void InitializeExploration()
	{
		for (int y = -Rules.StartingRevealRadius; y <= Rules.StartingRevealRadius; y++)
		{
			for (int x = -Rules.StartingRevealRadius; x <= Rules.StartingRevealRadius; x++)
			{
				_exploredCells.Add(new Vector2I(x, y));
			}
		}
	}

	private bool IsExplored(Vector2I cell)
	{
		return _exploredCells.Contains(cell);
	}

	private bool IsFrontierCell(Vector2I cell)
	{
		if (IsExplored(cell))
		{
			return false;
		}

		for (int y = -1; y <= 1; y++)
		{
			for (int x = -1; x <= 1; x++)
			{
				if (x == 0 && y == 0)
				{
					continue;
				}

				if (IsExplored(cell + new Vector2I(x, y)))
				{
					return true;
				}
			}
		}

		return false;
	}

	private double GetCurrentStepDurationSeconds()
	{
		int runningLevel = _characterState.GetSkillLevel(GameCatalog.Running.Id);
		double speedMultiplier = System.Math.Pow(Rules.RunningSpeedMultiplierPerLevel, runningLevel - 1);
		return Rules.BaseStepDurationSeconds / speedMultiplier;
	}

	private double GetCurrentExploreDurationSeconds()
	{
		int exploringLevel = _characterState.GetSkillLevel(GameCatalog.Exploring.Id);
		double speedMultiplier = System.Math.Pow(Rules.ExploringSpeedMultiplierPerLevel, exploringLevel - 1);
		return Rules.ExploreDurationSeconds / speedMultiplier;
	}

	private Rect2 GetVisibleWorldRect()
	{
		Transform2D screenToWorld = GetViewport().GetCanvasTransform().AffineInverse();
		Vector2 viewportSize = GetViewportRect().Size;

		Vector2 topLeft = screenToWorld * Vector2.Zero;
		Vector2 topRight = screenToWorld * new Vector2(viewportSize.X, 0.0f);
		Vector2 bottomLeft = screenToWorld * new Vector2(0.0f, viewportSize.Y);
		Vector2 bottomRight = screenToWorld * viewportSize;

		float minX = Mathf.Min(Mathf.Min(topLeft.X, topRight.X), Mathf.Min(bottomLeft.X, bottomRight.X));
		float minY = Mathf.Min(Mathf.Min(topLeft.Y, topRight.Y), Mathf.Min(bottomLeft.Y, bottomRight.Y));
		float maxX = Mathf.Max(Mathf.Max(topLeft.X, topRight.X), Mathf.Max(bottomLeft.X, bottomRight.X));
		float maxY = Mathf.Max(Mathf.Max(topLeft.Y, topRight.Y), Mathf.Max(bottomLeft.Y, bottomRight.Y));

		return new Rect2(new Vector2(minX, minY), new Vector2(maxX - minX, maxY - minY));
	}

	private Vector2 ScreenToWorld(Vector2 screenPosition)
	{
		Transform2D screenToWorld = GetViewport().GetCanvasTransform().AffineInverse();
		return screenToWorld * screenPosition;
	}

	private Vector2I WorldToCell(Vector2 worldPosition)
	{
		return new Vector2I(
			Mathf.FloorToInt(worldPosition.X / Rules.TileSize),
			Mathf.FloorToInt(worldPosition.Y / Rules.TileSize));
	}

	private bool IsClickOnCharacter(Vector2 worldPosition)
	{
		return _player is not null && worldPosition.DistanceTo(_player.GlobalPosition) <= 18.0f;
	}

	private void DrawUnexploredTile(Vector2 tileOrigin, Vector2I cell)
	{
		Rect2 tileRect = new(tileOrigin, new Vector2(Rules.TileSize, Rules.TileSize));
		bool isQueuedExplore = IsQueuedForExplore(cell);

		if (CanQueueExploreCell(cell))
		{
			DrawRect(tileRect, FrontierFogColor);
			DrawRect(tileRect, FrontierOutline, false, 1.0f);
			if (isQueuedExplore)
			{
				DrawRect(tileRect, QueuedExploreOverlay);
				DrawRect(tileRect.Grow(-1.0f), QueuedExploreOutline, false, 2.0f);
			}
			Vector2 center = tileOrigin + new Vector2(Rules.TileSize * 0.5f, Rules.TileSize * 0.5f);
			DrawCircle(center, 3.0f, isQueuedExplore ? QueuedExploreOutline : FrontierOutline);
			return;
		}

		DrawRect(tileRect, FogColor);
		if (isQueuedExplore)
		{
			DrawRect(tileRect, QueuedExploreOverlay);
			DrawRect(tileRect.Grow(-1.0f), QueuedExploreOutline, false, 2.0f);
		}
	}

	private void DrawGrassTile(Vector2 tileOrigin, Vector2I cell)
	{
		Color grassColor = ToUnitFloat(HashCell(cell.X, cell.Y, 0x3C6EF372u)) > 0.5f ? GrassA : GrassB;
		Rect2 tileRect = new(tileOrigin, new Vector2(Rules.TileSize, Rules.TileSize));

		DrawRect(tileRect, grassColor);

		float accentHeight = 8.0f + ToUnitFloat(HashCell(cell.X, cell.Y, 0xA54FF53Au)) * 10.0f;
		DrawRect(new Rect2(tileOrigin.X + 6.0f, tileOrigin.Y + 4.0f, Rules.TileSize - 12.0f, accentHeight), GrassAccent);
		DrawRect(tileRect, GridLine, false, 1.0f);
	}

	private void DrawResource(Vector2 tileOrigin, string resourceId)
	{
		switch (resourceId)
		{
			case "tree":
				DrawTree(tileOrigin);
				break;
			case "stone":
				DrawStone(tileOrigin);
				break;
			case "berries":
				DrawBerries(tileOrigin);
				break;
		}
	}

	private void DrawTree(Vector2 tileOrigin)
	{
		Vector2 center = tileOrigin + new Vector2(Rules.TileSize * 0.5f, Rules.TileSize * 0.52f);

		DrawRect(new Rect2(center.X - 4.0f, center.Y + 6.0f, 8.0f, 14.0f), TreeTrunk);
		DrawCircle(center + new Vector2(-8.0f, -2.0f), 10.0f, TreeLeafLight);
		DrawCircle(center + new Vector2(8.0f, -1.0f), 10.0f, TreeLeafLight);
		DrawCircle(center + new Vector2(0.0f, -9.0f), 12.0f, TreeLeaf);
		DrawCircle(center + new Vector2(0.0f, 2.0f), 9.0f, TreeLeaf);
	}

	private void DrawStone(Vector2 tileOrigin)
	{
		Vector2 center = tileOrigin + new Vector2(Rules.TileSize * 0.5f, Rules.TileSize * 0.62f);

		DrawCircle(center + new Vector2(-8.0f, 2.0f), 8.0f, StoneDark);
		DrawCircle(center + new Vector2(4.0f, -1.0f), 10.0f, StoneLight);
		DrawCircle(center + new Vector2(11.0f, 5.0f), 6.0f, StoneDark);
	}

	private void DrawBerries(Vector2 tileOrigin)
	{
		Vector2 center = tileOrigin + new Vector2(Rules.TileSize * 0.5f, Rules.TileSize * 0.64f);

		DrawRect(new Rect2(center.X - 1.5f, center.Y - 12.0f, 3.0f, 13.0f), BerryLeaf);
		DrawCircle(center + new Vector2(-6.0f, -5.0f), 4.0f, BerryFruit);
		DrawCircle(center + new Vector2(0.0f, -8.0f), 4.0f, BerryFruit);
		DrawCircle(center + new Vector2(6.0f, -4.0f), 4.0f, BerryFruit);
		DrawCircle(center + new Vector2(-2.0f, -13.0f), 3.0f, BerryLeaf);
		DrawCircle(center + new Vector2(3.0f, -12.0f), 3.0f, BerryLeaf);
	}

	private void DrawTown(Vector2 tileOrigin)
	{
		Vector2 center = tileOrigin + new Vector2(Rules.TileSize * 0.5f, Rules.TileSize * 0.5f);

		DrawRect(new Rect2(tileOrigin.X + 8.0f, tileOrigin.Y + 18.0f, Rules.TileSize - 16.0f, 18.0f), TownWall);
		DrawColoredPolygon(
			new[]
			{
				new Vector2(center.X, tileOrigin.Y + 6.0f),
				new Vector2(tileOrigin.X + 6.0f, tileOrigin.Y + 22.0f),
				new Vector2(tileOrigin.X + Rules.TileSize - 6.0f, tileOrigin.Y + 22.0f),
			},
			TownRoof);
		DrawRect(new Rect2(center.X - 4.0f, tileOrigin.Y + 24.0f, 8.0f, 12.0f), TownDoor);
	}

	private static string? GetResourceId(Vector2I cell)
	{
		string? starterResourceId = GetStarterResourceId(cell);
		if (starterResourceId is not null)
		{
			return starterResourceId;
		}

		float treeField = SampleField(cell.X, cell.Y, 0.16f, 0.11f, 0.8f);
		float stoneField = SampleField(cell.X, cell.Y, 0.07f, 0.09f, 2.2f);
		float berryField = SampleField(cell.X, cell.Y, 0.22f, 0.18f, -1.4f);
		float roll = ToUnitFloat(HashCell(cell.X, cell.Y, 0x68BC21EBu));

		if (treeField > 0.78f && roll > 0.965f)
		{
			return GameCatalog.Tree.Id;
		}

		if (stoneField > 0.82f && roll > 0.972f)
		{
			return GameCatalog.Stone.Id;
		}

		if (berryField > 0.84f && roll > 0.975f)
		{
			return GameCatalog.BerryBush.Id;
		}

		return null;
	}

	private static string? GetStarterResourceId(Vector2I cell)
	{
		return cell switch
		{
			{ X: -1, Y: -1 } => GameCatalog.Tree.Id,
			{ X: 1, Y: -1 } => GameCatalog.Stone.Id,
			{ X: 0, Y: -1 } => GameCatalog.BerryBush.Id,
			{ X: -1, Y: 1 } => GameCatalog.Tree.Id,
			{ X: 1, Y: 1 } => GameCatalog.Stone.Id,
			{ X: 0, Y: 2 } => GameCatalog.BerryBush.Id,
			_ => null,
		};
	}

	private static Vector2I GetNextStep(Vector2I from, Vector2I to)
	{
		int nextX = from.X == to.X ? from.X : from.X + Mathf.Sign(to.X - from.X);
		int nextY = from.Y == to.Y ? from.Y : from.Y + Mathf.Sign(to.Y - from.Y);
		return new Vector2I(nextX, nextY);
	}

	private static int GetStepDistance(Vector2I from, Vector2I to)
	{
		return System.Math.Max(System.Math.Abs(to.X - from.X), System.Math.Abs(to.Y - from.Y));
	}

	private static float SampleField(int x, int y, float scaleX, float scaleY, float phase)
	{
		float xf = x;
		float yf = y;

		return (
			Mathf.Sin(xf * scaleX + phase) +
			Mathf.Cos(yf * scaleY - (phase * 0.7f)) +
			Mathf.Sin((xf + yf) * ((scaleX + scaleY) * 0.45f) + (phase * 1.3f))
		) / 3.0f;
	}

	private static float ToUnitFloat(uint value)
	{
		return (value & 0x00FFFFFFu) / 16777215.0f;
	}

	private static uint HashCell(int x, int y, uint salt)
	{
		unchecked
		{
			uint hash = Rules.WorldSeed ^ salt;
			hash += (uint)x * 0x9E3779B9u;
			hash ^= (uint)y * 0x85EBCA6Bu;
			hash ^= hash >> 16;
			hash *= 0x7FEB352Du;
			hash ^= hash >> 15;
			hash *= 0x846CA68Bu;
			hash ^= hash >> 16;
			return hash;
		}
	}
}
