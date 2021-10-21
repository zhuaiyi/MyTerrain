using UnityEditor;

public class MTAssetBundleConfig
{
    public static BuildAssetBundleOptions BundleBuildOptions = BuildAssetBundleOptions.DeterministicAssetBundle | BuildAssetBundleOptions.ChunkBasedCompression;
    public static BuildTarget BundleTarget = BuildTarget.Android;
}