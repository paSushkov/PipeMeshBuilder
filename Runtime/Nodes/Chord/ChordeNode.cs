using System;
using UnityEngine;

namespace PipeBuilder.Nodes
{
    [Serializable]
    public class ChordeNode : BaseNode
    {
        [SerializeField] private Quaternion rotation;

        public Vector3 Right
        {
            get => this.rotation * Vector3.right;
            set => this.rotation = Quaternion.FromToRotation(Vector3.right, value);
        }

        public Vector3 Up
        {
            get => this.rotation * Vector3.up;
            set => this.rotation = Quaternion.FromToRotation(Vector3.up, value);
        }

        public Vector3 Forward
        {
            get => this.rotation * Vector3.forward;
            set => this.rotation = Quaternion.LookRotation(value);
        }

        public Quaternion Rotation
        {
            get => rotation;
            set => rotation = value;
        }

        public Vector3 WorldUp => pipeBuilder.transform.TransformDirection(Up);
        public Vector3 WorldForward => pipeBuilder.transform.TransformDirection(Forward);
        public Vector3 WorldRight => pipeBuilder.transform.TransformDirection(Right);


        public ChordeNode(PipeBuilder pipeBuilder, Vector3 position, Quaternion rotation) : base(pipeBuilder, position)
        {
            this.rotation = rotation;
        }
        
        public override void AssignPosition(Vector3 value)
        {
            position = value;
            localPosition = pipeBuilder.transform.InverseTransformPoint(value);
        }

        public override void AssignLocalPosition(Vector3 value)
        {
            localPosition = value;
            position = pipeBuilder.transform.TransformPoint(value);
        }
    }
}