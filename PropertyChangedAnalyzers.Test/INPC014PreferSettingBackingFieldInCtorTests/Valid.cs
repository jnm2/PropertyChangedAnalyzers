namespace PropertyChangedAnalyzers.Test.INPC014PreferSettingBackingFieldInCtorTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class Valid
    {
        private static readonly DiagnosticAnalyzer Analyzer = new AssignmentAnalyzer();

        [Test]
        public static void WhenSettingField()
        {
            var code = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.Serialization;

    [DataContract]
    public class C : INotifyPropertyChanged
    {
        private int value;

        public event PropertyChangedEventHandler PropertyChanged;

        public C(int value)
        {
            this.value = value;
        }

        [DataMember]
        public int Value
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
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void AutoProperty()
        {
            var code = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.Serialization;

    [DataContract]
    public class C : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public C(int value)
        {
            this.Value = value;
        }

        [DataMember]
        public int Value { get; private set; }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoreWhenValidationThrows()
        {
            var code = @"
namespace N
{
    using System;
    using System.ComponentModel;

    public class C : INotifyPropertyChanged
    {
        private int value;

        public event PropertyChangedEventHandler PropertyChanged;

        public C(int value)
        {
            this.Value = value;
        }

        public int Value
        {
            get => this.value;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException();
                }

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
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoreWhenValidationCall()
        {
            var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;

    public class C : INotifyPropertyChanged
    {
        private int value;

        public event PropertyChangedEventHandler PropertyChanged;

        public C(int value)
        {
            this.Value = value;
        }

        public int Value
        {
            get => this.value;
            set
            {
                GreaterThan(value, 0, nameof(value));
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

        private static void GreaterThan<T>(T value, T min, string parameterName)
            where T : IComparable<T>
        {
            Debug.Assert(!string.IsNullOrEmpty(parameterName), nameof(parameterName));
            if (Comparer<T>.Default.Compare(value, min) <= 0)
            {
                string message = $""Expected {parameterName} to be greater than {min}, {parameterName} was {value}"";
                throw new ArgumentException(message, parameterName);
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoreWhenSideEffect()
        {
            var code = @"
namespace N
{
    using System.ComponentModel;

    public class C : INotifyPropertyChanged
    {
        private int value;
        private int count;

        public event PropertyChangedEventHandler PropertyChanged;

        public C(int value)
        {
            this.Value = value;
        }

        public int Value
        {
            get => this.value;
            set
            {
                if (value == this.value)
                {
                    return;
                }

                this.value = value;
                this.count++;
                this.OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("(_, __) => this.Value = value;")]
        [TestCase("delegate { this.Value = value; };")]
        public static void SettingNotifyingPropertyInLambda(string lambda)
        {
            var code = @"
namespace N
{
    using System.ComponentModel;

    public class C : INotifyPropertyChanged
    {
        private string value;

        public event PropertyChangedEventHandler PropertyChanged;

        public C(string value)
        {
            this.PropertyChanged += (_, __) => this.Value = value;
        }

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
}".AssertReplace("(_, __) => this.Value = value;", lambda);

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void SettingNotifyingPropertyInLocalFunction()
        {
            var code = @"
namespace N
{
    using System.ComponentModel;

    public class C : INotifyPropertyChanged
    {
        private string value;

        public event PropertyChangedEventHandler PropertyChanged;

        public C(string value)
        {
            void OnChanged(object _, PropertyChangedEventArgs __) => this.Value = value;

            this.PropertyChanged += OnChanged;
        }

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
}";

            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
