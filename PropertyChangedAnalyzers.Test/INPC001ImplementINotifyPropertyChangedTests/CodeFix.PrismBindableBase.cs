namespace PropertyChangedAnalyzers.Test.INPC001ImplementINotifyPropertyChangedTests
{
    using System.Collections.Immutable;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using NUnit.Framework;
    using PropertyChangedAnalyzers.Test.Helpers;

    public static partial class CodeFix
    {
        public static class PrismBindableBase
        {
            internal static ImmutableArray<MetadataReference> MetadataReferences { get; } = SpecialMetadataReferences.Prism;

            [Test]
            public static void SubclassBindableBaseAddUsing()
            {
                var before = @"
namespace N
{
    public class ↓C
    {
        public int P { get; set; }
    }
}";

                var after = @"
namespace N
{
    using Microsoft.Practices.Prism.Mvvm;

    public class C : BindableBase
    {
        public int P { get; set; }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "Subclass Microsoft.Practices.Prism.Mvvm.BindableBase and add using.", metadataReferences: MetadataReferences);
            }

            [Test]
            public static void SubclassBindableBaseFullyQualified()
            {
                var before = @"
namespace N
{
    public class ↓C
    {
        public int P { get; set; }
    }
}";

                var after = @"
namespace N
{
    public class C : Microsoft.Practices.Prism.Mvvm.BindableBase
    {
        public int P { get; set; }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "Subclass Microsoft.Practices.Prism.Mvvm.BindableBase fully qualified.", metadataReferences: MetadataReferences);
            }

            [Test]
            public static void ImplementINotifyPropertyChangedAddUsings()
            {
                var before = @"
namespace N
{
    public class ↓C
    {
        public int P { get; set; }
    }
}";

                var after = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int P { get; set; }

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
namespace N
{
    public class ↓C
    {
        public int P { get; set; }
    }
}";

                var after = @"
namespace N
{
    public class C : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public int P { get; set; }

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
