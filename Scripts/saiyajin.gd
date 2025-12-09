extends Player
@export var raca_:String

func _ready() -> void:
	super.Set_animation()
	if Sexo == 0:
		$Body.texture = load("res://Icones/Humanoid body/NewPaleMale.png")
	else:
		$Body.texture = load("res://Icones/Humanoid body/NewPaleFemale.png")

	
