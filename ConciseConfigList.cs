using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace ConciseConfigList;

internal static class UIListExtension
{
    extension(UIElement element)
    {
        public void AppendAt(int index, UIElement item)
        {
            item.Remove();
            item.Parent = element;
            element.Elements.Insert(index, item);
            item.Recalculate();
        }
    }
    extension(UIList list)
    {
        public void Insert(int index, UIElement item)
        {
            list._items.Insert(index, item);
            list._innerList.AppendAt(index, item);
            list.UpdateOrder();
            list._innerList.Recalculate();
        }
    }
}

public class UIImageHover(Asset<Texture2D> texture) : UIImage(texture)
{
    public string HoverText { get; set; }
    public Color HoverColor { get; set; } = Color.White;
    public Color CommonColor { get; set; } = Color.White;
    public float HoverFactor { get; private set; }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        HoverFactor = MathHelper.Lerp(HoverFactor, IsMouseHovering ? 1 : 0, 0.1f);
        Color = Color.Lerp(CommonColor, HoverColor, HoverFactor);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        if (IsMouseHovering && HoverText is { } text)
            Main.instance.MouseText(text);
    }
}

// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
public class ConciseConfigList : Mod
{
    public override void Load()
    {
        if (typeof(UIModConfigList).GetMethod(nameof(UIModConfigList.PopulateMods), BindingFlags.NonPublic | BindingFlags.Instance) is { } populateMod)
            MonoModHooks.Add(populateMod, PopulateModModify);
        if (typeof(UIModConfigList).GetMethod(nameof(UIModConfigList.PopulateConfigs), BindingFlags.NonPublic | BindingFlags.Instance) is { } populateConfig)
            MonoModHooks.Add(populateConfig, PopulateConfigModify);

        On_UIElement.Draw += On_UIElement_Draw;
        base.Load();
    }

    private void On_UIElement_Draw(On_UIElement.orig_Draw orig, UIElement self, SpriteBatch spriteBatch)
    {
        orig.Invoke(self, spriteBatch);
        return;
        var dimension = self.GetDimensions();

        spriteBatch.Draw(TextureAssets.MagicPixel.Value, dimension.Position(), new Rectangle(0, 0, 1, 1), Color.White, 0, default, new Vector2(dimension.Width, 1), 0, 0);
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, dimension.Position(), new Rectangle(0, 0, 1, 1), Color.White, 0, default, new Vector2(1, dimension.Height), 0, 0);
    }

    private static Asset<Texture2D> GetIcon(Mod mod)
    {
        Asset<Texture2D> asset;
        if (mod.HasAsset("icon"))
        {
            asset = mod.Assets.Request<Texture2D>("icon", AssetRequestMode.ImmediateLoad);
            return asset;
        }
        return null;
    }

    private static Asset<Texture2D> GetSmallIcon(Mod mod, out bool isLargeIcon)
    {
        Asset<Texture2D> asset;
        if (mod.HasAsset("icon_small"))
        {
            asset = mod.Assets.Request<Texture2D>("icon_small", AssetRequestMode.ImmediateLoad);
            isLargeIcon = false;
            return asset;
        }
        else if (mod.HasAsset("icon"))
        {
            asset = mod.Assets.Request<Texture2D>("icon", AssetRequestMode.ImmediateLoad);
            isLargeIcon = true;
            return asset;
        }
        isLargeIcon = false;
        return null;
    }

    public static void PopulateModModify(Action<UIModConfigList> orig, UIModConfigList self)
    {
        switch (ConciseConfig.Instance.ModListMode)
        {
            case ModListMode.Vanilla:
                orig?.Invoke(self);
                return;

            case ModListMode.Icon:
                IconModeList(self);
                return;

            case ModListMode.SmallIcon:
                SmallIconModeList(self);
                return;
        }
    }

    public static void IconModeList(UIModConfigList self)
    {
        self.modList?.Clear();
        List<Mod> configMods = [];
        List<Mod> noConfigMods = [];
        foreach (var mod in ModLoader.Mods)
        {
            if (mod.Name == "ModLoader")
                continue;
            if (ConfigManager.Configs.ContainsKey(mod))
                configMods.Add(mod);
            else
                noConfigMods.Add(mod);
        }
        configMods.Sort((x, y) => x.DisplayNameClean.CompareTo(y.DisplayNameClean));
        if (ConciseConfig.Instance.ShowDespiteNoConfig)
            noConfigMods.Sort((x, y) => x.DisplayNameClean.CompareTo(y.DisplayNameClean));
        UIPanel currentContainer = null;
        int count = 0;
        int tier = 0;
        foreach (var mod in configMods)
        {
            if (currentContainer == null || count == 4)
            {
                count = 0;
                tier++;
                currentContainer = new()
                {
                    Width = StyleDimension.Fill,
                    Height = new(84, 0),
                    BackgroundColor = Color.Transparent,
                    BorderColor = Color.Transparent,
                    MarginTop = -2,
                    MarginBottom = -2
                };
                currentContainer.SetPadding(0);
                self.modList.Add(currentContainer);
            }

            var modPanel = new UIPanel()
            {
                Width = new(84, 0),
                Height = new(84, 0),
                HAlign = count / 3f,
                VAlign = .5f,
                BackgroundColor = Color.BlueViolet * .25f,
                BorderColor = Color.Transparent
            };
            modPanel.SetPadding(0);
            var modIcon = new UIImageHover(GetIcon(mod))
            {
                HAlign = .5f,
                VAlign = .5f,
                AllowResizingDimensions = false,
                ScaleToFit = true,
                Width = new(72f, 0),
                Height = new(72f, 0),
                HoverText = mod.DisplayName,
            };
            modIcon.OnUpdate += delegate
            {
                if (self.selectedMod == mod)
                {
                    modIcon.HoverColor = modIcon.CommonColor = Color.White;
                }
                else
                {
                    modIcon.HoverColor = Color.White * .85f;
                    modIcon.CommonColor = Color.White * .7f;
                }
            };
            modPanel.Append(modIcon);
            currentContainer.Append(modPanel);
            modIcon.OnLeftClick += delegate (UIMouseEvent evt, UIElement listeningElement)
            {
                self.selectedMod = mod;
                self.PopulateConfigs();
            };
            count++;
        }

        if (!ConciseConfig.Instance.ShowDespiteNoConfig) return;

        foreach (var mod in noConfigMods)
        {
            if (currentContainer == null || count == 4)
            {
                count = 0;
                tier++;
                currentContainer = new()
                {
                    Width = StyleDimension.Fill,
                    Height = new(84, 0),
                    BackgroundColor = Color.Transparent,
                    BorderColor = Color.Transparent,
                    MarginTop = -2,
                    MarginBottom = -2
                };
                currentContainer.SetPadding(0);
                self.modList.Add(currentContainer);
            }

            var modPanel = new UIPanel()
            {
                Width = new(84, 0),
                Height = new(84, 0),
                HAlign = count / 3f,
                VAlign = .5f,
                BackgroundColor = Color.Gray * .125f,
                BorderColor = Color.Transparent
            };
            modPanel.SetPadding(0);
            var modIcon = new UIImageHover(GetIcon(mod))
            {
                HAlign = .5f,
                VAlign = .5f,
                AllowResizingDimensions = false,
                ScaleToFit = true,
                Width = new(72f, 0),
                Height = new(72f, 0),
                HoverColor = Color.DarkGray,
                CommonColor = Color.DarkGray * .5f,
                HoverText = $"{mod.DisplayName} | {Language.GetTextValue("tModLoader.ModConfigModLoaderButNoConfigs")}",
            };
            modPanel.Append(modIcon);
            currentContainer.Append(modPanel);
            count++;
        }
    }

    public static void SmallIconModeList(UIModConfigList self)
    {
        self.modList?.Clear();
        List<Mod> configMods = [];
        List<Mod> noConfigMods = [];
        foreach (var mod in ModLoader.Mods)
        {
            if (mod.Name == "ModLoader")
                continue;
            if (ConfigManager.Configs.ContainsKey(mod))
                configMods.Add(mod);
            else
                noConfigMods.Add(mod);
        }
        configMods.Sort((x, y) => x.DisplayNameClean.CompareTo(y.DisplayNameClean));
        if (ConciseConfig.Instance.ShowDespiteNoConfig)
            noConfigMods.Sort((x, y) => x.DisplayNameClean.CompareTo(y.DisplayNameClean));
        UIPanel currentContainer = null;
        int count = 0;
        int tier = 0;
        foreach (var mod in configMods)
        {
            if (currentContainer == null || count == 8)
            {
                count = 0;
                tier++;
                currentContainer = new()
                {
                    Width = StyleDimension.Fill,
                    Height = new(42, 0),
                    BackgroundColor = Color.Transparent,
                    BorderColor = Color.Transparent,
                    MarginTop = -2,
                    MarginBottom = -2
                };
                currentContainer.SetPadding(0);
                self.modList.Add(currentContainer);
            }

            var modPanel = new UIPanel()
            {
                Width = new(42, 0),
                Height = new(42, 0),
                HAlign = count / 7f,
                VAlign = .5f,
                BackgroundColor = Color.BlueViolet * .25f,
                BorderColor = Color.Transparent
            };
            modPanel.SetPadding(0);
            var modIcon = new UIImageHover(GetSmallIcon(mod, out var isLargeIcon))
            {
                HAlign = .5f,
                VAlign = .5f,
                AllowResizingDimensions = false,
                ScaleToFit = true,
                Width = new(30f, 0),
                Height = new(30f, 0),
                HoverText = mod.DisplayName,
            };
            modIcon.OnUpdate += delegate
            {
                if (self.selectedMod == mod)
                {
                    modIcon.HoverColor = modIcon.CommonColor = Color.White;
                }
                else
                {
                    modIcon.HoverColor = Color.White * .75f;
                    modIcon.CommonColor = Color.White * .5f;
                }
            };
            modPanel.Append(modIcon);
            currentContainer.Append(modPanel);
            modIcon.OnLeftClick += delegate (UIMouseEvent evt, UIElement listeningElement)
            {
                self.selectedMod = mod;
                self.PopulateConfigs();
            };
            count++;
        }

        if (!ConciseConfig.Instance.ShowDespiteNoConfig) return;

        foreach (var mod in noConfigMods)
        {
            if (currentContainer == null || count == 8)
            {
                count = 0;
                tier++;
                currentContainer = new()
                {
                    Width = StyleDimension.Fill,
                    Height = new(42, 0),
                    BackgroundColor = Color.Transparent,
                    BorderColor = Color.Transparent,
                    MarginTop = -2,
                    MarginBottom = -2
                };
                currentContainer.SetPadding(0);
                self.modList.Add(currentContainer);
            }

            var modPanel = new UIPanel()
            {
                Width = new(42, 0),
                Height = new(42, 0),
                HAlign = count / 7f,
                VAlign = .5f,
                BackgroundColor = Color.Gray * .125f,
                BorderColor = Color.Transparent,
            };
            modPanel.SetPadding(0);
            var modIcon = new UIImageHover(GetSmallIcon(mod, out var isLargeIcon))
            {
                HAlign = .5f,
                VAlign = .5f,
                AllowResizingDimensions = false,
                ScaleToFit = true,
                Width = new(30f, 0),
                Height = new(30f, 0),
                HoverColor = Color.Gray,
                CommonColor = Color.Gray * .5f,
                HoverText = $"{mod.DisplayName} | {Language.GetTextValue("tModLoader.ModConfigModLoaderButNoConfigs")}",
            };
            modPanel.Append(modIcon);
            currentContainer.Append(modPanel);
            count++;
        }
    }

    public static void PopulateConfigModify(Action<UIModConfigList> orig, UIModConfigList self)
    {
        orig?.Invoke(self);

        if (!ConciseConfig.Instance.UseConciseConfigList) return;
        foreach (var panel in self.configList)
        {
            if (panel is UIButton<LocalizedText> button)
            {
                button.HAlign = 0f;
                button.MarginTop = -4f;
                button.MarginBottom = -4f;
                button.MinWidth = new(0, 1);
                button.BorderColor = Color.Transparent;
                button._borderColor = Color.Transparent;
                button.HoverBorderColor = Color.Transparent;
                button.TextOriginX = 0;
                button.MaxWidth = StyleDimension.Fill;
            }
        }

        int count = self.configList.Count;
        for (int n = 0; n <= count; n++)
            self.configList.Insert(n * 2, CreateSpearator());
    }

    public static UIHorizontalSeparator CreateSpearator() => new() { Width = new(0, 1), Height = new(4, 0), Color = UICommon.DefaultUIBlue };
}