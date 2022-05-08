using System;
using UnityEngine;

namespace PipeBuilder.PipeSettings
{
    [Serializable]
    public class ControlNodeRadiusSettings
    {
        [SerializeField] private float outerRadius;
        [SerializeField] private float innerRadius;
        
        public float OuterRadius
        {
            get => outerRadius;
            set
            {
                if (value > innerRadius)
                    outerRadius = value;
            }
        }

        public float InnerRadius
        {
            get => innerRadius;
            set
            {
                if (value < outerRadius && value > 0.1)
                innerRadius = value;
            }
        }
        
        public ControlNodeRadiusSettings()
        {
            outerRadius = 1f;
            innerRadius = 0.5f;
        }

        public ControlNodeRadiusSettings(float outerRadius = 1f, float innerRadius = 0.5f)
        {
            this.outerRadius = outerRadius;
            this.innerRadius = innerRadius;
        }
        
        public ControlNodeRadiusSettings Clone()
        {
            return new ControlNodeRadiusSettings(OuterRadius, InnerRadius);
        }

    }
}