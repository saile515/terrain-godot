using System;
using System.Collections.Generic;
using Godot;

public partial class GenerateChunk : MeshInstance3D
{
    private ArrayMesh mesh = new ArrayMesh();
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> indices = new List<int>();
    private List<Vector2> UVs = new List<Vector2>();
    private ShaderMaterial material = new ShaderMaterial();

    public void Generate(
        string id,
        int size,
        DiamondSquare height_map,
        Vector3 world_offset,
        int lod
    )
    {
        GetParent().Name = id;
        int lod_size = (int)Math.Round(Math.Pow(2, lod - 1));

        for (int x = 0; x <= size * lod_size; x += lod_size)
        {
            for (int z = 0; z <= size * lod_size; z += lod_size)
            {
                vertices.Add(
                    new Vector3(
                        x,
                        height_map.get((int)world_offset.X + x, (int)world_offset.Z + z),
                        z
                    )
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

        material.Shader = GD.Load<Shader>("terrain.gdshader");

        SurfaceTool surface_tool = new SurfaceTool();
        surface_tool.Begin(Mesh.PrimitiveType.Triangles);
        surface_tool.SetMaterial(material);
        surface_tool.SetSmoothGroup(UInt32.MaxValue);

        for (int i = 0; i < vertices.Count; i++)
        {
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
