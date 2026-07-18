# Nano 2D Lua API

`main.lua` is the entry point. Modules are loaded from `scripts/` with
`require("module")`; dotted names map to subdirectories and modules are cached.

## Frame and input

- `nano.width`, `nano.height`, `nano.delta_time`
- `nano.time.elapsed`, smoothed `nano.time.fps`, and instantaneous `nano.time.frame_time` in milliseconds
- `nano.input.x/y`, directions, `a/b`, and `a_pressed/b_pressed`

## Drawing

- `nano.draw.clear(r, g, b, a?)`
- `rect(x, y, width, height, ...)`, `circle(x, y, radius, ...)`
- `line(x1, y1, x2, y2, ...)`
- `outline_rect(x, y, width, height, thickness, ...)`
- `outline_circle(x, y, radius, thickness, ...)`

Colors use byte channels from 0 to 255. Alpha is optional and defaults to 255.

Draw commands are submitted directly to the host GPU renderer. The runtime does
not create a software SDL framebuffer, read pixels back to the CPU, or upload a
full-screen RGBA texture each frame. SDL remains the compatibility backend for
standalone/off-screen engine rendering.

When Play is selected, the game page is pushed immediately and keeps rendering a
GPU loading screen while the archive, Lua modules, and audio device are prepared
on background threads. Input controls and the game loop start only after every
preload stage has completed.

## Audio and HTTP

- `nano.audio.play(path, volume?, loop?)` returns a handle (`0` if unavailable)
- `preload(path)` caches the asset and initializes the audio device asynchronously
- `stop(handle)`, `stop_all()`, `is_playing(handle)`, `set_volume(handle, volume)`
- `nano.net.get(url)` returns a non-blocking request handle
- Poll with `status(handle)`: `pending`, `done`, `error`, or `cancelled`
- Read `body(handle)`, `code(handle)`, `error(handle)`, or call `cancel(handle)`
- Call `release(handle)` when a response is no longer needed; completed requests are also reaped automatically

Only HTTP(S) is accepted. Responses are limited to 2 MB and time out after 15 seconds.

## Math, geometry, and files

- `nano.math`: `clamp`, `lerp`, `distance`, `angle`, `move_towards`, `normalize`,
  `deg_to_rad`, and `rad_to_deg`
- `nano.geom`: point/rectangle, rectangle/rectangle, circle/circle,
  point/circle, circle/rectangle, and line/line intersection tests
- `nano.file`: `read`, `write`, `exists`, and `list`; paths are confined to `game.nn`

## 2D physics

- `nano.physics.new_world(gravity_x?, gravity_y?)`
- `new_circle(world, x, y, radius, mass?, type?)`
- `new_box(world, center_x, center_y, width, height, mass?, type?)`
- Body types are `dynamic`, `static`, and `kinematic`
- `step(world, dt, iterations?)`, `body(handle)`, `contacts(world)`, `is_touching(a, b)`
- `body(handle, result_table)` reuses a Lua table; `contact_count(world)` and
  `is_grounded(body, minimum_normal_y?)` avoid allocating contact tables
- `set_position`, `set_velocity`, `apply_force`, `apply_impulse`
- `set_gravity_scale`, `set_restitution`, `set_friction`, and destruction methods

The solver supports gravity, accumulated forces, impulses, restitution, friction,
circle/circle, box/box, and circle/box collision resolution. Boxes are axis-aligned.

## Immediate-mode UI

`nano.ui` is implemented entirely by the game engine. Widgets emit Nano draw
commands, use the engine's compact 5x7 layout metrics, and receive mouse/touch
input without depending on the host UI toolkit. Text is batched through the GPU
font atlas used by the renderer.

- `panel(x, y, width, height, title?)`
- `label(text, x, y, scale?, r?, g?, b?, a?)`
- `button(id, text, x, y, width, height)` returns `true` when clicked
- `progress(x, y, width, height, value)` where value is from 0 to 1
- `slider(id, x, y, width, height, value, minimum?, maximum?)` returns the value
- `checkbox(id, text, x, y, value)` returns `{ value, changed }`
- `separator(x, y, width)`
- `vstack(x, y, width, gap?)` and `next(layout, height, result_table?)` provide vertical layout
- `measure(text, scale?, result_table?)` returns `{ width, height }`
- `theme(name, r, g, b, a?)` and `reset_theme()` customize the engine theme

Theme keys are `panel`, `shadow`, `border`, `text`, `button`, `button_hover`,
`button_active`, `button_text`, `track`, and `accent`. Widget IDs must be stable
and unique within a frame.

## Included examples

The root `main.lua` delegates to `scripts/launcher.lua`, which provides a touch
and mouse-friendly example selector. Every example follows the optional
`start()`, `update(dt)`, `draw()`, and `stop()` lifecycle.

- **Neon Collector** combines joystick input, physics boundaries, audio, UI,
  collision helpers, and persistence in `save/best.txt`.
- **Platformer** demonstrates gravity, dynamic/kinematic/static bodies, contact
  normals, jumping, moving platforms, collectibles, and audio.
- **Physics Lab** spawns circles and boxes with impulses, friction, restitution,
  contact reporting, and safe body/world destruction.
- **Engine Showcase** demonstrates layouts, buttons, checkbox, slider, progress,
  theme colors, bitmap text, sound, asynchronous HTTP, FPS, and frame time.

The common `MENU` button calls `stop()` before returning to the launcher so that
physics worlds, requests, and audio do not leak between examples.

## Runtime statistics

`nano.stats.physics_worlds()`, `physics_bodies()`, `audio_players()`,
`network_requests()`, and `draw_commands()` expose lightweight counters useful
for checking that examples release their resources.
