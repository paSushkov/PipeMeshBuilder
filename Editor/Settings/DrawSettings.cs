using UnityEditor;
using UnityEngine;

namespace PipeBuilder.Editor.Settings
{
    public class DrawSettings
    {
        public Color controlLineColor;
        public Color controlNodesColor;
        public float controlLineWidth = 15f;
        public float controlNodesSize = 1f;

        public Color chordeLineColor;
        public Color chordeNodesColor;
        public float chordeLineWidth;
        public float chordeNodesSize = 0.5f;

        public Color chordeLineCircleColor;
        public float chordeLineCircleSize = 1f;

        public void Load()
        {
            controlLineColor = GetColorFromString(EditorPrefs.GetString($"{GetType().FullName}_{nameof(controlLineColor)}", "0072FFC4"));
            controlNodesColor = GetColorFromString(EditorPrefs.GetString($"{GetType().FullName}_{nameof(controlNodesColor)}", "003BFFB4"));
            chordeLineColor = GetColorFromString(EditorPrefs.GetString($"{GetType().FullName}_{nameof(chordeLineColor)}", "FF2D00C4"));
            chordeNodesColor = GetColorFromString(EditorPrefs.GetString($"{GetType().FullName}_{nameof(chordeNodesColor)}", "FF0007B4"));
            chordeLineCircleColor = GetColorFromString(EditorPrefs.GetString($"{GetType().FullName}_{nameof(chordeLineCircleColor)}", "03FF00FF"));

            controlLineWidth = EditorPrefs.GetFloat($"{GetType().FullName}_{nameof(controlLineWidth)}", 15f);
            controlNodesSize = EditorPrefs.GetFloat($"{GetType().FullName}_{nameof(controlNodesSize)}", 1f);
            chordeLineWidth = EditorPrefs.GetFloat($"{GetType().FullName}_{nameof(chordeLineWidth)}", 6.5f);
            chordeNodesSize = EditorPrefs.GetFloat($"{GetType().FullName}_{nameof(chordeNodesSize)}", 6.5f);
            chordeLineCircleSize = EditorPrefs.GetFloat($"{GetType().FullName}_{nameof(chordeLineCircleSize)}", 1f);
        }
        
        public void Save()
        {
            EditorPrefs.SetString($"{GetType().FullName}_{nameof(controlLineColor)}", GetStringFromColor(controlLineColor));
            EditorPrefs.SetString($"{GetType().FullName}_{nameof(controlNodesColor)}", GetStringFromColor(controlNodesColor));
            EditorPrefs.SetString($"{GetType().FullName}_{nameof(chordeLineColor)}", GetStringFromColor(chordeLineColor));
            EditorPrefs.SetString($"{GetType().FullName}_{nameof(chordeNodesColor)}", GetStringFromColor(chordeNodesColor));
            EditorPrefs.SetString($"{GetType().FullName}_{nameof(chordeLineCircleColor)}", GetStringFromColor(chordeLineCircleColor));

            EditorPrefs.SetFloat($"{GetType().FullName}_{nameof(controlLineWidth)}", controlLineWidth);
            EditorPrefs.SetFloat($"{GetType().FullName}_{nameof(controlNodesSize)}", controlNodesSize);
            EditorPrefs.SetFloat($"{GetType().FullName}_{nameof(chordeLineWidth)}", chordeLineWidth);
            EditorPrefs.SetFloat($"{GetType().FullName}_{nameof(chordeNodesSize)}", chordeNodesSize);
            EditorPrefs.SetFloat($"{GetType().FullName}_{nameof(chordeLineCircleSize)}", chordeLineCircleSize);
        }

        private int HexToDec(string hex)
        {
            return System.Convert.ToInt32(hex, 16);
        }
        
        private string DecToHex(int value)
        {
            return value.ToString("X2");
        }

        private string FloatNormalizedToHex(float value)
        {
            return DecToHex(Mathf.RoundToInt(value * 255f));
        }
        
        private float HexToFloatNormalized(string hex)
        {
            return HexToDec(hex) / 255f;
        }

        private Color GetColorFromString(string hexString)
        {
            var r = HexToFloatNormalized(hexString.Substring(0, 2));
            var g = HexToFloatNormalized(hexString.Substring(2, 2));
            var b = HexToFloatNormalized(hexString.Substring(4, 2));
            var a = 1f;
            if (hexString.Length>=8)
                a = HexToFloatNormalized(hexString.Substring(6, 2));

            return new Color(r, g, b, a);
        }

        private string GetStringFromColor(Color color, bool useAlpha = true)
        {
            var r = FloatNormalizedToHex(color.r);
            var g = FloatNormalizedToHex(color.g);
            var b = FloatNormalizedToHex(color.b);
            var a = FloatNormalizedToHex(color.a);
            return useAlpha ? r + g + b + a : r + g + b;
        }
    }
}