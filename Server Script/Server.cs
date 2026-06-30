using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Godot;
using LiteNetLib;
using LiteNetLib.Utils;

public partial class Server : Node
{
	private const string ConnectKey = "project-x";
	private const int HostPlayerId = 1;
	private const int DefaultPlanetId = 1;
	private const string DefaultPlayerName = "Test";
	private const string DefaultPlayerRace = "Saiyan";
	private const string DefaultPlanetName = "Planet";

	private readonly PackedScene _planetScene = ResourceLoader.Load<PackedScene>("res://Cenas/planet.tscn");
	private readonly PackedScene _playerScene = ResourceLoader.Load<PackedScene>("res://Cenas/Saiyajin.tscn");
	private readonly EventBasedNetListener _serverListener = new();
	private readonly EventBasedNetListener _clientListener = new();
	private readonly Dictionary<int, PlayerRecord> _players = new();
	private readonly Dictionary<int, NetPeer> _peersByPlayerId = new();
	private readonly Dictionary<int, int> _playerIdByPeerId = new();

	private NetManager _server;
	private NetManager _client;
	private NetPeer _serverPeer;
	private bool _listenersConfigured;
	private bool _clientHandshakeComplete;
	private int _nextPlayerId = HostPlayerId + 1;

	public readonly Dictionary<int, PlayerStateData> player_state = new();
	public readonly Dictionary<int, string> planetid = new();
	public readonly Dictionary<int, int> player_planet = new();
	public readonly Dictionary<int, List<int>> Planet_player = new();

	[Signal]
	public delegate void ConectadoEventHandler();

	public int LocalPlayerId { get; private set; }

	public override void _Ready()
	{
		ConfigureListeners();
	}

	public override void _Process(double delta)
	{
		_server?.PollEvents();
		_client?.PollEvents();
	}

	public override void _ExitTree()
	{
		ShutdownNetworking();
	}

	public bool criar_server(int porta)
	{
		if (HasActiveSession())
		{
			GD.PrintErr("Ja existe uma sessao de rede ativa.");
			return false;
		}

		ConfigureListeners();
		_server = new NetManager(_serverListener)
		{
			AutoRecycle = true,
		};

		if (!_server.Start(porta))
		{
			GD.PrintErr($"Falha ao iniciar servidor LiteNetLib na porta {porta}.");
			_server = null;
			return false;
		}

		LocalPlayerId = HostPlayerId;
		_nextPlayerId = HostPlayerId + 1;

		PlayerRecord hostRecord = CreatePlayerRecord(LocalPlayerId, "server", DefaultPlayerRace, DefaultPlanetId, DefaultPlanetName);
		TrackPlayerRecord(hostRecord);
		SpawnOrUpdatePlayer(hostRecord);

		GD.Print($"Servidor LiteNetLib iniciado na porta {porta}.");
		return true;
	}

	public void criar_cliente(string ip, int porta)
	{
		if (HasActiveSession())
		{
			GD.PrintErr("Ja existe uma sessao de rede ativa.");
			return;
		}

		ConfigureListeners();
		_client = new NetManager(_clientListener)
		{
			AutoRecycle = true,
		};

		if (!_client.Start())
		{
			GD.PrintErr("Falha ao iniciar o cliente LiteNetLib.");
			_client = null;
			return;
		}

		_clientHandshakeComplete = false;
		_serverPeer = _client.Connect(ip, porta, ConnectKey);
		if (_serverPeer == null)
		{
			GD.PrintErr($"Falha ao conectar em {ip}:{porta}.");
			ShutdownClient();
		}
	}

	public bool IsLocalPlayer(int playerId)
	{
		return playerId != 0 && playerId == LocalPlayerId;
	}

	public void PublishLocalTransform(int playerId, Vector2 velocity, Vector2 position, Vector2 animationDirection)
	{
		if (!IsLocalPlayer(playerId))
		{
			return;
		}

		if (!_players.TryGetValue(playerId, out PlayerRecord record))
		{
			return;
		}

		record.Position = position;
		record.Velocity = velocity;
		record.AnimationDirection = animationDirection;

		if (_server != null)
		{
			BroadcastPlayerTransform(record, null);
			return;
		}

		if (_serverPeer == null || !_clientHandshakeComplete)
		{
			return;
		}

		SendPlayerTransform(_serverPeer, record, DeliveryMethod.Sequenced);
	}

	public void PublishLocalPunch(int playerId, Vector2 animationDirection)
	{
		if (!IsLocalPlayer(playerId))
		{
			return;
		}

		if (_server != null)
		{
			BroadcastPunch(playerId, animationDirection, null);
			return;
		}

		if (_serverPeer == null || !_clientHandshakeComplete)
		{
			return;
		}

		PacketProcessor processor = PacketProcessor.ForWrite(MessageType.PlayerPunch);
		ProcessPunch(processor, ref playerId, ref animationDirection);
		processor.SendTo(_serverPeer, DeliveryMethod.ReliableOrdered);
	}

	public void End_connection()
	{
		GD.Print("Conexao com servidor perdida.");
		ShutdownClient();
		ResetRuntimeState(true);
		OpenMenuIfNeeded();
	}

	private void ConfigureListeners()
	{
		if (_listenersConfigured)
		{
			return;
		}

		_serverListener.ConnectionRequestEvent += request => request.AcceptIfKey(ConnectKey);
		_serverListener.PeerConnectedEvent += peer => GD.Print($"Cliente conectado: peer {peer.Id}");
		_serverListener.PeerDisconnectedEvent += OnServerPeerDisconnected;
		_serverListener.NetworkReceiveEvent += OnServerNetworkReceive;
		_serverListener.NetworkErrorEvent += OnNetworkError;

		_clientListener.PeerConnectedEvent += OnClientConnected;
		_clientListener.PeerDisconnectedEvent += OnClientDisconnected;
		_clientListener.NetworkReceiveEvent += OnClientNetworkReceive;
		_clientListener.NetworkErrorEvent += OnNetworkError;

		_listenersConfigured = true;
	}

	private void OnClientConnected(NetPeer peer)
	{
		_serverPeer = peer;
		GD.Print($"Conectado ao servidor. Peer local: {peer.Id}.");

		string playerName = DefaultPlayerName, playerRace = DefaultPlayerRace, planetName = DefaultPlanetName;
		int planetId = DefaultPlanetId;
		PacketProcessor processor = PacketProcessor.ForWrite(MessageType.ClientHello);
		ProcessHello(processor, ref playerName, ref playerRace, ref planetId, ref planetName);
		processor.SendTo(peer, DeliveryMethod.ReliableOrdered);
	}

	private void OnClientDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
	{
		GD.Print($"Servidor desconectado: {disconnectInfo.Reason}");
		End_connection();
	}

	private void OnServerPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
	{
		if (!_playerIdByPeerId.TryGetValue(peer.Id, out int playerId))
		{
			return;
		}

		GD.Print($"Cliente {playerId} saiu: {disconnectInfo.Reason}");
		RemovePlayer(playerId, true);
	}

	private void OnServerNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
	{
		PacketProcessor processor = PacketProcessor.ForRead(reader);

		switch (processor.ReadMessageType())
		{
			case MessageType.ClientHello:
				HandleClientHello(peer, processor);
				break;
			case MessageType.PlayerTransform:
				HandleClientTransform(peer, processor);
				break;
			case MessageType.PlayerPunch:
				HandleClientPunch(peer, processor);
				break;
		}

		reader.Recycle();
	}

	private void OnClientNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
	{
		PacketProcessor processor = PacketProcessor.ForRead(reader);

		switch (processor.ReadMessageType())
		{
			case MessageType.Welcome:
				HandleWelcome(processor);
				break;
			case MessageType.PlayerJoined:
				HandlePlayerJoined(processor);
				break;
			case MessageType.PlayerLeft:
				HandlePlayerLeft(processor);
				break;
			case MessageType.PlayerTransform:
				HandlePlayerTransform(processor);
				break;
			case MessageType.PlayerPunch:
				HandlePlayerPunch(processor);
				break;
		}

		reader.Recycle();
	}

	private void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
	{
		GD.PrintErr($"Erro de rede em {endPoint}: {socketError}");
	}

	private void HandleClientHello(NetPeer peer, PacketProcessor processor)
	{
		if (_playerIdByPeerId.ContainsKey(peer.Id))
		{
			return;
		}

		string playerName = null, playerRace = null, planetName = null;
		int planetId = 0;
		ProcessHello(processor, ref playerName, ref playerRace, ref planetId, ref planetName);
		int playerId = _nextPlayerId++;

		PlayerRecord record = CreatePlayerRecord(
			playerId,
			string.IsNullOrWhiteSpace(playerName) ? DefaultPlayerName : playerName,
			string.IsNullOrWhiteSpace(playerRace) ? DefaultPlayerRace : playerRace,
			planetId == 0 ? DefaultPlanetId : planetId,
			string.IsNullOrWhiteSpace(planetName) ? DefaultPlanetName : planetName
		);

		TrackPlayerRecord(record);
		_peersByPlayerId[playerId] = peer;
		_playerIdByPeerId[peer.Id] = playerId;

		SpawnOrUpdatePlayer(record);
		SendWelcome(peer, playerId);
		BroadcastPlayerJoined(record, playerId);
	}

	private void HandleClientTransform(NetPeer peer, PacketProcessor processor)
	{
		int incomingPlayerId = 0;
		Vector2 position = Vector2.Zero, velocity = Vector2.Zero, animationDirection = Vector2.Zero;
		ProcessTransform(processor, ref incomingPlayerId, ref position, ref velocity, ref animationDirection);

		if (!_playerIdByPeerId.TryGetValue(peer.Id, out int playerId) || !_players.TryGetValue(playerId, out PlayerRecord record))
		{
			return;
		}

		record.Position = position;
		record.Velocity = velocity;
		record.AnimationDirection = animationDirection;
		ApplyPlayerTransform(record);
		BroadcastPlayerTransform(record, playerId);
	}

	private void HandleClientPunch(NetPeer peer, PacketProcessor processor)
	{
		int incomingPlayerId = 0;
		Vector2 animationDirection = Vector2.Zero;
		ProcessPunch(processor, ref incomingPlayerId, ref animationDirection);

		if (!_playerIdByPeerId.TryGetValue(peer.Id, out int playerId))
		{
			return;
		}

		PlayPlayerPunch(playerId, animationDirection);
		BroadcastPunch(playerId, animationDirection, playerId);
	}

	private void HandleWelcome(PacketProcessor processor)
	{
		int localPlayerId = 0, playerCount = 0;
		processor.Process(ref localPlayerId);
		processor.Process(ref playerCount);
		LocalPlayerId = localPlayerId;

		_players.Clear();
		player_state.Clear();
		player_planet.Clear();
		Planet_player.Clear();
		planetid.Clear();
		ClearSpawnedPlayers(false);

		for (int i = 0; i < playerCount; i++)
		{
			PlayerRecord record = null;
			ProcessPlayerRecord(processor, ref record);
			TrackPlayerRecord(record);
			SpawnOrUpdatePlayer(record);
		}

		bool shouldEmitSignal = !_clientHandshakeComplete;
		_clientHandshakeComplete = true;

		if (shouldEmitSignal)
		{
			EmitSignal(SignalName.Conectado);
		}
	}

	private void HandlePlayerJoined(PacketProcessor processor)
	{
		PlayerRecord record = null;
		ProcessPlayerRecord(processor, ref record);
		TrackPlayerRecord(record);
		SpawnOrUpdatePlayer(record);
	}

	private void HandlePlayerLeft(PacketProcessor processor)
	{
		int playerId = 0;
		processor.Process(ref playerId);
		RemovePlayerNode(playerId);
		UntrackPlayerRecord(playerId);
	}

	private void HandlePlayerTransform(PacketProcessor processor)
	{
		int playerId = 0;
		Vector2 position = Vector2.Zero, velocity = Vector2.Zero, animationDirection = Vector2.Zero;
		ProcessTransform(processor, ref playerId, ref position, ref velocity, ref animationDirection);

		if (!_players.TryGetValue(playerId, out PlayerRecord record))
		{
			return;
		}

		record.Position = position;
		record.Velocity = velocity;
		record.AnimationDirection = animationDirection;
		ApplyPlayerTransform(record);
	}

	private void HandlePlayerPunch(PacketProcessor processor)
	{
		int playerId = 0;
		Vector2 animationDirection = Vector2.Zero;
		ProcessPunch(processor, ref playerId, ref animationDirection);
		PlayPlayerPunch(playerId, animationDirection);
	}

	private void SendWelcome(NetPeer peer, int localPlayerId)
	{
		PacketProcessor processor = PacketProcessor.ForWrite(MessageType.Welcome);
		int playerCount = _players.Count;
		processor.Process(ref localPlayerId);
		processor.Process(ref playerCount);

		foreach (PlayerRecord player in _players.Values)
		{
			PlayerRecord record = player;
			ProcessPlayerRecord(processor, ref record);
		}

		processor.SendTo(peer, DeliveryMethod.ReliableOrdered);
	}

	private void BroadcastPlayerJoined(PlayerRecord record, int? excludedPlayerId)
	{
		foreach (KeyValuePair<int, NetPeer> entry in _peersByPlayerId)
		{
			if (excludedPlayerId.HasValue && entry.Key == excludedPlayerId.Value)
			{
				continue;
			}

			PacketProcessor processor = PacketProcessor.ForWrite(MessageType.PlayerJoined);
			ProcessPlayerRecord(processor, ref record);
			processor.SendTo(entry.Value, DeliveryMethod.ReliableOrdered);
		}
	}

	private void BroadcastPlayerTransform(PlayerRecord record, int? excludedPlayerId)
	{
		foreach (KeyValuePair<int, NetPeer> entry in _peersByPlayerId)
		{
			if (excludedPlayerId.HasValue && entry.Key == excludedPlayerId.Value)
			{
				continue;
			}

			SendPlayerTransform(entry.Value, record, DeliveryMethod.Sequenced);
		}
	}

	private void BroadcastPunch(int playerId, Vector2 animationDirection, int? excludedPlayerId)
	{
		foreach (KeyValuePair<int, NetPeer> entry in _peersByPlayerId)
		{
			if (excludedPlayerId.HasValue && entry.Key == excludedPlayerId.Value)
			{
				continue;
			}

			PacketProcessor processor = PacketProcessor.ForWrite(MessageType.PlayerPunch);
			ProcessPunch(processor, ref playerId, ref animationDirection);
			processor.SendTo(entry.Value, DeliveryMethod.ReliableOrdered);
		}
	}

	private void SendPlayerTransform(NetPeer peer, PlayerRecord record, DeliveryMethod deliveryMethod)
	{
		PacketProcessor processor = PacketProcessor.ForWrite(MessageType.PlayerTransform);
		int playerId = record.PlayerId;
		Vector2 position = record.Position, velocity = record.Velocity, animationDirection = record.AnimationDirection;
		ProcessTransform(processor, ref playerId, ref position, ref velocity, ref animationDirection);
		processor.SendTo(peer, deliveryMethod);
	}

	private void ApplyPlayerTransform(PlayerRecord record)
	{
		Player playerNode = GetPlayerNode(record.PlayerId);
		playerNode?.ApplyNetworkTransform(record.Velocity, record.Position, record.AnimationDirection);
	}

	private void PlayPlayerPunch(int playerId, Vector2 animationDirection)
	{
		Player playerNode = GetPlayerNode(playerId);
		playerNode?.PlayNetworkPunch(animationDirection);
	}

	private void SpawnOrUpdatePlayer(PlayerRecord record)
	{
		Node planetNode = EnsurePlanetLoaded(record.PlanetId, record.PlanetName);
		if (planetNode == null)
		{
			return;
		}

		Player playerNode = GetPlayerNode(record.PlayerId);
		if (playerNode != null && playerNode.GetParent() != planetNode)
		{
			Vector2 globalPosition = playerNode.GlobalPosition;
			playerNode.GetParent()?.RemoveChild(playerNode);
			planetNode.AddChild(playerNode);
			playerNode.GlobalPosition = globalPosition;
		}

		if (playerNode == null)
		{
			playerNode = _playerScene.Instantiate<Player>();
			playerNode.Name = record.PlayerId.ToString();
			planetNode.AddChild(playerNode);
		}

		playerNode.Nome = record.State.Name;
		playerNode.ConfigureNetworkIdentity(record.PlayerId, record.PlayerId == LocalPlayerId);
		playerNode.ApplyNetworkTransform(record.Velocity, record.Position, record.AnimationDirection);
	}

	private void RemovePlayer(int playerId, bool broadcastToClients)
	{
		if (_peersByPlayerId.TryGetValue(playerId, out NetPeer peer))
		{
			_playerIdByPeerId.Remove(peer.Id);
			_peersByPlayerId.Remove(playerId);
		}

		RemovePlayerNode(playerId);
		UntrackPlayerRecord(playerId);

		if (!broadcastToClients)
		{
			return;
		}

		foreach (KeyValuePair<int, NetPeer> entry in _peersByPlayerId)
		{
			PacketProcessor processor = PacketProcessor.ForWrite(MessageType.PlayerLeft);
			processor.Process(ref playerId);
			processor.SendTo(entry.Value, DeliveryMethod.ReliableOrdered);
		}
	}

	private void TrackPlayerRecord(PlayerRecord record)
	{
		_players[record.PlayerId] = record;
		player_state[record.PlayerId] = record.State;
		EnsurePlanetRegistered(record.PlanetId, record.PlanetName);
		SetPlayerPlanet(record.PlayerId, record.PlanetId);
	}

	private void UntrackPlayerRecord(int playerId)
	{
		_players.Remove(playerId);
		player_state.Remove(playerId);
		RemovePlayerFromPlanet(playerId);
	}

	private void EnsurePlanetRegistered(int planetIdValue, string planetName)
	{
		int resolvedPlanetId = planetIdValue == 0 ? DefaultPlanetId : planetIdValue;
		string resolvedPlanetName = string.IsNullOrWhiteSpace(planetName) ? DefaultPlanetName : planetName;

		planetid[resolvedPlanetId] = resolvedPlanetName;
		if (!Planet_player.ContainsKey(resolvedPlanetId))
		{
			Planet_player[resolvedPlanetId] = new List<int>();
		}
	}

	private void SetPlayerPlanet(int playerId, int planetIdValue)
	{
		int resolvedPlanetId = planetIdValue == 0 ? DefaultPlanetId : planetIdValue;
		RemovePlayerFromPlanet(playerId);
		EnsurePlanetRegistered(resolvedPlanetId, planetid.GetValueOrDefault(resolvedPlanetId, DefaultPlanetName));

		List<int> playersOnPlanet = Planet_player[resolvedPlanetId];
		if (!playersOnPlanet.Contains(playerId))
		{
			playersOnPlanet.Add(playerId);
		}

		player_planet[playerId] = resolvedPlanetId;
	}

	private void RemovePlayerFromPlanet(int playerId)
	{
		if (!player_planet.TryGetValue(playerId, out int currentPlanetId))
		{
			return;
		}

		if (Planet_player.TryGetValue(currentPlanetId, out List<int> playersOnPlanet))
		{
			playersOnPlanet.Remove(playerId);
		}

		player_planet.Remove(playerId);
	}

	private void RemovePlayerNode(int playerId)
	{
		Player playerNode = GetPlayerNode(playerId);
		if (playerNode == null)
		{
			return;
		}

		playerNode.GetParent()?.RemoveChild(playerNode);
		playerNode.QueueFree();
	}

	private Player GetPlayerNode(int playerId)
	{
		if (player_planet.TryGetValue(playerId, out int planetIdValue))
		{
			Player playerNode = GetPlanetNode(planetIdValue)?.GetNodeOrNull<Player>(playerId.ToString());
			if (playerNode != null)
			{
				return playerNode;
			}
		}

		foreach (Node planetNode in GetAllPlanetNodes())
		{
			Player playerNode = planetNode.GetNodeOrNull<Player>(playerId.ToString());
			if (playerNode != null)
			{
				return playerNode;
			}
		}

		return null;
	}

	private void ShutdownNetworking()
	{
		ShutdownClient();
		ShutdownServer();
	}

	private void ShutdownClient()
	{
		_serverPeer = null;

		if (_client == null)
		{
			return;
		}

		_client.Stop();
		_client = null;
		_clientHandshakeComplete = false;
	}

	private void ShutdownServer()
	{
		if (_server == null)
		{
			return;
		}

		_server.Stop();
		_server = null;
	}

	private void ResetRuntimeState(bool clearWorld)
	{
		_players.Clear();
		_peersByPlayerId.Clear();
		_playerIdByPeerId.Clear();
		player_state.Clear();
		planetid.Clear();
		player_planet.Clear();
		Planet_player.Clear();
		LocalPlayerId = 0;
		_nextPlayerId = HostPlayerId + 1;
		_clientHandshakeComplete = false;

		if (clearWorld)
		{
			ClearSpawnedPlayers(true);
		}
	}

	private bool HasActiveSession()
	{
		return _server != null || _client != null || LocalPlayerId != 0;
	}

	private Node EnsurePlanetLoaded(int planetIdValue, string planetName)
	{
		int resolvedPlanetId = planetIdValue == 0 ? DefaultPlanetId : planetIdValue;
		string resolvedPlanetName = string.IsNullOrWhiteSpace(planetName) ? DefaultPlanetName : planetName;

		EnsurePlanetRegistered(resolvedPlanetId, resolvedPlanetName);

		Node planetNode = GetPlanetNode(resolvedPlanetId);
		if (planetNode != null)
		{
			ConfigurePlanetNode(planetNode, resolvedPlanetId, resolvedPlanetName);
			return planetNode;
		}

		Node mainWorld = GetMainWorldNode();
		if (mainWorld == null)
		{
			GD.PushError("MainWorld nao foi encontrado.");
			return null;
		}

		planetNode = _planetScene.Instantiate();
		ConfigurePlanetNode(planetNode, resolvedPlanetId, resolvedPlanetName);
		mainWorld.AddChild(planetNode);
		return planetNode;
	}

	private void ConfigurePlanetNode(Node planetNode, int planetIdValue, string planetName)
	{
		planetNode.Name = GetPlanetNodeName(planetIdValue);
		if (planetNode is planet planetScript)
		{
			planetScript.planetid = planetIdValue;
			planetScript.planet_name = planetName;
		}
	}

	private Node GetPlanetNode(int planetIdValue)
	{
		Node mainWorld = GetMainWorldNode();
		if (mainWorld == null)
		{
			return null;
		}

		string nodeName = GetPlanetNodeName(planetIdValue);
		Node planetNode = mainWorld.GetNodeOrNull<Node>(nodeName);
		if (planetNode != null)
		{
			return planetNode;
		}

		if (planetIdValue == DefaultPlanetId)
		{
			return mainWorld.GetNodeOrNull<Node>("Planet");
		}

		return null;
	}

	private List<Node> GetAllPlanetNodes()
	{
		List<Node> planets = new();
		Node mainWorld = GetMainWorldNode();
		if (mainWorld == null)
		{
			return planets;
		}

		foreach (Node child in mainWorld.GetChildren())
		{
			string childName = child.Name.ToString();
			if (child is planet || childName == "Planet" || childName.StartsWith("Planet_"))
			{
				planets.Add(child);
			}
		}

		return planets;
	}

	private Node GetMainWorldNode()
	{
		return GetNodeOrNull<Node>("/root/MainWorld");
	}

	private void ClearSpawnedPlayers(bool removePlanet)
	{
		foreach (Node planetNode in GetAllPlanetNodes())
		{
			if (removePlanet)
			{
				planetNode.GetParent()?.RemoveChild(planetNode);
				planetNode.QueueFree();
				continue;
			}

			foreach (Node child in planetNode.GetChildren())
			{
				planetNode.RemoveChild(child);
				child.QueueFree();
			}
		}
	}

	private void OpenMenuIfNeeded()
	{
		Node mainWorld = GetMainWorldNode();
		if (mainWorld == null || mainWorld.GetNodeOrNull("Menu") != null)
		{
			return;
		}

		PackedScene menuScene = ResourceLoader.Load<PackedScene>("res://Cenas/menu.tscn");
		mainWorld.AddChild(menuScene.Instantiate());
	}

	private static PlayerRecord CreatePlayerRecord(int playerId, string playerName, string playerRace, int planetIdValue, string planetName)
	{
		return new PlayerRecord(playerId, new PlayerStateData(playerName, playerRace), planetIdValue, planetName);
	}

	// --- Serializacao: cada layout de mensagem descrito uma unica vez via Process(ref) ---

	private static void ProcessHello(PacketProcessor processor, ref string playerName, ref string playerRace, ref int planetId, ref string planetName)
	{
		processor.Process(ref playerName);
		processor.Process(ref playerRace);
		processor.Process(ref planetId);
		processor.Process(ref planetName);
	}

	private static void ProcessTransform(PacketProcessor processor, ref int playerId, ref Vector2 position, ref Vector2 velocity, ref Vector2 animationDirection)
	{
		processor.Process(ref playerId);
		processor.Process(ref position);
		processor.Process(ref velocity);
		processor.Process(ref animationDirection);
	}

	private static void ProcessPunch(PacketProcessor processor, ref int playerId, ref Vector2 animationDirection)
	{
		processor.Process(ref playerId);
		processor.Process(ref animationDirection);
	}

	private static void ProcessPlayerRecord(PacketProcessor processor, ref PlayerRecord record)
	{
		int playerId = record?.PlayerId ?? 0, planetId = record?.PlanetId ?? 0;
		string playerName = record?.State.Name, playerRace = record?.State.Race, planetName = record?.PlanetName;
		Vector2 position = record?.Position ?? Vector2.Zero, velocity = record?.Velocity ?? Vector2.Zero, animationDirection = record?.AnimationDirection ?? Vector2.Zero;

		processor.Process(ref playerId);
		processor.Process(ref playerName);
		processor.Process(ref playerRace);
		processor.Process(ref planetId);
		processor.Process(ref planetName);
		processor.Process(ref position);
		processor.Process(ref velocity);
		processor.Process(ref animationDirection);

		if (processor.IsWriting)
		{
			return;
		}

		record = CreatePlayerRecord(playerId, playerName, playerRace, planetId, planetName);
		record.Position = position;
		record.Velocity = velocity;
		record.AnimationDirection = animationDirection;
	}

	private static string GetPlanetNodeName(int planetIdValue)
	{
		return planetIdValue == DefaultPlanetId ? "Planet" : $"Planet_{planetIdValue}";
	}

	// Serializador bidirecional: o mesmo Process() escreve ou le um campo conforme
	// o processor foi criado (ForWrite x ForRead), garantindo que leitura e escrita
	// nunca saiam de sincronia.
	private sealed class PacketProcessor
	{
		private readonly NetDataWriter _writer;
		private readonly NetPacketReader _reader;

		private PacketProcessor(NetDataWriter writer) => _writer = writer;
		private PacketProcessor(NetPacketReader reader) => _reader = reader;

		public bool IsWriting => _writer != null;

		public static PacketProcessor ForWrite(MessageType messageType)
		{
			NetDataWriter writer = new();
			writer.Put((byte)messageType);
			return new PacketProcessor(writer);
		}

		public static PacketProcessor ForRead(NetPacketReader reader) => new(reader);

		public MessageType ReadMessageType() => (MessageType)_reader.GetByte();

		public void SendTo(NetPeer peer, DeliveryMethod deliveryMethod) => peer.Send(_writer, deliveryMethod);

		public void Process(ref int value) { if (IsWriting) _writer.Put(value); else value = _reader.GetInt(); }

		public void Process(ref float value) { if (IsWriting) _writer.Put(value); else value = _reader.GetFloat(); }

		public void Process(ref string value) { if (IsWriting) _writer.Put(value ?? string.Empty); else value = _reader.GetString(); }

		public void Process(ref Vector2 value)
		{
			float x = value.X, y = value.Y;
			Process(ref x);
			Process(ref y);
			value = new Vector2(x, y);
		}
	}

	private enum MessageType : byte
	{
		ClientHello = 1,
		Welcome = 2,
		PlayerJoined = 3,
		PlayerLeft = 4,
		PlayerTransform = 5,
		PlayerPunch = 6,
	}

	public sealed class PlayerStateData
	{
		public PlayerStateData(string name, string race)
		{
			Name = name;
			Race = race;
		}

		public string Name { get; set; }
		public string Race { get; set; }
	}

	private sealed class PlayerRecord
	{
		public PlayerRecord(int playerId, PlayerStateData state, int planetIdValue, string planetName)
		{
			PlayerId = playerId;
			State = state;
			PlanetId = planetIdValue;
			PlanetName = planetName;
		}

		public int PlayerId { get; }
		public PlayerStateData State { get; }
		public int PlanetId { get; set; }
		public string PlanetName { get; set; }
		public Vector2 Position { get; set; }
		public Vector2 Velocity { get; set; }
		public Vector2 AnimationDirection { get; set; }
	}
}
