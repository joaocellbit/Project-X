extends CharacterBody2D
class_name Player

@export var Nome:String
@export var Max_Health:int
@export var race:Raca
@export var Power_level:int
@export var Sexo:sexo

func Set_race() -> void:
	var Racas = race.NomeRaca.keys()
	print(Racas[race.Race_Name])
	
	
enum sexo{
	M,F
}
