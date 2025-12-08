extends CharacterBody2D
class_name Player

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

func Set_race() -> void:
	var _Racas = race.NomeRaca.keys()
	
	
	
	
enum sexo{
	M,F
}

func _physics_process(_delta: float) -> void:
	pass
