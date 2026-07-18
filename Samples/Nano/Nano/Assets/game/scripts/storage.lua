local storage = {}

function storage.load_best()
    if not nano.file.exists("save/best.txt") then return 0 end
    return tonumber(nano.file.read("save/best.txt")) or 0
end

function storage.save_best(value)
    if nano.file.exists("save") then
        nano.file.write("save/best.txt", tostring(value))
    end
end

return storage
