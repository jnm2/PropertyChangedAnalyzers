namespace PropertyChangedAnalyzers.Test.INPC015PropertyIsRecursiveTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class Diagnostics
    {
        private static readonly DiagnosticAnalyzer Analyzer = new PropertyDeclarationAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.INPC015PropertyIsRecursive);

        [Test]
        public static void ExpressionBody()
        {
            var code = @"
namespace N
{
    public class C
    {
        public int P => ↓this.P;
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void GetterStatementBody()
        {
            var code = @"
namespace N
{
    public class C
    {
        public int P
        {
            get { return ↓this.P; }
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void GetterExpressionBody()
        {
            var code = @"
namespace N
{
    public class C
    {
        public int P
        {
            get => ↓this.P;
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void SetterStatementBody()
        {
            var code = @"
namespace N
{
    public class C
    {
        private int p;

        public int P
        {
            get { return this.p; }
            set { ↓this.P = value; }
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void SetterExpressionBody()
        {
            var code = @"
namespace N
{
    public class C
    {
        private int p;

        public int P
        {
            get => this.p;
            set => ↓this.P = value;
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }
    }
}
