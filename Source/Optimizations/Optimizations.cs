using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.Hydrogen.Optimizations;

public static class Optimizations {
    public interface IOptimization {
        public bool Pure { get; }
        public int Level { get; }
        public void Load();
        public void Unload();
    }

    public static readonly List<IOptimization> AllOptimizations = [
        new NonPureOptimizations.SlowDownAudioUpdate(), 
        new NonPureOptimizations.SlowDownAutoSplitterUpdate(), 
        new NonPureOptimizations.SlowDownPollEvents(),
        new NonPureOptimizations.SlowDownDiscordSDK(),
        new NonPureOptimizations.SlowDownMInputUpdate(),
        new NonPureOptimizations.LessenBackdropUpdates(),
        new NonPureOptimizations.LessenAnimatedTilesUpdate(),
        new NonPureOptimizations.LessenParticles(),
        new NonPureOptimizations.LessenFrameworkDispatcherUpdates(),
        new NonPureOptimizations.SlowDownCrystalSpinnerHueUpdate(),
        new NonPureOptimizations.SlowDownMeasureBirdTutorialGui(),
        new NonPureOptimizations.OptimizeAudioPosition(),
        // new PureOptimizations.PlayerUpdateChaserStates(),
        // new PureOptimizations.GameUpdateArrayConstruction(),
    ];
    
    private static readonly HashSet<IOptimization> CurrentOptimizations = [];

    public static void SwitchTo(int level, bool loadOnlyPure = true) {
        // Remove old
        foreach (IOptimization optimization in CurrentOptimizations) {
            if (optimization.Level <= level && (!loadOnlyPure || optimization.Pure)) continue;
            optimization.Unload();
            CurrentOptimizations.Remove(optimization);
        }
        
        // Apply new
        foreach (IOptimization optimization in AllOptimizations) {
            if (CurrentOptimizations.Contains(optimization)) continue;
            if (optimization.Level > level || (loadOnlyPure && !optimization.Pure)) continue;
            optimization.Load();
            CurrentOptimizations.Add(optimization);
        }
    }

    public static int GetMaxLevel() {
        return AllOptimizations.Select(optimization => optimization.Level).Prepend(0).Max();
    }
    

}