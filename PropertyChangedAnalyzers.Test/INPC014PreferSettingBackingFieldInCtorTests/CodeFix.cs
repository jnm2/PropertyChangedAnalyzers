namespace PropertyChangedAnalyzers.Test.INPC014PreferSettingBackingFieldInCtorTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new AssignmentAnalyzer();
        private static readonly CodeFixProvider Fix = new SetBackingFieldFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.INPC014SetBackingFieldInConstructor);

        private const string ViewModelBase = @"
namespace N.Core
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool TrySet<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";

        [Test]
        public static void SimplePropertyWithBackingFieldStatementBodySetter()
        {
            var before = @"
namespace N
{
    public class C
    {
        private int value;

        public C(int value)
        {
            ↓this.Value = value;
        }

        public int Value
        {
            get => this.value;
            private set
            {
                this.value = value;
            }
        }
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        private int value;

        public C(int value)
        {
            this.value = value;
        }

        public int Value
        {
            get => this.value;
            private set
            {
                this.value = value;
            }
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void SimplePropertyWithBackingFieldExpressionBodySetter()
        {
            var before = @"
namespace N
{
    public class C
    {
        private int value;

        public C(int value)
        {
            ↓this.Value = value;
        }

        public int Value
        {
            get => this.value;
            private set => this.value = value;
        }
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        private int value;

        public C(int value)
        {
            this.value = value;
        }

        public int Value
        {
            get => this.value;
            private set => this.value = value;
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void SimplePropertyWithBackingFieldExpressionBodySetterKeyword()
        {
            var before = @"
namespace N
{
    public class C
    {
        private int @default;

        public C(int @default)
        {
            ↓this.Value = @default;
        }

        public int Value
        {
            get => this.@default;
            private set => this.@default = value;
        }
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        private int @default;

        public C(int @default)
        {
            this.@default = @default;
        }

        public int Value
        {
            get => this.@default;
            private set => this.@default = value;
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void SimplePropertyWithBackingFieldExpressionBodySetterCollisionParameter()
        {
            var before = @"
namespace N
{
    public class C
    {
        private int f;

        public C(int f)
        {
            ↓this.Value = f;
        }

        public int Value
        {
            get => f;
            private set => f = value;
        }
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        private int f;

        public C(int f)
        {
            this.f = f;
        }

        public int Value
        {
            get => f;
            private set => f = value;
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void SimplePropertyWithBackingFieldExpressionBodySetterCollisionLocal()
        {
            var before = @"
namespace N
{
    public class C
    {
        private int f;

        public C(int value)
        {
            var f = 1;
            ↓this.Value = value;
        }

        public int Value
        {
            get => f;
            private set => f = value;
        }
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        private int f;

        public C(int value)
        {
            var f = 1;
            this.f = value;
        }

        public int Value
        {
            get => f;
            private set => f = value;
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void SimplePropertyWithBackingFieldUnderscoreNames()
        {
            var before = @"
namespace N
{
    public class C
    {
        private int _value;

        public C(int value)
        {
            ↓Value = value;
        }

        public int Value
        {
            get => _value;
            private set
            {
                _value = value;
            }
        }
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        private int _value;

        public C(int value)
        {
            _value = value;
        }

        public int Value
        {
            get => _value;
            private set
            {
                _value = value;
            }
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [TestCase("value == this.value")]
        [TestCase("this.value == value")]
        [TestCase("Equals(this.value, value)")]
        [TestCase("Equals(value, this.value)")]
        [TestCase("ReferenceEquals(this.value, value)")]
        [TestCase("ReferenceEquals(value, this.value)")]
        [TestCase("value.Equals(this.value)")]
        [TestCase("this.value.Equals(value)")]
        public static void NotifyingProperty(string equals)
        {
            var before = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.Serialization;

    [DataContract]
    public class C : INotifyPropertyChanged
    {
        private string value;

        public event PropertyChangedEventHandler PropertyChanged;

        public C(string value)
        {
            ↓this.Value = value;
        }

        [DataMember]
        public string Value
        {
            get => this.value;
            private set
            {
                if (value == this.value)
                {
                    return;
                }

                this.value = value;
                this.OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}".AssertReplace("value == this.value", equals);

            var after = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.Serialization;

    [DataContract]
    public class C : INotifyPropertyChanged
    {
        private string value;

        public event PropertyChangedEventHandler PropertyChanged;

        public C(string value)
        {
            this.value = value;
        }

        [DataMember]
        public string Value
        {
            get => this.value;
            private set
            {
                if (value == this.value)
                {
                    return;
                }

                this.value = value;
                this.OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}".AssertReplace("value == this.value", equals);
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void WhenSettingFieldUsingTrySet()
        {
            var before = @"
namespace N.Client
{
    public class C : N.Core.ViewModelBase
    {
        private int p;

        public C(int p)
        {
            ↓this.P = p;
        }
        
        public int P
        {
            get => this.p;
            set => this.TrySet(ref this.p, value);
        }
    }
}";
            var after = @"
namespace N.Client
{
    public class C : N.Core.ViewModelBase
    {
        private int p;

        public C(int p)
        {
            this.p = p;
        }
        
        public int P
        {
            get => this.p;
            set => this.TrySet(ref this.p, value);
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { ViewModelBase, before }, after);
        }

        [Test]
        public static void WhenSettingFieldUsingTrySetAndNotifyForOther()
        {
            var before = @"
namespace N.Client
{
    public class C : N.Core.ViewModelBase
    {
        private string name;

        public C(string name)
        {
            ↓this.Name = name;
        }
        
        public string Greeting => $""Hello {this.Name}"";

        public string Name
        {
            get { return this.name; }
            set
            {
                if (this.TrySet(ref this.name, value))
                {
                    this.OnPropertyChanged(nameof(this.Greeting));
                }
            }
        }
    }
}";
            var after = @"
namespace N.Client
{
    public class C : N.Core.ViewModelBase
    {
        private string name;

        public C(string name)
        {
            this.name = name;
        }
        
        public string Greeting => $""Hello {this.Name}"";

        public string Name
        {
            get { return this.name; }
            set
            {
                if (this.TrySet(ref this.name, value))
                {
                    this.OnPropertyChanged(nameof(this.Greeting));
                }
            }
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { ViewModelBase, before }, after);
        }

        [Test]
        public static void WhenShadowingParameter()
        {
            var before = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        public C(bool x)
        {
            ↓X = x;
        }

        private bool x;

        public bool X
        {
            get => x;
            set
            {
                if (value == x)
                {
                    return;
                }

                x = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";
            var after = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        public C(bool x)
        {
            this.x = x;
        }

        private bool x;

        public bool X
        {
            get => x;
            set
            {
                if (value == x)
                {
                    return;
                }

                x = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { ViewModelBase, before }, after);
        }

        [Test]
        public static void WhenShadowingLocal()
        {
            var before = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        public C(bool a)
        {
            var x = a;
            ↓X = a;
        }

        private bool x;

        public bool X
        {
            get => x;
            set
            {
                if (value == x)
                {
                    return;
                }

                x = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";
            var after = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        public C(bool a)
        {
            var x = a;
            this.x = a;
        }

        private bool x;

        public bool X
        {
            get => x;
            set
            {
                if (value == x)
                {
                    return;
                }

                x = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { ViewModelBase, before }, after);
        }

        [Test]
        public static void WhenShadowingLocalKeyword()
        {
            var before = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        public C(bool a)
        {
            var @default = a;
            ↓X = a;
        }

        private bool @default;

        public bool X
        {
            get => @default;
            set
            {
                if (value == @default)
                {
                    return;
                }

                @default = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";
            var after = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        public C(bool a)
        {
            var @default = a;
            this.@default = a;
        }

        private bool @default;

        public bool X
        {
            get => @default;
            set
            {
                if (value == @default)
                {
                    return;
                }

                @default = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { ViewModelBase, before }, after);
        }
    }
}
