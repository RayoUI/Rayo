using Lua;

namespace Nano.GameEngine;

/// <summary>Lua binding for <see cref="NanoPhysicsService"/>.</summary>
internal sealed class NanoPhysicsLuaApi(NanoPhysicsService physics)
{
    public LuaTable CreateTable() => new()
    {
        ["new_world"] = Function(context =>
        {
            var gravityX = OptionalNumber(context, 0, 0);
            var gravityY = OptionalNumber(context, 1, 980);
            return Return(context, physics.CreateWorld(gravityX, gravityY));
        }),
        ["destroy_world"] = Function(context => { physics.DestroyWorld(Integer(context, 0)); return 0; }),
        ["new_circle"] = Function(context => Return(context, physics.CreateCircle(
            Integer(context, 0), Number(context, 1), Number(context, 2), Number(context, 3),
            OptionalNumber(context, 4, 1), OptionalKind(context, 5)))),
        ["new_box"] = Function(context => Return(context, physics.CreateBox(
            Integer(context, 0), Number(context, 1), Number(context, 2), Number(context, 3), Number(context, 4),
            OptionalNumber(context, 5, 1), OptionalKind(context, 6)))),
        ["destroy_body"] = Function(context => { physics.DestroyBody(Integer(context, 0)); return 0; }),
        ["step"] = Function(context =>
        {
            physics.Step(Integer(context, 0), Number(context, 1), (int)OptionalNumber(context, 2, 4));
            return 0;
        }),
        ["set_position"] = Function(context =>
        {
            physics.SetPosition(Integer(context, 0), Number(context, 1), Number(context, 2));
            return 0;
        }),
        ["set_velocity"] = Function(context =>
        {
            physics.SetVelocity(Integer(context, 0), Number(context, 1), Number(context, 2));
            return 0;
        }),
        ["apply_force"] = Function(context =>
        {
            physics.ApplyForce(Integer(context, 0), Number(context, 1), Number(context, 2));
            return 0;
        }),
        ["apply_impulse"] = Function(context =>
        {
            physics.ApplyImpulse(Integer(context, 0), Number(context, 1), Number(context, 2));
            return 0;
        }),
        ["set_gravity_scale"] = Function(context =>
        {
            physics.SetGravityScale(Integer(context, 0), Number(context, 1));
            return 0;
        }),
        ["set_restitution"] = Function(context =>
        {
            physics.SetRestitution(Integer(context, 0), Number(context, 1));
            return 0;
        }),
        ["set_friction"] = Function(context =>
        {
            physics.SetFriction(Integer(context, 0), Number(context, 1));
            return 0;
        }),
        ["body"] = Function(context =>
        {
            var body = physics.GetBody(Integer(context, 0));
            context.Return(new LuaTable
            {
                ["x"] = body.X,
                ["y"] = body.Y,
                ["vx"] = body.VelocityX,
                ["vy"] = body.VelocityY,
                ["type"] = body.Kind.ToString().ToLowerInvariant(),
                ["shape"] = body.Shape.ToString().ToLowerInvariant(),
                ["radius"] = body.Radius,
                ["width"] = body.Width,
                ["height"] = body.Height
            });
            return 1;
        }),
        ["is_touching"] = Function(context =>
        {
            context.Return(physics.IsTouching(Integer(context, 0), Integer(context, 1)));
            return 1;
        }),
        ["contacts"] = Function(context =>
        {
            var result = new LuaTable();
            var contacts = physics.Contacts(Integer(context, 0));
            for (var index = 0; index < contacts.Count; index++)
            {
                var contact = contacts[index];
                result[index + 1] = new LuaTable
                {
                    ["a"] = contact.BodyA,
                    ["b"] = contact.BodyB,
                    ["normal_x"] = contact.NormalX,
                    ["normal_y"] = contact.NormalY,
                    ["penetration"] = contact.Penetration
                };
            }
            context.Return(result);
            return 1;
        })
    };

    private static BodyKind OptionalKind(LuaFunctionExecutionContext context, int index)
    {
        if (context.ArgumentCount <= index)
            return BodyKind.Dynamic;
        return context.GetArgument<string>(index).ToLowerInvariant() switch
        {
            "static" => BodyKind.Static,
            "kinematic" => BodyKind.Kinematic,
            "dynamic" => BodyKind.Dynamic,
            _ => throw new InvalidOperationException("Body type must be dynamic, static, or kinematic.")
        };
    }

    private static float OptionalNumber(LuaFunctionExecutionContext context, int index, float fallback) =>
        context.ArgumentCount > index ? Number(context, index) : fallback;

    private static int Integer(LuaFunctionExecutionContext context, int index) =>
        (int)context.GetArgument<double>(index);

    private static float Number(LuaFunctionExecutionContext context, int index) =>
        (float)context.GetArgument<double>(index);

    private static int Return(LuaFunctionExecutionContext context, int value)
    {
        context.Return(value);
        return 1;
    }

    private static LuaFunction Function(Func<LuaFunctionExecutionContext, int> callback) =>
        new((context, _) => new ValueTask<int>(callback(context)));
}
