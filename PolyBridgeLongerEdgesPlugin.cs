// BUGS: Moving points, just moving them in general, you will see what I mean
// The circle that the game draws around the materials is linked to materials and not the length. this somhow isn't an issue for placing but it is laggy for moving
// also ropes don't get a circle if you set them a max length

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

namespace LongerEdges
{
    [BepInPlugin(pluginGuid, pluginName, pluginVerson)]
    [BepInDependency(PolyTechMain.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
    public class PolyBridgeLongerEdges: PolyTechMod
    {
        public const string pluginGuid = "polytech.longeredges";
        public const string pluginName = "Longer Material Lengths";
        public const string pluginVerson = "1.0.0";
        public static ConfigEntry<bool> mEnabled;
        public static ConfigEntry<bool> _infiniteLength;
        public static ConfigEntry<float> _roadLen;
        public static ConfigEntry<float> _reinforcedRoadLen;
        public static ConfigEntry<float> _woodLen;
        public static ConfigEntry<float> _steelLen;
        public static ConfigEntry<float> _hydraulicLen;
        public static ConfigEntry<float> _ropeLen;
        public static ConfigEntry<float> _cableLen;
        public static ConfigEntry<float> _springLen;

        public ConfigDefinition modEnableDef = new ConfigDefinition(pluginVerson, "Enable/Disable Mod");
        public ConfigDefinition infiniteLengthDef = new ConfigDefinition(pluginVerson, "Infintite Material Lengths");

        public static MethodInfo CreateArcsMethod;
        public static MethodInfo PosInsideBoundaryMethod;
        public static MethodInfo PosSatisfiesAllContraintsMethod;
        public static int MaxSelectionCircleDots = 20945;

        public override void enableMod(){
            mEnabled.Value = true;
        }
        public override void disableMod(){
            mEnabled.Value = false;
        }
        public override string getSettings(){return "";}
        public override void setSettings(string settings){}
        
        public PolyBridgeLongerEdges(){
            Config.Bind(modEnableDef, true, new ConfigDescription("Controls if the mod should be enabled or disabled", null, new ConfigurationManagerAttributes { Order = 2 }));
            _infiniteLength = Config.Bind(infiniteLengthDef, true, new ConfigDescription("Toggles infinite material lengths", null, new ConfigurationManagerAttributes { Order = 1 }));
            _roadLen = Config.Bind(new ConfigDefinition(pluginVerson, "Road Length"), 2.0f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 0 }));
            _reinforcedRoadLen = Config.Bind(new ConfigDefinition(pluginVerson, "Reinforced Road Length"), 2.0f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -1 }));
            _woodLen = Config.Bind(new ConfigDefinition(pluginVerson, "Wood Length"), 2.02f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -2 }));
            _steelLen = Config.Bind(new ConfigDefinition(pluginVerson, "Steel Length"), 4.03f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -3 }));
            _hydraulicLen = Config.Bind(new ConfigDefinition(pluginVerson, "Hydraulic Length"), 4.03f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -4 }));
            _ropeLen = Config.Bind(new ConfigDefinition(pluginVerson, "Rope Length"), 1000000.0f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -5 }));
            _cableLen = Config.Bind(new ConfigDefinition(pluginVerson, "Cable Length"), 1000000.0f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -6 }));
            _springLen = Config.Bind(new ConfigDefinition(pluginVerson, "Spring Length"), 3.0f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = -7 }));
        }

        void Awake()
        {
            this.repositoryUrl = "https://github.com/hippolippo/longer_materials_mod/";
            mEnabled = (ConfigEntry<bool>)Config[modEnableDef];
            this.isCheat = true;
            PolyTechMain.registerMod(this);
            Logger.LogInfo("Longer Edges Registered.");
            CreateArcsMethod = typeof(BridgeJointMovement).GetMethod("CreateArcs", BindingFlags.NonPublic | BindingFlags.Static);
            PosInsideBoundaryMethod = typeof(BridgeJointMovement).GetMethod("PosInsideBoundary", BindingFlags.NonPublic | BindingFlags.Static);
            PosSatisfiesAllContraintsMethod = typeof(BridgeJointMovement).GetMethod("PosSatisfiesAllContraints", BindingFlags.NonPublic | BindingFlags.Static);
            Harmony.CreateAndPatchAll(typeof(PolyBridgeLongerEdges));
            Logger.LogInfo("Longer Edges Methods Patched.");
            Update();
            mEnabled.SettingChanged += onEnableDisable;
            this.isEnabled = mEnabled.Value;
        }
        public void onEnableDisable(object sender, EventArgs e)
        {
            this.isEnabled = mEnabled.Value;
        }

        private bool usingCheatLengths(){
            return _infiniteLength.Value || 
            _roadLen.Value > (float)_roadLen.DefaultValue || 
            _reinforcedRoadLen.Value > (float)_reinforcedRoadLen.DefaultValue ||
            _woodLen.Value > (float)_woodLen.DefaultValue || 
            _steelLen.Value > (float)_steelLen.DefaultValue || 
            _hydraulicLen.Value > (float)_hydraulicLen.DefaultValue || 
            _springLen.Value > (float)_springLen.DefaultValue;
        }

        void Update(){
            //Logger.LogInfo($"{isCheat} {usingCheatLengths()}");
            if (usingCheatLengths()){
                PolyTechMain.setCheat(this, true);
            }
            else {
                PolyTechMain.setCheat(this, false);
            }
            /*if (!mEnabled.Value){
                PolyTechMain.setCheat(this, false);
            }*/
        }
        /*
        [HarmonyPatch(typeof(BridgeMaterials), "GetMaxEdgeLength")]
        [HarmonyPrefix]
        private static bool giveMaxLength(BridgeMaterialType materialType, ref float __result){
            if(mEnabled.Value){
                if (_infiniteLength.Value){
                    Logger.LogInfo("1");
                    __result = 1000000.0f;
                    Logger.LogInfo("2");
                    return false; 
                }   
                return true;
            }else{
                return true;
            }
        }
        */
        [HarmonyPatch(typeof(BridgeMaterial), "HasUnlimitedLength")]
        [HarmonyPostfix]
        private static void HasUnlimitedLengthPatch(ref BridgeMaterial __instance, ref bool __result){
            if(mEnabled.Value){
                
                if (__instance.m_MaterialType == BridgeMaterialType.ROAD) __result = _roadLen.Value >= 1000;
                if (__instance.m_MaterialType == BridgeMaterialType.REINFORCED_ROAD) __result = _reinforcedRoadLen.Value >= 1000;
                if (__instance.m_MaterialType == BridgeMaterialType.WOOD) __result = _woodLen.Value >= 1000;
                if (__instance.m_MaterialType == BridgeMaterialType.STEEL) __result = _steelLen.Value >= 1000;
                if (__instance.m_MaterialType == BridgeMaterialType.HYDRAULICS) __result = _hydraulicLen.Value >= 1000;
                if (__instance.m_MaterialType == BridgeMaterialType.ROPE) __result = _ropeLen.Value >= 1000;
                if (__instance.m_MaterialType == BridgeMaterialType.CABLE) __result = _cableLen.Value >= 1000;
                if (__instance.m_MaterialType == BridgeMaterialType.SPRING) __result = _springLen.Value >= 1000;
                if (_infiniteLength.Value){
                    __result = true;
                }
            }
        }
        [HarmonyPatch(typeof(BridgeMaterials), "GetMaxEdgeLength")]
        [HarmonyPostfix]
        private static void giveMaxLength(BridgeMaterialType materialType, ref float __result){
            if(mEnabled.Value){
                if (materialType == BridgeMaterialType.ROAD && _roadLen.Value != (float)_roadLen.DefaultValue) __result = _roadLen.Value;
                if (materialType == BridgeMaterialType.REINFORCED_ROAD && _reinforcedRoadLen.Value != (float)_reinforcedRoadLen.DefaultValue) __result = _reinforcedRoadLen.Value;
                if (materialType == BridgeMaterialType.WOOD && _woodLen.Value != (float)_woodLen.DefaultValue) __result = _woodLen.Value;
                if (materialType == BridgeMaterialType.STEEL && _steelLen.Value != (float)_steelLen.DefaultValue) __result = _steelLen.Value;
                if (materialType == BridgeMaterialType.HYDRAULICS && _hydraulicLen.Value != (float)_hydraulicLen.DefaultValue) __result = _hydraulicLen.Value;
                if (materialType == BridgeMaterialType.ROPE && _ropeLen.Value != (float)_ropeLen.DefaultValue) __result = _ropeLen.Value;
                if (materialType == BridgeMaterialType.CABLE && _cableLen.Value != (float)_cableLen.DefaultValue) __result = _cableLen.Value;
                if (materialType == BridgeMaterialType.SPRING && _springLen.Value != (float)_springLen.DefaultValue) __result = _springLen.Value;
                if (_infiniteLength.Value){
                    __result = 1000000.0f;
                }
            }
        }
        [HarmonyPatch(typeof(BridgeJointMovement), "CreateMovementBoundary")]
        [HarmonyPrefix]
        private static bool createMovementBoundaryPatch(ref List<VectorLine> ___m_Arcs){
            
            for (int i = 0; i < ___m_Arcs.Count; i++)
		    {
		    	VectorLine vectorLine = ___m_Arcs[i];
		    	if (vectorLine != null)
		    	{
		    		VectorLine.Destroy(ref vectorLine);
		    	}
		    }
		    ___m_Arcs.Clear();
		    List<BridgeEdge> edgesConnectedToJoint = BridgeEdges.GetEdgesConnectedToJoint(BridgeJointMovement.m_SelectedJoint);
		    foreach (BridgeEdge bridgeEdge in edgesConnectedToJoint)
		    {
		    	if (!bridgeEdge.m_Material.HasUnlimitedLength())
		    	{
                    CreateArcsMethod.Invoke(null, new object[] { 
                        (bridgeEdge.m_JointA == BridgeJointMovement.m_SelectedJoint) ? bridgeEdge.m_JointB : bridgeEdge.m_JointA, 
                        bridgeEdge.GetMaxLength(), 
                        edgesConnectedToJoint 
                    });
		    	}
		    }
            return false;
        }

        [HarmonyPatch(typeof(BridgeJointMovement), "CalculateTargetPos")]
        [HarmonyPrefix]
        private static bool CalculateTargetPosPatch(Vector3 mouseWorldPos, BridgeJoint joint, List<BridgeEdge> edges, ref Vector3 __result){
            if ((bool)PosInsideBoundaryMethod.Invoke(null, new object[] { mouseWorldPos, joint, edges }))
		    {
		    	__result = mouseWorldPos;
                return false;
		    }
		    List<Vector3> list = new List<Vector3>();
		    foreach (BridgeEdge bridgeEdge in edges)
		    {
		    	if (!bridgeEdge.m_Material.HasUnlimitedLength())
		    	{
		    		BridgeJoint bridgeJoint = (bridgeEdge.m_JointA == joint) ? bridgeEdge.m_JointB : bridgeEdge.m_JointA;
		    		Vector3 normalized = (mouseWorldPos - bridgeJoint.transform.position).normalized;
		    		Vector3 vector = bridgeJoint.transform.position + normalized * bridgeEdge.GetMaxLength();
		    		Vector3 normalized2 = (bridgeJoint.transform.position - mouseWorldPos).normalized;
		    		Vector3 normalized3 = (joint.transform.position - mouseWorldPos).normalized;
		    		if (Vector3.Dot(normalized2, normalized3) >= 0f && (bool)PosSatisfiesAllContraintsMethod.Invoke(null, new object[] { vector, bridgeJoint, edges }))
		    		{
		    			list.Add(vector);
		    		}
		    	}
		    }
		    if (list.Count == 0)
		    {
		    	__result = mouseWorldPos;
                return false;
		    }
		    Vector3 result = list[0];
		    float num = float.MaxValue;
		    foreach (Vector3 vector2 in list)
		    {
		    	float num2 = Vector3.Distance(mouseWorldPos, vector2);
		    	if (num2 < num)
		    	{
		    		num = num2;
		    		result = vector2;
		    	}
		    }
		    __result = result;
            return false;
        }
        
        [HarmonyPatch(typeof(Panel_TraceTool), "GetFillSegmentLength")]
        [HarmonyPrefix]
        private static bool FillRangePatch(ref Panel_TraceTool __instance ,ref float __result){
            BridgeMaterial material = BridgeMaterials.GetMaterial(Bridge.m_BuildMaterialType);
	        float b = Mathf.Min(4f, BridgeMaterials.GetMaxEdgeLength(material.m_MaterialType));
	        __result = Mathf.Lerp(1f, b, Mathf.Clamp01(__instance.m_FillSlider.value / 100f));
            return false;
        }

        [HarmonyPatch(typeof(BridgeJointPlacement), "CreateSelectionCircleDots")]
        [HarmonyPrefix]
        private static bool CreateSelectionCircleDotsPatch(
            ref GameObject ___m_SelectionCircleDotsParent,
            ref List<GameObject> ___m_SelectionCircleDots
        ){
            if (!mEnabled.Value) return true;
            ___m_SelectionCircleDotsParent = new GameObject("SelectionCircleDotsParent");
	        ___m_SelectionCircleDotsParent.hideFlags = HideFlags.HideInHierarchy;
	        UnityEngine.Object.DontDestroyOnLoad(___m_SelectionCircleDotsParent);
	        ___m_SelectionCircleDotsParent.SetActive(false);
	        for (int i = 0; i < MaxSelectionCircleDots; i++)
	        {
	        	___m_SelectionCircleDots.Add(UnityEngine.Object.Instantiate<GameObject>(Prefabs.m_Instance.m_SelectionCircleDot));
	        	___m_SelectionCircleDots[i].transform.parent = ___m_SelectionCircleDotsParent.transform;
	        }
            return false;
        }

        [HarmonyPatch(typeof(BridgeJointPlacement), "PositionSelectionCircleDots")]
        [HarmonyPrefix]
        private static bool PositionSelectionCircleDotsPatch(
            float radius,
            ref List<GameObject> ___m_SelectionCircleDots
        ){
            if (!mEnabled.Value) return true;
            float num = 0.3f;
	        float num2 = 6.28318548f * radius;
	        float num3 = 360f * (num / num2);
	        int num4 = Mathf.RoundToInt(360f / num3);
	        float num5 = 0f;
            if (num4 > MaxSelectionCircleDots){
                return false;
            }
	        for (int i = 0; i < num4; i++)
	        {
	        	___m_SelectionCircleDots[i].SetActive(true);
	        	___m_SelectionCircleDots[i].transform.localPosition = new Vector3(radius * Mathf.Sin(0.0174532924f * num5), radius * Mathf.Cos(0.0174532924f * num5), 0f);
	        	num5 += num3;
	        }
	        for (int j = num4; j < MaxSelectionCircleDots; j++)
	        {
	        	___m_SelectionCircleDots[j].SetActive(false);
	        }
            return false;
        }
    }
}
