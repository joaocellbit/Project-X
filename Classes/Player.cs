using Godot;

[GlobalClass]
public partial class Player : CharacterBody2D
{
	private const float MoveSpeed = 100.0f;

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

	private Server _server;
	private bool _isLocalPlayer;

	public enum sexo
	{
		M,
		F,
	}

	public override void _Ready()
	{
		_server = GetNodeOrNull<Server>("/root/Server");
		Set_animation();
		ApplyLocalControlState();
	}

	public void ConfigureNetworkIdentity(int playerId, bool isLocalPlayer)
	{
		id = playerId;
		_isLocalPlayer = isLocalPlayer;
		ApplyLocalControlState();
	}

	public void Set_animation()
	{
		animationtree = GetNodeOrNull<AnimationTree>("AnimationTree");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_isLocalPlayer)
		{
			return;
		}

		float diry = Input.GetAxis("ui_up", "ui_down");
		float dirx = Input.GetAxis("ui_left", "ui_right");
		Vector2 dir = new(dirx, diry);
		Velocity = dir.Normalized() * MoveSpeed;
		MoveAndSlide();

		if (dir != Vector2.Zero)
		{
			CoordAnima = new Vector2(Mathf.Round(dir.X), Mathf.Round(dir.Y));
		}

		update_animation(CoordAnima, Velocity);
		_server?.PublishLocalTransform(id, Velocity, Position, CoordAnima);

		if (!Input.IsActionJustPressed("Punch"))
		{
			return;
		}

		tocar_punch(CoordAnima);
		_server?.PublishLocalPunch(id, CoordAnima);
	}

	public void ApplyNetworkTransform(Vector2 velocity, Vector2 position, Vector2 animationDirection)
	{
		Position = position;
		update_animation(animationDirection, velocity);
	}

	public void PlayNetworkPunch(Vector2 animationDirection)
	{
		tocar_punch(animationDirection);
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

		animationtree.Set("parameters/StateMachine/Idle/blend_position", coordanima);
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

	private void ApplyLocalControlState()
	{
		Camera2D camera = GetNodeOrNull<Camera2D>("Camera2D");
		if (camera == null)
		{
			return;
		}

		camera.Enabled = _isLocalPlayer;
		if (_isLocalPlayer)
		{
			camera.MakeCurrent();
		}
	}
}
