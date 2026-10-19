# NetP3DLib
This is a .NET library for reading, modifying and writing Radical Pure3D files.

Written in `.NET Standard 2.0` and requires `System.Numerics.Vectors v4.5.0`.

# Example code

## Loading a P3D file
The library currently only supports loading a Pure3D file, rather than any stream.
```cs
string filePath = @"C:\temp\example.p3d";
var p3dFile = new NetP3DLib.P3D.P3DFile(filePath);
```
The library supports compressed files, as well as either Little Endian or Big Endian.

There are a couple of errors that can be thrown when reading a Pure3D file:
1. `FileNotFoundException` - Self explanatory, but will be thrown if the given file cannot be found on the filesystem.
2. `InvalidDataException` - This will be throw if the file given is an invalid Pure3D file. The exception message will contains details on what is invalid.

## Adding chunk types
The library contains chunk definitions for all chunks used by `Simpsons: Hit & Run`, however if you wish to add custom chunk types you can do so.

```cs
NetP3DLib.P3D.ChunkLoader.LoadChunkTypes("MyNamespace.P3D.Chunks", true, true);
```
`LoadChunkTypes` has 3 arguments:
- `string nameSpace`
  - The namespace to load chunk types from. The `NetP3DLib.P3D.ChunkLoader` class automatically loads from `NetP3DLib.P3D.Chunks` on load.
- `bool throwOnDuplicate`
  - If `true`, if a known chunk identifier is encountered, it will throw an error.
  - If `false`, if a known chunk identifier is encountered, it will be overwritten.
- `bool throwOnInvalid`
  - If `true`, if an invalid chunk class is encountered, it will throw an error.
  - If `false`, if an invalid chunk class is encountered, it will be ignored.

There are 2 helper classes you can inherit from.
- `NamedChunk` - For use on chunks with a `Name` property. Allows for finding chunks by name.
- `ParamChunk` - For use on chunks with a FourCC `Param` property. Allows for finding chunks by param.