using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.UI
{
    // ScriptableObject định nghĩa danh sách các Prefab được quản lý bởi UIManager
    [CreateAssetMenu(fileName = "UIRegistry", menuName = "UI/UI Registry")]
    public class UIRegistry : ScriptableObject
    {
        [Serializable]
        public struct UIEntry
        {
            public string key;
            public GameObject prefab;
            public UILayerType layer;
        }

        [SerializeField] private List<UIEntry> entries = new List<UIEntry>();

        public IReadOnlyList<UIEntry> Entries => entries;

        public GameObject GetPrefab(string key)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].key == key) return entries[i].prefab;
            }
            return null;
        }

        public GameObject GetPrefab<T>() where T : Component
        {
            string typeName = typeof(T).Name;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].prefab != null && entries[i].prefab.GetComponent<T>() != null)
                {
                    return entries[i].prefab;
                }
                if (entries[i].key == typeName)
                {
                    return entries[i].prefab;
                }
            }
            return null;
        }

        public void AddOrUpdateEntry(string key, GameObject prefab, UILayerType layer)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].key == key)
                {
                    entries[i] = new UIEntry { key = key, prefab = prefab, layer = layer };
                    return;
                }
            }
            entries.Add(new UIEntry { key = key, prefab = prefab, layer = layer });
        }
    }
}
