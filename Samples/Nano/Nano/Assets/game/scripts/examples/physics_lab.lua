local lab = {}

local world = nil
local bodies = {}
local boundaries = {}
local contacts = 0
local play_height = 0
local next_color = 1
local palette = {
    { 75, 200, 140 }, { 65, 180, 255 }, { 255, 195, 70 }, { 235, 82, 105 }
}

local function remember(handle, color)
    table.insert(bodies, { handle = handle, color = color, state = {} })
    if #bodies > 24 then
        nano.physics.destroy_body(bodies[1].handle)
        table.remove(bodies, 1)
    end
end

local function color()
    local result = palette[next_color]
    next_color = next_color % #palette + 1
    return result
end

local function spawn_circle()
    local handle = nano.physics.new_circle(world, nano.width * 0.5, 105, 10 + math.random(0, 7), 1, "dynamic")
    nano.physics.set_restitution(handle, 0.72)
    nano.physics.set_friction(handle, 0.25)
    local direction_x = math.abs(nano.input.x) > 0.1 and nano.input.x or (math.random() * 2 - 1)
    nano.physics.apply_impulse(handle, direction_x * 150, -90 - math.random() * 80)
    remember(handle, color())
end

local function spawn_box()
    local size = 18 + math.random(0, 16)
    local handle = nano.physics.new_box(world, nano.width * 0.5, 105, size, size, 1.4, "dynamic")
    nano.physics.set_restitution(handle, 0.35)
    nano.physics.set_friction(handle, 0.65)
    nano.physics.apply_impulse(handle, nano.input.x * 180, -120)
    remember(handle, color())
end

function lab.start()
    if world then nano.physics.destroy_world(world) end
    math.randomseed(9182)
    play_height = math.max(260, nano.height - 165)
    world = nano.physics.new_world(0, 520)
    bodies = {}
    boundaries = {
        { handle = nano.physics.new_box(world, nano.width * 0.5, play_height - 12, nano.width, 24, 1, "static"), x = nano.width * 0.5, y = play_height - 12, w = nano.width, h = 24 },
        { handle = nano.physics.new_box(world, -8, play_height * 0.5, 16, play_height, 1, "static"), x = -8, y = play_height * 0.5, w = 16, h = play_height },
        { handle = nano.physics.new_box(world, nano.width + 8, play_height * 0.5, 16, play_height, 1, "static"), x = nano.width + 8, y = play_height * 0.5, w = 16, h = play_height },
        { handle = nano.physics.new_box(world, nano.width * 0.32, play_height - 105, 115, 14, 1, "static"), x = nano.width * 0.32, y = play_height - 105, w = 115, h = 14 },
        { handle = nano.physics.new_box(world, nano.width * 0.75, play_height - 185, 95, 14, 1, "static"), x = nano.width * 0.75, y = play_height - 185, w = 95, h = 14 }
    }
    for _ = 1, 5 do spawn_circle() end
    for _ = 1, 3 do spawn_box() end
end

function lab.stop()
    if world then nano.physics.destroy_world(world) end
    world = nil
    bodies = {}
    boundaries = {}
end

function lab.update(dt)
    if not world then return end
    if nano.input.a_pressed then spawn_circle() end
    if nano.input.b_pressed then spawn_box() end
    nano.physics.step(world, dt, 6)
    contacts = nano.physics.contact_count(world)
end

function lab.draw()
    nano.draw.clear(10, 13, 25)
    for _, boundary in ipairs(boundaries) do
        nano.draw.rect(boundary.x - boundary.w * 0.5, boundary.y - boundary.h * 0.5, boundary.w, boundary.h, 55, 75, 110)
        nano.draw.outline_rect(boundary.x - boundary.w * 0.5, boundary.y - boundary.h * 0.5, boundary.w, boundary.h, 1, 115, 145, 185)
    end

    for _, item in ipairs(bodies) do
        local state = nano.physics.body(item.handle, item.state)
        if state.shape == "circle" then
            nano.draw.circle(state.x, state.y, state.radius, item.color[1], item.color[2], item.color[3])
            nano.draw.outline_circle(state.x, state.y, state.radius, 1, 235, 245, 255, 170)
        else
            nano.draw.rect(state.x - state.width * 0.5, state.y - state.height * 0.5, state.width, state.height, item.color[1], item.color[2], item.color[3])
            nano.draw.outline_rect(state.x - state.width * 0.5, state.y - state.height * 0.5, state.width, state.height, 1, 235, 245, 255, 170)
        end
    end

    nano.ui.panel(8, 8, nano.width - 16, 62)
    nano.ui.label("PHYSICS LAB", 18, 17, 2)
    nano.ui.label("BODIES " .. #bodies .. "  CONTACTS " .. contacts, 18, 43, 1, 125, 225, 170)
    nano.ui.label("A CIRCLE  B BOX  JOYSTICK AIM", 18, 56, 1, 155, 180, 215)
end

return lab
