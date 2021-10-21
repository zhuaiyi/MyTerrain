using UnityEngine;

public class GameConfig
{
    public static readonly Vector3 PlayerBirthPos = new Vector3(186, 12, 88);

    public const string BundleExt = ".assetbundle";
    public const string TerrainDataBundlesRelativeFolder = "/TerrainData";
    public const string TerrainObjsBundlesRelativeFolder = "/TerrainObjs";

    public static string GetFullTerrainDataBundlePath(string fileName)
    {
        return string.Format("{0}{1}/{2}{3}", ServerFolder, TerrainDataBundlesRelativeFolder, fileName, BundleExt);
    }

    public static string GetFullTerrainObjsBundlePath(string fileName)
    {
        return string.Format("{0}{1}/{2}{3}", ServerFolder, TerrainObjsBundlesRelativeFolder, fileName, BundleExt);
    }

    public static string ServerFolder
    {
        get
        {
            if (_serverFolder == null)
            {
                _serverFolder = 
#if UNITY_5_3_OR_NEWER
                    ""
#else
                    ((Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor) ? "file://" : "") 
#endif
                    + Application.streamingAssetsPath + "/Android";
            }
            return _serverFolder;
        }
    }
    private static string _serverFolder = null;
}

