using System.IO;
using PipeBuilder.Editor.Settings;
using UnityEditor;
using UnityEngine;

namespace PipeBuilder.Editor.Menus
{
    public class PreviewEditMenu : PipeEditMenu
    {
        #region MenuItem implementation

        public PreviewEditMenu(string name, PipeBuilder pipeBuilder, PipeBuilderEditorSettings settings) : base(name, pipeBuilder, settings)
        {
        }

        public override void OnEnable()
        {
        }

        public override void OnDisable()
        {
        }

        public override void DrawInspector()
        {
            DrawAskForGizmoPreviewMesh();
            DrawSaveButton();
            
            DrawAskForPreviewMesh();
            DrawMaterials();
            DrawPreviewTrianglesAmount();
        }

        public override void DrawScene()
        {
        }
        
        #endregion
        
        private void DrawAskForGizmoPreviewMesh()
        {
            var needPreview = pipeBuilder.drawGizmosMesh || pipeBuilder.drawGizmosWireMesh;
            
            EditorGUI.BeginChangeCheck();
            
            GUILayout.BeginHorizontal();
            pipeBuilder.drawGizmosMesh = GUILayout.Toggle(pipeBuilder.drawGizmosMesh, "Gizmo", "Button", GUILayout.Width(80));
            pipeBuilder.previewMeshColor = EditorGUILayout.ColorField(pipeBuilder.previewMeshColor);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            pipeBuilder.drawGizmosWireMesh = GUILayout.Toggle(pipeBuilder.drawGizmosWireMesh, "Wire", "Button", GUILayout.Width(80));
            pipeBuilder.previewWireMeshColor = EditorGUILayout.ColorField(pipeBuilder.previewWireMeshColor);
            GUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                if (!needPreview && (pipeBuilder.drawGizmosMesh || pipeBuilder.drawGizmosWireMesh))
                    pipeBuilder.RebuildPreviewMesh();
                EditorUtility.SetDirty(pipeBuilder);
            }
        }
        
        private void DrawAskForPreviewMesh()
        {
            EditorGUI.BeginChangeCheck();
            pipeBuilder.previewMeshFilter = (MeshFilter)EditorGUILayout.ObjectField("Preview mesh", pipeBuilder.previewMeshFilter, typeof(MeshFilter), true);
            if (EditorGUI.EndChangeCheck())
            {
                if (pipeBuilder.previewMeshFilter)
                    pipeBuilder.RebuildPreviewMesh();
                EditorUtility.SetDirty(pipeBuilder);
            }
        }

        private void DrawMaterials()
        {
            EditorGUI.BeginChangeCheck();
            pipeBuilder.material = (Material)EditorGUILayout.ObjectField("Material", pipeBuilder.material, typeof(Material), false);
            if (EditorGUI.EndChangeCheck())
            {
                pipeBuilder.UpdateMaterials();
                EditorUtility.SetDirty(pipeBuilder);
            }
        }
        
        private void DrawPreviewTrianglesAmount()
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.DelayedIntField("Triangles:", pipeBuilder.PreviewTrianglesCount);
            EditorGUI.EndDisabledGroup();
        }

        private void DrawSaveButton()
        {
            EditorGUI.BeginDisabledGroup(!pipeBuilder.previewMesh);
            if (GUILayout.Button("Save mesh"))
            {
                SavePreviewMesh(pipeBuilder);
            }

            EditorGUI.EndDisabledGroup();

        }

        private void SavePreviewMesh(PipeBuilder builder)
        {
            var path = builder.meshPath ?? string.Empty;
            path = Path.GetFullPath(Application.dataPath + path);
            
            if (!Directory.Exists(path))
                path = Application.dataPath;

            var fileName = $"{builder.gameObject.name}_mesh";
            var mesh = builder.previewMesh;

            path = EditorUtility.SaveFilePanel("Save mesh to:", path, fileName, "asset");
            if (!string.IsNullOrEmpty(path))
            {
                var newMesh = new Mesh
                {
                    vertices = mesh.vertices,
                    triangles = mesh.triangles,
                    uv = mesh.uv,
                    normals = mesh.normals,
                    colors = mesh.colors,
                    tangents = mesh.tangents
                };
                
                var projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../"));
                var assetPath = path.Substring(projectPath.Length);
                AssetDatabase.CreateAsset(newMesh, assetPath);

                builder.meshPath = Path.GetDirectoryName(path).Substring(Application.dataPath.Length);
                EditorUtility.SetDirty(builder);
                GUIUtility.ExitGUI();
            }
        }
    }
}