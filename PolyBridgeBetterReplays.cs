
using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using PolyTechFramework;
using BepInEx.Configuration;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using Vectrosity;
using UnityEngine.UI;

namespace PolyBridgeBetterReplays
{
    [BepInPlugin(pluginGuid, pluginName, pluginVerson)]
    [BepInDependency(PolyTechMain.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
    public class PolyBridgeBetterReplays: PolyTechMod
    {
        public const string pluginGuid = "polytech.betterreplays";
        public const string pluginName = "Better Replays Mod";
        public const string pluginVerson = "1.0.0";
        public static ConfigEntry<bool> mEnabled;
        public static ConfigEntry<bool> _unlimitedBitrate;
        public static ConfigEntry<int> _width;
        public static ConfigEntry<int> _height;
        public static ConfigEntry<int> _framerate;
        public static ConfigEntry<int> _bitrate;
        
        
        public static string repositoryUrl = "https://github.com/hippolippo/better_replays_mod/";

        public static Boolean ptfEnabled = true;

        public ConfigDefinition modEnableDef = new ConfigDefinition(pluginVerson, "Enable/Disable Mod");
        public ConfigDefinition unlimitedBitrateDef = new ConfigDefinition(pluginVerson, "Unlimited Bitrate");

        public static FieldInfo presets;

        public override void enableMod(){
            mEnabled.Value = true;
            this.isEnabled = true;
        }
        public override void disableMod(){
            mEnabled.Value = false;
            this.isEnabled = false;
        }
        public override string getSettings(){return "";}
        public override void setSettings(string settings){}
        
        public PolyBridgeBetterReplays(){
            Config.Bind(modEnableDef, true, new ConfigDescription("Controls if the mod should be enabled or disabled", null, new ConfigurationManagerAttributes { Order = 2 }));
            _unlimitedBitrate = Config.Bind(unlimitedBitrateDef, true, new ConfigDescription("Toggles unlimited bitrate", null, new ConfigurationManagerAttributes { Order = 1 }));
            _width = Config.Bind(new ConfigDefinition(pluginVerson, "Video Width"), 640, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 0 }));
            _height = Config.Bind(new ConfigDefinition(pluginVerson, "Video Height"), 360, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -1 }));
            _framerate = Config.Bind(new ConfigDefinition(pluginVerson, "Video Framerate (fps)"), 30, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -2 }));
            _bitrate = Config.Bind(new ConfigDefinition(pluginVerson, "Video Bitrate (Kbps)"), 512, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -3 }));
        }

        void Awake()
        {
            mEnabled = (ConfigEntry<bool>)Config[modEnableDef];
            this.isCheat = false;
            PolyTechMain.registerMod(this);
            Logger.LogInfo("Better Replays Registered.");
            Harmony.CreateAndPatchAll(typeof(PolyBridgeBetterReplays));
            Logger.LogInfo("Better Replays Methods Patched.");
            mEnabled.SettingChanged += onEnableDisable;
            this.isEnabled = mEnabled.Value;
        }
        public void onEnableDisable(object sender, EventArgs e)
        {
            this.isEnabled = mEnabled.Value;
        }
        [HarmonyPatch(typeof(AsyncCapture), "GetPresetForQuality")]
        [HarmonyPrefix]
        private static bool GetPresetForQuality(AsyncCaptureQuality quality, ref AsyncCapture __instance, ref AsyncCaptureQualityPreset __result){
            if(mEnabled.Value){
                AsyncCaptureQualityPreset custom = new AsyncCaptureQualityPreset();
                custom.m_Width = _width.Value;
                custom.m_Height = _height.Value;
                custom.m_Framerate = _framerate.Value;
                __result = custom;
                return false;
            }
            return true;
        }
        [HarmonyPatch(typeof(Panel_ShareReplay), "SaveVideo")]
        [HarmonyPrefix]
        private static bool SaveVideo(){
            if(mEnabled.Value){
                if(!_unlimitedBitrate.Value)
                Gallery.MAX_VIDEO_KBPS = _bitrate.Value;
                else
                Gallery.MAX_VIDEO_KBPS = 100000;
            }
            return true;
        }
        
    }
}
