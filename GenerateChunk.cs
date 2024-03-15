using System;
using System.Collections.Generic;
using Godot;

public partial class GenerateChunk : MeshInstance3D
{
    private ArrayMesh mesh = new ArrayMesh();
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> indices = new List<int>();
    private List<Vector2> UVs = new List<Vector2>();
    private Material material = new Material();

    public void Generate(int size, Noise noise, Vector3 world_offset)
    {
        GetParent().Name = $"{world_offset.X}:{world_offset.Z}";

        for (int x = 0; x <= size; x++)
        {
            for (int z = 0; z <= size; z++)
            {
                vertices.Add(
                    new Vector3(x, noise.GetNoise2D(world_offset.X + x, world_offset.Z + z) * 10, z)
                );
                UVs.Add(new Vector2(x / (size + 1), z / (size + 1)));
            }
        }

        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                indices.Add(x + z * (size + 1));
                indices.Add(x + (z + 1) * (size + 1));
                indices.Add(x + 1 + (z + 1) * (size + 1));
                indices.Add(x + z * (size + 1));
                indices.Add(x + 1 + (z + 1) * (size + 1));
                indices.Add(x + 1 + z * (size + 1));
            }
        }

        SurfaceTool surface_tool = new SurfaceTool();
        surface_tool.Begin(Mesh.PrimitiveType.Triangles);
        surface_tool.SetMaterial(material);
        surface_tool.SetSmoothGroup(UInt32.MaxValue);

        for (int i = 0; i < vertices.Count; i++)
        {
            surface_tool.SetColor(new Color(1.0f, 1.0f, 1.0f));
            surface_tool.SetUV(UVs[i]);
            surface_tool.AddVertex(vertices[i]);
        }

        for (int i = 0; i < indices.Count; i++)
        {
            surface_tool.AddIndex(indices[i]);
        }
        surface_tool.GenerateNormals();
        surface_tool.GenerateTangents();
        surface_tool.Commit(mesh);
        Mesh = mesh;
    }
}
