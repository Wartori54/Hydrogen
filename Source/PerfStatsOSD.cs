using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.Hydrogen.OSD;

public class PerfStatsOSD : Entity {

    public double UPSMax = 0f;
    public double UPSMin = Double.MaxValue;
    public double UPSAvg = 0f;
    public int UPSAvgCounter = 0;
    public bool UPSInvalid = false;
    private string? upsAvgText;
    private string? upsMultText;

    public PerfStatsOSD() {
      this.Tag = (int) Tags.HUD | (int) Tags.Global | (int) Tags.PauseUpdate | (int) Tags.TransitionUpdate | (int) Tags.FrozenUpdate;
      this.Depth = -100;
      this.Y = 50+60+50+60+60;
      this.X = 32f;
      CalculateBaseSizes();
    }
    
    public static void hook_LevelLoader_LoadingThread(ILContext ctx) {
        ILCursor cursor = new(ctx);
        // Match the last `Level.Add` which should be `GrabbyIcon`
        cursor.GotoNext(MoveType.After, i => i.MatchNewobj(typeof(GrabbyIcon)),
            i => i.MatchCallvirt<Scene>(nameof(Scene.Add)));
        // Pass the levelloader instance instead because its easier
        cursor.EmitLdarg0();
        cursor.EmitDelegate<Action<LevelLoader>>(_this => {
            _this.Level.Add(new PerfStatsOSD());
        });
    }

    public override void Update() {
        if (!HydrogenModule.Settings.EnableOSD) return;
        UPSAvg += SpeedManipulator.UpdateTime;
        UPSAvgCounter++;
        if (!HydrogenModule.Settings.UncappedSpeed) UPSInvalid = true;
        base.Update();
    }

    public override void Render() {
        if (!HydrogenModule.Settings.EnableOSD) return;
        if (UPSAvgCounter != 0) {
            UPSAvg /= UPSAvgCounter;
            if (!UPSInvalid) {
                if (UPSAvg > UPSMax) UPSMax = UPSAvg;
                if (UPSAvg < UPSMin) UPSMin = UPSAvg;
            }

            UPSAvgCounter = 0;
            upsAvgText = (1 / UPSAvg).ToString("F2", CultureInfo.InvariantCulture) + " UPS " + 
                         (1/UPSMax).ToString("F2", CultureInfo.InvariantCulture) + " / " + 
                         (1/UPSMin).ToString("F2", CultureInfo.InvariantCulture);
            upsMultText = "x" + ((1f / UPSAvg) / 60F).ToString("F2", CultureInfo.InvariantCulture) + " " +
                          (1/UPSMax / 60f). ToString("F2", CultureInfo.InvariantCulture) + " / " +
                          (1/UPSMin / 60f).ToString("F2", CultureInfo.InvariantCulture);
        } // Only update if we got new data, draw previous contents otherwise

        UPSAvg = 0f;
        UPSInvalid = false;
        DrawTime(Position, upsAvgText ?? "0.0");
        DrawTime(Position + new Vector2(0, 50), upsMultText ?? "0.0");
        base.Render();
    }

    private static float spacerWidth;
    private static float numberWidth;
    
    // Copied from celeste, modified as required
    public static void DrawTime(
        Vector2 position,
        string timeString,
        float scale = 1f,
        float alpha = 1f)
    {
        PixelFont font = Dialog.Languages["english"].Font;
        float fontFaceSize = Dialog.Languages["english"].FontFaceSize;
        float currentScale = scale;
        float currX = position.X;
        float currY = position.Y;
        Color white = Color.White * alpha;
        Color gray = Color.LightGray * alpha; 
        foreach (char ch in timeString) {
            if (ch == ' ') { // reset it
                currentScale = scale;
                currY = position.Y;
            } else if (ch == '.') {
                currentScale = scale * 0.7f;
                currY -= 5f * scale;
            }
            Color charColor = ch == ':' || ch == '.' || currentScale < scale ? gray : white;
            float currWidth = ch is ':' or '.' ? spacerWidth : numberWidth;
            float realWidth = (float) (currWidth + 4.0) * currentScale;
            font.DrawOutline(
                fontFaceSize, 
                ch.ToString(), 
                new Vector2(currX + realWidth / 2f, currY), 
                new Vector2(0.5f, 1f), 
                Vector2.One * currentScale, 
                charColor, 2f, 
                Color.Black);
            currX += realWidth;
        }
    }
    
    // Also stolen
    public static void CalculateBaseSizes() {
        PixelFontSize pixelFontSize = Dialog.Languages["english"].Font.Get(Dialog.Languages["english"].FontFaceSize);
        for (int index = 0; index < 10; ++index) {
            float x = pixelFontSize.Measure(index.ToString()).X;
            if (x > numberWidth)
                numberWidth = x;
        }
        spacerWidth = pixelFontSize.Measure('.').X;
    }
}