namespace PropertyChangedAnalyzers.Test.INPC016NotifyAfterUpdateTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class Diagnostics
    {
        private static readonly DiagnosticAnalyzer Analyzer = new SetAccessorAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.INPC016NotifyAfterMutation);

        private const string ViewModelBaseCode = @"
namespace N.Core
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq.Expressions;
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

        protected virtual void OnPropertyChanged<T>(Expression<Func<T>> property)
        {
            this.OnPropertyChanged(((MemberExpression)property.Body).Member.Name);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";

        [Test]
        public static void OnPropertyChangedBeforeAssign()
        {
            var code = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        private int p;

        public event PropertyChangedEventHandler PropertyChanged;

        public int P
        {
            get { return this.p; }
            set
            {
                if (value == this.p)
                {
                    return;
                }

                ↓this.OnPropertyChanged(nameof(this.P));
                this.p = value;
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void OnPropertyChangedPropertyChangedEventArgsBeforeAssign()
        {
            var code = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        private int p;

        public event PropertyChangedEventHandler PropertyChanged;

        public int P
        {
            get { return this.p; }
            set
            {
                if (value == this.p)
                {
                    return;
                }

                ↓this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(P)));
                this.p = value;
            }
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            this.PropertyChanged?.Invoke(this, e);
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void PropertyChangedInvokeBeforeAssign()
        {
            var code = @"
namespace N
{
    using System.ComponentModel;

    public class C : INotifyPropertyChanged
    {
        private int p;

        public event PropertyChangedEventHandler PropertyChanged;

        public int P
        {
            get { return this.p; }
            set
            {
                if (value == this.p)
                {
                    return;
                }

                ↓this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(P)));
                this.p = value;
            }
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            this.PropertyChanged?.Invoke(this, e);
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [TestCase("this.TrySet(ref this.name, value)")]
        [TestCase("_ = this.TrySet(ref this.name, value)")]
        public static void BeforeTrySet(string trySet)
        {
            var code = @"
namespace N.Client
{
    public class C : N.Core.ViewModelBase
    {
        private string name;

        public string Greeting => $""Hello {this.Name}"";

        public string Name
        {
            get { return this.name; }
            set
            {
                ↓this.OnPropertyChanged(nameof(this.Greeting));
                this.TrySet(ref this.name, value);
            }
        }
    }
}".AssertReplace("this.TrySet(ref this.name, value)", trySet);

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, ViewModelBaseCode, code);
        }
    }
}
