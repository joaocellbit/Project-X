extends Node

@onready var playerId
@onready var player_planet:Dictionary  # player_id -> planet_id
@onready var Planet_player:Dictionary  # planet_id -> Array[player_id]
@onready var Server = ENetMultiplayerPeer.new()
# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(_delta: float) -> void:
	pass

func criar_server(Porta:int) -> void:
	var err = Server.create_server(Porta)
	if err != OK:
		print("erro ao criar server")
		return
	multiplayer.multiplayer_peer = Server
	print("ok")
	multiplayer.peer_connected.connect(New_connection)



func criar_cliente(ip:String, Porta:int) -> void:
	Server.create_client(ip,Porta)
	multiplayer.multiplayer_peer = Server
	

func New_connection(id) -> void:
	print(id)
