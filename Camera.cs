using System;
using Godot;

public partial class Camera : Camera3D
{
    [Export]
    public float move_speed = 10.0f;

    [Export]
    public float look_speed = 0.001f;

    private Vector2 rotation = Vector2.Zero;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Process(double delta)
    {
        Vector3 move_vector = Vector3.Zero;
        Basis basis = GlobalTransform.Basis;
        if (Input.IsActionPressed("move_forward"))
        {
            move_vector -= basis.Z;
        }

        if (Input.IsActionPressed("move_back"))
        {
            move_vector += basis.Z;
        }

        if (Input.IsActionPressed("move_left"))
        {
            move_vector -= basis.X;
        }

        if (Input.IsActionPressed("move_right"))
        {
            move_vector += basis.X;
        }

        if (Input.IsActionPressed("move_up"))
        {
            move_vector += Vector3.Up;
        }

        if (Input.IsActionPressed("move_down"))
        {
            move_vector += Vector3.Down;
        }
        move_vector = move_vector.Normalized();

        GlobalPosition += move_vector * move_speed * (float)delta;

        if (Input.IsKeyPressed(Key.Escape))
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouse_event)
        {
            rotation += mouse_event.Relative * look_speed;

            Transform3D transform = Transform;
            transform.Basis = Basis.Identity;
            Transform = transform;

            RotateObjectLocal(Vector3.Up, rotation.X);
            RotateObjectLocal(Vector3.Right, rotation.Y);
        }
    }
}
