﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("NPerf.Core")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyProduct("NPerf.Core")]
[assembly: AssemblyCulture("")]

#if DEBUG
[assembly: InternalsVisibleTo("NPerf.Test.Core")]
[assembly: InternalsVisibleTo("NPerf.Test.Helpers")]
#endif

[assembly: ComVisible(false)]

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
