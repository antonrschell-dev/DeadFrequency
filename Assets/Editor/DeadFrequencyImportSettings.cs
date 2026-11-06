using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Dead Frequency - one-click import settings pass.
// Source of truth: "README - Unity Project Setup.md" step 4 + "Audio Event Map.csv" (Load Type column).
// Textures: Albedo = sRGB on; Normal = Normal Map type; Roughness/AO/Height/Metallic = sRGB off (linear data maps).
// Models: Scale Factor 1, no animation import (kit pieces).
// Audio: Load Type per asset ID (DF_XXX_NNN) from the Audio Event Map.
public static class DeadFrequencyImportSettings
{
    const string StreamingIds = "DF_AMB_001,DF_AMB_002,DF_AMB_003,DF_AMB_004,DF_AMB_005,DF_AMB_006,DF_AMB_007,DF_AMB_008,DF_AMB_009,DF_AMB_010,DF_AMB_011,DF_AMB_012,DF_AMB_013,DF_AMB_014,DF_AMB_015,DF_AMB_016,DF_AMB_017,DF_AMB_018,DF_AMB_019,DF_AMB_020,DF_AMB_021,DF_AMB_022,DF_AMB_023,DF_AMB_024,DF_AMB_025,DF_AMB_026,DF_AMB_027,DF_AMB_028,DF_AMB_029,DF_AMB_030,DF_AMB_031,DF_AMB_032,DF_AMB_033,DF_AMB_034,DF_AMB_035,DF_AMB_036,DF_AMB_037,DF_AMB_038,DF_AMB_039,DF_AMB_040,DF_END_001,DF_END_002,DF_END_003,DF_END_004,DF_END_005,DF_END_006,DF_END_007,DF_END_008,DF_END_009,DF_END_010,DF_END_011,DF_END_012,DF_END_013,DF_END_014,DF_END_015,DF_MCH_001,DF_MCH_002,DF_MCH_003,DF_MCH_004,DF_MCH_005,DF_MCH_006,DF_MCH_007,DF_MCH_008,DF_MCH_009,DF_MCH_010,DF_MCH_011,DF_MCH_012,DF_MCH_013,DF_MCH_014,DF_MCH_015,DF_MCH_016,DF_MCH_017,DF_MCH_018,DF_MCH_019,DF_MCH_020,DF_MCH_021,DF_MCH_022,DF_MCH_023,DF_MCH_024,DF_MCH_025,DF_MCH_026,DF_MCH_027,DF_MCH_028,DF_MCH_029,DF_MCH_030,DF_MCH_031,DF_MCH_032,DF_MCH_033,DF_MCH_034,DF_MCH_035,DF_MUS_001,DF_MUS_002,DF_MUS_003,DF_MUS_004,DF_MUS_005,DF_MUS_006,DF_MUS_007,DF_MUS_008,DF_MUS_009,DF_MUS_010,DF_MUS_011,DF_MUS_012,DF_MUS_013,DF_MUS_014,DF_MUS_015,DF_MUS_016,DF_MUS_017,DF_MUS_018,DF_MUS_019,DF_MUS_020,DF_STM_001,DF_STM_002,DF_STM_003,DF_STM_004,DF_STM_005,DF_STM_006,DF_STM_007,DF_STM_008,DF_STM_009,DF_STM_010,DF_STM_011,DF_STM_012,DF_STM_013,DF_STM_014,DF_STM_015,DF_VNT_001,DF_VNT_002,DF_VNT_003,DF_VNT_004,DF_VNT_005,DF_VNT_006,DF_VNT_007,DF_VNT_008,DF_VNT_009,DF_VNT_010,DF_VNT_011,DF_VNT_012,DF_VNT_013,DF_VNT_014,DF_VNT_015,DF_VOX_001,DF_VOX_006,DF_VOX_015";
    const string DecompressIds = "DF_DOR_001,DF_DOR_002,DF_DOR_008,DF_DOR_010,DF_DOR_011,DF_DOR_012,DF_DOR_013,DF_DOR_014,DF_DOR_015,DF_DOR_016,DF_DOR_017,DF_DOR_018,DF_DOR_019,DF_DOR_021,DF_DOR_023,DF_DOR_026,DF_DOR_027,DF_ELE_004,DF_ELE_005,DF_ELE_006,DF_ELE_007,DF_ELE_012,DF_ELE_018,DF_ELE_019,DF_ELE_021,DF_ELE_024,DF_EQP_001,DF_EQP_002,DF_EQP_003,DF_EQP_004,DF_EQP_005,DF_EQP_006,DF_EQP_007,DF_EQP_008,DF_EQP_009,DF_EQP_010,DF_EQP_011,DF_EQP_012,DF_EQP_013,DF_EQP_014,DF_EQP_015,DF_EQP_016,DF_EQP_017,DF_EQP_018,DF_EQP_019,DF_EQP_020,DF_EQP_021,DF_EQP_022,DF_EQP_023,DF_EQP_024,DF_EQP_025,DF_FDB_001,DF_FDB_004,DF_FDB_005,DF_FDB_010,DF_FDB_011,DF_FDB_012,DF_FDB_013,DF_FDB_014,DF_FOOT_023,DF_FOOT_024,DF_FOOT_025,DF_FOOT_026,DF_FOOT_030,DF_INT_002,DF_INT_006,DF_INT_008,DF_INT_009,DF_INT_010,DF_INT_011,DF_INT_013,DF_INT_014,DF_INT_015,DF_INT_016,DF_INT_025,DF_OPS_003,DF_OPS_004,DF_OPS_005,DF_OPS_007,DF_OPS_008,DF_OPS_009,DF_OPS_011,DF_OPS_012,DF_OPS_013,DF_OPS_014,DF_OPS_015,DF_OPS_016,DF_OPS_018,DF_RAD_015,DF_RAD_017,DF_RAD_018,DF_RAD_020,DF_RAD_021,DF_RAD_022,DF_RAD_023,DF_RAD_024,DF_RAD_025,DF_RAD_026,DF_RAD_027,DF_RAD_028,DF_RAD_029,DF_RAD_030,DF_RAD_031,DF_RAD_032,DF_RAD_033,DF_RAD_034,DF_UI_001,DF_UI_002,DF_UI_003,DF_UI_004,DF_UI_005,DF_UI_006,DF_UI_007,DF_UI_008,DF_UI_009,DF_UI_010,DF_UI_011,DF_UI_012,DF_UI_013,DF_UI_014,DF_UI_015,DF_UI_016,DF_UI_017,DF_UI_018,DF_UI_019,DF_UI_020";
    const string CompressedIds = "DF_ALM_001,DF_ALM_002,DF_ALM_003,DF_ALM_004,DF_ALM_005,DF_ALM_006,DF_ALM_007,DF_ALM_008,DF_ALM_009,DF_ALM_010,DF_ALM_011,DF_ALM_012,DF_ALM_013,DF_ALM_014,DF_ALM_015,DF_ALM_016,DF_ALM_017,DF_ALM_018,DF_ALM_019,DF_ALM_020,DF_DOR_003,DF_DOR_004,DF_DOR_005,DF_DOR_006,DF_DOR_007,DF_DOR_009,DF_DOR_020,DF_DOR_022,DF_DOR_024,DF_DOR_025,DF_DOR_028,DF_DOR_029,DF_DOR_030,DF_ELE_001,DF_ELE_002,DF_ELE_003,DF_ELE_008,DF_ELE_009,DF_ELE_010,DF_ELE_011,DF_ELE_013,DF_ELE_014,DF_ELE_015,DF_ELE_016,DF_ELE_017,DF_ELE_020,DF_ELE_022,DF_ELE_023,DF_ELE_025,DF_ENT_001,DF_ENT_002,DF_ENT_003,DF_ENT_004,DF_ENT_005,DF_ENT_006,DF_ENT_007,DF_ENT_008,DF_ENT_009,DF_ENT_010,DF_ENT_011,DF_ENT_012,DF_ENT_013,DF_ENT_014,DF_ENT_015,DF_FDB_002,DF_FDB_003,DF_FDB_006,DF_FDB_007,DF_FDB_008,DF_FDB_009,DF_FDB_015,DF_FOOT_001,DF_FOOT_002,DF_FOOT_003,DF_FOOT_004,DF_FOOT_005,DF_FOOT_006,DF_FOOT_007,DF_FOOT_008,DF_FOOT_009,DF_FOOT_010,DF_FOOT_011,DF_FOOT_012,DF_FOOT_013,DF_FOOT_014,DF_FOOT_015,DF_FOOT_016,DF_FOOT_017,DF_FOOT_018,DF_FOOT_019,DF_FOOT_020,DF_FOOT_021,DF_FOOT_022,DF_FOOT_027,DF_FOOT_028,DF_FOOT_029,DF_FOOT_031,DF_FOOT_032,DF_FOOT_033,DF_FOOT_034,DF_FOOT_035,DF_HOR_001,DF_HOR_002,DF_HOR_003,DF_HOR_004,DF_HOR_005,DF_HOR_006,DF_HOR_007,DF_HOR_008,DF_HOR_009,DF_HOR_010,DF_HOR_011,DF_HOR_012,DF_HOR_013,DF_HOR_014,DF_HOR_015,DF_HOR_016,DF_HOR_017,DF_HOR_018,DF_HOR_019,DF_HOR_020,DF_HOR_021,DF_HOR_022,DF_HOR_023,DF_HOR_024,DF_HOR_025,DF_INT_001,DF_INT_003,DF_INT_004,DF_INT_005,DF_INT_007,DF_INT_012,DF_INT_017,DF_INT_018,DF_INT_019,DF_INT_020,DF_INT_021,DF_INT_022,DF_INT_023,DF_INT_024,DF_LIS_001,DF_LIS_002,DF_LIS_003,DF_LIS_004,DF_LIS_005,DF_LIS_006,DF_LIS_007,DF_LIS_008,DF_LIS_009,DF_LIS_010,DF_LIS_011,DF_LIS_012,DF_LIS_013,DF_LIS_014,DF_LIS_015,DF_LIS_016,DF_LIS_017,DF_LIS_018,DF_LIS_019,DF_LIS_020,DF_LIS_021,DF_LIS_022,DF_LIS_023,DF_LIS_024,DF_LIS_025,DF_LIS_026,DF_LIS_027,DF_LIS_028,DF_LIS_029,DF_LIS_030,DF_OPS_001,DF_OPS_002,DF_OPS_006,DF_OPS_010,DF_OPS_017,DF_OPS_019,DF_OPS_020,DF_RAD_001,DF_RAD_002,DF_RAD_003,DF_RAD_004,DF_RAD_005,DF_RAD_006,DF_RAD_007,DF_RAD_008,DF_RAD_009,DF_RAD_010,DF_RAD_011,DF_RAD_012,DF_RAD_013,DF_RAD_014,DF_RAD_016,DF_RAD_019,DF_RAD_035,DF_RAD_036,DF_RAD_037,DF_RAD_038,DF_RAD_039,DF_RAD_040,DF_RAD_041,DF_RAD_042,DF_RAD_043,DF_RAD_044,DF_RAD_045,DF_VOX_002,DF_VOX_003,DF_VOX_004,DF_VOX_005,DF_VOX_007,DF_VOX_008,DF_VOX_009,DF_VOX_010,DF_VOX_011,DF_VOX_012,DF_VOX_013,DF_VOX_014,DF_VOX_016,DF_VOX_017,DF_VOX_018,DF_VOX_019,DF_VOX_020,DF_VOX_021,DF_VOX_022,DF_VOX_023,DF_VOX_024,DF_VOX_025,DF_VOX_026,DF_VOX_027,DF_VOX_028,DF_VOX_029,DF_VOX_030";

    [MenuItem("DEAD FREQUENCY/Apply Import Settings")]
    public static void Apply()
    {
        int texChanged = 0, texSeen = 0, mdlChanged = 0, mdlSeen = 0, audChanged = 0, audSeen = 0, audUnmapped = 0;

        if (Directory.Exists("Assets/Textures"))
        {
            foreach (var path in Directory.GetFiles("Assets/Textures", "*.png", SearchOption.AllDirectories))
            {
                var p = path.Replace('\\', '/');
                var imp = AssetImporter.GetAtPath(p) as TextureImporter;
                if (imp == null) continue;
                texSeen++;
                bool dirty = false;
                if (p.EndsWith("_Albedo.png"))
                {
                    if (imp.textureType != TextureImporterType.Default) { imp.textureType = TextureImporterType.Default; dirty = true; }
                    if (!imp.sRGBTexture) { imp.sRGBTexture = true; dirty = true; }
                }
                else if (p.EndsWith("_Normal.png"))
                {
                    if (imp.textureType != TextureImporterType.NormalMap) { imp.textureType = TextureImporterType.NormalMap; dirty = true; }
                }
                else if (p.EndsWith("_Roughness.png") || p.EndsWith("_AO.png") || p.EndsWith("_Height.png") || p.EndsWith("_Metallic.png"))
                {
                    if (imp.sRGBTexture) { imp.sRGBTexture = false; dirty = true; }
                }
                if (dirty) { imp.SaveAndReimport(); texChanged++; }
            }
        }

        if (Directory.Exists("Assets/Models"))
        {
            foreach (var path in Directory.GetFiles("Assets/Models", "*.fbx", SearchOption.AllDirectories))
            {
                var p = path.Replace('\\', '/');
                var imp = AssetImporter.GetAtPath(p) as ModelImporter;
                if (imp == null) continue;
                mdlSeen++;
                bool dirty = false;
                if (imp.globalScale != 1f) { imp.globalScale = 1f; dirty = true; }
                if (imp.importAnimation) { imp.importAnimation = false; dirty = true; }
                if (imp.animationType != ModelImporterAnimationType.None) { imp.animationType = ModelImporterAnimationType.None; dirty = true; }
                if (dirty) { imp.SaveAndReimport(); mdlChanged++; }
            }
        }

        var streaming = new HashSet<string>(StreamingIds.Split(','));
        var decompress = new HashSet<string>(DecompressIds.Split(','));
        var compressed = new HashSet<string>(CompressedIds.Split(','));

        if (Directory.Exists("Assets/Audio"))
        {
            foreach (var path in Directory.GetFiles("Assets/Audio", "*.ogg", SearchOption.AllDirectories))
            {
                var p = path.Replace('\\', '/');
                var imp = AssetImporter.GetAtPath(p) as AudioImporter;
                if (imp == null) continue;
                audSeen++;
                var name = Path.GetFileNameWithoutExtension(p);
                var m = Regex.Match(name, @"^DF_[A-Z]+_\d+");
                var id = m.Success ? m.Value : name;
                AudioClipLoadType target;
                if (streaming.Contains(id)) target = AudioClipLoadType.Streaming;
                else if (decompress.Contains(id)) target = AudioClipLoadType.DecompressOnLoad;
                else if (compressed.Contains(id)) target = AudioClipLoadType.CompressedInMemory;
                else { audUnmapped++; Debug.LogWarning("[DF Import] Not in Audio Event Map: " + p); continue; }
                var s = imp.defaultSampleSettings;
                if (s.loadType != target) { s.loadType = target; imp.defaultSampleSettings = s; imp.SaveAndReimport(); audChanged++; }
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("[DF Import] Textures: " + texChanged + "/" + texSeen + " updated | Models: " + mdlChanged + "/" + mdlSeen + " updated | Audio: " + audChanged + "/" + audSeen + " updated | Audio unmapped: " + audUnmapped);
    }
}
