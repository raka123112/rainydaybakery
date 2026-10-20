using System;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using RainyDayBakery;

[InitializeOnLoad]
public static class BakeryProject
{
    const string PipelinePath = "Assets/Settings/BakeryPipeline.asset";
    static BakeryProject() { EditorApplication.delayCall += Configure; }
    [MenuItem("Rainy Day Bakery/Configure URP and Project")]
    public static void Configure()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)return;
        if(!AssetDatabase.IsValidFolder("Assets/Settings"))AssetDatabase.CreateFolder("Assets","Settings");
        var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        if(pipeline==null)
        {
            var renderer=AssetDatabase.LoadAssetAtPath<Renderer2DData>("Assets/Settings/BakeryRenderer.asset");
            if(renderer==null)
            {
                renderer=ScriptableObject.CreateInstance<Renderer2DData>();
                ResourceReloader.ReloadAllNullIn(renderer,"Packages/com.unity.render-pipelines.universal");
                AssetDatabase.CreateAsset(renderer,"Assets/Settings/BakeryRenderer.asset");
            }
            pipeline=UniversalRenderPipelineAsset.Create(renderer);pipeline.name="Bakery URP 2D";
            AssetDatabase.CreateAsset(pipeline,PipelinePath);
        }
        GraphicsSettings.renderPipelineAsset=pipeline;
        QualitySettings.renderPipeline=pipeline;
        var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers=tags.FindProperty("m_SortingLayers");
        string[] names={"Background","Environment_Base","Environment_Decor","Characters","Foreground","UI"};
        for(int i=0;i<names.Length;i++)
        {
            bool exists=false;for(int j=0;j<layers.arraySize;j++)if(layers.GetArrayElementAtIndex(j).FindPropertyRelative("name").stringValue==names[i])exists=true;
            if(exists)continue;
            layers.InsertArrayElementAtIndex(layers.arraySize);var layer=layers.GetArrayElementAtIndex(layers.arraySize-1);
            layer.FindPropertyRelative("name").stringValue=names[i];layer.FindPropertyRelative("uniqueID").intValue=1100+i;
        }
        tags.ApplyModifiedProperties();
        PlayerSettings.companyName="Rainy Day Studio";PlayerSettings.productName="Rainy Day Bakery";
        PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=1000;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
        PlayerSettings.colorSpace=ColorSpace.Linear;
        var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var input=settings.FindProperty("activeInputHandler");if(input!=null){input.intValue=0;settings.ApplyModifiedProperties();}
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Bakery.unity",true)};
        AssetDatabase.SaveAssets();
    }
    [MenuItem("Rainy Day Bakery/Build Windows")]
    public static void BuildWindows()
    {
        Configure();VerifyRules();Directory.CreateDirectory("Builds/Windows");
        var result=BuildPipeline.BuildPlayer(new[]{"Assets/Scenes/Bakery.unity"},"Builds/Windows/RainyDayBakery.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);
        if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Build failed: "+result.summary.result);
    }
    [MenuItem("Rainy Day Bakery/Verify Gameplay Rules")]
    public static void VerifyRules()
    { BakeryModelChecks.Run();Debug.Log("Rainy Day Bakery: all model checks passed."); }
}
public sealed class BakeryArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Resources/Art/"))return;
        var importer=(TextureImporter)assetImporter;
        importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;
        importer.mipmapEnabled=false;importer.isReadable=assetPath.EndsWith("characters.png");
        importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
    }
}
