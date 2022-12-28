using UnityEditor;

namespace PipeBuilder.Editor.Settings
{
    public class PipeBuilderEditorSettings
    {
        private DrawSettings drawSettings = new DrawSettings();
        private PivotRotationMode rotationMode = PivotRotationMode.Local;
        private float cursorInputTolerance;
        public DrawSettings DrawSettings => drawSettings;

        public PivotRotationMode RotationMode
        {
            get => rotationMode;
            set => rotationMode = value;
        }

        public float CursorInputTolerance
        {
            get => cursorInputTolerance;
            set => cursorInputTolerance = value;
        }

        public void Load()
        {
            drawSettings.Load();
            rotationMode = (PivotRotationMode)EditorPrefs.GetInt($"{GetType().FullName}_{nameof(rotationMode)}", (int)PivotRotationMode.Local);
            cursorInputTolerance = EditorPrefs.GetFloat($"{GetType().FullName}_{nameof(cursorInputTolerance)}", 0.25f);
        }

        public void Save()
        {
            DrawSettings.Save();
            EditorPrefs.SetInt($"{GetType().FullName}_{nameof(rotationMode)}", (int)rotationMode);
            EditorPrefs.SetFloat($"{GetType().FullName}_{nameof(cursorInputTolerance)}", cursorInputTolerance);
        }
    }
}