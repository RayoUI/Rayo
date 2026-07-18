local config = require("config")
local world = { enemies = {}, stars = {}, target = { x = 0, y = 0, radius = config.target_radius } }

local function random_position(radius, play_height)
    return
        math.random(radius + 12, math.max(radius + 12, nano.width - radius - 12)),
        math.random(radius + config.top + 6, math.max(radius + config.top + 6, play_height - radius - 12))
end

function world.place_target(play_height)
    world.target.x, world.target.y = random_position(world.target.radius, play_height)
end

function world.reset(play_height)
    world.enemies = {
        { x = 52, y = 86, radius = 14, vx = 72, vy = 56 },
        { x = nano.width - 58, y = 150, radius = 16, vx = -62, vy = 76 },
        { x = nano.width * 0.5, y = play_height - 48, radius = 12, vx = 86, vy = -54 }
    }
    world.place_target(play_height)
end

function world.create_stars(play_height)
    world.stars = {}
    for index = 1, config.star_count do
        world.stars[index] = {
            x = math.random(8, math.max(8, nano.width - 8)),
            y = math.random(config.top, math.max(config.top, play_height - 8)),
            radius = index % 3 == 0 and 2 or 1
        }
    end
end

function world.update(dt, play_height)
    for _, enemy in ipairs(world.enemies) do
        enemy.x = enemy.x + enemy.vx * dt
        enemy.y = enemy.y + enemy.vy * dt
        if enemy.x < enemy.radius or enemy.x > nano.width - enemy.radius then
            enemy.x = nano.math.clamp(enemy.x, enemy.radius, nano.width - enemy.radius)
            enemy.vx = -enemy.vx
        end
        if enemy.y < config.top + enemy.radius or enemy.y > play_height - enemy.radius then
            enemy.y = nano.math.clamp(enemy.y, config.top + enemy.radius, play_height - enemy.radius)
            enemy.vy = -enemy.vy
        end
    end
end

function world.speed_up()
    for _, enemy in ipairs(world.enemies) do
        enemy.vx = enemy.vx * 1.04
        enemy.vy = enemy.vy * 1.04
    end
end

function world.draw()
    for _, star in ipairs(world.stars) do
        nano.draw.circle(star.x, star.y, star.radius, 125, 150, 190, 155)
    end
    nano.draw.outline_circle(world.target.x, world.target.y, world.target.radius + 5, 2, 255, 195, 70, 90)
    nano.draw.circle(world.target.x, world.target.y, world.target.radius, 255, 210, 85)
    for _, enemy in ipairs(world.enemies) do
        nano.draw.circle(enemy.x, enemy.y, enemy.radius, 235, 82, 105)
        nano.draw.circle(enemy.x, enemy.y, enemy.radius * 0.45, 95, 20, 38)
    end
end

return world
