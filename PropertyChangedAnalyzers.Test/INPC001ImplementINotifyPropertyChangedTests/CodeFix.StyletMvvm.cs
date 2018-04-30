namespace PropertyChangedAnalyzers.Test.INPC001ImplementINotifyPropertyChangedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;
    using PropertyChangedAnalyzers.Test.Helpers;

    internal partial class CodeFix
    {
        internal class StyletMvvm
        {
            [OneTimeSetUp]
            public void OneTimeSetUp()
            {
                AnalyzerAssert.MetadataReferences.AddRange(SpecialMetadataReferences.Stylet);
            }

            [OneTimeTearDown]
            public void TearDown()
            {
                AnalyzerAssert.ResetAll();
            }

            [Test]
            public void SubclassPropertyChangedBaseAddUsing()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public class ↓Foo
    {
        public int Bar { get; set; }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using Stylet;

    public class Foo : PropertyChangedBase
    {
        public int Bar { get; set; }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode, fixTitle: "Subclass Stylet.PropertyChangedBase and add using.");
            }

            [Test]
            public void SubclassPropertyChangedBaseFullyQualified()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public class ↓Foo
    {
        public int Bar { get; set; }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo : Stylet.PropertyChangedBase
    {
        public int Bar { get; set; }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode, fixTitle: "Subclass Stylet.PropertyChangedBase fully qualified.");
            }

            [Test]
            public void ImplementINotifyPropertyChangedAddUsings()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public class ↓Foo
    {
        public int Bar { get; set; }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class Foo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int Bar { get; set; }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode, fixTitle: "Implement INotifyPropertyChanged and add usings.");
            }

            [Test]
            public void ImplementINotifyPropertyChangedFullyQualified()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public class ↓Foo
    {
        public int Bar { get; set; }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public int Bar { get; set; }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode, fixTitle: "Implement INotifyPropertyChanged fully qualified.");
            }
        }
    }
}
