namespace PropertyChangedAnalyzers.Test.INPC005CheckIfDifferentBeforeNotifyingTests
{
    using System.Collections.Generic;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class Valid
    {
        private static readonly DiagnosticAnalyzer Analyzer = new InvocationAnalyzer();

        private static readonly IReadOnlyList<TestCaseData> TestCases = new[]
        {
            new TestCaseData("string", "Equals(value, this.bar)"),
            new TestCaseData("string", "Equals(this.bar, value)"),
            new TestCaseData("string", "Equals(value, bar)"),
            new TestCaseData("string", "Equals(value, Bar)"),
            new TestCaseData("string", "Equals(Bar, value)"),
            new TestCaseData("string", "object.Equals(Bar, value)"),
            new TestCaseData("string", "Object.Equals(Bar, value)"),
            new TestCaseData("string", "System.Object.Equals(Bar, value)"),
            new TestCaseData("string", "Nullable.Equals(value, this.bar)"),
            new TestCaseData("int?", "Nullable.Equals(value, this.bar)"),
            new TestCaseData("int?", "System.Nullable.Equals(value, this.bar)"),
            new TestCaseData("string", "value.Equals(this.bar)"),
            new TestCaseData("string", "value.Equals(bar)"),
            new TestCaseData("string", "this.bar.Equals(value)"),
            new TestCaseData("string", "bar.Equals(value)"),
            new TestCaseData("string", "string.Equals(value, this.bar, StringComparison.OrdinalIgnoreCase)"),
            new TestCaseData("string", "System.Collections.Generic.EqualityComparer<string>.Default.Equals(value, this.bar)"),
            new TestCaseData("string", "ReferenceEquals(value, this.bar)"),
            new TestCaseData("string", "object.ReferenceEquals(value, this.bar)"),
            new TestCaseData("string", "Object.ReferenceEquals(value, this.bar)"),
            new TestCaseData("string", "System.Object.ReferenceEquals(value, this.bar)"),
        };

        [TestCaseSource(nameof(TestCases))]
        public static void Check(string type, string expression)
        {
            var code = @"
namespace N
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        private int bar;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Bar
        {
            get => this.bar;
            set
            {
                if (Equals(value, this.bar))
                {
                    return;
                }

                this.bar = value;
                this.OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}".AssertReplace("Equals(value, this.bar)", expression)
  .AssertReplace("int", type);

            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCaseSource(nameof(TestCases))]
        public static void NegatedCheck(string type, string expression)
        {
            var code = @"
namespace N
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        private int bar;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Bar
        {
            get { return this.bar; }
            set
            {
                if (!Equals(value, this.bar))
                {
                    this.bar = value;
                    this.OnPropertyChanged();
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}".AssertReplace("Equals(value, this.bar)", expression)
  .AssertReplace("int", type);

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void SimpleProperty()
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
            set { this.p = value; }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void CallsRaisePropertyChangedWithEventArgsIfReturn()
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
                if (value == this.p) return;
                this.p = value;
                this.OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void CallsRaisePropertyChangedWithEventArgsIfReturnUseProperty()
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
                if (value == this.P) return;
                this.p = value;
                this.OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void CallsRaisePropertyChangedWithEventArgsIfBody()
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
                if (value != this.p)
                {
                    this.p = value;
                    this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(P)));
                }
            }
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            this.PropertyChanged?.Invoke(this, e);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void CallsRaisePropertyChangedCallerMemberName()
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
                if (value == this.p) return;
                this.p = value;
                this.OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void Invokes()
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

                this.p = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.P)));
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void InvokesCached()
        {
            var code = @"
namespace N
{
    using System.ComponentModel;

    public class C : INotifyPropertyChanged
    {
        private static readonly PropertyChangedEventArgs PPropertyChangedArgs = new PropertyChangedEventArgs(nameof(P));
 
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

                this.p = value;
                this.PropertyChanged?.Invoke(this, PPropertyChangedArgs);
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void CheckSideEffectReturn()
        {
            var code = @"
namespace N
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        private string p;
        private int misses;

        public event PropertyChangedEventHandler PropertyChanged;

        public string P
        {
            get { return this.p; }
            set
            {
                if (Equals(value, this.p))
                {
                    misses++;
                    return;
                }

                this.p = value;
                this.OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WithCheckAndThrowBefore()
        {
            var code = @"
namespace N
{
    using System;

    public class C : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _isBusy;

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public bool IsBusy
        {
            get => _isBusy;

            private set
            {
                if (value && _isBusy)
                {
                    throw new InvalidOperationException(""Already busy"");
                }

                if (value == _isBusy)
                {
                    return;
                }

                _isBusy = value;
                OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("if (Math.Abs(value - this.value) < 1e-6)")]
        [TestCase("if (Math.Abs(this.value - value) < 1e-6)")]
        public static void WithCheckAndThrowBefore(string expression)
        {
            var code = @"
namespace N
{
    using System;

    public class C : System.ComponentModel.INotifyPropertyChanged
    {
        private double value;
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public double Value
        {
            get => value;

            set
            {
                if (Math.Abs(value - this.value) < 1e-6)
                {
                    return;
                }

                this.value = value;
                OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}".AssertReplace("if (Math.Abs(value - this.value) < 1e-6)", expression);

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void CheckInLock()
        {
            var code = @"
namespace N
{
    using System;

    public class C : System.ComponentModel.INotifyPropertyChanged
    {
        private readonly object _busyLock = new object();
        private bool _value;

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public bool Value
        {
            get => _value;
            private set
            {
                lock (_busyLock)
                {
                    if (value && _value)
                    {
                        throw new InvalidOperationException();
                    }

                    if (value == _value)
                    {
                        return;
                    }

                    _value = value;
                }

                OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void CheckLockCheck()
        {
            var code = @"
namespace N
{
    using System;

    public class C : System.ComponentModel.INotifyPropertyChanged
    {
        private readonly object _gate = new object();
        private object _value;

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public object Value
        {
            get => _value;
            private set
            {
                if (ReferenceEquals(value, _value))
                {
                    return;
                }

                lock (_gate)
                {
                    if (ReferenceEquals(value, _value))
                    {
                        return;
                    }

                    _value = value;
                }

                OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenSettingNestedField()
        {
            var c2 = @"
namespace N
{
    public class C2
    {
        public int C2Value;
    }
}";
            var code = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C1 : INotifyPropertyChanged
    {
        private readonly C2 c2 = new C2();
        public event PropertyChangedEventHandler PropertyChanged;

        public int Value
        {
            get => this.c2.C2Value;
            set
            {
                if (value == this.c2.C2Value)
                {
                    return;
                }

                this.c2.C2Value = value;
                this.OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";
            RoslynAssert.Valid(Analyzer, c2, code);
        }

        [Test]
        public static void WrappingPoint()
        {
            var code = @"
namespace N
{
    using System.ComponentModel;
    using System.Drawing;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        private Point point;

        public event PropertyChangedEventHandler PropertyChanged;

        public int X
        {
            get => this.point.X;
            set
            {
                if (value == this.point.X)
                {
                    return;
                }

                this.point = new Point(value, this.point.Y);
                this.OnPropertyChanged();
            }
        }

        public int Y
        {
            get => this.point.Y;
            set
            {
                if (value == this.point.Y)
                {
                    return;
                }

                this.point = new Point(this.point.X, value);
                this.OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void TimeSpanTicks()
        {
            var code = @"
namespace N
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        private TimeSpan timeSpan;

        public event PropertyChangedEventHandler PropertyChanged;

        public long Ticks
        {
            get => this.timeSpan.Ticks;
            set
            {
                if (value == this.timeSpan.Ticks)
                {
                    return;
                }

                this.timeSpan = TimeSpan.FromTicks(value);
                this.OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
