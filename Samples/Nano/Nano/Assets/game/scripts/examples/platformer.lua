local platformer = {}

local world = nil
local player = nil
local platforms = {}
local coins = {}
local grounded = false
local score = 0
local moving_direction = 1
local play_height = 0
local player_state = {}

local function add_platform(x, y, width, height, body_type, moving)
    local handle = nano.physics.new_box(world, x, y, width, height, 1, body_type or "static")
    table.insert(platforms, {
        body = handle,
        x = x, y = y, width = width, height = height,
        moving = moving or false,
        state = {}
    })
    return handle
end

local function reset_player()
    nano.physics.set_position(player, 42, play_height - 72)
    nano.physics.set_velocity(player, 0, 0)
end

function platformer.start()
    if world then nano.physics.destroy_world(world) end
    play_height = math.max(260, nano.height - 165)
    world = nano.physics.new_world(0, 760)
    platforms = {}
    coins = {}
    score = 0
    grounded = false
    moving_direction = 1

    player = nano.physics.new_box(world, 42, play_height - 72, 24, 34, 1, "dynamic")
    nano.physics.set_restitution(player, 0)
    nano.physics.set_friction(player, 0.2)

    add_platform(nano.width * 0.5, play_height - 12, nano.width, 24)
    add_platform(82, play_height - 88, 105, 18)
    add_platform(nano.width - 76, play_height - 154, 112, 18)
    add_platform(nano.width * 0.5, play_height - 225, 96, 18)
    add_platform(nano.width * 0.5, play_height - 128, 82, 16, "kinematic", true)
    add_platform(-10, play_height * 0.5, 20, play_height)
    add_platform(nano.width + 10, play_height * 0.5, 20, play_height)

    coins = {
        { x = 82, y = play_height - 116, radius = 9, taken = false },
        { x = nano.width - 76, y = play_height - 182, radius = 9, taken = false },
        { x = nano.width * 0.5, y = play_height - 253, radius = 9, taken = false }
    }
end

function platformer.stop()
    if world then nano.physics.destroy_world(world) end
    world = nil
    player = nil
    platforms = {}
end

local function update_moving_platform()
    for _, platform in ipairs(platforms) do
        if platform.moving then
            local state = nano.physics.body(platform.body, platform.state)
            if state.x > nano.width - 85 then moving_direction = -1 end
            if state.x < 85 then moving_direction = 1 end
            nano.physics.set_velocity(platform.body, moving_direction * 62, 0)
            platform.x = state.x
            platform.y = state.y
        end
    end
end

local function update_grounded()
    grounded = nano.physics.is_grounded(player, 0.45)
end

function platformer.update(dt)
    if not world then return end
    if nano.input.b_pressed then
        platformer.start()
        return
    end

    local state = nano.physics.body(player, player_state)
    local horizontal = nano.input.x * 145
    if math.abs(nano.input.x) < 0.08 then horizontal = state.vx * 0.78 end
    nano.physics.set_velocity(player, horizontal, state.vy)
    if nano.input.a_pressed and grounded then
        nano.physics.set_velocity(player, horizontal, -330)
        nano.audio.play("sounds/sillypop.wav", 0.45)
        grounded = false
    end

    update_moving_platform()
    nano.physics.step(world, dt, 7)
    update_grounded()

    state = nano.physics.body(player, player_state)
    if state.y > play_height + 80 then reset_player() end
    for _, coin in ipairs(coins) do
        if not coin.taken and nano.geom.circle_rect(
            coin.x, coin.y, coin.radius,
            state.x - state.width * 0.5, state.y - state.height * 0.5,
            state.width, state.height) then
            coin.taken = true
            score = score + 1
            nano.audio.play("sounds/sillypop.wav", 0.65)
        end
    end
end

function platformer.draw()
    nano.draw.clear(9, 18, 34)
    for index = 1, 30 do
        nano.draw.circle((index * 53) % nano.width, 80 + (index * 37) % math.max(1, play_height - 100), 1, 120, 165, 215, 130)
    end

    for _, platform in ipairs(platforms) do
        local state = nano.physics.body(platform.body, platform.state)
        local red = platform.moving and 90 or 70
        local green = platform.moving and 190 or 115
        local blue = platform.moving and 220 or 165
        nano.draw.rect(
            state.x - platform.width * 0.5, state.y - platform.height * 0.5,
            platform.width, platform.height, red, green, blue)
        nano.draw.outline_rect(
            state.x - platform.width * 0.5, state.y - platform.height * 0.5,
            platform.width, platform.height, 1, 155, 205, 235)
    end

    for _, coin in ipairs(coins) do
        if not coin.taken then
            nano.draw.outline_circle(coin.x, coin.y, coin.radius + 3, 2, 255, 195, 70, 100)
            nano.draw.circle(coin.x, coin.y, coin.radius, 255, 210, 75)
        end
    end

    local state = nano.physics.body(player, player_state)
    nano.draw.rect(state.x - state.width * 0.5, state.y - state.height * 0.5, state.width, state.height, 75, 210, 150)
    nano.draw.rect(state.x - 7, state.y - 7, 4, 4, 235, 250, 255)
    nano.draw.rect(state.x + 3, state.y - 7, 4, 4, 235, 250, 255)

    nano.ui.panel(8, 8, nano.width - 16, 56)
    nano.ui.label("PLATFORMER  COINS " .. score .. "/" .. #coins, 18, 18, 2)
    nano.ui.label("JOYSTICK MOVE  A JUMP  B RESET", 18, 45, 1, 150, 180, 215)
end

return platformer
