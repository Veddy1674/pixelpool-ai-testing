using System.Numerics;
using System.Runtime.CompilerServices;
using Raylib_cs;

// optimized struct version of PhysicsBody, more hardcoded
public struct PhysicsBodyStruct(Vector2 position) : IPhysicsBody
{
    public Vector2 Position = position;
    public Vector2 LinearVelocity = Vector2.Zero;
    public bool IsActive = true;

    private const float Radius = IPoolEnv.ballRadius;
    private const float Radius2 = Radius * 2f;
    private const float Radius2Sq = Radius2 * Radius2; // precalc for CheckCollision

    public const float LinearDamping = 0.98f;
    public const float Mass = 1.0f;
    public const float MassHalf = Mass / 2f;
    public const float Mass2 = Mass + Mass;
    public const float Bounciness = 0.9f;
    private const float Restitution = 0.95f;
    private const float ImpulseCoeff = (1f + Restitution) / Mass2; // (1 + restitution) / (m1 + m2)

    // collision boundaries
    private const float left = 484 + Radius; // IPoolEnv.poolTableCollision.X + Radius;
    private const float right = 484 + 952 - Radius; // IPoolEnv.poolTableCollision.X + IPoolEnv.poolTableCollision.Width - Radius;
    private const float top = 289 + Radius; // IPoolEnv.poolTableCollision.Y + Radius;
    private const float bottom = 289 + 430 - Radius; // IPoolEnv.poolTableCollision.Y + IPoolEnv.poolTableCollision.Height - Radius;

    public readonly bool IsMoving
        => IsActive && (LinearVelocity != Vector2.Zero);

    [MethodImpl(256)]
    public void Update(float deltaTime)
    {
        if (!IsActive) return;

        LinearVelocity *= LinearDamping;
        Position += LinearVelocity * deltaTime;

        // Stop when slow
        if (LinearVelocity.LengthSquared() < 1f)
            LinearVelocity = Vector2.Zero;
    }

    [MethodImpl(256)]
    public readonly bool CheckCollision(PhysicsBodyStruct other)
    {
        if (!IsActive | !other.IsActive) return false; // bitwise OR is quicker than &&
        if (!IsMoving & !other.IsMoving) return false;

        var delta = Position - other.Position;
        return delta.LengthSquared() < Radius2Sq;
    }

    public void ResolveCollision(ref PhysicsBodyStruct other)
    {
        var delta = other.Position - Position;
        float distSq = delta.LengthSquared();

        if (distSq < 0.0001f) return;
        
        float dist = MathF.Sqrt(distSq);
        float overlap = Radius2 - dist;

        if (overlap <= 0f) return;

        // normalize with precalculated dist (avoid 2nd sqrt)
        var collisionNormal = delta / dist;
        float correction = overlap * MassHalf;

        Position -= collisionNormal * correction;
        other.Position += collisionNormal * correction;

        // impulse calc
        Vector2 relativeVelocity = LinearVelocity - other.LinearVelocity;
        float velocityAlongNormal = Vector2.Dot(relativeVelocity, collisionNormal);

        // i have no idea how to explain this. it just works
        if (velocityAlongNormal <= 0f) return;

        //float impulseScalar = (1f + Restitution) * velocityAlongNormal / Mass2;
        Vector2 impulse = (ImpulseCoeff * velocityAlongNormal) * collisionNormal;

        // mass ignored
        LinearVelocity -= impulse;
        other.LinearVelocity += impulse;
    }

    public void ResolveWallCollisions()
    {
        if (!IsMoving | !IsActive) return;

        // X axis
        if (Position.X < left)
        {
            Position.X = left;
            LinearVelocity.X *= -Bounciness;
        }
        else if (Position.X > right)
        {
            Position.X = right;
            LinearVelocity.X *= -Bounciness;
        }

        // Y axis
        if (Position.Y < top)
        {
            Position.Y = top;
            LinearVelocity.Y *= -Bounciness;
        }
        else if (Position.Y > bottom)
        {
            Position.Y = bottom;
            LinearVelocity.Y *= -Bounciness;
        }
    }

    [MethodImpl(256)]
    public readonly bool CheckFall((Vector2 position, float radius)[] colliders)
    {
        if (!IsActive) return false;

        for (int i = 0; i < colliders.Length; i++)
        {
            var other = colliders[i].position;
            var holeRadius = colliders[i].radius;

            var delta = Position - other; // distance calc (cheaper check)

            float totalRadius = ((IPoolEnv.ballRadius + holeRadius) * 0.88f); // margin

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
        LinearVelocity = Vector2.Zero;
        IsActive = true;
    }
}