using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace IdleNet;

public partial class TownUiSmokeTest : Node
{
	[Export]
	public PackedScene? MainScene { get; set; }

	private readonly List<string> _failures = new();
	private InfiniteWorld? _world;
	private TownUI? _townUi;

	public override void _Ready()
	{
		CallDeferred(nameof(StartSmokeRun));
	}

	private async void StartSmokeRun()
	{
		try
		{
			if (MainScene is null)
			{
				Fail("Main scene was not assigned to the town UI smoke test.");
				Finish();
				return;
			}

			_world = MainScene.Instantiate<InfiniteWorld>();
			AddChild(_world);

			await WaitFrames(3);

			_townUi = _world.GetNode<TownUI>("Hud/TownUI");
			_world.OpenTownUi();
			await WaitFrames(2);

			Require(_world.IsTownUiOpen(), "Town UI should open through the world controller.");
			Require(_townUi.IsVisibleInTree(), "Town UI should be visible in the scene tree after opening.");

			Control frame = _townUi.GetNode<Control>("Frame");
			Button closeButton = _townUi.GetNode<Button>("Frame/OuterMargin/RootColumn/CloseButton");
			ProgressBar stockpileBar = _townUi.GetNode<ProgressBar>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/ResourcePanel/ResourceColumn/StockpileBar");
			Button stockpileUpgradeButton = _townUi.GetNode<Button>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/ResourcePanel/ResourceColumn/StockpileUpgradeButton");
			VBoxContainer resourceList = _townUi.GetNode<VBoxContainer>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/ResourcePanel/ResourceColumn/ResourceList");
			Label sellPromptLabel = _townUi.GetNode<Label>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/SellPanel/SellColumn/SellPromptLabel");
			Label sellValueLabel = _townUi.GetNode<Label>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/SellPanel/SellColumn/SellSummaryRow/SellValueLabel");
			HSlider sellSlider = _townUi.GetNode<HSlider>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/SellPanel/SellColumn/SellSlider");
			Button sellButton = _townUi.GetNode<Button>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/SellPanel/SellColumn/SellButton");
			ScrollContainer bodyScroll = _townUi.GetNode<ScrollContainer>("Frame/OuterMargin/RootColumn/BodyScroll");
			ScrollContainer buildingScroll = _townUi.GetNode<ScrollContainer>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/BuildPanel/BuildColumn/BuildingScroll");
			VBoxContainer buildingList = _townUi.GetNode<VBoxContainer>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/BuildPanel/BuildColumn/BuildingScroll/BuildingList");

			AssertInsideViewport(frame, "Town frame");
			AssertInsideViewport(closeButton, "Town close button");
			AssertInsideViewport(stockpileBar, "Stockpile bar");

			Require(stockpileBar.IsVisibleInTree(), "Stockpile bar should be visible when the town UI is open.");
			Require(stockpileUpgradeButton.IsVisibleInTree(), "Stockpile upgrade button should be visible when the town UI is open.");
			Require(stockpileUpgradeButton.Disabled, "Stockpile upgrade button should start disabled before materials are gathered.");
			Require(resourceList.GetChildCount() > 0, "Town resource rows should be populated.");
			Require(buildingList.GetChildCount() > 0, "Build & Upgrade cards should be populated.");
			Require(bodyScroll.IsVisibleInTree(), "Town body scroll should stay visible so lower sections remain reachable.");
			Require(buildingScroll.Size.Y > 0.0f, "Building scroll container should have usable height.");
			Require(closeButton.IsVisibleInTree(), "Close button should be visible.");

			bodyScroll.EnsureControlVisible(sellButton);
			await WaitFrames(3);
			AssertInsideViewport(sellSlider, "Sell slider");
			AssertInsideViewport(sellButton, "Sell button");

			string initialPrompt = sellPromptLabel.Text;
			_townUi.RequestSellResourceSelection("sticks");
			await WaitFrames(4);
			Require(sellPromptLabel.Text != initialPrompt, "Selecting a resource row should update the sell prompt.");

			_townUi.RequestSellPercent(50);
			await WaitFrames(4);
			Require(sellValueLabel.Text.Contains("50%"), "Sell summary should reflect slider changes.");
			Require(Mathf.IsEqualApprox((float)sellSlider.Value, 50.0f), "Sell slider should move to the requested percentage.");
			Require(sellButton.Disabled, "Sell button should stay disabled at a fresh start because there is nothing stored to sell.");

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
