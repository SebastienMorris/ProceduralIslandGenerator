using System.Collections;
using System.Collections.Generic;
using Content.Scripts.ComputeShaderGen.C_;
using UnityEditor;
using UnityEngine;

public class GUI : MonoBehaviour
{
        [SerializeField] private ChunkGenerator chunkGenerator;
        [SerializeField] private CameraController cameraController;

        // Scaling settings
        private Vector2 referenceResolution = new Vector2(1920, 1080);
        private float guiScale = 1f;

        // Drag value state
        private bool isDragging = false;
        private string draggingField = "";
        private float dragStartValue = 0f;
        private float dragStartMouseX = 0f;

        // Cache for text field strings
        private string dimensionXText = "0";
        private string dimensionYText = "0";
        private string dimensionZText = "0";
        private string seedText = "0";
        private string rotXText = "0";
        private string rotYText = "0";
        private string rotZText = "0";

        private int fps = 0;
        private float fpsTimer = 1.0f;

        private void Start()
        {
            if (chunkGenerator == null)
                chunkGenerator = GetComponent<ChunkGenerator>();
            
            if (cameraController == null)
                cameraController = FindObjectOfType<CameraController>();

            // Initialize text fields
            UpdateTextFieldsFromValues();
        }

        private void UpdateTextFieldsFromValues()
        {
            if (chunkGenerator != null)
            {
                dimensionXText = chunkGenerator.Dimensions.x.ToString();
                dimensionYText = chunkGenerator.Dimensions.y.ToString();
                dimensionZText = chunkGenerator.Dimensions.z.ToString();
                seedText = chunkGenerator.NoiseSettings.seed.ToString();
                rotXText = chunkGenerator.MainLightRotation.x.ToString("F1");
                rotYText = chunkGenerator.MainLightRotation.y.ToString("F1");
                rotZText = chunkGenerator.MainLightRotation.z.ToString("F1");
            }
        }

        private void OnGUI()
        {
            if (chunkGenerator == null || cameraController == null) return;

            // Calculate scale based on screen size
            float scaleX = Screen.width / referenceResolution.x;
            float scaleY = Screen.height / referenceResolution.y;
            guiScale = Mathf.Min(scaleX, scaleY);

            // Save original matrix
            Matrix4x4 originalMatrix = UnityEngine.GUI.matrix;

            // Apply scaling
            UnityEngine.GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(guiScale, guiScale, 1));

            // Draw GUI using reference resolution coordinates
            GUILayout.BeginArea(new Rect(10, 10, 300, 800));
            GUILayout.BeginVertical("box");

            DrawCameraControls();
            GUILayout.Space(10);
            DrawDimensionsControls();
            GUILayout.Space(10);
            DrawSurfaceLevelControl();
            GUILayout.Space(5);
            DrawGrassBlendControl();
            GUILayout.Space(10);
            DrawSeedControl();
            DrawNoiseControls();
            GUILayout.Space(10);
            DrawLightingControls();
            GUILayout.Space(10);
            DrawFPSCounter();
            GUILayout.Space(10);
            DrawGizmoButton();

            GUILayout.EndVertical();
            GUILayout.EndArea();

            // Restore original matrix
            UnityEngine.GUI.matrix = originalMatrix;
        }

        private void DrawCameraControls()
        {
            GUILayout.Label("Generator Controls", EditorStyles.boldLabel);
            GUILayout.Label("Move     WASD");
            GUILayout.Label("Zoom     Mouse Scroll");
            
            float newDist = GUILayout.HorizontalSlider(cameraController.Distance, 2.0f, 800.0f);
            if (Mathf.Abs(newDist - cameraController.Distance) > 0.001f)
            {
                cameraController.Distance = newDist;
            }
        }

        // Helper method for draggable integer field
        private int DraggableIntField(string label, int value, string fieldId, float sensitivity = 1f)
        {
            GUILayout.BeginHorizontal();
            
            Rect labelRect = GUILayoutUtility.GetRect(new GUIContent(label), UnityEngine.GUI.skin.label, GUILayout.Width(40));
            
            // Draw underline when hovering
            Event e = Event.current;
            bool isHovering = labelRect.Contains(e.mousePosition);
            
            if (isHovering)
            {
                Rect underlineRect = new Rect(labelRect.x, labelRect.yMax - 2, labelRect.width, 2);
                EditorGUI.DrawRect(underlineRect, new Color(0.3f, 0.6f, 1f, 0.8f));
            }
            
            UnityEngine.GUI.Label(labelRect, label);
            
            // Check for mouse drag on label
            if (labelRect.Contains(e.mousePosition))
            {
                EditorGUIUtility.AddCursorRect(labelRect, MouseCursor.SlideArrow);
                
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    isDragging = true;
                    draggingField = fieldId;
                    dragStartValue = value;
                    dragStartMouseX = e.mousePosition.x;
                    e.Use();
                }
            }
            
            // Handle dragging
            if (isDragging && draggingField == fieldId)
            {
                if (e.type == EventType.MouseDrag)
                {
                    float delta = (e.mousePosition.x - dragStartMouseX) * sensitivity;
                    value = Mathf.RoundToInt(dragStartValue + delta);
                    e.Use();
                }
                else if (e.type == EventType.MouseUp)
                {
                    isDragging = false;
                    draggingField = "";
                    e.Use();
                }
            }
            
            string textValue = value.ToString();
            string newText = GUILayout.TextField(textValue, GUILayout.Width(50));
            
            GUILayout.EndHorizontal();
            
            if (int.TryParse(newText, out int parsedValue))
            {
                return parsedValue;
            }
            
            return value;
        }

        // Helper method for draggable float field
        private float DraggableFloatField(string label, float value, string fieldId, float sensitivity = 0.1f)
        {
            GUILayout.BeginHorizontal();
            
            Rect labelRect = GUILayoutUtility.GetRect(new GUIContent(label), UnityEngine.GUI.skin.label, GUILayout.Width(40));
            
            // Draw underline when hovering
            Event e = Event.current;
            bool isHovering = labelRect.Contains(e.mousePosition);
            
            if (isHovering)
            {
                Rect underlineRect = new Rect(labelRect.x, labelRect.yMax - 2, labelRect.width, 2);
                EditorGUI.DrawRect(underlineRect, new Color(0.3f, 0.6f, 1f, 0.8f));
            }
            
            UnityEngine.GUI.Label(labelRect, label);
            
            // Check for mouse drag on label
            if (labelRect.Contains(e.mousePosition))
            {
                EditorGUIUtility.AddCursorRect(labelRect, MouseCursor.SlideArrow);
                
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    isDragging = true;
                    draggingField = fieldId;
                    dragStartValue = value;
                    dragStartMouseX = e.mousePosition.x;
                    e.Use();
                }
            }
            
            // Handle dragging
            if (isDragging && draggingField == fieldId)
            {
                if (e.type == EventType.MouseDrag)
                {
                    float delta = (e.mousePosition.x - dragStartMouseX) * sensitivity;
                    value = dragStartValue + delta;
                    e.Use();
                }
                else if (e.type == EventType.MouseUp)
                {
                    isDragging = false;
                    draggingField = "";
                    e.Use();
                }
            }
            
            string textValue = value.ToString("F1");
            string newText = GUILayout.TextField(textValue, GUILayout.Width(50));
            
            GUILayout.EndHorizontal();
            
            if (float.TryParse(newText, out float parsedValue))
            {
                return parsedValue;
            }
            
            return value;
        }

        private void DrawDimensionsControls()
        {
            GUILayout.Label("Dimensions", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            
            var dims = chunkGenerator.Dimensions;
            
            int newX = DraggableIntField("X:", dims.x, "dimX", 0.5f);
            int newY = DraggableIntField("Y:", dims.y, "dimY", 0.5f);
            int newZ = DraggableIntField("Z:", dims.z, "dimZ", 0.5f);

            Vector3Int newDim = new Vector3Int(newX, newY, newZ);
            if (newDim != chunkGenerator.Dimensions)
            {
                chunkGenerator.Dimensions = newDim;
                dimensionXText = newX.ToString();
                dimensionYText = newY.ToString();
                dimensionZText = newZ.ToString();
            }
            
            GUILayout.EndHorizontal();
        }

        private void DrawSurfaceLevelControl()
        {
            GUILayout.Label($"Surface Level {chunkGenerator.SurfaceLevel:F3}", EditorStyles.boldLabel);
            float newSurfaceLevel = GUILayout.HorizontalSlider(chunkGenerator.SurfaceLevel, 0f, 1f);
            if (Mathf.Abs(newSurfaceLevel - chunkGenerator.SurfaceLevel) > 0.001f)
            {
                chunkGenerator.SurfaceLevel = newSurfaceLevel;
            }
        }

        private void DrawGrassBlendControl()
        {
            var textureData = chunkGenerator.TextureData;
            GUILayout.Label($"Grass Ratio: {textureData.grassBlend:F3}", EditorStyles.boldLabel);
            float newGrassBlend = GUILayout.HorizontalSlider(textureData.grassBlend, 0f, 1f);

            if (Mathf.Abs(newGrassBlend - textureData.grassBlend) > 0.001f)
            {
                textureData.grassBlend = newGrassBlend;
                chunkGenerator.TextureData = textureData;
            }
        }

        private void DrawSeedControl()
        {
            GUILayout.Label("Seed", EditorStyles.boldLabel);
            
            var noiseSettings = chunkGenerator.NoiseSettings;
            int newSeed = DraggableIntField("Value:", noiseSettings.seed, "seed", 1f);

            if (newSeed != noiseSettings.seed)
            {
                noiseSettings.seed = newSeed;
                chunkGenerator.NoiseSettings = noiseSettings;
                seedText = newSeed.ToString();
            }
        }

        private void DrawNoiseControls()
        {
            var noiseSettings = chunkGenerator.NoiseSettings;

            GUILayout.Label("Noise", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            // Frequency
            GUILayout.Label($"Frequency: {noiseSettings.frequency}", EditorStyles.boldLabel);
            int newFrequency = (int)GUILayout.HorizontalSlider(noiseSettings.frequency, 1.0f, 200.0f);
            if (noiseSettings.frequency != newFrequency)
            {
                noiseSettings.frequency = newFrequency;
                chunkGenerator.NoiseSettings = noiseSettings;
            }

            GUILayout.Space(5);
            // Octaves
            GUILayout.Label($"Octaves: {noiseSettings.octaves}", EditorStyles.boldLabel);
            int newOctaves = (int)GUILayout.HorizontalSlider(noiseSettings.octaves, 1.0f, 6.0f);
            if (noiseSettings.octaves != newOctaves)
            {
                noiseSettings.octaves = newOctaves;
                chunkGenerator.NoiseSettings = noiseSettings;
            }

            GUILayout.Space(5);
            // Persistence
            GUILayout.Label($"Persistence: {noiseSettings.persistence:F3}", EditorStyles.boldLabel);
            float newPersistence = GUILayout.HorizontalSlider(noiseSettings.persistence, 0.0f, 1.0f);
            if (Mathf.Abs(newPersistence - noiseSettings.persistence) > 0.001f)
            {
                noiseSettings.persistence = newPersistence;
                chunkGenerator.NoiseSettings = noiseSettings;
            }

            GUILayout.Space(5);
            // Apply FallOff Map
            GUILayout.Label("ApplyFallOffMap", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            bool newApplyFallOff = GUILayout.Toggle(noiseSettings.applyFallOffMap, "");
            GUILayout.EndHorizontal();
            if (newApplyFallOff != noiseSettings.applyFallOffMap)
            {
                noiseSettings.applyFallOffMap = newApplyFallOff;
                chunkGenerator.NoiseSettings = noiseSettings;
            }

            GUILayout.Space(5);
            // FallOff Steepness
            GUILayout.Label($"FallOff Steepness: {noiseSettings.steepness:F3}", EditorStyles.boldLabel);
            float newSteepness = GUILayout.HorizontalSlider(noiseSettings.steepness, 0.1f, 10.0f);
            if (Mathf.Abs(newSteepness - noiseSettings.steepness) > 0.001f)
            {
                noiseSettings.steepness = newSteepness;
                chunkGenerator.NoiseSettings = noiseSettings;
            }

            GUILayout.Space(5);
            // FallOff Center Size
            GUILayout.Label($"FallOff Center Size: {noiseSettings.centerSize:F3}", EditorStyles.boldLabel);
            float newCenterSize = GUILayout.HorizontalSlider(noiseSettings.centerSize, 0.1f, 10.0f);
            if (Mathf.Abs(newCenterSize - noiseSettings.centerSize) > 0.001f)
            {
                noiseSettings.centerSize = newCenterSize;
                chunkGenerator.NoiseSettings = noiseSettings;
            }
        }

        private void DrawLightingControls()
        {
            GUILayout.Label("Lighting", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            // Light Rotation
            GUILayout.Label("Light Rotation", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            
            var rotation = chunkGenerator.MainLightRotation;
            
            float newRx = DraggableFloatField("X:", rotation.x, "rotX", 0.5f);
            float newRy = DraggableFloatField("Y:", rotation.y, "rotY", 0.5f);
            float newRz = DraggableFloatField("Z:", rotation.z, "rotZ", 0.5f);

            Vector3 newRot = new Vector3(newRx, newRy, newRz);
            if (newRot != chunkGenerator.MainLightRotation)
            {
                chunkGenerator.MainLightRotation = newRot;
                rotXText = newRx.ToString("F1");
                rotYText = newRy.ToString("F1");
                rotZText = newRz.ToString("F1");
            }
            
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            // Light Intensity
            GUILayout.Label($"Intensity: {chunkGenerator.Intensity:F3}", EditorStyles.boldLabel);
            float newIntensity = GUILayout.HorizontalSlider(chunkGenerator.Intensity, 0f, 100f);
            if (Mathf.Abs(newIntensity - chunkGenerator.Intensity) > 0.001f)
            {
                chunkGenerator.Intensity = newIntensity;
            }

            GUILayout.Space(5);
            // Shadow Strength
            GUILayout.Label($"Shadow Strength {chunkGenerator.ShadowStrength:F3}", EditorStyles.boldLabel);
            float newShadow = GUILayout.HorizontalSlider(chunkGenerator.ShadowStrength, 0f, 1f);
            if (Mathf.Abs(newShadow - chunkGenerator.ShadowStrength) > 0.001f)
            {
                chunkGenerator.ShadowStrength = newShadow;
            }
        }

        private void DrawFPSCounter()
        {
            fpsTimer -= Time.deltaTime;
            if (fpsTimer <= 0)
            {
                fps = (int)(1.0f / Time.unscaledDeltaTime);
                fpsTimer = 1.0f;
            }
            
            GUILayout.Label($"FPS: {fps}");
            
        }

        private void DrawGizmoButton()
        {
            if (GUILayout.Button(chunkGenerator.Guizmo ? "Hide Gizmo" : "Show Gizmo"))
            {
                chunkGenerator.Guizmo = !chunkGenerator.Guizmo;
            }
        }
    }