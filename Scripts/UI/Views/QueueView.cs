using Godot;

namespace IdleNet;

public partial class QueueView : Control
{
    private QueuePanel? _queuePanel;

    public QueuePanel? QueuePanel => _queuePanel;

    public override void _Ready()
    {
        _queuePanel = GetNode<QueuePanel>("QueuePanel");
    }
}
