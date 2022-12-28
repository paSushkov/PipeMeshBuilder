using System;
using System.Collections.Generic;
using PipeBuilder.Lines;
using PipeBuilder.Lods;
using PipeBuilder.PipeSettings;
using PipeBuilder.Utility;
using UnityEngine;

namespace PipeBuilder
{
    public class PipeBuilder : MonoBehaviour
    {
        public int circleDetails = 5;
        public float rotation = 0f;
        public bool generateOuterSide = true;
        public bool generateInnerSide = true;
        public bool generateEdges = true;
        public Material material;
        public UvProperties uvProperties = new UvProperties();
        public bool drawGizmosMesh = true;
        public MeshFilter previewMeshFilter;
        public string meshPath;
        
        [NonSerialized] 
        public Mesh previewMesh;
        
        [SerializeField] private ControlNodeTurnSettings defaultControlLineTurnSettings;
        [SerializeField] private ControlNodeRadiusSettings defaultControlLineRadiusSettings;
        [SerializeField] private ControlLine controlLine;
        [SerializeField] private int previewTrianglesCount;
        [SerializeField] private List<LodInfo> lodInfos = new List<LodInfo>();
        [SerializeField] private int lodCount = 1;
        [SerializeField] private int lodDegradeStep = 0;
        [SerializeField] private List<LodElement> lodElements = new List<LodElement>();
        private List<LodElement> lodElementsCache = new List<LodElement>();
        private bool initialized;
        
        public bool Initialized => initialized;
        public ControlLine ControlLine => controlLine;
        public ControlNodeTurnSettings DefaultControlLineTurnSettings => defaultControlLineTurnSettings;
        public ControlNodeRadiusSettings DefaultControlLineRadiusSettings => defaultControlLineRadiusSettings;

        public List<LodInfo> LODInfos => lodInfos;
        public int PreviewTrianglesCount => previewTrianglesCount;
        
        public int MaxLodCount => lodDegradeStep < 1 ? 1 : 1 + (circleDetails - 3) / lodDegradeStep;

        public int MaxDegradeStep => circleDetails - 3;

        public int LODCount
        {
            get => lodCount;
            set => SetLodCount(value);
        }

        public int LODDegradeStep
        {
            get => lodDegradeStep;
            set => SetDegradeStep(value);
        }

        public List<LodElement> LODElements => lodElements;
        
        public void Initialize()
        {
            InitializeControlLine();
            initialized = true;
        }

        public void UpdateGlobalPositions()
        {
            UpdateControlLineGlobalPositions();
        }

        public int ForecastTrianglesAmount(int detail)
        {
            var lenght = ControlLine.ChordeNodesCount()-1;
            var result = 0;
            if (generateOuterSide)
                result += lenght * detail * 2;
            if(generateInnerSide)
                result += lenght * detail * 2;
            if (generateEdges)
                result += detail* 2*2;
            
            return result;
        }

        public void RebuildPreviewMesh()
        {
            if (!previewMesh || previewMesh == null)
                previewMesh = new Mesh {name = $"{gameObject.name}_PreviewMesh"};
            else
                previewMesh.Clear();

            var buildOptions = new MeshBuilder.MeshBuildParams(generateOuterSide, generateInnerSide,generateEdges, ControlLine, circleDetails, rotation, uvProperties);
            MeshBuilder.GeneratePipeMesh(buildOptions, previewMesh);
            if (previewMeshFilter)
            {
                previewMeshFilter.sharedMesh = previewMesh;
                UpdateMaterials();
            }
            previewTrianglesCount = ForecastTrianglesAmount(circleDetails);
        }

        private void GenerateLodsCache()
        {
            DestroyAllLodsCache();
            for (var i = 0; i < lodInfos.Count; i++)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.DisplayProgressBar("Pipe Mesh Builder", "Building meshes", (float) i / lodInfos.Count);
#endif
                var info = lodInfos[i];
                var mesh = new Mesh();
                var buildOptions = new MeshBuilder.MeshBuildParams(generateOuterSide, generateInnerSide, generateEdges, ControlLine, info.circleDetail, rotation, uvProperties);
                MeshBuilder.GeneratePipeMesh(buildOptions, mesh);
                var name = gameObject.name + $"_LOD[{i}]";
                mesh.name = name;
                var lodElement = new LodElement(transform, name, mesh, material);
                lodElementsCache.Add(lodElement);
            }
#if UNITY_EDITOR
            UnityEditor.EditorUtility.ClearProgressBar();
#endif
        }

        public void GenerateLODs(string path = null)
        {
            GenerateLodsCache();
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

        public void GenerateLodGroup()
        {
            if (!TryGetComponent<LODGroup>(out var lodGroup))
                lodGroup = gameObject.AddComponent<LODGroup>();
            var lods = new LOD[lodElements.Count];
            for (var i = 0; i < lods.Length; i++)
            {
                var step = 1f / (lods.Length+1);
                var lod = new LOD(1f-(i+1)*step, lodElements[i].GameObject.GetComponents<Renderer>());
                lods[i] = lod;
            }
            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();
        }

        public void DestroyAllLods()
        {
            for (var i = 0; i < lodElements.Count; i++)
                lodElements[i].Destroy();
            lodElements.Clear();
        }

        public void UpdateMaterials()
        {
            if (previewMeshFilter && previewMeshFilter.TryGetComponent<MeshRenderer>(out var meshRenderer))
                meshRenderer.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
        
        public void RecalculateLODsInfo()
        {
            lodInfos.Clear();
            for (var i = 0; i < lodCount; i++)
            {
                var detail = circleDetails - lodDegradeStep * i;
                if (detail<3)
                    return;
                var triangles = ForecastTrianglesAmount(detail);
                var newInfo = new LodInfo(detail, triangles);
                lodInfos.Add(newInfo);
            }
        }

        private void InitializeControlLine()
        {
            if (controlLine is null)
                controlLine = new ControlLine();
            if (defaultControlLineTurnSettings is null)
                defaultControlLineTurnSettings = new ControlNodeTurnSettings();
            if (defaultControlLineRadiusSettings is null)
                defaultControlLineRadiusSettings = new ControlNodeRadiusSettings();
            if (!controlLine.Initialized)
                controlLine.Initialize(this);
            if (drawGizmosMesh)
                RebuildPreviewMesh();
            RecalculateLODsInfo();
        }

        private void UpdateControlLineGlobalPositions()
        {
            for (var i = 0; i < ControlLine.ControlNodes.Count; i++)
            {
                var node = ControlLine.ControlNodes[i];
                node.AssignLocalPosition(node.LocalPosition);
            }
            for (var i = 0; i < ControlLine.ControlNodes.Count; i++)
            {
                controlLine.RecalculateTurnArcCenter(i);
            }
        }

        private void SetLodCount(int count)
        {
            if (lodDegradeStep < 1)
                lodCount = 1;
            else
                lodCount = Mathf.Clamp(count, 1, MaxLodCount);
            RecalculateLODsInfo();
        }

        private void SetDegradeStep(int step)
        {
            lodDegradeStep = Mathf.Clamp(step,0, MaxDegradeStep);
            SetLodCount(lodCount);
        }
        
        private void DestroyAllLodsCache()
        {
            for (var i = 0; i < lodElementsCache.Count; i++)
                lodElementsCache[i].Destroy();
            lodElementsCache.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmosMesh || !previewMesh)
                return;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireMesh(previewMesh);
        }
    }
}