using Raylib_cs;
using System.Numerics;
using System.Runtime.CompilerServices;

class PhysicsBody : IPhysicsBody
{
    public int Id { get; set; }

    public Vector2 Position { get; set; }
    public float Angle { get; set; } // z axis, in radians

    // forces related
    private Vector2 LinearVelocity { get; set; }
    private float AngularVelocity { get; set; }

    public float LinearDamping { get; set; } = 0.98f; // friction
    public float AngularDamping { get; set; } = 0.95f; // angular friction

    // body related
    public float Mass { get; set; } = 1.0f;
    public float RadiusCollider { get; set; } // circle
    public float Bounciness { get; set; } = 0.9f;

    public bool IsMoving
        => IsActive && (LinearVelocity != Vector2.Zero || AngularVelocity != 0f);
    public bool IsActive { get; set; } = true;

    public PhysicsBody(float mass, float radius, Vector2 position)
    {
        Reset();

        Position = position;
        Mass = mass;
        RadiusCollider = radius;
    }

    [MethodImpl(256)]
    public void Update(float deltaTime)
    {
        if (!IsActive) return;

        LinearVelocity *= LinearDamping;
        Position += LinearVelocity * deltaTime;

        AngularVelocity *= AngularDamping;
        Angle += AngularVelocity * deltaTime;

        // stop when slow
        if (LinearVelocity.LengthSquared() < 1f)
            LinearVelocity = default;

        if (AngularVelocity.Abs() < 1f)
            AngularVelocity = default;
    }

    [MethodImpl(256)]
    public bool CheckCollision(PhysicsBody other)
    {
        if ((!IsMoving && !other.IsMoving) || (!IsActive || !other.IsActive))
            return false;

        float distance = (Position - other.Position).Length();
        return distance < RadiusCollider + other.RadiusCollider;
    }

    public void ResolveCollision(PhysicsBody other)
    {
        var collisionNormal = (other.Position - Position).Normalized();

        float distance = Vector2.Distance(Position, other.Position);
        float overlap = (RadiusCollider + other.RadiusCollider) - distance;

        if (overlap > 0)
        {
            float totalMass = Mass + other.Mass;
            float ratio1 = other.Mass / totalMass; // balls for now have the same mass, but...
            float ratio2 = Mass / totalMass;

            Position -= collisionNormal * overlap * ratio1;
            other.Position += collisionNormal * overlap * ratio2;
        }

        Vector2 relativeVelocity = LinearVelocity - other.LinearVelocity;

        float restitution = 0.95f;
        float impulse = (1f + restitution) * Vector2.Dot(relativeVelocity, collisionNormal) / (Mass + other.Mass);

        LinearVelocity -= impulse * other.Mass * collisionNormal;
        other.LinearVelocity += impulse * Mass * collisionNormal;

        Vector2 tangent = new(-collisionNormal.Y, collisionNormal.X);
        float torque = Vector2.Dot(relativeVelocity, tangent) * 0.1f;

        AngularVelocity += torque / (RadiusCollider * RadiusCollider);
        other.AngularVelocity += torque / (other.RadiusCollider * other.RadiusCollider);
    }

    public void ResolveWallCollisions(in Rectangle bounds)
    {
        if (!IsMoving || !IsActive) return;

        if (Raylib.CheckCollisionCircleRec(Position, RadiusCollider, bounds))
        {
            // Left
            if (Position.X - RadiusCollider < bounds.X)
            {
                Position = new Vector2(bounds.X + RadiusCollider, Position.Y);
                LinearVelocity = new Vector2(-LinearVelocity.X * Bounciness, LinearVelocity.Y);
            }
            // Right
            else if (Position.X + RadiusCollider > bounds.X + bounds.Width)
            {
                Position = new Vector2(bounds.X + bounds.Width - RadiusCollider, Position.Y);
                LinearVelocity = new Vector2(-LinearVelocity.X * Bounciness, LinearVelocity.Y);
            }

            // Top (inverted y)
            if (Position.Y - RadiusCollider < bounds.Y)
            {
                Position = new Vector2(Position.X, bounds.Y + RadiusCollider);
                LinearVelocity = new Vector2(LinearVelocity.X, -LinearVelocity.Y * Bounciness);
            }
            // Bottom
            else if (Position.Y + RadiusCollider > bounds.Y + bounds.Height)
            {
                Position = new Vector2(Position.X, bounds.Y + bounds.Height - RadiusCollider);
                LinearVelocity = new Vector2(LinearVelocity.X, -LinearVelocity.Y * Bounciness);
            }
        }
    }

    [MethodImpl(256)]
    public bool CheckFall((Vector2 position, float radius)[] colliders)
    {
        if (!IsActive) return false;

        for (int i = 0; i < colliders.Length; i++)
        {
            var other = colliders[i].position;
            var holeRadius = colliders[i].radius;

            var delta = Position - other; // distance calc (cheaper check)

            float totalRadius = ((RadiusCollider + holeRadius) * 0.88f); // margin

            if (delta.LengthSquared() < totalRadius * totalRadius)
                return true;
        }
        return false;
    }

    public void SetVelocity(Vector2 velocity)
    {
        LinearVelocity = velocity;
    }

    public void Reset()
    {
        Angle = 0f;
        LinearVelocity = Vector2.Zero;
        AngularVelocity = 0f;

        IsActive = true;
    }
}