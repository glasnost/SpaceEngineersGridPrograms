#region Prelude
using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using VRageMath;
using VRage.Game;
using VRage.Collections;
using Sandbox.ModAPI.Ingame;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;

namespace SpaceEngineers.UWBlockPrograms.OxygenFarm {
    public sealed class Program : MyGridProgram {
#endregion

static Int32 TERMWIDTH = 80;                   // Terminal Width
static string CDATATAG = "oxygenfarm";         // Custom Data Tag
static string DISPLAYNAME = "Oxygen Farm LCD"; // Output Display Name

private List<IMyGasTank> storageTanks;
private List<IMyOxygenFarm> oxygenFarms;
private IMyTextPanel displayScreen;

// Constructor
public Program()
{
    // Update every 100 Ticks
    Runtime.UpdateFrequency = UpdateFrequency.Update100;

    // Initialize output screen by name
    displayScreen = GridTerminalSystem.GetBlockWithName(DISPLAYNAME) as IMyTextPanel;
    displayScreen.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;

    // Initialize Oxygen Storage Tanks and Farms
    storageTanks = DiscoverStorageTanks(CDATATAG);
    oxygenFarms = DiscoverOxygenFarms();
}

// Program Entry Point
public void Main(string args, UpdateType updateSource)
{
    // Initialize Output Screen by name

    // Ghetto String Templating Galore
    var strout = new StringBuilder();

    // Storage Tank Details
    double totalCapacity = 0;
    double totalOxygen = 0;

    // Farm Details
    double totalOutput = 0;
    var activeFarmsCount = oxygenFarms.Where(
            f => f.CanProduce &&
                    f.IsWorking &&
                    f.GetOutput() > 0
        ).Count<IMyOxygenFarm>();

    // Inventory oxygen farms
    foreach (var farm in oxygenFarms) {
        totalOutput += farm.GetOutput();
    }

    // Inventory oxygen tanks
    foreach (var tank in storageTanks) {
        totalCapacity += tank.Capacity;
        totalOxygen += (tank.Capacity * tank.FilledRatio);
    }

    double filledPercentage = totalOxygen * 100 / totalCapacity;

    strout.AppendLine(
        "==================================\n" +
        "      Oxygen Farm Monitor  v0.1a  \n" +
        "==================================\n\n" +
        "     Oxygen Storage: " + Math.Round(filledPercentage, 3) + "%\n" +
        "  [" + RenderProgressBar((Int32)filledPercentage, TERMWIDTH) + "]\n\n" +
        " Total Tanks: " + storageTanks.Count + "\n" +
        " Active Farms: " + activeFarmsCount + "/" + oxygenFarms.Count + "\n\n" +
        " Oxygen Production Rate: " + Math.Round(totalOutput, 4) + "L/m"
        );

    // Flush output to display
    displayScreen.WriteText(strout.ToString());
}

// On Save Callback
public void Save()
{
    // Use the save event callback to refresh known tanks and farms
    // We have nothing to serialize but this saves some cycles every 100 ticks.
    // I'm sorry
    storageTanks = DiscoverStorageTanks(CDATATAG);
    oxygenFarms = DiscoverOxygenFarms();
}

private List<IMyGasTank> DiscoverStorageTanks(string cdatatag)
{
    var discoveredTanks = new List<IMyGasTank>();
    GridTerminalSystem.GetBlocksOfType<IMyGasTank>(discoveredTanks);

    // Alright look let me level with you here, I have NO IDEA how to distinguish
    // between an oxygen and an hydrogen tank.
    // I have therefore decided to just wing it with the Type ID String
    return discoveredTanks.Where(
        t => t.CustomData == cdatatag &&
                t.IsFunctional &&
                t.BlockDefinition.TypeId.ToString().Contains("Oxygen")
    ).ToList<IMyGasTank>();
}

private List<IMyOxygenFarm> DiscoverOxygenFarms()
{
    var discoveredFarms = new List<IMyOxygenFarm>();
    GridTerminalSystem.GetBlocksOfType<IMyOxygenFarm>(discoveredFarms);

    // Only return functional farms, ignore half-built or damaged ones.
    return discoveredFarms.Where(
        f => f.IsFunctional
    ).ToList<IMyOxygenFarm>();
}

// Fill and return Progress Bar
private string RenderProgressBar(int percent, int length)
{
    char[] progressBar = new char[length];
    int completedFactor = Convert.ToInt32(length * percent / 100);

    for (int i = 0; i < (length - 1); i++) {
        if (i <= completedFactor) {
            progressBar[i] = '|';
        } else {
            progressBar[i] = (char)39;
        }
    }

    return new string(progressBar);
}

#region PreludeFooter
        // =====================================================================
        // -- END PROGRAM BLOCK --
        // =====================================================================
    }
}
#endregion