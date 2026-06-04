using System;
using System.Reflection;
using System.Linq;

class Program {
    static void Main() {
        var asm = Assembly.LoadFrom(@"C:\Users\Admin PC\.nuget\packages\microsoft.kernelmemory.core\0.98.250508.3\lib\net10.0\Microsoft.KernelMemory.Core.dll");
        Type[] types;
        try {
            types = asm.GetTypes();
        } catch (ReflectionTypeLoadException ex) {
            types = ex.Types.Where(t => t != null).ToArray();
        }
        foreach(var t in types) {
            if (t.Name.Contains("Chunk") || t.Name.Contains("Partition")) {
                Console.WriteLine(t.FullName);
                foreach(var m in t.GetMethods()) {
                    if (m.IsPublic) Console.WriteLine("  " + m.Name);
                }
            }
        }
    }
}
