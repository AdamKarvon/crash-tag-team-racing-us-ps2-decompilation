using System;
using System.IO;
using System.Linq;
using NetP3DLib.P3D;
using NetP3DLib.P3D.Chunks;

namespace CTTR_Online.Tools
{
    class P3DSkeletonDumper
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: P3DSkeletonDumper.exe <file.p3d>");
                return;
            }

            string filePath = args[0];
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: File not found: {filePath}");
                return;
            }

            try
            {
                Console.WriteLine($"--- Loading P3D File: {Path.GetFileName(filePath)} ---");
                
                // Use NetP3DLib to load the file
                P3DFile p3d = new P3DFile(filePath);

                // Find all Skeleton2 chunks (CTTR specific)
                var skeletons = p3d.Chunks.OfType<Skeleton2Chunk>().ToList();

                if (skeletons.Count == 0)
                {
                    Console.WriteLine("No Skeleton2 (CTTR format) chunks found. Checking for standard skeletons...");
                    var stdSkeletons = p3d.Chunks.OfType<SkeletonChunk>().ToList();
                    foreach (var skel in stdSkeletons)
                    {
                        DumpStandardSkeleton(skel);
                    }
                }
                else
                {
                    foreach (var skel in skeletons)
                    {
                        DumpCTTRSkeleton(skel);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal Error: {ex.Message}");
            }
        }

        static void DumpCTTRSkeleton(Skeleton2Chunk skel)
        {
            Console.WriteLine($"\n[Skeleton2] Name: {skel.Name} | Version: {skel.Version}");
            Console.WriteLine($"Joint Count: {skel.NumJoints} | Partitions: {skel.NumPartitions} | Limbs: {skel.NumLimbs}");

            var joints = skel.Children.OfType<SkeletonJoint2Chunk>().ToList();
            for (int i = 0; i < joints.Count; i++)
            {
                var joint = joints[i];
                string indent = new string(' ', 4);
                Console.WriteLine($"{indent}Joint [{i}] Name: {joint.Name} | Parent: {joint.Parent}");
                
                // Dump Rest Pose Matrix
                var m = joint.RestPose;
                Console.WriteLine($"{indent}    M1: {m.M11:F4}, {m.M12:F4}, {m.M13:F4}, {m.M14:F4}");
                Console.WriteLine($"{indent}    M2: {m.M21:F4}, {m.M22:F4}, {m.M23:F4}, {m.M24:F4}");
                Console.WriteLine($"{indent}    M3: {m.M31:F4}, {m.M32:F4}, {m.M33:F4}, {m.M34:F4}");
                Console.WriteLine($"{indent}    M4: {m.M41:F4}, {m.M42:F4}, {m.M43:F4}, {m.M44:F4}");
            }
        }

        static void DumpStandardSkeleton(SkeletonChunk skel)
        {
            Console.WriteLine($"\n[Skeleton] Name: {skel.Name} | Version: {skel.Version}");
            Console.WriteLine($"Joint Count: {skel.NumJoints}");

            var joints = skel.Children.OfType<SkeletonJointChunk>().ToList();
            for (int i = 0; i < joints.Count; i++)
            {
                var joint = joints[i];
                Console.WriteLine($"    Joint [{i}] Name: {joint.Name} | Parent: {joint.Parent}");
            }
        }
    }
}
