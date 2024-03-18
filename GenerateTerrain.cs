using System;
using System.Collections;
using System.Threading;
using Godot;

public partial class GenerateTerrain : Node3D
{
    [Export]
    public const int tile_size = 256;

    [Export]
    public const int render_distance = 32;

    [Export]
    public const int chunk_size = 32;

    private PackedScene chunk_scene = GD.Load<PackedScene>("res://chunk.tscn");
    private DiamondSquare height_map = new DiamondSquare(tile_size * chunk_size + 1);

    private Hashtable chunk_cache = new Hashtable();
    private Vector2I last_chunk = Vector2I.Zero;

    public override void _Ready()
    {
        GetNode<Camera3D>("/root/Scene/Camera3D")
            .Translate(
                new Vector3(
                    tile_size * chunk_size / 2,
                    height_map.get(tile_size * chunk_size / 2, tile_size * chunk_size / 2) + 5,
                    tile_size * chunk_size / 2
                )
            );
    }

    private void GenerateChunk(int x, int z, int lod)
    {
        if (x < 0 || z < 0 || x > tile_size || z > tile_size)
        {
            return;
        }
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

        void GenerateThread()
        {
            chunk
                .GetChild<GenerateChunk>(0)
                .Generate(
                    id,
                    chunk_size,
                    height_map,
                    new Vector3(x * chunk_size, 0, z * chunk_size),
                    lod
                );
            chunk_cache[id] = chunk;
            chunk.CallDeferred("translate", new Vector3(x * chunk_size, 0, z * chunk_size));
            CallDeferred("add_child", chunk);
        }

        Thread thread = new(new ThreadStart(GenerateThread));
        thread.Start();
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

        foreach (Node node in GetChildren())
        {
            RemoveChild(node);
        }

        for (int lod = 1; lod < 4; lod++)
        {
            int lod_size = (int)Math.Round(Math.Pow(3, lod - 1));
            int chunk_size = (int)Math.Round(Math.Pow(2, lod - 1));
            for (int x = 0; x < render_distance * lod_size; x += chunk_size)
            {
                int local_x = current_chunk.X + x;
                for (int z = 0; z < render_distance * lod_size; z += chunk_size)
                {
                    int local_z = current_chunk.Y + z;
                    if (
                        lod > 1
                        && local_x < render_distance * lod_size * 2 / 3
                        && local_z < render_distance * lod_size * 2 / 3
                        && local_x > render_distance * lod_size / 3
                        && local_z > render_distance * lod_size / 3
                    )
                    {
                        continue;
                    }

                    GenerateChunk(
                        local_x - render_distance * lod_size / 2,
                        local_z - render_distance * lod_size / 2,
                        lod
                    );
                }
            }
        }

        last_chunk = current_chunk;
    }
}
