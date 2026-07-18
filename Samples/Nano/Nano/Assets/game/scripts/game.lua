local config = require("config")
local player = require("player")
local storage = require("storage")
local world = require("world")
local physics = require("physics")
local hud = require("hud")

local game = {}
local score = 0
local best_score = 0
local hit_flash = 0
local initialized = false
local physics_world = nil

local function play_height()
    return math.max(180, nano.height - config.controls_height)
end

local function overlaps(a, b)
    return nano.geom.circles_overlap(a.x, a.y, a.radius, b.x, b.y, b.radius)
end

local function reset_round()
    score = 0
    hit_flash = 0
    player.reset(play_height())
    world.reset(play_height())
end

local function initialize()
    math.randomseed(7319)
    best_score = storage.load_best()
    physics_world = physics.new_arena(nano.width, play_height(), config.top)
    player.initialize(physics_world, play_height())
    world.create_stars(play_height())
    reset_round()
    initialized = true
end

function game.update(dt)
    if not initialized then initialize() end
    if hud.is_paused() then return end
    if nano.input.b_pressed then reset_round() end

    player.update(dt)
    world.update(dt, play_height())
    nano.physics.step(physics_world, dt, 6)
    player.sync()
    hit_flash = math.max(0, hit_flash - dt)

    for _, enemy in ipairs(world.enemies) do
        if overlaps(player, enemy) then
            nano.audio.play("sounds/explode.wav", hud.sound_volume() * 0.85)
            reset_round()
            hit_flash = config.hit_duration
            return
        end
    end

    if overlaps(player, world.target) then
        score = score + 1
        nano.audio.play("sounds/sillypop.wav", hud.sound_volume())
        if score > best_score then
            best_score = score
            storage.save_best(best_score)
        end
        world.place_target(play_height())
        world.speed_up()
    end
end

function game.draw()
    if not initialized then return end
    nano.draw.clear(hit_flash > 0 and 66 or 7, hit_flash > 0 and 18 or 11, hit_flash > 0 and 30 or 24)
    world.draw()
    player.draw()
    local action = hud.draw(score, best_score, player.dash_time / config.dash_duration)
    if action == "restart" then reset_round() end
end

return game
