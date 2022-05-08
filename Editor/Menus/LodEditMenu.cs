using PipeBuilder.Editor.Settings;
using UnityEditor;
using UnityEngine;

namespace PipeBuilder.Editor.Menus
{
    public class LodEditMenu : PipeEditMenu
    {
        public LodEditMenu(string name, PipeBuilder pipeBuilder, PipeBuilderEditorSettings settings) : base(name, pipeBuilder, settings)
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
            DrawUILine(Color.gray);
            DrawLodCount();
            DrawLodDegradeStep();
            GUILayout.BeginHorizontal();
            AskForLodGenerate();
            AskForLodsDestroy();
            GUILayout.EndHorizontal();
            AskForLodAssignment();
            DrawLodInfos();
        }

        public override void DrawScene()
        {
        }

        private void DrawLodCount()
        {
            var count = pipeBuilder.LODCount;
            EditorGUI.BeginChangeCheck();
            count = EditorGUILayout.IntSlider("LOD count:", count, 1, pipeBuilder.MaxLodCount);
            if (EditorGUI.EndChangeCheck())
            {
                pipeBuilder.LODCount = count;
                EditorUtility.SetDirty(pipeBuilder);
            }
        }

        private void DrawLodDegradeStep()
        {
            var step = pipeBuilder.LODDegradeStep;
            EditorGUI.BeginChangeCheck();
            step = EditorGUILayout.IntSlider("LOD degrade:", step, 0, pipeBuilder.MaxDegradeStep);
            if (EditorGUI.EndChangeCheck())
            {
                pipeBuilder.LODDegradeStep = step;
                EditorUtility.SetDirty(pipeBuilder);
            }
        }

        private void DrawLodInfos()
        {
            var infos = pipeBuilder.LODInfos;
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.indentLevel = 1;
            for (var i = 0; i < infos.Count; i++)
            {
                var info = infos[i];
                EditorGUILayout.BeginVertical("box");
                GUILayout.Label($"LOD[{i}]");
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Detail:");
                EditorGUILayout.DelayedIntField(info.circleDetail);
                GUILayout.Label("Triangles:");
                EditorGUILayout.DelayedIntField(info.trianglesAmount);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel = 0;
        }

        private void AskForLodGenerate()
        {
            if (GUILayout.Button("Generate LODs"))
            {
                var saveMeshes = EditorUtility.DisplayDialog("Pipe Builder", "Save meshes as assets?", "Yes", "No");
                var path = string.Empty;
                if (saveMeshes)
                    path = EditorUtility.SaveFolderPanel("Pipe Builder - target folder", string.Empty, string.Empty);
                pipeBuilder.GenerateLODs(path);
                EditorUtility.SetDirty(pipeBuilder);
                GUIUtility.ExitGUI();
            }
        }

        private void AskForLodsDestroy()
        {
            if (GUILayout.Button("Destroy LODs"))
            {
                pipeBuilder.DestroyAllLods();
                EditorUtility.SetDirty(pipeBuilder);
            }

        }

        private void AskForLodAssignment()
        {
            if (GUILayout.Button("Assign LOD Group"))
            {
                pipeBuilder.GenerateLodGroup();
                EditorUtility.SetDirty(pipeBuilder.gameObject);
            }
        }
    }
}