extends Node2D

@export var planet_name:String
@export var planet_infos:Planets
@export var noise:Noise

func _ready() -> void:
	var type = planet_infos.PlanetType.keys()
	print(type[planet_infos.Planet_Type])
