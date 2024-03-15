using System;
using System.Collections;
using Godot;

public partial class GenerateTerrain : Node3D
{
    [Export]
    public int size = 15;

    [Export]
    public int chunk_size = 32;

    private PackedScene chunk_scene = GD.Load<PackedScene>("res://chunk.tscn");
    private NoiseTexture2D noise = new NoiseTexture2D();

    private Hashtable chunk_cache = new Hashtable();
    private Vector2I last_chunk = Vector2I.Zero;

    public override void _Ready()
    {
        noise.Noise = new FastNoiseLite();

        for (int x = -((size - 1) / 2); x < ((size + 1) / 2); x++)
        {
            for (int z = -((size - 1) / 2); z < ((size + 1) / 2); z++)
            {
                GenerateChunk(x, z);
            }
        }
    }

    private void GenerateChunk(int x, int z)
    {
        string id = $"{x}_{z}";

        if (HasNode(id))
        {
            return;
        }

        if (chunk_cache.ContainsKey(id))
        {
            AddChild(chunk_cache[id] as Node3D);
            return;
        }

        Node3D chunk = chunk_scene.Instantiate<Node3D>();
        chunk
            .GetChild<GenerateChunk>(0)
            .Generate(id, chunk_size, noise.Noise, new Vector3(x * chunk_size, 0, z * chunk_size));
        chunk.Translate(new Vector3(x * chunk_size, 0, z * chunk_size));
        chunk_cache[id] = chunk;
        AddChild(chunk);
    }

    private void DestroyChunk(int x, int z)
    {
        string id = $"{x}_{z}";

        if (HasNode(id))
        {
            RemoveChild(GetNode(id));
        }
    }

    public override void _Process(double delta)
    {
        Node3D camera = GetNode("/root/Scene/Camera3D") as Node3D;
        Vector2I current_chunk = Vector2I.Zero;
        current_chunk.X += (int)MathF.Round(camera.GlobalPosition.X / chunk_size);
        current_chunk.Y += (int)MathF.Round(camera.GlobalPosition.Z / chunk_size);

        if (current_chunk == last_chunk)
        {
            return;
        }

        Vector2I chunk_delta = current_chunk - last_chunk;

        for (int i = 0; i < size; i++)
        {
            GenerateChunk(
                current_chunk.X + chunk_delta.X * ((size - 1) / 2) + Math.Abs(chunk_delta.Y) * (-(size - 1) / 2 + i),
                current_chunk.Y + chunk_delta.Y * ((size - 1) / 2) + Math.Abs(chunk_delta.X) * (-(size - 1) / 2 + i)
            );

            DestroyChunk(
                current_chunk.X + chunk_delta.X * (-(size + 1) / 2) + Math.Abs(chunk_delta.Y) * (-(size - 1) / 2 + i),
                current_chunk.Y + chunk_delta.Y * (-(size + 1) / 2) + Math.Abs(chunk_delta.X) * (-(size - 1) / 2 + i)
            );
        }
        last_chunk = current_chunk;
    }
}
