using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetBundleManager : MonoSingleton<AssetBundleManager>
{
    private string mMainManifestPath
    {
        get
        {
            return string.Format("{0}/{1}", Application.streamingAssetsPath, "AssetBundlDir/AssetBundlDir");
        }
    }

    private Dictionary<string, AssetReqBaseInfo> mAssetBundleInfoDic = new Dictionary<string, AssetReqBaseInfo>();
    private Queue<AssetReq> mAssetRequestQueue = new Queue<AssetReq>();
    private List<AssetReq> mLoadingAssetReq = new List<AssetReq>();
    private List<bool> mLoadingAssetFlag = new List<bool>(mMaxLoadCount);
    private List<string> mDelayReleaseAssets = new List<string>();
    private const int mMaxLoadCount = 5;
    private const int mMaxReleaseCount = 20;
    private const int mReleaseCountPerFrame = 5;
    private WaitForEndOfFrame mWaitFrameEnd = new WaitForEndOfFrame();
    private float mTime = 0;
    private float mTimeInterval = 50;
    public delegate void AssetReqCallBack(UnityEngine.Object rOriginalRes, string rABName, string rResName);
    public class AssetReq
    {
        public string mABName;
        public string mResName;
        public AssetReqCallBack mCallBack;
        public bool mDelay;
        public AssetReq(string rABName, string rResName, AssetReqCallBack rCallBack, bool rDelay)
        {
            mABName = rABName;
            mResName = rResName;
            mCallBack = rCallBack;
            mDelay = rDelay;
        }
    }
    public class AssetReqBaseInfo
    {
        public string mABName;
        public AssetBundle mAssetBundle;
        public string[] mDependenceAB;
        public int mRefCount;
        public int mVersion;
        public ABLoadState mABState;
        public Dictionary<string, AssetInfo> mAsset;
        public AssetReqBaseInfo(string rABName, string[] rDepABName, int rVersion)
        {
            mABName = rABName;
            mDependenceAB = rDepABName;
            mRefCount = 0;
            mAssetBundle = null;
            mVersion = rVersion;
            mAsset = new Dictionary<string, AssetInfo>();
            mABState = ABLoadState.None;
        }
    }
    public class AssetInfo
    {
        public UnityEngine.Object mAsset;
        public AssetLodState mState;
        public AssetInfo(UnityEngine.Object rAsset, AssetLodState rAssetState)
        {
            mAsset = rAsset;
            mState = rAssetState;
        }
    }
    public enum AssetLodState
    {
        LoadFailed,
        Loading,
        Loaded,
    }
    public enum ABLoadState
    {
        None,
        Loading,
        Loaded,
        Release,
    }

    private void Update()
    {
        if (mTime > mTimeInterval)
            DelayRelease();
        else
            mTime += Time.deltaTime;
    }
    /// <summary>
    /// release assetbundle which refcount is zero 
    /// </summary>
    /// <param name="rABName"></param>
    /// <param name="rDelay"></param>
    /// <param name="rCallBack"></param>
    public void Release(string rABName, bool rDelay, AssetReqCallBack rCallBack)
    {
        AssetReqBaseInfo rBaseInfo = mAssetBundleInfoDic[rABName];
        rBaseInfo.mRefCount--;
        if (rDelay && rBaseInfo.mRefCount == 0 && rBaseInfo.mABState == ABLoadState.Loaded && rBaseInfo.mAssetBundle != null)
        {
            if (!mDelayReleaseAssets.Contains(rABName))
                mDelayReleaseAssets.Add(rABName);
        }
        else if (rBaseInfo.mABState == ABLoadState.Loaded && rBaseInfo.mAssetBundle != null && rBaseInfo.mRefCount == 0)
            UnloadAssetbundle(rABName);

        if (rCallBack != null)
            rCallBack(null, rABName, null);
    }

    private void DelayRelease()
    {
        if (mDelayReleaseAssets.Count > mMaxReleaseCount)
        {
            int rRemoveIndex = 0;
            for (int i = 0; i < mReleaseCountPerFrame; i++)
            {
                if (mDelayReleaseAssets.Count > 0)
                {
                    string rReleaseABName = mDelayReleaseAssets[i];
                    UnloadAssetbundle(rReleaseABName);
                    rRemoveIndex++;
                }
            }
            mDelayReleaseAssets.RemoveRange(0, rRemoveIndex);
        }
    }
    private void UnloadAssetbundle(string rABName)
    {
        AssetReqBaseInfo rBaseInfo = mAssetBundleInfoDic[rABName];
        rBaseInfo.mAsset.Clear();
        rBaseInfo.mAssetBundle.Unload(true);
        rBaseInfo.mAssetBundle = null;
        rBaseInfo.mABState = ABLoadState.None;
    }

    public void Load(string rABName, string rResName, AssetReqCallBack rCallBack)
    {
        AssetReq rReq = new global::AssetBundleManager.AssetReq(rABName, rResName, rCallBack, false);
        mAssetRequestQueue.Enqueue(rReq);
        AddToLoadList();
        LoopLoadAsset();
    }
    private void AddToLoadList()
    {
        if (mAssetRequestQueue.Count > 0 && !mLoadingAssetFlag.Contains(true))
        {
            mLoadingAssetReq.Clear();
            for (int count = 0; count < mMaxLoadCount; count++)
            {
                if (mAssetRequestQueue.Count <= 0)
                    break;
                AssetReq rAssetRequest = mAssetRequestQueue.Dequeue();
                mLoadingAssetReq.Add(rAssetRequest);
            }
        }
    }
    private void LoopLoadAsset()
    {
        for (int req_index = 0; req_index < mLoadingAssetReq.Count; req_index++)
        {
            StartCoroutine(LoadAssetBundleFromFile(mLoadingAssetReq[req_index], null, req_index));
        }
    }
    private bool CheckABName(string rABName)
    {
        if (mAssetBundleInfoDic.ContainsKey(rABName))
            return true;
        return false;
    }
    private IEnumerator LoadAssetBundleFromFile(AssetReq rAssetReq, string rDependABName, int rCurIndex)
    {
        if (rAssetReq == null)
        {
            if (!CheckABName(rDependABName))
            {
                Debug.LogError("not find assetbundle of " + rDependABName);
                yield break;
            }
            AssetReqBaseInfo rAssetReqInfo = mAssetBundleInfoDic[rDependABName];
            if (rAssetReqInfo.mABState == ABLoadState.Loaded && rAssetReqInfo.mAsset != null)
                yield break;
            while (rAssetReqInfo.mABState == ABLoadState.Loading)
            {
                //wait dependency assetbundle load finish;
                yield return null;
            }
            rAssetReqInfo.mABState = ABLoadState.Loading;
            AssetBundleCreateRequest rABCreateRequest = AssetBundle.LoadFromFileAsync(Application.streamingAssetsPath + "/AssetBundlDir/" + rAssetReqInfo.mABName);
            yield return rABCreateRequest;
            if (!rABCreateRequest.isDone)
            {
                Debug.LogError(rAssetReqInfo.mABName + " assetbundle load faid ");
                yield break;
            }
            rAssetReqInfo.mABState = ABLoadState.Loaded;
            rAssetReqInfo.mRefCount++;
        }
        else
        {
            //load assetbundle and asset
            if (!CheckABName(rAssetReq.mABName))
            {
                Debug.LogError("not find assetbundle of " + rAssetReq.mABName);
                yield break;
            }
            mLoadingAssetFlag[rCurIndex] = true;
            AssetReqBaseInfo rAssetReqInfo = mAssetBundleInfoDic[rAssetReq.mABName];
            while (rAssetReqInfo.mABState == ABLoadState.Loading)
            {
                //wait dependency assetbundle load finish;
                yield return null;
            }
            if (rAssetReqInfo.mABState == ABLoadState.None)
            {
                //load dependency assetbundle
                rAssetReqInfo.mABState = ABLoadState.Loading;
                mDelayReleaseAssets.Remove(rAssetReq.mABName);
                for (int dep_index = 0; dep_index < rAssetReqInfo.mDependenceAB.Length; dep_index++)
                {
                    yield return StartCoroutine(LoadAssetBundleFromFile(null, rAssetReqInfo.mDependenceAB[dep_index], -1));
                }
                AssetBundleCreateRequest rABCreateRequest = AssetBundle.LoadFromFileAsync(Application.streamingAssetsPath + "/AssetBundlDir/" + rAssetReqInfo.mABName);
                yield return rABCreateRequest;
                if (!rABCreateRequest.isDone)
                {
                    rAssetReqInfo.mABState = ABLoadState.None;
                    Debug.LogError(rAssetReqInfo.mABName + " assetbundle load faid ");
                    yield break;
                }
                else
                {
                    rAssetReqInfo.mABState = ABLoadState.Loaded;
                    rAssetReqInfo.mAssetBundle = rABCreateRequest.assetBundle;
                }
            }
            if (rAssetReqInfo.mABState == ABLoadState.Loaded)
            {
                if (!rAssetReqInfo.mAsset.ContainsKey(rAssetReq.mResName))
                {
                    AssetInfo rAssetInfo = new AssetInfo(null, AssetLodState.Loading);
                    rAssetReqInfo.mAsset.Add(rAssetReq.mResName, rAssetInfo);
                    AssetBundleRequest rABResReq = rAssetReqInfo.mAssetBundle.LoadAssetAsync(rAssetReq.mResName);
                    yield return rABResReq;
                    if (rABResReq.isDone)
                        rAssetInfo.mAsset = rABResReq.asset;
                    else
                    {
                        Debug.LogError("fail load " + rAssetReq.mResName + " from " + rAssetReq.mABName);
                        rAssetInfo.mState = AssetLodState.LoadFailed;
                        yield break;
                    }
                }
                else
                {
                    while (rAssetReqInfo.mAsset[rAssetReq.mResName].mState == AssetLodState.Loading)
                    {
                        yield return null;
                    }
                    if (rAssetReqInfo.mAsset[rAssetReq.mResName].mState == AssetLodState.LoadFailed)
                        yield break;
                }
                rAssetReq.mCallBack(rAssetReqInfo.mAsset[rAssetReq.mResName].mAsset, rAssetReqInfo.mABName, rAssetReq.mResName);
            }
            rAssetReqInfo.mRefCount++;
            mLoadingAssetFlag[rCurIndex] = false;
            yield return mWaitFrameEnd;
            //loop load assetrequest
            if (!mLoadingAssetFlag.Contains(true))
            {
                if (mAssetRequestQueue.Count > 0)
                {
                    AddToLoadList();
                    LoopLoadAsset();
                }
            }
        }
    }
    /// <summary>
    /// Load MainManifest File
    /// </summary>
    /// <param name="rVersion"></param>
    /// <returns></returns>
    IEnumerator LoadAssetBaseInfo(int rVersion)
    {
        AssetBundleCreateRequest rRequest = AssetBundle.LoadFromFileAsync(mMainManifestPath);
        yield return rRequest;
        if (!rRequest.isDone)
        {
            Debug.LogError("Fail load Mainmanifest file at " + mMainManifestPath);
            yield break;
        }
        else
        {
            if (rRequest.assetBundle != null)
            {
                AssetBundleRequest rABReq = rRequest.assetBundle.LoadAllAssetsAsync();
                yield return rABReq;
                if (rABReq.isDone)
                {
                    AssetBundleManifest rManifest = rABReq.asset as AssetBundleManifest;
                    string[] rAllAssetNames = rManifest.GetAllAssetBundles();
                    for (int asset_index = 0; asset_index < rAllAssetNames.Length; asset_index++)
                    {
                        string[] rDependencsName = rManifest.GetAllDependencies(rAllAssetNames[asset_index]);
                        for (int i = 0; i < rDependencsName.Length; i++)
                        {
                            Debug.LogError(rDependencsName[i]);
                        }
                        AssetReqBaseInfo rBaseInfo = new AssetReqBaseInfo(rAllAssetNames[asset_index], rDependencsName, rVersion);
                        mAssetBundleInfoDic.Add(rAllAssetNames[asset_index], rBaseInfo);
                    }
                }
            }
            else
            {
                Debug.LogError("Fail load Mainmanifest's  all assets at " + mMainManifestPath);
                yield break;
            }
        }
    }

    protected override void Initialize()
    {
        for (int i = 0; i < mMaxLoadCount; i++)
        {
            mLoadingAssetFlag.Add(false);
        }
        StartCoroutine(LoadAssetBaseInfo(1));
    }
}