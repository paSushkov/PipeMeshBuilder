using System;
using UnityEngine;

namespace PipeBuilder.Lods
{
    [Serializable]
    public class LodElement
    {
        [SerializeField] private GameObject gameObject;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        
        public GameObject GameObject => gameObject;

        public MeshFilter MeshFilter => meshFilter;

        public MeshRenderer MeshRenderer => meshRenderer;

        public LodElement(Transform parent, string name, Mesh mesh, Material material)
        {
            gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
        }
        
        public LodElement(Transform parent, string name, Mesh mesh, Material[] materials)
        {
            gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = materials;
        }

        public void Destroy()
        {
            if (gameObject)
                GameObject.DestroyImmediate(gameObject);
        }
    }
}