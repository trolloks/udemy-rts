using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOverHandler : NetworkBehaviour
{
    public static event Action ServerOnGameOver;
    public static event Action<string> ClientOnGameOver; 

    private List<UnitBase> unitBases = new List<UnitBase>();

    #region Server
    public override void OnStartServer()
    {
        UnitBase.ServerOnBaseSpawned += ServerHandleBaseSpawned;
        UnitBase.ServerOnBaseDespawned += ServerHandleBaseDespawned;
    }

    public override void OnStopServer()
    {
        UnitBase.ServerOnBaseSpawned -= ServerHandleBaseSpawned;
        UnitBase.ServerOnBaseDespawned -= ServerHandleBaseDespawned;
    }

    [Server]
    private void ServerHandleBaseSpawned(UnitBase unitBase)
    {
        unitBases.Add(unitBase);
    }

    [Server]
    private void ServerHandleBaseDespawned(UnitBase unitBase)
    {
        unitBases.Remove(unitBase);

        if (unitBases.Count > 1)
            return;

        // Winner
        int playerId = unitBases[0].connectionToClient.connectionId;

        RpcGameOver($"Player {playerId}");

        ServerOnGameOver?.Invoke();
    }
    #endregion

    #region Client
    [ClientRpc]
    private void RpcGameOver(string winner)
    {
        ClientOnGameOver?.Invoke(winner);
    }
    #endregion
}
