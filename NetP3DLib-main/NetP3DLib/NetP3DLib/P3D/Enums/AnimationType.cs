namespace NetP3DLib.P3D.Enums;
public enum AnimationType : uint
{
    Undefined = 0,
    /// <summary>
    /// AOBJ
    /// </summary>
    AnimatedObject = 0x4A424F41,
    /// <summary>
    /// TEX
    /// </summary>
    Texture = 0x584554,
    /// <summary>
    /// CAM
    /// </summary>
    Camera = 0x4D4143,
    /// <summary>
    /// LITE
    /// </summary>
    Light = 0x4554494C,
    /// <summary>
    /// EXP
    /// </summary>
    Expression = 0x505845,
    /// <summary>
    /// PTRN
    /// </summary>
    PoseTransform = 0x4E525450,
    /// <summary>
    /// PVIS
    /// </summary>
    PoseVisibility = 0x53495650,
    /// <summary>
    /// STRN
    /// </summary>
    ScenegraphTransform = 0x4E525453,
    /// <summary>
    /// SVIS
    /// </summary>
    ScenegraphVisibility = 0x53495653,
    /// <summary>
    /// EVT
    /// </summary>
    Event = 0x545645,
    /// <summary>
    /// BQG
    /// </summary>
    BillboardQuadGroup = 0x475142,
    /// <summary>
    /// EFX
    /// </summary>
    Effect = 0x584645,
    /// <summary>
    /// VRTX
    /// </summary>
    Vertex = 0x58545256,
    /// <summary>
    /// SHAD
    /// </summary>
    Shader = 0x44414853,
}

public static class AnimationTypeExtensions
{
    public static string ToFourCC(this AnimationType animationType)
    {
        var value = (uint)animationType;
        return new string([
            (char)(value & 0xFF),
            (char)((value >> 8) & 0xFF),
            (char)((value >> 16) & 0xFF),
            (char)((value >> 24) & 0xFF)
        ]);
    }
}