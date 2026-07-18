local hud = {
    paused = false,
    sound = true,
    volume = 0.65
}

function hud.sound_volume()
    return hud.sound and hud.volume or 0
end

function hud.is_paused()
    return hud.paused
end

function hud.reset()
    hud.paused = false
end

function hud.draw(score, best_score, dash_ratio)
    local width = nano.width - 16
    nano.ui.panel(8, 8, width, 64)
    nano.ui.label("SCORE " .. score .. "  BEST " .. best_score, 18, 17, 2)

    nano.ui.progress(18, 54, math.min(150, width - 100), 7, dash_ratio)

    if nano.ui.button("pause", hud.paused and "PLAY" or "PAUSE", nano.width - 90, 42, 70, 24) then
        hud.paused = not hud.paused
    end

    if not hud.paused then return nil end

    local menu_width = math.min(300, nano.width - 32)
    local menu_x = (nano.width - menu_width) * 0.5
    local menu_y = math.max(72, (nano.height - 270) * 0.35)
    nano.ui.panel(menu_x, menu_y, menu_width, 250, "PAUSED")
    local layout = nano.ui.vstack(menu_x + 20, menu_y + 42, menu_width - 40, 12)

    local row = nano.ui.next(layout, 38)
    if nano.ui.button("resume", "RESUME", row.x, row.y, row.width, row.height) then
        hud.paused = false
    end

    row = nano.ui.next(layout, 38)
    if nano.ui.button("restart", "RESTART", row.x, row.y, row.width, row.height) then
        hud.paused = false
        return "restart"
    end

    row = nano.ui.next(layout, 22)
    local sound = nano.ui.checkbox("sound", "SOUND", row.x, row.y, hud.sound)
    hud.sound = sound.value

    row = nano.ui.next(layout, 18)
    nano.ui.label("VOLUME " .. math.floor(hud.volume * 100) .. "%", row.x, row.y, 1)
    row = nano.ui.next(layout, 28)
    hud.volume = nano.ui.slider("volume", row.x, row.y, row.width, row.height, hud.volume, 0, 1)
    return nil
end

return hud
