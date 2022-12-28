using System;
using PipeBuilder.Editor.Settings;
using PipeBuilder.Lines;
using UnityEditor;
using UnityEngine;

namespace PipeBuilder.Editor.Menus
{
    public class LineEditMenu : PipeEditMenu
    {
        private const float CameraInputMaxDistance = 1500f;
        private const float CursorInputTolerance = 0.75f;

        private bool allowEdit;
        private GUIStyle labelStyle;
        private GUIStyle helpBoxStyle;
        private bool editPreviewStyle;
        private bool manualEdit;
        private bool isInited;

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
        
        private GUIStyle HelpBoxStyle
        {
            get
            {
                if (helpBoxStyle is null)
                {
                    helpBoxStyle = new GUIStyle("HelpBox");
                    helpBoxStyle.richText = true;
                }
                return helpBoxStyle;
            }
        }

        public LineEditMenu(string name, PipeBuilder pipeBuilder, PipeBuilderEditorSettings settings) :
            base(name, pipeBuilder, settings)
        {
        }

        public override void OnEnable()
        {
            if (allowEdit)
                Tools.hidden = true;
        }

        public override void OnDisable()
        {
            Tools.hidden = false;
        }

        public override void DrawInspector()
        {
            DrawUILine(Color.gray);
            EditorGUI.BeginChangeCheck();
            allowEdit = GUILayout.Toggle(allowEdit, "Edit control line", "Button");
            if (EditorGUI.EndChangeCheck())
                Tools.hidden = allowEdit;
            if (allowEdit)
                DrawTooltip();

            DrawControlLineSettings(pipeBuilder.ControlLine);
            DrawDefaultControlNodesSettings(pipeBuilder);

            if (GUILayout.Button("Pivot to center"))
            {
                pipeBuilder.PivotToMeshCenter();
            }
            
            manualEdit = GUILayout.Toggle(manualEdit, "Control nodes", "Button");
            if (manualEdit)
                DrawControlNodesManualEdit(pipeBuilder);

            DrawStyleSettings(settings);
        }

        public override void DrawScene()
        {
            if (allowEdit)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                DrawControlLineAndNodes(pipeBuilder.ControlLine, settings.DrawSettings);
            }

            DrawChordeLineAndNodes(pipeBuilder, settings.DrawSettings);
            if (allowEdit)
            {
                DrawPointInfo(pipeBuilder.ControlLine);
                ProcessInput(pipeBuilder, settings.DrawSettings);
            }
        }

        #region Scene info drawing

        private void DrawControlLineAndNodes(ControlLine line, DrawSettings drawSettings)
        {
            DrawControlLine(line, drawSettings);
            DrawControlNodes(line, drawSettings);
        }

        private void DrawControlNodes(ControlLine line, DrawSettings drawSettings)
        {
            var colorCache = Handles.color;
            Handles.color = drawSettings.controlNodesColor;
            for (var i = 0; i < line.ControlNodes.Count; i++)
            {
                var node = line.ControlNodes[i];
                Handles.SphereHandleCap(-1, node, Quaternion.identity, drawSettings.controlNodesSize,
                    EventType.Repaint);
            }

            Handles.color = colorCache;
        }

        private void DrawControlLine(ControlLine line, DrawSettings drawSettings)
        {
            var colorCache = Handles.color;
            Handles.color = drawSettings.controlLineColor;
            var controlNodes = line.ControlNodes;
            for (var i = 1; i < line.ControlNodes.Count; i++)
                Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, drawSettings.controlLineWidth, controlNodes[i-1], controlNodes[i]);
            Handles.color = colorCache;
        }

        private void DrawChordeLineAndNodes(PipeBuilder pipeBuilder, DrawSettings drawSettings)
        {
            DrawChordeLine(pipeBuilder.ControlLine, drawSettings);
            DrawChordeNodes(pipeBuilder.ControlLine, drawSettings);
            DrawChordeDiscs(pipeBuilder.ControlLine, drawSettings);
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
            var prevNode = line.GetChordeNode(0);
            
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

        private void DrawChordeDiscs(ControlLine line, DrawSettings settings)
        {
            var colorCache = Handles.color;
            var handleColor = new Color();
            var transform = pipeBuilder.transform;
            for (var i = 0; i < line.ControlNodes.Count; i++)
            {
                var controlNode = line.ControlNodes[i];
                for (var ii = 0; ii < controlNode.ChordeNodes.Count; ii++)
                {
                    var node = controlNode.ChordeNodes[ii];
                    Handles.color = settings.chordeLineCircleColor;
                    Handles.DrawWireDisc(node, transform.TransformDirection(node.Forward), settings.chordeLineCircleSize);
                    handleColor = Color.green;
                    handleColor.a = settings.chordeLineCircleColor.a;
                    Handles.color = handleColor;
                    var endPos = node + transform.TransformDirection(node.Up) * settings.chordeLineCircleSize; 
                    Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, 6f, node, endPos);
                    handleColor = Color.blue;
                    handleColor.a = settings.chordeLineCircleColor.a;
                    Handles.color = handleColor;
                    endPos = node + transform.TransformDirection(node.Forward) * settings.chordeLineCircleSize; 
                    Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, 6f, node, endPos);
                    handleColor = Color.red;
                    handleColor.a = settings.chordeLineCircleColor.a;
                    Handles.color = handleColor;
                    endPos = node + transform.TransformDirection(node.Right) * settings.chordeLineCircleSize; 
                    Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, 6f, node, endPos);
                }
            }
            Handles.color = colorCache;
        }

        private void DrawPointInfo(ControlLine controlLine)
        {
            var nodes = controlLine.ControlNodes;
            for (var i = 0; i < controlLine.ControlNodes.Count; i++)
            {
                Handles.Label(nodes[i], $"Node {i}\n{nodes[i].AngleBetweenNeighbors.ToString("0.00")} \u00B0",
                    LabelStyle);
            }
        }

        #endregion


        #region Cursor input processing

        private void ProcessInput(PipeBuilder pipeBuilder, DrawSettings drawSettings)
        {
            var guiEvent = Event.current;

            if (guiEvent.control && !guiEvent.shift)
                AddControlPoints(pipeBuilder, drawSettings, guiEvent);
            else if (!guiEvent.control && guiEvent.shift)
                AllowDeleteControlPoints(pipeBuilder, drawSettings, guiEvent);
            else
                MoveControlPoints(pipeBuilder);
            
            if (guiEvent.control || guiEvent.shift)
                SceneView.RepaintAll();
        }

        private void MoveControlPoints(PipeBuilder pipeBuilder)
        {
            var controlLine = pipeBuilder.ControlLine;
            var nodes = controlLine.ControlNodes;
            for (var i = 0; i < nodes.Count; i++)
            {
                var oldPosition = nodes[i].Position;
                var position = Handles.PositionHandle(oldPosition, GetControlRotation(controlLine, i, settings));
                if (position == oldPosition)
                    continue;
                else if (controlLine.IsMovingAllowed(i, position, true))
                {
                    controlLine.MoveNode(nodes[i], position, true);
                    pipeBuilder.RecalculateLODsInfo();
                    if (pipeBuilder.drawGizmosMesh || pipeBuilder.previewMeshFilter)
                        pipeBuilder.RebuildPreviewMesh();
                    EditorUtility.SetDirty(pipeBuilder);
                    HandleUtility.Repaint();
                }
            }
        }

        private void AddControlPoints(PipeBuilder pipeBuilder, DrawSettings drawSettings, Event guiEvent)
        {
            var controlLine = pipeBuilder.ControlLine;
            var request = new PointOnLineRequest
            {
                line = controlLine,
                maxCameraDistance = CameraInputMaxDistance,
                cursorTolerance = CursorInputTolerance
            };
            var pointOnLineResult = TryGetPointOnLine(request);
            if (!pointOnLineResult.pointExists)
                return;

            var nodes = controlLine.ControlNodes;
            var colorCache = Handles.color;
            var indexForNew = pointOnLineResult.afterPoint ? pointOnLineResult.closestIndex + 1 : pointOnLineResult.closestIndex;
            var canAdd = controlLine.IsAddAllowed(pointOnLineResult.position, indexForNew,
                pipeBuilder.DefaultControlLineTurnSettings.TurnRadius);
            if (canAdd)
            {
                Handles.color = Color.yellow;
                Handles.SphereHandleCap(-1, pointOnLineResult.position, Quaternion.identity, drawSettings.controlNodesSize, EventType.Repaint);
                if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0)
                {
                    controlLine.AddNode(indexForNew, pointOnLineResult.position);
                    pipeBuilder.RecalculateLODsInfo();
                    if (pipeBuilder.drawGizmosMesh || pipeBuilder.previewMeshFilter)
                        pipeBuilder.RebuildPreviewMesh();
                    EditorUtility.SetDirty(pipeBuilder);
                }
            }
            else
            {
                Handles.color = Color.red;
                if (Vector3.Distance(nodes[pointOnLineResult.closestIndex], pointOnLineResult.position) < nodes[pointOnLineResult.closestIndex].Padding)
                    Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, drawSettings.controlLineWidth, pointOnLineResult.position, nodes[pointOnLineResult.closestIndex]);
                
                var secondIndex = pointOnLineResult.afterPoint
                    ? pointOnLineResult.closestIndex + 1
                    : pointOnLineResult.closestIndex - 1;
                
                if (Vector3.Distance(nodes[secondIndex], pointOnLineResult.position) < nodes[secondIndex].Padding)
                    Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, drawSettings.controlLineWidth, pointOnLineResult.position, nodes[secondIndex]);
                
                Handles.SphereHandleCap(-1, pointOnLineResult.position, Quaternion.identity, drawSettings.controlNodesSize, EventType.Repaint);
            }
            Handles.color = colorCache;
        }

        private void AllowDeleteControlPoints(PipeBuilder pipeBuilder, DrawSettings drawSettings, Event guiEvent)
        {
            var controlLine = pipeBuilder.ControlLine;

            var request = new PointOnLineRequest
            {
                line = controlLine,
                maxCameraDistance = CameraInputMaxDistance,
                cursorTolerance = CursorInputTolerance
            };
            var pointOnLineResult = TryGetPointOnLine(request);
            if (!pointOnLineResult.pointExists)
                return;
            var nodes = controlLine.ControlNodes;
            var index = pointOnLineResult.closestIndex;
            var closestNode = nodes[index];
            if (Vector3.Distance(pointOnLineResult.position, closestNode) > CursorInputTolerance)
                return;
            
            var colorCache = Handles.color;
            Handles.color = Color.red;
            Handles.SphereHandleCap(-1, closestNode.Position, Quaternion.identity, drawSettings.controlNodesSize, EventType.Repaint);
            var canDeleteNode = controlLine.IsDeleteAllowed(closestNode, out var testPrev, out var testNext);
            if (canDeleteNode)
            {
                if (index - 1 >= 0 && index + 1 < nodes.Count)
                {
                    Handles.color = Color.green;
                    Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, drawSettings.controlLineWidth, nodes[index - 1], nodes[index + 1]);
                }
                if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0)
                {
                    Undo.RecordObject(pipeBuilder, "Remove point");
                    controlLine.DeleteNode(nodes[index]);
                    pipeBuilder.RecalculateLODsInfo();
                    if (pipeBuilder.previewMeshFilter || pipeBuilder.drawGizmosMesh)
                        pipeBuilder.RebuildPreviewMesh();
                    EditorUtility.SetDirty(pipeBuilder);
                }
            }
            else
            {
                if (index - 1 >= 0 && index + 1 < nodes.Count)
                {
                    Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, drawSettings.controlLineWidth, nodes[index - 1], nodes[index + 1]);
                    if (index-2 >= 0 && !testPrev)
                        Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, drawSettings.controlLineWidth, nodes[index - 2], nodes[index - 1]);
                    if (index+2 < nodes.Count && !testNext)
                        Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, drawSettings.controlLineWidth, nodes[index + 1], nodes[index + 2]);
                }
            }

            Handles.color = colorCache;
        }

        #endregion


        #region Inspector settings drawing

        private void DrawTooltip()
        {
            EditorGUILayout.TextArea("<size=15><b>Control help:</b></size>\n"
                                     + "<size=12><color=blue>Add control nodes:</color> Hold CTRL + LMB click <color=blue>\nRemove control node:</color> Hold SHIFT + LMB click"
                                     + "\n<b>New nodes can be added between existing one</b></size>", HelpBoxStyle);

        }

        private void DrawDefaultControlNodesSettings(PipeBuilder pipeBuilder)
        {
            var controlLine = pipeBuilder.ControlLine;
            var defaultSettings = pipeBuilder.DefaultControlLineTurnSettings;
            var turnRadius = defaultSettings.TurnRadius;
            
            EditorGUI.BeginChangeCheck();
            turnRadius = EditorGUILayout.Slider("Default turn radius:", turnRadius, 0.001f, 50f);
            if (EditorGUI.EndChangeCheck() && controlLine.IsNewDefaultTurnRadiusAllowed(turnRadius))
            {
                defaultSettings.TurnRadius = turnRadius;
                for (var i = 0; i < controlLine.ControlNodes.Count; i++)
                {
                    if (controlLine.ControlNodes[i].UseDefaultTurnSettings)
                        controlLine.RecalculateTurnArcCenterAround(i);

                }
                controlLine.RebuildChordeNodes();
                pipeBuilder.RecalculateLODsInfo();
                if (pipeBuilder.drawGizmosMesh || pipeBuilder.previewMeshFilter)
                    pipeBuilder.RebuildPreviewMesh();
                EditorUtility.SetDirty(pipeBuilder);
            }

            EditorGUI.BeginChangeCheck();
            defaultSettings.TurnDetails = EditorGUILayout.IntSlider("Turn details:", defaultSettings.TurnDetails, 1, 30);
            if (EditorGUI.EndChangeCheck())
            {
                controlLine.RebuildChordeNodes();
                pipeBuilder.RecalculateLODsInfo();
                if (pipeBuilder.drawGizmosMesh || pipeBuilder.previewMeshFilter)
                    pipeBuilder.RebuildPreviewMesh();
                EditorUtility.SetDirty(pipeBuilder);
            }
        }

        private void DrawControlLineSettings(ControlLine controlLine)
        {
            DrawUILine(Color.gray);

            var currentMinAngle = controlLine.MinTurnAngle;
            var allowedAngle = controlLine.MinAllowedAngle;
            
            GUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            allowedAngle = EditorGUILayout.Slider("Min allowed angle",allowedAngle, 0.0f, 180f);
            if (EditorGUI.EndChangeCheck() && allowedAngle > currentMinAngle)
            {
                controlLine.MinAllowedAngle = allowedAngle;
                EditorUtility.SetDirty(pipeBuilder);
            }


            EditorGUI.BeginDisabledGroup(!controlLine.CheckAngle && controlLine.MinTurnAngle < currentMinAngle);
            EditorGUI.BeginChangeCheck();
            controlLine.CheckAngle = GUILayout.Toggle(controlLine.CheckAngle, "Use restriction", "Button");
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(pipeBuilder);
            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.DelayedFloatField("Current MIN angle", currentMinAngle);
            EditorGUI.EndDisabledGroup();
        }

        private void DrawControlNodesManualEdit(PipeBuilder pipeBuilder)
        {
            var controlLine = pipeBuilder.ControlLine;
            var nodes = controlLine.ControlNodes;
            DrawUILine(Color.gray, 1);
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Node[{i}] {nodes[i].AngleBetweenNeighbors.ToString("0.00")}\u00B0");
                if (GUILayout.Button("To pivot"))
                {
                    pipeBuilder.MoveControlNodeToPivot(i);
                    
                    if (pipeBuilder.drawGizmosMesh || pipeBuilder.previewMeshFilter)
                        pipeBuilder.RebuildPreviewMesh();
                    EditorUtility.SetDirty(pipeBuilder);
                }                
                if (GUILayout.Button("Set pivot"))
                {
                    pipeBuilder.SetNodeAsPivot(i);
                    if (pipeBuilder.drawGizmosMesh || pipeBuilder.previewMeshFilter)
                        pipeBuilder.RebuildPreviewMesh();
                    EditorUtility.SetDirty(pipeBuilder);
                }

                GUILayout.EndHorizontal();

                var global = node.Position;
                var local = node.LocalPosition;
                var useDefault = node.UseDefaultTurnSettings;

                GUILayout.BeginHorizontal();
                GUILayout.Space(30f);
                GUILayout.BeginVertical();

                EditorGUI.BeginChangeCheck();
                global = EditorGUILayout.Vector3Field("Global:", global);
                if (EditorGUI.EndChangeCheck())
                {
                    controlLine.MoveNode(i, global);
                    pipeBuilder.RecalculateLODsInfo();
                    if (pipeBuilder.drawGizmosMesh || pipeBuilder.previewMeshFilter)
                        pipeBuilder.RebuildPreviewMesh();
                    EditorUtility.SetDirty(pipeBuilder);
                }

                EditorGUI.BeginChangeCheck();
                local = EditorGUILayout.Vector3Field("Local:", local);
                if (EditorGUI.EndChangeCheck())
                {
                    controlLine.MoveNode(i, local, false);
                    pipeBuilder.RecalculateLODsInfo();
                    if (pipeBuilder.drawGizmosMesh || pipeBuilder.previewMeshFilter)
                        pipeBuilder.RebuildPreviewMesh();
                    EditorUtility.SetDirty(pipeBuilder);
                }

                EditorGUI.BeginChangeCheck();
                useDefault = GUILayout.Toggle(useDefault, "Use default settings", "Button");
                if (EditorGUI.EndChangeCheck())
                {
                    node.UseDefaultTurnSettings = useDefault;
                    controlLine.RecalculateTurnArcCenter(node);
                    controlLine.RebuildChordeNodes();
                    pipeBuilder.RecalculateLODsInfo();
                    if (pipeBuilder.drawGizmosMesh || pipeBuilder.previewMeshFilter)
                        pipeBuilder.RebuildPreviewMesh();
                    EditorUtility.SetDirty(pipeBuilder);
                }

                if (!useDefault)
                {
                    var turnRadius = node.TurnRadius;
                    EditorGUI.BeginChangeCheck();
                    turnRadius = EditorGUILayout.Slider("Turn radius", turnRadius, 0.001f, 150f);
                    if (EditorGUI.EndChangeCheck() &&
                        pipeBuilder.ControlLine.IsTurnRadiusAllowedForNode(turnRadius, node))
                    {
                        node.AssignCustomTurnRadius(turnRadius);
                        controlLine.RecalculateTurnArcCenter(i);
                        controlLine.RebuildChordeNodes();
                        pipeBuilder.RecalculateLODsInfo();
                        if (pipeBuilder.drawGizmosMesh || pipeBuilder.previewMeshFilter)
                            pipeBuilder.RebuildPreviewMesh();
                        EditorUtility.SetDirty(pipeBuilder);
                    }

                    var turnDetails = node.TurnDetails;

                    EditorGUI.BeginChangeCheck();
                    turnDetails = EditorGUILayout.IntSlider("Turn details", turnDetails, 1, 30);
                    if (EditorGUI.EndChangeCheck())
                    {
                        node.AssignCustomTurnDetails(turnDetails);
                        controlLine.RebuildChordeNodes();
                        pipeBuilder.RecalculateLODsInfo();
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

        private void DrawStyleSettings(PipeBuilderEditorSettings settings)
        {
            DrawUILine(Color.gray);
            editPreviewStyle = GUILayout.Toggle(editPreviewStyle, "Edit preview style", "Button");
            if (!editPreviewStyle)
                return;

            var drawSettings = settings.DrawSettings;

            EditorGUI.BeginChangeCheck();

            settings.RotationMode =
                (PivotRotationMode) EditorGUILayout.EnumPopup("Rotation mode", settings.RotationMode);

            DrawUILine(Color.grey);

            drawSettings.controlLineWidth =
                EditorGUILayout.Slider("Size of Control Line ", drawSettings.controlLineWidth, 0.001f, 10f);
            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Color of Control Line:");
            drawSettings.controlLineColor = EditorGUILayout.ColorField(drawSettings.controlLineColor);
            GUILayout.EndHorizontal();

            DrawUILine(Color.grey);

            drawSettings.controlNodesSize =
                EditorGUILayout.Slider("Size of Control Nodes ", drawSettings.controlNodesSize, 0.001f, 5f);
            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Color of Control Nodes:");
            drawSettings.controlNodesColor = EditorGUILayout.ColorField(drawSettings.controlNodesColor);
            GUILayout.EndHorizontal();

            DrawUILine(Color.grey);

            drawSettings.chordeLineWidth =
                EditorGUILayout.Slider("Size of Chorde Line ", drawSettings.chordeLineWidth, 0.001f, 10f);
            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Color of Chorde Line:");
            drawSettings.chordeLineColor = EditorGUILayout.ColorField(drawSettings.chordeLineColor);
            GUILayout.EndHorizontal();

            DrawUILine(Color.grey);

            drawSettings.chordeNodesSize =
                EditorGUILayout.Slider("Size of Chorde Line Nodes", drawSettings.chordeNodesSize, 0.001f, 5f);
            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Color of Chorde Line Nodes:");
            drawSettings.chordeNodesColor = EditorGUILayout.ColorField(drawSettings.chordeNodesColor);
            GUILayout.EndHorizontal();

            DrawUILine(Color.grey);

            drawSettings.chordeLineCircleSize =
                EditorGUILayout.Slider("Size of circles", drawSettings.chordeLineCircleSize, 0.001f, 10f);
            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Color of circles:");
            drawSettings.chordeLineCircleColor = EditorGUILayout.ColorField(drawSettings.chordeLineCircleColor);
            GUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
                settings.Save();
        }

        #endregion


        #region Calculation and utility

        private struct ClosestPointsForTwoLinesResult
        {
            public bool pointExists;
            public Vector3 pointAtLine1;
            public Vector3 pointAtLine2;
        }

        private struct ClosestPointsForTwoLinesRequest
        {
            public Vector3 firstLinePoint;
            public Vector3 firstLineDirection;
            public Vector3 secondLinePoint;
            public Vector3 secondLineDirection;
        }

        private struct PointOnLineRequest
        {
            public ControlLine line;
            public float maxCameraDistance;
            public float cursorTolerance;
        }

        private struct PointOnLineResult
        {
            public bool pointExists;
            public Vector3 position;
            public int closestIndex;
            public bool afterPoint;
        }

        private PointOnLineResult TryGetPointOnLine(PointOnLineRequest request)
        {
            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            var nodes = request.line.ControlNodes;
            var sqrCursorTolerance = request.cursorTolerance * request.cursorTolerance;

            for (var i = 1; i < nodes.Count; i++)
            {
                var requestInfo = new ClosestPointsForTwoLinesRequest
                {
                    firstLinePoint = nodes[i],
                    firstLineDirection = nodes[i].Position - nodes[i - 1].Position,
                    secondLinePoint = ray.origin,
                    secondLineDirection = ray.direction * request.maxCameraDistance
                };

                var closestPointsResult = ClosestPointsOnTwoLines(requestInfo);
                if (!closestPointsResult.pointExists)
                    continue;

                var sqrDistance =
                    Vector3.SqrMagnitude(closestPointsResult.pointAtLine1 - closestPointsResult.pointAtLine2);
                if (sqrDistance > sqrCursorTolerance ||
                    !IsCBetweenAB(nodes[i - 1], nodes[i], closestPointsResult.pointAtLine1))
                    continue;

                var sqrDistanceToCurrent = Vector3.SqrMagnitude(closestPointsResult.pointAtLine1 - nodes[i]);
                var sqrDistanceToPrev = Vector3.SqrMagnitude(closestPointsResult.pointAtLine1 - nodes[i - 1]);
                var indexOfClosest = sqrDistanceToCurrent < sqrDistanceToPrev ? i : i - 1;

                var result = new PointOnLineResult
                {
                    pointExists = true,
                    position = closestPointsResult.pointAtLine1,
                    closestIndex = indexOfClosest,
                    afterPoint = indexOfClosest != i
                };
                return result;
            }

            return new PointOnLineResult {pointExists = false};
        }

        private ClosestPointsForTwoLinesResult ClosestPointsOnTwoLines(ClosestPointsForTwoLinesRequest requestInfo)
        {
            var a = Vector3.Dot(requestInfo.firstLineDirection, requestInfo.firstLineDirection);
            var b = Vector3.Dot(requestInfo.firstLineDirection, requestInfo.secondLineDirection);
            var e = Vector3.Dot(requestInfo.secondLineDirection, requestInfo.secondLineDirection);

            var d = a * e - b * b;

            // Check if lines are parallel
            if (d == 0.0f)
                return new ClosestPointsForTwoLinesResult {pointExists = false};

            var r = requestInfo.firstLinePoint - requestInfo.secondLinePoint;
            var c = Vector3.Dot(requestInfo.firstLineDirection, r);
            var f = Vector3.Dot(requestInfo.secondLineDirection, r);

            var s = (b * f - c * e) / d;
            var t = (a * f - c * b) / d;

            return new ClosestPointsForTwoLinesResult
            {
                pointExists = true,
                pointAtLine1 = requestInfo.firstLinePoint + requestInfo.firstLineDirection * s,
                pointAtLine2 = requestInfo.secondLinePoint + requestInfo.secondLineDirection * t
            };
        }

        private bool IsCBetweenAB(Vector3 a, Vector3 b, Vector3 c)
        {
            return Vector3.Dot((b - a).normalized, (c - b).normalized) < 0f &&
                   Vector3.Dot((a - b).normalized, (c - a).normalized) < 0f;
        }

        private Quaternion GetControlRotation(ControlLine line, int index, PipeBuilderEditorSettings settings)
        {
            switch (settings.RotationMode)
            {
                case PivotRotationMode.World:
                    return Quaternion.identity;
                case PivotRotationMode.Local:
                    return line.PipeBuilder.transform.rotation;
                case PivotRotationMode.Relative:
                    var controlNodes  = line.ControlNodes;
                    if (index + 1 < controlNodes.Count)
                    {
                        var direction = controlNodes[index + 1].Position - controlNodes[index].Position;
                        return Quaternion.LookRotation(direction, Vector3.up);
                    }
                    else if (index > 0)
                        return GetControlRotation(line, index - 1, settings);
                    else
                        return line.PipeBuilder.transform.localRotation;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion
    }
}