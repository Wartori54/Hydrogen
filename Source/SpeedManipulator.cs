using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Hydrogen;

public static class SpeedManipulator {
    public static bool IsDrawUpdate { get; private set; } = false;

    private static readonly Stopwatch FrameTimer = Stopwatch.StartNew();
    
    public static double UpdateTime { get; private set; }

    private static int fastUpdates = 0;
    public static int TotalUpdatesPerFrame { get; private set; }

    public static event Action? DrawUpdate;

    public static void Celeste_Update(On.Celeste.Celeste.orig_Update orig_Update, Celeste celeste, GameTime gameTime) {
        fastUpdates++;
        UpdateTime = gameTime.ElapsedGameTime.TotalSeconds;
        if (!HydrogenModule.Settings.UncappedSpeed) {
            orig_Update(celeste, gameTime);
        } else {
            orig_Update(celeste, new GameTime(gameTime.TotalGameTime, celeste.TargetElapsedTime, gameTime.IsRunningSlowly));
        }

        if (HydrogenModule.Settings.ToggleSpeed.Pressed)
            HydrogenModule.Settings.UncappedSpeed = !HydrogenModule.Settings.UncappedSpeed;

        celeste.IsFixedTimeStep = !HydrogenModule.Settings.UncappedSpeed;
        IsDrawUpdate = !HydrogenModule.Settings.UncappedSpeed;
        if (FrameTimer.ElapsedMilliseconds >= celeste.TargetElapsedTime.TotalMilliseconds) {
            FrameTimer.Restart();
            IsDrawUpdate = true;
        }

        if (IsDrawUpdate) {
            TotalUpdatesPerFrame = fastUpdates;
            fastUpdates = 0;
            DrawUpdate?.Invoke();
        }
    }

    public delegate bool BeginDrawDelegate(Game game);

    public static bool Game_BeginDraw(BeginDrawDelegate orig_BeginDraw, Game game) {
        if (IsDrawUpdate)
            return orig_BeginDraw(game);
        return false;
    }
    
}