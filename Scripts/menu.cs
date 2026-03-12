using Godot;

public partial class menu : Node2D
{
	private readonly PackedScene _planeta = ResourceLoader.Load<PackedScene>("res://Cenas/planet.tscn");
	private readonly PackedScene _jogador = ResourceLoader.Load<PackedScene>("res://Cenas/Saiyajin.tscn");

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
		GetParent().AddChild(_planeta.Instantiate());
		Node personagem = _jogador.Instantiate();
		personagem.Name = Multiplayer.GetUniqueId().ToString();
		GetParent().GetNode("Planet").AddChild(personagem);
	}

	public void _on_join_pressed()
	{
		Server server = GetNode<Server>("/root/Server");
		server.criar_cliente("127.0.0.1", 30000);
		server.Conectado += _fechar_menu;
		GetParent().AddChild(_planeta.Instantiate());
		Node personagem = _jogador.Instantiate();
		personagem.Name = Multiplayer.GetUniqueId().ToString();
		personagem.SetMultiplayerAuthority(Multiplayer.GetUniqueId());
		GetParent().GetNode("Planet").AddChild(personagem);
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