using Godot;
using Godot.Collections;

[GlobalClass]
public partial class Player : CharacterBody2D
{
	private const float MoveSpeed = 100.0f;
	private const string PunchStateName = "Punch";

	[Export]
	public int id { get; set; }

	[Export]
	public string Nome { get; set; } = string.Empty;

	[Export]
	public int Max_Health { get; set; }

	[Export]
	public Raca race { get; set; }

	[Export]
	public float Power_level { get; set; }

	[Export]
	public sexo Sexo { get; set; }

	[Export]
	public int Age { get; set; }

	[Export]
	public int Fisico { get; set; }

	[Export]
	public int Agilidade { get; set; }

	[Export]
	public int Ki { get; set; }

	[Export]
	public int KiControl { get; set; }

	[Export]
	public int Reflexo { get; set; }

	[Export]
	public int magic { get; set; }

	[Export]
	public float LatentPower { get; set; }

	public AnimationTree animationtree;
	public Vector2 CoordAnima;
	public bool is_flying;

	public enum sexo
	{
		M,
		F,
	}

	public override void _Ready()
	{
		Set_animation();
	}

	public void Set_animation()
	{
		foreach (Node child in GetChildren())
		{
			if (child is AnimationTree)
			{
				animationtree = (AnimationTree)child;
				break;
			}
		}
	}

	public void Set_id()
	{
		id = Multiplayer.GetUniqueId();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!IsMultiplayerAuthority())
		{
			return;
		}

		float diry = Input.GetAxis("ui_up", "ui_down");
		float dirx = Input.GetAxis("ui_left", "ui_right");
		Vector2 dir = new Vector2(dirx, diry);
		Velocity = dir.Normalized() * MoveSpeed;
		MoveAndSlide();

		AnimationNodeStateMachinePlayback playback = GetPlayback();
		if (dir != Vector2.Zero)
		{
			CoordAnima = new Vector2(Mathf.Round(dir.X), Mathf.Round(dir.Y));
		}

		update_animation(CoordAnima, Velocity);
		if (IsMultiplayerConnected())
		{
			Rpc(nameof(atualizar_posicao), Velocity, Position, CoordAnima);
		}

		if (Input.IsActionJustPressed("Punch") && playback != null)
		{
			if (IsMultiplayerConnected())
			{
				Rpc(nameof(sincronizar_punch), CoordAnima);
			}
			else
			{
				tocar_punch(CoordAnima);
			}
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void atualizar_posicao(Vector2 vel, Vector2 posicao, Vector2 coordanima_server)
	{
		Position = posicao;
		update_animation(coordanima_server, vel);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void sincronizar_punch(Vector2 coordanima)
	{
		tocar_punch(coordanima);
	}

	public void update_animation(Vector2 coordanima, Vector2 vel)
	{
		AnimationNodeStateMachinePlayback playback = GetPlayback();
		if (playback == null || animationtree == null)
		{
			return;
		}


		if (vel != Vector2.Zero)
		{
			playback.Travel("Walk");
			animationtree.Set("parameters/StateMachine/Idle/blend_position", coordanima);
			animationtree.Set("parameters/StateMachine/Punch/blend_position", coordanima);
			animationtree.Set("parameters/StateMachine/Walk/blend_position", coordanima);
			return;
		}

		playback.Travel("Idle");
	}

	private AnimationNodeStateMachinePlayback GetPlayback()
	{
		if (animationtree == null)
		{
			return null;
		}

		return (AnimationNodeStateMachinePlayback)animationtree.Get("parameters/StateMachine/playback");
	}

	private void tocar_punch(Vector2 coordanima)
	{
		AnimationNodeStateMachinePlayback playback = GetPlayback();
		if (playback == null || animationtree == null)
		{
			return;
		}

		animationtree.Set("parameters/StateMachine/Punch/blend_position", coordanima);
		animationtree.Set("parameters/StateMachine/Idle/blend_position", coordanima);
		animationtree.Set("parameters/StateMachine/Walk/blend_position", coordanima);
		playback.Travel("Punch");
	}

	private bool IsMultiplayerConnected()
	{
		MultiplayerPeer peer = Multiplayer.MultiplayerPeer;
		if (peer == null)
		{
			return false;
		}

		return peer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected;
	}
}
