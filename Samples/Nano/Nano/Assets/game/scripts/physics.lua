local physics = {}

function physics.new_arena(width, height, top)
    top = top or 0
    local world = nano.physics.new_world(0, 0)
    local wall = 32
    nano.physics.new_box(world, width * 0.5, top - wall * 0.5, width + wall * 2, wall, 1, "static")
    nano.physics.new_box(world, width * 0.5, height + wall * 0.5, width + wall * 2, wall, 1, "static")
    nano.physics.new_box(world, -wall * 0.5, (top + height) * 0.5, wall, height - top, 1, "static")
    nano.physics.new_box(world, width + wall * 0.5, (top + height) * 0.5, wall, height - top, 1, "static")
    return world
end

function physics.new_player(world, x, y, radius)
    local body = nano.physics.new_circle(world, x, y, radius, 1, "dynamic")
    nano.physics.set_gravity_scale(body, 0)
    nano.physics.set_restitution(body, 0.2)
    nano.physics.set_friction(body, 0.25)
    return body
end

return physics
