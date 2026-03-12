using Godot;

public partial class namek : Player
{
    [Export]
    public string _raca { get; set; }
    
    public override void _Ready()
    {
        base._Ready();
        
        Sprite2D body = GetNodeOrNull<Sprite2D>("Body");

        
    }
    
}