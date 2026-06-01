using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceManagement
{
    public class ResourceManager : MonoBehaviour
    {
        public static T Instance { get; private set; }

        public void Awake()
        {
            Instance = this as T;
        }

        public static string PREFAB_PATH = "Podels/";
        public static string MODEL_PATH = "Models/";
        public static string SCENE_PATH = "Scenes/";
        public static string TEXTURE_PATH = "Textures/";

        public static string ROOT_PATH_AB = Application.dataPath + "/AssetBundles/";
        public static string MAIN_BUNDLE_PATH = "assetbundles";
        public static string PREFAB_PATH_AB = "prefab";
        public static string MODEL_PATH_AB = "model";

        public bool AB_Mode = true;

        /// <summary>
        /// 获取GameObject资源实例
        /// </summary>
        /// <param name="rscName">资源名</param>
        /// <param name="obj">实例</param>
        public void LoadGameObject(string rscName, System.Action<GameObject> callback, bool async = true)
        {
            string rscPath = string.Empty;
            if (AB_Mode)
            {
                rscPath = PREFAB_PATH_AB;
            }
            else
            {
                rscPath = PREFAB_PATH;
            }
            LoadResource<GameObject>(rscName, rscPath, (obj) =>
            {
                m_isLoading = false;
                if (callback != null)
                    callback(obj);
            }, async);
        }

        /// <summary>
        /// 销毁GameObject资源实例
        /// </summary>
        /// <param name="obj">销毁实例</param>
        public void UnloadGameObject(ref GameObject obj)
        {
            string rscPath = string.Empty;
            if (AB_Mode)
            {
                rscPath = PREFAB_PATH_AB;
            }
            else
            {
                rscPath = PREFAB_PATH;
            }
            if (m_dicInstanceMap.ContainsKey(obj))
            {
                string rscName = m_dicInstanceMap[obj];
                UnloadResource(rscName, rscPath, obj);
            }
            else
            {
                Debug.LogWarning("[ResourceManager] : 实例丢失引用！");
                Object.Destroy(obj);
            }
            obj = null;
        }

        /// <summary>
        /// 加载资源并获取资源实例
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="rscName">资源名</param>
        /// <param name="rscPath">资源路径</param>
        /// <param name="obj">实例</param>
        private void LoadResource<T>(string rscName, string rscPath, System.Action<T> callback, bool async = true) where T : UnityEngine.Object
        {
            m_isLoading = true;
            if (AB_Mode)
            {
                // 获取AB包实例
                GetBundleInstance(rscPath, (bundleInstance) =>
                {
                    // 获取资源实例
                    bundleInstance.GetObjInstance<T>(rscName, (obj) =>
                    {
                        m_dicInstanceMap.Add(obj, rscName);
                        if (callback != null)
                        {
                            callback(obj);
                        }
                    }, async);
                }, async);
            }
            else
            {
                T obj = Object.Instantiate(Resources.Load<T>(rscPath + rscName));
                m_dicInstanceMap.Add(obj, rscName);
                if (callback != null)
                {
                    callback(obj);
                }
            }
        }

        /// <summary>
        /// 销毁实例并移除引用
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="rscName">资源名</param>
        /// <param name="rscPath">资源路径</param>
        /// <param name="obj">实例</param>
        private void UnloadResource<T>(string rscName, string rscPath, T obj) where T : UnityEngine.Object
        {
            m_dicInstanceMap.Remove(obj);
            if (AB_Mode && m_dicBundles.ContainsKey(rscPath))
            {
                BundleInstance bundleInstance = m_dicBundles[rscPath];
                bundleInstance.RemoveObjInstance(rscName, obj);
            }
            else
            {
                Object.Destroy(obj);
                Resources.UnloadUnusedAssets();
            }
        }

        /// <summary>
        /// 获取AB包实例
        /// </summary>
        /// <param name="bundleName">AB包名</param>
        /// <returns>AB包实例</returns>
        private void GetBundleInstance(string bundleName, System.Action<BundleInstance> callback, bool async = true)
        {
            BundleInstance bundleInstance = null;
            // 已加载AB包
            if (m_dicBundles.ContainsKey(bundleName))
            {
                bundleInstance = m_dicBundles[bundleName];
                if (callback != null)
                    callback(bundleInstance);
            }
            // 未加载AB包
            else
            {
                Debug.Log("[ResourceManager] : 加载AB包 :" + bundleName);
                // 异步加载AB包
                if (async)
                {
                    Instance.StartCoroutine(LoadAssetBundleAsync(ROOT_PATH_AB + bundleName, (opt) =>
                    {
                        AssetBundleCreateRequest loadOpt = opt as AssetBundleCreateRequest;
                        if (loadOpt == null || loadOpt.assetBundle == null)
                        {
                            Debug.LogError("[ResourceManager] : " + bundleName + "AB包加载失败");
                            return;
                        }

                        // 加载依赖包
                        string[] bundleDependenses = m_bundleManifest.GetAllDependencies(bundleName);

                        // 没有依赖
                        if (bundleDependenses.Length == 0)
                        {
                            bundleInstance = new BundleInstance(loadOpt.assetBundle, null);
                            m_dicBundles.Add(bundleName, bundleInstance);
                            if (callback != null)
                                callback(bundleInstance);
                        }
                        // 有依赖，递归地加载
                        else
                        {
                            LoadDependeces(bundleDependenses, (dependences) =>
                            {
                                bundleInstance = new BundleInstance(loadOpt.assetBundle, dependences);
                                m_dicBundles.Add(bundleName, bundleInstance);
                                if (callback != null)
                                    callback(bundleInstance);
                            }, true);
                        }
                    }));
                }
                // 同步加载AB包
                else
                {
                    AssetBundle ab = AssetBundle.LoadFromFile(bundleName);
                    if (ab)
                    {
                        // 加载依赖包
                        string[] bundleDependenses = m_bundleManifest.GetAllDependencies(bundleName);

                        // 没有依赖
                        if (bundleDependenses.Length == 0)
                        {
                            bundleInstance = new BundleInstance(ab, null);
                            m_dicBundles.Add(bundleName, bundleInstance);
                            if (callback != null)
                                callback(bundleInstance);
                        }
                        // 有依赖，递归地加载
                        else
                        {
                            LoadDependeces(bundleDependenses, (dependences) =>
                            {
                                bundleInstance = new BundleInstance(ab, dependences);
                                m_dicBundles.Add(bundleName, bundleInstance);
                                if (callback != null)
                                    callback(bundleInstance);
                            }, false);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 递归加载AB包依赖包
        /// </summary>
        /// <param name="bundleDependenses">所有依赖包</param>
        /// <param name="async">是否异步</param>
        /// <param name="callback">依赖包加载完成回调</param>
        /// <param name="index">当前加载依赖包索引</param>
        private void LoadDependeces(string[] bundleDependenses, System.Action<List<BundleInstance>> callback, bool async, int index = 0)
        {
            List<BundleInstance> dependences = new List<BundleInstance>();
            GetBundleInstance(bundleDependenses[index], (dependence) =>
            {
                // 跳出递归，添加被依赖项
                dependence.AddOneDependent();
                dependences.Add(dependence);
                if (++index >= bundleDependenses.Length)
                {
                    callback(dependences);
                    return;
                }
                LoadDependeces(bundleDependenses, callback, async, index);
            }, async);
        }

        /// <summary>
        /// 加载AB包协程
        /// </summary>
        /// <param name="bundleName">AB包路径</param>
        /// <param name="callback">协程完成回调</param>
        private IEnumerator LoadAssetBundleAsync(string bundleName, System.Action<AsyncOperation> callback)
        {
            AssetBundleCreateRequest opt = AssetBundle.LoadFromFileAsync(bundleName);
            opt.completed += callback;
            yield return 0;
        }

        #region 生命周期
        
        public void Start()
        {
            m_mainBundle = AssetBundle.LoadFromFile(ROOT_PATH_AB + MAIN_BUNDLE_PATH);
            m_bundleManifest = m_mainBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        }

        public void Update()
        {
            if (m_isLoading)
                return;
            if (AB_Mode)
            {
                List<string> lsWaitForUnload = new List<string>();
                foreach (var bundle in m_dicBundles)
                {
                    bundle.Value.DestroyUnusedResource();
                    if (bundle.Value.CanDestroy)
                    {
                        bundle.Value.UnloadBundle();
                        lsWaitForUnload.Add(bundle.Key);
                    }
                }
                for (int i = 0; i < lsWaitForUnload.Count; i++)
                {
                    m_dicBundles.Remove(lsWaitForUnload[i]);
                }
            }
            else
            {
                Resources.UnloadUnusedAssets();
            }
        }

        public void OnDestroy()
        {
            if (AB_Mode)
            {
                foreach (var bundle in m_dicBundles)
                {
                    bundle.Value.UnloadBundle();
                }
                m_dicBundles.Clear();
            }
            else
            {
                Resources.UnloadUnusedAssets();
            }
        }
        #endregion

        private AssetBundle m_mainBundle = null;

        private AssetBundleManifest m_bundleManifest = null;

        /// <summary>
        /// AB包实例
        /// </summary>
        private Dictionary<string, BundleInstance> m_dicBundles = new Dictionary<string, BundleInstance>();

        /// <summary>
        /// 实例-资源名映射
        /// </summary>
        private Dictionary<Object, string> m_dicInstanceMap = new Dictionary<Object, string>();

        /// <summary>
        /// 是否正在加载
        /// </summary>
        private bool m_isLoading = false;
    }
}