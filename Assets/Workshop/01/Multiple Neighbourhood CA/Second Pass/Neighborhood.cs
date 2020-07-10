using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Neighborhood", menuName = "Cellular Automata/MNCA/Neighborhood")]
public class Neighborhood : ScriptableObject
{
	public bool[] cells = GetDefaultCells();

#if UNITY_EDITOR
	[SerializeField] private bool radialMirror = true;
#endif

	public bool GetCell(int x, int y)
	{
		if (x > 15 || x < -15 || y > 15 || y < -15)
			throw new System.IndexOutOfRangeException("X and Y must be within -15 to 15");
		return cells[(x + 15) + 31 * (y + 15)];
	}

	private static bool[] GetDefaultCells()
	{
		var cells = new bool[31 * 31];
		cells[cells.Length / 2] = true;

		return cells;
	}

	public Vector4[] GetOffsets()
	{
		var index = 0;
		var offsets = new List<Vector4>(cells.Length);
		for (int x = -15; x <= 15; x++)
		{
			for (int y = -15; y <= 15; y++)
			{
				if (cells[index])
					offsets.Add(new Vector4(x, y));
				index++;
			}
		}

		return offsets.ToArray();
	}
}