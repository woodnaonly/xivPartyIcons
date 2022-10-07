﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Network;
using Dalamud.IoC;
using Dalamud.Logging;
using Newtonsoft.Json;
using PartyIcons.Entities;
using PartyIcons.Utils;

namespace PartyIcons.Runtime;

public sealed class PartyListHUDUpdater : IDisposable
{
    public bool UpdateHUD = false;

    [PluginService] public PartyList PartyList { get; set; }
    [PluginService] public Framework Framework { get; set; }
    [PluginService] public GameNetwork GameNetwork { get; set; }
    [PluginService] public ClientState ClientState { get; set; }

    private readonly Configuration _configuration;
    private readonly PartyListHUDView _view;
    private readonly RoleTracker _roleTracker;

    private bool _displayingRoles = false;

    private bool _previousInParty = false;
    private bool _previousTesting = false;
    private DateTime _lastUpdate = DateTime.Today;

    private const string OpcodesUrl = "https://raw.githubusercontent.com/karashiiro/FFXIVOpcodes/master/opcodes.min.json";
    private List<int> _prepareZoningOpcodes = new();

    public PartyListHUDUpdater(PartyListHUDView view, RoleTracker roleTracker, Configuration configuration)
    {
        _view = view;
        _roleTracker = roleTracker;
        _configuration = configuration;

        Task.WaitAll(new[] { DownloadOpcodes() });
    }

    public void Enable()
    {
        _roleTracker.OnAssignedRolesUpdated += OnAssignedRolesUpdated;
        Framework.Update += OnUpdate;
        GameNetwork.NetworkMessage += OnNetworkMessage;
        _configuration.OnSave += OnConfigurationSave;
        ClientState.EnterPvP += OnEnterPvP;
    }

    public void Dispose()
    {
        ClientState.EnterPvP -= OnEnterPvP;
        _configuration.OnSave -= OnConfigurationSave;
        GameNetwork.NetworkMessage -= OnNetworkMessage;
        Framework.Update -= OnUpdate;
        _roleTracker.OnAssignedRolesUpdated -= OnAssignedRolesUpdated;
    }

    private async Task DownloadOpcodes()
    {
        var client = new HttpClient();
        var data = await client.GetStringAsync(OpcodesUrl);
        dynamic json = JsonConvert.DeserializeObject(data);

        foreach (var clientType in json)
        {
            if (clientType.region == "Global")
            {
                foreach (var record in clientType["lists"]["ServerZoneIpcType"])
                {
                    var name = record.name.ToString();
                    var opcode = (int)record.opcode;
                    if (name == "PrepareZoning")
                    {
                        _prepareZoningOpcodes.Add(opcode);
                        PluginLog.Debug($"Adding zoning opcode - {record.name} ({record.opcode})");
                    }
                }
            }
        }
    }

    private void OnEnterPvP()
    {
        if (_displayingRoles)
        {
            PluginLog.Debug("PartyListHUDUpdater: reverting party list due to entering a PvP zone");
            _displayingRoles = false;
            _view.RevertSlotNumbers();
        }
    }

    private void OnConfigurationSave()
    {
        if (_displayingRoles)
        {
            PluginLog.Debug("PartyListHUDUpdater: reverting party list before the update due to config change");
            _view.RevertSlotNumbers();
        }

        PluginLog.Debug("PartyListHUDUpdater forcing update due to changes in the config");
        PluginLog.Verbose(_view.GetDebugInfo());
        UpdatePartyListHUD();
    }

    private void OnAssignedRolesUpdated()
    {
        PluginLog.Debug("PartyListHUDUpdater forcing update due to assignments update");
        PluginLog.Verbose(_view.GetDebugInfo());
        UpdatePartyListHUD();
    }

    private void OnNetworkMessage(IntPtr dataptr, ushort opcode, uint sourceactorid, uint targetactorid,
        NetworkMessageDirection direction)
    {
        if (direction == NetworkMessageDirection.ZoneDown && _prepareZoningOpcodes.Contains(opcode) &&
            targetactorid == ClientState.LocalPlayer?.ObjectId)
        {
            PluginLog.Debug("PartyListHUDUpdater Forcing update due to zoning");
            PluginLog.Verbose(_view.GetDebugInfo());
            UpdatePartyListHUD();
        }
    }

    private void OnUpdate(Framework framework)
    {
        var inParty = PartyList.Any();

        if ((!inParty && _previousInParty) || (!_configuration.TestingMode && _previousTesting))
        {
            PluginLog.Debug("No longer in party/testing mode, reverting party list HUD changes");
            _displayingRoles = false;
            _view.RevertSlotNumbers();
        }

        _previousInParty = inParty;
        _previousTesting = _configuration.TestingMode;

        if (DateTime.Now - _lastUpdate > TimeSpan.FromSeconds(15))
        {
            UpdatePartyListHUD();
            _lastUpdate = DateTime.Now;
        }
    }

    private void UpdatePartyListHUD()
    {
        if (!_configuration.DisplayRoleInPartyList)
        {
            return;
        }

        if (_configuration.TestingMode)
        {
            var localPlayer = ClientState.LocalPlayer;
            _view.SetPartyMemberRole(localPlayer.Name.ToString(), localPlayer.ObjectId, RoleId.M1);
        }

        if (!UpdateHUD)
        {
            return;
        }

        if (ClientState.IsPvP)
        {
            return;
        }

        PluginLog.Verbose("Updating party list HUD");
        _displayingRoles = true;

        foreach (var member in PartyList)
        {
            if (_roleTracker.TryGetAssignedRole(member.Name.ToString(), member.World.Id, out var roleId))
            {
                PluginLog.Verbose($"Updating party list hud: member {member.Name} to {roleId}");
                _view.SetPartyMemberRole(member.Name.ToString(), member.ObjectId, roleId);
            }
        }
    }
}