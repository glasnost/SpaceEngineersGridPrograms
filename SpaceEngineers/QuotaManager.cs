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

namespace SpaceEngineers.UWBlockPrograms.InventoryManager {
    public sealed class Program : MyGridProgram {
        #endregion

        // Constants
        static Int32 TERMWIDTH = 80;
        static string DISPLAYNAME = "Component Monitor LCD";

        // Globals
        private List<IMyEntity> inventoryBlocks;
        private List<IMyAssembler> assemblerBlocks;
        private IMyTextPanel displayScreen;
        private readonly Dictionary<VRage.MyTuple<string, string>, ItemDef> itemDict;

        // Struct for defining an item to track.
        private struct ItemDef
        {
            public float Min;
            public float Available;
            public int Queued;
            public ItemDef(float min = -1f, float available = 0f, int queued = 0)
            {
                Min = min;
                Available = available;
                Queued = queued;
            }
        }

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            displayScreen = GridTerminalSystem.GetBlockWithName(DISPLAYNAME) as IMyTextPanel;

            inventoryBlocks = GetInventoryBlocks(Me);
            assemblerBlocks = GetAssemblerBlocks(Me);

            itemDict = new Dictionary<VRage.MyTuple<string, string>, ItemDef>
            {
                //{ VRage.MyTuple.Create("MyObjectBuilder_Ingot", "Cobalt"), new ItemDef() },
                //{ VRage.MyTuple.Create("MyObjectBuilder_Ingot", "Gold"), new ItemDef() },
                //{ VRage.MyTuple.Create("MyObjectBuilder_Ingot", "Iron"), new ItemDef() },
                //{ VRage.MyTuple.Create("MyObjectBuilder_Ingot", "Magnesium"), new ItemDef() },
                //{ VRage.MyTuple.Create("MyObjectBuilder_Ingot", "Nickel"), new ItemDef() },
                //{ VRage.MyTuple.Create("MyObjectBuilder_Ingot", "Platinum"), new ItemDef() },
                //{ VRage.MyTuple.Create("MyObjectBuilder_Ingot", "Silicon"), new ItemDef() },
                //{ VRage.MyTuple.Create("MyObjectBuilder_Ingot", "Silver"), new ItemDef() },
                //{ VRage.MyTuple.Create("MyObjectBuilder_Ingot", "Stone"), new ItemDef() },
                //{ VRage.MyTuple.Create("MyObjectBuilder_Ingot", "Uranium"), new ItemDef() },
                { VRage.MyTuple.Create("MyObjectBuilder_Component", "BulletproofGlass"), new ItemDef() },
                { VRage.MyTuple.Create("MyObjectBuilder_Component", "Canvas"), new ItemDef() },
                { VRage.MyTuple.Create("MyObjectBuilder_Component", "Computer"), new ItemDef() },
                { VRage.MyTuple.Create("MyObjectBuilder_Component", "Construction"), new ItemDef() },
                { VRage.MyTuple.Create("MyObjectBuilder_Component", "Detector"), new ItemDef() },
                { VRage.MyTuple.Create("MyObjectBuilder_Component", "Display"), new ItemDef() },
                { VRage.MyTuple.Create("MyObjectBuilder_Component", "Explosives"), new ItemDef() },
                { VRage.MyTuple.Create("MyObjectBuilder_Component", "Girder"), new ItemDef() },
                { VRage.MyTuple.Create("MyObjectBuilder_Component", "GravityGenerator"), new ItemDef() },
                { VRage.MyTuple.Create("MyObjectBuilder_Component", "InteriorPlate"), new ItemDef() },
                { VRage.MyTuple.Create("MyObjectBuilder_Component", "LargeTube"), new ItemDef() },
                { VRage.MyTuple.Create("MyObjectBuilder_Component", "Medical"), new ItemDef() },
                { VRage.MyTuple.Create("MyObjectBuilder_Component", "MetalGrid"), new ItemDef() },
                { VRage.MyTuple.Create("MyObjectBuilder_Component", "Motor"), new ItemDef() },
                { VRage.MyTuple.Create("MyObjectBuilder_Component", "PowerCell"), new ItemDef() },
                { VRage.MyTuple.Create("MyObjectBuilder_Component", "RadioCommunication"), new ItemDef() },
                { VRage.MyTuple.Create("MyObjectBuilder_Component", "Reactor"), new ItemDef() },
                { VRage.MyTuple.Create("MyObjectBuilder_Component", "SmallTube"), new ItemDef() },
                { VRage.MyTuple.Create("MyObjectBuilder_Component", "SolarCell"), new ItemDef() },
                { VRage.MyTuple.Create("MyObjectBuilder_Component", "SteelPlate"), new ItemDef(5) },
                { VRage.MyTuple.Create("MyObjectBuilder_Component", "Superconductor"), new ItemDef() },
                { VRage.MyTuple.Create("MyObjectBuilder_Component", "Thrust"), new ItemDef() }
            };
        }

        public void Save()
        {
            inventoryBlocks = GetInventoryBlocks(Me);
            assemblerBlocks = GetAssemblerBlocks(Me);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            // Copy to reset the values
            var localItemDict = new Dictionary<VRage.MyTuple<string, string>, ItemDef>(itemDict);

            // Enumerate the blocks that have inventory
            inventoryBlocks.ForEach(delegate (IMyEntity e)
            {
                var items = new List<MyInventoryItem>();
                for (int i = 0; i < e.InventoryCount; i++)
                {
                    e.GetInventory(i).GetItems(items);
                    ItemDef thisItem;
                    items.ForEach(delegate (MyInventoryItem item)
                    {
                        var itemKey = VRage.MyTuple.Create(item.Type.TypeId, item.Type.SubtypeId);
                        if (localItemDict.TryGetValue(itemKey, out thisItem))
                        {
                            thisItem.Available += (float)item.Amount;
                            localItemDict[itemKey] = thisItem;
                        }
                    });
                }
            });
            // Get the total queued items
            var queuedItems = new List<MyProductionItem>();
            assemblerBlocks.Where(a => a.Mode == MyAssemblerMode.Assembly)
                .ForEach(delegate (IMyAssembler a)
            {
                var q = new List<MyProductionItem>();
                a.GetQueue(q);
                q.ForEach(delegate (MyProductionItem i)
                {
                    string iname = i.BlueprintId.SubtypeName;
                    if (iname.EndsWith("Component"))
                    {
                        iname = iname.Substring(0, iname.Length - "Component".Length);
                    }
                    var itemKey = VRage.MyTuple.Create("MyObjectBuilder_Component", iname);
                    ItemDef thisItem;
                    if (localItemDict.TryGetValue(itemKey, out thisItem))
                    {
                        thisItem.Queued += (int)i.Amount;
                        localItemDict[itemKey] = thisItem;
                    }
                });
            });

            // Find any components that need to be assembled
            StringBuilder output = new StringBuilder(256);
            foreach (var key in localItemDict.Keys)
            {
                var item = localItemDict[key];
                string itemName = key.Item2;
                int toQueue = (int)item.Min - ((int)item.Available + (int)item.Queued);
                if (toQueue > 0)
                {
                    output.Append(itemName + ": " + (int)item.Min + " min/" + (int)item.Available + " avai/" + (int)item.Queued + " q'd->" + toQueue + "to add\n");
                    if (itemName == "Computer" || itemName == "Construction"
                        || itemName == "Detector" || itemName == "Explosives"
                        || itemName == "Girder" || itemName == "GravityGenerator"
                        || itemName == "Medical" || itemName == "Motor"
                        || itemName == "RadioCommunication" || itemName == "Reactor"
                        || itemName == "Thrust")
                    { itemName += "Component"; }
                    MyDefinitionId bp = new MyDefinitionId();
                    if (MyDefinitionId.TryParse("MyObjectBuilder_BlueprintDefinition/" + itemName, out bp))
                    {
                        //assemblerBlocks[0].AddQueueItem(bp, (decimal)toQueue);
                    }
                }
            }
            output.Append("Last runtime: " + this.Runtime.LastRunTimeMs + "ms");
            Me.GetSurface(0).WriteText(output);
        }

        // Returns all blocks that have an inventory on the same grid as parentGridBlock
        private List<IMyEntity> GetInventoryBlocks(IMyTerminalBlock parentGridBlock)
        {
            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocks(blocks);
            return blocks.Where(
                b => b.CubeGrid == parentGridBlock.CubeGrid &&
                        (b.HasInventory || b.InventoryCount > 0) &&
                        b.IsFunctional
            ).ToList<IMyEntity>();
        }

        // Returns a list of assemblers that are on the current grid and are in assembly mode.
        private List<IMyAssembler> GetAssemblerBlocks(IMyTerminalBlock parentGridBlock)
        {
            var blocks = new List<IMyAssembler>();
            GridTerminalSystem.GetBlocksOfType(blocks, delegate (IMyAssembler a)
            {
                return a.CubeGrid == parentGridBlock.CubeGrid;
            });
            return blocks;
        }

        #region PreludeFooter
    }
}
#endregion