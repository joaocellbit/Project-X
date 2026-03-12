using Godot;

public partial class saiyajin : Player
{
	[Export]
	public string raca_ { get; set; } = string.Empty;

	public override void _Ready()
	{
		base._Ready();

		Sprite2D body = GetNodeOrNull<Sprite2D>("Body");
		if (body == null)
		{
			return;
		}

		string texturePath = Sexo == sexo.M
			? "res://Icones/Humanoid body/NewPaleMale.png"
			: "res://Icones/Humanoid body/NewPaleFemale.png";

		body.Texture = ResourceLoader.Load<Texture2D>(texturePath);
	}
}