
using Sandbox;
using Sandbox.UI;
using System;
using System.Linq;

//
// You don't need to put things in a namespace, but it doesn't hurt.
//
namespace MyGame;

/// <summary>
/// This is your game class. This is an entity that is created serverside when
/// the game starts, and is replicated to the client. 
/// 
/// You can use this to create things like HUDs and declare which player class
/// to use for spawned players.
/// </summary>
public partial class MyGame : Sandbox.GameManager
{
	public SWB_HUD.HUD UI;
	private bool restarting;
	private TimeSince roundFinish;
	public Hud MyGameHud;

	/// <summary>
	/// Called when the game is created (on both the server and client)
	/// </summary>
	public MyGame()
	{
		if ( Game.IsClient )
		{
			MyGameHud = new Hud();
			Game.RootPanel = MyGameHud;
		}

		if ( Game.IsServer)
		{
			UI = new SWB_HUD.HUD();
		}
	}

	/// <summary>
	/// A client has joined the server. Make them a pawn to play with
	/// </summary>
	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );

		// Create a pawn for this client to play with
		//var pawn = new Pawn();
		//pawn.DressFromClient( client );
		var pawn = new Player(client);
		pawn.Respawn();
		client.Pawn = pawn;

		// Get all of the spawnpoints
		var spawnpoints = Entity.All.OfType<SpawnPoint>();

		// chose a random one
		var randomSpawnPoint = spawnpoints.OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		// if it exists, place the pawn there
		if ( randomSpawnPoint != null )
		{
			var tx = randomSpawnPoint.Transform;
			tx.Position = tx.Position + Vector3.Up * 50.0f; // raise it up
			pawn.Transform = tx;
		}

	}

	[GameEvent.Tick.Server]
	public void CheckAlive() 
	{
		var players = Sandbox.Entity.All.OfType<SWB_Player.PlayerBase>().ToList();
		var total = players.Count();
		var alive = players.Count(p => p.Alive());
		// Handle solo player case
		if (total == 1) {
			alive += 1;
		}
		if (alive <= 1  && !restarting) {
		 	restarting = true;
		 	roundFinish = 0;
			ShowRestarting(true);
		}
		if (restarting && roundFinish > 5) {
			foreach (var player in players) {
				RespawnPlayer(player);
			}
			ShowRestarting(false);
			restarting = false;
		}
	}

	[Event( "mygame.restart_round" )]
	public void DoRestartRound()
	{
		restarting = true;
		roundFinish = 0;
		ShowRestarting(true);
	}

	[ConCmd.Server( "restart_round" )]
	public static void RestartRound()
	{
		Event.Run("mygame.restart_round");
	}

	public void RespawnPlayer(SWB_Player.PlayerBase player) {
		player.Respawn();

		// Get all of the spawnpoints
		var spawnpoints = Entity.All.OfType<SpawnPoint>();

		// chose a random one
		var randomSpawnPoint = spawnpoints.OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		// if it exists, place the pawn there
		if ( randomSpawnPoint != null )
		{
			var tx = randomSpawnPoint.Transform;
			tx.Position = tx.Position + Vector3.Up * 50.0f; // raise it up
			player.Transform = tx;
		}
	}

    [ClientRpc]
	public void ShowRestarting(bool shouldShow) 
	{
		// TODO: FIX THIS!!! bruh
		if (shouldShow) {
			// Label label = new Label();
			// label.Text = "Restarting in 5 seconds...";
			// // can't do variables with partial classes :/ 
			// // MyGameHud.showRestart = shouldShow;
			// MyGameHud.AddChild(label);
		} else {
			// MyGameHud.ChildrenOfType<Label>().First<Label>().Delete();
		}
	}
}

