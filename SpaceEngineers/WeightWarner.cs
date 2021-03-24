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
using VRage.Game.GUI.TextPanel;

namespace SpaceEngineers.UWBlockPrograms.WeightWarner {
    public sealed class Program : MyGridProgram {

        // private bool doneInit = false;
        // private List<IMyInventory> inventories = new List<IMyInventory>();

        private bool showConfHelp = false;
        private string confHelp = "Put in Custom Data and then Run:\nCockpitId,DisplayNumber,KgDanger,KgFatal";
        private string help = "";
        private string cockpitId;
        private int displayNum;
        private int wgt1 = 0;
        private int wgt2 = 0;

        private IMyTextSurface display;
        private RectangleF viewport;
        private MySpriteDrawFrame frame;
        private List<IMyInventory> inventories;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            var confString = Me.CustomData;
            if (confString.Length == 0) {
                showConfHelp = true;
                return;
            }

            var conf = confString.Split(',');
            if (conf.Length != 4) { 
                showConfHelp = true;
                help = "\nInvalid Conf:\n" + confString;
                return;
            }
            cockpitId = conf[0];
            displayNum = int.Parse(conf[1]);
            wgt1 = int.Parse(conf[2]);
            wgt2 = int.Parse(conf[3]);

            if (showConfHelp) {
                ShowHelp();
                return;
            }

            var cockpit = GridTerminalSystem.GetBlockWithName(cockpitId);
            if (cockpit == null) {
                help = "\nBad Cockpit Name";
                ShowHelp();
                return;
            }

            try {
                display = (cockpit as IMyTextSurfaceProvider).GetSurface(displayNum);
            } catch {
                help = "\nCould not get display surface";
                ShowHelp();
                return;
            }

            PrepareTextSurfaceForSprites(display);
            viewport = new RectangleF(
                (display.TextureSize - display.SurfaceSize) / 2f,
                display.SurfaceSize
            );

            inventories = GetInventories();
        }

        public void Main(string argument, UpdateType updateSource)
        {
            frame = display.DrawFrame();
            VRage.MyFixedPoint totKg = 0;
            inventories.ForEach(delegate (IMyInventory i) {
                totKg += i.CurrentMass;
            });

            draw(totKg.ToIntSafe());
            frame.Dispose();
        }

        private void draw(int totKg) {
            var fg = display.ScriptForegroundColor;
            var bg = display.ScriptBackgroundColor;
            var cOk = Color.White.Alpha(0.07f);
            var cWarn = Color.DarkOrange.Alpha(0.80f);
            var cBad = Color.DarkRed.Alpha(0.80f);

            var showWarn = totKg >= wgt1;
            var showBad = totKg >= wgt2;

            // BG
            var sprite = new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "Grid",
                Position = viewport.Center,
                Size = display.TextureSize * 2,
                Color = Color.RoyalBlue,
                Alignment = TextAlignment.CENTER
            };
            frame.Add(sprite);

            var sb = new StringBuilder();
            sb.AppendLine(" DNGR WGT  " + KgToString(wgt1));
            sb.AppendLine("FATAL WGT  " + KgToString(wgt2));
            sb.AppendLine("~ ~ ~ ~ ~ ~");
            sb.AppendLine("CARGO WGT  " + KgToString(totKg));
            var textPosition = new Vector2(viewport.Width/2, 0) + viewport.Position;

            var textColor = fg;
            if (showWarn) {
                textColor = cWarn;
            }
            if (showBad) {
                textColor = cBad;
            }
            
            // Text
            sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = sb.ToString(),
                Position = textPosition,
                RotationOrScale = 0.9f, // 90% font size
                Color = textColor,
                Alignment = TextAlignment.CENTER,
                FontId = "White"
            };
            frame.Add(sprite);

            // Start positions/proportions on the bar
            // var okP = 0.0f;
            // var badP = 0.9f;
            var totBarKg = (wgt2 + wgt2/9f);
            var warnP = ((float)wgt1 / totBarKg);
            var filledP = (float)totKg / totBarKg;

            var barHeight = viewport.Height / 3f;
            var barWidth = viewport.Width * 0.9f;
            var barPosition = viewport.Position + new Vector2(
                (viewport.Width - barWidth) / 2f,
                viewport.Height - barHeight/2f - barHeight/4f);
            
            var okWidth = barWidth * warnP;
            var badWidth = barWidth * 0.1f;
            var warnWidth = barWidth - okWidth - badWidth;

            var rulerHeight = barHeight * 0.15f;
            var nonRulerHeight = barHeight - rulerHeight;
            var filledWidth = barWidth * filledP;
            var emptyWidth = barWidth - filledWidth;

            // Ruler
            var rulerPosition = barPosition;
            drawBar(
                rulerPosition,
                new Vector2(okWidth, rulerHeight),
                cOk
            );
            drawBar(
                rulerPosition + new Vector2(okWidth, 0),
                new Vector2(warnWidth, rulerHeight),
                cWarn
            );
            drawBar(
                rulerPosition + new Vector2(okWidth + warnWidth, 0),
                new Vector2(badWidth, rulerHeight),
                cBad
            );

            // Weight Bar
            var fillPosition = barPosition + new Vector2(0, rulerHeight/2f);
            var cx = (int)Math.Ceiling(fillPosition.X);
            var cy = (int)Math.Ceiling(fillPosition.Y);
            var cw = (int)Math.Ceiling(filledWidth);
            var ch = (int)Math.Ceiling(nonRulerHeight);
            var clipRect = new Rectangle(cx, cy, cw, ch);
            frame.Add(MySprite.CreateClipRect(clipRect));
            drawBar(
                fillPosition,
                new Vector2(okWidth, nonRulerHeight),
                cOk
            );
            drawBar(
                fillPosition + new Vector2(okWidth, 0),
                new Vector2(warnWidth, nonRulerHeight),
                cWarn
            );
            drawBar(
                fillPosition + new Vector2(okWidth + warnWidth, 0),
                new Vector2(badWidth, nonRulerHeight),
                cBad
            );
            frame.Add(MySprite.CreateClearClipRect());

        }

        private void drawBar(
            Vector2 pos,
            Vector2 size,
            Color color
        ) {
            var sprite = new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = "SquareSimple",
                Position = pos,
                Size = size,
                Color = color,
                Alignment = TextAlignment.LEFT
            };
            frame.Add(sprite);
        }
        private void ShowHelp() {
            Echo(confHelp + help);
        }

        private List<IMyInventory> GetInventories() {
            var invs = new List<IMyInventory>();
            var entities = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(entities, ent => {
                var ok = false;
                try { ok = ent.IsSameConstructAs(Me); } catch {}
                return ok;
            });
            foreach (IMyTerminalBlock e in entities) {
                if (!e.HasInventory || e.InventoryCount == 0) { continue; }
                invs.Add(e.GetInventory(0));
                if (e.InventoryCount == 2) {
                    invs.Add(e.GetInventory(1));
                }
            }
            return invs;
        }

        private string KgToString(int kg) {
            var t = Convert.ToDecimal(kg);
            if (t < 500) {
                return t.ToString() + " Kg";
            }
            if (t < 500000) {
                return Math.Round(t/1000,2) + " Mg";
            }
            if (t < 500000000) {
                return Math.Round(t/1000000,2) + " Gg";
            }
            if (t < 500000000000) {
                return Math.Round(t/1000000000,2) + " Tg";
            }
            return Math.Round(t/1000000000000,2) + " Pg";
        }

        public void PrepareTextSurfaceForSprites(IMyTextSurface textSurface)
        {
            // Set the sprite display mode
            textSurface.ContentType = ContentType.SCRIPT;
            // Make sure no built-in script has been selected
            textSurface.Script = "";
        }

    }
}
