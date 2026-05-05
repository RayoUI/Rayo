namespace Rayo.Rendering.Vulkan;

/// <summary>
/// GLSL source strings for all Vulkan shader stages.
/// Compiled to SPIR-V at runtime via Silk.NET.Shaderc.
/// </summary>
internal static class VulkanShaders
{
    // ── Shape pipeline (filled geometry, no texture) ──────────────────────

    public const string ShapeVertex = @"
#version 450
layout(location = 0) in vec2 pos;
layout(location = 1) in vec4 color;

layout(push_constant) uniform PC {
    mat4 proj;
} pc;

layout(location = 0) out vec4 fragColor;

void main() {
    gl_Position = pc.proj * vec4(pos, 0.0, 1.0);
    fragColor = color;
}
";

    public const string ShapeFragment = @"
#version 450
layout(location = 0) in vec4 fragColor;
layout(location = 0) out vec4 outColor;

void main() {
    outColor = fragColor;
}
";

    // ── Text pipeline (R8 font atlas, alpha-multiply) ─────────────────────

    public const string TextVertex = @"
#version 450
layout(location = 0) in vec2 pos;
layout(location = 1) in vec2 uv;
layout(location = 2) in vec4 color;

layout(push_constant) uniform PC {
    mat4 proj;
} pc;

layout(location = 0) out vec2 fragUV;
layout(location = 1) out vec4 fragColor;

void main() {
    gl_Position = pc.proj * vec4(pos, 0.0, 1.0);
    fragUV    = uv;
    fragColor = color;
}
";

    public const string TextFragment = @"
#version 450
layout(location = 0) in vec2 fragUV;
layout(location = 1) in vec4 fragColor;
layout(location = 0) out vec4 outColor;

layout(binding = 0) uniform sampler2D atlas;

void main() {
    float alpha = texture(atlas, fragUV).r;
    outColor = vec4(fragColor.rgb, fragColor.a * alpha);
}
";

    // ── Image pipeline (RGBA texture) ─────────────────────────────────────

    public const string ImageVertex = TextVertex; // identical vertex layout

    public const string ImageFragment = @"
#version 450
layout(location = 0) in vec2 fragUV;
layout(location = 1) in vec4 fragColor;
layout(location = 0) out vec4 outColor;

layout(binding = 0) uniform sampler2D tex;

void main() {
    outColor = texture(tex, fragUV) * fragColor;
}
";
}
