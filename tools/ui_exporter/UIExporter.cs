using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using NetP3DLib.P3D;
using NetP3DLib.P3D.Chunks;
using NetP3DLib.P3D.Extensions;
using NetP3DLib.IO;

namespace CTTR_Online.Tools
{
    // ---------------------------------------------------------------------------
    //  JSON data-transfer objects
    // ---------------------------------------------------------------------------

    public record SpriteEntry(
        [property: JsonPropertyName("name")]    string Name,
        [property: JsonPropertyName("texture")] string Texture,
        [property: JsonPropertyName("u0")]      float  U0,
        [property: JsonPropertyName("v0")]      float  V0,
        [property: JsonPropertyName("u1")]      float  U1,
        [property: JsonPropertyName("v1")]      float  V1,
        [property: JsonPropertyName("x")]       float  X,
        [property: JsonPropertyName("y")]       float  Y,
        [property: JsonPropertyName("w")]       float  W,
        [property: JsonPropertyName("h")]       float  H,
        [property: JsonPropertyName("colour")]  uint   Colour
    );

    public record Pure3DViewport(
        [property: JsonPropertyName("name")]    string Name,
        [property: JsonPropertyName("x")]       float  X,
        [property: JsonPropertyName("y")]       float  Y,
        [property: JsonPropertyName("w")]       float  W,
        [property: JsonPropertyName("h")]       float  H
    );

    public record LayoutDocument(
        [property: JsonPropertyName("sprites")]   List<SpriteEntry>    Sprites,
        [property: JsonPropertyName("viewports")] List<Pure3DViewport> Viewports,
        [property: JsonPropertyName("god_objects")] List<GodObjectEntry> GodObjects
    );

    public record GodObjectEntry(
        [property: JsonPropertyName("name")]      string Name,
        [property: JsonPropertyName("kind")]      string Kind,
        [property: JsonPropertyName("path")]      string Path,
        [property: JsonPropertyName("namespace")] string Namespace,
        [property: JsonPropertyName("line")]      int Line
    );

    public record GlyphEntry(
        [property: JsonPropertyName("codepoint")]  int   Codepoint,
        [property: JsonPropertyName("texture")]    string Texture,
        [property: JsonPropertyName("u0")]         float U0,
        [property: JsonPropertyName("v0")]         float V0,
        [property: JsonPropertyName("u1")]         float U1,
        [property: JsonPropertyName("v1")]         float V1,
        [property: JsonPropertyName("advance")]    float Advance,
        [property: JsonPropertyName("w")]          float W,
        [property: JsonPropertyName("h")]          float H
    );

    public record FontDocument(
        [property: JsonPropertyName("name")]       string           Name,
        [property: JsonPropertyName("line_height")] float           LineHeight,
        [property: JsonPropertyName("glyphs")]     List<GlyphEntry> Glyphs
    );

    public record ImageDecodeReport(
        [property: JsonPropertyName("owner_type")] string OwnerType,
        [property: JsonPropertyName("owner_name")] string OwnerName,
        [property: JsonPropertyName("output_texture")] string OutputTexture,
        [property: JsonPropertyName("chunk_width")] uint ChunkWidth,
        [property: JsonPropertyName("chunk_height")] uint ChunkHeight,
        [property: JsonPropertyName("chunk_bpp")] uint ChunkBpp,
        [property: JsonPropertyName("chunk_format")] string ChunkFormat,
        [property: JsonPropertyName("chunk_palettized")] bool ChunkPalettized,
        [property: JsonPropertyName("chunk_has_alpha")] bool ChunkHasAlpha,
        [property: JsonPropertyName("p3di_format")] int P3diFormat,
        [property: JsonPropertyName("p3di_width")] int P3diWidth,
        [property: JsonPropertyName("p3di_height")] int P3diHeight,
        [property: JsonPropertyName("p3di_bpp")] int P3diBpp,
        [property: JsonPropertyName("palette_size")] int PaletteSize,
        [property: JsonPropertyName("pixel_data_size")] int PixelDataSize,
        [property: JsonPropertyName("raw_byte_count")] int RawByteCount,
        [property: JsonPropertyName("decoded_sha256")] string DecodedSha256,
        [property: JsonPropertyName("unique_rgba_colors")] int UniqueRgbaColors,
        [property: JsonPropertyName("alpha_min")] byte AlphaMin,
        [property: JsonPropertyName("alpha_max")] byte AlphaMax,
        [property: JsonPropertyName("decode_notes")] string DecodeNotes
    );

    internal record ImageOwnerInfo(
        string OwnerType,
        string OwnerName,
        uint Width,
        uint Height,
        uint Bpp,
        bool Palettized,
        bool HasAlpha,
        string Format
    );

    internal record DecodedImage(
        int Format,
        int Width,
        int Height,
        int Bpp,
        int PaletteSize,
        int PixelDataSize,
        int RawByteCount,
        byte[] Rgba,
        string Notes
    );

    // ---------------------------------------------------------------------------
    //  TGA writer
    // ---------------------------------------------------------------------------

    internal static class TgaWriter
    {
        public static void Write(string path, int width, int height, byte[] pixels)
        {
            using var fs  = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var bw  = new BinaryWriter(fs);

            bool hasAlpha = HasMeaningfulAlpha(pixels);
            byte bpp      = hasAlpha ? (byte)32 : (byte)24;
            byte imgDesc  = hasAlpha ? (byte)0x28 : (byte)0x20;

            bw.Write((byte)0);
            bw.Write((byte)0);
            bw.Write((byte)2);
            bw.Write((ushort)0);
            bw.Write((ushort)0);
            bw.Write((byte)0);
            bw.Write((ushort)0);
            bw.Write((ushort)0);
            bw.Write((ushort)width);
            bw.Write((ushort)height);
            bw.Write(bpp);
            bw.Write(imgDesc);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                bw.Write(pixels[i + 2]); // B
                bw.Write(pixels[i + 1]); // G
                bw.Write(pixels[i + 0]); // R
                if (hasAlpha) bw.Write(pixels[i + 3]); // A
            }
        }

        private static bool HasMeaningfulAlpha(byte[] pixels)
        {
            for (int i = 3; i < pixels.Length; i += 4)
                if (pixels[i] != 255) return true;
            return false;
        }
    }

    // ---------------------------------------------------------------------------
    //  PS2 decoders
    // ---------------------------------------------------------------------------

    internal static class PixelDecoder
    {
        public enum IndexLayout
        {
            Linear,
            PSMT8,
        }

        public static byte[] UnswizzlePSMT8(byte[] swizzled, int width, int height)
        {
            var linear = new byte[width * height];
            int storageWidth = (width < 128) ? 128 : ((width + 127) & ~127);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int src = GetPSMT8Address(x, y, storageWidth);
                    int dst = y * width + x;
                    if (src >= 0 && src < swizzled.Length)
                        linear[dst] = swizzled[src];
                }
            }
            return linear;
        }

        public static byte[] UnswizzlePSMT4(byte[] swizzled, int width, int height)
        {
            var dst = new byte[width * height];
            int storageWidth = (width < 128) ? 128 : ((width + 127) & ~127);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int byteAddr = GetPSMT4Address(x, y, storageWidth);
                    if (byteAddr >= 0 && byteAddr < swizzled.Length)
                    {
                        byte b = swizzled[byteAddr];
                        byte nibble = (x % 2 == 0) ? (byte)(b & 0x0F) : (byte)(b >> 4);
                        dst[y * width + x] = nibble;
                    }
                }
            }
            return dst;
        }

        private static int GetPSMT4Address(int x, int y, int storageWidth)
        {
            int blockX = x / 32;
            int blockY = y / 16;
            int blocksPerRow = Math.Max(1, storageWidth / 32);
            int blockOffset = (blockY * blocksPerRow + blockX) * 256;

            int lx = x % 32;
            int ly = y % 16;

            int subBlock = (ly / 8) * 2 + (lx / 16);
            int subX = lx % 16;
            int subY = ly % 8;

            int byteNum = subY * 8 + ((subX / 2) ^ ((subY & 1) * 4));
            return blockOffset + subBlock * 64 + byteNum;
        }

        public static byte[] UnswizzlePSMT8WithStorageWidth(byte[] swizzled, int width, int height, int storageWidth, bool inverse)
        {
            var linear = new byte[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int addr = GetPSMT8Address(x, y, storageWidth);
                    int rowMajor = y * width + x;
                    if (addr < 0 || addr >= swizzled.Length || rowMajor >= linear.Length)
                        continue;

                    if (inverse)
                    {
                        if (addr < linear.Length && rowMajor < swizzled.Length)
                            linear[addr] = swizzled[rowMajor];
                    }
                    else
                    {
                        linear[rowMajor] = swizzled[addr];
                    }
                }
            }
            return linear;
        }

        private static int GetPSMT8Address(int x, int y, int width)
        {
            int blockLocation = (y & ~0x0F) * width + (x & ~0x0F) * 2;
            int swapSelector = (((y + 2) >> 2) & 0x01) * 4;
            int posY = (((y & ~0x03) >> 1) + (y & 0x01)) & 0x07;
            int columnLocation = posY * width * 2 + ((x + swapSelector) & 0x07) * 4;
            int byteNum = ((y >> 1) & 0x01) + ((x >> 2) & 0x02);
            return blockLocation + columnLocation + byteNum;
        }

        public static byte[] FromPSMCT32(byte[] raw, int width, int height)
        {
            if (raw.Length < width * height * 4)
                throw new InvalidDataException("PSMCT32 buffer too small.");
            var dst = new byte[width * height * 4];
            Buffer.BlockCopy(raw, 0, dst, 0, dst.Length);
            return dst;
        }

        public static byte[] FromPSMCT16(byte[] raw, int width, int height)
        {
            var dst = new byte[width * height * 4];
            for (int i = 0, o = 0; i < width * height; i++, o += 4)
            {
                ushort px = (ushort)(raw[i * 2] | (raw[i * 2 + 1] << 8));
                dst[o + 0] = (byte)((px & 0x1F) << 3);
                dst[o + 1] = (byte)(((px >> 5)  & 0x1F) << 3);
                dst[o + 2] = (byte)(((px >> 10) & 0x1F) << 3);
                dst[o + 3] = (px & 0x8000) != 0 ? (byte)255 : (byte)0;
            }
            return dst;
        }

        public static byte[] FromPSMT8(byte[] indices, byte[] clut, int width, int height)
        {
            if (clut.Length < 256 * 4)
                throw new InvalidDataException("CLUT buffer too small for PSMT8.");
            var dst = new byte[width * height * 4];
            for (int i = 0, o = 0; i < width * height; i++, o += 4)
            {
                int colorIdx = indices[i];
                // PS2 8-bit CLUT Swizzling remapping
                int swizzledIdx = (colorIdx & 0xE7) | ((colorIdx & 0x08) << 1) | ((colorIdx & 0x10) >> 1);
                int ci = swizzledIdx * 4;
                dst[o + 0] = clut[ci + 0];
                dst[o + 1] = clut[ci + 1];
                dst[o + 2] = clut[ci + 2];
                dst[o + 3] = (byte)Math.Min(clut[ci + 3] * 2, 255);
            }
            return dst;
        }

        public static byte[] FromPSMT8LinearPalette(byte[] indices, byte[] clut, int width, int height)
        {
            if (clut.Length < 256 * 4)
                throw new InvalidDataException("CLUT buffer too small for PSMT8.");
            var dst = new byte[width * height * 4];
            for (int i = 0, o = 0; i < width * height; i++, o += 4)
            {
                int ci = indices[i] * 4;
                dst[o + 0] = clut[ci + 0];
                dst[o + 1] = clut[ci + 1];
                dst[o + 2] = clut[ci + 2];
                dst[o + 3] = (byte)Math.Min(clut[ci + 3] * 2, 255);
            }
            return dst;
        }

        public static byte[] FromPSMT8WithChannelOrder(byte[] indices, byte[] clut, int width, int height, bool remapClut, int rByte, int gByte, int bByte, int aByte)
        {
            if (clut.Length < 256 * 4)
                throw new InvalidDataException("CLUT buffer too small for PSMT8.");

            var dst = new byte[width * height * 4];
            for (int i = 0, o = 0; i < width * height; i++, o += 4)
            {
                int colorIdx = indices[i];
                if (remapClut)
                    colorIdx = (colorIdx & 0xE7) | ((colorIdx & 0x08) << 1) | ((colorIdx & 0x10) >> 1);

                int ci = colorIdx * 4;
                dst[o + 0] = clut[ci + rByte];
                dst[o + 1] = clut[ci + gByte];
                dst[o + 2] = clut[ci + bByte];
                dst[o + 3] = aByte >= 0 ? (byte)Math.Min(clut[ci + aByte] * 2, 255) : (byte)255;
            }
            return dst;
        }

        public static byte[] FromPSMT4(byte[] indices, byte[] clut, int width, int height)
        {
            if (clut.Length < 16 * 4)
                throw new InvalidDataException("CLUT buffer too small for PSMT4.");
            var dst = new byte[width * height * 4];
            int pixelCount = width * height;
            for (int i = 0; i < pixelCount; i++)
            {
                int colorIdx = indices[i];
                int ci = colorIdx * 4, o = i * 4;
                dst[o + 0] = clut[ci + 0];
                dst[o + 1] = clut[ci + 1];
                dst[o + 2] = clut[ci + 2];
                dst[o + 3] = (byte)Math.Min(clut[ci + 3] * 2, 255);
            }
            return dst;
        }
    }

    // ---------------------------------------------------------------------------
    //  Coordinate normaliser
    // ---------------------------------------------------------------------------

    internal static class Coords
    {
        private const float VW = 640f;
        private const float VH = 480f;

        public static float NX(float x) => x / VW;
        public static float NY(float y) => y / VH;
        public static float NW(float w) => w / VW;
        public static float NH(float h) => h / VH;
    }

    // ---------------------------------------------------------------------------
    //  Main Exporter Program
    // ---------------------------------------------------------------------------

    class UIExporter
    {
        static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Usage: UIExporter <frontend_pal.p3d> <output_dir> [frontend.god]");
                return 1;
            }

            string p3dPath   = args[0];
            string outputDir = args[1];
            string? godPath  = args.Length >= 3 ? args[2] : null;

            if (!File.Exists(p3dPath))
            {
                Console.Error.WriteLine($"[ERROR] p3d not found: {p3dPath}");
                return 1;
            }

            string texDir = Path.Combine(outputDir, "textures");
            Directory.CreateDirectory(texDir);

            Console.WriteLine($"[UIExporter] Loading {p3dPath} ...");
            var p3dFile = new P3DFile(p3dPath);
            var allChunks = p3dFile.Chunks.SelectMany(c => c.AllChildren.Prepend(c)).ToList();

            var sprites   = new List<SpriteEntry>();
            var viewports = new List<Pure3DViewport>();
            var godObjects = new List<GodObjectEntry>();
            var fontDocs  = new List<FontDocument>();
            var decodeReports = new List<ImageDecodeReport>();
            var texMap    = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // --- 1. Textures ---
            Console.WriteLine("[UIExporter] Extracting textures...");
            foreach (var chunk in allChunks.OfType<TextureChunk>())
            {
                string cleanName = GetCleanName(chunk.Name);
                string tgaName = cleanName + ".tga";
                string tgaPath = Path.Combine(texDir, tgaName);
                try
                {
                    ExportTexture(chunk, tgaPath, decodeReports);
                    texMap[cleanName] = tgaName;
                    texMap[chunk.Name] = tgaName;
                    Console.WriteLine($"  [TEX] {chunk.Name} -> textures/{tgaName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  [WARN] Failed to export texture {chunk.Name}: {ex.Message}");
                }
            }

            Console.WriteLine("[UIExporter] Extracting frontend images...");
            foreach (var chunk in allChunks.OfType<ImageChunk>())
            {
                string cleanName = GetCleanName(chunk.Name);
                if (string.IsNullOrWhiteSpace(cleanName))
                    continue;

                string tgaName = cleanName + ".tga";
                string tgaPath = Path.Combine(texDir, tgaName);
                try
                {
                    ExportImage(chunk, tgaPath, decodeReports);
                    texMap[cleanName] = tgaName;
                    texMap[chunk.Name] = tgaName;
                    Console.WriteLine($"  [IMG] {chunk.Name} -> textures/{tgaName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  [WARN] Failed to export image {chunk.Name}: {ex.Message}");
                }
            }

            foreach (var res in allChunks.OfType<FrontendImageResourceChunk>())
            {
                string cleanName = GetCleanName(res.Name);
                string cleanFile = GetCleanName(res.Filename);
                if (texMap.TryGetValue(cleanFile, out string? mapped))
                {
                    texMap[cleanName] = mapped;
                    texMap[res.Name] = mapped;
                    texMap[res.Filename] = mapped;
                }
            }

            // --- 2. Sprites (0x18006 / 0x18022) ---
            Console.WriteLine("[UIExporter] Extracting sprites...");
            foreach (var chunk in allChunks)
            {
                if (chunk.ID == 0x18006 || chunk.ID == 0x18022)
                {
                    try
                    {
                        ParseSpriteChunk(chunk, texMap, sprites);
                    }
                    catch (Exception ex)
                    {
                        string cName = chunk is NamedChunk nc ? nc.Name : "<unnamed>";
                        Console.WriteLine($"  [WARN] Failed to parse sprite chunk {cName} (0x{chunk.ID:X4}): {ex.Message}");
                    }
                }
            }

            // --- 3. Viewports (0x18008 / 0x18024) ---
            Console.WriteLine("[UIExporter] Extracting viewports...");
            foreach (var chunk in allChunks)
            {
                if (chunk.ID == 0x18008 || chunk.ID == 0x18024)
                {
                    try
                    {
                        ParseViewportChunk(chunk, viewports);
                    }
                    catch (Exception ex)
                    {
                        string cName = chunk is NamedChunk nc ? nc.Name : "<unnamed>";
                        Console.WriteLine($"  [WARN] Failed to parse viewport chunk {cName} (0x{chunk.ID:X4}): {ex.Message}");
                    }
                }
            }

            // --- 3b. GOD hierarchy references ---
            if (!string.IsNullOrWhiteSpace(godPath))
            {
                if (File.Exists(godPath))
                {
                    Console.WriteLine("[UIExporter] Scanning GOD hierarchy references...");
                    ParseGodObjects(godPath, godObjects);
                    Console.WriteLine($"  [GOD] {godObjects.Count} object/resource reference(s)");
                }
                else
                {
                    Console.WriteLine($"  [WARN] GOD file not found: {godPath}");
                }
            }

            // --- 4. Fonts (0x22000) ---
            Console.WriteLine("[UIExporter] Extracting fonts...");
            foreach (var chunk in allChunks.OfType<TextureFontChunk>())
            {
                try
                {
                    Console.WriteLine($"  [FONT] {chunk.Name}");
                    var fontTextures = chunk.Children.OfType<TextureChunk>().ToList();
                    var fontTexFiles = new List<string>();

                    foreach (var texChunk in fontTextures)
                    {
                        string cleanName = GetCleanName(texChunk.Name);
                        string tgaName = cleanName + ".tga";
                        fontTexFiles.Add(tgaName);
                        if (!texMap.ContainsKey(cleanName))
                        {
                            string tgaPath = Path.Combine(texDir, tgaName);
                            ExportTexture(texChunk, tgaPath, decodeReports);
                            texMap[cleanName] = tgaName;
                            texMap[texChunk.Name] = tgaName;
                        }
                    }

                    var glyphListChunk = chunk.Children.OfType<TextureGlyphListChunk>().FirstOrDefault();
                    if (glyphListChunk == null)
                    {
                        Console.WriteLine($"    [WARN] No glyph list for font {chunk.Name}");
                        continue;
                    }

                    var glyphs = new List<GlyphEntry>();
                    foreach (var g in glyphListChunk.Glyphs)
                    {
                        string texFile = g.TextureNum < fontTexFiles.Count
                            ? fontTexFiles[(int)g.TextureNum]
                            : (fontTexFiles.Count > 0 ? fontTexFiles[0] : "unknown.tga");
                        glyphs.Add(new GlyphEntry(
                            Codepoint: (int)g.Code,
                            Texture:   texFile,
                            U0:        g.BottomLeft.X,
                            V0:        1.0f - g.BottomLeft.Y, // Flip V coord for OpenGL top-left origin convention
                            U1:        g.TopRight.X,
                            V1:        1.0f - g.TopRight.Y,
                            Advance:   Coords.NW(g.Advance),
                            W:         Coords.NW(g.Width),
                            H:         Coords.NH(chunk.FontHeight)
                        ));
                    }

                    fontDocs.Add(new FontDocument(
                        Name:       chunk.Name,
                        LineHeight: Coords.NH(chunk.FontHeight),
                        Glyphs:     glyphs
                    ));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  [WARN] Failed to parse font {chunk.Name}: {ex.Message}");
                }
            }

            // --- 5. Serialization ---
            var jsonOpts = new JsonSerializerOptions { WriteIndented = true };

            string layoutPath = Path.Combine(outputDir, "ui_layout.json");
            File.WriteAllText(layoutPath, JsonSerializer.Serialize(new LayoutDocument(sprites, viewports, godObjects), jsonOpts));
            Console.WriteLine($"[UIExporter] Saved layout to {layoutPath}");

            string fontsPath = Path.Combine(outputDir, "ui_fonts.json");
            File.WriteAllText(fontsPath, JsonSerializer.Serialize(fontDocs, jsonOpts));
            Console.WriteLine($"[UIExporter] Saved fonts to {fontsPath}");

            string reportPath = Path.Combine(outputDir, "texture_decode_report.json");
            File.WriteAllText(reportPath, JsonSerializer.Serialize(decodeReports, jsonOpts));
            Console.WriteLine($"[UIExporter] Saved texture decode report to {reportPath}");

            Console.WriteLine("[UIExporter] Done successfully!");
            return 0;
        }

        private static void ParseGodObjects(string godPath, List<GodObjectEntry> godObjects)
        {
            var loadObjects = new Regex(
                @"LoadObjects\(""(?<path>[^""]+)""\s*,\s*""(?<name>[^""]+)""\s*,\s*""(?<namespace>[^""]+)""\)",
                RegexOptions.Compiled);
            var backgroundObject = new Regex(
                @"SetBackgroundObject\(""(?<slot>[^""]+)""\s*,\s*""(?<name>[^""]+)""\)",
                RegexOptions.Compiled);
            var screenResources = new Regex(
                @"SetScreenResources\(""(?<project>[^""]+)""\s*,\s*""(?<name>[^""]+)""\)",
                RegexOptions.Compiled);

            int lineNo = 0;
            foreach (string line in File.ReadLines(godPath))
            {
                lineNo++;

                var load = loadObjects.Match(line);
                if (load.Success)
                {
                    godObjects.Add(new GodObjectEntry(
                        Name: load.Groups["name"].Value,
                        Kind: "LoadObjects",
                        Path: load.Groups["path"].Value,
                        Namespace: load.Groups["namespace"].Value,
                        Line: lineNo));
                    continue;
                }

                var bg = backgroundObject.Match(line);
                if (bg.Success)
                {
                    godObjects.Add(new GodObjectEntry(
                        Name: bg.Groups["name"].Value,
                        Kind: "SetBackgroundObject",
                        Path: bg.Groups["slot"].Value,
                        Namespace: "Frontend",
                        Line: lineNo));
                    continue;
                }

                var resources = screenResources.Match(line);
                if (resources.Success)
                {
                    godObjects.Add(new GodObjectEntry(
                        Name: resources.Groups["name"].Value,
                        Kind: "SetScreenResources",
                        Path: resources.Groups["project"].Value,
                        Namespace: "Frontend",
                        Line: lineNo));
                }
            }
        }

        private static void ParseSpriteChunk(Chunk chunk, Dictionary<string, string> texMap, List<SpriteEntry> sprites)
        {
            string name = "";
            int posX = 0, posY = 0;
            uint dimX = 0, dimY = 0;
            uint colourRaw = 0xFFFFFFFF;
            var imageNames = new List<string>();

            if (chunk.ID == 0x18006) // v1
            {
                var fs = (FrontendMultiSpriteChunk)chunk;
                name = fs.Name;
                posX = fs.PositionX;
                posY = fs.PositionY;
                dimX = fs.DimensionX;
                dimY = fs.DimensionY;
                colourRaw = PackColour(fs.Colour);
                imageNames = fs.ImageNames.ToList();
            }
            else // v2 manual binary parsing with reverse-engineered extra byte shift
            {
                using var ms = new MemoryStream(chunk.DataBytes);
                using var br = new EndianAwareBinaryReader(ms, NetP3DLib.IO.Endianness.Little);

                name = br.ReadP3DString();
                uint version = br.ReadUInt32();
                posX = br.ReadInt32();
                posY = br.ReadInt32();
                dimX = br.ReadUInt32();
                dimY = br.ReadUInt32();
                uint justificationX = br.ReadUInt32();
                
                br.ReadByte(); // The shift byte at Offset 33!
                
                uint justificationY = br.ReadUInt32();
                colourRaw = PackColour(br.ReadColor());
                uint translucency = br.ReadUInt32();
                float rotationValue = br.ReadSingle();
                uint numImageNames = br.ReadUInt32();

                for (int i = 0; i < numImageNames; i++)
                {
                    imageNames.Add(br.ReadP3DString());
                }
            }

            string textureFile = "unknown.tga";
            if (imageNames.Count > 0)
            {
                string spriteTexName = GetCleanName(imageNames[0]);
                if (texMap.TryGetValue(spriteTexName, out string? mapped))
                {
                    textureFile = mapped;
                }
                else if (texMap.TryGetValue(imageNames[0], out mapped))
                {
                    textureFile = mapped;
                }
                else
                {
                    textureFile = spriteTexName + ".tga";
                }
            }

            sprites.Add(new SpriteEntry(
                Name:    name,
                Texture: textureFile,
                U0:      0.0f,
                V0:      0.0f,
                U1:      1.0f,
                V1:      1.0f,
                X:       Coords.NX(posX),
                Y:       Coords.NY(posY),
                W:       Coords.NW(dimX),
                H:       Coords.NH(dimY),
                Colour:  colourRaw
            ));
        }

        private static void ParseViewportChunk(Chunk chunk, List<Pure3DViewport> viewports)
        {
            string name = "";
            int posX = 0, posY = 0;
            uint dimX = 0, dimY = 0;

            if (chunk.ID == 0x18008) // v1
            {
                var fv = (FrontendPure3DObjectChunk)chunk;
                name = fv.Name;
                posX = fv.PositionX;
                posY = fv.PositionY;
                dimX = fv.DimensionX;
                dimY = fv.DimensionY;
            }
            else // v2 manual binary parsing
            {
                using var ms = new MemoryStream(chunk.DataBytes);
                using var br = new EndianAwareBinaryReader(ms, NetP3DLib.IO.Endianness.Little);

                name = br.ReadP3DString();
                uint version = br.ReadUInt32();
                posX = br.ReadInt32();
                posY = br.ReadInt32();
                dimX = br.ReadUInt32();
                dimY = br.ReadUInt32();
                uint justificationX = br.ReadUInt32();
                
                br.ReadByte(); // The shift byte at Offset 33!
                
                uint justificationY = br.ReadUInt32();
                br.ReadColor();
                uint translucency = br.ReadUInt32();
                float rotationValue = br.ReadSingle();
                string pure3DFilename = br.ReadP3DString();
            }

            viewports.Add(new Pure3DViewport(
                Name: name,
                X:    Coords.NX(posX),
                Y:    Coords.NY(posY),
                W:    Coords.NW(dimX),
                H:    Coords.NH(dimY)
            ));
        }

        private static void ExportTexture(TextureChunk chunk, string tgaPath, List<ImageDecodeReport> reports)
        {
            var imageChunk = chunk.Children.SelectMany(c => c.AllChildren.Prepend(c)).OfType<ImageChunk>().FirstOrDefault();
            var imgDataChunk = chunk.Children.SelectMany(c => c.AllChildren.Prepend(c)).OfType<ImageDataChunk>().FirstOrDefault();
            if (imgDataChunk == null)
                throw new Exception("No image data found under texture chunk.");
            var owner = new ImageOwnerInfo(
                "TextureChunk",
                chunk.Name,
                chunk.Width,
                chunk.Height,
                chunk.Bpp,
                chunk.TextureType == TextureChunk.TextureTypes.Palettized || chunk.TextureType == TextureChunk.TextureTypes.PS2_4Bit || chunk.TextureType == TextureChunk.TextureTypes.PS2_8Bit,
                chunk.AlphaDepth > 0,
                $"{chunk.TextureType}; image={imageChunk?.Format.ToString() ?? "missing"}");
            ExportFromImageData(imgDataChunk, tgaPath, owner, reports);
        }

        private static void ExportImage(ImageChunk chunk, string tgaPath, List<ImageDecodeReport> reports)
        {
            var imgDataChunk = chunk.Children.SelectMany(c => c.AllChildren.Prepend(c)).OfType<ImageDataChunk>().FirstOrDefault();
            if (imgDataChunk == null)
                throw new Exception("No image data found under image chunk.");
            var owner = new ImageOwnerInfo(
                "ImageChunk",
                chunk.Name,
                chunk.Width,
                chunk.Height,
                chunk.Bpp,
                chunk.Palettized,
                chunk.HasAlpha,
                chunk.Format.ToString());
            ExportFromImageData(imgDataChunk, tgaPath, owner, reports);
        }

        private static void ExportFromImageData(ImageDataChunk dataChunk, string tgaPath, ImageOwnerInfo owner, List<ImageDecodeReport> reports)
        {
            var decoded = DecodeImageData(dataChunk);
            TgaWriter.Write(tgaPath, decoded.Width, decoded.Height, decoded.Rgba);
            reports.Add(BuildDecodeReport(owner, tgaPath, decoded));

            string cleanName = Path.GetFileNameWithoutExtension(tgaPath);
            if (IsProbeTexture(cleanName))
                WriteProbeVariants(dataChunk, tgaPath);
        }

        private static DecodedImage DecodeImageData(ImageDataChunk dataChunk)
        {
            byte[] bytes = dataChunk.ImageData.RawArray;
            if (bytes.Length < 28)
                throw new Exception("Image data too small (no header).");

            string magic = System.Text.Encoding.ASCII.GetString(bytes, 0, 4);
            if (magic != "P3DI")
                throw new Exception($"Invalid image magic: {magic}");

            int format = BitConverter.ToInt32(bytes, 4);
            int width = BitConverter.ToInt32(bytes, 8);
            int height = BitConverter.ToInt32(bytes, 12);
            int bpp = BitConverter.ToInt32(bytes, 16);
            int paletteSize = BitConverter.ToInt32(bytes, 20);
            int pixelDataSize = BitConverter.ToInt32(bytes, 24);

            byte[] rgba;
            string notes;

            if (bpp == 8)
            {
                byte[] indices = new byte[pixelDataSize];
                Buffer.BlockCopy(bytes, 28, indices, 0, pixelDataSize);
                var layout = ShouldUnswizzleIndices(format, bpp) ? PixelDecoder.IndexLayout.PSMT8 : PixelDecoder.IndexLayout.Linear;
                if (layout == PixelDecoder.IndexLayout.PSMT8)
                    indices = PixelDecoder.UnswizzlePSMT8(indices, width, height);
                
                byte[] clut = new byte[paletteSize];
                Buffer.BlockCopy(bytes, 28 + pixelDataSize, clut, 0, paletteSize);
                
                rgba = PixelDecoder.FromPSMT8(indices, clut, width, height);
                notes = $"PSMT8 indices={layout}, clut=PS2-8bit-remap, rgba=raw-clut-order, alpha=ps2x2";
            }
            else if (bpp == 4)
            {
                byte[] rawIndices = new byte[pixelDataSize];
                Buffer.BlockCopy(bytes, 28, rawIndices, 0, pixelDataSize);
                byte[] indices = PixelDecoder.UnswizzlePSMT4(rawIndices, width, height);
                
                byte[] clut = new byte[paletteSize];
                Buffer.BlockCopy(bytes, 28 + pixelDataSize, clut, 0, paletteSize);
                
                rgba = PixelDecoder.FromPSMT4(indices, clut, width, height);
                notes = "PSMT4 indices=unswizzled, clut=linear, rgba=raw-clut-order, alpha=ps2x2";
            }
            else if (bpp == 16)
            {
                byte[] raw = new byte[pixelDataSize];
                Buffer.BlockCopy(bytes, 28, raw, 0, pixelDataSize);
                rgba = PixelDecoder.FromPSMCT16(raw, width, height);
                notes = "PSMCT16 5551";
            }
            else
            {
                byte[] raw = new byte[pixelDataSize];
                Buffer.BlockCopy(bytes, 28, raw, 0, pixelDataSize);
                rgba = PixelDecoder.FromPSMCT32(raw, width, height);
                notes = "PSMCT32 raw";
            }

            return new DecodedImage(format, width, height, bpp, paletteSize, pixelDataSize, bytes.Length, rgba, notes);
        }

        private static bool ShouldUnswizzleIndices(int format, int bpp)
        {
            return bpp == 8 || bpp == 4;
        }

        private static ImageDecodeReport BuildDecodeReport(ImageOwnerInfo owner, string tgaPath, DecodedImage decoded)
        {
            (byte minA, byte maxA) = AlphaRange(decoded.Rgba);
            return new ImageDecodeReport(
                owner.OwnerType,
                owner.OwnerName,
                Path.GetFileName(tgaPath),
                owner.Width,
                owner.Height,
                owner.Bpp,
                owner.Format,
                owner.Palettized,
                owner.HasAlpha,
                decoded.Format,
                decoded.Width,
                decoded.Height,
                decoded.Bpp,
                decoded.PaletteSize,
                decoded.PixelDataSize,
                decoded.RawByteCount,
                Convert.ToHexString(SHA256.HashData(decoded.Rgba)).ToLowerInvariant(),
                CountUniqueRgba(decoded.Rgba),
                minA,
                maxA,
                decoded.Notes);
        }

        private static (byte min, byte max) AlphaRange(byte[] rgba)
        {
            byte min = 255, max = 0;
            for (int i = 3; i < rgba.Length; i += 4)
            {
                min = Math.Min(min, rgba[i]);
                max = Math.Max(max, rgba[i]);
            }
            return (min, max);
        }

        private static int CountUniqueRgba(byte[] rgba)
        {
            var colors = new HashSet<uint>();
            for (int i = 0; i + 3 < rgba.Length; i += 4)
            {
                uint packed = (uint)(rgba[i + 0] | (rgba[i + 1] << 8) | (rgba[i + 2] << 16) | (rgba[i + 3] << 24));
                colors.Add(packed);
            }
            return colors.Count;
        }

        private static bool IsProbeTexture(string cleanName)
        {
            return cleanName.Equals("frontend_background", StringComparison.OrdinalIgnoreCase)
                || cleanName.Equals("menu_character_crash", StringComparison.OrdinalIgnoreCase)
                || cleanName.Equals("LT_531441_24_texture_0", StringComparison.OrdinalIgnoreCase);
        }

        private static void WriteProbeVariants(ImageDataChunk dataChunk, string tgaPath)
        {
            byte[] bytes = dataChunk.ImageData.RawArray;
            int format = BitConverter.ToInt32(bytes, 4);
            int width = BitConverter.ToInt32(bytes, 8);
            int height = BitConverter.ToInt32(bytes, 12);
            int bpp = BitConverter.ToInt32(bytes, 16);
            int paletteSize = BitConverter.ToInt32(bytes, 20);
            int pixelDataSize = BitConverter.ToInt32(bytes, 24);
            if (bpp != 8 || paletteSize < 1024)
                return;

            byte[] swizzled = new byte[pixelDataSize];
            Buffer.BlockCopy(bytes, 28, swizzled, 0, pixelDataSize);
            byte[] linear = new byte[pixelDataSize];
            Buffer.BlockCopy(swizzled, 0, linear, 0, pixelDataSize);
            byte[] unswizzled = PixelDecoder.UnswizzlePSMT8(swizzled, width, height);
            byte[] clut = new byte[paletteSize];
            Buffer.BlockCopy(bytes, 28 + pixelDataSize, clut, 0, paletteSize);

            string dir = Path.Combine(Path.GetDirectoryName(tgaPath) ?? ".", "_decode_probes");
            Directory.CreateDirectory(dir);
            string name = Path.GetFileNameWithoutExtension(tgaPath);

            TgaWriter.Write(Path.Combine(dir, $"{name}.linear_ps2clut.tga"), width, height, PixelDecoder.FromPSMT8(linear, clut, width, height));
            TgaWriter.Write(Path.Combine(dir, $"{name}.linear_linearclut.tga"), width, height, PixelDecoder.FromPSMT8LinearPalette(linear, clut, width, height));
            TgaWriter.Write(Path.Combine(dir, $"{name}.unswizzled_ps2clut.tga"), width, height, PixelDecoder.FromPSMT8(unswizzled, clut, width, height));
            TgaWriter.Write(Path.Combine(dir, $"{name}.unswizzled_linearclut.tga"), width, height, PixelDecoder.FromPSMT8LinearPalette(unswizzled, clut, width, height));
            if (name.Equals("menu_character_crash", StringComparison.OrdinalIgnoreCase))
            {
                WriteCrashChannelProbes(dir, name, unswizzled, clut, width, height);
                WriteCrashAddressProbes(dir, name, swizzled, clut, width, height);
            }
            _ = format;
        }

        private static void WriteCrashChannelProbes(string dir, string name, byte[] indices, byte[] clut, int width, int height)
        {
            var orders = new (string Label, int R, int G, int B, int A)[]
            {
                ("0123_rgba", 0, 1, 2, 3),
                ("2103_bgra", 2, 1, 0, 3),
                ("1230_gbar", 1, 2, 3, 0),
                ("2301_barg", 2, 3, 0, 1),
                ("3012_argb", 3, 0, 1, 2),
                ("3210_abgr", 3, 2, 1, 0),
                ("012x_rgb_opaque", 0, 1, 2, -1),
                ("210x_bgr_opaque", 2, 1, 0, -1),
            };

            foreach (bool remap in new[] { false, true })
            {
                string clutMode = remap ? "ps2clut" : "linearclut";
                foreach (var order in orders)
                {
                    string outPath = Path.Combine(dir, $"{name}.unswizzled_{clutMode}_{order.Label}.tga");
                    var rgba = PixelDecoder.FromPSMT8WithChannelOrder(indices, clut, width, height, remap, order.R, order.G, order.B, order.A);
                    TgaWriter.Write(outPath, width, height, rgba);
                }
            }
        }

        private static void WriteCrashAddressProbes(string dir, string name, byte[] swizzled, byte[] clut, int width, int height)
        {
            foreach (int storageWidth in new[] { width, 128, 256 })
            {
                foreach (bool inverse in new[] { false, true })
                {
                    string mode = inverse ? "inverse" : "forward";
                    var indices = PixelDecoder.UnswizzlePSMT8WithStorageWidth(swizzled, width, height, storageWidth, inverse);
                    string outPath = Path.Combine(dir, $"{name}.addr_{mode}_w{storageWidth}.tga");
                    TgaWriter.Write(outPath, width, height, PixelDecoder.FromPSMT8(indices, clut, width, height));
                }
            }
        }

        private static string GetCleanName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            string normalized = name.Replace('\\', '/');
            string filename = Path.GetFileName(normalized);
            while (true)
            {
                string ext = Path.GetExtension(filename);
                if (string.IsNullOrEmpty(ext)) break;
                string lowerExt = ext.ToLowerInvariant();
                if (lowerExt == ".png" || lowerExt == ".tga" || lowerExt == ".bmp" || lowerExt == ".jpg")
                {
                    filename = Path.GetFileNameWithoutExtension(filename);
                }
                else
                {
                    break;
                }
            }
            return SanitiseName(filename);
        }

        private static string SanitiseName(string name) =>
            string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));

        private static uint PackColour(System.Drawing.Color c) =>
            ((uint)c.R << 24) | ((uint)c.G << 16) | ((uint)c.B << 8) | c.A;
    }
}
