using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace GitHubReleaser
{
    static class EmbeddedAssembly
    {
        private static Assembly assembly = Assembly.GetExecutingAssembly();
        private static string[] embeddedLibraries = assembly.GetManifestResourceNames().Where(x => x.EndsWith(".dll")).ToArray();

        public static void Init()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name).Name + ".dll";

            var resourceName = embeddedLibraries.FirstOrDefault(x => x.EndsWith(assemblyName));
            if(resourceName!=null)
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    var bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);
                    return Assembly.Load(bytes);
                }
            return null;
        }

    }
}
