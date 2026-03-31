using System;
using System.Collections.Generic;
using Godot;

namespace IdleNet;

public partial class VillagerFilterBar : Control
{
    public event Action<string>? FilterChanged;

    private readonly Dictionary<string, Button> _buttons = new();

    public override void _Ready()
    {
        RegisterButton("all", "RootFlow/AllButton");
        RegisterButton("idle", "RootFlow/IdleButton");
        RegisterButton("assigned", "RootFlow/AssignedButton");
        RegisterButton("builders", "RootFlow/BuildersButton");
        RegisterButton("carrying", "RootFlow/CarryingButton");
        SetActiveFilter("all");
    }

    public void SetActiveFilter(string filterId)
    {
        foreach (KeyValuePair<string, Button> entry in _buttons)
        {
            bool active = entry.Key == filterId;
            entry.Value.Modulate = active ? Colors.White : new Color(0.90f, 0.86f, 0.78f, 0.88f);
            entry.Value.Scale = active ? new Vector2(1.02f, 1.02f) : Vector2.One;
        }
    }

    private void RegisterButton(string filterId, string path)
    {
        Button button = GetNode<Button>(path);
        button.Pressed += () => FilterChanged?.Invoke(filterId);
        SelectionPanelStyles.ApplyActionButtonStyle(button, new Color(0.70f, 0.58f, 0.34f), false, new Vector2(72.0f, 28.0f), 11);
        _buttons[filterId] = button;
    }
}
