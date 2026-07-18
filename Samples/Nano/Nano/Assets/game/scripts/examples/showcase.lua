local showcase = {}

local elapsed = 0
local volume = 0.65
local sound = true
local request = nil
local progress = 0

function showcase.start()
    elapsed = 0
    progress = 0
    request = nil
end

function showcase.stop()
    if request then
        if nano.net.status(request) == "pending" then nano.net.cancel(request) end
        nano.net.release(request)
    end
    request = nil
end

function showcase.update(dt)
    elapsed = elapsed + dt
    progress = (progress + dt * 0.22) % 1
end

function showcase.draw()
    local pulse = 0.5 + math.sin(elapsed * 2.4) * 0.5
    nano.draw.clear(8, 12, 25)
    for index = 1, 14 do
        local angle = elapsed * (0.22 + index * 0.01) + index * 0.7
        local radius = 45 + index * 13
        nano.draw.circle(
            nano.width * 0.5 + math.cos(angle) * radius,
            nano.height * 0.42 + math.sin(angle) * radius * 0.45,
            index % 3 + 1,
            65, 180, 255, 90)
    end

    nano.ui.theme("accent", 75, 180 + pulse * 55, 145 + pulse * 60)
    nano.ui.panel(18, 105, nano.width - 36, 330, "ENGINE SHOWCASE")
    nano.ui.label("UI AUDIO NETWORK TIME MATH", 36, 141, 1, 145, 175, 215)
    nano.ui.separator(36, 160, nano.width - 72)

    local layout = nano.ui.vstack(36, 176, nano.width - 72, 12)
    local row = nano.ui.next(layout, 32)
    if nano.ui.button("demo_sound", "PLAY SOUND", row.x, row.y, row.width, row.height) and sound then
        nano.audio.play("sounds/sillypop.wav", volume)
    end

    row = nano.ui.next(layout, 22)
    local checked = nano.ui.checkbox("demo_sound_enabled", "SOUND ENABLED", row.x, row.y, sound)
    sound = checked.value

    row = nano.ui.next(layout, 12)
    nano.ui.label("VOLUME " .. math.floor(volume * 100) .. "%", row.x, row.y, 1)
    row = nano.ui.next(layout, 28)
    volume = nano.ui.slider("demo_volume", row.x, row.y, row.width, row.height, volume, 0, 1)

    row = nano.ui.next(layout, 16)
    nano.ui.progress(row.x, row.y, row.width, row.height, progress)

    row = nano.ui.next(layout, 32)
    if nano.ui.button("demo_http", "HTTP GET", row.x, row.y, row.width, row.height) then
        if request then
            if nano.net.status(request) == "pending" then nano.net.cancel(request) end
            nano.net.release(request)
        end
        request = nano.net.get("https://example.com")
    end

    local status = request and nano.net.status(request) or "READY"
    nano.ui.label("NETWORK " .. string.upper(status), 38, 410, 1, 125, 225, 170)
    nano.ui.label(
        "RES W" .. nano.stats.physics_worlds() ..
        " B" .. nano.stats.physics_bodies() ..
        " A" .. nano.stats.audio_players() ..
        " N" .. nano.stats.network_requests(),
        170, 410, 1, 150, 175, 210)
    nano.ui.label("FRAME " .. math.floor(nano.time.frame_time + 0.5) .. "MS", 18, 18, 1, 125, 225, 170)
end

return showcase
