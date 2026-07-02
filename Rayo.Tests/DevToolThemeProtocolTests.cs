using Rayo.DevTool.Shared.Protocol;

namespace Rayo.Tests;

public sealed class DevToolThemeProtocolTests
{
    [Fact]
    public void Theme_snapshot_messages_round_trip_through_protocol()
    {
        var source = new ThemeSnapshotResponse
        {
            RequestId = "request",
            Name = "custom",
            Brightness = "Dark",
            Density = "Touch",
            TextScale = 1.25f,
            HighContrast = true,
            ReduceMotion = true,
            Colors =
            [
                new ThemeColorDto
                {
                    Name = "Primary",
                    Value = "#112233FF",
                    OnValue = "#FFFFFFFF",
                    Contrast = 8.5f,
                },
            ],
            Tokens =
            [
                new ThemeTokenDto
                {
                    Name = "chart.grid",
                    ValueType = "Color",
                    Value = "#445566FF",
                    Color = "#445566FF",
                },
            ],
        };

        var restored = Assert.IsType<ThemeSnapshotResponse>(
            MessageSerializer.Deserialize(MessageSerializer.Serialize(source)));

        Assert.Equal(source.RequestId, restored.RequestId);
        Assert.Equal(source.Name, restored.Name);
        Assert.Equal(source.Colors[0].Value, restored.Colors[0].Value);
        Assert.Equal(source.Tokens[0].Name, restored.Tokens[0].Name);
    }
}
