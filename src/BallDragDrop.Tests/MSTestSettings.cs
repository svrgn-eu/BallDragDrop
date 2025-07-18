using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

// Define a custom attribute for STA thread tests
namespace Microsoft.VisualStudio.TestTools.UnitTesting
{
    [AttributeUsage(AttributeTargets.Method)]
    public class STATestMethodAttribute : TestMethodAttribute
    {
        public STATestMethodAttribute() : base()
        {
        }

        public override TestResult[] Execute(ITestMethod testMethod)
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                return base.Execute(testMethod);

            TestResult[] result = null;
            var thread = new Thread(() =>
            {
                result = base.Execute(testMethod);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            return result;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class STATestClassAttribute : TestClassAttribute
    {
        public STATestClassAttribute() : base()
        {
        }
    }
}
