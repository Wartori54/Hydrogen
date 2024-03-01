using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.Hydrogen {
    public class HydrogenModuleSettings : EverestModuleSettings {

        [SettingIgnore]
        public bool UncappedSpeed { get; set; } = false;
        
        [DefaultButtonBinding(0, Keys.RightControl)]
        public ButtonBinding ToggleSpeed { get; set; }

        private bool _onlyPure = true;
        public bool OnlyPure {
            get => _onlyPure;
            set {
                _onlyPure = value;
                Optimizations.Optimizations.SwitchTo(OptimizationLevel, _onlyPure);
            }
        }

        private int _optimizationLevel = -1;
        
        [SettingRange(-1, 3)]
        public int OptimizationLevel {
            get => _optimizationLevel;
            set {
                _optimizationLevel = value;
                Optimizations.Optimizations.SwitchTo(_optimizationLevel, OnlyPure);
            }
        }
        
        public bool EnableOSD { get; set; }
    }
}
