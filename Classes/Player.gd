extends CharacterBody2D
class_name Player

@export var id:int
@export var Nome:String
@export var Max_Health:int
@export var race:Raca
@export var Power_level:float #Calculo baseado no ki 
@export var Sexo:sexo
@export var Age:int
@export var Fisico:int #Força e durabilidade
@export var Agilidade:int #velocidade de movimento e Acerto de golpes fisicos
@export var Ki:int #poder bruto do ki, multiplica os atributos fisicos
@export var KiControl:int #diminui o consumo do ki
@export var Reflexo:int #Aumenta a chance de desviar de golpes
@export var magic:int #Chance de aprender técnicas magicas
@export var LatentPower:float #Define o potencial da pessoa
@onready var animationtree:Node #node do animation tree
@onready var CoordAnima:Vector2 
@onready var is_flying:bool

func Set_animation() -> void:
	var tree:Array [Node] = get_children()
	for i in tree:
		if i is AnimationTree:
			animationtree = i

func Set_id() -> void:
	id = multiplayer.get_unique_id()

enum sexo{
	M,F
}

func _physics_process(_delta: float) -> void:
	if is_multiplayer_authority():
		var diry:float = Input.get_axis("ui_up", "ui_down")
		var dirx:float = Input.get_axis("ui_left","ui_right")
		var dir:Vector2 = Vector2(dirx,diry)
		velocity = dir.normalized()*100
		move_and_slide()
		
		var playback = animationtree.get("parameters/StateMachine/playback")
		if dir:
			CoordAnima = Vector2(round(dir.x), round(dir.y))
		update_animation(CoordAnima, velocity)
		atualizar_posicao.rpc(velocity,position,CoordAnima)
		if Input.is_action_just_pressed("Punch"):
			playback.travel("Punch")
	
@rpc("authority","call_remote","reliable")
func atualizar_posicao(vel:Vector2, posicao:Vector2, coordanima_server:Vector2):
	position = posicao
	update_animation(coordanima_server, vel)
	

func update_animation(coordanima:Vector2, vel:Vector2 ):
	var playback = animationtree.get("parameters/StateMachine/playback")
	if vel:
		playback.travel("Walk")
		animationtree.set("parameters/StateMachine/Idle/blend_position", coordanima)
		animationtree.set("parameters/StateMachine/Punch/blend_position", coordanima)
		animationtree.set("parameters/StateMachine/Walk/blend_position", coordanima)
	else:
		playback.travel("Idle")
