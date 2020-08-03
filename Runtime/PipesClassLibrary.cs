using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PipeContructor
{
    [System.Serializable]
    public class ControlNode
    {
        // Does this node should be modified when DefaultTurnRadius modified in holder?
        public bool UseDefaultTurnRadius = true;
        /// Currently used to store angle between 2 neighbours. Will be reworked after "joints" implementation. When it will be necesary.
        [SerializeField]
        private float angleBetweenNeighbors = 180f;
        /// <summary>
        /// !!! On change also: Calculates padding of turn radius, distance to the turn-arc center and turn-arc angle;
        /// </summary>
        public float AngleBetweenNeighbors
        {
            get => angleBetweenNeighbors;
            set
            {
                angleBetweenNeighbors = value;
                if (angleBetweenNeighbors % 180f == 0f)
                {
                    TurnArcCenterDistance = 0f;
                    Padding = 0f;
                    TurnArcCenterPos = nodePositionLocal;
                    TurnArcAngle = 180f;
                }
                else
                {
                    float halfAngle = angleBetweenNeighbors / 2f;
                    TurnArcCenterDistance = TurnRadius / Mathf.Sin(halfAngle * Mathf.Deg2Rad);
                    Padding = TurnArcCenterDistance * Mathf.Cos(halfAngle * Mathf.Deg2Rad);
                    TurnArcAngle = 2f * (180f - 90f - halfAngle);
                }
            }
        }
        // Distance to the center of turn-arc
        [SerializeField]
        private float turnArcCenterDistance = 0f;
        public float TurnArcCenterDistance { get => turnArcCenterDistance; private set => turnArcCenterDistance = value; }

        [SerializeField]
        private Vector3 turnArcCenterPos;
        public Vector3 TurnArcCenterPos { get => turnArcCenterPos; set => turnArcCenterPos = value; }

        [SerializeField]
        private float turnArcAngle = 180f;
        public float TurnArcAngle { get => turnArcAngle; private set => turnArcAngle = value; }

        [SerializeField]
        private float turnRadius = 3f;
        /// <summary>
        /// !!! On change also: Calculates padding of turn radius (Padding), distance to the turn-arc center (TurnArcCenterDistance) and turn-arc angle (TurnArcAngle)
        /// </summary>
        public float TurnRadius
        {
            get => turnRadius;
            set
            {
                turnRadius = value;
                if (angleBetweenNeighbors % 180f == 0f)
                {
                    TurnArcCenterDistance = 0f;
                    Padding = 0f;
                    TurnArcAngle = 180f;
                }
                else
                {
                    float halfAngle = angleBetweenNeighbors / 2f;
                    TurnArcCenterDistance = TurnRadius / Mathf.Sin(halfAngle * Mathf.Deg2Rad);
                    Padding = TurnArcCenterDistance * Mathf.Cos(halfAngle * Mathf.Deg2Rad);
                    TurnArcAngle = 2f * (180f - 90f - halfAngle);
                }
            }
        }

        [SerializeField]
        private float padding = 0f;
        public float Padding { get => padding; private set => padding = value; }

        [SerializeField]
        private Vector3 nodePositionLocal;
        public Vector3 NodePositionLocal { get => nodePositionLocal; private set => nodePositionLocal = value; }

        [SerializeField]
        private Vector3 nodePositionGlobal;
        public Vector3 NodePositionGlobal { get => nodePositionGlobal; private set => nodePositionGlobal = value; }

        /// <summary>
        /// Returs GlobalPosition of ControlNode
        /// </summary>
        public static implicit operator Vector3(ControlNode node) => node.NodePositionGlobal;
        public void ChangePosition(Vector3 position, Transform nodeHolderTransform)
        {
            NodePositionLocal = position;
            NodePositionGlobal = nodeHolderTransform.TransformPoint(position);
        }
        public void UpdateGlobalPosition(Transform nodeHolderTransform)
        {
            NodePositionGlobal = nodeHolderTransform.TransformPoint(NodePositionLocal);
        }

        [SerializeField]
        private NodeConnectionList connections;
        [SerializeField]
        private NodeConnectionList directedConections;
        public NodeConnectionList Connections { get => connections; private set => connections = value; }
        public NodeConnectionList DirectedConections { get => directedConections; private set => directedConections = value; }

        // Constructors
        public ControlNode(Vector3 position, float turnRaduis)
        {
            NodePositionLocal = position;
            TurnArcCenterPos = position;
            connections = new NodeConnectionList(this);
            directedConections = new NodeConnectionList(this);
            TurnRadius = turnRaduis;
        }
        public ControlNode(Vector3 position, Transform holderTransform, float turnRaduis)
        {
            NodePositionLocal = position;
            TurnArcCenterPos = position;
            UpdateGlobalPosition(holderTransform);
            TurnRadius = turnRaduis;
            connections = new NodeConnectionList(this);
            directedConections = new NodeConnectionList(this);
        }

        #region Next functions are used in code, but does not used for any logic right now:
        //They will be used when joints will be implemented
        #region Common graph Connection variables and related functions
        private void ConnectTo(ControlNode node)
        {
            Connections.AddConnectionTo(node);
        }
        private void DisconnectFrom(ControlNode node)
        {
            Connections.RemoveConnectionTo(node);
        }
        public void Connect(ControlNode node)
        {
            this.ConnectTo(node);
            node.ConnectTo(this);
        }
        public void Disconnect(ControlNode node)
        {
            this.DisconnectFrom(node);
            node.DisconnectFrom(this);
        }
        public void DisconnetAll()
        {
            if (Connections != null)
                Connections.DisconnectAll(true);
        }
        public bool isConnectedTo(ControlNode node)
        {
            return Connections.Contains(node);
        }
        #endregion
        #region Directed graph Connection variables and related functions
        public void ConnectDirected(ControlNode node)
        {
            if (!node.DirectedConections.Contains(this))
                DirectedConections.AddConnectionTo(node);
        }
        public void DisconnectDirected(ControlNode node)
        {
            DirectedConections.RemoveConnectionTo(node);
        }
        public void DisconnetAllDirected()
        {
            DirectedConections.Clear();
        }
        public bool isConnectedDirectlyTo(ControlNode node)
        {
            return DirectedConections.Contains(node);
        }
        #endregion
        #endregion

        public static float CalculatePadding(float angle, float radius)
        {
            if (angle % 180f == 0f)
                return 0f;

            float halfAngle = angle / 2f;
            float circleCenterDistance = radius / Mathf.Sin(halfAngle * Mathf.Deg2Rad);
            float padding = circleCenterDistance * Mathf.Cos(halfAngle * Mathf.Deg2Rad);

            return padding;
        }
    }
    
    /// <summary>
    /// To store connection info (to which node it links... other info like lenght can be added later)
    /// </summary>
    [Serializable]
    public class NodeConnection
    {
        [SerializeReference]
        private ControlNode connectedNode;
        public ControlNode ConnectedNode { get => connectedNode; set => connectedNode = value; }
        public NodeConnection(ControlNode node)
        {
            ConnectedNode = node;
        }
    }
    /// <summary>
    /// Stores List of NodeConnection. Created for further scalability and opportunity to implement extra logic which could be above standart List functions
    /// </summary>
    [Serializable]
    public class NodeConnectionList : IEnumerable
    {
        [SerializeReference]
        private ControlNode owner;
        public ControlNode Owner { get => owner; private set => owner = value; }
        
        [SerializeField]
        private List<NodeConnection> connectionList;
        public List<NodeConnection> ConnectionList { get => connectionList; private set => connectionList = value; }

        /// <summary>
        /// Get NodeConnection which is connected to node. Returns null if there are no such NodeConnection
        /// </summary>
        public NodeConnection this[ControlNode node]
        {
            get
            {
                foreach (NodeConnection connection in ConnectionList)
                {
                    if (connection.ConnectedNode == node)
                        return connection;
                }
                return null;
            }
        }
        /// <summary>
        /// Check if ConnectionNode List contains node
        /// </summary>
        public bool Contains(ControlNode node)
        {
            foreach (NodeConnection connection in ConnectionList)
            {
                if (connection.ConnectedNode == node)
                    return true;
            }
            return false;
        }
        /// <summary>
        /// Add node to NodeConnection List if it`s not alredy there
        /// </summary>
        public void AddConnectionTo(ControlNode node)
        {
            if (Contains(node))
                return;
            ConnectionList.Add(new NodeConnection(node));
        }
        /// <summary>
        /// Removes connection if it exists.
        /// </summary>
        public void RemoveConnectionTo(ControlNode node)
        {
            if (Contains(node))
                connectionList.Remove(this[node]);
        }
        /// <summary>
        /// If DualSide == true: removes all coonections, FROM and TO owner. If DualSide == false: just clears current list;
        /// </summary>
        public void DisconnectAll(bool DualSide)
        {
            if (DualSide && ConnectionList!=null)
            {
                foreach (NodeConnection connection in ConnectionList)
                {
                    connection.ConnectedNode.Connections.RemoveConnectionTo(owner);
                }
            }
            ConnectionList.Clear();
        }
        public void Clear()
        {
            ConnectionList.Clear();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public NodeConnectionList(ControlNode Owner)
        {
            ConnectionList = new List<NodeConnection>();
            this.Owner = Owner;
        }
        private class MyEnumerator : IEnumerator
        {
            public NodeConnection[] connectionArray;
            int position = -1;

            //constructor
            public MyEnumerator(List<NodeConnection> connectionList)
            {
                connectionArray = connectionList.ToArray();
            }
            private IEnumerator getEnumerator()
            {
                return (IEnumerator)this;
            }
            //IEnumerator
            public bool MoveNext()
            {
                position++;
                return (position < connectionArray.Length);
            }
            //IEnumerator
            public void Reset()
            {
                position = -1;
            }
            //IEnumerator
            public object Current
            {
                get
                {
                    try
                    {
                        return connectionArray[position];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
        }
        public IEnumerator GetEnumerator()
        {
            return new MyEnumerator(connectionList);
        }

    }

    /// <summary>
    /// Stores info of central line. This nodes are used for vertex generation
    /// </summary>

    [System.Serializable]
    public class CenterLineNode
    {
        public Vector3 LocalPosition;
        public Vector3 GlobalPosition;

        public Vector3 Forward;
        public Vector3 Up;
        public Vector3 Right;

        public static implicit operator Vector3(CenterLineNode node) => node.LocalPosition;
        public CenterLineNode(Vector3 LocalPosition, Vector3 Forward, Vector3 Up, Vector3 Right)
        {
            this.LocalPosition = LocalPosition;
            this.Forward = Forward;
            this.Up = Up;
            this.Right = Right;
        }
    }
}

