extends Node2D


# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass# Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(_delta: float) -> void:
	pass


func _on_hostear_pressed() -> void:
	Server.criar_server(30000) # Replace with function body.


func _on_join_pressed() -> void:
	Server.criar_cliente("127.0.0.1",30000) # Replace with function body.
