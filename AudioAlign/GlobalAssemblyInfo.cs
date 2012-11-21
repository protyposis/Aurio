using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// http://stackoverflow.com/questions/62353/what-are-the-best-practices-for-using-assembly-attributes

[assembly: AssemblyProduct("AudioAlign")]
[assembly: AssemblyCompany("Mario Guggenberger")]
[assembly: AssemblyCopyright("Copyright © 2011-2012 Mario Guggenberger")]
//[assembly: AssemblyTrademark("")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
[assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0 Alpha")]
