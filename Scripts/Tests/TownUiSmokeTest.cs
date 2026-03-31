using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace IdleNet;

public partial class TownUiSmokeTest : Node
{
	[Export]
	public PackedScene? MainScene { get; set; }

	[Export]
	public float TimeScale { get; set; } = 16.0f;

	private readonly List<string> _failures = new();
	private InfiniteWorld? _world;
	private TownUI? _townUi;
	private double _previousTimeScale = 1.0;

	public override void _Ready()
	{
		CallDeferred(nameof(StartSmokeRun));
	}

	private async void StartSmokeRun()
	{
		try
		{
			_previousTimeScale = Engine.TimeScale;
			if (TimeScale > 0.0f)
			{
				Engine.TimeScale = TimeScale;
			}

			if (MainScene is null)
			{
				Fail("Main scene was not assigned to the town UI smoke test.");
				Finish();
				return;
			}

			_world = MainScene.Instantiate<InfiniteWorld>();
			AddChild(_world);

			await WaitFrames(3);

			_townUi = _world.GetNode<TownUI>("Hud/GameHUD/RootMargin/RootColumn/MainRow/OverviewPanel/OuterMargin/RootColumn/ContextFrame/ContextMargin/ContextColumn/ContextHost/TownUI");
			GameHUD hud = _world.GetNode<GameHUD>("Hud/GameHUD");
			_world.OpenTownUi();
			await WaitFrames(2);

			Require(_world.IsTownUiOpen(), "Town UI should open through the world controller.");
			Require(_townUi.IsVisibleInTree(), "Town UI should be visible in the scene tree after opening.");
			Require(hud.CurrentSection == HudSection.Town, "Opening the town UI should switch the HUD into the Town section.");

			Control frame = _townUi.GetNode<Control>("Frame");
			ScrollContainer bodyScroll = _townUi.GetNode<ScrollContainer>("Frame/OuterMargin/RootColumn/BodyScroll");
			PanelContainer commercePanel = _townUi.GetNode<PanelContainer>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/CommercePanel");
			PanelContainer overviewPanel = _townUi.GetNode<PanelContainer>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/OverviewPanel");
			PanelContainer buildPanel = _townUi.GetNode<PanelContainer>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/BuildPanel");
			ProgressBar stockpileBar = _townUi.GetNode<ProgressBar>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/CommercePanel/CommerceMargin/CommerceColumn/StockpileBar");
			Button stockpileUpgradeButton = _townUi.GetNode<Button>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/CommercePanel/CommerceMargin/CommerceColumn/StockpileMetaRow/StockpileUpgradeButton");
			VBoxContainer resourceList = _townUi.GetNode<VBoxContainer>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/CommercePanel/CommerceMargin/CommerceColumn/CommerceRow/ResourceColumn/ResourceList");
			Label sellPromptLabel = _townUi.GetNode<Label>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/CommercePanel/CommerceMargin/CommerceColumn/CommerceRow/SellColumn/SellPromptLabel");
			Label sellValueLabel = _townUi.GetNode<Label>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/CommercePanel/CommerceMargin/CommerceColumn/CommerceRow/SellColumn/SellSummaryRow/SellValueLabel");
			HSlider sellSlider = _townUi.GetNode<HSlider>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/CommercePanel/CommerceMargin/CommerceColumn/CommerceRow/SellColumn/SellSlider");
			Button sellButton = _townUi.GetNode<Button>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/CommercePanel/CommerceMargin/CommerceColumn/CommerceRow/SellColumn/SellButton");
			Button openWorksButton = _townUi.GetNode<Button>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/OverviewPanel/OverviewMargin/OverviewColumn/OverviewHeaderRow/OpenWorksButton");
			VBoxContainer buildingList = _townUi.GetNode<VBoxContainer>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/BuildPanel/BuildMargin/BuildColumn/BuildingList");
			Button closeButton = _townUi.GetNode<Button>("Frame/OuterMargin/RootColumn/FooterPanel/FooterMargin/FooterRow/CloseButton");

			AssertInsideViewport(frame, "Town frame");
			AssertInsideViewport(stockpileBar, "Stockpile bar");
			AssertInsideViewport(sellSlider, "Sell slider");
			AssertInsideViewport(sellButton, "Sell button");
			AssertInsideViewport(closeButton, "Close button");
			AssertInsideViewport(openWorksButton, "Open Works button");

			Require(bodyScroll.IsVisibleInTree(), "Town body scroll should be visible.");
			Require(commercePanel.IsVisibleInTree(), "Commerce panel should be visible in the Town overview.");
			Require(overviewPanel.IsVisibleInTree(), "Town overview panel should be visible in the Town section.");
			Require(!buildPanel.IsVisibleInTree(), "Detailed build panel should stay hidden until the works view is opened.");
			Require(stockpileBar.IsVisibleInTree(), "Stockpile bar should be visible when the town UI is open.");
			Require(stockpileUpgradeButton.IsVisibleInTree(), "Stockpile upgrade button should be visible when the town UI is open.");
			Require(!stockpileUpgradeButton.Disabled, "Stockpile upgrade button should stay available because missing materials auto-queue now.");
			Require(resourceList.GetChildCount() > 0, "Town resource rows should be populated.");
			Require(openWorksButton.IsVisibleInTree(), "Open Works button should be visible in the town overview.");
			Require(closeButton.IsVisibleInTree(), "Close button should be visible.");

			string initialPrompt = sellPromptLabel.Text;
			_townUi.RequestSellResourceSelection("sticks");
			await WaitFrames(4);
			Require(sellPromptLabel.Text != initialPrompt, "Selecting a resource row should update the sell prompt.");

			_townUi.RequestSellPercent(50);
			await WaitFrames(4);
			Require(sellValueLabel.Text.Contains("50%"), "Sell summary should reflect slider changes.");
			Require(Mathf.IsEqualApprox((float)sellSlider.Value, 50.0f), "Sell slider should move to the requested percentage.");
			Require(sellButton.Disabled, "Sell button should stay disabled at a fresh start because there is nothing stored to sell.");

			_townUi.RequestOpenWorks();
			await WaitFrames(4);
			Require(hud.CurrentSection == HudSection.Buildings, "Open Works should switch to the Buildings section.");
			Require(buildPanel.IsVisibleInTree(), "Build panel should be visible in the Buildings section.");
			Require(!overviewPanel.IsVisibleInTree(), "Town overview should hide when the Buildings section is active.");
			Require(buildingList.GetChildCount() > 0, "Build & Upgrade cards should be populated in the Buildings section.");

			if (buildingList.GetChildCount() > 0 && buildingList.GetChild(0) is Control firstCard)
			{
				AssertInsideViewport(firstCard, "First building card");
			}

			_townUi.RequestClose();
			await WaitFrames(4);
			Require(!_world.IsTownUiOpen(), "Town UI should close when the close button is pressed.");

			Finish();
		}
		catch (Exception exception)
		{
			Fail($"Unhandled exception during town UI smoke test: {exception}");
			Finish();
		}
	}

	private async Task WaitFrames(int count)
	{
		for (int index = 0; index < count; index++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private void AssertInsideViewport(Control control, string label)
	{
		Rect2 viewportRect = GetViewport().GetVisibleRect();
		Rect2 controlRect = control.GetGlobalRect();
		float right = viewportRect.Position.X + viewportRect.Size.X;
		float bottom = viewportRect.Position.Y + viewportRect.Size.Y;

		bool inside =
			controlRect.Position.X >= viewportRect.Position.X - 1.0f &&
			controlRect.Position.Y >= viewportRect.Position.Y - 1.0f &&
			controlRect.End.X <= right + 1.0f &&
			controlRect.End.Y <= bottom + 1.0f;

		Require(inside, $"{label} should be fully inside the viewport, but was {controlRect} within {viewportRect}.");
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			Fail(message);
		}
	}

	private void Fail(string message)
	{
		_failures.Add(message);
		GD.PushError($"TOWN_UI_SMOKE: {message}");
	}

	private void Finish()
	{
		Engine.TimeScale = _previousTimeScale;

		if (_failures.Count == 0)
		{
			GD.Print("TOWN_UI_SMOKE:PASS");
			GetTree().Quit(0);
			return;
		}

		GD.PrintErr("TOWN_UI_SMOKE:FAIL");
		foreach (string failure in _failures)
		{
			GD.PrintErr($"TOWN_UI_SMOKE: {failure}");
		}

		GetTree().Quit(1);
	}
}
