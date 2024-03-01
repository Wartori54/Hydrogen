using System;
using System.Reflection;
using Celeste.Mod.Hydrogen.OSD;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.Hydrogen {
    public class HydrogenModule : EverestModule {
        public static HydrogenModule? Instance { get; private set; }

        public override Type SettingsType => typeof(HydrogenModuleSettings);
        public static HydrogenModuleSettings Settings => (HydrogenModuleSettings?) Instance?._Settings ?? throw new InvalidOperationException();

        public override Type SessionType => typeof(HydrogenModuleSession);
        public static HydrogenModuleSession Session => (HydrogenModuleSession?) Instance?._Session ?? throw new InvalidOperationException();

        public override Type SaveDataType => typeof(HydrogenModuleSaveData);
        public static HydrogenModuleSaveData SaveData => (HydrogenModuleSaveData?) Instance?._SaveData ?? throw new InvalidOperationException();

        public static Hook? GameBeginDrawHook;

        public HydrogenModule() {
            Instance = this;
#if DEBUG
            // debug builds use verbose logging
            Logger.SetLogLevel(nameof(HydrogenModule), LogLevel.Verbose);
#else
            // release builds use info logging to reduce spam in log files
            Logger.SetLogLevel(nameof(HydrogenModule), LogLevel.Info);
#endif
        }

        public override void Load() {
            // TODO: apply any hooks that should always be active

            On.Celeste.Celeste.Update += SpeedManipulator.Celeste_Update;

            GameBeginDrawHook = new Hook(typeof(Game).GetMethod("BeginDraw", 
                                             BindingFlags.Instance | BindingFlags.NonPublic) 
                                         ?? throw new Exception("Could not find FNA.Game method `BeginDraw`"),
                SpeedManipulator.Game_BeginDraw);
            GameBeginDrawHook.Apply();

            IL.Celeste.LevelLoader.LoadingThread += PerfStatsOSD.hook_LevelLoader_LoadingThread;
            
            Optimizations.Optimizations.SwitchTo(Settings.OptimizationLevel, Settings.OnlyPure);
        }

        public override void Unload() {
            // TODO: unapply any hooks applied in Load()
            On.Celeste.Celeste.Update -= SpeedManipulator.Celeste_Update;
            GameBeginDrawHook?.Dispose();
            IL.Celeste.LevelLoader.LoadingThread -= PerfStatsOSD.hook_LevelLoader_LoadingThread;
            Optimizations.Optimizations.SwitchTo(-1); // this disables everthing
        }

        public static void LogAllInstrs(ILCursor il) {
            Console.WriteLine("Logging instructions");
            Console.WriteLine("In method " + il.Method.FullName);
            foreach (Instruction? instr in il.Instrs) {
                try {
                    Console.WriteLine(instr.ToString());
                }
                catch (InvalidCastException ex) {
                    Console.WriteLine("Unknown instr");
                }
            }
            Console.WriteLine("Logging instructions end");
        }
    }
}
