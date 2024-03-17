using System;
using System.Collections.Generic;
using Godot;

public partial class DiamondSquare : GodotObject
{
    private List<List<float>> height_map = new List<List<float>>();
    private int size;
    private Random random = new Random();
    private float factor = 0.5f;
    private float height_variation;

    public DiamondSquare(int _size)
    {
        size = _size;
        height_variation = _size / 2;
        for (int x = 0; x < size; x++)
        {
            height_map.Add(new List<float>());
            for (int z = 0; z < size; z++)
            {
                height_map[x].Add(0.0f);
            }
        }

        height_map[0][0] = ((float)random.NextDouble() - 0.5f) * height_variation;
        height_map[size - 1][0] = ((float)random.NextDouble() - 0.5f) * height_variation;
        height_map[0][size - 1] = ((float)random.NextDouble() - 0.5f) * height_variation;
        height_map[size - 1][size - 1] = ((float)random.NextDouble() - 0.5f) * height_variation;

        Step(1);
    }

    public float get(int x, int z)
    {
        if (x >= height_map.Count || x < 0 || z >= height_map[x].Count || z < 0)
        {
            return 0.0f;
        }
        return height_map[x][z];
    }

    private void Step(int k)
    {
        int gap = (int)((size - 1) / Math.Pow(2, k - 1));

        if (gap == 1)
        {
            return;
        }

        for (int x = 0; x < size - gap; x += gap)
        {
            for (int z = 0; z < size - gap; z += gap)
            {
                height_map[x + gap / 2][z + gap / 2] =
                    (
                        height_map[x][z]
                        + height_map[x + gap][z]
                        + height_map[x][z + gap]
                        + height_map[x + gap][z + gap]
                    ) / 4
                    + ((float)random.NextDouble() - 0.5f) * height_variation * MathF.Pow(factor, k);
            }
        }

        for (int z = 0; z < size; z += gap / 2)
        {
            int offset = z % gap == 0 ? gap / 2 : 0;
            for (int x = 0; x < size - offset; x += gap)
            {
                int neighbors = 4;
                float total = 0.0f;

                if (x + offset == 0)
                {
                    neighbors--;
                }
                else
                {
                    total += height_map[x + offset - gap / 2][z];
                }

                if (x + offset == size - 1)
                {
                    neighbors--;
                }
                else
                {
                    total += height_map[x + offset + gap / 2][z];
                }

                if (z == 0)
                {
                    neighbors--;
                }
                else
                {
                    total += height_map[x + offset][z - gap / 2];
                }

                if (z == size - 1)
                {
                    neighbors--;
                }
                else
                {
                    total += height_map[x + offset][z + gap / 2];
                }

                height_map[x + offset][z] =
                    total / neighbors
                    + ((float)random.NextDouble() - 0.5f) * height_variation * MathF.Pow(factor, k);
            }
        }

        Step(k + 1);
    }
}
