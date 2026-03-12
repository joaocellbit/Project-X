using Godot;
using Godot.Collections;

public partial class Server : Node
{
	private readonly PackedScene _planetScene = ResourceLoader.Load<PackedScene>("res://Cenas/planet.tscn");
	private readonly PackedScene _playerScene = ResourceLoader.Load<PackedScene>("res://Cenas/Saiyajin.tscn");

	public Dictionary<long, string> playerid = new();
	public Dictionary<long, Dictionary> player_state = new();
	public Dictionary<int, string> planetid = new();
	public Dictionary<long, int> player_planet = new();
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
		Multiplayer.PeerDisconnected += Disconnected;
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
		playerid[1L] = "server";
		GD.Print(playerid);
		Multiplayer.PeerConnected += New_connection;
		
		update_client(BuildPlayerIdDictionary());
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
		EnsurePlanetLoaded();
		EmitSignal(SignalName.Conectado);
		Multiplayer.ConnectedToServer -= client_info;
		GD.Print("enviando dados do cliente de id: ", Multiplayer.GetUniqueId());
		Dictionary state_test = new Dictionary
		{
			{ "nome", "Test" },
			{ "Raca", "test" },
		};
		RpcId(1L, nameof(send_client_info), Multiplayer.GetUniqueId(), state_test);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void send_client_info(long id, Dictionary data)
	{
		player_state[id] = data;
		GD.Print(player_state, " ", Multiplayer.GetUniqueId());
	}

	public void New_connection(long id)
	{
		long peerId = id;
		GD.Print(peerId);
		playerid[peerId] = "test";
		GD.Print(playerid, Multiplayer.GetUniqueId());
		Rpc(nameof(update_client), BuildPlayerIdDictionary());
	}

	public void spawn_character(long id)
	{
		Node planetNode = EnsurePlanetLoaded();
		if (planetNode.GetNodeOrNull(id.ToString()) != null)
		{
			return;
		}

		Node perso = _playerScene.Instantiate();
		perso.Name = id.ToString();
		perso.SetMultiplayerAuthority(checked((int)id));
		planetNode.AddChild(perso);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void update_client(Dictionary ids)
	{
		EnsurePlanetLoaded();
		playerid = new Dictionary<long, string>();
		foreach (Variant key in ids.Keys)
		{
			long peerId = key.AsInt64();
			playerid[peerId] = ids[key].AsString();
			spawn_character(peerId);
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

	private Node EnsurePlanetLoaded()
	{
		Node planetNode = _scene.GetNodeOrNull("Planet");
		if (planetNode != null)
		{
			return planetNode;
		}

		planetNode = _planetScene.Instantiate();
		_scene.AddChild(planetNode);
		return planetNode;
	}
	public void Disconnected(long id){
		
		GD.Print("o id: ", id, " saiu!");
		foreach(long i in playerid.Keys)
		{
			if(i == id)
			{
				playerid.Remove(id);
				GD.Print(playerid);
			}
		}
		Node planetplayerin = _scene.GetNodeOrNull("Planet");
		if(planetplayerin == null){
			return;
		}
		Node playertoremove = planetplayerin.GetNodeOrNull(id.ToString());
		if(playertoremove == null){
			return;
		}
		playertoremove.QueueFree();
	
	}
	private Dictionary BuildPlayerIdDictionary()
	{
		Dictionary ids = new();
		foreach (long playerId in playerid.Keys)
		{
			ids[playerId] = playerid[playerId];
		}

		return ids;
	}
}
