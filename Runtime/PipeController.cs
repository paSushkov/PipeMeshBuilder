using System.Collections.Generic;
using UnityEngine;
using PipeContructor;
using System;
using Random = UnityEngine.Random;

[Serializable]
public class PipeController : MonoBehaviour
{
    /// TODO: add check for vertices overlay in corners. Small turn arc + a lot of detail + huge pipe radius = vertcices overlay each other !!!
    /// TODO: add option to loop control line and mesh
    /// TODO: add check on attemt to change min turn radius

    /// Important!!! TODO - forbid to List.Clear. Connections needs to be removed first! Otherwise nodes will remember each other and still exist in memory
    /// WAY TO DO: custom class?
    /// 
    ///INFO:
    /// At first we build a line of ControlNodes, which stores:
    ///                                                        - info about position (local and global)
    ///                                                        - angle between neighbors-nodes
    ///                                                        - turn-arc center position (local)
    ///                                                        - turn-arc radius
    ///                                                        - angle of turn-arc
    ///                                                        - padding (distance from ControlNode, where turn-arc begins)
    ///                                                        - lists of connected ControlNodes - currently not in use. Will be useful for implementation of joint-nodes
    ///
    /// Then we calculate and fill list of CenterLineNodes, which rely on list of ControlNodes and form smooth turn-arcs in corners.
    /// They store:
    ///             - info about position (local and global)
    ///             - directions (forward, up, right) - which are used for generating and positioning of vertices
    ///                 normal Up - is direction of generation of first vertex around node, which we rotate around normal Forward
    ///                 Important !!! Up normals are not literally "UP". it`s just direction for first vertrex around node. Some math applied to make them the most co-directional with neighbours as possiblle
    /// 

    // Currently applied to all nodes which have turn Radius. 
    //TODO: implement ControlNodes selection in inspector, to toggle "on/off" using of default turn-radius and assigning custom value
    private float defaultTurnRad = 2f;

    /// Mesh which is used for preview in SceneView vith Gizmos
    public Mesh previewMesh;

    // List of GameObjects which represents LOD Meshes. 
    // Filled with InstantiateLODGameobjects() - strongly after GenerateLODmeshes() !
    public List<GameObject> lodVariantsList;

    // List of LOD meshes. Filled with GenerateLODmeshes()
    public List<Mesh> lodMeshesList;

    public bool GenerateOuterSide = true;
    public bool GenerateInnerSide = true;

    /// <summary>
    /// Step which will be used to generate LOD meshes. Affects amount of vertices generated around each CenterLineNode
    /// </summary>
    public int lodDecreaseStep = 1;

    /// <summary>
    /// Amount of LOD meshes which would be generated
    /// </summary>
    public int lodVariantsCount = 1;

    public float extraRotation = 0f;

    /// <summary>
    /// Only list with index 0 in use now. Created because other variants of CenterLines (ex. - less points in turns) could be used for LOD generation;
    /// </summary>
    [SerializeReference]
    public List<List<CenterLineNode>> centerLinesList;

    /// <summary>
    /// Use index 0 only. Others are not generated now, but could be used for further LOD optimisation
    /// If main list == null, will autogenerate itself, add one line and call it`s calcilation
    /// </summary>
    public List<List<CenterLineNode>> CenterLinesList
    {
        get
        {
            if (centerLinesList == null)
            {
                centerLinesList = new List<List<CenterLineNode>>();
                List<CenterLineNode> baseList = new List<CenterLineNode>();
                BuildTubeCenterLine(baseList, baseCornersDetail);
                centerLinesList.Add(baseList);
            }
            return centerLinesList;


        }
        private set => centerLinesList = value;
    }

    public Vector2 uvTilingInner;
    public Vector2 uvOffsetInner;
    public Vector2 uvTilingOuter;
    public Vector2 uvOffsetOuter;
    public Vector2 uvTilingEdges;
    public Vector2 uvOffsetEdges;
    
    public bool displayCenterLine = true;

    private bool viewPreviewMesh = false;
    public bool ViewPreviewMesh
    {
        get => viewPreviewMesh;
        set
        {
            viewPreviewMesh = value;
            if (value)
            {
                RebuildPreviewMesh();
                UpdatePrewiewEvent += RebuildPreviewMesh;
            }
            else
            {
                UpdatePrewiewEvent -= RebuildPreviewMesh;
            }
        }
    }


    private float minTurnAngle = 90;
    public float MinTurnAngle
    {
        get => minTurnAngle;
        set
        {
            (float MinAngle, int index) nodeWithMinAngle = FindMinTurnAngle();
            if (value > nodeWithMinAngle.MinAngle)
            {
                Debug.LogWarning($"Attempt to set new minimum turn angle conflicts with current angle for ControlNode {nodeWithMinAngle.index}," +
                    $" which has {nodeWithMinAngle.MinAngle.ToString("0.00")} angle between other nodes");
            }
            else
            {
                minTurnAngle = value;
            }

        }
    }

    /// <summary>
    /// On attempt to change - iterates through all ControlNodes to check if change is possible
    /// </summary>
    public float DefaultTurnRad
    {
        get => defaultTurnRad;
        set
        {
            bool canChange = true;
            foreach (ControlNode node in ControlNodes)
            {
                canChange = IsNewDefaultRadiusAllowed(value, node);
                if (!canChange)
                {
                    return;
                }
            }
            if (canChange)
            {
                defaultTurnRad = value;
                foreach (ControlNode node in ControlNodes)
                {

                    if (node.UseDefaultTurnRadius)
                    {
                        node.TurnRadius = defaultTurnRad;
                        RecalculateTurnArcCenter(node);
                    }
                }
            }

        }
    }
    private float outerRadius = 2.5f;
    /// <summary>
    /// Checks if value > inner radius
    /// </summary>
    public float OuterRadius
    {
        get => outerRadius;
        set
        {
            if (value > InnerRadius)
                outerRadius = value;
        }
    }

    private float innerRadius = 2f;
    /// <summary>
    /// Checks value if it`s not higher than OuterRadius
    /// </summary>
    public float InnerRadius
    {
        get => innerRadius;
        set
        {
            if (value < OuterRadius)
            {
                if (value < 0.1f)
                {
                    innerRadius = 0.1f;
                }
                else
                {
                    innerRadius = value;
                }
            }
        }
    }

    public int basePipeDetail = 8;

    public delegate void PrewiewUpdater();
    public event PrewiewUpdater UpdatePrewiewEvent;

    // Offset which will be used to generate list of default ControlNodes
    private Vector3 defaultOffset = new Vector3(15f, 0, 0);

    [SerializeField]
    private List<ControlNode> controlNodes;
    public List<ControlNode> ControlNodes
    {
        get
        {
            if (controlNodes == null)
            {
                controlNodes = new List<ControlNode>();
                SetDefaults();
            }
            return controlNodes;
        }
        set
        {
            if (ControlNodes != null)
            {
                foreach (ControlNode node in ControlNodes)
                {
                    node.DisconnetAll();
                    node.DisconnetAllDirected();
                }
                ControlNodes.Clear();
            }
            controlNodes = value;
        }

    }

    public int baseCornersDetail = 3;

    /// <summary>
    /// Default angle which is assigned to the first and last ControlNodes
    /// </summary>
    private float defaultTilesAngle = 180f;

    public Material innerSideMaterial;
    public Material outerSideMaterial;
    public Material edgesSideMaterial;

    /// <summary>
    /// Toggle for inspector
    /// </summary>
    public bool editMode = false;

    /////////////////////////////////////////////////////////////////////////////////

    #region Functions to manage list of ControlNodes 

    /// <summary>
    /// Adds new node to the end of ControlNode list with givel local position.
    /// </summary>
    /// <param name="position"></param>
    public void AddNode(Vector3 position)
    {
        if (ControlNodes != null)
            ControlNodes.Add(new ControlNode(position, this.transform, DefaultTurnRad));


        BuildTubeCenterLine(CenterLinesList[0], baseCornersDetail);
    }

    /// <summary>
    /// Adds new control node to the end of list
    /// </summary>
    /// <param name="position">Postion of new node</param>
    /// <param name="worldPosition">Is position is given in world-space?</param>
    public void AddNode(Vector3 position, bool worldPosition)
    {
        ControlNode node;
        if (worldPosition)
        {
            node = new ControlNode(transform.InverseTransformPoint(position), this.transform, DefaultTurnRad);
        }
        else
        {
            node = new ControlNode(position, this.transform, DefaultTurnRad);
            ControlNodes.Add(node);
        }
        ControlNodes.Add(node);
        RecalculateTurnArcCenter(node);

        BuildTubeCenterLine(CenterLinesList[0], baseCornersDetail);



    }

    /// <summary>
    /// Inserts new control node to the ControlNode list. World position will be auto calculated
    /// </summary>
    /// <param name="index">At which index insert new node</param>
    /// <param name="position">New node local position</param>
    public void AddNode(int index, Vector3 position)
    {
        if (ControlNodes != null)
            ControlNodes.Insert(index, new ControlNode(position, this.transform, DefaultTurnRad));
        RecalculateTurnArcCenter(ControlNodes[index]);

        BuildTubeCenterLine(CenterLinesList[0], baseCornersDetail);
    }

    /// <summary>
    /// Inserts control node to list at given index
    /// </summary>
    /// <param name="index">Index of new node in list</param>
    /// <param name="position">Position of node</param>
    /// <param name="worldPosition">is position is given in global space?</param>
    public void AddNode(int index, Vector3 position, bool worldPosition)
    {
        if (worldPosition)
            ControlNodes.Insert(index, new ControlNode(transform.InverseTransformPoint(position), this.transform, DefaultTurnRad));
        else
            ControlNodes.Insert(index, new ControlNode(position, this.transform, DefaultTurnRad));
        RecalculateTurnArcCenter(ControlNodes[index]);

        BuildTubeCenterLine(CenterLinesList[0], baseCornersDetail);

    }

    /// <summary>
    /// Deletes ControlNode from list, wipes out all connections to the other nodes
    /// and from other nodes to this one.
    /// </summary>
    public void DeleteNode(ControlNode node)
    {
        if (ControlNodes.Contains(node))
        {
            int index = ControlNodes.IndexOf(node);

            node.DisconnetAll();
            node.DisconnetAllDirected();
            foreach (ControlNode my_node in ControlNodes)
            {
                if (my_node.isConnectedDirectlyTo(node))
                    my_node.DisconnectDirected(node);
            }
            ControlNodes.Remove(node);
            SetAngleForTiles(defaultTilesAngle);

            if (index - 1 >= 0)
            {
                RecalculateAngleForNode(ControlNodes[index - 1]);
                RecalculateTurnArcCenter(ControlNodes[index - 1]);
            }
            if (index < ControlNodes.Count)
            {
                RecalculateAngleForNode(ControlNodes[index]);
                RecalculateTurnArcCenter(ControlNodes[index]);
            }
        }


        BuildTubeCenterLine(CenterLinesList[0], baseCornersDetail);
    }

    /// <summary>
    /// Wipes out all connections between Control nodes. Clears ControlNode list.
    /// Generates default nodes.
    /// </summary>
    public void SetDefaults()
    {
        if (ControlNodes.Count > 0)
        {
            foreach (ControlNode node in ControlNodes)
            {
                node.DisconnetAll();
                node.DisconnetAllDirected();
            }
            ControlNodes.Clear();
        }
        MinTurnAngle = 90f;
        baseCornersDetail = 5;
        DefaultTurnRad = 2f;
        ControlNodes.Add(new ControlNode(defaultOffset, transform, DefaultTurnRad));
        ControlNodes.Add(new ControlNode(Vector3.zero + Vector3.up * defaultOffset.magnitude, transform, DefaultTurnRad));
        ControlNodes.Add(new ControlNode(-defaultOffset, transform, DefaultTurnRad));
        SetAngleForTiles(defaultTilesAngle);
        RecalculateAngleAround(ControlNodes[1]);
        RecalculateTurnArcCenter(ControlNodes[1]);


        BuildTubeCenterLine(CenterLinesList[0], baseCornersDetail);



    }
    #endregion

    /////////////////////////////////////////////////////////////////////////////////

    #region Functions to manage ControlNodes in list

    /// <summary>
    /// Moves Control Node to new position
    /// </summary>
    /// <param name="node">Which node</param>
    /// <param name="newPosition">New position for node</param>
    /// <param name="isGlobalValue">Is new position is given in global space?</param>
    public void MoveControlNode(ControlNode node, Vector3 newPosition, bool isGlobalValue)
    {
        if (ControlNodes.Contains(node))
        {
            if (isGlobalValue)
                node.ChangePosition(this.transform.InverseTransformPoint(newPosition), this.transform);
            else
                node.ChangePosition(newPosition, this.transform);

            RecalculateAngleAround(node);
            ReaclulateTurnCenterAround(node);


            BuildTubeCenterLine(CenterLinesList[0], baseCornersDetail);
        }
    }

    /// <summary>
    /// Updates GlobalPosition for each ControlNode and all Central Lines variants
    /// </summary>
    public void UpdateAllGlobalPositionControl()
    {
        foreach (ControlNode node in ControlNodes)
        {
            node.UpdateGlobalPosition(this.transform);
        }
        foreach (List<CenterLineNode> list in CenterLinesList)
        {
            foreach (CenterLineNode node in list)
            {
                node.GlobalPosition = transform.TransformPoint(node.LocalPosition);
            }
        }


    }

    /// <summary>
    /// Returns array of local positions of ControlNodes
    /// </summary>
    public Vector3[] GetLocalPostitionsControl()
    {
        Vector3[] result = new Vector3[ControlNodes.Count];
        for (int i = 0; i < ControlNodes.Count; i++)
        {
            result[i] = ControlNodes[i].NodePositionLocal;
        }
        return result;
    }

    /// <summary>
    /// Returns array of global positions of ControlNodes
    /// </summary>
    public Vector3[] GetGlobalPostitionsControl()
    {
        Vector3[] result = new Vector3[ControlNodes.Count];
        for (int i = 0; i < ControlNodes.Count; i++)
        {
            result[i] = ControlNodes[i].NodePositionGlobal;
        }
        return result;
    }

    /// <summary>
    /// Reacalculates and assigns angle between node and it`s neighbours
    /// </summary>
    public void RecalculateAngleForNode(ControlNode node)
    {
        int index = ControlNodes.IndexOf(node);
        if (index == 0 || index == ControlNodes.Count - 1)
        {
            ControlNodes[index].AngleBetweenNeighbors = defaultTilesAngle;
        }
        else
        {
            ControlNodes[index].AngleBetweenNeighbors = AngleBetween(ControlNodes[index - 1], ControlNodes[index], ControlNodes[index + 1]);
        }

    }

    /// <summary>
    /// Calls RecalculateAngleForNode for current node and its neighbours
    /// </summary>
    /// <param name="node"></param>
    public void RecalculateAngleAround(ControlNode node)
    {
        if (ControlNodes.Contains(node))
        {
            int index = ControlNodes.IndexOf(node);
            RecalculateAngleForNode(node);

            if (index > 0)
                RecalculateAngleForNode(ControlNodes[index - 1]);
            if (index < ControlNodes.Count - 1)
                RecalculateAngleForNode(ControlNodes[index + 1]);
        }
    }

    /// <summary>
    /// Recalculates and assigns new center of turn-arc
    /// </summary>
    public void RecalculateTurnArcCenter(ControlNode node)
    {
        int index = ControlNodes.IndexOf(node);
        if (index == 0 || index == ControlNodes.Count - 1 || node.AngleBetweenNeighbors % 180f == 0f)
        {
            ControlNodes[index].TurnArcCenterPos = ControlNodes[index].NodePositionLocal;
        }
        else
        {
            Vector3 a = (ControlNodes[index - 1].NodePositionLocal - ControlNodes[index].NodePositionLocal).normalized;
            Vector3 b = (ControlNodes[index + 1].NodePositionLocal - ControlNodes[index].NodePositionLocal).normalized;
            Vector3 c = (a + b).normalized;
            Vector3 pos = node.NodePositionLocal + c * node.TurnArcCenterDistance;
            ControlNodes[index].TurnArcCenterPos = pos;
        }
    }

    /// <summary>
    /// Reacalculates and assigns new turn-arc center for node and its neighbours if there are any
    /// </summary>
    private void ReaclulateTurnCenterAround(ControlNode node)
    {
        if (ControlNodes.Contains(node))
        {
            int index = ControlNodes.IndexOf(node);
            RecalculateTurnArcCenter(node);
            if (index > 0)
                RecalculateTurnArcCenter(ControlNodes[index - 1]);
            if (index < ControlNodes.Count - 1)
                RecalculateTurnArcCenter(ControlNodes[index + 1]);
        }
    }

    /// <summary>
    /// Assigns turn angle for the first and the last ControlNode in list
    /// </summary>
    public void SetAngleForTiles(float angle)
    {
        ControlNodes[0].AngleBetweenNeighbors = angle;
        ControlNodes[ControlNodes.Count - 1].AngleBetweenNeighbors = angle;
    }

    /// <summary>
    /// Calculates normalized Vector3.Cross for directions to the next and previous ControlNodes
    /// </summary>
    private Vector3 CalculateNormal(ControlNode node)
    {
        if (ControlNodes.Contains(node))
        {
            int i = ControlNodes.IndexOf(node);
            if (i != 0 && (i != ControlNodes.Count - 1))
            {
                Vector3 baseNormal = Vector3.Cross((ControlNodes[i + 1].NodePositionLocal - ControlNodes[i].NodePositionLocal).normalized, (ControlNodes[i - 1].NodePositionLocal - ControlNodes[i].NodePositionLocal).normalized).normalized;
                return baseNormal;
            }
        }
        return Vector3.zero;
    }

    /// <summary>
    /// Checks if it`s possible for given node to apply new default turn radius
    /// </summary>
    private bool IsNewDefaultRadiusAllowed(float radius, ControlNode node)
    {
        if (ControlNodes.Contains(node))
        {
            int i = ControlNodes.IndexOf(node);


            //If given node is the first one or the last one - it doesn`t have turn radius or padding
            if (i == 0 || (i == ControlNodes.Count - 1))
                return true;
            // if given node is in the middle - we need to check padding for it 
            // and check if padding of neigbours would be changed
            else
            {
                float newPadding = ControlNode.CalculatePadding(node.AngleBetweenNeighbors, radius);

                float prevNodePadding = ControlNodes[i - 1].Padding;
                float nextNodePadding = ControlNodes[i + 1].Padding;

                if (ControlNodes[i - 1].UseDefaultTurnRadius)
                    prevNodePadding = ControlNode.CalculatePadding(ControlNodes[i - 1].AngleBetweenNeighbors, radius);

                if (ControlNodes[i + 1].UseDefaultTurnRadius)
                    prevNodePadding = ControlNode.CalculatePadding(ControlNodes[i + 1].AngleBetweenNeighbors, radius);

                // If distance between current node and its neighbours will not overlap - new default turn radius can be applied for current bode
                if ((Vector3.Distance(ControlNodes[i], ControlNodes[i - 1]) - newPadding - prevNodePadding) < 0
                    || (Vector3.Distance(ControlNodes[i], ControlNodes[i + 1]) - newPadding - nextNodePadding < 0))
                {
                    Debug.LogWarning("Padding conflict at Control node " + i);
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if given custom turn radius is allowed for Node.
    /// </summary>
    private bool IsRadiusAllowed4Single(float radius, ControlNode node)
    {
        if (ControlNodes.Contains(node))
        {
            int i = ControlNodes.IndexOf(node);

            if (i == 0 || (i == ControlNodes.Count - 1))
                return true;
            else
            {
                float padding4Current = ControlNode.CalculatePadding(node.AngleBetweenNeighbors, radius);

                if ((Vector3.Distance(ControlNodes[i], ControlNodes[i - 1]) - padding4Current - ControlNodes[i - 1].Padding) < 0
                    || (Vector3.Distance(ControlNodes[i], ControlNodes[i + 1]) - padding4Current - ControlNodes[i + 1].Padding < 0))
                {
                    return false;
                }
                return true;
            }
        }
        return false;
    }

    #endregion

    /////////////////////////////////////////////////////////////////////////////////

    #region Functions to manage CenterLineNodes

    /// <summary>
    /// Rebuilds list of CenterLine nodes
    /// </summary>
    public void BuildTubeCenterLine(List<CenterLineNode> CentralLineVariant, int CornerDetail)
    {
        if (CentralLineVariant == null)
            CentralLineVariant = new List<CenterLineNode>();
        else
            CentralLineVariant.Clear();

        // At first - declare variables for node`s normals. Normals will be used for vertex generation and visualization
        Vector3 forward;
        Vector3 up;
        Vector3 right;

        Vector3 randomUp;
        Vector3 possibleUp;

        Vector3 cross;

        //Adding first point of the center line:

        forward = (-(ControlNodes[0].NodePositionLocal - ControlNodes[1].NodePositionLocal)).normalized;
        // Generate random normal Up for the first node. Later we will assign normals which based on position and normals of the next one
        randomUp = PipeController.RandomOrthogonal(forward).normalized;

        up = randomUp;
        right = Vector3.Cross(up, forward).normalized;
        CenterLineNode newNode = new CenterLineNode(ControlNodes[0].NodePositionLocal, forward, up, right);
        CentralLineVariant.Add(newNode);

        // Iterating through next ControlNodes, except the last one.
        for (int i = 1; i < ControlNodes.Count - 1; i++)
        {
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////// TODO consider just store normal of ControlNode inside of it
            Vector3 baseNormal = CalculateNormal(ControlNodes[i]);

            Vector3 directionFromTurnCenter;

            Vector3[] possibleUpNormals = new Vector3[2];

            // if CornerDetail is low - lets just take UP normal from previous node
            if (CornerDetail == 1 && !(ControlNodes[i].AngleBetweenNeighbors % 180f == 0f))
            {
                forward = Vector3.Cross(baseNormal.normalized, (ControlNodes[i].NodePositionLocal - ControlNodes[i].TurnArcCenterPos).normalized).normalized;

                if (i > 1)
                {
                    directionFromTurnCenter = (ControlNodes[i].NodePositionLocal - ControlNodes[i].TurnArcCenterPos).normalized;
                    possibleUpNormals[0] = directionFromTurnCenter;
                    possibleUpNormals[1] = Vector3.Cross(forward, directionFromTurnCenter).normalized;
                    up = FindOptimalByDot(possibleUpNormals, CentralLineVariant[CentralLineVariant.Count - 1].Up);
                    right = Vector3.Cross(up, forward).normalized;
                }
                else
                {
                    up = (ControlNodes[i].NodePositionLocal - ControlNodes[i].TurnArcCenterPos).normalized;
                    right = Vector3.Cross(up, forward).normalized;
                }

                newNode = new CenterLineNode(ControlNodes[i].NodePositionLocal, forward, up, right);
                CentralLineVariant.Add(newNode);
            }
            // if it`s node with no turn angle (may be just added node) - lets just take Up of previous
            else if (ControlNodes[i].AngleBetweenNeighbors % 180f == 0f && CornerDetail != 1)
            {
                forward = (ControlNodes[i + 1].NodePositionLocal - ControlNodes[i].NodePositionLocal).normalized;
                up = CentralLineVariant[CentralLineVariant.Count - 1].Up;
                right = Vector3.Cross(up, forward).normalized;
                newNode = new CenterLineNode(ControlNodes[i].NodePositionLocal, forward, up, right);
                CentralLineVariant.Add(newNode);
            }
            else if (CornerDetail == 1 && (ControlNodes[i].AngleBetweenNeighbors % 180f == 0f))
            {
                forward = (ControlNodes[i + 1].NodePositionLocal - ControlNodes[i].NodePositionLocal).normalized;
                up = CentralLineVariant[CentralLineVariant.Count - 1].Up;
                right = CentralLineVariant[CentralLineVariant.Count - 1].Right;
                newNode = new CenterLineNode(ControlNodes[i].NodePositionLocal, forward, up, right);
                CentralLineVariant.Add(newNode);
            }


            // Current control node is in the middle, have turn angle and level of details of turns are not min
            // We need to add a bunch of CentralLineNodes, which will form an arc
            else
            {
                // First point in current arc:
                Vector3 pos;
                // Direction from previous ControlNode to current:
                Vector3 dir = ControlNodes[i].NodePositionLocal - ControlNodes[i - 1].NodePositionLocal;

                // it will be located on current ContrlNode padding distance
                pos = ControlNodes[i - 1].NodePositionLocal + Vector3.ClampMagnitude(dir, dir.magnitude - ControlNodes[i].Padding);

                directionFromTurnCenter = (pos - ControlNodes[i].TurnArcCenterPos).normalized;

                forward = (ControlNodes[i].NodePositionLocal - pos).normalized;

                up = directionFromTurnCenter;
                right = Vector3.Cross(up, forward).normalized;

                // If current Control node is not the first from the middle ones, we CANT just use direction from turn center
                // We need the normals to be co-directed as possible with previous
                if (i > 1)
                {
                    possibleUpNormals[0] = directionFromTurnCenter;
                    possibleUpNormals[1] = Vector3.Cross(forward, directionFromTurnCenter).normalized;

                    //Lets feed this function possible normals and it will choose optimal one and rewrite the normals
                    possibleUp = FindOptimalByDot(possibleUpNormals, CentralLineVariant[CentralLineVariant.Count - 1].Up);
                    up = (CentralLineVariant[CentralLineVariant.Count - 1].Up * 1 + possibleUp).normalized;
                    cross = Vector3.Cross(forward, up);
                    up = Vector3.Cross(cross, forward).normalized;



                    right = Vector3.Cross(up, forward).normalized;
                }
                newNode = new CenterLineNode(pos, forward, up, right);
                CentralLineVariant.Add(newNode);

                // Now we iterating to create bunch of nodes which will form a turn arc
                // by using direction from turn center and turn arc angle
                float angleStep = ControlNodes[i].TurnArcAngle / (CornerDetail - 1);
                Vector3 vectToTurn = pos - ControlNodes[i].TurnArcCenterPos;

                for (int j = 1; j < CornerDetail; j++)
                {
                    Vector3 rotatedVector = Quaternion.AngleAxis(angleStep * j, baseNormal) * vectToTurn;
                    pos = ControlNodes[i].TurnArcCenterPos + rotatedVector;

                    forward = Vector3.Cross(ControlNodes[i].TurnArcCenterPos - pos, baseNormal).normalized;

                    up = rotatedVector.normalized;
                    right = Vector3.Cross(up, forward).normalized;

                    directionFromTurnCenter = rotatedVector.normalized;

                    // Again, if it is not first actual turn, we need the most co-directed with prevoius normals
                    if (i > 1)
                    {
                        possibleUpNormals[0] = directionFromTurnCenter;
                        possibleUpNormals[1] = Vector3.Cross(forward, directionFromTurnCenter).normalized;
                        possibleUp = FindOptimalByDot(possibleUpNormals, CentralLineVariant[CentralLineVariant.Count - 1].Up);
                        up = (CentralLineVariant[CentralLineVariant.Count - 1].Up * 1 + possibleUp).normalized;
                        cross = Vector3.Cross(forward, up);
                        up = Vector3.Cross(cross, forward).normalized;

                        right = Vector3.Cross(up, forward).normalized;
                    }

                    newNode = new CenterLineNode(pos, forward, up, right);
                    CentralLineVariant.Add(newNode);
                }
            }
        }
        //Add the very add last one

        forward = (ControlNodes[ControlNodes.Count - 1].NodePositionLocal - CentralLineVariant[CentralLineVariant.Count - 1].LocalPosition).normalized;
        up = CentralLineVariant[CentralLineVariant.Count - 1].Up;

        cross = Vector3.Cross(forward, up);
        up = Vector3.Cross(cross, forward).normalized;
        right = Vector3.Cross(up, forward).normalized;
        newNode = new CenterLineNode(ControlNodes[ControlNodes.Count - 1].NodePositionLocal, forward, up, right);
        CentralLineVariant.Add(newNode);


        // also, we need to iterate through all of them now to make sure that all random normals wiped out
        // Because if there was nodes without turn-angle after the very first - they took those random normals
        if (CentralLineVariant.Count > 2)
        {
            foreach (CenterLineNode node in CentralLineVariant)
            {
                node.Up = FindNextDifferUP(node, randomUp, CentralLineVariant);
                node.Right = Vector3.Cross(node.Up, node.Forward);
            }
        }


        // Now we assign assign Up and Right normals of first node based on the next one

        forward = CentralLineVariant[0].Forward;

        cross = Vector3.Cross(forward, CentralLineVariant[1].Up);
        up = Vector3.Cross(cross, forward).normalized;
        right = Vector3.Cross(up, forward).normalized;

        CentralLineVariant[0].Up = up;
        CentralLineVariant[0].Right = right;



        UpdateGlobalPositionsOfCenterLine(CentralLineVariant);

        UpdatePrewiewEvent?.Invoke();
    }

    /// <summary>
    /// Returns array of global positions of the CenterLineNodes
    /// </summary>
    public Vector3[] GetGlobalPositionsOfCentral(List<CenterLineNode> CentralLineVariant)
    {

        Vector3[] result = new Vector3[CentralLineVariant.Count];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = CentralLineVariant[i].GlobalPosition;
        }

        return result;
    }

    public void UpdateGlobalPositionsOfCenterLine(List<CenterLineNode> CentralLine)
    {
        foreach (CenterLineNode node in CentralLine)
        {
            node.GlobalPosition = this.transform.TransformPoint(node.LocalPosition);
        }

    }

    /// <summary>
    /// Recursive search for Node.Up normal which differs from given;
    /// Iterates through nodes by index incrementally till the last node
    /// </summary>
    private Vector3 FindNextDifferUP(CenterLineNode node, Vector3 differFrom, List<CenterLineNode> CentralLineVariant)
    {
        int index = CentralLineVariant.IndexOf(node);
        Vector3 result = node.Up;
        if (result == differFrom && index + 1 < CentralLineVariant.Count - 1)
        {
            return FindNextDifferUP(CentralLineVariant[index + 1], differFrom, CentralLineVariant);
        }
        else
            return result;
    }

    #endregion

    /////////////////////////////////////////////////////////////////////////////////

    #region Tools-functions
    /// <summary>
    /// Seeks minimum angle between ContolNodes
    /// </summary>
    public (float minAngle, int index) FindMinTurnAngle()
    {
        float result = 360f;
        int index = -1;
        foreach (ControlNode node in ControlNodes)
        {
            if (node.AngleBetweenNeighbors < result)
            {
                result = node.AngleBetweenNeighbors;
                index = ControlNodes.IndexOf(node);
            }
        }
        return (result, index);
    }

    /// <summary>
    /// Calculates angle between given points
    /// </summary>
    public float AngleBetween(Vector3 point1, Vector3 point2, Vector3 point3)
    {
        Vector3 vector1 = point1 - point2;
        Vector3 vector2 = point3 - point2;
        return Vector3.Angle(vector1, vector2);
    }

    /// <summary>
    /// Returns random orthogonal vor vector
    /// </summary>
    private static Vector3 RandomOrthogonal(Vector3 v)
    {
        Vector3 vPerpendicular = Vector3.one;
        if (v.x != 0)
        {
            vPerpendicular.x = -(v.y * vPerpendicular.y + v.z * vPerpendicular.z) / v.x;
        }
        else
        {
            vPerpendicular.x = Random.Range(0.0f, 1.0f);
        }
        if (v.y != 0)
        {
            vPerpendicular.y = -(v.x * vPerpendicular.x + v.z * vPerpendicular.z) / v.y;
        }
        else
        {
            vPerpendicular.y = Random.Range(0.0f, 1.0f);
        }
        if (v.z != 0)
        {
            vPerpendicular.z = -(v.y * vPerpendicular.y + v.x * vPerpendicular.x) / v.z;
        }
        else
        {
            vPerpendicular.z = Random.Range(0.0f, 1.0f);
        }
        return vPerpendicular;

    }

    /// <summary>
    /// Compares elements of array by Vector3.Dot (including their opposite) 
    /// and returns the most co-directional variant
    /// </summary>
    private Vector3 FindOptimalByDot(Vector3[] arr, Vector3 CompareTo)
    {
        Vector3 optimal = arr[0].normalized;
        float MaxDot = Vector3.Dot(CompareTo, arr[0].normalized);
        for (int i = 0; i < arr.Length; i++)
        {
            (Vector3, Vector3) tuple = (CompareTo, arr[i].normalized);
            if (MaxDot < Vector3.Dot(tuple.Item1, tuple.Item2))
            {
                optimal = tuple.Item2;
                MaxDot = Vector3.Dot(tuple.Item1, tuple.Item2);
            }
            if (MaxDot < Vector3.Dot(tuple.Item1, -tuple.Item2))
            {
                optimal = -tuple.Item2;
                MaxDot = Vector3.Dot(tuple.Item1, -tuple.Item2);
            }
        }
        return optimal;
    }

    #endregion

    /////////////////////////////////////////////////////////////////////////////////

    /// Destructor. We need to wipe out all connections between nodes. Otherwise they will have a link to each other and not collected by GC
    /// Just to be sure, it`s done, if connections are used
    ~PipeController()
    {
        foreach (ControlNode node in ControlNodes)
        {
            node.DisconnetAll();
            node.DisconnetAllDirected();
        }
        ControlNodes.Clear();
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region Functions-tools which used for mesh generation

    public void GeneratePipeVertices(ref List<CenterLineNode> CentralLineVariant, ref int CircleDetail, ref float radius, out Vector3[] Vertices)
    {
        Vertices = new Vector3[CentralLineVariant.Count * (CircleDetail)];
        var angleStep = 360f / (CircleDetail - 1);
        var index = 0;
        for (var i = 0; i < CentralLineVariant.Count; i++)
        {
            for (var j = 0; j < CircleDetail; j++)
            {

                Vertices[index] = CentralLineVariant[i].LocalPosition + (Quaternion.AngleAxis(angleStep * j + extraRotation, CentralLineVariant[i].Forward) * CentralLineVariant[i].Up) * radius;
                index++;
            }
        }
    }

    public void GenerateEdgesVertices(ref List<CenterLineNode> CentralLineVariant, ref int CircleDetail, ref float OuterRadius, ref float InnerRadius, out Vector3[] Edge1Vertices, out Vector3[] Edge2Vertices)
    {
        Edge1Vertices = new Vector3[CircleDetail * 2];
        Edge2Vertices = new Vector3[CircleDetail * 2];

        var angleStep = 360f / (CircleDetail - 1);

        for (var index = 0; index < CircleDetail; index++)
        {
            Edge1Vertices[index] = CentralLineVariant[0].LocalPosition + (Quaternion.AngleAxis(angleStep * index + extraRotation, CentralLineVariant[0].Forward) * CentralLineVariant[0].Up) * InnerRadius;
            Edge1Vertices[index + CircleDetail] = CentralLineVariant[0].LocalPosition + (Quaternion.AngleAxis(angleStep * index + extraRotation, CentralLineVariant[0].Forward) * CentralLineVariant[0].Up) * OuterRadius;
        }

        CenterLineNode lastNode = CentralLineVariant[CentralLineVariant.Count - 1];
        for (var index = 0; index < CircleDetail; index++)
        {

            Edge2Vertices[index] = (lastNode.LocalPosition + (Quaternion.AngleAxis(angleStep * index + extraRotation, lastNode.Forward) * lastNode.Up) * InnerRadius);
            Edge2Vertices[index + CircleDetail] = lastNode.LocalPosition + (Quaternion.AngleAxis(angleStep * index + extraRotation, lastNode.Forward) * lastNode.Up) * OuterRadius;
        }
    }

    public void GenerateInnerPipeTriangles(ref List<CenterLineNode> CentralLineVariant, ref int CircleDetail, out int[] Triangles)
    {
        Triangles = new int[CircleDetail * 6 * (CentralLineVariant.Count - 1)];

        var ySize = CentralLineVariant.Count - 1;
        var xSize = CircleDetail - 1;

        for (int ti = 0, vi = 0, y = 0; y < ySize; y++, vi++)
        {
            for (var x = 0; x < xSize; x++, ti += 6, vi++)
            {
                Triangles[ti] = vi;
                Triangles[ti + 3] = Triangles[ti + 2] = vi + 1;
                Triangles[ti + 4] = Triangles[ti + 1] = vi + xSize + 1;
                Triangles[ti + 5] = vi + xSize + 2;
            }
        }
    }

    public void GenerateOuterPipeTriangles(ref List<CenterLineNode> CentralLineVariant, ref int CircleDetail, out int[] Triangles)
    {
        Triangles = new int[(CircleDetail) * 6 * (CentralLineVariant.Count - 1)];

        var ySize = CentralLineVariant.Count - 1;
        var xSize = CircleDetail - 1;

        for (int ti = 0, vi = 0, y = 0; y < ySize; y++, vi++)
        {
            for (var x = 0; x < xSize; x++, ti += 6, vi++)
            {
                Triangles[ti] = vi;
                Triangles[ti + 1] = vi + 1;
                Triangles[ti + 2] = vi + xSize + 1;
                Triangles[ti + 3] = vi + 1;
                Triangles[ti + 4] = vi + xSize + 2;
                Triangles[ti + 5] = vi + xSize + 1;
            }
        }

    }

    public void GeneratePipeEdgesTriangles(ref int CircleDetail, out int[] FirstEdge, out int[] SecondEdge)
    {
        FirstEdge = new int[CircleDetail * 2 * 6];
        SecondEdge = new int[CircleDetail * 2 * 6];


        var ySize = 1;
        var xSize = CircleDetail - 1;

        for (int ti = 0, vi = 0, y = 0; y < ySize; y++, vi++)
        {
            for (var x = 0; x < xSize; x++, ti += 6, vi++)
            {
                FirstEdge[ti] = vi;
                FirstEdge[ti + 1] = vi + 1;
                FirstEdge[ti + 2] = vi + xSize + 1;
                FirstEdge[ti + 3] = vi + 1;
                FirstEdge[ti + 4] = vi + xSize + 2;
                FirstEdge[ti + 5] = vi + xSize + 1;
            }
        }

        for (int ti = 0, vi = 0, y = 0; y < ySize; y++, vi++)
        {
            for (int x = 0; x < xSize; x++, ti += 6, vi++)
            {
                SecondEdge[ti] = vi;
                SecondEdge[ti + 1] = vi + xSize + 1;
                SecondEdge[ti + 2] = vi + 1;
                SecondEdge[ti + 3] = vi + 1;
                SecondEdge[ti + 4] = vi + xSize + 1;
                SecondEdge[ti + 5] = vi + xSize + 2;
            }
        }
    }

    public void GeneratePipeMesh(ref Mesh MyMesh, bool InnerSide, bool OuterSide, List<CenterLineNode> CentralLineVariant, int CircleDetail)
    {
        var lenghtUVsOriginal = BuildUVsInLenght(ref CentralLineVariant, ref CircleDetail);

        if (MyMesh == null)
            MyMesh = new Mesh();
        else
        {
            MyMesh.Clear();
        }

        if (InnerSide && !OuterSide)
        {
            Vector3[] vertices;
            GeneratePipeVertices(ref CentralLineVariant, ref CircleDetail, ref innerRadius, out vertices);

            int[] triangles;
            GenerateInnerPipeTriangles(ref CentralLineVariant, ref CircleDetail, out triangles);

            FlipUVs_U(ref lenghtUVsOriginal);
            var innerUVs = ApplyTilingAndOffset(ref lenghtUVsOriginal, uvTilingInner, uvOffsetInner);
            BuildPipeMesh(ref MyMesh, ref vertices, ref triangles, ref innerUVs);
        }
        else if (!InnerSide && OuterSide)
        {
            Vector3[] vertices;
            GeneratePipeVertices(ref CentralLineVariant, ref CircleDetail, ref outerRadius, out vertices);

            int[] triangles;
            GenerateOuterPipeTriangles(ref CentralLineVariant, ref CircleDetail, out triangles);

            var outerUVs = ApplyTilingAndOffset(ref lenghtUVsOriginal, uvTilingOuter, uvOffsetOuter);
            BuildPipeMesh(ref MyMesh, ref vertices, ref triangles, ref outerUVs);
        }
        else if (InnerSide && OuterSide)
        {
            Vector3[] outerVertices;
            Vector3[] innerVertices;
            Vector3[] edge1Vertices;
            Vector3[] edge2Vertices;

            GeneratePipeVertices(ref CentralLineVariant, ref CircleDetail, ref innerRadius, out innerVertices);
            GeneratePipeVertices(ref CentralLineVariant, ref CircleDetail, ref outerRadius, out outerVertices);
            GenerateEdgesVertices(ref CentralLineVariant, ref CircleDetail, ref outerRadius, ref innerRadius, out edge1Vertices, out edge2Vertices);

            int[] innerTriangles;
            int[] outTriangles;
            int[] edge1Triangles;
            int[] edge2Triangles;

            var outerUVs = ApplyTilingAndOffset(ref lenghtUVsOriginal, uvTilingOuter, uvOffsetOuter);
            var innerUVs = ApplyTilingAndOffset(ref lenghtUVsOriginal, uvTilingInner, uvOffsetInner);
            FlipUVs_U(ref innerUVs);

            var UVs = new Vector2[outerVertices.Length * 2 + edge1Vertices.Length * 2];
            
            outerUVs.CopyTo(UVs, 0);
            innerUVs.CopyTo(UVs, outerVertices.Length);

            var edgeUVs = new Vector2[edge1Vertices.Length * 2];

            for (var i = 0; i < CircleDetail; i++)
            {
                edgeUVs[i] = new Vector2((float)i / (1-CircleDetail), 0f);
                edgeUVs[i + CircleDetail] = new Vector2((float)i / (1-CircleDetail), 1f);

                edgeUVs[CircleDetail * 4 - i - 1] = new Vector2((float)i / (CircleDetail-1), 0f);
                edgeUVs[CircleDetail * 3 - i - 1] = new Vector2((float)i / (CircleDetail-1), 1f);

            }

            edgeUVs = ApplyTilingAndOffset(ref edgeUVs, uvTilingEdges, uvOffsetEdges);
            edgeUVs.CopyTo(UVs, outerVertices.Length * 2);


            GenerateInnerPipeTriangles(ref CentralLineVariant, ref CircleDetail, out innerTriangles);
            GenerateOuterPipeTriangles(ref CentralLineVariant, ref CircleDetail, out outTriangles);
            GeneratePipeEdgesTriangles(ref CircleDetail, out edge1Triangles, out edge2Triangles);

            BuildCombinedPipeMesh(ref MyMesh, ref outerVertices, ref outTriangles,
                                       ref innerVertices, ref innerTriangles,
                                       ref edge1Vertices, ref edge1Triangles,
                                       ref edge2Vertices, ref edge2Triangles,
                                       ref UVs);


        }
    }


    private Vector2[] BuildUVsInLenght(ref List<CenterLineNode> CentralLineVariant, ref int CircleDetail)
    {

        var UVs = new Vector2[CentralLineVariant.Count * CircleDetail];

        var lenghtOfCentralLine = 0f;

        for (var i = 1; i < CentralLineVariant.Count; i++)
        {
            lenghtOfCentralLine += Vector3.Distance(CentralLineVariant[i - 1].LocalPosition, CentralLineVariant[i].LocalPosition);
        }

        // Setting up first row of UVs
        for (var i = 0; i < CircleDetail; i++)
        {
            UVs[i] = new Vector2(0, 1- (float)i / (float)(CircleDetail));
        }
        // Setting up others, based on central line nodes distance from start
        for (var segment = 1; segment < CentralLineVariant.Count; segment++)
        {
            for (var row = 0; row < CircleDetail; row++)
            {
                var distanceToPrev = Vector3.Distance(CentralLineVariant[segment - 1].LocalPosition, CentralLineVariant[segment].LocalPosition);

                var extraU = distanceToPrev / lenghtOfCentralLine;

                UVs[row + segment * CircleDetail] = new Vector2(UVs[row + segment * CircleDetail - CircleDetail].x + extraU, 1- (float)row / (float)(CircleDetail));
            }
        }

        return UVs;
    }

    private void FlipUVs_U(ref Vector2[] UV)
    {
        for (var i = 0; i < UV.Length; i++)
        {
            UV[i] = new Vector2(UV[i].x, 1 - UV[i].y);
        }
    }

    private Vector2[] ApplyTilingAndOffset(ref Vector2[] original, Vector2 tiling, Vector2 offset)
    {
        var result = new Vector2[original.Length];
        for (var i = 0; i < original.Length; i++)
        {
            result[i] = original[i] * tiling;
            result[i] += offset;
        }
        return result;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Calculates amount of triangles for CenterLinesList[0] and passed amount of vertices around nodes
    /// </summary>
    public int ForecastTrianglesAmount(int detail)
    {
        var lenght = CenterLinesList[0].Count - 1;

        var result = (GenerateOuterSide ? (lenght * 6 * detail) : 0)
                     + (GenerateInnerSide ? (lenght * 6 * detail) : 0)
                     + ((GenerateOuterSide && GenerateInnerSide) ? (detail * 2 * 6) * 2 : 0);

        return result;
    }
    /// <summary>
    /// Builds mesh with one wall only
    /// </summary>
    public void BuildPipeMesh(ref Mesh MeshToBuild, ref Vector3[] Vertices, ref int[] Triangles, ref Vector2[] UV)
    {
        if (MeshToBuild == null)
        {
            MeshToBuild = new Mesh();
        }
        else
        {
            MeshToBuild.Clear();
        }

        MeshToBuild.SetVertices(Vertices);
        MeshToBuild.SetTriangles(Triangles, 0);
        MeshToBuild.SetUVs(0, UV);

        MeshToBuild.RecalculateNormals();
        MeshToBuild.RecalculateTangents();
        MeshToBuild.RecalculateBounds();
        MeshToBuild.Optimize();
    }

    /// <summary>
    /// Builds mesh with inner+outer walls + edges
    /// </summary>
    public void BuildCombinedPipeMesh(ref Mesh MeshToBuild, ref Vector3[] OuterSideVertices, ref int[] OuterSideTriangles,
                                       ref Vector3[] InnerSideVertices, ref int[] InnerSideTriangles,
                                       ref Vector3[] Edge1Vertices, ref int[] Edge1Triangles,
                                       ref Vector3[] Edge2Vertices, ref int[] Edge2Triangles,
                                       ref Vector2[] UV)
    {
        // Setting up Vertices

        if (MeshToBuild == null)
        {
            MeshToBuild = new Mesh();
        }
        else
        {
            MeshToBuild.Clear();
        }


        MeshToBuild.subMeshCount = 3;


        var innerVertLenght = InnerSideVertices.Length;
        var outerVertLenght = OuterSideVertices.Length;
        var edgeVerticesLenght = Edge1Vertices.Length;

        var combinedVerticesList = new List<Vector3>();


        combinedVerticesList.AddRange(OuterSideVertices);
        combinedVerticesList.AddRange(InnerSideVertices);
        combinedVerticesList.AddRange(Edge1Vertices);
        combinedVerticesList.AddRange(Edge2Vertices);

        MeshToBuild.SetVertices(combinedVerticesList);

        MeshToBuild.SetUVs(0, UV);


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Setting up triangles

        MeshToBuild.SetTriangles(OuterSideTriangles, 0);

        for (var i = 0; i < InnerSideTriangles.Length; i++)
        {
            InnerSideTriangles[i] += innerVertLenght;
        }

        MeshToBuild.SetTriangles(InnerSideTriangles, 1);

        var edgeCombinedTriangles = new int[Edge1Triangles.Length + Edge2Triangles.Length];

        for (var i = 0; i < Edge1Triangles.Length; i++)
        {
            edgeCombinedTriangles[i] = Edge1Triangles[i] + innerVertLenght + outerVertLenght;
            edgeCombinedTriangles[i + Edge1Triangles.Length] = Edge2Triangles[i] + innerVertLenght + outerVertLenght + edgeVerticesLenght;
        }
        MeshToBuild.SetTriangles(edgeCombinedTriangles, 2);


        MeshToBuild.RecalculateNormals();
        MeshToBuild.RecalculateTangents();
        MeshToBuild.RecalculateBounds();
        MeshToBuild.Optimize();
    }

    public void RebuildPreviewMesh()
    {
        if (previewMesh == null)
        {
            previewMesh = new Mesh();
            previewMesh.name = "PreviewPipeMesh";
        }
        else
            previewMesh.Clear();

        GeneratePipeMesh(ref previewMesh, GenerateInnerSide, GenerateOuterSide, CenterLinesList[0], basePipeDetail);
    }
    #endregion

    /// <summary>
    /// Fills lodMeshesList list
    /// </summary>
    public void GenerateLODMeshes(int DegradeStep, int Count)
    {
        if (basePipeDetail - (DegradeStep * (Count - 1)) < 4)
        {
            Debug.LogWarning($"You are trying to generate too much LOD meshes or using too high step. Current detail of pipe is {basePipeDetail}(-1).\n" +
                $"You are trying to degrade it down to $(basePipeDetail - (DegradeStep * Count)) with {DegradeStep} step, {Count} times.\n" +
                $"Minimal possible detail is 3.");
            return;
        }
        if (lodMeshesList == null)
            lodMeshesList = new List<Mesh>();
        else
            lodMeshesList.Clear();

        Mesh myLODmesh;
        for (int i = 0; i < Count; i++)
        {
            myLODmesh = new Mesh();
            myLODmesh.name = "PipeLOD_" + i;
            GeneratePipeMesh(ref myLODmesh, GenerateInnerSide, GenerateOuterSide, CenterLinesList[0], basePipeDetail - i * DegradeStep);
            lodMeshesList.Add(myLODmesh);
        }
    }
    /// <summary>
    /// Fills lodVariantsList. Use only after GenerateLODMeshes()!
    /// </summary>
    public void InstantiateLODGameObjects(int count)
    {

        if (lodVariantsList == null)
            lodVariantsList = new List<GameObject>(count);

        var activeCount = lodVariantsList.Count;

        // Clearing excessive LOD-GameObjects        
        if (activeCount > count)
        {
            while (activeCount > count)
            {

                if (lodVariantsList[activeCount-1])
                    DestroyImmediate(lodVariantsList[activeCount-1]);
                
                lodVariantsList.RemoveAt(activeCount-1);
                activeCount = lodVariantsList.Count;
            }
        }
        // Adding some more LOD-GameObjects        
        else if (activeCount < count)
        {
            lodVariantsList.Capacity = count;
            
            var parentGO = gameObject;
            var parentTransform = transform;
            var parentName = parentGO.name;
            var parentLayer = parentGO.layer;
            var parentTag = parentGO.tag;

            for (var i = activeCount; i < count; i++)
            {
                var newLODVariant = new GameObject
                {
                    layer = parentLayer,
                    tag = parentTag, 
                    name = $"{parentName}_LOD[{i}]"
                };

                newLODVariant.transform.position = parentTransform.position;
                newLODVariant.transform.rotation = parentTransform.rotation;
                newLODVariant.transform.parent = parentTransform;
                lodVariantsList.Add(newLODVariant);
            }
        }

        var newLods = new List<LOD>(count);
        var lodStep = 1f / count;

        for (var i = 0; i < lodVariantsList.Count; i++)
        {
            var lodGO = lodVariantsList[i];
            
            if (!lodGO.TryGetComponent(out MeshFilter filter))
                filter = lodGO.AddComponent<MeshFilter>();
            
            if (!lodGO.TryGetComponent(out MeshRenderer renderer))
                renderer = lodGO.AddComponent<MeshRenderer>();
            
            lodVariantsList[i].GetComponent<MeshFilter>().sharedMesh = lodMeshesList[i];

            if (GenerateOuterSide && !GenerateInnerSide)
                renderer.sharedMaterial = outerSideMaterial;
            else if (!GenerateOuterSide && GenerateInnerSide)
                renderer.sharedMaterial = innerSideMaterial;
            else if (GenerateOuterSide && GenerateInnerSide)
                renderer.sharedMaterials = new [] { outerSideMaterial, innerSideMaterial, edgesSideMaterial };

            var newLOD = new LOD
            {
                renderers = new Renderer[] {renderer},
                screenRelativeTransitionHeight = i < count-1 ? lodStep*(count-i-1) : 0.05f
            };
            newLods.Add(newLOD);
        }
        
        if (!TryGetComponent(out LODGroup lodGroup))
            lodGroup = gameObject.AddComponent<LODGroup>();
        
        lodGroup.SetLODs(newLods.ToArray());
        lodGroup.RecalculateBounds();
    }
    /// <summary>
    /// For each GameObject in lodVariantsList clears mesh and calls DestroyImmediate().
    /// Clears lodVariantsList
    /// </summary>
    public void DestroyLODGameObjects()
    {
        foreach (var lodVariantGO in lodVariantsList)
        {
            if (lodVariantGO != null)
            {
                if (lodVariantGO.TryGetComponent<MeshFilter>(out MeshFilter meshFilter))
                {
                    if (meshFilter.sharedMesh != null)
                        meshFilter.sharedMesh.Clear();
                }
            }
            DestroyImmediate(lodVariantGO);
        }
        lodVariantsList.Clear();
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public static Color ControlLineColor = hexToColor("2A17B1");
    public static Color ControlNodesColor = hexToColor("695CC4");
    public static float ControlLineWidth = 15f;
    public static float ControlNodesSize = 1f;

    public static Color CenterLineColor = hexToColor("FF4F00");
    public static Color CenterNodesColor = hexToColor("C53D00");
    public static float CenterLineWidth = 6.5f;
    public static float CenterNodesSize = 0.5f;

    public static Color CenterLineCircleColor = hexToColor("A8F000");
    public static float CenterLineCircleSize = 1f;
    public static Color hexToColor(string hex)
    {
        hex = hex.Replace("0x", "");//in case the string is formatted 0xFFFFFF
        hex = hex.Replace("#", "");//in case the string is formatted #FFFFFF
        byte a = 255;//assume fully visible unless specified in hex
        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        //Only use alpha if the string has enough characters
        if (hex.Length == 8)
        {
            a = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        }
        return new Color32(r, g, b, a);
    }
    private void OnDrawGizmosSelected()
    {
        if (ViewPreviewMesh)
        {
            if (previewMesh == null)
            {
                RebuildPreviewMesh();
            }

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireMesh(previewMesh);
        }

    }

}