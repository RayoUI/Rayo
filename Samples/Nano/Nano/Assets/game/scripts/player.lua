local config = require("config")
local physics = require("physics")
local player = { x = 0, y = 0, radius = config.player_radius, dash_time = 0 }
local body = nil

function player.initialize(physics_world, play_height)
    body = physics.new_player(
        physics_world, nano.width * 0.5, play_height * 0.55, player.radius)
    player.reset(play_height)
end

function player.reset(play_height)
    player.x = nano.width * 0.5
    player.y = play_height * 0.55
    player.dash_time = 0
    nano.physics.set_position(body, player.x, player.y)
    nano.physics.set_velocity(body, 0, 0)
end

function player.update(dt)
    if nano.input.a_pressed then player.dash_time = config.dash_duration end
    player.dash_time = math.max(0, player.dash_time - dt)
    local speed = player.dash_time > 0 and config.dash_speed or config.walk_speed
    nano.physics.set_velocity(body, nano.input.x * speed, nano.input.y * speed)
end

function player.sync()
    local state = nano.physics.body(body)
    player.x = state.x
    player.y = state.y
end

function player.draw()
    local red = player.dash_time > 0 and 245 or 65
    local green = player.dash_time > 0 and 245 or 180
    nano.draw.circle(player.x, player.y, player.radius + 5, 65, 180, 255, 55)
    nano.draw.circle(player.x, player.y, player.radius, red, green, 255)
    nano.draw.circle(player.x - 6, player.y - 5, 4, 235, 248, 255)
end

return player
