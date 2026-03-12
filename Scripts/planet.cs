using Godot;

public partial class planet : Node2D
{
	[Export]
	public int planetid { get; set; }

	[Export]
	public string planet_name { get; set; } = string.Empty;

	[Export]
	public Planets planet_infos { get; set; }

	[Export]
	public Noise noise { get; set; }

	public override void _Ready()
	{
		if (planet_infos == null)
		{
			return;
		}

		string[] typeNames = System.Enum.GetNames(typeof(Planets.PlanetType));
		GD.Print(typeNames[(int)planet_infos.Planet_Type]);
	}
}