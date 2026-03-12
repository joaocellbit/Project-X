using Godot;
using Godot.Collections;

public partial class Server : Node
{
	public Dictionary<int, string> playerid = new();
	public Dictionary<int, Dictionary> player_state = new();
	public Dictionary<int, string> planetid = new();
	public Dictionary<int, int> player_planet = new();
	public Dictionary<int, Array> Planet_player = new();
	public Dictionary<int, int> npc_planet = new();
	public Dictionary<int, Array> planet_npc = new();
	public ENetMultiplayerPeer ServerPeer = new();
	private Node _scene;

	[Signal]
	public delegate void ConectadoEventHandler();

	public override void _Ready()
	{
		_scene = GetTree().Root.GetNode("MainWorld");
	}

	public override void _Process(double delta)
	{
	}

	public bool criar_server(int Porta)
	{
		Error err = ServerPeer.CreateServer(Porta);
		if (err != Error.Ok)
		{
			GD.Print("erro ao criar server");
			return false;
		}

		Multiplayer.MultiplayerPeer = ServerPeer;
		GD.Print("ok");
		playerid[1] = "server";
		GD.Print(playerid);
		Multiplayer.PeerConnected += New_connection;
		return true;
	}

	public void criar_cliente(string ip, int Porta)
	{
		ServerPeer.CreateClient(ip, Porta);
		Multiplayer.MultiplayerPeer = ServerPeer;
		Multiplayer.ConnectedToServer += client_info;
		Multiplayer.ServerDisconnected += End_connection;
	}

	public void client_info()
	{
		EmitSignal(SignalName.Conectado);
		Multiplayer.ConnectedToServer -= client_info;
		GD.Print("enviando dados do cliente de id: ", Multiplayer.GetUniqueId());
		Node perso_adm = ResourceLoader.Load<PackedScene>("res://Cenas/Saiyajin.tscn").Instantiate();
		perso_adm.Name = "1";
		_scene.GetNode("Planet").AddChild(perso_adm);
		Dictionary state_test = new Dictionary
		{
			{ "nome", "Test" },
			{ "Raca", "test" },
		};
		RpcId(1, nameof(send_client_info), Multiplayer.GetUniqueId(), state_test);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void send_client_info(int id, Dictionary data)
	{
		player_state[id] = data;
		GD.Print(player_state, " ", Multiplayer.GetUniqueId());
	}

	public void New_connection(long id)
	{
		int peerId = (int)id;
		GD.Print(peerId);
		playerid[peerId] = "test";
		GD.Print(playerid, Multiplayer.GetUniqueId());
		Rpc(nameof(update_client), playerid);
		spawn_character(peerId);
	}

	public void spawn_character(int id)
	{
		Node perso = ResourceLoader.Load<PackedScene>("res://Cenas/Saiyajin.tscn").Instantiate();
		perso.Name = id.ToString();
		perso.SetMultiplayerAuthority(id);
		_scene.GetNode("Planet").AddChild(perso);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void update_client(Dictionary ids)
	{
		playerid = new Dictionary<int, string>();
		foreach (Variant key in ids.Keys)
		{
			playerid[(int)key.AsInt32()] = ids[key].AsString();
		}

		GD.Print(playerid, Multiplayer.GetUniqueId());
	}

	public void End_connection()
	{
		GD.Print("conexão com server perdida");
		Multiplayer.MultiplayerPeer = null;
		Multiplayer.ServerDisconnected -= End_connection;
		playerid.Clear();
		player_state.Clear();
		PackedScene menu = ResourceLoader.Load<PackedScene>("res://Cenas/menu.tscn");
		GetTree().Root.GetNode("MainWorld").AddChild(menu.Instantiate());
	}
}