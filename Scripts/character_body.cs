using Godot;

public partial class character_body : AnimatedSprite2D
{
	public override void _Ready()
	{
	}

	public override void _Process(double delta)
	{
	}

	public void _on_animation_changed()
	{
		AnimatedSprite2D eye = GetNodeOrNull<AnimatedSprite2D>("Eye");
		AnimatedSprite2D tail = GetNodeOrNull<AnimatedSprite2D>("Tail");
		if (tail == null)
		{
			return;
		}

		bool showEye = Animation != "FlyNorth"
			&& Animation != "IdleNorth"
			&& Animation != "WalkNorth"
			&& Animation != "Downed"
			&& Animation != "Meditating";

		if (eye != null)
		{
			eye.Visible = showEye;
			if (showEye)
			{
				eye.Play(Animation);
			}
		}

		tail.Play(Animation);
	}
}