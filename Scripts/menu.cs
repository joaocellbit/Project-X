using Godot;

public partial class menu : Node2D
{
	public override void _Ready()
	{
	}

	public override void _Process(double delta)
	{
	}

	public void _on_hostear_pressed()
	{
		Server server = GetNode<Server>("/root/Server");
		if (!server.criar_server(30000))
		{
			return;
		}

		_fechar_menu();
	}

	public void _on_join_pressed()
	{
		Server server = GetNode<Server>("/root/Server");
		server.Conectado += _fechar_menu;
		server.criar_cliente("127.0.0.1", 30000);
	}

	public void _fechar_menu()
	{
		Server server = GetNodeOrNull<Server>("/root/Server");
		if (server != null)
		{
			server.Conectado -= _fechar_menu;
		}

		QueueFree();
	}
}