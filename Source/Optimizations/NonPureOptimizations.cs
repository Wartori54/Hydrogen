using System;
using System.Collections.Generic;
using System.Reflection;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.Hydrogen.Optimizations;

public static class NonPureOptimizations {
    /// <summary>
    /// Disables audio updates on non update frames
    /// </summary>
    public class SlowDownAudioUpdate : Optimizations.IOptimization {
        public bool Pure => false;
        public int Level => 1;
        public void Load() {
            On.Celeste.Audio.Update += Patch;
            On.Celeste.Audio.Play_string += PatchPlay;
            On.Celeste.Audio.Play_string_Vector2 += PatchPlay;
            On.Celeste.Audio.Play_string_string_float += PatchPlay;
            On.Celeste.Audio.Play_string_Vector2_string_float += PatchPlay;
            On.Celeste.Audio.Play_string_Vector2_string_float_string_float += PatchPlay;
        }

        public void Unload() {
            On.Celeste.Audio.Update -= Patch;
            On.Celeste.Audio.Play_string -= PatchPlay;
            On.Celeste.Audio.Play_string_Vector2 -= PatchPlay;
            On.Celeste.Audio.Play_string_string_float -= PatchPlay;
            On.Celeste.Audio.Play_string_Vector2_string_float -= PatchPlay;
            On.Celeste.Audio.Play_string_Vector2_string_float_string_float -= PatchPlay;
        }

        private static void Patch(On.Celeste.Audio.orig_Update orig) {
            if (!SpeedManipulator.IsDrawUpdate) {
                return;
            }

            orig();
        }

        private static EventInstance PatchPlay(On.Celeste.Audio.orig_Play_string orig, string path) {
            if (!SpeedManipulator.IsDrawUpdate)
                return null!;
            return orig(path);
        }
        
        private static EventInstance PatchPlay(On.Celeste.Audio.orig_Play_string_Vector2 orig, string path, Vector2 vector) {
            if (!SpeedManipulator.IsDrawUpdate)
                return null!;
            return orig(path, vector);
        }
        
        private static EventInstance PatchPlay(On.Celeste.Audio.orig_Play_string_string_float orig, string path, string s, float value) {
            if (!SpeedManipulator.IsDrawUpdate)
                return null!;
            return orig(path, s, value);
        }
        
        private static EventInstance PatchPlay(On.Celeste.Audio.orig_Play_string_Vector2_string_float orig, string path, Vector2 vector, string s, float value) {
            if (!SpeedManipulator.IsDrawUpdate)
                return null!;
            return orig(path, vector, s, value);
        }
        
        private static EventInstance PatchPlay(On.Celeste.Audio.orig_Play_string_Vector2_string_float_string_float orig, string path, Vector2 vector, string s, float value, string param2, float value2) {
            if (!SpeedManipulator.IsDrawUpdate)
                return null!;
            return orig(path, vector, s, value, param2, value2);
        }
    }

    /// <summary>
    /// Disables autosplitter updates on non frame updates
    /// </summary>
    public class SlowDownAutoSplitterUpdate : Optimizations.IOptimization {
        // This is not true in theory, but practically theres no real effect
        public bool Pure => true;
        public int Level => 1;
        public void Load() {
            On.Celeste.AutoSplitterInfo.Update += Patch;
        }

        public void Unload() {
            On.Celeste.AutoSplitterInfo.Update -= Patch;
        }

        private static void Patch(On.Celeste.AutoSplitterInfo.orig_Update orig, AutoSplitterInfo self) {
            if (!SpeedManipulator.IsDrawUpdate)
                return;
            
            orig(self);
        }
    }

    /// <summary>
    /// Disables DiscordSDK on non frame updates
    /// </summary>
    public class SlowDownDiscordSDK : Optimizations.IOptimization {
        // Lie, but no practical difference either
        public bool Pure => true;
        public int Level => 1;

        private Hook? hook;
        public void Load() {
            hook = new Hook(
                typeof(Everest.DiscordSDK).GetMethod("Update",
                BindingFlags.Instance | BindingFlags.Public)
                ?? throw new InvalidOperationException("Cannot find `Update` on `DiscordSDK`"), 
                Patch);
        }

        public void Unload() {
            hook?.Dispose();
        }

        public static void Patch(Action<Everest.DiscordSDK, GameTime> orig, Everest.DiscordSDK self, GameTime gt) {
            if (!SpeedManipulator.IsDrawUpdate) return;
            orig(self, gt);
        }
    }
    
    /// <summary>
    /// Disables MInput on non frame updates
    /// </summary>
    public class SlowDownMInputUpdate : Optimizations.IOptimization {
        public bool Pure => false;
        public int Level => 1;
        public void Load() {
            On.Monocle.MInput.MouseData.Update += Patch;
        }

        public void Unload() {
            On.Monocle.MInput.MouseData.Update -= Patch;
        }

        private static void Patch(On.Monocle.MInput.MouseData.orig_Update orig, Monocle.MInput.MouseData self) {
            if (!SpeedManipulator.IsDrawUpdate) return;
            orig(self);
        }
    }

    /// <summary>
    /// Skips event polling in FNA on non frame updates, this adds a frame of input lag
    /// </summary>
    public class SlowDownPollEvents : Optimizations.IOptimization, IDisposable {
        public bool Pure => false;
        public int Level => 2;

        private bool Enabled;

        public void Load() {
            if (Enabled) return;
            Enabled = true;
            IL.Microsoft.Xna.Framework.Game.Tick += Patch2;
        }

        public void Unload() {
            Enabled = false;
        }

        // This patch simply adds an if statement before the `PollEvents` call to make it only run on draw updates
        private static void Patch2(ILContext ctx) {
            Console.WriteLine("Patching");
            ILCursor cursor = new(ctx);
            if (!cursor.TryGotoNext(MoveType.AfterLabel, i => i.MatchLdsfld("Microsoft.Xna.Framework.FNAPlatform",
                    "PollEvents"))) throw new InvalidOperationException();

            cursor.EmitDelegate(() => SpeedManipulator.IsDrawUpdate);
            ILCursor labelCursor = cursor.Clone();
            if (!labelCursor.TryGotoNext(MoveType.After,
                                i => i.MatchCallvirt("Microsoft.Xna.Framework.FNAPlatform/PollEventsFunc", "Invoke"))) {
                throw new InvalidOperationException("Could not find `FNAPlatform.PollEvents` call in `FNA.Game`");
            }

            ILLabel jumpLabel = labelCursor.DefineLabel();
            labelCursor.MarkLabel(jumpLabel);
            
            cursor.Emit(OpCodes.Brfalse, jumpLabel);
        }

        public void Dispose() { // Use dispose pattern instead
            IL.Microsoft.Xna.Framework.Game.Tick -= Patch2;
        }
    }
    
    
    /// <summary>
    /// Only updates backdrops on frame updates, purely visual
    /// </summary>
    public class LessenBackdropUpdates : Optimizations.IOptimization {
        public bool Pure => false;
        public int Level => 2;
        public void Load() {
            On.Celeste.BackdropRenderer.Update += Patch;
        }

        public void Unload() {
            On.Celeste.BackdropRenderer.Update -= Patch;
        }

        private static void Patch(On.Celeste.BackdropRenderer.orig_Update orig, BackdropRenderer self, Scene scene) {
            if (!SpeedManipulator.IsDrawUpdate) return;
            orig(self, scene);
        }
    }

    /// <summary>
    /// Only updates animated tiles on frame updates, purely visual
    /// </summary>
    public class LessenAnimatedTilesUpdate : Optimizations.IOptimization {
        public bool Pure => false;
        public int Level => 2;
        public void Load() {
            On.Celeste.AnimatedTiles.Update += Patch;
        }

        public void Unload() {
            On.Celeste.AnimatedTiles.Update -= Patch;
        }

        private static void Patch(On.Celeste.AnimatedTiles.orig_Update orig, AnimatedTiles self) {
            if (!SpeedManipulator.IsDrawUpdate) return;
            orig(self);
        }
    }

    /// <summary>
    /// Only updates particles on frame updates, and only emits them on frame updates
    /// Purely visual
    /// </summary>
    public class LessenParticles : Optimizations.IOptimization {
        public bool Pure => false;
        public int Level => 3;

        public void Load() {
            On.Monocle.ParticleSystem.Update += Patch;
            On.Monocle.ParticleSystem.Add += LessenEmit;
            On.Monocle.ParticleSystem.Emit_ParticleType_Vector2 += LessenEmit;
            On.Monocle.ParticleSystem.Emit_ParticleType_Vector2_float += LessenEmit;
            On.Monocle.ParticleSystem.Emit_ParticleType_Vector2_Color += LessenEmit;
            On.Monocle.ParticleSystem.Emit_ParticleType_int_Vector2_Vector2 += LessenEmit;
            On.Monocle.ParticleSystem.Emit_ParticleType_Vector2_Color_float += LessenEmit;
            On.Monocle.ParticleSystem.Emit_ParticleType_int_Vector2_Vector2_float += LessenEmit;
            On.Monocle.ParticleSystem.Emit_ParticleType_int_Vector2_Vector2_Color += LessenEmit;
            On.Monocle.ParticleSystem.Emit_ParticleType_Entity_int_Vector2_Vector2_float += LessenEmit;
            On.Monocle.ParticleSystem.Emit_ParticleType_int_Vector2_Vector2_Color_float += LessenEmit;
        }

        public void Unload() {
            On.Monocle.ParticleSystem.Update -= Patch;
            On.Monocle.ParticleSystem.Add -= LessenEmit;
            On.Monocle.ParticleSystem.Emit_ParticleType_Vector2 -= LessenEmit;
            On.Monocle.ParticleSystem.Emit_ParticleType_Vector2_float -= LessenEmit;
            On.Monocle.ParticleSystem.Emit_ParticleType_Vector2_Color -= LessenEmit;
            On.Monocle.ParticleSystem.Emit_ParticleType_int_Vector2_Vector2 -= LessenEmit;
            On.Monocle.ParticleSystem.Emit_ParticleType_Vector2_Color_float -= LessenEmit;
            On.Monocle.ParticleSystem.Emit_ParticleType_int_Vector2_Vector2_float -= LessenEmit;
            On.Monocle.ParticleSystem.Emit_ParticleType_int_Vector2_Vector2_Color -= LessenEmit;
            On.Monocle.ParticleSystem.Emit_ParticleType_Entity_int_Vector2_Vector2_float -= LessenEmit;
            On.Monocle.ParticleSystem.Emit_ParticleType_int_Vector2_Vector2_Color_float -= LessenEmit;
        }

        private static void Patch(On.Monocle.ParticleSystem.orig_Update orig, ParticleSystem self) {
            if (!SpeedManipulator.IsDrawUpdate) return;
            orig(self);
        }
        
        // WARNING: The following code is not appropriate for all ages
        private static void LessenEmit(On.Monocle.ParticleSystem.orig_Add orig, ParticleSystem self, Particle particle) {
            if (!SpeedManipulator.IsDrawUpdate) return;
            orig(self, particle);
        }
        
        private static void LessenEmit(On.Monocle.ParticleSystem.orig_Emit_ParticleType_int_Vector2_Vector2_Color_float orig, ParticleSystem self, ParticleType type, int amount, Vector2 position, Vector2 positionrange, Color color, float direction) {
            if (!SpeedManipulator.IsDrawUpdate) return;
            orig(self, type, amount, position, positionrange, color, direction);
        }
        
        private static void LessenEmit(On.Monocle.ParticleSystem.orig_Emit_ParticleType_Entity_int_Vector2_Vector2_float orig, ParticleSystem self, ParticleType type, Entity track, int amount, Vector2 position, Vector2 positionrange, float direction) {
            if (!SpeedManipulator.IsDrawUpdate) return;
            orig(self, type, track, amount, position, positionrange, direction);
        }

        private static void LessenEmit(On.Monocle.ParticleSystem.orig_Emit_ParticleType_int_Vector2_Vector2_Color orig, ParticleSystem self, ParticleType type, int amount, Vector2 position, Vector2 positionrange, Color color) {
            if (!SpeedManipulator.IsDrawUpdate) return;
            orig(self, type, amount, position, positionrange, color);
        }

        private static void LessenEmit(On.Monocle.ParticleSystem.orig_Emit_ParticleType_int_Vector2_Vector2_float orig, ParticleSystem self, ParticleType type, int amount, Vector2 position, Vector2 positionrange, float direction) {
            if (!SpeedManipulator.IsDrawUpdate) return;
            orig(self, type, amount, position, positionrange, direction);
        }

        private static void LessenEmit(On.Monocle.ParticleSystem.orig_Emit_ParticleType_Vector2_Color_float orig, ParticleSystem self, ParticleType type, Vector2 position, Color color, float direction) {
            if (!SpeedManipulator.IsDrawUpdate) return;
            orig(self, type, position, color, direction);
        }

        private static void LessenEmit(On.Monocle.ParticleSystem.orig_Emit_ParticleType_int_Vector2_Vector2 orig, ParticleSystem self, ParticleType type, int amount, Vector2 position, Vector2 positionrange) {
            if (!SpeedManipulator.IsDrawUpdate) return;
            orig(self, type, amount, position, positionrange);
        }

        private static void LessenEmit(On.Monocle.ParticleSystem.orig_Emit_ParticleType_Vector2_Color orig, ParticleSystem self, ParticleType type, Vector2 position, Color color) {
            if (!SpeedManipulator.IsDrawUpdate) return;
            orig(self, type, position, color);
        }

        private static void LessenEmit(On.Monocle.ParticleSystem.orig_Emit_ParticleType_Vector2_float orig, ParticleSystem self, ParticleType type, Vector2 position, float direction) {
            if (!SpeedManipulator.IsDrawUpdate) return;
            orig(self, type, position, direction);
        }

        private static void LessenEmit(On.Monocle.ParticleSystem.orig_Emit_ParticleType_Vector2 orig, ParticleSystem self, ParticleType type, Vector2 position) {
            if (!SpeedManipulator.IsDrawUpdate) return;
            orig(self, type, position);
        }
    }
    
    /// <summary>
    /// Slows down the FrameworkDispatcher updates since that has no effect on the game loop
    /// </summary>
    public class LessenFrameworkDispatcherUpdates : Optimizations.IOptimization {
        public bool Pure => false;
        public int Level => 3;
        public void Load() {
            On.Microsoft.Xna.Framework.FrameworkDispatcher.Update += Patch;
        }

        public void Unload() {
            On.Microsoft.Xna.Framework.FrameworkDispatcher.Update -= Patch;
        }
        
        private static void Patch(On.Microsoft.Xna.Framework.FrameworkDispatcher.orig_Update orig) {
            if (!SpeedManipulator.IsDrawUpdate) return;
            orig();
        }
    }
    
    /// <summary>
    /// Only updates spinner hue on frame updates, purely visual
    /// </summary>
    public class SlowDownCrystalSpinnerHueUpdate : Optimizations.IOptimization {
        public bool Pure => false;
        public int Level => 2;
        public void Load() {
            On.Celeste.CrystalStaticSpinner.UpdateHue += Patch;
        }

        public void Unload() {
            On.Celeste.CrystalStaticSpinner.UpdateHue -= Patch;
        }

        private static void Patch(On.Celeste.CrystalStaticSpinner.orig_UpdateHue orig, CrystalStaticSpinner self) {
            if (!SpeedManipulator.IsDrawUpdate) return;
            orig(self);
        }
    }
    
    /// <summary>
    /// Only recalculates size on frame updates
    /// </summary>
    public class SlowDownMeasureBirdTutorialGui : Optimizations.IOptimization {
        public bool Pure => false;
        public int Level => 3;
        public void Load() {
            On.Celeste.BirdTutorialGui.UpdateControlsSize += Patch;
        }

        public void Unload() {
            On.Celeste.BirdTutorialGui.UpdateControlsSize -= Patch;
        }

        private static void Patch(On.Celeste.BirdTutorialGui.orig_UpdateControlsSize orig, BirdTutorialGui self) {
            if (!SpeedManipulator.IsDrawUpdate) return;
            orig(self);
        }
    }
    
    /// <summary>
    /// Updates the audio position on frame updates, keeping the latest assigned position
    /// </summary>
    public class OptimizeAudioPosition : Optimizations.IOptimization {
        public bool Pure => false;
        public int Level => 3;

        private static readonly Dictionary<EventInstance, Vector2> Positions = new();
        public void Load() {
            On.Celeste.Audio.Position += Patch;
            SpeedManipulator.DrawUpdate += Flush;
        }

        public void Unload() {
            On.Celeste.Audio.Position -= Patch;
            SpeedManipulator.DrawUpdate -= Flush;
        }
        
        private static void Patch(On.Celeste.Audio.orig_Position orig, EventInstance instance, Vector2 position) {
            Positions[instance] = position;
        }

        private static void Flush() {
            foreach (KeyValuePair<EventInstance, Vector2> pair in Positions) {
                Audio.Position(pair.Key, pair.Value);
            }
            Positions.Clear();
        }
    }
}