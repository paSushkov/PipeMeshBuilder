using System;
using System.Collections.Generic;
using PipeBuilder.Nodes;
using UnityEngine;

namespace PipeBuilder.Lines
{
    [Serializable]
    public class ControlLine
    {
        private const float DefaultNodeAngle = 180f;

        [SerializeField] private bool initialized;
        [SerializeField] private PipeBuilder pipeBuilder;
        [SerializeField] private List<ControlNode> controlNodes;
        [SerializeField] private bool checkAngle = true;
        [SerializeField] private float minAllowedAngle = 90f;
        [SerializeField] private float minTurnAngle;
        [SerializeField] private Vector3 lastRandomOrthogonal;
        [SerializeField] private Vector3 lastRandomOrthogonalBase;

        public bool Initialized => initialized;
        public PipeBuilder PipeBuilder => pipeBuilder;
        public IReadOnlyList<ControlNode> ControlNodes => controlNodes;

        public bool CheckAngle
        {
            get => checkAngle;
            set => checkAngle = value;
        }

        public float MinAllowedAngle
        {
            get => minAllowedAngle;
            set => minAllowedAngle = value;
        }
        
        public float MinTurnAngle => minTurnAngle;

        public void Initialize(PipeBuilder pipeBuilder)
        {
            this.pipeBuilder = pipeBuilder;
            if (controlNodes is null)
                controlNodes = new List<ControlNode>();
            if (controlNodes.Count < 2)
                GenerateInitialNodes(pipeBuilder);
            RebuildChordeNodes();
            initialized = true;
        }

        #region Nodes management

        public void AddNode(int index, Vector3 position, bool worldSpace = true)
        {
            var newNode = new ControlNode(pipeBuilder, position);
            if (!worldSpace)
                newNode.AssignLocalPosition(position);
            controlNodes.Insert(index, newNode);
            RecalculateAngleAround(index);
            RecalculateTurnArcCenterAround(index);
            FindCurrentMinAngle(out minTurnAngle, out _);
            RebuildChordeNodes();
        }

        public bool IsAddAllowed(Vector3 position, int index, float turnRadius, bool worldSpace = true)
        {
            if (index < 0 || index >= ControlNodes.Count)
                return false;
            if (index == 0)
            {
                var currentFirstNode = ControlNodes[index];
                var currentFirstPosition = worldSpace ? currentFirstNode.Position : currentFirstNode.LocalPosition;
                var nextNode = ControlNodes[1];
                var nextNodePosition = worldSpace ? nextNode.Position : nextNode.LocalPosition;
                var angle = AngleBetween(position, currentFirstPosition, nextNodePosition);
                if (CheckAngle && angle < MinAllowedAngle)
                    return false;
                var padding = ControlNode.CalculatePadding(angle, currentFirstNode.TurnRadius);
                var spacingToNew = Vector3.Distance(currentFirstPosition, position) - padding;
                var spacingToNext = Vector3.Distance(currentFirstPosition, nextNodePosition) - padding - nextNode.Padding;
                return spacingToNew > 0 && spacingToNext > 0;
            }
            if (index == ControlNodes.Count)
            {
                var currentLastNode = ControlNodes[ControlNodes.Count - 1];
                var currentLastPosition = worldSpace ? currentLastNode.Position : currentLastNode.LocalPosition;
                var prevNode = controlNodes[ControlNodes.Count - 2];
                var prevNodePosition = worldSpace ? prevNode.Position : prevNode.LocalPosition;
                var angle = AngleBetween(prevNodePosition, currentLastPosition, position);
                if (CheckAngle && angle < MinAllowedAngle)
                    return false;
                var padding = ControlNode.CalculatePadding(angle, currentLastNode.TurnRadius);
                var spacingToNew = Vector3.Distance(currentLastPosition, position) - padding;
                var spacingToPrev = Vector3.Distance(currentLastPosition, position) - padding - prevNode.Padding;
                return spacingToNew > 0 && spacingToPrev > 0;
            }
            else
            {
                var prevNode = ControlNodes[index - 1];
                var prevNodePosition = worldSpace ? prevNode.Position : prevNode.LocalPosition;
                    
                var nextNode = ControlNodes[index];
                var nextNodePosition = worldSpace ? nextNode.Position : nextNode.LocalPosition;

                var angleForNew = AngleBetween(prevNodePosition, position, nextNodePosition);
                if (CheckAngle && angleForNew < MinAllowedAngle)
                    return false;
                var paddingOfNew = ControlNode.CalculatePadding(angleForNew, turnRadius);
                var paddingOfPrev = prevNode.Padding;
                if (index - 2 >= 0)
                {
                    var secondPrevNode = ControlNodes[index - 2];
                    var secondPrevPosition = worldSpace ? secondPrevNode.Position : secondPrevNode.LocalPosition;
                    var newAngleOfPrev = AngleBetween(secondPrevPosition, prevNodePosition, position);
                    if (CheckAngle && newAngleOfPrev < MinAllowedAngle)
                        return false;
                    paddingOfPrev = ControlNode.CalculatePadding(newAngleOfPrev, prevNode.TurnRadius);
                    var spacingToSecondPrev = Vector3.Distance(secondPrevPosition, prevNodePosition) -
                                              secondPrevNode.Padding - paddingOfPrev;
                    if (spacingToSecondPrev <= 0)
                        return false;
                }

                var spacingToPrev = Vector3.Distance(prevNodePosition, position) - paddingOfPrev - paddingOfNew;
                if (spacingToPrev <= 0f)
                    return false;

                var paddingOfNext = nextNode.Padding;
                
                if (index + 1 < ControlNodes.Count)
                {
                    var secondNextNode = ControlNodes[index + 1];
                    var secondNextNodePosition = worldSpace ? secondNextNode.Position : secondNextNode.LocalPosition;
                    var newAngleOfNext = AngleBetween(position, nextNodePosition, secondNextNodePosition);
                    if (CheckAngle && newAngleOfNext < MinAllowedAngle)
                        return false;
                    paddingOfNext = ControlNode.CalculatePadding(newAngleOfNext, nextNode.TurnRadius);
                    var spacingToSecondNext = Vector3.Distance(nextNodePosition, secondNextNodePosition) -
                                              secondNextNode.Padding - paddingOfNext;
                    if (spacingToSecondNext <= 0)
                        return false;
                }

                var spacingToNext = Vector3.Distance(position, nextNodePosition) - paddingOfNew - paddingOfNext;
                if (spacingToNext <= 0)
                    return false;
                
                return true;
            }
        }

        public void DeleteNode(int index)
        {
            controlNodes.RemoveAt(index);
            
            SetAngleForTails(DefaultNodeAngle);

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

            FindCurrentMinAngle(out minTurnAngle, out _);
            RebuildChordeNodes();
        }
        
        public void DeleteNode(ControlNode node)
        {
            var index = controlNodes.IndexOf(node);
            DeleteNode(index);
        }

        public bool IsDeleteAllowed(int index, out bool testPassedForPrev, out bool testPassedForNext)
        {
            if (index < 0 || index >= ControlNodes.Count || ControlNodes.Count < 3)
            {
                testPassedForPrev = false;
                testPassedForNext = false;
                return false;
            }
            testPassedForPrev = true;
            testPassedForNext = true;
            if (index == 0 || index == controlNodes.Count - 1)
                return true;
            var prevNode = ControlNodes[index - 1];
            var nextNode = ControlNodes[index + 1];
            var newPaddingOfPrev = prevNode.Padding;
            var newPaddingOfNext = nextNode.Padding;
            
            if (index - 2 >= 0)
            {
                var secondPrevNode = controlNodes[index - 2];
                var newAngleOfPrev = AngleBetween(secondPrevNode.Position, prevNode.Position, nextNode.Position);
                if (CheckAngle &&
                    newAngleOfPrev < MinAllowedAngle &&
                    newAngleOfPrev < prevNode.AngleBetweenNeighbors)
                    testPassedForPrev = false;
                newPaddingOfPrev = ControlNode.CalculatePadding(newAngleOfPrev, prevNode.TurnRadius);
                var newSpacingOfPrev = Vector3.Distance(secondPrevNode.Position, prevNode.Position) -
                                       secondPrevNode.Padding - newPaddingOfPrev;
                if (newSpacingOfPrev <= 0f)
                    testPassedForPrev = false;

            }

            if (index + 2 < ControlNodes.Count)
            {
                var secondNextNode = ControlNodes[index + 2];
                var newAngleOfNext = AngleBetween(prevNode.Position, nextNode.Position, secondNextNode.Position);
                if (CheckAngle &&
                    newAngleOfNext < MinAllowedAngle &&
                    newAngleOfNext < nextNode.AngleBetweenNeighbors)
                    testPassedForNext = false;
                newPaddingOfNext = ControlNode.CalculatePadding(newAngleOfNext, nextNode.TurnRadius);
                var newSpacingOfNext = Vector3.Distance(secondNextNode.Position, nextNode.Position) -
                                       secondNextNode.Padding - newPaddingOfNext;
                if (newSpacingOfNext <= 0f)
                    testPassedForNext = false;
            }
            var spacing = Vector3.Distance(prevNode.Position, nextNode.Position) - newPaddingOfPrev - newPaddingOfNext;
            if (spacing <= 0)
            {
                testPassedForPrev = false;
                testPassedForNext = false;
            }

            return testPassedForPrev && testPassedForNext;
        }
        
        public bool IsDeleteAllowed(ControlNode node, out bool testPassedForPrev, out bool testPassedForNext)
        {
            var index = controlNodes.IndexOf(node);
            return IsDeleteAllowed(index, out testPassedForPrev, out testPassedForNext);
        }

        public void MoveNode(int index, Vector3 newPosition, bool worldSpace = true)
        {
            if (worldSpace)
                ControlNodes[index].AssignPosition(newPosition);
            else
                ControlNodes[index].AssignLocalPosition(newPosition);
            RecalculateAngleAround(index);
            RecalculateTurnArcCenterAround(index);
            FindCurrentMinAngle(out minTurnAngle, out _);
            RebuildChordeNodes();
        }
        
        public void MoveNode(ControlNode node, Vector3 newPosition, bool worldSpace = true)
        {
            var index = controlNodes.IndexOf(node);
            MoveNode(index, newPosition, worldSpace);
        }
        
        public bool IsMovingAllowed(int index, Vector3 newPosition, bool worldSpace = true)
        {
            if (index < 0 || index >= controlNodes.Count)
                return false;
            if (controlNodes.Count < 3)
                return true;
            if (index == 0)
            {
                var nextNode = ControlNodes[1];
                var nextNodePosition = worldSpace ? nextNode.Position : nextNode.LocalPosition;
                var secondNextNode =  ControlNodes[2];
                var secondNextNodePosition = worldSpace ? secondNextNode.Position : secondNextNode.LocalPosition;
                var newAngleForNext = pipeBuilder.ControlLine.AngleBetween(newPosition, nextNodePosition, secondNextNodePosition);
                if (CheckAngle &&
                    newAngleForNext < nextNode.AngleBetweenNeighbors &&
                    newAngleForNext < MinAllowedAngle)
                    return false;
                var newPaddingForNext = ControlNode.CalculatePadding(newAngleForNext, nextNode.TurnRadius);
                var paddingDistanceWithNext = Vector3.Magnitude(newPosition - nextNodePosition) - newPaddingForNext;
                var paddingDistanceForNextPair = Vector3.Distance(nextNodePosition, secondNextNodePosition) -
                                                 newPaddingForNext - secondNextNode.Padding;
                var nextPaddingDecreased = newPaddingForNext <= nextNode.Padding;

                return nextPaddingDecreased || (paddingDistanceWithNext > 0 && paddingDistanceForNextPair > 0); 
            }
            else if (index == ControlNodes.Count - 1)
            {
                var prevNode = ControlNodes[index - 1];
                var prevNodePosition = worldSpace ? prevNode.Position : prevNode.LocalPosition;
                var secondPrevNode = ControlNodes[index - 2];
                var secondPrevNodePosition = worldSpace ? secondPrevNode.Position : secondPrevNode.LocalPosition;
                var newAngleForPrevious = pipeBuilder.ControlLine.AngleBetween(secondPrevNodePosition, prevNodePosition, newPosition);
                if (CheckAngle &&
                    newAngleForPrevious < prevNode.AngleBetweenNeighbors &&
                    newAngleForPrevious < MinAllowedAngle)
                    return false;
                
                var newPaddingForPrev = ControlNode.CalculatePadding(newAngleForPrevious, prevNode.TurnRadius);
                var paddingDistanceWithPrev = Vector3.Magnitude(newPosition - prevNodePosition) - newPaddingForPrev;
                var paddingDistanceForPrevPair = Vector3.Distance(prevNodePosition, secondPrevNodePosition) -
                                                 newPaddingForPrev - secondPrevNode.Padding;
                
                var prevPaddingDecreased = newPaddingForPrev <= prevNode.Padding;
                return prevPaddingDecreased || (paddingDistanceWithPrev > 0 && paddingDistanceForPrevPair > 0);
            }
            else
            {
                var targetNode = ControlNodes[index];
                var prevNode = ControlNodes[index - 1];
                var prevNodePosition = worldSpace ? prevNode.Position : prevNode.LocalPosition;
                var nextNode = ControlNodes[index + 1];
                var nextNodePosition = worldSpace ? nextNode.Position : nextNode.LocalPosition;
                var newAngleForTarget = AngleBetween(prevNodePosition, newPosition, nextNodePosition);
                if (CheckAngle &&
                    newAngleForTarget < targetNode.AngleBetweenNeighbors &&
                    newAngleForTarget < MinAllowedAngle)
                    return false;
                var newPaddingForTarget = ControlNode.CalculatePadding(newAngleForTarget, targetNode.TurnRadius);
                
                // Before checks - lets set values which would pass anyway
                var paddingDistanceForNextPair = 1f;
                var paddingDistanceForPrevPair = 1f;

                var newPaddingForPrev = 0f;
                var newPaddingForNext = 0f;
                
                // Then - modify them if necessary
                if (index - 2 >= 0)
                {
                    var secondPrevNode = ControlNodes[index - 2];
                    var secondPrevNodePosition = worldSpace ? secondPrevNode.Position : secondPrevNode.LocalPosition;
                    var newAngleForPrev = AngleBetween(secondPrevNodePosition, prevNodePosition, newPosition);
                    
                    if (CheckAngle &&
                        newAngleForPrev < prevNode.AngleBetweenNeighbors &&
                        newAngleForPrev < MinAllowedAngle)
                        return false;
                    
                    newPaddingForPrev = ControlNode.CalculatePadding(newAngleForPrev, prevNode.TurnRadius);
                    paddingDistanceForPrevPair = Vector3.Distance(prevNodePosition, secondPrevNodePosition) -
                                                 newPaddingForPrev - secondPrevNode.Padding;
                }

                if (index + 2 < ControlNodes.Count)
                {
                    var secondNextNode = ControlNodes[index + 2];
                    var secondNextNodePosition = worldSpace ? secondNextNode.Position : secondNextNode.LocalPosition;
                    var newAngleForNext = AngleBetween(newPosition, nextNodePosition, secondNextNodePosition);
                    
                    if (CheckAngle &&
                        newAngleForNext < nextNode.AngleBetweenNeighbors &&
                        newAngleForNext < MinAllowedAngle)
                        return false;
                    
                    newPaddingForNext = ControlNode.CalculatePadding(newAngleForNext, nextNode.TurnRadius);
                    paddingDistanceForNextPair = Vector3.Distance(nextNodePosition, secondNextNodePosition) -
                                                 newPaddingForNext - secondNextNode.Padding;
                }
                var paddingDistanceWithNext = Vector3.Magnitude(newPosition - nextNodePosition) - newPaddingForNext - newPaddingForTarget;
                var paddingDistanceWithPrev = Vector3.Magnitude(newPosition - prevNodePosition) - newPaddingForPrev - newPaddingForTarget;

                return paddingDistanceWithNext > 0 &&
                       paddingDistanceForNextPair > 0 &&
                       paddingDistanceWithPrev > 0 &&
                       paddingDistanceForPrevPair > 0;
            }
        }
        
        public bool IsMovingAllowed(ControlNode node, Vector3 newPosition, bool worldSpace = true)
        {
            var index = controlNodes.IndexOf(node);
            return IsMovingAllowed(index, newPosition, worldSpace);
        }

        public void MoveNodes(Vector3 direction)
        {
            for (var i = 0; i < ControlNodes.Count; i++)
            {
                var node = ControlNodes[i];
                node.AssignPosition(node.Position+direction);
            }
        }

        #endregion


        #region Turn arc recalculation commands

        public void RecalculateTurnArcCenter(int index)
        {
            var node = ControlNodes[index];
            if (index == 0 || index == ControlNodes.Count - 1 || node.AngleBetweenNeighbors % 180f == 0f)
            {
                node.TurnArcCenter = Vector3.zero;
                node.Recalculate();
            }

            else
            {
                var toPrevious = (ControlNodes[index - 1].LocalPosition - node.LocalPosition).normalized;
                var toNext = (ControlNodes[index + 1].LocalPosition - node.LocalPosition).normalized;
                var toTurnCenter = (toPrevious + toNext).normalized;
                node.TurnArcCenter = toTurnCenter * node.TurnArcCenterDistance;
                node.Recalculate();
            }
        }
        
        public void RecalculateTurnArcCenter(ControlNode node)
        {
            var index = controlNodes.IndexOf(node);
            RecalculateTurnArcCenter(index);
        }
        
        public void RecalculateTurnArcCenterAround(int index)
        {
            RecalculateTurnArcCenter(index);
            if (index > 0)
                RecalculateTurnArcCenter(index - 1);
            if (index+1 < ControlNodes.Count)
                RecalculateTurnArcCenter(index + 1);
        }
        
        public void RecalculateTurnArcCenterAround(ControlNode node)
        {
            var index = controlNodes.IndexOf(node);
            RecalculateTurnArcCenterAround(index);
        }
        
        #endregion


        #region Angle between nodes recalculation commands

        public void RecalculateAngleForNode(int index)
        {
            if (index == 0 || index == ControlNodes.Count - 1)
                ControlNodes[index].AngleBetweenNeighbors = DefaultNodeAngle;
            else
            {
                var angle = AngleBetween(ControlNodes[index - 1], ControlNodes[index], ControlNodes[index + 1]);
                ControlNodes[index].AngleBetweenNeighbors = angle;
            }
        }
        
        public void RecalculateAngleForNode(ControlNode node)
        {
            var index = controlNodes.IndexOf(node);
            RecalculateAngleForNode(index);
        }

        public void RecalculateAngleAround(int index)
        {
            RecalculateAngleForNode(index);
            if (index > 0)
                RecalculateAngleForNode(index - 1);
            if (index + 1 < ControlNodes.Count)
                RecalculateAngleForNode(index + 1);
        }
        
        public void RecalculateAngleAround(ControlNode node)
        {
            var index = controlNodes.IndexOf(node);
            RecalculateAngleAround(index);
        }
        
        #endregion
        
        public bool TryGetChordeNode(int index, out ChordeNode result)
        {
            for (var i = 0; i < ControlNodes.Count; i++)
            {
                var node = ControlNodes[i];
                if (index < node.ChordeNodes.Count)
                {
                    result = node.ChordeNodes[index];
                    return true;
                }

                index -= node.ChordeNodes.Count;
            }
        
            result = default;
            return false;
        }
        
        public ChordeNode GetChordeNode(int index)
        {
            TryGetChordeNode(index, out var result);
            return result;
        }

        public int ChordeNodesCount()
        {
            var result = 0;
            for (var i = 0; i < ControlNodes.Count; i++)
                result += ControlNodes[i].ChordeNodes.Count;

            return result;
        }

        public void RebuildChordeNodes()
        {
            BuildFirstChordeNode();
            for (var i = 1; i < controlNodes.Count-1; i++)
                RebuildChordeNodesForMid(i);
            RebuildLastChordeNode();
            RebuildFirstChordeNodeNormals();
        }

        public void ControlNodeToPivot(int nodeIndex)
        {
            var movement = ControlNodes[nodeIndex].Position-pipeBuilder.transform.position;
            for (var i = 0; i < ControlNodes.Count; i++)
            {
                var node = ControlNodes[i];
                node.AssignPosition(node.Position-movement);
            }
        }

        public void SetNodeAsPivot(int nodeIndex)
        {
            var movement = ControlNodes[nodeIndex].Position-pipeBuilder.transform.position;
            pipeBuilder.transform.position += movement;
            ControlNodeToPivot(nodeIndex);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(pipeBuilder.transform);
#endif
        }

        private void RebuildChordeNodesForMid(int index)
        {
            Vector3 forward;
            Vector3 up;

            var transform = pipeBuilder.transform;
            var controlNode = controlNodes[index];
            controlNode.ChordeNodes.Clear();

            var baseNormal = CalculateAngleNormal(controlNode);
            var possibleUpNormals = new Vector3[4];
            var cornerDetail = controlNode.TurnDetails;

            var prevControlNode = controlNodes[index - 1];
            var lastChordeNode = prevControlNode.ChordeNodes[prevControlNode.ChordeNodes.Count - 1];
                
            // if CornerDetail is low - lets just take UP normal from previous node
            var reducedAngle = controlNode.AngleBetweenNeighbors % 180f;
            var almostFlat = Mathf.Approximately(reducedAngle, 0f) || Mathf.Abs(reducedAngle - 180f) < 0.1f;

            if (almostFlat)
            {
                forward = (controlNode.Position - lastChordeNode.Position).normalized;
                forward = transform.InverseTransformDirection(forward);
                up = lastChordeNode.Up;
                var rotation = Quaternion.LookRotation(forward, up);
                lastChordeNode = new ChordeNode(pipeBuilder, controlNode.Position, rotation);
                controlNode.ChordeNodes.Add(lastChordeNode);
            }
            else if (cornerDetail == 1)
            {
                var fromNodeToArcCenter = controlNode.TurnArcCenter.normalized;
                
                up = -fromNodeToArcCenter.normalized;
                forward = Vector3.Cross(fromNodeToArcCenter, baseNormal.normalized).normalized;

                possibleUpNormals[0] = up;
                possibleUpNormals[1] = -up;
                possibleUpNormals[2] = Vector3.Cross(forward, up).normalized;
                possibleUpNormals[3] = Vector3.Cross(up, forward).normalized;
                up = FindOptimalByDot(possibleUpNormals, lastChordeNode.WorldUp);
                up = (up + lastChordeNode.WorldUp).normalized;
                
                up = transform.InverseTransformDirection(up);
                forward = transform.InverseTransformDirection(forward);
                var rotation = Quaternion.LookRotation(forward, up);
                lastChordeNode = new ChordeNode(pipeBuilder, controlNode.Position, rotation);
                controlNode.ChordeNodes.Add(lastChordeNode);
            }
            
            // Current control node is in the middle, have turn angle and level of details of turns are not min
            // We need to add a bunch of CentralLineNodes, which will form an arc
            else
            {
                var globalTurnCenter = pipeBuilder.transform.TransformDirection(controlNode.TurnArcCenter) +
                                       controlNode.Position;
                
                var startDirection = (prevControlNode.Position - controlNode.Position).normalized;
                var startPosition = controlNode.Position+ startDirection* controlNode.Padding;
                var radius = controlNode.TurnRadius;
                var rotatingVector = (startPosition - globalTurnCenter).normalized * radius;
                var angleStep = controlNode.TurnArcAngle / (cornerDetail - 1);
                var lastChordeOfPrevControl = lastChordeNode;
                for (var j = 0; j < cornerDetail; j++)
                {

                    var rotatedVector = Quaternion.AngleAxis(angleStep * j, baseNormal) * rotatingVector;
                    var position = globalTurnCenter + rotatedVector;
                    up = rotatedVector.normalized;
                    forward = Vector3.Cross(baseNormal.normalized, up);

                    if (index > 1)
                    {
                        possibleUpNormals[0] = up;
                        possibleUpNormals[1] = -up;
                        possibleUpNormals[2] = Vector3.Cross(forward, up).normalized;
                        possibleUpNormals[3] = Vector3.Cross(up, forward).normalized;
                        up = FindOptimalByDot(possibleUpNormals, lastChordeNode.WorldUp);
                        up = (up + lastChordeOfPrevControl.WorldUp).normalized;
                    }

                    up = transform.InverseTransformDirection(up);
                    forward = transform.InverseTransformDirection(forward);

                    var rotation = Quaternion.LookRotation(forward, up);
                    lastChordeNode = new ChordeNode(pipeBuilder, position, rotation);
                    controlNode.ChordeNodes.Add(lastChordeNode);
                }

                // TODO: find best way to handle 2-detailed nodes?
                // if (controlNode.ChordeNodes.Count == 2)
                // {
                //     var first = controlNode.ChordeNodes[0];
                //     var second = controlNode.ChordeNodes[1];
                //     first.Rotation = Quaternion.LookRotation((second.LocalPosition - first.LocalPosition).normalized, first.Up);
                //     second.Rotation = first.Rotation;
                // }
            }
        }

        private void RebuildLastChordeNode()
        {
            var prevControlNode = ControlNodes[ControlNodes.Count - 2];
            var lastChordeNode = prevControlNode.ChordeNodes[prevControlNode.ChordeNodes.Count - 1];
            
            var lastControlNode = ControlNodes[ControlNodes.Count - 1];
            lastControlNode.ChordeNodes.Clear();
            
            var direction = (lastControlNode.LocalPosition - lastChordeNode.LocalPosition).normalized;
            var rotation = Quaternion.LookRotation(direction, lastChordeNode.Up); 
            var newChordNode = new ChordeNode(pipeBuilder, lastControlNode, rotation);
            lastControlNode.ChordeNodes.Add(newChordNode);
        }

        private void BuildFirstChordeNode()
        {
            var controlNode = controlNodes[0];
            var nextNode = controlNodes[1];
            
            controlNode.ChordeNodes.Clear();
            var forward = (nextNode.LocalPosition-controlNode.LocalPosition).normalized;
            
            var up = RandomOrthogonal(forward).normalized;
            var rotation = Quaternion.LookRotation(forward, up);
            var newNode = new ChordeNode(pipeBuilder, controlNode, rotation);
            controlNode.ChordeNodes.Add(newNode);
        }

        private void RebuildFirstChordeNodeNormals()
        {
            var node0 = GetChordeNode(0);
            var node1 = GetChordeNode(1);
            node0.Rotation = node1.Rotation;
        }

        #region Utility

        public float CalculateLenghtOfChordeLine()
        {
            var result = 0f;
            var lastChordeNode = GetChordeNode(0);
            for (var i = 0; i < ControlNodes.Count; i++)
            {
                var node = ControlNodes[i];
                for (var ii = 0; ii < node.ChordeNodes.Count; ii++)
                {
                    var chordeNode = node.ChordeNodes[ii];
                    result += Vector3.Distance(lastChordeNode, chordeNode);
                    lastChordeNode = chordeNode;
                }
            }

            return result;
        }

        public float AngleBetween(Vector3 point1, Vector3 point2, Vector3 point3)
        {
            var vector1 = point1 - point2;
            var vector2 = point3 - point2;
            return Vector3.Angle(vector1, vector2);
        }

        private Vector3 CalculateAngleNormal(int index)
        {
            if (index > 0 && index < ControlNodes.Count - 1)
            {
                var toNext = (ControlNodes[index + 1].Position - ControlNodes[index].Position).normalized;
                var fromPrev = (ControlNodes[index - 1].Position - ControlNodes[index].Position).normalized; 
                var baseNormal = Vector3.Cross(toNext,fromPrev).normalized;
                return baseNormal;
            }
            return Vector3.zero;
        }
        
        public Vector3 CalculateAngleNormal(ControlNode node)
        {
            var index = controlNodes.IndexOf(node);
            return CalculateAngleNormal(index);
        }

        public void FindCurrentMinAngle(out float angle, out int index)
        {
            angle = 360f;
            index = -1;

            for (var i = 0; i < ControlNodes.Count; i++)
            {
                var node = ControlNodes[i];
                if (node.AngleBetweenNeighbors < angle)
                {
                    angle = node.AngleBetweenNeighbors;
                    index = i;
                }
            }
        }

        public bool IsTurnRadiusAllowedForNode(float radius, int index)
        {
            var node = ControlNodes[index];
            if (index == 0 || index == ControlNodes.Count - 1)
                return true;
            
            var newPadding = ControlNode.CalculatePadding(node.AngleBetweenNeighbors, radius);
            var prevNodePadding = ControlNodes[index - 1].Padding;
            var nextNodePadding = ControlNodes[index + 1].Padding;

            // If padding will not overlap - new turn radius can be applied for current node
            return Vector3.Distance(ControlNodes[index], ControlNodes[index - 1]) - newPadding - prevNodePadding > 0 &&
                   Vector3.Distance(ControlNodes[index], ControlNodes[index + 1]) - newPadding - nextNodePadding > 0;
        }
        
        public bool IsTurnRadiusAllowedForNode(float radius, ControlNode node)
        {
            var index = controlNodes.IndexOf(node);
            return IsTurnRadiusAllowedForNode(radius, index);
        }

        public bool IsNewDefaultTurnRadiusAllowed(float radius)
        {
            for (var i = 0; i < ControlNodes.Count; i++)
            {
                if (ControlNodes[i].UseDefaultTurnSettings && !IsNewDefaultTurnRadiusAllowedForNode(radius, i))
                    return false;
            }
            return true;
        }

        private bool IsNewDefaultTurnRadiusAllowedForNode(float radius, int index, bool escape = false)
        {
            var node = ControlNodes[index];
            if (index == 0 || index == ControlNodes.Count - 1)
                return true;
            
            var newPadding = ControlNode.CalculatePadding(node.AngleBetweenNeighbors, radius);
            var prevNodePadding = ControlNodes[index - 1].Padding;
            var nextNodePadding = ControlNodes[index + 1].Padding;

            if (ControlNodes[index - 1].UseDefaultTurnSettings)
                prevNodePadding = ControlNode.CalculatePadding(ControlNodes[index - 1].AngleBetweenNeighbors, radius);
            if (ControlNodes[index + 1].UseDefaultTurnSettings)
                nextNodePadding = ControlNode.CalculatePadding(ControlNodes[index + 1].AngleBetweenNeighbors, radius);

            // If padding will not overlap - new turn radius can be applied for current node
            var prevSpacing = Vector3.Distance(ControlNodes[index], ControlNodes[index - 1]) - newPadding - prevNodePadding;
            var nextSpacing = Vector3.Distance(ControlNodes[index], ControlNodes[index + 1]) - newPadding - nextNodePadding;
            var result=  prevSpacing > 0 && nextSpacing > 0;
            return result;
        }

        private void GenerateInitialNodes(PipeBuilder pipeBuilder)
        {
            var firstPosition = pipeBuilder.transform.position;
            var secondPosition = firstPosition + pipeBuilder.transform.forward*2 + pipeBuilder.transform.up*2;
            var lastPosition = firstPosition + pipeBuilder.transform.forward*4;
            var firstNode = new ControlNode(pipeBuilder, firstPosition);
            var secondNode = new ControlNode(pipeBuilder, secondPosition);
            var lastNode = new ControlNode(pipeBuilder, lastPosition);
            controlNodes.Add(firstNode);
            controlNodes.Add(secondNode);
            controlNodes.Add(lastNode);
            RecalculateAngleAround(secondNode);
            RecalculateTurnArcCenterAround(secondNode);
            FindCurrentMinAngle(out minTurnAngle, out _);
        }

        private void SetAngleForTails(float angle)
        {
            if (ControlNodes.Count < 1)
                return;
            ControlNodes[0].AngleBetweenNeighbors = angle;
            ControlNodes[ControlNodes.Count - 1].AngleBetweenNeighbors = angle;
        }
        
        private Vector3 RandomOrthogonal(Vector3 v)
        {
            if (Mathf.Approximately(Vector3.Dot(v, lastRandomOrthogonalBase), 1f))
                return lastRandomOrthogonal;
            
            var vPerpendicular = Vector3.one;
            if (v.x != 0)
                vPerpendicular.x = -(v.y * vPerpendicular.y + v.z * vPerpendicular.z) / v.x;
            else
                vPerpendicular.x = UnityEngine.Random.Range(0.0f, 1.0f);
            
            if (v.y != 0)
                vPerpendicular.y = -(v.x * vPerpendicular.x + v.z * vPerpendicular.z) / v.y;
            else
                vPerpendicular.y = UnityEngine.Random.Range(0.0f, 1.0f);

            if (v.z != 0)
                vPerpendicular.z = -(v.y * vPerpendicular.y + v.x * vPerpendicular.x) / v.z;
            else
                vPerpendicular.z = UnityEngine.Random.Range(0.0f, 1.0f);
            
            lastRandomOrthogonalBase = v;
            lastRandomOrthogonal = vPerpendicular;
            return vPerpendicular;
        }
        
        private Vector3 FindOptimalByDot(Vector3[] arr, Vector3 compareTo)
        {
            var optimal = arr[0].normalized;
            var maxDot = Vector3.Dot(compareTo, arr[0].normalized);
            
            foreach (var vector in arr)
            {
                (Vector3, Vector3) tuple = (compareTo, vector.normalized);
                
                if (maxDot < Vector3.Dot(tuple.Item1, tuple.Item2))
                {
                    optimal = tuple.Item2;
                    maxDot = Vector3.Dot(tuple.Item1, tuple.Item2);
                }
                
                if (maxDot < Vector3.Dot(tuple.Item1, -tuple.Item2))
                {
                    optimal = -tuple.Item2;
                    maxDot = Vector3.Dot(tuple.Item1, -tuple.Item2);
                }
            }
            return optimal;
        }
        
        private Vector3 FindNextDifferUp(ChordeNode node, Vector3 differFrom, IList<ChordeNode> chordeNodes)
        {
            var index = chordeNodes.IndexOf(node);
            var result = node.Up;
            if (result == differFrom && index + 1 < chordeNodes.Count - 1)
                return FindNextDifferUp(chordeNodes[index + 1], differFrom, chordeNodes);
            else
                return result;
        }


        #endregion
    }
}