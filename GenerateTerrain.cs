using System;
using System.Collections;
using System.Threading;
using Godot;

public partial class GenerateTerrain : Node3D
{
    [Export]
    public const int tile_size = 256;

    [Export]
    public const int render_distance = 27;

    [Export]
    public const int chunk_size = 32;

    private PackedScene chunk_scene = GD.Load<PackedScene>("res://chunk.tscn");
    private DiamondSquare height_map = new(tile_size * chunk_size + 1);

    private readonly Hashtable[] chunk_cache = { new(), new(), new() };
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
        int lod_size = (int)Math.Pow(3, lod - 1);
        if (
            x * lod_size < 0
            || z * lod_size < 0
            || x * lod_size > tile_size
            || z * lod_size > tile_size
        )
        {
            return;
        }
        string id = $"{lod}_{x}_{z}";

        if (chunk_cache[lod - 1].ContainsKey(id))
        {
            CallDeferred(MethodName.AddChild, chunk_cache[lod - 1][id] as Node3D);
            return;
        }

        Node3D chunk = chunk_scene.Instantiate<Node3D>();

        Vector3 world_offset = new(x * chunk_size * lod_size, 1 - lod, z * chunk_size * lod_size);

        void GenerateThread()
        {
            chunk
                .GetChild<GenerateChunk>(0)
                .Generate(id, chunk_size, height_map, world_offset, lod);
            chunk_cache[lod - 1][id] = chunk;
            chunk.CallDeferred("translate", world_offset);
            CallDeferred(MethodName.AddChild, chunk);
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

        Vector2I chunk_delta = current_chunk - last_chunk;

        var children = GetChildren();

        void GenerateThread()
        {
            foreach (Node node in children)
            {
                CallDeferred(MethodName.RemoveChild, node);
            }

            for (int lod = 1; lod < 4; lod++)
            {
                int lod_size = (int)Math.Round(Math.Pow(3, lod - 1));
                for (int x = 0; x < render_distance * lod_size; x++)
                {
                    int local_x = (int)MathF.Floor(current_chunk.X / lod_size) + x;
                    for (int z = 0; z < render_distance * lod_size; z++)
                    {
                        int local_z = (int)MathF.Floor(current_chunk.Y / lod_size) + z;

                        GenerateChunk(
                            local_x - render_distance * lod_size / 2,
                            local_z - render_distance * lod_size / 2,
                            lod
                        );
                    }
                }
            }
        }

        Thread thread = new(new ThreadStart(GenerateThread));
        thread.Start();

        last_chunk = current_chunk;
    }
}
