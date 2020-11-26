﻿using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTSPlayer : NetworkBehaviour
{
    [SerializeField] private LayerMask buildingBlockLayer = new LayerMask();
    [SerializeField] private float buildingRangeLimit = 5f;
    [SerializeField] private Building[] buildings = new Building[0];

    [SyncVar(hook = nameof(ClientHandleResourcesUpdated))]
    private int resources = 500;

    private Color teamColor = new Color();
    private List<Unit> myUnits = new List<Unit>();
    private List<Building> myBuildings = new List<Building>();

    public event Action<int> ClientOnResourcesUpdated;

    public Color GetTeamColor()
    {
        return teamColor;
    }

    public int GetResources()
    {
        return resources;
    }

    public bool CanPlaceBuilding(BoxCollider buildingCollider, Vector3 point)
    {
        if (Physics.CheckBox(point + buildingCollider.center, buildingCollider.size / 2, Quaternion.identity, buildingBlockLayer))
            return false;

        foreach (Building building in myBuildings)
        {
            if ((point - building.transform.position).sqrMagnitude <= buildingRangeLimit * buildingRangeLimit)
            {
                return true;
            }
        }

        return false;
    }

    public List<Unit> GetMyUnits()
    {
        return myUnits;
    }

    public List<Building> GetMyBuildings()
    {
        return myBuildings;
    }

    #region Server
    public override void OnStartServer()
    {
        Unit.ServerOnUnitSpawned += ServerHandleUnitSpawned;
        Unit.ServerOnUnitDespawned += ServerHandleUnitDespawned;
        Building.ServerOnBuildingSpawned += ServerHandleBuildingSpawned;
        Building.ServerOnBuildingDespawned += ServerHandleBuildingDespawned;
    }

    public override void OnStopServer()
    {
        Unit.ServerOnUnitSpawned -= ServerHandleUnitSpawned;
        Unit.ServerOnUnitDespawned -= ServerHandleUnitDespawned;
        Building.ServerOnBuildingSpawned -= ServerHandleBuildingSpawned;
        Building.ServerOnBuildingDespawned -= ServerHandleBuildingDespawned;
    }


    [Server]
    public void SetTeamColor(Color color)
    {
        teamColor = color;
    }

    [Server]
    public void SetResources(int newResources)
    {
        resources = newResources;
    }

    [Command]
    public void CmdTryPlaceBuilding(int buildingId, Vector3 point)
    {
        Building buildingToPlace = null;
        foreach (Building building in buildings)
            if (building.GetId() == buildingId)
            {
                buildingToPlace = building;
                break;
            }

        if (buildingToPlace == null)
            return;

        if (resources < buildingToPlace.GetPrice())
            return;

        // collides
        BoxCollider buildingCollider = buildingToPlace.GetComponent<BoxCollider>();

        if (!CanPlaceBuilding(buildingCollider, point))
            return;

        GameObject buildingInstance = Instantiate(buildingToPlace.gameObject, point, buildingToPlace.transform.rotation);
        NetworkServer.Spawn(buildingInstance, connectionToClient);

        SetResources(resources - buildingToPlace.GetPrice());
    }

    private void ServerHandleBuildingSpawned(Building building)
    {
        if (building.connectionToClient.connectionId != connectionToClient.connectionId)
            return;

        myBuildings.Add(building);
    }

    private void ServerHandleBuildingDespawned(Building building)
    {
        if (building.connectionToClient.connectionId != connectionToClient.connectionId)
            return;

        myBuildings.Remove(building);
    }

    private void ServerHandleUnitSpawned(Unit unit)
    {
        if (unit.connectionToClient.connectionId != connectionToClient.connectionId)
            return;

        myUnits.Add(unit);
    }

    private void ServerHandleUnitDespawned(Unit unit)
    {
        if (unit.connectionToClient.connectionId != connectionToClient.connectionId)
            return;

        myUnits.Remove(unit);
    }

    #endregion
    #region Client
    public override void OnStartAuthority()
    {
        if (NetworkServer.active)
            return;

        Unit.AuthorityOnUnitSpawned += AuthorityHandleUnitSpawned;
        Unit.AuthorityOnUnitDespawned += AuthorityHandleUnitDespawned;
        Building.AuthorityOnBuildingSpawned += AuthorityHandleBuildingSpawned;
        Building.AuthorityOnBuildingDespawned += AuthorityHandleBuildingDespawned;
    }

    public override void OnStopClient()
    {
        if (!isClientOnly || !hasAuthority)
            return;

        Unit.AuthorityOnUnitSpawned -= AuthorityHandleUnitSpawned;
        Unit.AuthorityOnUnitDespawned -= AuthorityHandleUnitDespawned;
        Building.AuthorityOnBuildingSpawned -= AuthorityHandleBuildingSpawned;
        Building.AuthorityOnBuildingDespawned -= AuthorityHandleBuildingDespawned;
    }

    private void AuthorityHandleBuildingDespawned(Building building)
    {
        myBuildings.Add(building);
    }

    private void AuthorityHandleBuildingSpawned(Building building)
    {
        myBuildings.Remove(building);
    }

    private void AuthorityHandleUnitSpawned(Unit unit)
    {
        myUnits.Add(unit);
    }

    private void AuthorityHandleUnitDespawned(Unit unit)
    {
        myUnits.Remove(unit);
    }

    private void ClientHandleResourcesUpdated(int oldResources, int newResources)
    {
        ClientOnResourcesUpdated?.Invoke(newResources);
    }
    #endregion
}
