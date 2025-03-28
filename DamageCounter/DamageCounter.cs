using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Collections.Generic;
using System.Linq;

public class ProDamageReport : BasePlugin
{
    public override string ModuleName => "DamageCounter";
    public override string ModuleVersion => "1.0";

    private class DamageInfo
    {
        public int Damage { get; set; }
        public int Hits { get; set; }
        public int Headshots { get; set; }
    }

    private Dictionary<ulong, Dictionary<string, DamageInfo>> _damageData = new();

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
    }

    private HookResult OnRoundStart(EventRoundStart _, GameEventInfo __)
    {
        _damageData.Clear();
        return HookResult.Continue;
    }

    private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo __)
    {
        var attacker = @event.Attacker;
        var victim = @event.Userid;

        if (attacker == null || victim == null || !attacker.IsValid || attacker == victim)
            return HookResult.Continue;

        string victimName = victim.PlayerName;
        ulong attackerId = attacker.SteamID;

        if (!_damageData.TryGetValue(attackerId, out var attackerData))
        {
            attackerData = new Dictionary<string, DamageInfo>();
            _damageData[attackerId] = attackerData;
        }

        if (!attackerData.TryGetValue(victimName, out var info))
            info = new DamageInfo();

        info.Damage += @event.DmgHealth;
        info.Hits++;
        if (@event.Hitgroup == 1) info.Headshots++;

        attackerData[victimName] = info;
        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd _, GameEventInfo __)
    {
        foreach (var attackerEntry in _damageData)
        {
            var attacker = Utilities.GetPlayerFromSteamId(attackerEntry.Key);
            if (attacker == null || !attacker.IsValid) continue;

            var health = attacker.PlayerPawn?.Value?.Health ?? 0;
            var healthColor = health > 0 ? "\x07" : "\x02";


            foreach (var targetEntry in attackerEntry.Value)
            {
                Server.PrintToChatAll(
                    $" \x04[DMG]\x01 Damage to \x06{targetEntry.Key}\x01: " +
                    $"\x07{targetEntry.Value.Damage}dmg\x01/" +
                    $"\x05{targetEntry.Value.Hits}hits\x01/" +
                    $"\x09{targetEntry.Value.Headshots}hs\x01 | " //+
                    //$"Player: \x06{attacker.PlayerName}\x01 ({healthColor}{health}HP\x01)"
                    //remove this lines if you want to see player name and health
                );
            }
        }
        return HookResult.Continue;
    }
}