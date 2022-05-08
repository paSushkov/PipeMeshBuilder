using System;
using UnityEngine;

namespace PipeBuilder.Nodes
{
    [Serializable]
    public abstract class BaseNode
    {
        public static implicit operator Vector3(BaseNode node) => node.Position;

        [SerializeField] protected PipeBuilder pipeBuilder;
        [SerializeField] protected Vector3 position;
        [SerializeField] protected Vector3 localPosition;
        
        public Vector3 Position => position;

        public Vector3 LocalPosition => localPosition;

        public PipeBuilder Builder => pipeBuilder;

        public BaseNode(PipeBuilder pipeBuilder, Vector3 position)
        {
            this.pipeBuilder = pipeBuilder;
            this.position = position;
            localPosition = pipeBuilder.transform.InverseTransformPoint(position);
        }

        public virtual void AssignPosition(Vector3 value)
        {
            position = value;
            localPosition = pipeBuilder.transform.InverseTransformPoint(value);
        }

        public virtual void AssignLocalPosition(Vector3 value)
        {
            localPosition = value;
            position = pipeBuilder.transform.TransformPoint(value);
        }
    }
}