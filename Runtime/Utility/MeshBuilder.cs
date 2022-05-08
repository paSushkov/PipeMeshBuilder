using System.Collections.Generic;
using PipeBuilder.Lines;
using PipeBuilder.Lods;
using PipeBuilder.Nodes;
using PipeBuilder.PipeSettings;
using UnityEngine;

namespace PipeBuilder.Utility
{
    public static class MeshBuilder
    {
        public struct MeshBuildParams
        {
            public bool generateOuterSide;
            public bool generateInnerSide;
            public bool generateEdges;
            public ControlLine controlLine;
            public int circleDetail;
            public float rotaion;
            public UvProperties uvProperties;


            public MeshBuildParams(bool generateOuterSide, bool generateInnerSide, bool generateEdges, ControlLine controlLine,
                int circleDetail, float rotaion, UvProperties uvProperties)
            {
                this.generateOuterSide = generateOuterSide;
                this.generateInnerSide = generateInnerSide;
                this.generateEdges = generateEdges;
                this.controlLine = controlLine;
                this.circleDetail = circleDetail;
                this.rotaion = rotaion;
                this.uvProperties = uvProperties;
            }
        }
        
        public struct CombinedMeshResult
        {
            public Mesh mesh;
            public Material[] materials;

            public CombinedMeshResult(Mesh mesh, Material[] materials)
            {
                this.mesh = mesh;
                this.materials = materials;
            }
        } 
        
        private static List<Vector3> verticesCache = new List<Vector3>();
        private static List<int> trinanglesCache = new List<int>();
        private static List<Vector2> uvCache = new List<Vector2>();
        private static List<Material> materials = new List<Material>();
        
        public static void GeneratePipeMesh(MeshBuildParams buildParams, Mesh mesh)
        {
            verticesCache.Clear();
            trinanglesCache.Clear();
            uvCache.Clear();
            buildParams.circleDetail++;
            var uvPropertis = buildParams.uvProperties;
            if (buildParams.generateOuterSide)
            {
                GenerateOuterVertices(buildParams.controlLine, buildParams.circleDetail, buildParams.rotaion, verticesCache);
                GenerateOuterTriangles(buildParams.controlLine, buildParams.circleDetail, 0, trinanglesCache);
                GenerateOuterBaseUV(buildParams.controlLine, buildParams.circleDetail, uvCache);
                ApplyTilingAndOffset(uvCache, uvPropertis.outerTiling, uvPropertis.outerOffset, 0, uvCache.Count);
                if (uvPropertis.outerAutoTiling)
                {
                    var radius = buildParams.controlLine.PipeBuilder.DefaultControlLineRadiusSettings.OuterRadius;
                    ApplyAutoXTiling(buildParams.controlLine, radius , uvCache, uvPropertis.outerTiling.y,0, uvCache.Count);
                }

            }
            if (buildParams.generateInnerSide)
            {
                var offset = verticesCache.Count;
                GenerateInnerVertices(buildParams.controlLine, buildParams.circleDetail, buildParams.rotaion, verticesCache);
                GenerateInnerTriangles(buildParams.controlLine, buildParams.circleDetail, offset, trinanglesCache); 
                GenerateInnerBaseUV(buildParams.controlLine, buildParams.circleDetail, uvCache);
                ApplyTilingAndOffset(uvCache, uvPropertis.innerTiling, uvPropertis.innerOffset, offset, uvCache.Count);
                if (uvPropertis.outerAutoTiling)
                {
                    var radius = buildParams.controlLine.PipeBuilder.DefaultControlLineRadiusSettings.InnerRadius;
                    ApplyAutoXTiling(buildParams.controlLine, radius , uvCache, uvPropertis.innerTiling.y,offset, uvCache.Count);
                }
            }

            if (buildParams.generateEdges)
            {
                var offset = verticesCache.Count;
                GenerateEdgesVertices(buildParams.controlLine, buildParams.circleDetail, buildParams.rotaion, verticesCache);
                GenerateEdgesTriangles(buildParams.circleDetail, offset, trinanglesCache);
                GenerateEdgesUV(buildParams.controlLine, buildParams.circleDetail, uvCache);
                ApplyTilingAndOffset(uvCache, uvPropertis.edgeTiling, uvPropertis.edgeOffset, offset, uvCache.Count);
            }

            if (!mesh)
                mesh = new Mesh();
            else
                mesh.Clear();

            mesh.SetVertices(verticesCache);
            mesh.SetTriangles(trinanglesCache, 0);
            mesh.SetUVs(0, uvCache);

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            mesh.Optimize();
        }

        public static CombinedMeshResult CombineMesh(Transform parent, params LodElement[] lodElements)
        {
            GetUniqueMaterials(materials, lodElements);
            var trianglesArray = new List<int>[materials.Count];
            for (var i = 0; i < materials.Count; i++)
                trianglesArray[i] = new List<int>();
            
            var mesh = new Mesh();
            mesh.subMeshCount = materials.Count;
            verticesCache.Clear();
            uvCache.Clear();

            var parentRotation = parent.rotation;
            var parentScale = parent.localScale;
            parent.localScale = Vector3.one;
            parent.localRotation = Quaternion.identity;
            

            for (var i = 0; i < lodElements.Length; i++)
            {
                var lod = lodElements[i];
                var material = lod.MeshRenderer.sharedMaterial;
                var lodMesh = lod.MeshFilter.sharedMesh; 
                var subMeshIndex = materials.IndexOf(material);
             
                var triangles = trianglesArray[subMeshIndex];
                
                var offset = verticesCache.Count;
                for (var ii = 0; ii < lodMesh.vertices.Length; ii++)
                {
                    var vertex = lod.GameObject.transform.TransformPoint(lodMesh.vertices[ii]);
                    vertex = parent.InverseTransformPoint(vertex);
                    verticesCache.Add(vertex);
                }

                for (var ii = 0; ii < lodMesh.triangles.Length; ii++)
                    triangles.Add(lodMesh.triangles[ii]+offset);
                uvCache.AddRange(lodMesh.uv);
            }
            
            mesh.SetVertices(verticesCache);
            for (var i = 0; i < materials.Count; i++)
            {
                var triangles = trianglesArray[i];
                mesh.SetTriangles(triangles, i);
                mesh.SetUVs(0,uvCache);
            }
            
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            mesh.Optimize();

            var result = new CombinedMeshResult(mesh, materials.ToArray());
            parent.localRotation = parentRotation;
            parent.localScale = parentScale;
            return result;
        }

        private static void GenerateOuterVertices(ControlLine controlLine, int circleDetail, float extraRotation, List<Vector3> result)
        {
            var controlNodes = controlLine.ControlNodes;
            for (var i = 0; i < controlNodes.Count; i++)
                GenerateLengthSegmentVertices(controlNodes[i], true, circleDetail, extraRotation, result);
        }
        
        private static void GenerateInnerVertices(ControlLine controlLine, int circleDetail, float extraRotation, List<Vector3> result)
        {
            var controlNodes = controlLine.ControlNodes;
            for (var i = 0; i < controlNodes.Count; i++)
                GenerateLengthSegmentVertices(controlNodes[i], false, circleDetail, extraRotation, result);
        }
        
        private static void GenerateLengthSegmentVertices(ControlNode controlNode, bool byOuter, int circleDetail, float extraRotation, List<Vector3> result)
        {
            var chordeNodes = controlNode.ChordeNodes;
            var radius = byOuter ? controlNode.OuterRadius : controlNode.InnerRadius;

            for (var i = 0; i < chordeNodes.Count; i++)
                GenerateLengthSegmentVertices(chordeNodes[i], radius, circleDetail, extraRotation, result);
        }

        private static void GenerateLengthSegmentVertices(ChordeNode chordeNode, float radius, int circleDetail, float extraRotation, List<Vector3> result)
        {
            var angleStep = 360f / (circleDetail - 1);
            var up = chordeNode.Up;
            var forward = -chordeNode.Forward;
                    
            for (var i = 0; i < circleDetail; i++)
            {
                var rotation = Quaternion.AngleAxis(angleStep * i + extraRotation, forward).normalized;
                var offset = rotation * up * radius;
                result.Add(chordeNode.LocalPosition + offset);
            }
        }

        private static void GenerateOuterTriangles(ControlLine controlLine, int circleDetail, int offset, List<int> result)
        {
            var chordeNodesCount = controlLine.ChordeNodesCount(); 
            var ySize = chordeNodesCount - 1;
            var xSize = circleDetail - 1;

            for (int ti = 0, vi = 0, y = 0; y < ySize; y++, vi++)
            {
                for (var x = 0; x < xSize; x++, ti += 6, vi++)
                {
                    result.Add(vi+offset);
                    result.Add(vi + xSize + 1 + offset);
                    result.Add(vi + 1 + offset);
                    result.Add(vi + 1 + offset);
                    result.Add(vi + xSize + 1 + offset);
                    result.Add(vi + xSize + 2 + offset);
                }
            }
        }
        
        private static void GenerateInnerTriangles(ControlLine controlLine, int circleDetail, int offset, List<int> result)
        {
            var chordeNodesCount = controlLine.ChordeNodesCount(); 
            var ySize = chordeNodesCount - 1;
            var xSize = circleDetail - 1;

            for (int ti = 0, vi = 0, y = 0; y < ySize; y++, vi++)
            {
                for (var x = 0; x < xSize; x++, ti += 6, vi++)
                {
                    result.Add(vi + offset);
                    result.Add(vi + 1 + offset);
                    result.Add(vi + xSize + 1 + offset);
                    
                    result.Add(vi + xSize + 1 + offset);
                    result.Add(vi + 1 + offset);
                    result.Add(vi + xSize + 2 + offset);
                }
            }
        }

        private static void GenerateOuterBaseUV(ControlLine controlLine, int circleDetail, List<Vector2> result)
        {
            var step = 1f / (circleDetail-1);
            // Setting up first row of UVs
            for (var i = 0; i < circleDetail; i++)
                result.Add(new Vector2(0f,  1-step*i));

            var lenght = controlLine.CalculateLenghtOfChordeLine();
            var controlNodes = controlLine.ControlNodes;
            var lastChordeNode = controlLine.GetChordeNode(0);
            var segment = 1;
            for (var i = 1; i < controlNodes.Count; i++)
            {
                var controlNode = controlNodes[i];
                var chordeNodes = controlNode.ChordeNodes;
                for (var ii = 0; ii < chordeNodes.Count; ii++)
                {
                    var chordeNode = chordeNodes[ii];
                    var distanceToPrev = Vector3.Distance(lastChordeNode, chordeNode);
                    var extraU = distanceToPrev / lenght;

                    for (var row = 0; row < circleDetail; row++)
                    {
                        var prevX = result[row + segment * circleDetail - circleDetail].x;
                        result.Add(new Vector2(prevX + extraU, 1 - step*row));
                    }

                    lastChordeNode = chordeNode;
                    segment++;
                }
            }
        }
        
        private static void GenerateInnerBaseUV(ControlLine controlLine, int circleDetail, List<Vector2> result)
        {
            var step = 1f / (circleDetail-1);
            // Setting up first row of UVs
            for (var i = 0; i < circleDetail; i++)
                result.Add(new Vector2(0f,  step*i));

            var lenght = controlLine.CalculateLenghtOfChordeLine();
            var controlNodes = controlLine.ControlNodes;
            var lastChordeNode = controlLine.GetChordeNode(0);
            var segment = 1;
            for (var i = 1; i < controlNodes.Count; i++)
            {
                var controlNode = controlNodes[i];
                var chordeNodes = controlNode.ChordeNodes;
                for (var ii = 0; ii < chordeNodes.Count; ii++)
                {
                    var chordeNode = chordeNodes[ii];
                    var distanceToPrev = Vector3.Distance(lastChordeNode, chordeNode);
                    var extraU = distanceToPrev / lenght;

                    for (var row = 0; row < circleDetail; row++)
                    {
                        var prevX = result[row + segment * circleDetail - circleDetail].x;
                        result.Add(new Vector2(prevX + extraU, step*row));
                    }

                    lastChordeNode = chordeNode;
                    segment++;
                }
            }
        }

        private static void ApplyAutoXTiling(ControlLine controlLine, float radius, List<Vector2> result, float yTiling, int startIndex, int lastIndex)
        {
            var lenght = controlLine.CalculateLenghtOfChordeLine();
            var multiplier = new Vector2(lenght/(radius*2)*yTiling, 1);
            
            for (var i = startIndex; i < lastIndex; i++)
                result[i] *= multiplier;
        }

        private static void GenerateEdgesVertices(ControlLine controlLine, int circleDetail, float extraRotation, List<Vector3> result)
        {
            var controlNodes = controlLine.ControlNodes;
            var controlNode = controlNodes[0];
            var chordeNode = controlNode.ChordeNodes[0];
            GenerateEdgeVertices(chordeNode, circleDetail, controlNode.InnerRadius, controlNode.OuterRadius, extraRotation, result);
            controlNode = controlNodes[controlNodes.Count - 1];
            chordeNode = controlNode.ChordeNodes[controlNode.ChordeNodes.Count-1];
            GenerateEdgeVertices(chordeNode, circleDetail, controlNode.InnerRadius, controlNode.OuterRadius, extraRotation, result);
        }

        private static void GenerateEdgeVertices(ChordeNode node, int circleDetail, float innerRadius, float outerRadius, float extraRotation, List<Vector3> result)
        {
            var angleStep = 360f / (circleDetail - 1);
            var up = node.Up;
            var forward = -node.Forward;

            for (var index = 0; index < circleDetail; index++)
            {
                var rotation = Quaternion.AngleAxis(angleStep * index + extraRotation, forward);
                var offset = rotation * up * innerRadius;
                result.Add(node.LocalPosition + offset);
            }
            for (var index = 0; index < circleDetail; index++)
            {
                var rotation = Quaternion.AngleAxis(angleStep * index + extraRotation, forward);
                var offset = rotation * up * outerRadius;
                result.Add(node.LocalPosition + offset);
            }
        }

        private static void GenerateEdgesTriangles(int circleDetail, int verticesOffset, List<int> result)
        {
            var ySize = 1;
            var xSize = circleDetail - 1;

            for (int ti = 0, vi = 0, y = 0; y < ySize; y++, vi++)
            {
                for (var x = 0; x < xSize; x++, ti += 6, vi++)
                {
                    result.Add(vi+verticesOffset);
                    result.Add(vi + xSize + 1+verticesOffset);
                    result.Add(vi+1+verticesOffset);
                    result.Add(vi + 1 +verticesOffset);
                    result.Add(vi + xSize + 1 +verticesOffset);
                    result.Add(vi + xSize + 2 +verticesOffset);
                }
            }
            verticesOffset += circleDetail * 2;
            for (int ti = 0, vi = 0, y = 0; y < ySize; y++, vi++)
            {
                for (var x = 0; x < xSize; x++, ti += 6, vi++)
                {
                    result.Add(vi+verticesOffset);
                    result.Add(vi + 1+ verticesOffset);
                    result.Add(vi + xSize + 1+verticesOffset);
                    result.Add(vi + 1+ verticesOffset);
                    result.Add(vi + xSize + 2 + verticesOffset);
                    result.Add(vi + xSize + 1 + verticesOffset);
                }
            }
        }

        private static void GenerateEdgesUV(ControlLine controlLine, int circleDetail, List<Vector2> result)
        {
            var controlNodes = controlLine.ControlNodes;
            var controlNode = controlNodes[0];
            GenerateEdgeUV(circleDetail, controlNode.OuterRadius, controlNode.InnerRadius, result, true);
            controlNode = controlNodes[controlNodes.Count-1];
            GenerateEdgeUV(circleDetail, controlNode.OuterRadius, controlNode.InnerRadius, result, false);
        }

        private static void GenerateEdgeUV(int circleDetail, float outerRadius, float innerRadius, List<Vector2> result, bool clockWise)
        {
            var angleStep = 360f / (circleDetail - 1);
            if (!clockWise)
                angleStep *= -1;
            var halfMultiplier = new Vector2(0.5f, 0.5f);
            var multiplier = innerRadius / outerRadius;
            for (var i = 0; i < circleDetail; i++)
            {
                var angle = i * angleStep;
                var angleRadians = Mathf.Deg2Rad* angle;
                var uv = new Vector2(Mathf.Sin(angleRadians), Mathf.Cos(angleRadians));
                uv /= 2;
                uv *= multiplier;
                uv += halfMultiplier;
                result.Add(uv);
            }

            for (var i = 0; i < circleDetail; i++)
            {
                var angle = i * angleStep;
                var angleRadians = Mathf.Deg2Rad* angle;
                var uv = new Vector2(Mathf.Sin(angleRadians), Mathf.Cos(angleRadians));
                uv /= 2;
                uv += halfMultiplier;
                result.Add(uv);
            }
        }
        
        private static void ApplyTilingAndOffset(List<Vector2> uv, Vector2 tiling, Vector2 offset, int from, int to)
        {
            for (var i = from; i < to; i++)
            {
                uv[i] *= tiling;
                uv[i] += offset;
            }
        }

        private static void GetUniqueMaterials(ICollection<Material> result, IReadOnlyList<LodElement> lodElements)
        {
            result.Clear();
            for (var i = 0; i < lodElements.Count; i++)
            {
                var material = lodElements[i].MeshRenderer.sharedMaterial;
                if (!result.Contains(material))
                    result.Add(material);
            }
        }
    }
}