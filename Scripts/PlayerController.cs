using Godot;

namespace IdleNet;

public partial class PlayerController : CharacterBody2D
{
	public bool ShowProgressBar { get; private set; }

	public float ProgressBarRatio { get; private set; }

	public Vector2I CurrentCell { get; private set; }

	public Vector2I CurrentDisplayCell =>
		new(
			Mathf.RoundToInt((GlobalPosition.X - (TileSize * 0.5f)) / TileSize),
			Mathf.RoundToInt((GlobalPosition.Y - (TileSize * 0.5f)) / TileSize));

	public bool IsMoving { get; private set; }

	private int TileSize { get; set; } = 48;

	private Vector2I _moveStartCell;
	private Vector2I _moveTargetCell;
	private double _moveDurationSeconds = 2.0;
	private double _moveElapsedSeconds;

	public override void _Ready()
	{
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		if (!IsMoving)
		{
			return;
		}

		_moveElapsedSeconds += delta;
		float t = Mathf.Clamp((float)(_moveElapsedSeconds / _moveDurationSeconds), 0.0f, 1.0f);
		GlobalPosition = CellToWorld(_moveStartCell).Lerp(CellToWorld(_moveTargetCell), t);

		if (t < 1.0f)
		{
			return;
		}

		CurrentCell = _moveTargetCell;
		GlobalPosition = CellToWorld(CurrentCell);
		IsMoving = false;
	}

	public override void _Draw()
	{
		DrawCircle(Vector2.Zero, 14.0f, new Color(0.98f, 0.95f, 0.78f));
		DrawCircle(new Vector2(0.0f, -2.0f), 9.0f, new Color(0.18f, 0.24f, 0.18f));
		DrawCircle(new Vector2(-3.0f, -4.0f), 1.6f, Colors.White);
		DrawCircle(new Vector2(3.0f, -4.0f), 1.6f, Colors.White);
		DrawRect(new Rect2(-6.0f, 10.0f, 12.0f, 8.0f), new Color(0.27f, 0.43f, 0.85f));

		if (!ShowProgressBar)
		{
			return;
		}

		Rect2 barBackground = new(-16.0f, 22.0f, 32.0f, 6.0f);
		Rect2 barFill = new(-15.0f, 23.0f, 30.0f * ProgressBarRatio, 4.0f);
		DrawRect(barBackground, new Color(0.12f, 0.16f, 0.12f, 0.95f));
		DrawRect(barFill, new Color(0.92f, 0.83f, 0.36f, 1.0f));
	}

	public void SetProgressBar(bool visible, float ratio)
	{
		bool changed = ShowProgressBar != visible || !Mathf.IsEqualApprox(ProgressBarRatio, ratio);
		ShowProgressBar = visible;
		ProgressBarRatio = Mathf.Clamp(ratio, 0.0f, 1.0f);

		if (changed)
		{
			QueueRedraw();
		}
	}

	public void Initialize(Vector2I startCell, int tileSize)
	{
		TileSize = tileSize;
		CurrentCell = startCell;
		_moveStartCell = startCell;
		_moveTargetCell = startCell;
		IsMoving = false;
		GlobalPosition = CellToWorld(startCell);
	}

	public void BeginStep(Vector2I nextCell, double durationSeconds)
	{
		if (IsMoving || nextCell == CurrentCell)
		{
			return;
		}

		_moveStartCell = CurrentCell;
		_moveTargetCell = nextCell;
		_moveDurationSeconds = durationSeconds;
		_moveElapsedSeconds = 0.0;
		IsMoving = true;
	}

	private Vector2 CellToWorld(Vector2I cell)
	{
		return new Vector2(
			(cell.X * TileSize) + (TileSize * 0.5f),
			(cell.Y * TileSize) + (TileSize * 0.5f));
	}
}
