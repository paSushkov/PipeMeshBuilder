using System;
using System.Collections.Generic;
using System.Linq;
using PipeBuilder.Lods;
using PipeBuilder.Utility;
using UnityEngine;

namespace PipeBuilder
{
    public class PipeMeshCombiner : MonoBehaviour
    {
        public List<PipeBuilder> pipeBuilders = new List<PipeBuilder>();
        public string meshPath;

        [SerializeField] private List<LodElement> lodElements = new List<LodElement>();
        [SerializeField] private List<LodElement> lodElementsCache = new List<LodElement>();

        public bool CanBuild => CanBuildCheck();
        public bool CanGenerateLODGroup => CanGenerateLODGroupCheck();

        public List<LodElement> LODElements => lodElements;

        public void BuildCombinedLods(string path = null)
        {
            if (!CanBuild)
                return;

            GenerateLodsToCache();
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(path))
            {
                var result = MeshToAssetSaver.SaveLodMeshesToAssets(path, lodElementsCache);
                if (!result.success)
                {
                    switch (result.error)
                    {
                        case MeshToAssetSaver.Error.CanceledByUser:
                            var keepMeshes = UnityEditor.EditorUtility.DisplayDialog("Saving LOD mesh", "Canceling. Keep generated meshes in scene?", "Yes", "No");
                            if (!keepMeshes)
                            {
                                DestroyAllLodsCache();
                                return;
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
#endif
            DestroyAllLods();
            lodElements.AddRange(lodElementsCache);
            lodElementsCache.Clear();
        }

        public void DestroyAllLods()
        {
            for (var i = 0; i < lodElements.Count; i++)
                lodElements[i].Destroy();
            lodElements.Clear();
        }
        
        public void GenerateLodGroup()
        {
            if (!CanGenerateLODGroup)
                return;
            if (!TryGetComponent<LODGroup>(out var lodGroup))
                lodGroup = gameObject.AddComponent<LODGroup>();
            var lods = new LOD[lodElements.Count];
            for (var i = 0; i < lods.Length; i++)
            {
                var step = 1f / (lods.Length + 1);
                var lod = new LOD(1f - (i + 1) * step, lodElements[i].GameObject.GetComponents<Renderer>());
                lods[i] = lod;
            }

            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();
        }
        
        private void GenerateLodsToCache()
        {
            DestroyAllLodsCache();
            var lodCount = pipeBuilders[0].LODElements.Count;

            for (var i = 0; i < lodCount; i++)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.DisplayProgressBar("Pipe Mesh combiner", "Stitching LOD`s of meshes",
                    (float) i / lodCount);
#endif
                var requestLods = new LodElement[pipeBuilders.Count];
                for (var ii = 0; ii < pipeBuilders.Count; ii++)
                    requestLods[ii] = pipeBuilders[ii].LODElements[i];
                var result = MeshBuilder.CombineMesh(transform, requestLods);
                var name = gameObject.name + $"_LOD[{i}]";
                result.mesh.name = name;
                var lodElement = new LodElement(transform, name, result.mesh, result.materials);
                lodElementsCache.Add(lodElement);
            }
#if UNITY_EDITOR
            UnityEditor.EditorUtility.ClearProgressBar();
#endif
        }

        private void DestroyAllLodsCache()
        {
            for (var i = 0; i < lodElementsCache.Count; i++)
                lodElementsCache[i].Destroy();
            lodElementsCache.Clear();
        }

        private bool CanBuildCheck()
        {
            if (pipeBuilders.Count == 0 || pipeBuilders.Any(item => !item))
                return false;
            for (var i = 1; i < pipeBuilders.Count; i++)
            {
                if (pipeBuilders[i].LODElements.Count != pipeBuilders[0].LODElements.Count)
                    return false;
            }

            return true;
        }

        private bool CanGenerateLODGroupCheck()
        {
            if (lodElements.Count == 0)
                return false;
            for (var i = 0; i < lodElements.Count; i++)
            {
                if (lodElements[i] == null || !lodElements[i].GameObject)
                    return false;
            }

            return true;
        }
    }
}