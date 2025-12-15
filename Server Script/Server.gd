extends Node
@onready var playerid:Dictionary[int, String] # player_id -> nome
@onready var player_state:Dictionary[int,Dictionary] # player_id -> body,clothes,hair etc
@onready var planetid:Dictionary[int,String] # planet_id -> nome
@onready var player_planet:Dictionary[int,int]  # player_id -> planet_id
@onready var Planet_player:Dictionary[int,Array]  # planet_id -> Array[player_id]
@onready var npc_planet:Dictionary[int,int] # npc_id -> planet_id
@onready var planet_npc:Dictionary[int, Array] # planet_id -> array[Npc_id]
@onready var Server = ENetMultiplayerPeer.new()
signal conectado
@onready var scene = get_tree().root.get_node("MainWorld")
# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(_delta: float) -> void:
	pass

func criar_server(Porta:int) -> bool:
	var err = Server.create_server(Porta)
	if err != OK:
		print("erro ao criar server")
		return false
	multiplayer.multiplayer_peer = Server
	print("ok")
	playerid[1] = "server"
	print(playerid)
	multiplayer.peer_connected.connect(New_connection)
	return true



func criar_cliente(ip:String, Porta:int) -> void:
	Server.create_client(ip,Porta)
	multiplayer.multiplayer_peer = Server
	multiplayer.connected_to_server.connect(client_info)
	multiplayer.server_disconnected.connect(End_connection)
	
func client_info() ->void:
	conectado.emit()
	multiplayer.connected_to_server.disconnect(client_info)
	print("enviando dados do cliente de id: ", multiplayer.get_unique_id())
	var perso_adm = preload("res://Cenas/Saiyajin.tscn").instantiate()
	perso_adm.name = str(1)
	scene.get_node("Planet").add_child(perso_adm)
	var state_test:Dictionary = {"nome" = "Test", "Raca" = "test"}
	rpc_id(1, "send_client_info", multiplayer.get_unique_id(), state_test)
	
@rpc("any_peer","call_remote","reliable")
func send_client_info(id:int,data:Dictionary) -> void:
	player_state[id] = data
	print(player_state ," ", multiplayer.get_unique_id())
	
func New_connection(id) -> void:
	print(id)
	playerid[id] = "test"
	print(playerid,  multiplayer.get_unique_id())
	rpc("update_client", playerid)
	spawn_character(id)

func spawn_character(id:int):
	var perso = preload("res://Cenas/Saiyajin.tscn").instantiate()
	perso.name = str(id)
	perso.set_multiplayer_authority(id)
	scene.get_node("Planet").add_child(perso)

@rpc("authority","call_remote","reliable")
func update_client(ids:Dictionary):
	playerid = ids
	print(playerid, multiplayer.get_unique_id())

func End_connection() -> void:
	print("conexão com server perdida")
	multiplayer.multiplayer_peer = null
	multiplayer.server_disconnected.disconnect(End_connection)
	playerid = {}
	player_state = {}
	var menu = preload("res://Cenas/menu.tscn")
	get_tree().root.get_node("MainWorld").add_child(menu.instantiate())
	
