namespace NetP3DLib.Primitives;
public static class FourCC
{
    public static uint Make(char a, char b, char c, char d) => ((uint)(byte)a) | ((uint)(byte)b << 8) | ((uint)(byte)c << 16) | ((uint)(byte)d << 24);

    public static string ToString(uint code)
    {
        char a = (char)(code & 0xFF);
        char b = (char)((code >> 8) & 0xFF);
        char c = (char)((code >> 16) & 0xFF);
        char d = (char)((code >> 24) & 0xFF);

        return new string([a, b, c, d]);
    }
}
