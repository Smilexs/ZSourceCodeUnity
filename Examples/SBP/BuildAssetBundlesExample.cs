using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using BuildCompression = UnityEngine.BuildCompression;
using CompressionType = UnityEngine.CompressionType;

public static class BuildAssetBundlesExample
{
   
    public static bool BuildAssetBundles(string outputPath, bool forceRebuild, bool useChunkBasedCompression, BuildTarget buildTarget)
    {
        var options = BuildAssetBundleOptions.None;
        if (useChunkBasedCompression)
            options |= BuildAssetBundleOptions.ChunkBasedCompression;

        if (forceRebuild)
            options |= BuildAssetBundleOptions.ForceRebuildAssetBundle;

        Directory.CreateDirectory(outputPath);
        //传统AB构建方式改SBP的构建方式
        //var manifest = BuildPipeline.BuildAssetBundles(outputPath, options, buildTarget);
        var manifest = CompatibilityBuildPipeline.BuildAssetBundles(outputPath, options, buildTarget);
        return manifest != null;
    }
    
        
    public static bool BuildAssetBundlesByFileName(string outputPath, bool forceRebuild, bool useChunkBasedCompression, BuildTarget buildTarget)
    {
        var options = BuildAssetBundleOptions.None;
        if (useChunkBasedCompression)
            options |= BuildAssetBundleOptions.ChunkBasedCompression;

        if (forceRebuild)
            options |= BuildAssetBundleOptions.ForceRebuildAssetBundle;

        //得到要构建的所有AB
        var bundles = ContentBuildInterface.GenerateAssetBundleBuilds();
        //设置Bundle的 addressableNames 
        for (var i = 0; i < bundles.Length; i++)
            bundles[i].addressableNames = bundles[i].assetNames.Select(Path.GetFileNameWithoutExtension).ToArray();

        var manifest = CompatibilityBuildPipeline.BuildAssetBundles(outputPath, bundles, options, buildTarget);
        return manifest != null;
    }
    
    //Using ContentFileIdentifiers is required, otherwise the resulting AssetBundles will not be able to load.
    //Requires Unity 2022.2 or later
    // public static bool BuildAssetBundles(string outputPath, bool useChunkBasedCompression, BuildTarget buildTarget, BuildTargetGroup buildGroup)
    // {
    //     var buildContent = new BundleBuildContent(ContentBuildInterface.GenerateAssetBundleBuilds());
    //     var buildParams = new BundleBuildParameters(buildTarget, buildGroup, outputPath);
    //     if (useChunkBasedCompression)
    //         buildParams.BundleCompression = UnityEngine.BuildCompression.LZ4;
    //
    //     var tasks = DefaultBuildTasks.ContentFileCompatible();
    //     var buildLayout = new ClusterOutput();
    //     var exitCode = ContentPipeline.BuildAssetBundles(buildParams, buildContent, out _, tasks, new ContentFileIdentifiers(), buildLayout);
    //     return exitCode == ReturnCode.Success;
    // }
    
    /// <summary>
    /// SBP构建示例
    /// </summary>
    /// <param name="outputPath"></param>
    /// <param name="useChunkBasedCompression"></param>
    /// <param name="buildTarget"></param>
    /// <param name="buildGroup"></param>
    /// <returns></returns>
    public static bool BuildAssetBundles(string outputPath, bool useChunkBasedCompression, BuildTarget buildTarget, 
                                BuildTargetGroup buildGroup)
    {
        //构建内容
        var buildContent = new BundleBuildContent(ContentBuildInterface.GenerateAssetBundleBuilds());
        //构建参数
        var buildParams = new BundleBuildParameters(buildTarget, buildGroup, outputPath);
        //  设置Cache Server（多台机器之间实现更快的构建时间）
        buildParams.UseCache = true;
        buildParams.CacheServerHost = "buildcache.unitygames.com";
        buildParams.CacheServerPort = 8126;

        if (useChunkBasedCompression)
            buildParams.BundleCompression = UnityEngine.BuildCompression.LZ4;

        IBundleBuildResults results;
        ReturnCode exitCode = ContentPipeline.BuildAssetBundles(buildParams, buildContent, out results);
        return exitCode == ReturnCode.Success;
    }
    
    public static bool BuildAssetBundles(string outputPath, bool useChunkBasedCompression, BuildTarget buildTarget, 
                                    BuildTargetGroup buildGroup, CompressionType compressionType)
    {
        //构建内容
        var buildContent = new BundleBuildContent(ContentBuildInterface.GenerateAssetBundleBuilds());
        //自定义的构建参数
        var buildParams = new CustomBuildParameters(buildTarget, buildGroup, outputPath);
        // 设置标识记录特殊AB包的压缩方式
        buildParams.PerBundleCompression.Add("Bundle1", BuildCompression.Uncompressed);
        buildParams.PerBundleCompression.Add("Bundle2", BuildCompression.LZMA);

        if (compressionType == CompressionType.None)
            buildParams.BundleCompression = BuildCompression.Uncompressed;
        else if (compressionType == CompressionType.Lzma)
            buildParams.BundleCompression = BuildCompression.LZMA;
        else if (compressionType == CompressionType.Lz4 || compressionType == CompressionType.Lz4HC)
            buildParams.BundleCompression = BuildCompression.LZ4;

        IBundleBuildResults results;
        ReturnCode exitCode = ContentPipeline.BuildAssetBundles(buildParams, buildContent, out results);
        return exitCode == ReturnCode.Success;
    }

    /*
     * 自定义的构建参数
     */
    class CustomBuildParameters : BundleBuildParameters
    {
        public Dictionary<string, BuildCompression> PerBundleCompression { get; set; }

        public CustomBuildParameters(BuildTarget target, BuildTargetGroup group, string outputFolder) : base(target, group, outputFolder)
        {
            PerBundleCompression = new Dictionary<string, BuildCompression>();
        }

        // 重写获得AB包的压缩方式
        public override BuildCompression GetCompressionForIdentifier(string identifier)
        {
            BuildCompression value;
            if (PerBundleCompression.TryGetValue(identifier, out value)) //特殊AB包的压缩方式
                return value;
            return BundleCompression;
        }
    }

}