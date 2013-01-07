﻿namespace NPerf.Framework
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Defines a test set-up method.
    /// </summary>
    /// <include file='NPerf.Framework.Doc.xml' path='doc/remarkss/remarks[@name="PerfSetUpAttribute"]'/>
    /// <include file='NPerf.Framework.Doc.xml' path='doc/examples/example[@name="PerfSetUpTearDownAttribute"]'/>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class PerfSetUpAttribute : Attribute
    {
    }
}
