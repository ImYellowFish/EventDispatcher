// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

using System;
using UnityEngine;

namespace UnityEditor
{
    internal class ModifiedStandardShaderGUI : ShaderGUI
    {
        private enum WorkflowMode
        {
            Specular,
            Metallic,
            Dielectric
        }

        public enum BlendMode
        {
            Opaque,
            Cutout,
            Fade,   // Old school alpha-blending mode, fresnel does not affect amount of transparency
            Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
        }

        public enum SmoothnessMapChannel
        {
            SpecularMetallicAlpha,
            AlbedoAlpha,
        }

        private static class Styles
        {
            public static GUIContent uvSetLabel = new GUIContent("UV Set");

            public static GUIContent albedoText = new GUIContent("Albedo", "Albedo (RGB) and Transparency (A)");
            public static GUIContent alphaCutoffText = new GUIContent("Alpha Cutoff", "Threshold for alpha cutoff");
            public static GUIContent specularMapText = new GUIContent("Specular", "Specular (RGB) and Smoothness (A)");
            public static GUIContent metallicMapText = new GUIContent("Metallic", "Metallic (R) and Smoothness (A)");
            public static GUIContent smoothnessText = new GUIContent("Smoothness", "Smoothness value");
            public static GUIContent smoothnessScaleText = new GUIContent("Smoothness", "Smoothness scale factor");
            public static GUIContent smoothnessMapChannelText = new GUIContent("Source", "Smoothness texture and channel");
            public static GUIContent highlightsText = new GUIContent("Specular Highlights", "Specular Highlights");
            public static GUIContent reflectionsText = new GUIContent("Reflections", "Glossy Reflections");
            public static GUIContent normalMapText = new GUIContent("Normal Map", "Normal Map");
            public static GUIContent heightMapText = new GUIContent("Height Map", "Height Map (G)");
            public static GUIContent occlusionText = new GUIContent("Occlusion", "Occlusion (G)");
            public static GUIContent emissionText = new GUIContent("Color", "Emission (RGB)");
            public static GUIContent detailMaskText = new GUIContent("Detail Mask", "Mask for Secondary Maps (A)");
            public static GUIContent detailAlbedoText = new GUIContent("Detail Albedo x2", "Albedo (RGB) multiplied by 2");
            public static GUIContent detailNormalMapText = new GUIContent("Normal Map", "Normal Map");

            public static string primaryMapsText = "Main Maps";
            public static string secondaryMapsText = "Secondary Maps";
            public static string forwardText = "Forward Rendering Options";
            public static string renderingMode = "Rendering Mode";
            public static string advancedText = "Advanced Options";
            public static GUIContent emissiveWarning = new GUIContent("Emissive value is animated but the material has not been configured to support emissive. Please make sure the material itself has some amount of emissive.");
            public static readonly string[] blendNames = Enum.GetNames(typeof(BlendMode));
        }

        MaterialProperty blendMode = null;
        MaterialProperty albedoMap = null;
        MaterialProperty albedoColor = null;
        MaterialProperty alphaCutoff = null;
        MaterialProperty specularMap = null;
        MaterialProperty specularColor = null;
        MaterialProperty metallicMap = null;
        MaterialProperty metallic = null;
        MaterialProperty smoothness = null;
        MaterialProperty smoothnessScale = null;
        MaterialProperty smoothnessMapChannel = null;
        MaterialProperty highlights = null;
        MaterialProperty reflections = null;
        MaterialProperty bumpScale = null;
        MaterialProperty bumpMap = null;
        MaterialProperty occlusionStrength = null;
        MaterialProperty occlusionMap = null;
        MaterialProperty heigtMapScale = null;
        MaterialProperty heightMap = null;
        MaterialProperty emissionColorForRendering = null;
        MaterialProperty emissionMap = null;
        MaterialProperty detailMask = null;
        MaterialProperty detailAlbedoMap = null;
        MaterialProperty detailNormalMapScale = null;
        MaterialProperty detailNormalMap = null;
        MaterialProperty uvSetSecondary = null;

        MaterialEditor m_MaterialEditor;
        WorkflowMode m_WorkflowMode = WorkflowMode.Specular;
        ColorPickerHDRConfig m_ColorPickerHDRConfig = new ColorPickerHDRConfig(0f, 99f, 1 / 99f, 3f);

        bool m_FirstTimeApply = true;

        public void FindProperties(MaterialProperty[] props)
        {
            blendMode = FindProperty("_Mode", props);
            albedoMap = FindProperty("_MainTex", props);
            albedoColor = FindProperty("_Color", props);
            alphaCutoff = FindProperty("_Cutoff", props);
            specularMap = FindProperty("_SpecGlossMap", props, false);
            specularColor = FindProperty("_SpecColor", props, false);
            metallicMap = FindProperty("_MetallicGlossMap", props, false);
            metallic = FindProperty("_Metallic", props, false);
            if (specularMap != null && specularColor != null)
                m_WorkflowMode = WorkflowMode.Specular;
            else if (metallicMap != null && metallic != null)
                m_WorkflowMode = WorkflowMode.Metallic;
            else
                m_WorkflowMode = WorkflowMode.Dielectric;
            smoothness = FindProperty("_Glossiness", props);
            smoothnessScale = FindProperty("_GlossMapScale", props, false);
            smoothnessMapChannel = FindProperty("_SmoothnessTextureChannel", props, false);
            highlights = FindProperty("_SpecularHighlights", props, false);
            reflections = FindProperty("_GlossyReflections", props, false);
            bumpScale = FindProperty("_BumpScale", props);
            bumpMap = FindProperty("_BumpMap", props);
            heigtMapScale = FindProperty("_Parallax", props);
            heightMap = FindProperty("_ParallaxMap", props);
            occlusionStrength = FindProperty("_OcclusionStrength", props);
            occlusionMap = FindProperty("_OcclusionMap", props);
            emissionColorForRendering = FindProperty("_EmissionColor", props);
            emissionMap = FindProperty("_EmissionMap", props);
            detailMask = FindProperty("_DetailMask", props);
            detailAlbedoMap = FindProperty("_DetailAlbedoMap", props);
            detailNormalMapScale = FindProperty("_DetailNormalMapScale", props);
            detailNormalMap = FindProperty("_DetailNormalMap", props);
            uvSetSecondary = FindProperty("_UVSec", props);

            CustomFindProperty(props);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            FindProperties(props); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
            m_MaterialEditor = materialEditor;
            Material material = materialEditor.target as Material;

            // Make sure that needed setup (ie keywords/renderqueue) are set up if we're switching some existing
            // material to a standard shader.
            // Do this before any GUI code has been issued to prevent layout issues in subsequent GUILayout statements (case 780071)
            if (m_FirstTimeApply)
            {
                MaterialChanged(material, m_WorkflowMode);
                m_FirstTimeApply = false;
            }

            ShaderPropertiesGUI(material);
        }

        public void ShaderPropertiesGUI(Material material)
        {
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            // Detect any changes to the material
            EditorGUI.BeginChangeCheck();
            {
                BlendModePopup();

                // Primary properties
                GUILayout.Label(Styles.primaryMapsText, EditorStyles.boldLabel);
                DoAlbedoArea(material);
                DoSpecularMetallicArea();
                m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, bumpMap, bumpMap.textureValue != null ? bumpScale : null);
                m_MaterialEditor.TexturePropertySingleLine(Styles.heightMapText, heightMap, heightMap.textureValue != null ? heigtMapScale : null);
                m_MaterialEditor.TexturePropertySingleLine(Styles.occlusionText, occlusionMap, occlusionMap.textureValue != null ? occlusionStrength : null);
                m_MaterialEditor.TexturePropertySingleLine(Styles.detailMaskText, detailMask);
                DoEmissionArea(material);
                EditorGUI.BeginChangeCheck();
                m_MaterialEditor.TextureScaleOffsetProperty(albedoMap);
                if (EditorGUI.EndChangeCheck())
                    emissionMap.textureScaleAndOffset = albedoMap.textureScaleAndOffset; // Apply the main texture scale and offset to the emission texture as well, for Enlighten's sake

                EditorGUILayout.Space();

                // Secondary properties
                GUILayout.Label(Styles.secondaryMapsText, EditorStyles.boldLabel);
                m_MaterialEditor.TexturePropertySingleLine(Styles.detailAlbedoText, detailAlbedoMap);
                m_MaterialEditor.TexturePropertySingleLine(Styles.detailNormalMapText, detailNormalMap, detailNormalMapScale);
                m_MaterialEditor.TextureScaleOffsetProperty(detailAlbedoMap);
                m_MaterialEditor.ShaderProperty(uvSetSecondary, Styles.uvSetLabel.text);

                // Third properties
                GUILayout.Label(Styles.forwardText, EditorStyles.boldLabel);
                if (highlights != null)
                    m_MaterialEditor.ShaderProperty(highlights, Styles.highlightsText);
                if (reflections != null)
                    m_MaterialEditor.ShaderProperty(reflections, Styles.reflectionsText);
            }
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in blendMode.targets)
                    MaterialChanged((Material)obj, m_WorkflowMode);
            }

            EditorGUILayout.Space();

            // NB renderqueue editor is not shown on purpose: we want to override it based on blend mode
            GUILayout.Label(Styles.advancedText, EditorStyles.boldLabel);
            m_MaterialEditor.EnableInstancingField();
            m_MaterialEditor.DoubleSidedGIField();

            GUILayout.Label(CustomStyles.ExtensionText, EditorStyles.boldLabel);
            DoCustomGUI(material);
        }

        internal void DetermineWorkflow(MaterialProperty[] props)
        {
            if (FindProperty("_SpecGlossMap", props, false) != null && FindProperty("_SpecColor", props, false) != null)
                m_WorkflowMode = WorkflowMode.Specular;
            else if (FindProperty("_MetallicGlossMap", props, false) != null && FindProperty("_Metallic", props, false) != null)
                m_WorkflowMode = WorkflowMode.Metallic;
            else
                m_WorkflowMode = WorkflowMode.Dielectric;
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            // _Emission property is lost after assigning Standard shader to the material
            // thus transfer it before assigning the new shader
            if (material.HasProperty("_Emission"))
            {
                material.SetColor("_EmissionColor", material.GetColor("_Emission"));
            }

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
            {
                SetupMaterialWithBlendMode(material, (BlendMode)material.GetFloat("_Mode"));
                return;
            }

            BlendMode blendMode = BlendMode.Opaque;
            if (oldShader.name.Contains("/Transparent/Cutout/"))
            {
                blendMode = BlendMode.Cutout;
            }
            else if (oldShader.name.Contains("/Transparent/"))
            {
                // NOTE: legacy shaders did not provide physically based transparency
                // therefore Fade mode
                blendMode = BlendMode.Fade;
            }
            material.SetFloat("_Mode", (float)blendMode);

            DetermineWorkflow(MaterialEditor.GetMaterialProperties(new Material[] { material }));
            MaterialChanged(material, m_WorkflowMode);
        }

        void BlendModePopup()
        {
            EditorGUI.showMixedValue = blendMode.hasMixedValue;
            var mode = (BlendMode)blendMode.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (BlendMode)EditorGUILayout.Popup(Styles.renderingMode, (int)mode, Styles.blendNames);
            if (EditorGUI.EndChangeCheck())
            {
                m_MaterialEditor.RegisterPropertyChangeUndo("Rendering Mode");
                blendMode.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        void DoAlbedoArea(Material material)
        {
            m_MaterialEditor.TexturePropertySingleLine(Styles.albedoText, albedoMap, albedoColor);
            if (((BlendMode)material.GetFloat("_Mode") == BlendMode.Cutout))
            {
                m_MaterialEditor.ShaderProperty(alphaCutoff, Styles.alphaCutoffText.text, MaterialEditor.kMiniTextureFieldLabelIndentLevel + 1);
            }
        }

        void DoEmissionArea(Material material)
        {
            // Emission for GI?
            if (m_MaterialEditor.EmissionEnabledProperty())
            {
                bool hadEmissionTexture = emissionMap.textureValue != null;

                // Texture and HDR color controls
                m_MaterialEditor.TexturePropertyWithHDRColor(Styles.emissionText, emissionMap, emissionColorForRendering, m_ColorPickerHDRConfig, false);

                // If texture was assigned and color was black set color to white
                float brightness = emissionColorForRendering.colorValue.maxColorComponent;
                if (emissionMap.textureValue != null && !hadEmissionTexture && brightness <= 0f)
                    emissionColorForRendering.colorValue = Color.white;

                // change the GI flag and fix it up with emissive as black if necessary
                m_MaterialEditor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel, true);

                m_MaterialEditor.RangeProperty(_EmissionMultiplier, CustomStyles._EmissionMultiplier);
            }
        }

        void DoSpecularMetallicArea()
        {
            bool hasGlossMap = false;
            if (m_WorkflowMode == WorkflowMode.Specular)
            {
                hasGlossMap = specularMap.textureValue != null;
                m_MaterialEditor.TexturePropertySingleLine(Styles.specularMapText, specularMap, hasGlossMap ? null : specularColor);
            }
            else if (m_WorkflowMode == WorkflowMode.Metallic)
            {
                hasGlossMap = metallicMap.textureValue != null;
                m_MaterialEditor.TexturePropertySingleLine(Styles.metallicMapText, metallicMap, hasGlossMap ? null : metallic);
            }

            bool showSmoothnessScale = hasGlossMap;
            if (smoothnessMapChannel != null)
            {
                int smoothnessChannel = (int)smoothnessMapChannel.floatValue;
                if (smoothnessChannel == (int)SmoothnessMapChannel.AlbedoAlpha)
                    showSmoothnessScale = true;
            }

            int indentation = 2; // align with labels of texture properties
            m_MaterialEditor.ShaderProperty(showSmoothnessScale ? smoothnessScale : smoothness, showSmoothnessScale ? Styles.smoothnessScaleText : Styles.smoothnessText, indentation);

            ++indentation;
            if (smoothnessMapChannel != null)
                m_MaterialEditor.ShaderProperty(smoothnessMapChannel, Styles.smoothnessMapChannelText, indentation);
        }

        public static void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
        {
            switch (blendMode)
            {
                case BlendMode.Opaque:
                    material.SetOverrideTag("RenderType", "");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = -1;
                    break;
                case BlendMode.Cutout:
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    break;
                case BlendMode.Fade:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    break;
                case BlendMode.Transparent:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    break;
            }
        }

        static SmoothnessMapChannel GetSmoothnessMapChannel(Material material)
        {
            int ch = (int)material.GetFloat("_SmoothnessTextureChannel");
            if (ch == (int)SmoothnessMapChannel.AlbedoAlpha)
                return SmoothnessMapChannel.AlbedoAlpha;
            else
                return SmoothnessMapChannel.SpecularMetallicAlpha;
        }

        static void SetMaterialKeywords(Material material, WorkflowMode workflowMode)
        {
            // Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
            // (MaterialProperty value might come from renderer material property block)
            SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap") || material.GetTexture("_DetailNormalMap"));
            if (workflowMode == WorkflowMode.Specular)
                SetKeyword(material, "_SPECGLOSSMAP", material.GetTexture("_SpecGlossMap"));
            else if (workflowMode == WorkflowMode.Metallic)
                SetKeyword(material, "_METALLICGLOSSMAP", material.GetTexture("_MetallicGlossMap"));
            SetKeyword(material, "_PARALLAXMAP", material.GetTexture("_ParallaxMap"));
            SetKeyword(material, "_DETAIL_MULX2", material.GetTexture("_DetailAlbedoMap") || material.GetTexture("_DetailNormalMap"));

            // A material's GI flag internally keeps track of whether emission is enabled at all, it's enabled but has no effect
            // or is enabled and may be modified at runtime. This state depends on the values of the current flag and emissive color.
            // The fixup routine makes sure that the material is in the correct state if/when changes are made to the mode or color.
            MaterialEditor.FixupEmissiveFlag(material);
            bool shouldEmissionBeEnabled = (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0;
            SetKeyword(material, "_EMISSION", shouldEmissionBeEnabled);

            if (material.HasProperty("_SmoothnessTextureChannel"))
            {
                SetKeyword(material, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A", GetSmoothnessMapChannel(material) == SmoothnessMapChannel.AlbedoAlpha);
            }
        }

        static void MaterialChanged(Material material, WorkflowMode workflowMode)
        {
            SetupMaterialWithBlendMode(material, (BlendMode)material.GetFloat("_Mode"));

            SetMaterialKeywords(material, workflowMode);
        }

        static void SetKeyword(Material m, string keyword, bool state)
        {
            if (state)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }

        // ----------------- Custom functions --------------------
        public enum ZGameCullMode { Off = 0, Back = 1, Front = 2,}

        MaterialProperty _ZGameCullMode;
        MaterialProperty _EmissionMultiplier;

        MaterialProperty _RimEnabled;
        MaterialProperty _RimColor;
        MaterialProperty _RimPower;

        MaterialProperty _HsbcEnabled;
        MaterialProperty _HsbcParam;
        MaterialProperty _HsbcMask;
        
        MaterialProperty _ExtraLight_Enabled;
        
        MaterialProperty _SSS_Enabled;
        MaterialProperty _SSS_Power;
        MaterialProperty _SSS_Color;
        MaterialProperty _SSS_Ramp;
        MaterialProperty _SSS_Mask;

        MaterialProperty _UVFlow_Enabled;
        MaterialProperty _UVFlow_Color;
        MaterialProperty _UVFlow_Speed;
        MaterialProperty _UVFlow_Tex;

        
        private static class CustomStyles {
            public static string _EmissionMultiplier = "Emission Multiplier";

            public static string _CullModeLabel = "Cull Mode";
            public static string[] _CullModes = new string[] {"Off", "Back", "Front"};

            public static GUIContent _RimEnabled = new GUIContent("Rim Effect", "Rim Effect");
            public static GUIContent _RimColor = new GUIContent("Rim Color", "Rim Color");
            public static GUIContent _RimPower = new GUIContent("Rim Power", "Rim Power");

            public static GUIContent _HsbcEnabled = new GUIContent("HSBC", "Ajust hue, saturation, brightness and contrast");
            public static GUIContent _HsbcParam = new GUIContent("HSBC Parameters", "Hue, Saturation, Brightness and constrast");
            public static GUIContent _HsbcMask = new GUIContent("HSBC Mask", "HSBC Mask");

            public static GUIContent _ExtraLight_Enabled = new GUIContent("Extra Light", "Adds two lights in forward base class, handled by script");

            public static GUIContent _SSS_Enabled = new GUIContent("Subsurface scattering", "Subsurface scattering");
            public static GUIContent _SSS_Sigma = new GUIContent("SSS scatter", "SSS scatter");
            public static GUIContent _SSS_Power = new GUIContent("SSS power", "SSS power");
            public static GUIContent _SSS_Color = new GUIContent("SSS color", "SSS color");
            public static GUIContent _SSS_Ramp = new GUIContent("SSS ramp", "SSS ramp texture");
            public static GUIContent _SSS_Mask = new GUIContent("SSS mask", "SSS mask");

            public static GUIContent _UVFlow_Enabled = new GUIContent("UV Flow", "Adds a uv flow animation");
            public static GUIContent _UVFlow_Color = new GUIContent("UV Flow Color", "UV Flow Color");
            public static GUIContent _UVFlow_Speed = new GUIContent("UV Flow Speed", "UV Flow Speed");
            public static GUIContent _UVFlow_Tex = new GUIContent("UV Flow Texture", "UV Flow Texture");
            
            
            public static string ExtensionText = "Extensions";
        }
        

        private void CustomFindProperty(MaterialProperty[] props) {
            _ZGameCullMode = FindProperty("_ZGAME_CULL_MODE", props);

            _EmissionMultiplier = FindProperty("_EmissionMultiplier", props);

            _RimEnabled = FindProperty("_RimEnabled", props, false);
            _RimColor = FindProperty("_RimColor", props);
            _RimPower = FindProperty("_RimPower", props);

            _HsbcEnabled = FindProperty("_HsbcEnabled", props, false);
            _HsbcParam = FindProperty("_HsbcParam", props);
            _HsbcMask = FindProperty("_HsbcMask", props);

            _ExtraLight_Enabled = FindProperty("_ExtraLight_Enabled", props, false);

            _SSS_Enabled = FindProperty("_SSS_Enabled", props, false);
            _SSS_Color = FindProperty("_SSS_Color", props);
            _SSS_Power = FindProperty("_SSS_Power", props);
            _SSS_Ramp = FindProperty("_SSS_Ramp", props);
            _SSS_Mask = FindProperty("_SSS_Mask", props);

            _UVFlow_Enabled = FindProperty("_UVFlow_Enabled", props, false);
            _UVFlow_Color = FindProperty("_UVFlow_Color", props);
            _UVFlow_Speed = FindProperty("_UVFlow_Speed", props);
            _UVFlow_Tex = FindProperty("_UVFlow_Tex", props);

        }
        
        private void DoCustomGUI(Material m) {
            int indent = 2;

            DoCullModePopup(m);

            EditorGUI.BeginChangeCheck();
            {
                if (_RimEnabled != null)
                    m_MaterialEditor.ShaderProperty(_RimEnabled, CustomStyles._RimEnabled);
                if (_RimEnabled.floatValue > 0.5f) {
                    m_MaterialEditor.ShaderProperty(_RimColor, CustomStyles._RimColor, indent);
                    m_MaterialEditor.ShaderProperty(_RimPower, CustomStyles._RimPower, indent);
                }
                if (EditorGUI.EndChangeCheck()) {
                    if (_RimEnabled.floatValue > 0.5f)
                        m.EnableKeyword("_ZGAME_RIM");
                    else
                        m.DisableKeyword("_ZGAME_RIM");
                }
            }

            EditorGUI.BeginChangeCheck();
            {
                if (_HsbcEnabled != null)
                    m_MaterialEditor.ShaderProperty(_HsbcEnabled, CustomStyles._HsbcEnabled);
                if (_HsbcEnabled.floatValue > 0.5f) {
                    m_MaterialEditor.ShaderProperty(_HsbcParam, CustomStyles._HsbcParam, indent);
                    m_MaterialEditor.ShaderProperty(_HsbcMask, CustomStyles._HsbcMask, indent);
                }
                if (EditorGUI.EndChangeCheck()) {
                    if (_HsbcEnabled.floatValue > 0.5f)
                        m.EnableKeyword("_ZGAME_HSBC");
                    else
                        m.DisableKeyword("_ZGAME_HSBC");
                }
            }

            if (_ExtraLight_Enabled != null)
                m_MaterialEditor.ShaderProperty(_ExtraLight_Enabled, CustomStyles._ExtraLight_Enabled);
            
            EditorGUI.BeginChangeCheck();
            {
                if (_SSS_Enabled != null)
                    m_MaterialEditor.ShaderProperty(_SSS_Enabled, CustomStyles._SSS_Enabled);
                if (_SSS_Enabled.floatValue > 0.5f) {
                    m_MaterialEditor.ShaderProperty(_SSS_Color, CustomStyles._SSS_Color, indent);
                    m_MaterialEditor.ShaderProperty(_SSS_Power, CustomStyles._SSS_Power, indent);
                    m_MaterialEditor.ShaderProperty(_SSS_Ramp, CustomStyles._SSS_Ramp, indent);
                    m_MaterialEditor.ShaderProperty(_SSS_Mask, CustomStyles._SSS_Mask, indent);
                }
                if (EditorGUI.EndChangeCheck()) {
                    if (_SSS_Enabled.floatValue > 0.5f)
                        m.EnableKeyword("_ZGAME_SUBSURFACE_SCATTERING");
                    else
                        m.DisableKeyword("_ZGAME_SUBSURFACE_SCATTERING");
                }
            }

            EditorGUI.BeginChangeCheck();
            {
                if (_UVFlow_Enabled != null)
                    m_MaterialEditor.ShaderProperty(_UVFlow_Enabled, CustomStyles._UVFlow_Enabled);
                if (_UVFlow_Enabled.floatValue > 0.5f) {
                    m_MaterialEditor.ShaderProperty(_UVFlow_Color, CustomStyles._UVFlow_Color, indent);
                    m_MaterialEditor.ShaderProperty(_UVFlow_Speed, CustomStyles._UVFlow_Speed, indent);
                    m_MaterialEditor.ShaderProperty(_UVFlow_Tex, CustomStyles._UVFlow_Tex, indent);
                }
                if (EditorGUI.EndChangeCheck()) {
                    if (_UVFlow_Enabled.floatValue > 0.5f)
                        m.EnableKeyword("_ZGAME_UV_FLOW");
                    else
                        m.DisableKeyword("_ZGAME_UV_FLOW");
                }
            }
        }

        private void DoCullModePopup(Material m) {
            EditorGUI.BeginChangeCheck();
            {
                ZGameCullMode cullMode = ZGameCullMode.Off;
                if (_ZGameCullMode != null) {
                    cullMode = GetCullMode(_ZGameCullMode);
                    cullMode = (ZGameCullMode) EditorGUILayout.Popup(CustomStyles._CullModeLabel, (int) cullMode, CustomStyles._CullModes);
                }

                if (EditorGUI.EndChangeCheck()) {
                    m_MaterialEditor.RegisterPropertyChangeUndo("Cull Mode");
                    _ZGameCullMode.floatValue = (float) cullMode;
                }
            }
        }


        private static ZGameCullMode GetCullMode(MaterialProperty mp) {
            var mode = mp.floatValue;
            if (mode < 1)
                return ZGameCullMode.Off;
            else if (mode < 2)
                return ZGameCullMode.Back;
            else
                return ZGameCullMode.Front;
        }


        // ----------------- End Custom functions --------------------


    }
} // namespace UnityEditor
