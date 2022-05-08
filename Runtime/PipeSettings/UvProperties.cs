using System;
using UnityEngine;

namespace PipeBuilder.PipeSettings
{
    [Serializable]
    public class UvProperties
    {
        public int mapRowsCount = 1; 
        public float rowsOffset = 0; 
        public int mapColumnsCount = 1; 
        public float columnsOffset = 0; 
            
        public Vector2 outerTiling = Vector2.one;
        public Vector2 outerOffset = Vector2.zero;
        public int outerRow = 1; 
        public bool outerAutoTiling;
        
        public Vector2 innerTiling = Vector2.one;
        public Vector2 innerOffset = Vector2.zero;
        public int innerRow = 1; 
        public bool innerAutoTiling;
        
        public Vector2 edgeTiling = Vector2.one;
        public Vector2 edgeOffset = Vector2.zero;
        public int edgeRow = 1; 
        public int edgeColumn = 1; 
    }
}