using PipeBuilder.Editor.Settings;
using PipeBuilder.Lines;
using UnityEditor;
using UnityEngine;

namespace PipeBuilder.Editor.Menus
{
    public class MeshEditMenu : PipeEditMenu
    {
        private GUIStyle labelStyle;
        private bool manualEdit;


        private GUIStyle LabelStyle
        {
            get
            {
                if (labelStyle is null)
                {
                    labelStyle = new GUIStyle();
                    labelStyle.normal.textColor = Color.yellow;
                    labelStyle.fontStyle = FontStyle.Bold;
                    labelStyle.normal.background = Texture2D.grayTexture;
                }
                return labelStyle;
            }
        }
        
        public MeshEditMenu(string name, PipeBuilder pipeBuilder, PipeBuilderEditorSettings settings) : base(name,
            pipeBuilder, settings)
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
            EditorGUI.BeginChangeCheck();
            pipeBuilder.circleDetails = EditorGUILayout.IntSlider("Pipe circle detail", pipeBuilder.circleDetails, 3, 60);
            pipeBuilder.generateOuterSide = EditorGUILayout.Toggle("Generate outer side", pipeBuilder.generateOuterSide);
            pipeBuilder.generateInnerSide = EditorGUILayout.Toggle("Generate inner side", pipeBuilder.generateInnerSide);
            pipeBuilder.generateEdges = EditorGUILayout.Toggle("Generate edges", pipeBuilder.generateEdges);
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(pipeBuilder, "Change of details");
                pipeBuilder.LODDegradeStep = Mathf.Clamp(pipeBuilder.LODDegradeStep, 0, pipeBuilder.MaxDegradeStep);
                pipeBuilder.LODCount = Mathf.Clamp(pipeBuilder.LODCount, 1, pipeBuilder.MaxLodCount);
                pipeBuilder.RecalculateLODsInfo();
                pipeBuilder.RebuildPreviewMesh();
                EditorUtility.SetDirty(pipeBuilder);
            }
            DrawUILine(Color.gray);
            DrawDefaultRadiusSettings(pipeBuilder);
            DrawUILine(Color.gray);
            manualEdit = GUILayout.Toggle(manualEdit, "Control nodes", "Button");
            if (manualEdit)
                DrawControlNodesManualEdit(pipeBuilder);

        }

        public override void DrawScene()
        {
            DrawControlNodes(pipeBuilder.ControlLine, settings.DrawSettings);
            DrawChordeLineAndNodes(pipeBuilder, settings.DrawSettings);
            DrawNodesInfo(pipeBuilder.ControlLine);
        }

        private void DrawDefaultRadiusSettings(PipeBuilder pipeBuilder)
        {
            var radiusSettings = pipeBuilder.DefaultControlLineRadiusSettings;
            EditorGUI.BeginChangeCheck();
            radiusSettings.OuterRadius = EditorGUILayout.Slider("Outer radius", radiusSettings.OuterRadius, 0f, 30f);
            radiusSettings.InnerRadius = EditorGUILayout.Slider("Inner radius", radiusSettings.InnerRadius, 0f, 30f);
            pipeBuilder.rotation = EditorGUILayout.Slider("Extra rotation", pipeBuilder.rotation, -180f, 180f);
            if (EditorGUI.EndChangeCheck())
            {
                if (pipeBuilder.drawGizmosMesh || pipeBuilder.previewMeshFilter)
                    pipeBuilder.RebuildPreviewMesh();
                EditorUtility.SetDirty(pipeBuilder);
            }
        }


        private void DrawControlNodesManualEdit(PipeBuilder pipeBuilder)
        {
            var nodes = pipeBuilder.ControlLine.ControlNodes;
            DrawUILine(Color.gray, 1);
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                GUILayout.Label($"Node[{i}]");
                var useDefault = node.UseDefaultRadiusSettings;

                GUILayout.BeginHorizontal();
                GUILayout.Space(30f);
                GUILayout.BeginVertical();

                EditorGUI.BeginChangeCheck();
                useDefault = GUILayout.Toggle(useDefault, "Use default settings", "Button");
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(pipeBuilder, "Undo usage of default");
                    node.UseDefaultRadiusSettings = useDefault;
                    if (pipeBuilder.drawGizmosMesh || pipeBuilder.previewMeshFilter)
                        pipeBuilder.RebuildPreviewMesh();
                    EditorUtility.SetDirty(pipeBuilder);
                }

                if (!useDefault)
                {
                    EditorGUI.BeginChangeCheck();
                    node.OuterRadius = EditorGUILayout.Slider("Outer radius", node.OuterRadius, 0f, 30f);
                    node.InnerRadius = EditorGUILayout.Slider("Inner radius", node.InnerRadius, 0f, 30f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (pipeBuilder.drawGizmosMesh || pipeBuilder.previewMeshFilter)
                            pipeBuilder.RebuildPreviewMesh();
                        EditorUtility.SetDirty(pipeBuilder);
                    }
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();

                DrawUILine(Color.gray, 1);
            }
        }
        
        private void DrawChordeLineAndNodes(PipeBuilder pipeBuilder, DrawSettings drawSettings)
        {
            DrawChordeLine(pipeBuilder.ControlLine, drawSettings);
            DrawChordeNodes(pipeBuilder.ControlLine, drawSettings);
        }

        private void DrawChordeNodes(ControlLine line, DrawSettings settings)
        {
            var colorCache = Handles.color;
            Handles.color = settings.chordeNodesColor;

            for (var i = 0; i < line.ControlNodes.Count; i++)
            {
                var controlNode = line.ControlNodes[i];
                for (var ii = 0; ii < controlNode.ChordeNodes.Count; ii++)
                {
                    var node = controlNode.ChordeNodes[ii];
                    Handles.SphereHandleCap(-1, node, Quaternion.identity, settings.chordeNodesSize, Event.current.type);
                }
            }
            Handles.color = colorCache;
        }

        private void DrawChordeLine(ControlLine line, DrawSettings settings)
        {
            var colorCache = Handles.color;
            Handles.color = settings.chordeLineColor;
            var index = 0;
            var prevNode = line.GetChordeNode(index);
            
            for (var i = 1; i < line.ControlNodes.Count; i++)
            {
                var node = line.ControlNodes[i];
                for (var ii = 0; ii < node.ChordeNodes.Count; ii++)
                {
                    var chordeNode = node.ChordeNodes[ii];
                    Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, settings.chordeLineWidth, prevNode, chordeNode);
                    prevNode = chordeNode;
                }
            }

            Handles.color = colorCache;
        }

        private void DrawControlNodes(ControlLine controlLine, DrawSettings drawSettings)
        {
            var colorCache = Handles.color;
            Handles.color = drawSettings.controlNodesColor;

            var nodes = controlLine.ControlNodes;
            for (var i = 0; i < controlLine.ControlNodes.Count; i++)
            {
                var controlNode = nodes[i];
                var chordeNodes = controlNode.ChordeNodes;
                Handles.color = drawSettings.controlLineColor;
                for (var ii = 0; ii < chordeNodes.Count; ii++)
                {
                    var chordeNode = chordeNodes[ii];
                    Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, drawSettings.chordeLineWidth/2, controlNode.Position, chordeNode.Position);
                }
                Handles.color = drawSettings.controlNodesColor;
                Handles.SphereHandleCap(-1, controlNode.Position, Quaternion.identity, drawSettings.controlNodesSize, EventType.Repaint);

                Handles.Label(nodes[i], $"Node {i}" +
                                        $"\nInner r: {nodes[i].InnerRadius.ToString("0.00")}" +
                                        $"\nOuter r: {nodes[i].InnerRadius.ToString("0.00")}", LabelStyle);
            }
            Handles.color = colorCache;
        }
        
        private void DrawNodesInfo(ControlLine controlLine)
        {
            var nodes = controlLine.ControlNodes;
            for (var i = 0; i < controlLine.ControlNodes.Count; i++)
            {
                Handles.Label(nodes[i], $"Node {i}" +
                                        $"\nInner r: {nodes[i].InnerRadius.ToString("0.00")}" +
                                        $"\nOuter r: {nodes[i].InnerRadius.ToString("0.00")}", LabelStyle);
            }
        }
    }
}