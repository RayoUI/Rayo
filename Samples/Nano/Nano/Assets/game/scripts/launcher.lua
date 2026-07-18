local launcher = {}

local examples = {
    {
        id = "neon",
        button_id = "example_neon",
        title = "NEON COLLECTOR",
        description = "JOYSTICK AUDIO SAVE PHYSICS",
        module = require("game")
    },
    {
        id = "platformer",
        button_id = "example_platformer",
        title = "PLATFORMER",
        description = "GRAVITY JUMP CONTACTS COINS",
        module = require("examples.platformer")
    },
    {
        id = "physics",
        button_id = "example_physics",
        title = "PHYSICS LAB",
        description = "BODIES IMPULSES COLLISIONS",
        module = require("examples.physics_lab")
    },
    {
        id = "showcase",
        button_id = "example_showcase",
        title = "ENGINE SHOWCASE",
        description = "UI SOUND HTTP TIME",
        module = require("examples.showcase")
    }
}

local current = nil
local fps_size = {}
local button_bounds = { {}, {}, {}, {} }
local description_bounds = { {}, {}, {}, {} }

local function open(entry)
    current = entry
    if entry.module.start then entry.module.start() end
end

local function close()
    if current and current.module.stop then current.module.stop() end
    nano.audio.stop_all()
    current = nil
    collectgarbage("collect")
end

function launcher.update(dt)
    if current and current.module.update then current.module.update(dt) end
end

function launcher.stop()
    close()
end

local function draw_background()
    nano.draw.clear(6, 10, 22)
    for index = 1, 24 do
        local x = (index * 47) % math.max(1, nano.width)
        local y = 34 + (index * 83) % math.max(1, nano.height - 80)
        nano.draw.circle(x, y, index % 4 == 0 and 2 or 1, 90, 130, 190, 150)
    end
end

local function draw_fps()
    local text = "FPS " .. math.floor(nano.time.fps + 0.5)
    local size = nano.ui.measure(text, 1, fps_size)
    local x = nano.width - size.width - 14
    nano.draw.rect(x - 5, 7, size.width + 10, size.height + 8, 5, 10, 18, 220)
    nano.draw.outline_rect(x - 5, 7, size.width + 10, size.height + 8, 1, 75, 200, 140, 180)
    nano.ui.label(text, x, 11, 1, 125, 225, 170)
end

local function draw_menu()
    draw_background()
    local panel_width = math.min(350, nano.width - 28)
    local panel_x = (nano.width - panel_width) * 0.5
    local panel_y = 76
    nano.ui.panel(panel_x, panel_y, panel_width, 390, "NANO ENGINE EXAMPLES")
    nano.ui.label("SELECT AN EXAMPLE", panel_x + 20, panel_y + 34, 1, 125, 225, 170)

    local layout = nano.ui.vstack(panel_x + 18, panel_y + 60, panel_width - 36, 12)
    for index, entry in ipairs(examples) do
        local button = nano.ui.next(layout, 48, button_bounds[index])
        if nano.ui.button(entry.button_id, entry.title, button.x, button.y, button.width, button.height) then
            open(entry)
            return
        end
        local description = nano.ui.next(layout, 12, description_bounds[index])
        nano.ui.label(entry.description, description.x + 2, description.y, 1, 150, 170, 205)
    end

end

function launcher.draw()
    if not current then
        draw_menu()
        draw_fps()
        return
    end

    current.module.draw()
    draw_fps()
    if nano.ui.button("launcher_menu", "MENU", nano.width - 76, 80, 66, 24) then
        close()
    end
end

return launcher
