﻿namespace NPerf.Lab.TestBuilder
{
    using System;
    using System.CodeDom.Compiler;
    using System.Linq;
    using System.Reflection;
    using CodeDomUtilities;
    using Fasterflect;
    using Microsoft.CSharp;
    using NPerf.Framework;
    using NPerf.Framework.Interfaces;

    public class TestSuiteAssemblyBuilder
    {
        private readonly Type testerType;

        private readonly Type testedAbstraction;

        private readonly Type typeToTest;

        private readonly MethodInfo runDescriptor;

        private readonly MethodInfo setUp;

        private readonly MethodInfo tearDown;

        private readonly MethodInfo[] methods;

        private readonly int defaultTestCount;

        private readonly string description;

        private readonly string featureDescription;

        public TestSuiteAssemblyBuilder(Type testerType, Type testedType)
        {
            if (testerType == null)
            {
                throw new ArgumentNullException("testerType");
            }

            if (testedType == null)
            {
                throw new ArgumentNullException("testedType");
            }

            this.typeToTest = testedType;

            this.testerType = testerType;
            var testerAttribute = this.testerType.Attribute<PerfTesterAttribute>();
            if (testerAttribute == null)
            {
                throw new ArgumentException("Tester type must be marked by PerfTesterAttribute", "testerType");
            }

            this.testedAbstraction = testerAttribute.TestedType;
            if (!this.testedAbstraction.IsAssignableFrom(testedType))
            {
                throw new ArgumentException(string.Format("Tester type {0} is not assignable from {1}", this.testedAbstraction, testedType), "testedType");
            }

            this.defaultTestCount = testerAttribute.TestCount;
            this.description = testerAttribute.Description;
            this.featureDescription = testerAttribute.FeatureDescription;

            // get run descriptor
            this.runDescriptor =
                this.testerType.MethodsWith(Flags.AllMembers, typeof(PerfRunDescriptorAttribute))
                    .FirstOrDefault(
                        m => m.ReturnType == typeof(double) && m.HasParameterSignature(new[] { typeof(int) }));

            // get set up
            this.setUp =
                this.testerType.MethodsWith(Flags.AllMembers, typeof(PerfSetUpAttribute))
                    .FirstOrDefault(
                        m => m.ReturnType == typeof(void) && m.HasParameterSignature(new[] { typeof(int), this.testedAbstraction }));

            // get tear down
            this.tearDown =
                this.testerType.MethodsWith(Flags.AllMembers, typeof(PerfTearDownAttribute))
                    .FirstOrDefault(
                        m => m.ReturnType == typeof(void) && m.HasParameterSignature(new[] { this.testedAbstraction }));

            // get test method
            this.methods =
                this.testerType.MethodsWith(Flags.AllMembers, typeof(PerfTestAttribute))
                    .Where(m => m.ReturnType == typeof(void) && m.HasParameterSignature(new[] { this.testedAbstraction }))
                    .ToArray();
        }

        public string CreateSourceCode()
        {
            var testSuiteCode = new TestSuiteCodeBuilder
            {
                TesterType = this.testerType.Namespace + "." + this.testerType.Name,
                RunDescriptorMethodName = this.runDescriptor == null ? string.Empty : this.runDescriptor.Name,
                SetUpMethodName = this.setUp == null ? string.Empty : this.setUp.Name,
                TearDownMethodName = this.tearDown == null ? string.Empty : this.tearDown.Name,
                DefaultTestCount = this.defaultTestCount,
                TestedAbstraction = this.testedAbstraction.Namespace + "." + this.testedAbstraction.Name,
                TypeToTest = this.typeToTest.Namespace + "." + this.typeToTest.Name,
                Description = this.description,
                FeatureDescription = this.featureDescription,
                Tests = (from method in
                             this.testerType.MethodsWith(Flags.AllMembers, typeof(PerfTestAttribute))
                         where method.ReturnType == typeof(void) && method.HasParameterSignature(new[] { this.testedAbstraction })
                         let testAttribute = method.Attribute<PerfTestAttribute>()
                         let ignoreAttribute = method.Attribute<PerfIgnoreAttribute>()
                         select new TestInfo
                         {
                             Name = method.Name,
                             Description = testAttribute.Description,
                             IsIgnore = ignoreAttribute != null,
                             IgnoreMessage = ignoreAttribute == null ? string.Empty : ignoreAttribute.Message
                         }).ToArray()
            };

            return testSuiteCode.BuildCode().GetCSharp();
        }
        
        public string CreateTestSuite()
        {
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();
           
            CompilerParameters parameters = new CompilerParameters();

            parameters.ReferencedAssemblies.Add("System.dll");
            var assemblies = new[] { this.testedAbstraction.Assembly, this.testerType.Assembly, this.typeToTest.Assembly, typeof(IPerfTest).Assembly};
            foreach (var assm in assemblies.Distinct())
            {
                parameters.ReferencedAssemblies.Add(assm.Location);
            }
            
            parameters.GenerateInMemory = false;
            parameters.IncludeDebugInformation = true;

            var results = codeProvider.CompileAssemblyFromSource(parameters, this.CreateSourceCode());

            if (results.Errors.HasErrors)
            {
                var errorMessage = results.Errors.Count + " Errors:";

                for (var x = 0; x < results.Errors.Count; x++)
                {
                    errorMessage = errorMessage + "\r\nLine: " + results.Errors[x].Line + " - " + results.Errors[x].ErrorText;
                }
                throw new Exception(errorMessage);
            }
            return results.PathToAssembly;
        }
    }
}