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

namespace SpaceEngineers.UWBlockPrograms.BatteryMonitor {
    public sealed class Program : MyGridProgram {
#endregion

static Int32 TERMWIDTH = 80;
static string DISPLAYNAME = "Battery Monitor LCD";

private string terminalBanner;

private List<IMyBatteryBlock> batteryBlocks;
private IMyTextPanel displayScreen;


public Program()
{
    // Set update tickrate
    Runtime.UpdateFrequency = UpdateFrequency.Update100;

    // Initialize output screen by name in text mode
    displayScreen = GridTerminalSystem.GetBlockWithName(DISPLAYNAME) as IMyTextPanel;
    displayScreen.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;

    // Initialize batteries with output screen as reference grid
    batteryBlocks = DiscoverBatteries(displayScreen);

    // Prepare terminal header according to terminal width
    terminalBanner = MakeTerminalBanner(
        "Battery Monitor 3.1: Array Status", 
        TERMWIDTH
    );

}

public void Save()
{
    // We use the save event callback to refresh known battery blocks.
    // This introduces a delay in updating the display, but it also saves
    // some cycles every 100 ticks.
    batteryBlocks = DiscoverBatteries(displayScreen);
}

public void Main(string args)
{
    // Array properties
    Int32 arrayBatteryCount = batteryBlocks.Count();
    float arrayCapacity = 0;
    float arrayCurrentInput = 0;
    float arrayCurrentOutput = 0;
    float arrayCurrentStoredPower = 0;
    Int32 arrayPercentCharged = 0;

    // Templating Elements 
    var arrayChargeDirection = "";
    var arrayRuntimeStatus = "";
    

    // Fetch Battery Status Details
    foreach(var battery in batteryBlocks) {
        arrayCapacity += battery.MaxStoredPower;
        arrayCurrentInput += battery.CurrentInput;
        arrayCurrentOutput += battery.CurrentOutput;
        arrayCurrentStoredPower += battery.CurrentStoredPower;
    }

    // Determine charged percentage of entire array
    if(arrayBatteryCount != 0) {
        arrayPercentCharged = 
            (Int32)Math.Round((arrayCurrentStoredPower / arrayCapacity) * 100);
    }

    // Determine whether or not array is charging or discharging, update
    // status strings accordingly for templating.
    if(arrayPercentCharged == 100) {
        arrayChargeDirection = MakeSeparator('-', (TERMWIDTH / 100) * 75);
        arrayRuntimeStatus = "Array fully charged: Power drain minimal";

    } else if (arrayCurrentInput > arrayCurrentOutput) {
        arrayChargeDirection = MakeSeparator('>', TERMWIDTH/2);
        arrayRuntimeStatus = "Array fully charged in: " + 
            RenderRuntimeEstimate(
                (arrayCapacity - arrayCurrentStoredPower), 
                (arrayCurrentInput - arrayCurrentOutput)
            );
    } else {
        arrayChargeDirection = MakeSeparator('<', TERMWIDTH/2);
        arrayRuntimeStatus = "Array fully discharged in: " +
            RenderRuntimeEstimate(
                arrayCurrentStoredPower,
                (arrayCurrentOutput - arrayCurrentInput)
            );
    }

    // Template terminal output
    var terminalOutput = new StringBuilder();

    // Construct Output Header
    terminalOutput.AppendLine(terminalBanner);
    terminalOutput.Append('\n');

    // Battery Charge Progress Bar
    terminalOutput.Append(
        "                   Battery Charge: " + arrayPercentCharged + "%\n" +
        "    [" + RenderProgressBar(arrayPercentCharged, TERMWIDTH) + "]" +
        "\n\n"
    );

    // Status values
    terminalOutput.Append(
        "  Total batteries: " + arrayBatteryCount + "\n" +
        "  " + arrayRuntimeStatus +  "\n\n" +
        "  Current Charge: " + RenderPowerValue(arrayCurrentStoredPower) + "\n" +
        "  Maximum Capacity: " + RenderPowerValue(arrayCapacity) + "\n\n"
    );

    // Input and Output Status
    terminalOutput.Append(
        arrayChargeDirection + "\n" +
        "    Input: " + RenderPowerValue(arrayCurrentInput) +
        "                  Output: " + RenderPowerValue(arrayCurrentOutput) + "\n" +
        arrayChargeDirection
    );

    // Flush output to LCD Monitor
    displayScreen.WriteText(terminalOutput);
}

// Return a list of batteries attached to the parent grid of parentGridBlock
private List<IMyBatteryBlock> DiscoverBatteries(IMyTerminalBlock parentGridBlock)
{
    var discoveredBatteries = new List<IMyBatteryBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(discoveredBatteries);

    return discoveredBatteries.Where(
        b => b.IsFunctional &&
                b.CubeGrid == parentGridBlock.CubeGrid
    ).ToList<IMyBatteryBlock>();
}

private string RenderRuntimeEstimate(float capacity, float load)
{
    var renderedValue = "";

    double rawTimeValue = Math.Round(capacity / load * 60);
    if(rawTimeValue >= 1400) {
        renderedValue = Math.Round(rawTimeValue / 1440).ToString() + " days";
    } else if (rawTimeValue >= 60) {
        renderedValue = Math.Round(rawTimeValue / 60).ToString() + " hours";
    } else if (rawTimeValue > 1) {
        renderedValue = rawTimeValue.ToString() + " minutes";
    } else {
        renderedValue = "<1 minute";
    }

    return renderedValue;
}

// Convenience method to display power values with their suffix.
private string RenderPowerValue(float power)
{
    var renderedValue = "";
    if(power >= 1.0f) {
        renderedValue = (Int32)Math.Round(power, 0) + " MW";
    } else if (power < 0.001f || power == 0) {
        renderedValue =  "0 W";
    } else {
        renderedValue = (Int32)Math.Round((power * 1000), 0) + " kW";
    }

    return renderedValue;
}

// Render a progress bar of an arbitrary character length.
private string RenderProgressBar(int percent, int length)
{
    char[] progressBar = new char[length];
    int completedFactor = Convert.ToInt32(length * percent / 100);

    for (int i=0; i<(length-1); i++) {
        if (i <= completedFactor) {
            progressBar[i] = '|';
        } else {
            progressBar[i] = (char)39; // apostrophe
        }
    }

    return new string(progressBar);
}

// Render a horizontal line of arbitrary characters
private string MakeSeparator(char dchar, int len)
{
    return new string(dchar, len);
}

// Generate terminal banner with separators
private string MakeTerminalBanner(string title, int width)
{
    // Don't try to center if string is too long
    if ( width <= title.Length) return title;

    string sep = MakeSeparator('=', width - 1);

    int padding = width - title.Length;
    
    string ctitle = title.PadLeft(
        title.Length + padding / 2, ' ' 
    ).PadRight(width - 1, ' ');

    StringBuilder headersb = new StringBuilder();
    headersb.AppendLine(sep);
    headersb.AppendLine(ctitle);
    headersb.AppendLine(sep);
    return headersb.ToString();

}

#region PreludeFooter
    }
}
#endregion