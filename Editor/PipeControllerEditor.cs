using PipeContructor;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Turn on Edit mode to move Control Nodes.
/// While Edit mode:
///                     CTRL + LMB on the line  - add extra Control Node if it meets conditions of padding
///                     SHIFT + LMB on the node - remove control node, if line affter changes meets conditions of padding and angle
/// </summary>

// TODO: change way it`s visualized and proceed checks based on node connections, so later it can have "joints" for several directions, used as path-finding tool and ets.. 
// TODO: way to snap control nodes to borderline positions - currently you cant set node at position which makes exactly MaxAngle degree for itself or neighbours
// TODO: draw transform handles only for currently selected? try our which way is more comfortable

[CustomEditor(typeof(PipeController))]
public class PipeControllerEditor : Editor
{
    //                                                                                                                          TODO: Clear mess


    #region Variables to display
    private float _minAngle;
    private float _defTurnRadius;
    private bool _editMode;
    private int _cornerDetails;

    private bool editMeshProp;
    private float _outerRadius;
    private float _innerRadius;
    private float _thickness;

    private int _pipeDetail;
    #endregion

    #region Variables used for local calculations

    #endregion

    #region Tech variables
    private PipeController controller;
    private List<ControlNode> _nodeList;                                    // Refference to the List of main control nodes
    private List<CenterLineNode> _centerLine;                               // Refference to the list of central line, based on main, but modified by smoothed turns

    private Vector3[] _controlNodesGlobalPos;                                // Holds global positions of ControlNodes (main control line) as array. 
                                                                             // Because Handles.DrawPolyLine requests array and we dont want to generate one each frame

    private Vector3[] _centerNodesGlobalPos;                                 // Holds global positions of CenterLine (path and foundation of mesh) as array.
                                                                             // Because Handles.DrawPolyLine requests array and we dont want to generate one each frame

    Event guiEvent;
    private bool drawTransformHandles = true;                               // To hide main handle, when we draw Transform.Handles for each control node

    private float cameraInputMaxDistance = 1500f;                            // Distance of ray from camera, which is checked when mouse input expected
    private float cursorInputTolerance = 0.75f;                             // Tolerance for click-event based methods. User shouldnt be a sniper and aim to click exactly at 1px line
    #endregion

    #region Design variables
    private Color controlLineColor = PipeController.ControlLineColor;       // Color of the main control line
    private float colntolLineWidth = PipeController.ControlLineWidth;       // Width of the main control line
    private Color controlPointColor = PipeController.ControlNodesColor;     // Color of the main control nodes
    private float controlPointSize = PipeController.ControlNodesSize;       // Size of main control nodes

    private Color centerLineColor = PipeController.CenterLineColor;         // Color of the central line
    private float centerLineWidth = PipeController.CenterLineWidth;         // Width of the main control line
    private Color centerPointColor = PipeController.CenterNodesColor;       // Color of the central line points
    private float centerPointsSize = PipeController.CenterNodesSize;        // Size of center-line points

    private float circlesSize = PipeController.CenterLineCircleSize;      // Size of circles arount center line points
    private Color circlesColor = PipeController.CenterLineCircleColor;      // Color of circles arount center line points

    private bool editPrewiewStyle = false;


    private GUIStyle labelStyle;                                                    // Keeps the style of info-boxes for main control nodes
    private GUIStyle style;
    private bool _displayCenter;
    private bool _previewMesh;

    private bool _GenerateOuterSide;
    private bool _GenerateInnerSide;

    private int _lodDecreaseStep;
    private int _lodVariantsCount;

    SerializedProperty _innerMat;
    SerializedProperty _outerMat;
    SerializedProperty _edgesMat;

    Material defaultMaterial;

    #endregion

    private void OnEnable()
    {
        controller = (PipeController)target;
        _nodeList = controller.ControlNodes;

        _centerLine = controller.CenterLinesList[0];
        _displayCenter = controller.displayCenterLine;
        _previewMesh = controller.ViewPreviewMesh;

        _minAngle = controller.MinTurnAngle;
        _defTurnRadius = controller.DefaultTurnRad;
        _cornerDetails = controller.baseCornersDetail;

        _controlNodesGlobalPos = controller.GetGlobalPostitionsControl();
        _centerNodesGlobalPos = controller.GetGlobalPositionsOfCentral(controller.CenterLinesList[0]);

        _outerRadius = controller.OuterRadius;
        _innerRadius = controller.InnerRadius;
        _thickness = controller.OuterRadius - controller.InnerRadius;

        _lodDecreaseStep = controller.lodDecreaseStep;
        _lodVariantsCount = controller.lodVariantsCount;

        _pipeDetail = controller.basePipeDetail - 1;

        _GenerateOuterSide = controller.GenerateOuterSide;
        _GenerateInnerSide = controller.GenerateInnerSide;


        defaultMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");


        controller.innerSideMaterial = controller.innerSideMaterial == null ? defaultMaterial : controller.innerSideMaterial;
        controller.outerSideMaterial = controller.outerSideMaterial == null ? defaultMaterial : controller.outerSideMaterial;
        controller.edgesSideMaterial = controller.edgesSideMaterial == null ? defaultMaterial : controller.edgesSideMaterial;

        _innerMat = serializedObject.FindProperty("innerSideMaterial");
        _outerMat = serializedObject.FindProperty("outerSideMaterial");
        _edgesMat = serializedObject.FindProperty("edgesSideMaterial");


    }

    public override void OnInspectorGUI()
    {

        labelStyle = new GUIStyle();
        labelStyle.normal.textColor = Color.green;
        style = new GUIStyle("HelpBox");
        style.richText = true;

        if (controller != null)
        {
            EditorGUI.BeginChangeCheck();

            _previewMesh = GUILayout.Toggle(controller.ViewPreviewMesh, "Preview Mesh", "Button");

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(controller, "Change of preview toggle");
                controller.ViewPreviewMesh = _previewMesh;
            }

            editMeshProp = GUILayout.Toggle(editMeshProp, "Edit mesh properties", "Button");
            if (editMeshProp)
            {
                DrawUILine(Color.grey);

                EditorGUI.BeginChangeCheck();
                GUILayout.BeginHorizontal();
                _GenerateOuterSide = GUILayout.Toggle(controller.GenerateOuterSide, "Generate outer side", "Button");
                _GenerateInnerSide = GUILayout.Toggle(controller.GenerateInnerSide, "Generate inner side", "Button");


                GUILayout.EndHorizontal();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(controller, "Change sides generation parameters");
                    controller.GenerateOuterSide = _GenerateOuterSide;
                    controller.GenerateInnerSide = _GenerateInnerSide;

                    if (_previewMesh)
                    {
                        controller.RebuildPreviewMesh();
                    }
                }
                DrawUILine(Color.grey);


                #region Modify radiuses
                //////////////////////////////////////////////////////////////////////////////////////////////////
                EditorGUI.BeginChangeCheck();
                _outerRadius = EditorGUILayout.Slider("Outer radius of pipe", controller.OuterRadius, 0.2f, 50);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(controller, "Change of outer radius");
                    controller.InnerRadius = _outerRadius - _thickness;
                    controller.OuterRadius = _outerRadius;
                    _thickness = controller.OuterRadius - controller.InnerRadius;
                    if (_previewMesh)
                    {
                        controller.RebuildPreviewMesh();
                    }
                }
                //////////////////////////////////////////////////////////////////////////////////////////////////
                EditorGUI.BeginChangeCheck();
                _innerRadius = EditorGUILayout.Slider("Inner radius of pipe: ", controller.InnerRadius, 0.1f, 49.9f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(controller, "Change of inner radius");
                    controller.InnerRadius = _innerRadius;
                    _thickness = _outerRadius - _innerRadius;
                    if (_previewMesh)
                    {
                        controller.RebuildPreviewMesh();
                    }
                }
                //////////////////////////////////////////////////////////////////////////////////////////////////
                EditorGUI.BeginChangeCheck();
                _thickness = EditorGUILayout.Slider("Thickness: ", _thickness, 0.1f, controller.OuterRadius - 0.1f);
                if (EditorGUI.EndChangeCheck())
                {
                    controller.InnerRadius = controller.OuterRadius - _thickness;
                    if (_previewMesh)
                    {
                        controller.RebuildPreviewMesh();
                    }
                }
                //////////////////////////////////////////////////////////////////////////////////////////////////
                #endregion

                DrawUILine(Color.grey);

                EditorGUI.BeginChangeCheck();
                _pipeDetail = EditorGUILayout.IntSlider("Pipe circle detail", controller.basePipeDetail - 1, 3, 30);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(controller, "Change of details");
                    controller.basePipeDetail = _pipeDetail + 1;
                    controller.lodDecreaseStep = Mathf.Clamp(controller.lodDecreaseStep, 0, controller.basePipeDetail - 4);

                    if (controller.lodDecreaseStep == 0)
                        controller.lodVariantsCount = 1;
                    else
                        controller.lodVariantsCount = Mathf.Clamp(controller.lodVariantsCount, 0, (controller.basePipeDetail - 4) / controller.lodDecreaseStep);
                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

                    if (_previewMesh)
                    {
                        controller.RebuildPreviewMesh();
                    }
                }
                //////////////////////////////////////////////////////////////////////////////////////////////////
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.TextArea($"Current triangles amount:", style);
                EditorGUILayout.TextArea($"<b>{controller.ForecastTrianglesAmount(controller.basePipeDetail)}</b>", style);
                EditorGUILayout.EndHorizontal();
                DrawUILine(Color.grey);
                //////////////////////////////////////////////////////////////////////////////////////////////////
                EditorGUI.BeginChangeCheck();
                _lodDecreaseStep = EditorGUILayout.IntSlider("LOD decrease step: ", controller.lodDecreaseStep, 0, (controller.basePipeDetail - 1) - 3);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(controller, "Changed LOD step");
                    controller.lodDecreaseStep = _lodDecreaseStep;
                    if (controller.lodDecreaseStep == 0)
                        controller.lodVariantsCount = 1;
                    else
                        controller.lodVariantsCount = Mathf.Clamp(controller.lodVariantsCount, 0, (controller.basePipeDetail - 4) / controller.lodDecreaseStep + 1);
                }
                EditorGUI.BeginChangeCheck();
                if (controller.lodDecreaseStep > 0)
                    _lodVariantsCount = EditorGUILayout.IntSlider("LOD variants count: ", controller.lodVariantsCount, 0, (controller.basePipeDetail - 4) / controller.lodDecreaseStep + 1);
                else
                    _lodVariantsCount = EditorGUILayout.IntSlider("LOD variants count: ", controller.lodVariantsCount, 0, 1);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(controller, "Changed LOD count");
                    controller.lodVariantsCount = _lodVariantsCount;
                }
                if (controller.lodVariantsCount > 0)
                {
                    for (int i = 0; i < controller.lodVariantsCount; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.TextArea($"LOD {i} triangles:", style);
                        EditorGUILayout.TextArea($"<b>{controller.ForecastTrianglesAmount(controller.basePipeDetail - i * controller.lodDecreaseStep)}</b>", style);
                        EditorGUILayout.EndHorizontal();
                    }
                    if (GUILayout.Button("Generate LOD"))
                    {
                        controller.GenerateLODmeshes(controller.lodDecreaseStep, controller.lodVariantsCount);
                        controller.InstantiateLODGameobjects(controller.lodVariantsCount);
                    }
                    if (GUILayout.Button("Immediate destroy LODs GameObjets"))
                    {
                        controller.DestroyLODgameObjects();
                    }
                    DrawUILine(Color.grey);
                    EditorGUILayout.PropertyField(_outerMat, new GUIContent("Outer Material"));
                    EditorGUILayout.PropertyField(_innerMat, new GUIContent("Inner Material"));
                    EditorGUILayout.PropertyField(_edgesMat, new GUIContent("Edges"));
                }
                //////////////////////////////////////////////////////////////////////////////////////////////////
                DrawUILine(Color.grey);
            }


            EditorGUI.BeginChangeCheck();
            _displayCenter = GUILayout.Toggle(controller.displayCenterLine, "Display central line", "Button");
            _editMode = GUILayout.Toggle(controller.editMode, "Edit control line", "Button");
            if (EditorGUI.EndChangeCheck())
            {
                controller.editMode = _editMode;
                controller.displayCenterLine = _displayCenter;
                HandleUtility.Repaint();
            }

            if (controller.editMode)
            {
                EditorGUI.BeginChangeCheck();
                _minAngle = EditorGUILayout.Slider("Min angle which allowed: ", controller.MinTurnAngle, 0.0f, 180f);
                _defTurnRadius = EditorGUILayout.Slider("Default turn radius: ", controller.DefaultTurnRad, 0.1f, 150f);
                _cornerDetails = EditorGUILayout.IntSlider("Corner detail: ", controller.baseCornersDetail, 1, 10);


                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(controller, "Changed variables of line");
                    controller.MinTurnAngle = _minAngle;
                    controller.DefaultTurnRad = _defTurnRadius;
                    controller.baseCornersDetail = _cornerDetails;

                    controller.BuildTubeCenterLine(controller.CenterLinesList[0], controller.baseCornersDetail);

                    _centerNodesGlobalPos = controller.GetGlobalPositionsOfCentral(controller.CenterLinesList[0]);

                    if (_previewMesh)
                    {
                        controller.RebuildPreviewMesh();
                    }

                    HandleUtility.Repaint();

                }

                EditorGUILayout.TextArea("<size=15><b>Control help:</b></size>\n"
                    + "<size=12><color=blue>Add control nodes:</color> Hold CTRL + LMB click <color=blue>\nRemove control node:</color> Hold SHIFT + LMB click"
                    + "\n<b>New nodes can be added between existing one</b></size>", style);

                if (GUILayout.Button("Defaults"))
                {
                    controller.SetDefaults();
                    HandleUtility.Repaint();
                }

                editPrewiewStyle = GUILayout.Toggle(editPrewiewStyle, "Edit prewiew style", "Button");
                if (editPrewiewStyle)
                {
                    EditorGUI.BeginChangeCheck();

                    AskForStyleChanges();

                    if (EditorGUI.EndChangeCheck())
                    {
                        ApplyStyleChanges();
                    }
                }
            }
            //base.OnInspectorGUI();
        }
        serializedObject.ApplyModifiedProperties();
    }

    void OnSceneGUI()
    {
        if (controller != null && _nodeList != null)
        {
            guiEvent = Event.current;
            if (controller.transform.hasChanged)
            {
                controller.UpdateAllGlobalPositionControl();
                _controlNodesGlobalPos = controller.GetGlobalPostitionsControl();
                _centerNodesGlobalPos = controller.GetGlobalPositionsOfCentral(_centerLine);
            }

            if (_editMode)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                Tools.hidden = true;
                DrawControlLine();
                DrawControlPoints();
                DrawCenterLine(true);
                HandleUtility.Repaint();
                Input();
                DrawPointInfo();
            }
            else
            {
                if (_displayCenter)
                {
                    DrawCenterLine(true);
                }
                Tools.hidden = false;

            }
            HandleUtility.Repaint();

        }
    }

    private void OnDisable()
    {
        Tools.hidden = false;
    }

    /// <summary>
    /// Fires functions which display visual info, recieve input from SceneView, check that input and apply by calling functions from target script
    /// All "check if i can move/delete/add" logic are in them
    /// </summary>
    private void Input()
    {
        if (drawTransformHandles)
            AllowMoveControlPoints();

        if (guiEvent.control && !guiEvent.shift)
        {
            AllowAddControlPoints();
            drawTransformHandles = false;
        }
        else if (!guiEvent.control && guiEvent.shift)
        {
            AllowDeleteContolPoints();
            drawTransformHandles = false;
        }
        else
        {
            drawTransformHandles = true;
        }
    }
    //////////////////////////////////////////////////////////////////////////////////////

    #region Main logic functions

    /// <summary>
    /// Reads mouse input, checks if possible to add extra ControlNode between existing. 
    /// Adds ControlNode to target List if it`s possible (by LMB-click)
    /// </summary>
    private void AllowAddControlPoints()
    {
        // Tumple with way much result output. Consider refactoring
        (bool pointExists, Vector3 pointPosition, int indexOfNext, ControlNode closestNode) closestPoint = GetPointOnLine(_nodeList, cameraInputMaxDistance, cursorInputTolerance);

        if (closestPoint.pointExists)
        {
            bool canAdd = true;
            Handles.color = Color.red;
            if (Vector3.Distance(_nodeList[closestPoint.indexOfNext], closestPoint.pointPosition) < _nodeList[closestPoint.indexOfNext].Padding)
            {
                Handles.SphereHandleCap(-1, closestPoint.pointPosition, Quaternion.identity, controlPointSize, EventType.Repaint);
                Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, colntolLineWidth, _nodeList[closestPoint.indexOfNext], closestPoint.pointPosition);
                canAdd = false;
            }
            if (Vector3.Distance(_nodeList[closestPoint.indexOfNext - 1], closestPoint.pointPosition) < _nodeList[closestPoint.indexOfNext - 1].Padding)
            {
                Handles.SphereHandleCap(-1, closestPoint.pointPosition, Quaternion.identity, controlPointSize, EventType.Repaint);
                Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, colntolLineWidth, _nodeList[closestPoint.indexOfNext - 1], closestPoint.pointPosition);
                canAdd = false;
            }
            // Providing visual where new control point would be
            if (canAdd)
            {
                Handles.color = Color.yellow;
                Handles.SphereHandleCap(-1, closestPoint.pointPosition, Quaternion.identity, controlPointSize, EventType.Repaint);
            }
            HandleUtility.Repaint();

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && canAdd)
            {
                Undo.RecordObject(controller, "Add point");
                controller.AddNode(closestPoint.indexOfNext, closestPoint.pointPosition, true);
                _controlNodesGlobalPos = controller.GetGlobalPostitionsControl();
                _centerNodesGlobalPos = controller.GetGlobalPositionsOfCentral(_centerLine);
            }
        }
    }

    /// <summary>
    /// Displays Transform.handles for each ControlNode from target. Checks if new position of node can be applied
    /// </summary>
    private void AllowMoveControlPoints()
    {
        /// TODO: rewrite for readability in future. Frequently used action transfer to functions

        // For each Control node draw TransformHandles
        Vector3 newPos;
        for (int i = 0; i < _nodeList.Count; i++)
        {
            newPos = Handles.PositionHandle(_nodeList[i], Quaternion.identity);
            // Few check if we should and can move control point:

            // if Handles position changed (differs from original)
            if (newPos != _nodeList[i])
            {
                // We need to check angle only if there are more than 2 points
                if (_nodeList.Count > 2)
                {
                    bool canMove = true;
                    // if current is the first - we need to check angle for next one and padding for it`s pair
                    if (i == 0)
                    {
                        float newAngleForNext = controller.AngleBetween(newPos, _nodeList[i + 1], _nodeList[i + 2]);
                        float newPaddingForNext = ControlNode.CalculatePadding(newAngleForNext, _nodeList[i + 1].TurnRadius);

                        float paddingDistanceWithNext = Vector3.Magnitude(newPos - _nodeList[i + 1]) - newPaddingForNext;
                        float paddingDistanceForNextPair = Vector3.Distance(_nodeList[i + 1], _nodeList[i + 2]) - newPaddingForNext - _nodeList[i + 2].Padding;

                        if (newAngleForNext <= controller.MinTurnAngle || paddingDistanceWithNext < 0 || paddingDistanceForNextPair < 0)
                        {
                            canMove = false;
                        }
                        if (canMove)
                        {
                            controller.MoveControlNode(_nodeList[i], newPos, true);
                        }
                    }
                    // if it`s the last - we need to check previous one and its pair
                    else if (i == _nodeList.Count - 1)
                    {
                        float newAngleForPrevious = controller.AngleBetween(_nodeList[i - 2], _nodeList[i - 1], newPos);
                        float newPaddingForPrev = ControlNode.CalculatePadding(newAngleForPrevious, _nodeList[i - 1].TurnRadius);
                        float paddingDistanceWithPrev = Vector3.Magnitude(newPos - _nodeList[i - 1]) - newPaddingForPrev;
                        float paddingDistanceForPrevPair = Vector3.Distance(_nodeList[i - 1], _nodeList[i - 2]) - newPaddingForPrev - _nodeList[i - 2].Padding;

                        if (newAngleForPrevious < controller.MinTurnAngle || paddingDistanceWithPrev < 0 || paddingDistanceForPrevPair < 0)
                        {
                            canMove = false;
                        }

                        if (canMove)
                        {
                            controller.MoveControlNode(_nodeList[i], newPos, true);
                        }
                    }
                    // if it in the middle, we need to check new angle for CURRENT node and new angle for NEIGHBOURS nodes
                    // and padding for CURRENT + for NEIGHBOURS + for THEIR NEIGHBOURS
                    else
                    {
                        float newAngleForCurrent = controller.AngleBetween(_nodeList[i - 1], newPos, _nodeList[i + 1]);
                        if (newAngleForCurrent >= controller.MinTurnAngle)
                        {
                            float newPadding4Current = ControlNode.CalculatePadding(newAngleForCurrent, _nodeList[i].TurnRadius);

                            // Before checks - lets set values which would pass anyway
                            // Перед проверками допустим, что про изменение возможно
                            float newAngleForNeighbour1 = _nodeList[i - 1].AngleBetweenNeighbors;
                            float newAngleForNeighbour2 = _nodeList[i + 1].AngleBetweenNeighbors;

                            float paddingDistanceWithNext;
                            float paddingDistanceForNextPair = 1;

                            float paddingDistanceWithPrev;
                            float paddingDistanceForPrevPair = 1;

                            float newPaddingForPrev = 0f;
                            float newPaddingForNext = 0f;

                            // Then - modify them if its necessary

                            // Check if previous point was not first in line and need angle  and padding recalculation
                            if (i - 2 >= 0)
                            {
                                newAngleForNeighbour1 = controller.AngleBetween(_nodeList[i - 2], _nodeList[i - 1], newPos);
                                newPaddingForPrev = ControlNode.CalculatePadding(newAngleForNeighbour1, _nodeList[i - 1].TurnRadius);
                                paddingDistanceForPrevPair = Vector3.Distance(_nodeList[i - 1], _nodeList[i - 2]) - newPaddingForPrev - _nodeList[i - 2].Padding;
                            }
                            // Check if next was not the last in line and need angle recalculation
                            if (i + 2 < _nodeList.Count)
                            {
                                newAngleForNeighbour2 = controller.AngleBetween(newPos, _nodeList[i + 1], _nodeList[i + 2]);
                                newPaddingForNext = ControlNode.CalculatePadding(newAngleForNeighbour2, _nodeList[i + 1].TurnRadius);
                                paddingDistanceForNextPair = Vector3.Distance(_nodeList[i + 1], _nodeList[i + 2]) - newPaddingForNext - _nodeList[i + 2].Padding;
                            }
                            paddingDistanceWithNext = Vector3.Magnitude(newPos - _nodeList[i + 1]) - newPaddingForNext - newPadding4Current;
                            paddingDistanceWithPrev = Vector3.Magnitude(newPos - _nodeList[i - 1]) - newPaddingForPrev - newPadding4Current;

                            // Check if new angles for NEIGHBOURS match the condition
                            if (newAngleForNeighbour1 >= controller.MinTurnAngle && newAngleForNeighbour2 >= controller.MinTurnAngle
                                && paddingDistanceWithNext > 0f && paddingDistanceForNextPair > 0f
                                && paddingDistanceWithPrev > 0f && paddingDistanceForPrevPair > 0f)
                            {
                                controller.MoveControlNode(_nodeList[i], newPos, true);
                                _controlNodesGlobalPos = controller.GetGlobalPostitionsControl();
                                HandleUtility.Repaint();
                            }
                        }
                    }
                }
                // If there are only 2 control nodes - we can move them free, without any checks. No angles, no padding.
                else
                {
                    controller.MoveControlNode(_nodeList[i], newPos, true);
                    _controlNodesGlobalPos = controller.GetGlobalPostitionsControl();
                    _centerNodesGlobalPos = controller.GetGlobalPositionsOfCentral(_centerLine);
                }
            }
        }
    }

    /// <summary>
    /// Reads mouse input, checks if possible to remove ControlNode.
    /// </summary>
    private void AllowDeleteContolPoints()
    {
        (bool pointExists, Vector3 pointPosition, int indexOfNext, ControlNode closestNode) closestPoint = GetPointOnLine(_nodeList, cameraInputMaxDistance, cursorInputTolerance);

        if (closestPoint.pointExists && Vector3.Distance(closestPoint.pointPosition, closestPoint.closestNode) <= cursorInputTolerance * 1.5f)
        {
            Handles.color = Color.red;
            Handles.SphereHandleCap(-1, closestPoint.closestNode, Quaternion.identity, controlPointSize, EventType.Repaint);

            // Do not delete any nodes if there are only 2 of them. We wont find where to spawn another. Just remove the component at all if you dont need the pipe
            if (_nodeList.Count > 2)
            {
                int indexOfClosestNode = _nodeList.IndexOf(closestPoint.closestNode);
                bool canDeleteNode = false;

                // if currently selected node is the first or the last - we can just delete them if we want to, by LMB click
                if ((indexOfClosestNode == 0) || (indexOfClosestNode == _nodeList.Count - 1))
                {
                    canDeleteNode = true;
                }

                // does any of neighbours have any neighbours? So we need to check potencial angle without current node
                else if (indexOfClosestNode - 2 >= 0 || indexOfClosestNode + 2 < _nodeList.Count)
                {
                    bool testPassedForPrev = true;
                    bool testPassedForNext = true;

                    float newPadding4Prev = _nodeList[indexOfClosestNode - 1].Padding;
                    float newPadding4Next = _nodeList[indexOfClosestNode + 1].Padding;


                    // Do we need to test previous node? Checking angle and calculating new potential padding
                    if (indexOfClosestNode - 2 >= 0)
                    {
                        float newAngle4Prev = controller.AngleBetween(_nodeList[indexOfClosestNode - 2], _nodeList[indexOfClosestNode - 1], _nodeList[indexOfClosestNode + 1]);
                        newPadding4Prev = ControlNode.CalculatePadding(newAngle4Prev, _nodeList[indexOfClosestNode - 1].TurnRadius);

                        if (newAngle4Prev < controller.MinTurnAngle)
                        {
                            Handles.color = Color.red;
                            Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, 10f, _nodeList[indexOfClosestNode - 2], _nodeList[indexOfClosestNode - 1], _nodeList[indexOfClosestNode + 1]);
                            testPassedForPrev = false;
                        }
                    }
                    // Do we need to test next node? Checking angle and calculating new potential padding
                    if (indexOfClosestNode + 2 < _nodeList.Count)
                    {
                        float newAngle4Next = controller.AngleBetween(_nodeList[indexOfClosestNode - 1], _nodeList[indexOfClosestNode + 1], _nodeList[indexOfClosestNode + 2]);
                        newPadding4Next = ControlNode.CalculatePadding(newAngle4Next, _nodeList[indexOfClosestNode + 1].TurnRadius);

                        if (newAngle4Next < controller.MinTurnAngle)
                        {
                            Handles.color = Color.red;
                            Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, 10f, _nodeList[indexOfClosestNode - 1], _nodeList[indexOfClosestNode + 1], _nodeList[indexOfClosestNode + 2]);
                            testPassedForNext = false;
                        }
                    }
                    // Checking if distance between neighbours will fit their padding (changed or unchanged)
                    if (Vector3.Distance(_nodeList[indexOfClosestNode - 1], _nodeList[indexOfClosestNode + 1]) - newPadding4Next - newPadding4Prev < 0)
                    {
                        Handles.color = Color.red;
                        Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, 10f, _nodeList[indexOfClosestNode - 1], _nodeList[indexOfClosestNode + 1], _nodeList[indexOfClosestNode + 2]);
                        testPassedForNext = false;
                    }

                    if (testPassedForNext && testPassedForPrev)
                    {
                        canDeleteNode = true;
                        Handles.color = Color.green;
                        Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, 10f, _nodeList[indexOfClosestNode - 1], _nodeList[indexOfClosestNode + 1]);
                    }
                }
                else
                {
                    canDeleteNode = true;
                    Handles.color = Color.green;
                    Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, 10f, _nodeList[indexOfClosestNode - 1], _nodeList[indexOfClosestNode + 1]);
                }

                if ((guiEvent.type == EventType.MouseDown && guiEvent.button == 0) && canDeleteNode)
                {
                    Undo.RecordObject(controller, "Remove point");
                    controller.DeleteNode(_nodeList[indexOfClosestNode]);
                    _controlNodesGlobalPos = controller.GetGlobalPostitionsControl();
                    _centerNodesGlobalPos = controller.GetGlobalPositionsOfCentral(_centerLine);
                    return;
                }
            }
        }
        HandleUtility.Repaint();
    }
    #endregion

    //////////////////////////////////////////////////////////////////////////////////////

    #region Visualisation functions

    /// <summary>
    /// Displays line between ControlNodes (main control line)
    /// </summary>
    private void DrawControlLine()
    {
        Handles.color = controlLineColor;
        Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, colntolLineWidth, _controlNodesGlobalPos);
    }

    /// <summary>
    /// Displays labels with info about control nodes.
    /// </summary>
    private void DrawPointInfo()
    {
        for (int i = 0; i < _nodeList.Count; i++)
        {
            Handles.Label(_nodeList[i], $"Node {i}\n{_nodeList[i].AngleBetweenNeighbors.ToString("0.00")} \u00B0", labelStyle);
        }
    }

    /// <summary>
    /// Displays Handle-spheres for ControlNodes (main control line). Only for visual
    /// </summary>
    private void DrawControlPoints()
    {
        Handles.color = controlPointColor;
        foreach (ControlNode node in _nodeList)
        {
            Handles.SphereHandleCap(-1, node, Quaternion.identity, controlPointSize, EventType.Repaint);
        }
    }

    private void DrawCenterLine(bool DrawDiscs)
    {
        Handles.color = centerLineColor;
        Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, centerLineWidth, _centerNodesGlobalPos);

        Handles.color = centerPointColor;

        for (int i = 0; i < _centerNodesGlobalPos.Length; i++)
        {
            Handles.SphereHandleCap(-1, _centerNodesGlobalPos[i], Quaternion.identity, centerPointsSize, EventType.Repaint);
        }

        if (DrawDiscs)
        {
            Handles.color = circlesColor;
            foreach (CenterLineNode node in _centerLine)
            {
                Handles.DrawWireDisc(node.GlobalPosition, node.Forward, circlesSize);
                Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, 6f, node.GlobalPosition, node.GlobalPosition + node.Up);
            }
        }

    }

    #endregion

    //////////////////////////////////////////////////////////////////////////////////////

    #region Tools-functions

    /// <summary>
    /// Checks every pair of Control nodes if ray from cursor shoots close enough (tolerance) between them.
    /// </summary>
    /// <param name="nodes">List of ControlNodes to check</param>
    /// <param name="maxCameraDistance">Distance of ray that will be tested</param>
    /// <param name="tolerance">How close enough ray should be to the line between pairs of ControlNodes</param>
    private (bool exists, Vector3 position, int index, ControlNode node) GetPointOnLine(List<ControlNode> nodes, float maxCameraDistance, float tolerance)
    {
        // TODO: consider replacement with HandleUtility.DistanceToPolyLine + HandleUtility.ClosestPointToPolyLine

        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        Vector3 closestOnLine;
        Vector3 closestOnRay;

        for (int i = 1; i < nodes.Count; i++)
        {
            if (ClosestPointsOnTwoLines(out closestOnLine, out closestOnRay, nodes[i], nodes[i].NodePositionGlobal - nodes[i - 1].NodePositionGlobal, ray.origin, ray.direction * maxCameraDistance))
            {
                if (Vector3.Distance(closestOnLine, closestOnRay) < tolerance && IsCBetweenAB(nodes[i - 1], nodes[i], closestOnLine))
                {
                    ControlNode closestNode = Vector3.Distance(closestOnLine, nodes[i]) < Vector3.Distance(closestOnLine, nodes[i - 1]) ? nodes[i] : nodes[i - 1];
                    return (true, closestOnLine, i, closestNode);
                }
            }
        }
        return (false, Vector3.zero, -1, null);
    }

    /// <summary>
    /// Returns false if lines parallel, closestPointLine1 and closestPointLine2 Vector3.Zero
    /// Else returns true, closestPointLine1 and closestPointLine2 assigned closest points on given lines.
    /// </summary>
    private bool ClosestPointsOnTwoLines(out Vector3 closestPointLine1, out Vector3 closestPointLine2, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
    {
        closestPointLine1 = Vector3.zero;
        closestPointLine2 = Vector3.zero;

        float a = Vector3.Dot(lineVec1, lineVec1);
        float b = Vector3.Dot(lineVec1, lineVec2);
        float e = Vector3.Dot(lineVec2, lineVec2);

        float d = a * e - b * b;

        //lines are not parallel
        if (d != 0.0f)
        {

            Vector3 r = linePoint1 - linePoint2;
            float c = Vector3.Dot(lineVec1, r);
            float f = Vector3.Dot(lineVec2, r);

            float s = (b * f - c * e) / d;
            float t = (a * f - c * b) / d;

            closestPointLine1 = linePoint1 + lineVec1 * s;
            closestPointLine2 = linePoint2 + lineVec2 * t;
            return true;
        }

        else
        {
            return false;
        }
    }
    /// <summary>
    /// Check if C is on the line between A and B
    /// </summary>
    private bool IsCBetweenAB(Vector3 A, Vector3 B, Vector3 C)
    {
        return Vector3.Dot((B - A).normalized, (C - B).normalized) < 0f && Vector3.Dot((A - B).normalized, (C - A).normalized) < 0f;
    }

    /// <summary>
    /// Transforms array of Vector3 to Global Positions relative to target.transform
    /// </summary>
    /// <param name="arr">Source array</param>
    /// <returns>Array of transformed Vector3 elements</returns>
    public Vector3[] TransformToGlobal(Vector3[] arr)
    {
        if (controller != null && arr != null)
        {
            Vector3[] result = new Vector3[arr.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = controller.transform.TransformPoint(arr[i]);
            }
            return result;
        }
        else
            return null;
    }

    private void AskForStyleChanges()
    {

        colntolLineWidth = EditorGUILayout.Slider("Size of Control Line ", PipeController.ControlLineWidth, 1.0f, 60f);

        GUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Color of Control Line:");
        controlLineColor = EditorGUILayout.ColorField(PipeController.ControlLineColor);
        GUILayout.EndHorizontal();

        DrawUILine(Color.grey);

        controlPointSize = EditorGUILayout.Slider("Size of Control Nodes ", PipeController.ControlNodesSize, 0.5f, 5f);

        GUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Color of Control Nodes:");
        controlPointColor = EditorGUILayout.ColorField(PipeController.ControlNodesColor);
        GUILayout.EndHorizontal();

        DrawUILine(Color.grey);

        centerLineWidth = EditorGUILayout.Slider("Size of Center Line ", PipeController.CenterLineWidth, 1.0f, 60f);
        GUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Color of Center Line:");
        centerLineColor = EditorGUILayout.ColorField(PipeController.CenterLineColor);
        GUILayout.EndHorizontal();

        DrawUILine(Color.grey);

        centerPointsSize = EditorGUILayout.Slider("Size of Center Line Nodes", PipeController.CenterNodesSize, 0.5f, 5f);
        GUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Color of Center Line Nodes:");
        centerPointColor = EditorGUILayout.ColorField(PipeController.CenterNodesColor);
        GUILayout.EndHorizontal();

        DrawUILine(Color.grey);
        circlesSize = EditorGUILayout.Slider("Size of circles", PipeController.CenterLineCircleSize, 0.0f, 3f);
        GUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Color of circles:");
        circlesColor = EditorGUILayout.ColorField(PipeController.CenterLineCircleColor);
        GUILayout.EndHorizontal();
    }

    private void ApplyStyleChanges()
    {
        PipeController.ControlLineWidth = colntolLineWidth;
        PipeController.ControlLineColor = controlLineColor;

        PipeController.ControlNodesSize = controlPointSize;
        PipeController.ControlNodesColor = controlPointColor;

        PipeController.CenterLineWidth = centerLineWidth;
        PipeController.CenterLineColor = centerLineColor;

        PipeController.CenterNodesSize = centerPointsSize;
        PipeController.CenterNodesColor = centerPointColor;

        PipeController.CenterLineCircleSize = circlesSize;
        PipeController.CenterLineCircleColor = circlesColor;

        HandleUtility.Repaint();
    }

    public static void DrawUILine(Color color, int thickness = 2, int padding = 10)
    {
        Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
        r.height = thickness;
        r.y += padding / 2;
        r.x -= 2;
        r.width += 6;
        EditorGUI.DrawRect(r, color);
    }

    #endregion

    // Used for debugging:
    private void DrawAngleArcs()
    {
        Handles.color = Color.red;
        for (int i = 1; i < _nodeList.Count - 1; i++)
        {
            Vector3 normal = Vector3.Cross(_nodeList[i - 1].NodePositionGlobal - _nodeList[i].NodePositionGlobal, _nodeList[i + 1].NodePositionGlobal - _nodeList[i].NodePositionGlobal);
            Vector3 from = _nodeList[i - 1].NodePositionGlobal - _nodeList[i].NodePositionGlobal;
            Handles.DrawWireArc(_nodeList[i], normal, from, _nodeList[i].AngleBetweenNeighbors, 1f);
            Handles.DrawWireArc(_nodeList[i], normal, from, _nodeList[i].AngleBetweenNeighbors, 1.01f);
        }
    }
    private void drawBiss()
    {
        for (int i = 1; i < _nodeList.Count - 1; i++)
        {
            Vector3 test = Vector3.Normalize(_nodeList[i - 1].NodePositionGlobal - _nodeList[i - 1].NodePositionGlobal);

            float angle = _nodeList[i].AngleBetweenNeighbors;
            test = Quaternion.AngleAxis(angle, test) * test;
            test *= 10f;
            Handles.color = Color.yellow;
            Handles.DrawLine(_nodeList[i], _nodeList[i].NodePositionGlobal + test);

            Vector3 test1 = (_nodeList[i - 1].NodePositionGlobal - _nodeList[i]).normalized;
            Vector3 test2 = (_nodeList[i + 1].NodePositionGlobal - _nodeList[i]).normalized;

            Vector3 test3 = (test1 + test2);

            float halfAngle = _nodeList[i].AngleBetweenNeighbors / 2f;
            float TurnArcCenterDistance = _nodeList[i].TurnRadius / Mathf.Sin(Mathf.Deg2Rad * halfAngle);

            Vector3 turnCenter = _nodeList[i].NodePositionGlobal + test3.normalized * TurnArcCenterDistance;


            Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, 5f, _nodeList[i], _nodeList[i].NodePositionGlobal + test3.normalized * 24f);
            Handles.color = Color.black;
            Handles.SphereHandleCap(-1, turnCenter, Quaternion.identity, controlPointSize, EventType.Repaint);

        }
        Handles.color = Color.yellow;

        foreach (ControlNode node in _nodeList)
        {
            Vector3 pos = controller.transform.TransformPoint(node.TurnArcCenterPos);
            Handles.SphereHandleCap(-1, pos, Quaternion.identity, controlPointSize, EventType.Repaint);
        }
    }
    private void drawCenterLineDebug()
    {
        Vector3[] points = new Vector3[controller.CenterLinesList[0].Count];
        Handles.color = Color.yellow;

        for (int i = 0; i < points.Length; i++)
        {
            Vector3 pos = controller.transform.TransformPoint(controller.CenterLinesList[0][i]);
            Handles.SphereHandleCap(-1, pos, Quaternion.identity, controlPointSize / 2f, EventType.Repaint);
            Handles.DrawWireDisc(pos, controller.CenterLinesList[0][i].Forward, 1.5f);

            points[i] = pos;
        }
        Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, colntolLineWidth / 2f, points);

        foreach (CenterLineNode node in controller.CenterLinesList[0])
        {
            Handles.color = Color.green;
            Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, 6f, controller.transform.TransformPoint(node.LocalPosition), controller.transform.TransformPoint(node.LocalPosition + node.Up));
            Handles.color = Color.blue;
            Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, 6f, controller.transform.TransformPoint(node.LocalPosition), controller.transform.TransformPoint(node.LocalPosition + node.Forward));
            Handles.color = Color.red;
            Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, 6f, controller.transform.TransformPoint(node.LocalPosition), controller.transform.TransformPoint(node.LocalPosition + node.Right));
        }
    }




    //// Not ready yet, kinda buggy and works only for tails
    ///Purpose = to snap nodes to border-positions, if user tries to input with overshoot
    //private Vector3 CalculateForcedPosition(Vector3 inputPosition, Vector3 closestPointPos, Vector3 nextClosestPointPos)
    //{
    //    Vector3 a = inputPosition - closestPointPos;
    //    Vector3 b = nextClosestPointPos - closestPointPos;
    //    Vector3 normal1 = Vector3.Cross(a, b);
    //    Vector3 normal2 = Vector3.Cross(b, normal1);

    //    Vector3 result = closestPointPos + Vector3.Project(a, normal2);
    //    return result;
    //}
}
