namespace Rayo;

/// <summary>
/// Representa los radios de las esquinas de un rect�ngulo redondeado
/// </summary>
public readonly struct CornerRadius
{
    public float TopLeft { get; }
    public float TopRight { get; }
    public float BottomRight { get; }
    public float BottomLeft { get; }

    public CornerRadius(float uniformRadius)
    {
        TopLeft = TopRight = BottomRight = BottomLeft = uniformRadius;
    }

    public CornerRadius(float topLeft = 0, float topRight = 0, float bottomRight = 0, float bottomLeft = 0)
    {
        TopLeft = topLeft;
        TopRight = topRight;
        BottomRight = bottomRight;
        BottomLeft = bottomLeft;
    }

    public static CornerRadius None => new CornerRadius(0);
    
    public static implicit operator CornerRadius(float uniformRadius) => new (uniformRadius);
}
