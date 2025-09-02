using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace ConciseConfigList;

public enum ModListMode
{
    Vanilla = 0,
    Icon = 1,
    SmallIcon = 2
}

public class ConciseConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    [DefaultValue(false)]
    public bool ShowDespiteNoConfig { get; set; } = false;

    [DefaultValue(true)]
    public bool UseConciseConfigList { get; set; } = true;

    [DefaultValue(ModListMode.Icon)]
    [DrawTicks]
    public ModListMode ModListMode { get; set; } = ModListMode.Icon;

    public static ConciseConfig Instance { get; private set; }

    public override void OnLoaded()
    {
        Instance = this;
        base.OnLoaded();
    }
}