-- Nano Debug game
-- Move with the virtual joystick. A dashes; B restarts the round.

local player = { x = 0, y = 0, radius = 18 }
local target = { x = 0, y = 0, radius = 12 }
local enemies = {}
local stars = {}
local score = 0
local best_score = 0
local initialized = false
local dash_time = 0
local hit_flash = 0

local function clamp(value, minimum, maximum)
    return math.max(minimum, math.min(maximum, value))
end

local function distance_squared(a, b)
    local dx = a.x - b.x
    local dy = a.y - b.y
    return dx * dx + dy * dy
end

local function overlaps(a, b)
    local radius = a.radius + b.radius
    return distance_squared(a, b) <= radius * radius
end

local function play_height()
    -- Leave space for the virtual controls at the bottom.
    return math.max(180, nano.height - 165)
end

local function random_position(radius)
    return
        math.random(radius + 12, math.max(radius + 12, nano.width - radius - 12)),
        math.random(radius + 44, math.max(radius + 44, play_height() - radius - 12))
end

local function place_target()
    target.x, target.y = random_position(target.radius)
end

local function load_best_score()
    if not nano.file.exists("save/best.txt") then
        return 0
    end

    local value = tonumber(nano.file.read("save/best.txt"))
    return value or 0
end

local function save_best_score()
    if not nano.file.exists("save") then
        -- The archive starts with this directory, but keep the game tolerant
        -- of custom project packages that only contain main.lua.
        return
    end
    nano.file.write("save/best.txt", tostring(best_score))
end

local function reset_round()
    player.x = nano.width * 0.5
    player.y = play_height() * 0.55
    score = 0
    dash_time = 0
    hit_flash = 0

    enemies = {
        { x = 52, y = 86, radius = 14, vx = 72, vy = 56 },
        { x = nano.width - 58, y = 150, radius = 16, vx = -62, vy = 76 },
        { x = nano.width * 0.5, y = play_height() - 48, radius = 12, vx = 86, vy = -54 }
    }

    place_target()
end

local function initialize()
    math.randomseed(7319)
    best_score = load_best_score()

    for index = 1, 28 do
        stars[index] = {
            x = math.random(8, math.max(8, nano.width - 8)),
            y = math.random(38, math.max(38, play_height() - 8)),
            radius = index % 3 == 0 and 2 or 1
        }
    end

    reset_round()
    initialized = true
end

local function update_enemy(enemy, dt)
    enemy.x = enemy.x + enemy.vx * dt
    enemy.y = enemy.y + enemy.vy * dt

    if enemy.x < enemy.radius or enemy.x > nano.width - enemy.radius then
        enemy.x = clamp(enemy.x, enemy.radius, nano.width - enemy.radius)
        enemy.vx = -enemy.vx
    end

    if enemy.y < 38 + enemy.radius or enemy.y > play_height() - enemy.radius then
        enemy.y = clamp(enemy.y, 38 + enemy.radius, play_height() - enemy.radius)
        enemy.vy = -enemy.vy
    end
end

function update(dt)
    if not initialized then
        initialize()
    end

    if nano.input.b_pressed then
        reset_round()
    end

    if nano.input.a_pressed then
        dash_time = 0.16
    end

    dash_time = math.max(0, dash_time - dt)
    hit_flash = math.max(0, hit_flash - dt)

    local speed = dash_time > 0 and 330 or 155
    player.x = clamp(
        player.x + nano.input.x * speed * dt,
        player.radius,
        nano.width - player.radius)
    player.y = clamp(
        player.y + nano.input.y * speed * dt,
        38 + player.radius,
        play_height() - player.radius)

    for _, enemy in ipairs(enemies) do
        update_enemy(enemy, dt)
        if overlaps(player, enemy) then
            reset_round()
            hit_flash = 0.28
            break
        end
    end

    if overlaps(player, target) then
        score = score + 1
        if score > best_score then
            best_score = score
            save_best_score()
        end
        place_target()

        for _, enemy in ipairs(enemies) do
            enemy.vx = enemy.vx * 1.04
            enemy.vy = enemy.vy * 1.04
        end
    end
end

local function draw_score(value, y, red, green, blue)
    local count = math.min(value, 16)
    for index = 1, count do
        nano.draw.rect(12 + (index - 1) * 11, y, 7, 7, red, green, blue)
    end
end

function draw()
    if not initialized then
        return
    end

    if hit_flash > 0 then
        nano.draw.clear(66, 18, 30)
    else
        nano.draw.clear(7, 11, 24)
    end

    for _, star in ipairs(stars) do
        nano.draw.circle(star.x, star.y, star.radius, 125, 150, 190, 155)
    end

    -- Current score and persisted best score.
    draw_score(score, 12, 75, 225, 145)
    draw_score(best_score, 24, 255, 195, 70)

    nano.draw.circle(target.x, target.y, target.radius + 5, 255, 195, 70, 70)
    nano.draw.circle(target.x, target.y, target.radius, 255, 210, 85)

    for _, enemy in ipairs(enemies) do
        nano.draw.circle(enemy.x, enemy.y, enemy.radius, 235, 82, 105)
        nano.draw.circle(enemy.x, enemy.y, enemy.radius * 0.45, 95, 20, 38)
    end

    local player_red = dash_time > 0 and 245 or 65
    local player_green = dash_time > 0 and 245 or 180
    nano.draw.circle(player.x, player.y, player.radius + 5, 65, 180, 255, 55)
    nano.draw.circle(player.x, player.y, player.radius, player_red, player_green, 255)
    nano.draw.circle(player.x - 6, player.y - 5, 4, 235, 248, 255)
end
