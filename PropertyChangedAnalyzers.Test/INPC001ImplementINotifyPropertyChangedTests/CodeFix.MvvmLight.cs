namespace PropertyChangedAnalyzers.Test.INPC001ImplementINotifyPropertyChangedTests
{
    using System.Collections.Immutable;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using NUnit.Framework;
    using PropertyChangedAnalyzers.Test.Helpers;

    public static partial class CodeFix
    {
        public static class MvvmLight
        {
            private static readonly ImmutableArray<MetadataReference> MetadataReferences = SpecialMetadataReferences.MvvmLight;

            [Test]
            public static void SubclassViewModelBaseAddUsing()
            {
                var before = @"
namespace RoslynSandbox
{
    public class ↓Foo
    {
        public int Bar { get; set; }
    }
}";

                var after = @"
namespace RoslynSandbox
{
    using GalaSoft.MvvmLight;

    public class Foo : ViewModelBase
    {
        public int Bar { get; set; }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "Subclass GalaSoft.MvvmLight.ViewModelBase and add using.", metadataReferences: MetadataReferences);
            }

            [Test]
            public static void SubclassViewModelBaseFullyQualified()
            {
                var before = @"
namespace RoslynSandbox
{
    public class ↓Foo
    {
        public int Bar { get; set; }
    }
}";

                var after = @"
namespace RoslynSandbox
{
    public class Foo : GalaSoft.MvvmLight.ViewModelBase
    {
        public int Bar { get; set; }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "Subclass GalaSoft.MvvmLight.ViewModelBase fully qualified.", metadataReferences: MetadataReferences);
            }

            [Test]
            public static void ImplementINotifyPropertyChangedAddUsings()
            {
                var before = @"
namespace RoslynSandbox
{
    public class ↓Foo
    {
        public int Bar { get; set; }
    }
}";

                var after = @"
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "Implement INotifyPropertyChanged and add usings.", metadataReferences: MetadataReferences);
            }

            [Test]
            public static void ImplementINotifyPropertyChangedFullyQualified()
            {
                var before = @"
namespace RoslynSandbox
{
    public class ↓Foo
    {
        public int Bar { get; set; }
    }
}";

                var after = @"
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "Implement INotifyPropertyChanged fully qualified.", metadataReferences: MetadataReferences);
            }
        }
    }
}
