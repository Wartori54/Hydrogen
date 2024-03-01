using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.Hydrogen.Optimizations;

/// <summary>
/// Optimizations that have no side effects and are beneficial everywhere.
/// Currently none because apparently Celeste is not that slow (or the optimizations require changes on the game behavior)
/// </summary>
public static class PureOptimizations {
    public class PlayerUpdateChaserStates : Optimizations.IOptimization {
        public bool Pure => true;
        public int Level => 0;
        public void Load() {
            On.Celeste.Player.UpdateChaserStates += Patch;
        }

        public void Unload() {
            On.Celeste.Player.UpdateChaserStates -= Patch;
        }

        private static void Patch(On.Celeste.Player.orig_UpdateChaserStates orig, Player self) {
            // int i = 0;
            // foreach (Player.ChaserState chaserState in self.ChaserStates) {
            //     // if (self.Scene.TimeActive - (double)chaserState.TimeStamp > 4.0)
            //         i++;
            // }
            // Console.WriteLine($"{i} chaser states to remove");
            orig(self);
        }
    }
    
    public class GameUpdateArrayConstruction : Optimizations.IOptimization {
        public bool Pure => true;
        public int Level => 3;
        private Hook? hook;
        public void Load() {
            hook = new Hook(
                typeof(Game).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic) 
                ?? throw new InvalidOperationException("Cannot find `Update` in FNA `Game`"),
                Patch);
        }

        public void Unload() {
            hook?.Dispose();
        }

        private static DynamicData? dynData;
        private static List<IUpdateable>? comps;
        private static void Patch(Action<Game, GameTime> orig, Game game, GameTime gt) {
            dynData ??= DynamicData.For(game);
            comps ??= dynData.Get<List<IUpdateable>>("updateableComponents");
            if (comps != null) {
                foreach (IUpdateable comp in comps) {
                     if (comp.Enabled)
                         comp.Update(gt);
                }
                FrameworkDispatcher.Update();
            }
        }
    }
}