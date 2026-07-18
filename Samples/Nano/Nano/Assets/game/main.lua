-- Nano game entry point. The launcher loads examples from scripts/examples/.
local launcher = require("launcher")

function update(dt)
    launcher.update(dt)
end

function draw()
    launcher.draw()
end

function shutdown()
    launcher.stop()
end
