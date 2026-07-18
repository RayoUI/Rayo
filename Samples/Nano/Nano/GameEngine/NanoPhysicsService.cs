namespace Nano.GameEngine;

/// <summary>
/// Deterministic, lightweight 2D rigid-body simulation for circles and axis-aligned boxes.
/// Positions use pixels, time uses seconds, and forces use mass-pixels/second squared.
/// </summary>
internal sealed class NanoPhysicsService
{
    private readonly Dictionary<int, PhysicsWorld> _worlds = [];
    private readonly Dictionary<int, PhysicsBody> _bodies = [];
    private int _nextWorld;
    private int _nextBody;

    public int WorldCount => _worlds.Count;
    public int BodyCount => _bodies.Count;

    public int CreateWorld(float gravityX, float gravityY)
    {
        var handle = ++_nextWorld;
        _worlds[handle] = new PhysicsWorld(new Vector2(gravityX, gravityY));
        return handle;
    }

    public int CreateCircle(int worldHandle, float x, float y, float radius, float mass, BodyKind kind)
    {
        var body = CreateBody(worldHandle, x, y, mass, kind);
        body.Shape = ShapeKind.Circle;
        body.Radius = Math.Max(0.01f, radius);
        return body.Handle;
    }

    public int CreateBox(int worldHandle, float x, float y, float width, float height, float mass, BodyKind kind)
    {
        var body = CreateBody(worldHandle, x, y, mass, kind);
        body.Shape = ShapeKind.Box;
        body.HalfWidth = Math.Max(0.005f, width * 0.5f);
        body.HalfHeight = Math.Max(0.005f, height * 0.5f);
        return body.Handle;
    }

    public void DestroyWorld(int handle)
    {
        var world = World(handle);
        foreach (var body in world.Bodies)
            _bodies.Remove(body.Handle);
        _worlds.Remove(handle);
        world.Bodies.Clear();
        world.Contacts.Clear();
        world.Candidates.Clear();
        world.ContactPairs.Clear();
    }

    public void Clear()
    {
        foreach (var world in _worlds.Values)
        {
            world.Bodies.Clear();
            world.Contacts.Clear();
            world.Candidates.Clear();
            world.ContactPairs.Clear();
        }
        _worlds.Clear();
        _bodies.Clear();
    }

    public void DestroyBody(int handle)
    {
        var body = Body(handle);
        World(body.WorldHandle).Bodies.Remove(body);
        _bodies.Remove(handle);
    }

    public void SetPosition(int handle, float x, float y) => Body(handle).Position = new Vector2(x, y);

    public void SetVelocity(int handle, float x, float y) => Body(handle).Velocity = new Vector2(x, y);

    public void ApplyForce(int handle, float x, float y)
    {
        var body = Body(handle);
        if (body.Kind == BodyKind.Dynamic)
            body.Force += new Vector2(x, y);
    }

    public void ApplyImpulse(int handle, float x, float y)
    {
        var body = Body(handle);
        if (body.InverseMass > 0)
            body.Velocity += new Vector2(x, y) * body.InverseMass;
    }

    public void SetGravityScale(int handle, float scale) => Body(handle).GravityScale = scale;

    public void SetRestitution(int handle, float restitution) =>
        Body(handle).Restitution = Math.Clamp(restitution, 0, 1);

    public void SetFriction(int handle, float friction) =>
        Body(handle).Friction = Math.Max(0, friction);

    public PhysicsBodySnapshot GetBody(int handle)
    {
        var body = Body(handle);
        return new PhysicsBodySnapshot(
            body.Position.X,
            body.Position.Y,
            body.Velocity.X,
            body.Velocity.Y,
            body.Kind,
            body.Shape,
            body.Radius,
            body.HalfWidth * 2,
            body.HalfHeight * 2);
    }

    public IReadOnlyList<PhysicsContact> Contacts(int worldHandle) => World(worldHandle).Contacts;

    public int ContactCount(int worldHandle) => World(worldHandle).Contacts.Count;

    public bool IsGrounded(int bodyHandle, float minimumNormalY)
    {
        var body = Body(bodyHandle);
        foreach (var contact in World(body.WorldHandle).Contacts)
        {
            if (contact.BodyA == bodyHandle && contact.NormalY >= minimumNormalY)
                return true;
            if (contact.BodyB == bodyHandle && -contact.NormalY >= minimumNormalY)
                return true;
        }
        return false;
    }

    public bool IsTouching(int first, int second)
    {
        var body = Body(first);
        if (body.WorldHandle != Body(second).WorldHandle)
            return false;
        return World(body.WorldHandle).Contacts.Any(contact =>
            contact.BodyA == first && contact.BodyB == second ||
            contact.BodyA == second && contact.BodyB == first);
    }

    public void Step(int worldHandle, float deltaTime, int iterations)
    {
        var world = World(worldHandle);
        var dt = Math.Clamp(deltaTime, 0, 0.05f);
        world.Contacts.Clear();

        foreach (var body in world.Bodies)
        {
            if (body.Kind == BodyKind.Dynamic)
            {
                body.Velocity += (world.Gravity * body.GravityScale + body.Force * body.InverseMass) * dt;
                body.Position += body.Velocity * dt;
            }
            else if (body.Kind == BodyKind.Kinematic)
            {
                body.Position += body.Velocity * dt;
            }
            body.Force = default;
        }

        world.ContactPairs.Clear();
        world.Candidates.Clear();
        BuildCandidates(world);
        for (var iteration = 0; iteration < Math.Clamp(iterations, 1, 12); iteration++)
        {
            foreach (var pair in world.Candidates)
            {
                var a = pair.A;
                var b = pair.B;
                if (!TryCollide(a, b, out var collision))
                    continue;

                if (world.ContactPairs.Add((a.Handle, b.Handle)))
                {
                    world.Contacts.Add(new PhysicsContact(
                        a.Handle,
                        b.Handle,
                        collision.Normal.X,
                        collision.Normal.Y,
                        collision.Penetration));
                }
                Resolve(a, b, collision);
            }
        }
    }

    private static void BuildCandidates(PhysicsWorld world)
    {
        for (var first = 0; first < world.Bodies.Count; first++)
        {
            for (var second = first + 1; second < world.Bodies.Count; second++)
            {
                var a = world.Bodies[first];
                var b = world.Bodies[second];
                if (a.InverseMass + b.InverseMass > 0 && BoundsOverlap(a, b))
                    world.Candidates.Add((a, b));
            }
        }
    }

    private static bool BoundsOverlap(PhysicsBody a, PhysicsBody b)
    {
        var aWidth = a.Shape == ShapeKind.Circle ? a.Radius : a.HalfWidth;
        var aHeight = a.Shape == ShapeKind.Circle ? a.Radius : a.HalfHeight;
        var bWidth = b.Shape == ShapeKind.Circle ? b.Radius : b.HalfWidth;
        var bHeight = b.Shape == ShapeKind.Circle ? b.Radius : b.HalfHeight;
        return MathF.Abs(a.Position.X - b.Position.X) <= aWidth + bWidth &&
               MathF.Abs(a.Position.Y - b.Position.Y) <= aHeight + bHeight;
    }

    private PhysicsBody CreateBody(int worldHandle, float x, float y, float mass, BodyKind kind)
    {
        var world = World(worldHandle);
        var handle = ++_nextBody;
        var body = new PhysicsBody
        {
            Handle = handle,
            WorldHandle = worldHandle,
            Position = new Vector2(x, y),
            Kind = kind,
            Mass = kind == BodyKind.Dynamic ? Math.Max(0.001f, mass) : float.PositiveInfinity
        };
        world.Bodies.Add(body);
        _bodies[handle] = body;
        return body;
    }

    private static bool TryCollide(PhysicsBody a, PhysicsBody b, out Collision collision)
    {
        if (a.Shape == ShapeKind.Circle && b.Shape == ShapeKind.Circle)
            return CircleCircle(a, b, out collision);
        if (a.Shape == ShapeKind.Box && b.Shape == ShapeKind.Box)
            return BoxBox(a, b, out collision);
        if (a.Shape == ShapeKind.Circle)
            return CircleBox(a, b, out collision);

        var result = CircleBox(b, a, out collision);
        collision = collision with { Normal = -collision.Normal };
        return result;
    }

    private static bool CircleCircle(PhysicsBody a, PhysicsBody b, out Collision collision)
    {
        var difference = b.Position - a.Position;
        var distanceSquared = difference.LengthSquared;
        var radius = a.Radius + b.Radius;
        if (distanceSquared >= radius * radius)
        {
            collision = default;
            return false;
        }

        var distance = MathF.Sqrt(distanceSquared);
        var normal = distance > 0.00001f ? difference / distance : new Vector2(1, 0);
        collision = new Collision(normal, radius - distance);
        return true;
    }

    private static bool BoxBox(PhysicsBody a, PhysicsBody b, out Collision collision)
    {
        var difference = b.Position - a.Position;
        var overlapX = a.HalfWidth + b.HalfWidth - MathF.Abs(difference.X);
        var overlapY = a.HalfHeight + b.HalfHeight - MathF.Abs(difference.Y);
        if (overlapX <= 0 || overlapY <= 0)
        {
            collision = default;
            return false;
        }

        collision = overlapX < overlapY
            ? new Collision(new Vector2(MathF.Sign(difference.X == 0 ? 1 : difference.X), 0), overlapX)
            : new Collision(new Vector2(0, MathF.Sign(difference.Y == 0 ? 1 : difference.Y)), overlapY);
        return true;
    }

    private static bool CircleBox(PhysicsBody circle, PhysicsBody box, out Collision collision)
    {
        var left = box.Position.X - box.HalfWidth;
        var right = box.Position.X + box.HalfWidth;
        var top = box.Position.Y - box.HalfHeight;
        var bottom = box.Position.Y + box.HalfHeight;
        var closest = new Vector2(
            Math.Clamp(circle.Position.X, left, right),
            Math.Clamp(circle.Position.Y, top, bottom));
        var towardBox = closest - circle.Position;
        var distanceSquared = towardBox.LengthSquared;

        if (distanceSquared > 0.000001f)
        {
            if (distanceSquared >= circle.Radius * circle.Radius)
            {
                collision = default;
                return false;
            }
            var distance = MathF.Sqrt(distanceSquared);
            collision = new Collision(towardBox / distance, circle.Radius - distance);
            return true;
        }

        var leftDistance = circle.Position.X - left;
        var rightDistance = right - circle.Position.X;
        var topDistance = circle.Position.Y - top;
        var bottomDistance = bottom - circle.Position.Y;
        var minimum = Math.Min(Math.Min(leftDistance, rightDistance), Math.Min(topDistance, bottomDistance));
        collision = minimum == leftDistance
            ? new Collision(new Vector2(1, 0), circle.Radius + leftDistance)
            : minimum == rightDistance
                ? new Collision(new Vector2(-1, 0), circle.Radius + rightDistance)
                : minimum == topDistance
                    ? new Collision(new Vector2(0, 1), circle.Radius + topDistance)
                    : new Collision(new Vector2(0, -1), circle.Radius + bottomDistance);
        return true;
    }

    private static void Resolve(PhysicsBody a, PhysicsBody b, Collision collision)
    {
        var inverseMass = a.InverseMass + b.InverseMass;
        if (inverseMass <= 0)
            return;

        const float correctionPercent = 0.85f;
        const float slop = 0.01f;
        var correction = collision.Normal * (Math.Max(collision.Penetration - slop, 0) / inverseMass * correctionPercent);
        a.Position -= correction * a.InverseMass;
        b.Position += correction * b.InverseMass;

        var relativeVelocity = b.Velocity - a.Velocity;
        var velocityAlongNormal = Vector2.Dot(relativeVelocity, collision.Normal);
        if (velocityAlongNormal > 0)
            return;

        var impulseMagnitude = -(1 + Math.Min(a.Restitution, b.Restitution)) * velocityAlongNormal / inverseMass;
        var impulse = collision.Normal * impulseMagnitude;
        a.Velocity -= impulse * a.InverseMass;
        b.Velocity += impulse * b.InverseMass;

        relativeVelocity = b.Velocity - a.Velocity;
        var tangent = relativeVelocity - collision.Normal * Vector2.Dot(relativeVelocity, collision.Normal);
        if (tangent.LengthSquared <= 0.000001f)
            return;
        tangent = tangent / MathF.Sqrt(tangent.LengthSquared);
        var frictionMagnitude = -Vector2.Dot(relativeVelocity, tangent) / inverseMass;
        var frictionLimit = impulseMagnitude * MathF.Sqrt(a.Friction * b.Friction);
        var frictionImpulse = tangent * Math.Clamp(frictionMagnitude, -frictionLimit, frictionLimit);
        a.Velocity -= frictionImpulse * a.InverseMass;
        b.Velocity += frictionImpulse * b.InverseMass;
    }

    private PhysicsWorld World(int handle) => _worlds.TryGetValue(handle, out var world)
        ? world
        : throw new InvalidOperationException($"Physics world {handle} does not exist.");

    private PhysicsBody Body(int handle) => _bodies.TryGetValue(handle, out var body)
        ? body
        : throw new InvalidOperationException($"Physics body {handle} does not exist.");

    private sealed class PhysicsWorld(Vector2 gravity)
    {
        public Vector2 Gravity { get; } = gravity;
        public List<PhysicsBody> Bodies { get; } = [];
        public List<PhysicsContact> Contacts { get; } = [];
        public List<(PhysicsBody A, PhysicsBody B)> Candidates { get; } = [];
        public HashSet<(int A, int B)> ContactPairs { get; } = [];
    }

    private sealed class PhysicsBody
    {
        public int Handle { get; init; }
        public int WorldHandle { get; init; }
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public Vector2 Force { get; set; }
        public BodyKind Kind { get; init; }
        public ShapeKind Shape { get; set; }
        public float Mass { get; init; }
        public float InverseMass => Kind == BodyKind.Dynamic ? 1 / Mass : 0;
        public float Radius { get; set; }
        public float HalfWidth { get; set; }
        public float HalfHeight { get; set; }
        public float GravityScale { get; set; } = 1;
        public float Restitution { get; set; } = 0.15f;
        public float Friction { get; set; } = 0.4f;
    }

    private readonly record struct Collision(Vector2 Normal, float Penetration);

    private readonly record struct Vector2(float X, float Y)
    {
        public float LengthSquared => X * X + Y * Y;
        public static float Dot(Vector2 a, Vector2 b) => a.X * b.X + a.Y * b.Y;
        public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
        public static Vector2 operator -(Vector2 value) => new(-value.X, -value.Y);
        public static Vector2 operator *(Vector2 value, float scalar) => new(value.X * scalar, value.Y * scalar);
        public static Vector2 operator /(Vector2 value, float scalar) => new(value.X / scalar, value.Y / scalar);
    }
}

internal enum BodyKind { Static, Dynamic, Kinematic }

internal enum ShapeKind { Circle, Box }

internal readonly record struct PhysicsBodySnapshot(
    float X, float Y, float VelocityX, float VelocityY,
    BodyKind Kind, ShapeKind Shape, float Radius, float Width, float Height);

internal readonly record struct PhysicsContact(
    int BodyA, int BodyB, float NormalX, float NormalY, float Penetration);
