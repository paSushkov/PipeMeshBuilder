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
            
            DrawAskForWirePreviewMesh();
            DrawSaveButton();
            
            DrawAskForPreviewMesh();
            DrawMaterials();
            DrawPreviewTrianglesAmount();
        }

        public override void DrawScene()
        {
        }
        
        #endregion
        
        private void DrawAskForWirePreviewMesh()
        {
            EditorGUI.BeginChangeCheck();
            pipeBuilder.drawGizmosMesh = GUILayout.Toggle(pipeBuilder.drawGizmosMesh, "Draw gizmos mesh", "Button");
            if (EditorGUI.EndChangeCheck())
            {
                if (pipeBuilder.drawGizmosMesh)
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
                SavePreviewMesh(pipeBuilder.previewMesh);
            }

            EditorGUI.EndDisabledGroup();

        }

        private void SavePreviewMesh(Mesh mesh)
        {
            var path = EditorUtility.SaveFilePanel("Save mesh to:", string.Empty, $"{pipeBuilder.gameObject.name}_mesh", "asset");
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
                
                var relativePath = path.Substring(path.IndexOf("Assets/"));
                AssetDatabase.CreateAsset(newMesh, relativePath);
                GUIUtility.ExitGUI();
            }
        }
    }
}