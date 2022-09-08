using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Linq;
using UnityEngine;

public sealed class GameManager : NetworkBehaviour
{
	public static GameManager Instance { get; private set; }

	[SyncObject]
	public readonly SyncList<Player> players = new();

	[SyncVar]
	public bool canStart;

	[SyncVar] 
	public float roundTotalTime;

	[SyncVar] 
	public bool isLegalToDraw;

	[SyncVar] 
	public bool isRoundStarted;

	private void Awake()
	{
		Instance = this;
	}

	private void Update()
	{
		//if (!IsHost) return;

		canStart = players.All(player => player.isReady);

		if (isRoundStarted)
		{
			if (roundTotalTime >= 5)
			{
				isLegalToDraw = true;
			}
			roundTotalTime += Time.deltaTime;
		}
		
		Debug.Log(players.Count);
	}

	[ServerRpc(RequireOwnership = false)]
	public void StartGame()
	{
		//if (!canStart) return;
		Debug.Log("game Started");
		isRoundStarted = true;
		
		for (int i = 0; i < players.Count; i++)
		{
			players[i].StartGame();
		}

	}

	[Server]
	public void StopGame()
	{
		for (int i = 0; i < players.Count; i++)
		{
			players[i].StopGame();
		}
	}
}
