using UnityEngine;

public class CodeBlockLevelLoader : MonoBehaviour
{
    public CodeBlockCircuit circuit;
    public CodeBlockLevelData currentLevel;

    private bool[] targetOutputs;

    public void LoadLevel(CodeBlockLevelData level)
    {
        currentLevel = level;
        circuit.InitGrid(level.width, level.height);

        targetOutputs = new bool[level.outputs.Length];

        foreach (var input in level.inputs)
            circuit.SetFixedInput(input.x, input.y, input.value);

        for (int i = 0; i < level.outputs.Length; i++)
        {
            var output = level.outputs[i];
            circuit.SetOutput(output.x, output.y, i);
            targetOutputs[i] = output.targetValue;
        }

        circuit.Simulate();
    }

    // Panggil ini tiap kali player naruh/hapus gate, buat cek menang atau belum
    public bool IsLevelComplete()
    {
        if (currentLevel == null) return false;
        return circuit.CheckWin(targetOutputs);
    }
}