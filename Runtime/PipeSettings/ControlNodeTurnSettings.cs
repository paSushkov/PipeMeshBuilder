using System;
using UnityEngine;

namespace PipeBuilder.PipeSettings
{
    [Serializable]
    public class ControlNodeTurnSettings
    {
        [SerializeField] private float turnRadius;
        [SerializeField] private int turnDetails;

        public float TurnRadius
        {
            get => TurnDetails > 1 ? turnRadius : 0.1f;
            set => turnRadius = value;
        }
        public int TurnDetails
        {
            get => turnDetails;
            set => turnDetails = value > 0 ? value : 1;
        }

        public ControlNodeTurnSettings()
        {
            turnRadius = 1;
            turnDetails = 5;
        }

        public ControlNodeTurnSettings(float turnRadius = 1f, int turnDetails = 5)
        {
            this.turnRadius = turnRadius;
            this.turnDetails = turnDetails;
        }

        public ControlNodeTurnSettings Clone()
        {
            return new ControlNodeTurnSettings(TurnRadius, TurnDetails);
        }
    }
}