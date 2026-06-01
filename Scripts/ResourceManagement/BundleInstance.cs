using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceManagement
{
    public class BundleInstance
    {
        /// <summary>
        /// 该AB包是否需要卸载
        /// 当且仅当该AB包没有加载资源且不被其他任何AB包依赖时可卸载
        /// </summary>
        public bool CanDestroy { get { return m_dicResources.Count == 0 && m_beDependetNum == 0; } }

        /// <summary>
        /// 获取资源实例
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="rscName">资源名</param>
        /// <param name="obj">实例</param>
        public void GetObjInstance<T>(string rscName, System.Action<T> callback, bool async = true) where T : UnityEngine.Object
        {
            // 获取资源
            GetRscInstance<T>(rscName, (rscInstance) =>
            {
                if (rscInstance == null)
                    return;

                // 获取资源实例
                T obj = rscInstance.CreateObjInstance<T>() as T;
                if (callback != null)
                    callback(obj);
            }, async);
        }

        /// <summary>
        /// 移除资源实例
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="rscName">资源名</param>
        /// <param name="obj">实例</param>
        public void RemoveObjInstance<T>(string rscName, T obj) where T : UnityEngine.Object
        {
            if (m_dicResources.ContainsKey(rscName))
            {
                ResourceInstance rscInstance = m_dicResources[rscName];
                rscInstance.DestroyObjInstance(obj);
            }
            else
            {
                Object.Destroy(obj);
            }
        }

        /// <summary>
        /// 销毁未使用的资源
        /// </summary>
        public void DestroyUnusedResource()
        {
            List<string> lsWaitForDestroy = new List<string>();
            foreach (var resource in m_dicResources)
            {
                if (resource.Value.CanDestroy)
                {
                    lsWaitForDestroy.Add(resource.Key);
                }
            }
            for (int i = 0; i < lsWaitForDestroy.Count; i++)
            {
                m_dicResources.Remove(lsWaitForDestroy[i]);
            }
        }

        /// <summary>
        /// 卸载AB包
        /// </summary>
        public void UnloadBundle()
        {
            // 删除依赖包引用
            for (int i = 0; i < m_dependences.Count; i++)
            {
                m_dependences[i].RemoveOneDependent();
            }
            m_dependences.Clear();
            m_dicResources.Clear();
            m_abBundle.Unload(true);
        }

        /// <summary>
        /// 添加一个被依赖项
        /// </summary>
        public void AddOneDependent()
        {
            ++m_beDependetNum;
        }

        /// <summary>
        /// 移除一个被依赖项
        /// </summary>
        public void RemoveOneDependent()
        {
            if (--m_beDependetNum < 0)
            {
                Debug.LogError("[ResourceManager] : 错误删除AB包依赖");
                m_beDependetNum = 0;
            }
        }

        /// <summary>
        /// 获取资源实例
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="rscName">资源名</param>
        /// <returns>资源实例</returns>
        private void GetRscInstance<T>(string rscName, System.Action<ResourceInstance> callback, bool async = true) where T : UnityEngine.Object
        {
            ResourceInstance rscInstance = null;
            // 已加载过
            if (m_dicResources.ContainsKey(rscName))
            {
                rscInstance = m_dicResources[rscName];
                if (callback != null)
                    callback(rscInstance);
            }
            // 未加载过
            else
            {
                // 异步加载
                if (async)
                {
                    ResourceSingleton.Instance.StartCoroutine(LoadResourceAsync(rscName, (opt) =>
                    {
                        AssetBundleRequest loadOpt = opt as AssetBundleRequest;
                        if (loadOpt == null || loadOpt.asset == null)
                        {
                            Debug.LogError("[ResourceManager] : " + rscName + "资源加载失败");
                            return;
                        }

                        rscInstance = new ResourceInstance(loadOpt.asset);
                        m_dicResources.Add(rscName, rscInstance);
                        if (callback != null)
                            callback(rscInstance);
                    }));
                }
                // 同步加载
                else
                {
                    Object rsc = m_abBundle.LoadAsset<T>(rscName);
                    rscInstance = new ResourceInstance(rsc);
                    m_dicResources.Add(rscName, rscInstance);
                    if (callback != null)
                        callback(rscInstance);
                }
            }
        }

        /// <summary>
        /// 资源加载协程
        /// </summary>
        /// <param name="rscName">资源名</param>
        /// <param name="callback">协程完成回调</param>
        private IEnumerator LoadResourceAsync(string rscName, System.Action<AsyncOperation> callback)
        {
            AssetBundleRequest opt = m_abBundle.LoadAssetAsync(rscName);
            opt.completed += callback;
            yield return 0;
        }

        public BundleInstance(AssetBundle ab, List<BundleInstance> bundleInstances)
        {
            m_dicResources = new Dictionary<string, ResourceInstance>();
            m_abBundle = ab;
            m_dependences = bundleInstances;
            if (m_dependences == null)
                m_dependences = new List<BundleInstance>();
            m_beDependetNum = 0;
        }

        /// <summary>
        /// 该实例使用的AB包
        /// </summary>
        private AssetBundle m_abBundle = null;

        /// <summary>
        /// 该AB包实例化的资源索引
        /// </summary>
        private Dictionary<string, ResourceInstance> m_dicResources = null;

        /// <summary>
        /// 所有依赖包
        /// </summary>
        private List<BundleInstance> m_dependences = null;

        /// <summary>
        /// 被依赖的包个数
        /// </summary>
        private int m_beDependetNum = 0;
    }
}