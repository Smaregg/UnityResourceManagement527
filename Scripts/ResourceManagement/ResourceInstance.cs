using System.Collections.Generic;
using UnityEngine;

namespace ResourceManagement
{
    public class ResourceInstance
    {
        /// <summary>
        /// 该资源是否需要卸载
        /// </summary>
        public bool CanDestroy { get { return m_lsInstances.Count == 0; } }

        /// <summary>
        /// 获取实例
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <returns>实例</returns>
        public Object CreateObjInstance<T>() where T : UnityEngine.Object
        {
            Object instance = Object.Instantiate<T>(m_objResource as T);
            if (!m_lsInstances.Contains(instance))
                m_lsInstances.Add(instance);
            return instance;
        }

        /// <summary>
        /// 销毁实例
        /// </summary>
        /// <param name="instance">销毁实例</param>
        public void DestroyObjInstance(Object instance)
        {
            if (m_lsInstances.Contains(instance))
                m_lsInstances.Remove(instance);
            Object.Destroy(instance);
        }

        public ResourceInstance(Object obj)
        {
            m_objResource = obj;
            m_lsInstances = new List<Object>();
        }

        /// <summary>
        /// 该实例使用的资源
        /// </summary>
        private Object m_objResource = null;

        /// <summary>
        /// 该资源的所有实例化引用
        /// </summary>
        private List<Object> m_lsInstances = null;
    }
}