extends Node
@onready var playerid:Dictionary # player_id -> nome
@onready var player_state:Dictionary # player_id -> body,clothes,hair etc
@onready var planetid:Dictionary # planet_id -> nome
@onready var player_planet:Dictionary  # player_id -> planet_id
@onready var Planet_player:Dictionary  # planet_id -> Array[player_id]
@onready var npc_planet:Dictionary # npc_id -> planet_id
@onready var planet_npc:Dictionary # planet_id -> array[Npc_id]
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
	playerid[1] = "server"
	print(playerid)
	multiplayer.peer_connected.connect(New_connection)



func criar_cliente(ip:String, Porta:int) -> void:
	Server.create_client(ip,Porta)
	multiplayer.multiplayer_peer = Server
	

func New_connection(id) -> void:
	print(id)
	playerid[id] = "test"
	print(playerid,  multiplayer.get_unique_id())
	rpc("update_client", playerid)

@rpc("authority","call_remote","reliable")
func update_client(ids:Dictionary):
	playerid = ids
	print(playerid, multiplayer.get_unique_id())
