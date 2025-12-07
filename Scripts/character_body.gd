extends AnimatedSprite2D


# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(_delta: float) -> void:
	pass


func _on_animation_changed() -> void:
	if animation != "FlyNorth" and animation != "IdleNorth" and animation != "WalkNorth" and animation != "Downed" and animation != "Meditating":
		$Eye.visible = true
		$Eye.play(animation)
	else:
		$Eye.visible = false # Replace with function body.
	$Tail.play(animation)
