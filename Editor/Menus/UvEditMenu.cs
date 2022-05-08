using PipeBuilder.Editor.Settings;
using PipeBuilder.PipeSettings;
using UnityEditor;
using UnityEngine;

namespace PipeBuilder.Editor.Menus
{
    public class UvEditMenu : PipeEditMenu
    {
        private enum UvEditMode
        {
            Manual,
            TextureMap
        }

        private readonly string[] uvEditModeNames = new string[] {"Manual", "Texture map" };
        private int editMode;
        
        public UvEditMenu(string name, PipeBuilder pipeBuilder, PipeBuilderEditorSettings settings) : base(name, pipeBuilder, settings)
        {
        }

        public override void OnEnable()
        {
        }

        public override void OnDisable()
        {
        }

        public override void DrawInspector()
        {
            DrawUILine(Color.gray);
            editMode = GUILayout.SelectionGrid(editMode, uvEditModeNames, uvEditModeNames.Length, "Button");
            switch ((UvEditMode)editMode)
            {
                case UvEditMode.Manual:
                    DrawUVSettings(pipeBuilder.uvProperties);
                    break;
                case UvEditMode.TextureMap:
                    DrawMapSettings(pipeBuilder.uvProperties);
                    break;
            }
        }

        public override void DrawScene()
        {
        }

        private void DrawMapSettings(UvProperties uvProperties)
        {
            EditorGUI.BeginChangeCheck();
            uvProperties.mapRowsCount = EditorGUILayout.DelayedIntField("Rows count in texture:", uvProperties.mapRowsCount);
            uvProperties.rowsOffset = EditorGUILayout.Slider("Ofsset between rows", uvProperties.rowsOffset, 0, 0.1f);
            DrawUILine(Color.gray, 1);
            GUILayout.Label("Outer side");
            uvProperties.outerRow = EditorGUILayout.IntSlider("Row:", uvProperties.outerRow, 1, uvProperties.mapRowsCount);
            uvProperties.outerTiling.x = EditorGUILayout.FloatField("Tiling X:", uvProperties.outerTiling.x);
            uvProperties.outerOffset.x = EditorGUILayout.FloatField("Offset X:", uvProperties.outerOffset.x);
            uvProperties.outerAutoTiling = EditorGUILayout.Toggle("Auto-X-tiling", uvProperties.outerAutoTiling);
            DrawUILine(Color.gray, 1);
            GUILayout.Label("Inner side");
            uvProperties.innerRow = EditorGUILayout.IntSlider("Row", uvProperties.innerRow, 1, uvProperties.mapRowsCount);
            uvProperties.innerAutoTiling = EditorGUILayout.Toggle("Auto-X-tiling", uvProperties.innerAutoTiling);
            uvProperties.innerTiling.x = EditorGUILayout.FloatField("Tiling X:", uvProperties.innerTiling.x);
            uvProperties.innerOffset.x = EditorGUILayout.FloatField("Offset X:", uvProperties.innerOffset.x);
            DrawUILine(Color.gray, 1);
            GUILayout.Label("Edges");
            uvProperties.mapColumnsCount = EditorGUILayout.DelayedIntField("Columns count in texture:", uvProperties.mapColumnsCount);
            uvProperties.columnsOffset = EditorGUILayout.Slider("Offset between columns", uvProperties.rowsOffset, 0, 0.1f);
            GUILayout.Space(10);
            uvProperties.edgeRow = EditorGUILayout.IntSlider("Row", uvProperties.edgeRow, 1, uvProperties.mapRowsCount);
            uvProperties.edgeColumn = EditorGUILayout.IntSlider("Column", uvProperties.edgeColumn, 1, uvProperties.mapColumnsCount);



            if (EditorGUI.EndChangeCheck())
            {
                if (uvProperties.mapRowsCount < 1)
                    uvProperties.mapRowsCount = 1;
                uvProperties.outerRow = Mathf.Clamp(uvProperties.outerRow, 1, uvProperties.mapRowsCount);
                uvProperties.innerRow = Mathf.Clamp(uvProperties.innerRow, 1, uvProperties.mapRowsCount);
               
                var rowHeight = (1- uvProperties.rowsOffset * (uvProperties.mapRowsCount-1)) / uvProperties.mapRowsCount;
                
                uvProperties.outerTiling.y = rowHeight;
                uvProperties.innerTiling.y = rowHeight;

                var outerRow = uvProperties.mapRowsCount - uvProperties.outerRow;
                uvProperties.outerOffset.y = (outerRow) * (rowHeight + uvProperties.rowsOffset);
                
                var innerRow = uvProperties.mapRowsCount - uvProperties.innerRow;
                uvProperties.innerOffset.y = (innerRow) * (rowHeight + uvProperties.rowsOffset);

                var edgeRow = uvProperties.mapRowsCount - uvProperties.edgeRow;
                var columnLenght = (1- uvProperties.columnsOffset * (uvProperties.mapColumnsCount-1)) / uvProperties.mapColumnsCount;
                
                uvProperties.edgeTiling.y = rowHeight; 
                uvProperties.edgeTiling.x = columnLenght;
                uvProperties.edgeOffset.y = (edgeRow) * (rowHeight + uvProperties.rowsOffset);
                uvProperties.edgeOffset.x = (uvProperties.edgeColumn-1) * (columnLenght + uvProperties.columnsOffset);
                    
                if (pipeBuilder.previewMeshFilter)
                    pipeBuilder.RebuildPreviewMesh();
                EditorUtility.SetDirty(pipeBuilder);
            }

        }

        private void DrawUVSettings(UvProperties uvProperties)
        {
            EditorGUI.BeginChangeCheck();

            GUILayout.Label("Outer side");
            uvProperties.outerTiling = EditorGUILayout.Vector2Field("Tiling", uvProperties.outerTiling);
            uvProperties.outerOffset = EditorGUILayout.Vector2Field("Offset", uvProperties.outerOffset);
            uvProperties.outerAutoTiling = EditorGUILayout.Toggle("Auto-X-tiling", uvProperties.outerAutoTiling);
            GUILayout.Space(5f);
            GUILayout.Label("Inner side");
            uvProperties.innerTiling = EditorGUILayout.Vector2Field("Tiling", uvProperties.innerTiling);
            uvProperties.innerOffset = EditorGUILayout.Vector2Field("Offset", uvProperties.innerOffset);
            uvProperties.innerAutoTiling = EditorGUILayout.Toggle("Auto-X-tiling", uvProperties.innerAutoTiling);
            GUILayout.Space(5f);
            GUILayout.Label("Edges");
            uvProperties.edgeTiling = EditorGUILayout.Vector2Field("Tiling", uvProperties.edgeTiling);
            uvProperties.edgeOffset = EditorGUILayout.Vector2Field("Offset", uvProperties.edgeOffset);
            if (EditorGUI.EndChangeCheck())
            {
                if (pipeBuilder.previewMeshFilter)
                    pipeBuilder.RebuildPreviewMesh();
                EditorUtility.SetDirty(pipeBuilder);
            }

        }
    }
}