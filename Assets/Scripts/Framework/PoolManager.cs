using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 对象池管理器
/// </summary>
public class PoolManager
{
    // 单例模式
    private static PoolManager instance = new PoolManager();
    public static PoolManager Instance => instance;

    private const int DEFAULT_MAX_POOL_SIZE = 50;

    private Dictionary<string, Queue<GameObject>> poolDict = new Dictionary<string, Queue<GameObject>>();
    private GameObject poolRoot;

    private PoolManager()
    {
        poolRoot = new GameObject("PoolRoot");
        Object.DontDestroyOnLoad(poolRoot);

        poolDict = new Dictionary<string, Queue<GameObject>>();
    }

    /// <summary>
    /// 提前加载对象到池中，减少运行时的实例化开销
    /// </summary>
    /// <param name="prefab">加载的对象</param>
    /// <param name="count">加载数量</param>
    public void Preload(GameObject prefab, int count)
    {
        string poolKey = prefab.name;
        if (!poolDict.ContainsKey(poolKey))
        {
            poolDict[poolKey] = new Queue<GameObject>();
        }

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Object.Instantiate(prefab);
            obj.name = prefab.name;
            obj.SetActive(false);
            obj.transform.SetParent(poolRoot.transform);
            poolDict[poolKey].Enqueue(obj);
        }
    }

    /// <summary>
    /// 生成对象，如果池中有可用对象则复用，否则实例化新对象
    /// </summary>
    /// <param name="prefab">生成的对象</param>
    /// <param name="position">生成位置</param>
    /// <param name="rotation">生成角度</param>
    /// <returns></returns>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        string poolKey = prefab.name;
        if (!poolDict.ContainsKey(poolKey))
        {
            poolDict[poolKey] = new Queue<GameObject>();
        }

        GameObject obj;

        if (poolDict[poolKey].Count > 0)
        {
            obj = poolDict[poolKey].Dequeue();
        }
        else
        {
            obj = Object.Instantiate(prefab);
            obj.name = prefab.name;
        }

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.transform.SetParent(null);
        SceneManager.MoveGameObjectToScene(obj, SceneManager.GetActiveScene());
        obj.SetActive(true);

        return obj;
    }

    /// <summary>
    /// 销毁对象，将其返回池中以供复用，如果池已满则真正销毁对象
    /// </summary>
    /// <param name="obj">销毁的对象</param>
    public void Despawn(GameObject obj)
    {
        string poolKey = obj.name;

        if (!poolDict.ContainsKey(poolKey))
        {
            poolDict.Add(poolKey, new Queue<GameObject>());
        }

        if (poolDict[poolKey].Count >= DEFAULT_MAX_POOL_SIZE)
        {
            Object.Destroy(obj);
            return;
        }

        obj.SetActive(false);
        obj.transform.SetParent(poolRoot.transform);
        poolDict[poolKey].Enqueue(obj);
    }
}
