using System.IO;
using UnityEditor;
using UnityEngine;

namespace PipeBuilder.Editor.Settings
{
    [CustomEditor(typeof(PipeMeshCombiner))]
    public class PipeMeshCombinerEditor : UnityEditor.Editor
    {
        private const string ErrorMessage = "One or more of PipeBuilders are missng/non assigned or have diffeerent LODs count";
        private PipeMeshCombiner meshCombiner;

        private void OnEnable()
        {
            meshCombiner = (PipeMeshCombiner) target;
        }

        public override void OnInspectorGUI()
        {
            if (!meshCombiner)
                return;
            GUILayout.BeginHorizontal();
            DrawBuildButton();
            DrawDestroyLodElements();
            GUILayout.EndHorizontal();
            DrawGenerateLodGroup();
            serializedObject.Update();
            DrawTargetBuilders();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTargetBuilders()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(meshCombiner.pipeBuilders)));
        }

        private void DrawBuildButton()
        {
            var canBuild = meshCombiner.CanBuild;
            if (!canBuild)
                DrawErrorTooltip();
            EditorGUI.BeginDisabledGroup(!canBuild);
            if (GUILayout.Button("Build combined LODs"))
            {
                var saveMeshes = EditorUtility.DisplayDialog("Pipe Mesh Combiner", "Save meshes as assets?", "Yes", "No");
                if (saveMeshes)
                {
                    var path = meshCombiner.meshPath ?? string.Empty;
                    path = Path.GetFullPath(Application.dataPath + path);

                    if (!Directory.Exists(path))
                        path = Application.dataPath;
                    path = EditorUtility.SaveFolderPanel("Pipe Mesh Combiner - target folder", path, string.Empty);
                    meshCombiner.BuildCombinedLods(path);
                    meshCombiner.meshPath = path.Substring(Application.dataPath.Length);
                }
                else
                    meshCombiner.BuildCombinedLods(null);
                
                EditorUtility.SetDirty(meshCombiner);
                GUIUtility.ExitGUI();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawDestroyLodElements()
        {
            if (GUILayout.Button("Destroy LOD`s"))
            {
                meshCombiner.DestroyAllLods();
                EditorUtility.SetDirty(meshCombiner);
                GUIUtility.ExitGUI();
            }
        }

        private void DrawErrorTooltip()
        {
            EditorGUILayout.HelpBox(ErrorMessage, MessageType.Error);
        }

        private void DrawGenerateLodGroup()
        {
            EditorGUI.BeginDisabledGroup(!meshCombiner.CanGenerateLODGroup);
            if (GUILayout.Button("Generate LOD group"))
            {
                meshCombiner.GenerateLodGroup();
                EditorUtility.SetDirty(meshCombiner.gameObject);
            }
            EditorGUI.EndDisabledGroup();
        }



    }
}