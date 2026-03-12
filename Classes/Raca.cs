using Godot;

[GlobalClass]
public partial class Raca : Resource
{
	[Export]
	public NomeRaca Race_Name { get; set; }

	[Export]
	public int Max_Power_level_on_start { get; set; }

	[Export]
	public string Genetics { get; set; } = string.Empty;

	[Export]
	public float RaceLatentPower { get; set; }

	[Export]
	public int lifespan { get; set; }

	public enum NomeRaca
	{
		Saiyan,
		Half_Saiyan,
		Human,
		Namekian,
		Majin,
		Frost_Demon,
		Core_People,
		Cerealjin,
		Shadow_Dragon,
		Android,
		BioAnodrid,
	}
}