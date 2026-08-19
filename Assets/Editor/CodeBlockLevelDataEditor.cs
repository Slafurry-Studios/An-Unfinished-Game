using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CodeBlockLevelData))]
public class CodeBlockLevelDataEditor : Editor
{
    private CodeBlockLevelData level;
    private const int CellSize = 50;

    enum CellState { Empty, Input, Output }

    void OnEnable()
    {
        level = (CodeBlockLevelData)target;
        if (level.inputs == null) level.inputs = new CodeBlockLevelData.FixedInput[0];
        if (level.outputs == null) level.outputs = new CodeBlockLevelData.FixedOutput[0];
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("width"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("height"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("availableGates"), true);

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Grid Editor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Klik cell buat siklus: Kosong -> Input(0) -> Input(1) -> Output(0) -> Output(1) -> Kosong lagi.",
            MessageType.Info);

        EditorGUILayout.Space(4);
        DrawGrid();

        EditorGUILayout.Space(10);
        DrawSummary();
    }

    void DrawGrid()
    {
        for (int y = level.height - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            for (int x = 0; x < level.width; x++)
            {
                DrawCellButton(x, y);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }

    void DrawCellButton(int x, int y)
    {
        CellState state = GetCellState(x, y, out int valueIndex);
        string label = GetLabel(state, valueIndex);

        Color prevColor = GUI.backgroundColor;
        GUI.backgroundColor = GetColor(state, valueIndex);

        if (GUILayout.Button(label, GUILayout.Width(CellSize), GUILayout.Height(CellSize)))
        {
            CycleCell(x, y);
        }

        GUI.backgroundColor = prevColor;
    }

    // valueIndex: index ke array inputs/outputs kalau ketemu, dan sekaligus dipakai buat tau nilai true/false-nya
    CellState GetCellState(int x, int y, out int valueIndex)
    {
        for (int i = 0; i < level.inputs.Length; i++)
        {
            if (level.inputs[i].x == x && level.inputs[i].y == y)
            {
                valueIndex = i;
                return CellState.Input;
            }
        }
        for (int i = 0; i < level.outputs.Length; i++)
        {
            if (level.outputs[i].x == x && level.outputs[i].y == y)
            {
                valueIndex = i;
                return CellState.Output;
            }
        }
        valueIndex = -1;
        return CellState.Empty;
    }

    string GetLabel(CellState state, int valueIndex)
    {
        switch (state)
        {
            case CellState.Input:
                return $"IN\n{(level.inputs[valueIndex].value ? 1 : 0)}";
            case CellState.Output:
                return $"OUT\n{(level.outputs[valueIndex].targetValue ? 1 : 0)}";
            default:
                return "";
        }
    }

    Color GetColor(CellState state, int valueIndex)
    {
        switch (state)
        {
            case CellState.Input:
                return level.inputs[valueIndex].value
                    ? new Color(0.3f, 0.7f, 1f)
                    : new Color(0.6f, 0.8f, 1f);
            case CellState.Output:
                return level.outputs[valueIndex].targetValue
                    ? new Color(1f, 0.6f, 0.2f)
                    : new Color(1f, 0.8f, 0.5f);
            default:
                return Color.white;
        }
    }

    void CycleCell(int x, int y)
    {
        Undo.RecordObject(level, "Edit Code Block Level Cell");

        CellState state = GetCellState(x, y, out int valueIndex);
        List<CodeBlockLevelData.FixedInput> inputs = new List<CodeBlockLevelData.FixedInput>(level.inputs);
        List<CodeBlockLevelData.FixedOutput> outputs = new List<CodeBlockLevelData.FixedOutput>(level.outputs);

        switch (state)
        {
            case CellState.Empty:
                inputs.Add(new CodeBlockLevelData.FixedInput { x = x, y = y, value = false });
                break;

            case CellState.Input:
                if (!inputs[valueIndex].value)
                {
                    var updated = inputs[valueIndex];
                    updated.value = true;
                    inputs[valueIndex] = updated;
                }
                else
                {
                    inputs.RemoveAt(valueIndex);
                    outputs.Add(new CodeBlockLevelData.FixedOutput { x = x, y = y, targetValue = false });
                }
                break;

            case CellState.Output:
                if (!outputs[valueIndex].targetValue)
                {
                    var updated = outputs[valueIndex];
                    updated.targetValue = true;
                    outputs[valueIndex] = updated;
                }
                else
                {
                    outputs.RemoveAt(valueIndex);
                }
                break;
        }

        level.inputs = inputs.ToArray();
        level.outputs = outputs.ToArray();

        EditorUtility.SetDirty(level);
    }

    void DrawSummary()
    {
        EditorGUILayout.LabelField("Ringkasan", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Jumlah Input : {level.inputs.Length}");
        EditorGUILayout.LabelField($"Jumlah Output: {level.outputs.Length}");
    }
}