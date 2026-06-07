using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ABManager : MonoBehaviour
{
    public static ABManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    // 主包
    private AssetBundle mainAB = null;
    private AssetBundleManifest manifest = null;

    private Dictionary<string, AssetBundle> ABDic = new Dictionary<string, AssetBundle>();

    /// <summary>
    /// AB包的路径
    /// </summary>
    private string PathURL
    {
        get
        {
            return Application.streamingAssetsPath + "/";
        }
    }
    /// <summary>
    /// 主包的名字
    /// </summary>
    private string MainABName
    {
        get
        {
#if UNITY_IOS
            return "IOS";
#elif UNITY_ANDROID
            return "Android";
#else
            return "PC";
#endif
        }
    }


    /// <summary>
    /// 加载前置包
    /// </summary>
    /// <param name="ABName">AB包名称</param>
    private void LoadDependence(string ABName)
    {
        // 加载主包
        if (mainAB == null)
        {
            mainAB = AssetBundle.LoadFromFile(PathURL + MainABName);
            manifest = mainAB.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        }

        AssetBundle ab = null;

        // 加载依赖包
        string[] dependences = manifest.GetAllDependencies(ABName);
        foreach (string item in dependences)
        {
            if (!ABDic.ContainsKey(item))
            {
                ab = AssetBundle.LoadFromFile(PathURL + item);
                ABDic.Add(item, ab);
            }
        }

        // 加载目标包
        if (!ABDic.ContainsKey(ABName))
        {
            ab = AssetBundle.LoadFromFile(PathURL + ABName);
            ABDic.Add(ABName, ab);
        }
    }
    

    /// <summary>
    /// 同步加载资源
    /// </summary>
    /// <param name="ABName">AB包名称</param>
    /// <param name="resName">资源名称</param>
    public Object LoadResource(string ABName, string resName)
    {
        LoadDependence(ABName);

        // 加载资源
        Object obj = ABDic[ABName].LoadAsset(resName);
        if (obj is GameObject)
            return Instantiate(obj);
        else
            return obj;
    }

    public T LoadResource<T>(string ABName, string resName) where T : Object
    {
        LoadDependence(ABName);
        // 加载资源
        T obj = ABDic[ABName].LoadAsset<T>(resName);
        if (obj is GameObject)
            return Instantiate(obj);
        else
            return obj;
    }

    public Object LoadResource(string ABName, string resName, System.Type type)
    {
        LoadDependence(ABName);

        // 加载资源
        Object obj = ABDic[ABName].LoadAsset(resName, type);
        if (obj is GameObject)
            return Instantiate(obj);
        else
            return obj;
    }


    /// <summary>
    /// 异步加载资源
    /// </summary>
    /// <param name="ABName"></param>
    /// <param name="resName"></param>
    /// <param name="callback"></param>
    public void LoadResourceAsync(string ABName, string resName, System.Action<Object> callback)
    {
        StartCoroutine(LoadResourceAsyncCoroutine(ABName, resName, callback));
    }
    private IEnumerator LoadResourceAsyncCoroutine(string ABName, string resName, System.Action<Object> callback)
    {
        LoadDependence(ABName);

        // 加载资源
        AssetBundleRequest request = ABDic[ABName].LoadAssetAsync(resName);
        yield return request;

        if (request.asset is GameObject)
            callback?.Invoke(Instantiate(request.asset));
        else
            callback?.Invoke(request.asset);
    }

    public void LoadResourceAsync<T>(string ABName, string resName, System.Action<T> callback) where T : Object
    {
        StartCoroutine(LoadResourceAsyncCoroutine<T>(ABName, resName, callback));
    }
    private IEnumerator LoadResourceAsyncCoroutine<T>(string ABName, string resName, System.Action<T> callback) where T : Object
    {
        LoadDependence(ABName);
        // 加载资源
        AssetBundleRequest request = ABDic[ABName].LoadAssetAsync<T>(resName);
        yield return request;
        if (request.asset is GameObject)
            callback?.Invoke(Instantiate(request.asset) as T);
        else
            callback?.Invoke(request.asset as T);
    }

    public void LoadResourceAsync(string ABName, string resName, System.Type type, System.Action<Object> callback)
    {
        StartCoroutine(LoadResourceAsyncCoroutine(ABName, resName, type, callback));
    }
    private IEnumerator LoadResourceAsyncCoroutine(string ABName, string resName, System.Type type, System.Action<Object> callback)
    {
        LoadDependence(ABName);
        // 加载资源
        AssetBundleRequest request = ABDic[ABName].LoadAssetAsync(resName, type);
        yield return request;
        if (request.asset is GameObject)
            callback?.Invoke(Instantiate(request.asset));
        else
            callback?.Invoke(request.asset);
    }


    /// <summary>
    /// 卸载单个包
    /// </summary>
    /// <param name="ABName"></param>
    public void UnLoadAB(string ABName)
    {
        if (ABDic.ContainsKey(ABName))
        {
            ABDic[ABName].Unload(true);
            ABDic.Remove(ABName);
        }
    }
    /// <summary>
    /// 卸载所有包
    /// </summary>
    public void Clear()
    {
        AssetBundle.UnloadAllAssetBundles(false);
        ABDic.Clear();
        mainAB = null;
        manifest = null;
    }
}
