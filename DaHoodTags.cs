/*
    Da Hood Gang Tags & Skinning
    - Premium Glass UI with background blur
    - Integrated Gang Management (Pirus, Vagos, Surenos, Disciples)
    - Automatic Item Renaming & Skinning
    - Individual Spray Image Previews in UI
    - Admin Testing & Management Commands
*/

using System.Collections.Generic;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("DaHoodTags", "Gemini", "1.4.0")]
    [Description("Polished gang tag system for Da Hood with premium Glass UI and Spray Previews")]
    public class DaHoodTags : RustPlugin
    {
        private const string permAdmin = "dahoodtags.admin";
        
        private enum GangType { None, Pirus, Vagos, Surenos, Disciples }

        private class SprayOption
        {
            public string Name;
            public ulong SkinID;
            public string ImageUrl;
        }

        private class GangConfig
        {
            public string Name;
            public string Color;      // RGBA format for UI accents
            public string ChatPrefix; // Color tag for chat
            public string Permission;
            public string LogoUrl;    // URL to the gang crest
            public List<SprayOption> Sprays;
        }

        private Dictionary<GangType, GangConfig> _gangs = new Dictionary<GangType, GangConfig>
        {
            [GangType.Pirus] = new GangConfig {
                Name = "WESTSIDE PIRUS", 
                Color = "0.7 0.1 0.1 0.85", ChatPrefix = "#FF3131", 
                Permission = "dahood.pirus",
                LogoUrl = "https://i.imgur.com/example_pirus_logo.png",
                Sprays = new List<SprayOption> {
                    new SprayOption { Name = "Piru Tag", SkinID = 3639835623, ImageUrl = "https://i.imgur.com/vupaESp.png" },
                    new SprayOption { Name = "Block Mural", SkinID = 3639835623, ImageUrl = "https://i.imgur.com/vupaESp.png" },
                    new SprayOption { Name = "Piru Emblem", SkinID = 3639835623, ImageUrl = "https://i.imgur.com/vupaESp.png" }
                }
            },
            [GangType.Vagos] = new GangConfig {
                Name = "NORTHSIDE VAGOS", 
                Color = "0.9 0.8 0.1 0.85", ChatPrefix = "#FFFF00",
                Permission = "dahood.vagos",
                LogoUrl = "https://i.imgur.com/example_vagos_logo.png",
                Sprays = new List<SprayOption> {
                    new SprayOption { Name = "Vago Tag", SkinID = 201, ImageUrl = "https://i.imgur.com/tag2.png" },
                    new SprayOption { Name = "Yellow Pride", SkinID = 202, ImageUrl = "https://i.imgur.com/mural2.png" },
                    new SprayOption { Name = "Vago Crest", SkinID = 203, ImageUrl = "https://i.imgur.com/emblem2.png" }
                }
            },
            [GangType.Surenos] = new GangConfig {
                Name = "SOUTHSIDE SUREÑOS", 
                Color = "0.1 0.2 0.7 0.85", ChatPrefix = "#3131FF",
                Permission = "dahood.surenos",
                LogoUrl = "https://i.imgur.com/example_surenos_logo.png",
                Sprays = new List<SprayOption> {
                    new SprayOption { Name = "Blue Tag", SkinID = 301, ImageUrl = "https://i.imgur.com/tag3.png" },
                    new SprayOption { Name = "South Side", SkinID = 302, ImageUrl = "https://i.imgur.com/mural3.png" },
                    new SprayOption { Name = "13 Crest", SkinID = 303, ImageUrl = "https://i.imgur.com/emblem3.png" }
                }
            },
            [GangType.Disciples] = new GangConfig {
                Name = "EASTSIDE DISCIPLES", 
                Color = "0.2 0.2 0.2 0.95", ChatPrefix = "#808080",
                Permission = "dahood.disciples",
                LogoUrl = "https://i.imgur.com/example_disciples_logo.png",
                Sprays = new List<SprayOption> {
                    new SprayOption { Name = "Void Tag", SkinID = 401, ImageUrl = "https://i.imgur.com/tag4.png" },
                    new SprayOption { Name = "East Mural", SkinID = 402, ImageUrl = "https://i.imgur.com/mural4.png" },
                    new SprayOption { Name = "Disciple Star", SkinID = 403, ImageUrl = "https://i.imgur.com/emblem4.png" }
                }
            }
        };

        #region Oxide Hooks
        private void Init()
        {
            permission.RegisterPermission(permAdmin, this);
            foreach (var gang in _gangs.Values) permission.RegisterPermission(gang.Permission, this);
        }

        private object OnSprayCreate(SprayCan sc, Vector3 vector, Quaternion quaternion)
        {
            if (sc.skinID != 0)
            {
                BaseEntity entity = GameManager.server.CreateEntity(sc.SprayDecalEntityRef.resourcePath, vector, quaternion, true);
                entity.skinID = sc.skinID;
                entity.OnDeployed(null, sc.GetOwnerPlayer(), sc.GetItem());
                entity.Spawn();
                sc.GetItem().LoseCondition(sc.ConditionLossPerSpray);
                return false;
            }
            return null;
        }
        #endregion

        #region UI Implementation
        [ChatCommand("tag")]
        private void CmdTag(BasePlayer player)
        {
            GangType gang = GetPlayerGang(player);
            if (gang == GangType.None && !permission.UserHasPermission(player.UserIDString, permAdmin))
            {
                player.ChatMessage("<color=#ff0000>[Da Hood]</color> You must belong to a gang to use this.");
                return;
            }
            if (gang == GangType.None) gang = GangType.Pirus;
            OpenTagMenu(player, gang);
        }

        private void OpenTagMenu(BasePlayer player, GangType gangType)
        {
            CuiHelper.DestroyUi(player, "TagMenuBg");
            var config = _gangs[gangType];
            var container = new CuiElementContainer();

            // Background Overlay
            container.Add(new CuiPanel {
                Image = { Color = "0 0 0 0.8" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                CursorEnabled = true
            }, "Overlay", "TagMenuBg");

            container.Add(new CuiButton {
                Button = { Command = "tag.close", Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
            }, "TagMenuBg");

            // Main Glass Content Panel
            container.Add(new CuiPanel {
                Image = { Color = "0 0 0 0.95", Material = "assets/content/ui/uibackgroundblur-runway.mat" },
                RectTransform = { AnchorMin = "0.15 0.2", AnchorMax = "0.85 0.8" }
            }, "TagMenuBg", "TagMenu");

            // Header Accent Line
            container.Add(new CuiPanel {
                Image = { Color = config.Color },
                RectTransform = { AnchorMin = "0 0.98", AnchorMax = "1 1" }
            }, "TagMenu");

            // Gang Logo (Top Center)
            container.Add(new CuiElement {
                Parent = "TagMenu",
                Components = {
                    new CuiRawImageComponent { Url = config.LogoUrl, Sprite = "assets/content/textures/generic/fullwhite.tga" },
                    new CuiRectTransformComponent { AnchorMin = "0.44 0.82", AnchorMax = "0.56 0.96" }
                }
            });

            // Gang Title
            container.Add(new CuiLabel {
                Text = { Text = config.Name, FontSize = 24, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0 0.72", AnchorMax = "1 0.82" }
            }, "TagMenu");

            // Action Buttons with Image Previews
            int i = 0;
            foreach (var spray in config.Sprays)
            {
                float xMin = 0.05f + (i * 0.31f);
                float xMax = xMin + 0.28f;

                string btnName = $"btn_{i}";
                
                // Button Background
                container.Add(new CuiButton {
                    Button = { Command = $"tag.select {spray.SkinID} \"{spray.Name}\"", Color = "0.15 0.15 0.15 0.8", Material = "assets/content/ui/uibackgroundblur-runway.mat" },
                    RectTransform = { AnchorMin = $"{xMin} 0.1", AnchorMax = $"{xMax} 0.65" },
                    Text = { Text = "" } // Label handled separately
                }, "TagMenu", btnName);

                // Spray Preview Image
                container.Add(new CuiElement {
                    Parent = btnName,
                    Components = {
                        new CuiRawImageComponent { Url = spray.ImageUrl, Sprite = "assets/content/textures/generic/fullwhite.tga" },
                        new CuiRectTransformComponent { AnchorMin = "0.1 0.35", AnchorMax = "0.9 0.9" }
                    }
                });

                // Spray Name Label
                container.Add(new CuiLabel {
                    Text = { Text = $"<b>{spray.Name.ToUpper()}</b>", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "0.9 0.9 0.9 1" },
                    RectTransform = { AnchorMin = "0 0.1", AnchorMax = "1 0.3" }
                }, btnName);

                // Button Glow Underline
                container.Add(new CuiPanel {
                    Image = { Color = config.Color },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.04" }
                }, btnName);

                i++;
            }

            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("tag.select")]
        private void CmdSelect(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || arg.Args == null || arg.Args.Length < 2) return;

            ulong skinID;
            if (!ulong.TryParse(arg.Args[0], out skinID)) return;
            string tagName = arg.Args[1];

            Item item = ItemManager.CreateByItemID(-596876839, 1, skinID);
            item.name = $"{tagName} Spray Can";
            player.GiveItem(item);
            
            CuiHelper.DestroyUi(player, "TagMenuBg");

            GangType gangType = GetPlayerGang(player);
            string colorCode = gangType != GangType.None ? _gangs[gangType].ChatPrefix : "#ffffff";
            player.ChatMessage($"<color={colorCode}>[Da Hood]</color> You have received the <b>{tagName}</b>.");
        }

        [ConsoleCommand("tag.close")]
        private void CmdClose(ConsoleSystem.Arg arg) => CuiHelper.DestroyUi(arg.Player(), "TagMenuBg");
        #endregion

        #region Helpers & Admin
        private GangType GetPlayerGang(BasePlayer player)
        {
            foreach (var gang in _gangs)
                if (permission.UserHasPermission(player.UserIDString, gang.Value.Permission)) return gang.Key;
            return GangType.None;
        }

        [ChatCommand("tagtest")]
        private void CmdTagTest(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, permAdmin)) return;
            if (args.Length == 0) {
                player.ChatMessage("Usage: /tagtest <pirus|vagos|surenos|disciples>");
                return;
            }

            switch (args[0].ToLower())
            {
                case "pirus": OpenTagMenu(player, GangType.Pirus); break;
                case "vagos": OpenTagMenu(player, GangType.Vagos); break;
                case "surenos": OpenTagMenu(player, GangType.Surenos); break;
                case "disciples": OpenTagMenu(player, GangType.Disciples); break;
                default: player.ChatMessage("Invalid Gang Name."); break;
            }
        }
        #endregion
    }
}