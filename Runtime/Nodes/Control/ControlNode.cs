using System;
using System.Collections.Generic;
using PipeBuilder.PipeSettings;
using UnityEngine;

namespace PipeBuilder.Nodes
{
    [Serializable]
    public class ControlNode: BaseNode
    {
        [SerializeField] protected bool useDefaultTurnSettings = true;
        [SerializeField] protected bool useDefaultRadiusSettings = true;
        [SerializeField] protected ControlNodeTurnSettings customTurnSettings; 
        [SerializeField] protected ControlNodeRadiusSettings customRadiusSettings; 
        [SerializeField] protected float angleBetweenNeighbors = 180f;
        [SerializeField] protected float turnArcAngle = 180f;
        [SerializeField] protected Vector3 turnArcCenterPos;
        [SerializeField] protected Vector3 turnArcCenterPosLocal;
        [SerializeField] protected float turnArcCenterDistance = 0f;
        [SerializeField] protected float padding = 0f;
        [SerializeField] protected List<ChordeNode> chordeNodes;

        public bool UseDefaultTurnSettings
        {
            get => useDefaultTurnSettings;
            set => SwitchTurnSettings(value);
        }
        
        public bool UseDefaultRadiusSettings
        {
            get => useDefaultRadiusSettings;
            set => SwitchRadiusSettings(value);
        }
        
        public float AngleBetweenNeighbors
        {
            get => angleBetweenNeighbors;
            set => AssignAngleBetweenNeighbors(value);
        }

        public float TurnRadius
        {
            get => TurnSettings.TurnRadius;
            set => AssignCustomTurnRadius(value);
        }

        public int TurnDetails
        {
            get => TurnSettings.TurnDetails;
            set => AssignCustomTurnDetails(value);
        }

        public float OuterRadius
        {
            get => RadiusSettings.OuterRadius;
            set => RadiusSettings.OuterRadius = value;
        }
        
        public float InnerRadius
        {
            get => RadiusSettings.InnerRadius;
            set => RadiusSettings.InnerRadius= value;
        }

        public ControlNodeTurnSettings DefaultTurnSettings => Builder.DefaultControlLineTurnSettings;
        public ControlNodeRadiusSettings DefaultRadiusSettings => Builder.DefaultControlLineRadiusSettings;

        public Vector3 TurnArcCenterPos
        {
            get => turnArcCenterPos;
            set => AssignTurnArcCenterPosition(value);
        }
        
        public Vector3 TurnArcCenterPosLocal
        {
            get => turnArcCenterPosLocal;
            set => AssignTurnArcCenterPositionLocal(value);
        }

        private ControlNodeTurnSettings TurnSettings => UseDefaultTurnSettings ? DefaultTurnSettings : customTurnSettings;
        private ControlNodeRadiusSettings RadiusSettings => UseDefaultRadiusSettings ? DefaultRadiusSettings : customRadiusSettings;
        
        public float TurnArcCenterDistance => turnArcCenterDistance;

        public float Padding => padding;

        public float TurnArcAngle => turnArcAngle;

        public List<ChordeNode> ChordeNodes => chordeNodes;


        public ControlNode(PipeBuilder pipeBuilder, Vector3 position) : base(pipeBuilder, position)
        {
            customTurnSettings = DefaultTurnSettings.Clone();
            customRadiusSettings = DefaultRadiusSettings.Clone();
            chordeNodes = new List<ChordeNode>();
        }
        
        public static float CalculatePadding(float angle, float turnRadius)
        {
            if (Mathf.Approximately(angle, 180f))
                return 0f;

            var halfAngle = angle / 2f;
            var circleCenterDistance = turnRadius / Mathf.Sin(halfAngle * Mathf.Deg2Rad);
            var padding = circleCenterDistance * Mathf.Cos(halfAngle * Mathf.Deg2Rad);

            return padding;
        }

        public void AssignTurnArcCenterPosition(Vector3 position)
        {
            turnArcCenterPos = position;
            turnArcCenterPosLocal = pipeBuilder.transform.InverseTransformPoint(position);
        }
        
        public void AssignTurnArcCenterPositionLocal(Vector3 position)
        {
            turnArcCenterPosLocal = position;
            turnArcCenterPos = pipeBuilder.transform.TransformPoint(position);
        }

        public void AssignAngleBetweenNeighbors(float value)
        {
            angleBetweenNeighbors = value;
            Recalculate();
        }

        public void SwitchTurnSettings(bool useDefault)
        {
            if (useDefault == useDefaultTurnSettings)
                return;
            useDefaultTurnSettings = useDefault;
            Recalculate();
        }
        
        public void SwitchRadiusSettings(bool useDefault)
        {
            if (useDefault == useDefaultRadiusSettings)
                return;
            useDefaultRadiusSettings = useDefault;
        }

        public void AssignCustomTurnRadius(float value)
        {
            customTurnSettings.TurnRadius = value;
            if (!UseDefaultTurnSettings)
                Recalculate();
        }
        
        public void AssignCustomTurnDetails(int value)
        {
            customTurnSettings.TurnDetails = value;
        }
        
        public void Recalculate()
        {
            if (Mathf.Approximately(angleBetweenNeighbors, 180f))
            {
                turnArcCenterDistance = 0f;
                padding = 0f;
                turnArcAngle = 180f;
            }
            else
            {
                var halfAngle = angleBetweenNeighbors / 2f;
                turnArcCenterDistance = TurnRadius / Mathf.Sin(halfAngle * Mathf.Deg2Rad);
                padding = TurnArcCenterDistance * Mathf.Cos(halfAngle * Mathf.Deg2Rad);
                turnArcAngle = 180f - angleBetweenNeighbors;
            }
        }

        public override void AssignPosition(Vector3 value)
        {
            base.AssignPosition(value);
            for (var i = 0; i < ChordeNodes.Count; i++)
            {
                var chordeNode = ChordeNodes[i];
                chordeNode.AssignLocalPosition(chordeNode.LocalPosition);
            }
        }

        public override void AssignLocalPosition(Vector3 value)
        {
            base.AssignLocalPosition(value);
            for (var i = 0; i < ChordeNodes.Count; i++)
            {
                var chordeNode = ChordeNodes[i];
                chordeNode.AssignLocalPosition(chordeNode.LocalPosition);
            }

        }
    }
}