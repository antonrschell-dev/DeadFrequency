using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Dead Frequency - one-click material build.
// Source of truth: the 25 M_*.json definitions in "09 Materials" (README - Unity Project Setup.md, step 5).
// Builds URP/Lit materials in Assets/Materials, wiring Albedo/Normal/AO/Height maps directly.
// URP Lit has no standalone roughness slot, so per material a packed MetallicSmoothness map is
// generated into Assets/Textures: R = metallic (map, or the JSON metallic_value), A = 255 - roughness.
// Tiling: material texture scale = 1 / tiling_default_m on the base map (URP Lit shares that transform).
public static class DeadFrequencyMaterials
{
    struct MatDef
    {
        public string Name, Albedo, Normal, Roughness, AO, Height, Metallic;
        public float MetallicValue, RoughnessBase, TilingM;
        public MatDef(string name, string albedo, string normal, string roughness, string ao, string height, string metallic, float metallicValue, float roughnessBase, float tilingM)
        {
            Name = name; Albedo = albedo; Normal = normal; Roughness = roughness; AO = ao; Height = height; Metallic = metallic;
            MetallicValue = metallicValue; RoughnessBase = roughnessBase; TilingM = tilingM;
        }
    }

    static readonly MatDef[] Defs =
    {
        new MatDef("M_Aged_Concrete", "T_Aged_Concrete_Albedo", "T_Aged_Concrete_Normal", "T_Aged_Concrete_Roughness", "T_Aged_Concrete_AO", "T_Aged_Concrete_Height", "", 0.00f, 0.95f, 2.00f),
        new MatDef("M_Broken_Glass", "T_Broken_Glass_Albedo", "T_Broken_Glass_Normal", "T_Broken_Glass_Roughness", "T_Broken_Glass_AO", "T_Broken_Glass_Height", "", 0.00f, 0.20f, 2.00f),
        new MatDef("M_Brushed_Steel", "T_Brushed_Steel_Albedo", "T_Brushed_Steel_Normal", "T_Brushed_Steel_Roughness", "T_Brushed_Steel_AO", "T_Brushed_Steel_Height", "T_Brushed_Steel_Metallic", 0.90f, 0.38f, 2.00f),
        new MatDef("M_Cable_Insulation", "T_Cable_Insulation_Albedo", "T_Cable_Insulation_Normal", "T_Cable_Insulation_Roughness", "T_Cable_Insulation_AO", "T_Cable_Insulation_Height", "", 0.00f, 0.65f, 2.00f),
        new MatDef("M_Ceiling_Panels", "T_Ceiling_Panels_Albedo", "T_Ceiling_Panels_Normal", "T_Ceiling_Panels_Roughness", "T_Ceiling_Panels_AO", "T_Ceiling_Panels_Height", "", 0.00f, 0.85f, 2.00f),
        new MatDef("M_Ceramic_Flooring", "T_Ceramic_Flooring_Albedo", "T_Ceramic_Flooring_Normal", "T_Ceramic_Flooring_Roughness", "T_Ceramic_Flooring_AO", "T_Ceramic_Flooring_Height", "", 0.00f, 0.35f, 2.00f),
        new MatDef("M_Clean_Concrete", "T_Clean_Concrete_Albedo", "T_Clean_Concrete_Normal", "T_Clean_Concrete_Roughness", "T_Clean_Concrete_AO", "T_Clean_Concrete_Height", "", 0.00f, 0.92f, 2.00f),
        new MatDef("M_Cracked_Concrete", "T_Cracked_Concrete_Albedo", "T_Cracked_Concrete_Normal", "T_Cracked_Concrete_Roughness", "T_Cracked_Concrete_AO", "T_Cracked_Concrete_Height", "", 0.00f, 0.94f, 2.00f),
        new MatDef("M_Dark_Industrial_Steel", "T_Dark_Industrial_Steel_Albedo", "T_Dark_Industrial_Steel_Normal", "T_Dark_Industrial_Steel_Roughness", "T_Dark_Industrial_Steel_AO", "T_Dark_Industrial_Steel_Height", "T_Dark_Industrial_Steel_Metallic", 0.85f, 0.50f, 2.00f),
        new MatDef("M_Dirty_Glass", "T_Dirty_Glass_Albedo", "T_Dirty_Glass_Normal", "T_Dirty_Glass_Roughness", "T_Dirty_Glass_AO", "T_Dirty_Glass_Height", "", 0.00f, 0.25f, 2.00f),
        new MatDef("M_Emergency_Equipment", "T_Emergency_Equipment_Albedo", "T_Emergency_Equipment_Normal", "T_Emergency_Equipment_Roughness", "T_Emergency_Equipment_AO", "T_Emergency_Equipment_Height", "", 0.00f, 0.50f, 2.00f),
        new MatDef("M_Frosted_Glass", "T_Frosted_Glass_Albedo", "T_Frosted_Glass_Normal", "T_Frosted_Glass_Roughness", "T_Frosted_Glass_AO", "T_Frosted_Glass_Height", "", 0.00f, 0.55f, 2.00f),
        new MatDef("M_Industrial_Flooring", "T_Industrial_Flooring_Albedo", "T_Industrial_Flooring_Normal", "T_Industrial_Flooring_Roughness", "T_Industrial_Flooring_AO", "T_Industrial_Flooring_Height", "T_Industrial_Flooring_Metallic", 0.40f, 0.60f, 2.00f),
        new MatDef("M_Insulation", "T_Insulation_Albedo", "T_Insulation_Normal", "T_Insulation_Roughness", "T_Insulation_AO", "T_Insulation_Height", "", 0.00f, 0.95f, 2.00f),
        new MatDef("M_Laboratory_Surfaces", "T_Laboratory_Surfaces_Albedo", "T_Laboratory_Surfaces_Normal", "T_Laboratory_Surfaces_Roughness", "T_Laboratory_Surfaces_AO", "T_Laboratory_Surfaces_Height", "", 0.00f, 0.40f, 2.00f),
        new MatDef("M_Medical_Equipment_Surfaces", "T_Medical_Equipment_Surfaces_Albedo", "T_Medical_Equipment_Surfaces_Normal", "T_Medical_Equipment_Surfaces_Roughness", "T_Medical_Equipment_Surfaces_AO", "T_Medical_Equipment_Surfaces_Height", "T_Medical_Equipment_Surfaces_Metallic", 0.10f, 0.35f, 2.00f),
        new MatDef("M_Painted_Industrial_Metal", "T_Painted_Industrial_Metal_Albedo", "T_Painted_Industrial_Metal_Normal", "T_Painted_Industrial_Metal_Roughness", "T_Painted_Industrial_Metal_AO", "T_Painted_Industrial_Metal_Height", "T_Painted_Industrial_Metal_Metallic", 0.15f, 0.60f, 2.00f),
        new MatDef("M_Pipes", "T_Pipes_Albedo", "T_Pipes_Normal", "T_Pipes_Roughness", "T_Pipes_AO", "T_Pipes_Height", "T_Pipes_Metallic", 0.60f, 0.70f, 2.00f),
        new MatDef("M_Plastic", "T_Plastic_Albedo", "T_Plastic_Normal", "T_Plastic_Roughness", "T_Plastic_AO", "T_Plastic_Height", "", 0.00f, 0.55f, 2.00f),
        new MatDef("M_Rubber", "T_Rubber_Albedo", "T_Rubber_Normal", "T_Rubber_Roughness", "T_Rubber_AO", "T_Rubber_Height", "", 0.00f, 0.90f, 2.00f),
        new MatDef("M_Rusted_Metal", "T_Rusted_Metal_Albedo", "T_Rusted_Metal_Normal", "T_Rusted_Metal_Roughness", "T_Rusted_Metal_AO", "T_Rusted_Metal_Height", "T_Rusted_Metal_Metallic", 0.35f, 0.88f, 2.00f),
        new MatDef("M_Server_Equipment", "T_Server_Equipment_Albedo", "T_Server_Equipment_Normal", "T_Server_Equipment_Roughness", "T_Server_Equipment_AO", "T_Server_Equipment_Height", "T_Server_Equipment_Metallic", 0.50f, 0.50f, 2.00f),
        new MatDef("M_Stainless_Steel", "T_Stainless_Steel_Albedo", "T_Stainless_Steel_Normal", "T_Stainless_Steel_Roughness", "T_Stainless_Steel_AO", "T_Stainless_Steel_Height", "T_Stainless_Steel_Metallic", 0.90f, 0.30f, 2.00f),
        new MatDef("M_Water-Damaged_Walls", "T_Water-Damaged_Walls_Albedo", "T_Water-Damaged_Walls_Normal", "T_Water-Damaged_Walls_Roughness", "T_Water-Damaged_Walls_AO", "T_Water-Damaged_Walls_Height", "", 0.00f, 0.80f, 2.00f),
        new MatDef("M_Wet_Concrete", "T_Wet_Concrete_Albedo", "T_Wet_Concrete_Normal", "T_Wet_Concrete_Roughness", "T_Wet_Concrete_AO", "T_Wet_Concrete_Height", "", 0.00f, 0.45f, 2.00f),
    };

    [MenuItem("DEAD FREQUENCY/Build Materials")]
    public static void Build()
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) { Debug.LogError("[DF Materials] URP/Lit shader not found - is this a URP project?"); return; }

        // Index every texture in Assets/Textures by file name (no extension).
        var texByName = new Dictionary<string, Texture2D>();
        foreach (var path in Directory.GetFiles("Assets/Textures", "*.png", SearchOption.AllDirectories))
        {
            var p = path.Replace('\\', '/');
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            if (t != null) texByName[Path.GetFileNameWithoutExtension(p)] = t;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");

        int created = 0, updated = 0, packed = 0, missing = 0;
        foreach (var d in Defs)
        {
            var matPath = "Assets/Materials/" + d.Name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            bool isNew = mat == null;
            if (isNew) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, matPath); }
            mat.shader = shader;

            Texture2D tex;
            if (!string.IsNullOrEmpty(d.Albedo) && texByName.TryGetValue(d.Albedo, out tex)) mat.SetTexture("_BaseMap", tex); else if (!string.IsNullOrEmpty(d.Albedo)) { missing++; Debug.LogWarning("[DF Materials] Missing albedo " + d.Albedo + " for " + d.Name); }
            if (!string.IsNullOrEmpty(d.Normal) && texByName.TryGetValue(d.Normal, out tex)) { mat.SetTexture("_BumpMap", tex); mat.EnableKeyword("_NORMALMAP"); }
            if (!string.IsNullOrEmpty(d.AO) && texByName.TryGetValue(d.AO, out tex)) { mat.SetTexture("_OcclusionMap", tex); mat.EnableKeyword("_OCCLUSIONMAP"); }
            if (!string.IsNullOrEmpty(d.Height) && texByName.TryGetValue(d.Height, out tex)) { mat.SetTexture("_ParallaxMap", tex); mat.EnableKeyword("_PARALLAXMAP"); }

            // Packed metallic/smoothness map (URP Lit reads smoothness from its alpha).
            Texture2D rghTex = null, metTex = null;
            if (!string.IsNullOrEmpty(d.Roughness)) texByName.TryGetValue(d.Roughness, out rghTex);
            if (!string.IsNullOrEmpty(d.Metallic)) texByName.TryGetValue(d.Metallic, out metTex);
            var packedTex = BuildPackedMap(d, rghTex, metTex);
            if (packedTex != null)
            {
                mat.SetTexture("_MetallicGlossMap", packedTex);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                mat.SetFloat("_Metallic", 1f);   // real values live in the packed R channel
                mat.SetFloat("_Smoothness", 1f);  // real values live in the packed A channel
                packed++;
            }
            else
            {
                mat.SetFloat("_Metallic", d.MetallicValue);
                mat.SetFloat("_Smoothness", 1f - d.RoughnessBase);
            }

            if (d.TilingM > 0f) mat.SetTextureScale("_BaseMap", new Vector2(1f / d.TilingM, 1f / d.TilingM));

            EditorUtility.SetDirty(mat);
            if (isNew) created++; else updated++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[DF Materials] Materials: " + created + " created, " + updated + " updated | Packed maps written: " + packed + " | Missing textures: " + missing);
    }

    static Texture2D BuildPackedMap(MatDef d, Texture2D rghTex, Texture2D metTex)
    {
        if (rghTex == null && metTex == null) return null;
        int w = rghTex != null ? rghTex.width : metTex.width;
        int h = rghTex != null ? rghTex.height : metTex.height;
        var rghPx = rghTex != null ? ReadPixels(rghTex, w, h) : null;
        var metPx = metTex != null ? ReadPixels(metTex, w, h) : null;
        var outPx = new Color32[w * h];
        byte metUniform = (byte)Mathf.RoundToInt(d.MetallicValue * 255f);
        byte smUniform = (byte)Mathf.RoundToInt((1f - d.RoughnessBase) * 255f);
        for (int i = 0; i < outPx.Length; i++)
        {
            byte r = metPx != null ? metPx[i].r : metUniform;
            byte a = rghPx != null ? (byte)(255 - rghPx[i].r) : smUniform;
            outPx[i] = new Color32(r, 0, 0, a);
        }
        var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        outTex.SetPixels32(outPx);
        outTex.Apply();
        var baseName = !string.IsNullOrEmpty(d.Roughness) ? d.Roughness.Replace("_Roughness", "") : d.Name;
        var path = "Assets/Textures/" + baseName + "_MetallicSmoothness.png";
        File.WriteAllBytes(path, outTex.EncodeToPNG());
        Object.DestroyImmediate(outTex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null && imp.sRGBTexture) { imp.sRGBTexture = false; imp.SaveAndReimport(); }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Color32[] ReadPixels(Texture tex, int w, int h)
    {
        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        var prev = RenderTexture.active;
        Graphics.Blit(tex, rt);
        RenderTexture.active = rt;
        var t2 = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t2.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        t2.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        var px = t2.GetPixels32();
        Object.DestroyImmediate(t2);
        return px;
    }
}
