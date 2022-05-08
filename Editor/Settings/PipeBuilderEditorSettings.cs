using UnityEditor;

namespace PipeBuilder.Editor.Settings
{
    public class PipeBuilderEditorSettings
    {
        private DrawSettings drawSettings = new DrawSettings();
        private PivotRotationMode rotationMode = PivotRotationMode.Local;
        public DrawSettings DrawSettings => drawSettings;

        public PivotRotationMode RotationMode
        {
            get => rotationMode;
            set => rotationMode = value;
        }

        public void Load()
        {
            drawSettings.Load();
            rotationMode = (PivotRotationMode)EditorPrefs.GetInt($"{GetType().FullName}_{nameof(rotationMode)}", (int)PivotRotationMode.Local);
        }

        public void Save()
        {
            DrawSettings.Save();
            EditorPrefs.SetInt($"{GetType().FullName}_{nameof(rotationMode)}", (int)rotationMode);
        }
    }
}