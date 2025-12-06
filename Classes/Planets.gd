extends Resource
class_name Planets

@export var Planet_size:Vector2
@export var Planet_Type:PlanetType
@export var gravity:float




enum PlanetType{
	Garden,
	Barren,
	desert,
	water_world,
	Gas_Giant
}
