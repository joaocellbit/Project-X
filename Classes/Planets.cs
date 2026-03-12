using Godot;

[GlobalClass]
public partial class Planets : Resource
{
	[Export]
	public Vector2 Planet_size { get; set; }

	[Export]
	public PlanetType Planet_Type { get; set; }

	[Export]
	public float gravity { get; set; }

	public enum PlanetType
	{
		Garden,
		Barren,
		desert,
		water_world,
		Gas_Giant,
	}
}