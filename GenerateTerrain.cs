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
    private DiamondSquare height_map = new(tile_size * chunk_size + 1);

    private readonly Hashtable[] chunk_cache = { new(), new(), new() };
    private Vector2I last_chunk = Vector2I.Zero;

    public override void _Ready()
    {
        Camera3D camera = GetNode<Camera3D>("/root/Scene/Camera3D");
        float camera_height = height_map.get(
            tile_size * chunk_size / 2,
            tile_size * chunk_size / 2
        );
        camera.Translate(
            new Vector3(
                tile_size * chunk_size / 2,
                (camera_height > -150 ? camera_height : -150) + 5,
                tile_size * chunk_size / 2
            )
        );

        Vector2I current_chunk = Vector2I.Zero;
        current_chunk.X += (int)MathF.Round(camera.GlobalPosition.X / chunk_size);
        current_chunk.Y += (int)MathF.Round(camera.GlobalPosition.Z / chunk_size);

        void GenerateThread()
        {
            for (int lod = 1; lod < 4; lod++)
            {
                int lod_size = (int)Math.Round(Math.Pow(3, lod - 1));
                for (int x = 0; x < render_distance; x++)
                {
                    int local_x = (int)MathF.Floor(current_chunk.X / lod_size) + x;
                    for (int z = 0; z < render_distance; z++)
                    {
                        int local_z = (int)MathF.Floor(current_chunk.Y / lod_size) + z;

                        GenerateChunk(
                            local_x - render_distance / 2,
                            local_z - render_distance / 2,
                            lod
                        );
                    }
                }
            }
        }

        Thread thread = new(new ThreadStart(GenerateThread));
        thread.Start();
    }

    public void GenerateChunk(int x, int z, int lod)
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

    public void RemoveChunk(int x, int y, int lod)
    {
        string id = $"{lod}_{x}_{y}";
        if (HasNode(id))
        {
            RemoveChild(GetNode($"{lod}_{x}_{y}"));
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

        void GenerateThread()
        {
            for (int lod = 1; lod < 4; lod++)
            {
                int lod_size = (int)Math.Round(Math.Pow(3, lod - 1));
                Vector2I chunk_delta =
                    new(
                        ((int)MathF.Floor(current_chunk.X / lod_size))
                            - ((int)MathF.Floor(last_chunk.X / lod_size)),
                        ((int)MathF.Floor(current_chunk.Y / lod_size))
                            - ((int)MathF.Floor(last_chunk.Y / lod_size))
                    );

                if (chunk_delta.Length() == 0)
                {
                    continue;
                }

                int local_x = (int)MathF.Floor(current_chunk.X / lod_size);
                int local_z = (int)MathF.Floor(current_chunk.Y / lod_size);
                int offset = (int)MathF.Floor(render_distance / 2);

                for (int i = 0; i < render_distance; i++)
                {
                    CallDeferred(
                        nameof(RemoveChunk),
                        local_x
                            + Math.Abs(chunk_delta.Y) * (i - offset)
                            + chunk_delta.X * (-1 - offset),
                        local_z
                            + Math.Abs(chunk_delta.X) * (i - offset)
                            + chunk_delta.Y * (-1 - offset),
                        lod
                    );
                    CallDeferred(
                        nameof(GenerateChunk),
                        local_x + Math.Abs(chunk_delta.Y) * (i - offset) + chunk_delta.X * offset,
                        local_z + Math.Abs(chunk_delta.X) * (i - offset) + chunk_delta.Y * offset,
                        lod
                    );
                }
            }

            last_chunk = current_chunk;
        }

        Thread thread = new(new ThreadStart(GenerateThread));
        thread.Start();
    }
}
