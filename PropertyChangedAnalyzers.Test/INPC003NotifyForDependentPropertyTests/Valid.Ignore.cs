namespace PropertyChangedAnalyzers.Test.INPC003NotifyForDependentPropertyTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static partial class Valid
    {
        public static class Ignore
        {
            [Test]
            public static void Lazy1()
            {
                var delegateCommand = @"
namespace N
{
    using System;
    using System.Windows.Input;

    public class DelegateCommand : ICommand
    {
        public DelegateCommand(Func<object, bool> func)
        {
            throw new NotImplementedException();
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            throw new NotImplementedException();
        }

        public void Execute(object parameter)
        {
            throw new NotImplementedException();
        }
    }
}";
                var code = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        private bool p;
        private DelegateCommand command;

        public event PropertyChangedEventHandler PropertyChanged;

        public DelegateCommand Command
        {
            get
            {
                if (this.command != null)
                {
                    return this.command;
                }

                this.command = new DelegateCommand(param => this.P = true);
                return this.command;
            }
        }

        public bool P
        {
            get
            {
                return this.p;
            }

            set
            {
                if (this.p != value)
                {
                    this.p = value;
                    this.OnPropertyChanged();
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}";
                RoslynAssert.Valid(Analyzer, delegateCommand, code);
            }

            [Test]
            public static void Lazy2()
            {
                var delegateCommand = @"
namespace N
{
    using System;
    using System.Windows.Input;

    public class DelegateCommand : ICommand
    {
        public DelegateCommand(Func<object, bool> func)
        {
            throw new NotImplementedException();
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            throw new NotImplementedException();
        }

        public void Execute(object parameter)
        {
            throw new NotImplementedException();
        }
    }
}";
                var code = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        private bool p;
        private DelegateCommand command;

        public event PropertyChangedEventHandler PropertyChanged;

        public DelegateCommand Command
        {
            get
            {
                if (this.command == null)
                {
                    this.command = new DelegateCommand(param => this.P = true);
                }

                return this.command;
            }
        }

        public bool P
        {
            get
            {
                return this.p;
            }

            set
            {
                if (this.p != value)
                {
                    this.p = value;
                    this.OnPropertyChanged();
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}";

                RoslynAssert.Valid(Analyzer, delegateCommand, code);
            }

            [Test]
            public static void LazyNullCoalesce()
            {
                var delegateCommand = @"
namespace N
{
    using System;
    using System.Windows.Input;

    public class DelegateCommand : ICommand
    {
        public DelegateCommand(Func<object, bool> func)
        {
            throw new NotImplementedException();
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            throw new NotImplementedException();
        }

        public void Execute(object parameter)
        {
            throw new NotImplementedException();
        }
    }
}";
                var code = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        private bool p2;
        private DelegateCommand p1;

        public event PropertyChangedEventHandler PropertyChanged;

        public DelegateCommand P1
        {
            get
            {
                return this.p1 ?? (this.p1= new DelegateCommand(param => this.P2 = true));
            }
        }

        public bool P2
        {
            get
            {
                return this.p2;
            }

            set
            {
                if (this.p2 != value)
                {
                    this.p2 = value;
                    this.OnPropertyChanged();
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}";

                RoslynAssert.Valid(Analyzer, delegateCommand, code);
            }

            [Test]
            public static void LazyNullCoalesceExpressionBody()
            {
                var delegateCommand = @"
namespace N
{
    using System;
    using System.Windows.Input;

    public class DelegateCommand : ICommand
    {
        public DelegateCommand(Func<object, bool> func)
        {
            throw new NotImplementedException();
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            throw new NotImplementedException();
        }

        public void Execute(object parameter)
        {
            throw new NotImplementedException();
        }
    }
}";
                var code = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        private bool p;
        private DelegateCommand pCommand;

        public event PropertyChangedEventHandler PropertyChanged;

        public DelegateCommand PCommand => this.pCommand ?? (this.pCommand = new DelegateCommand(param => this.P = true));

        public bool P
        {
            get
            {
                return this.p;
            }

            set
            {
                if (this.p != value)
                {
                    this.p = value;
                    this.OnPropertyChanged();
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}";

                RoslynAssert.Valid(Analyzer, delegateCommand, code);
            }

            [Test]
            public static void InCtor()
            {
                var code = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        private string name;

        public C(string name)
        {
            this.name = name;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name => this.name;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";

                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void InInitializer()
            {
                var code = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        private string name;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name => this.name;

        public C Create(string name)
        {
            return new C { name = name };
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";

                RoslynAssert.Valid(Analyzer, code);
            }

            [Test(Description = "We let INPC002 nag about this.")]
            public static void SimplePropertyWithBackingField()
            {
                var code = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        private int value;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Value
        {
            get { return this.value; }
            set { this.value = value; }
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
            public static void AssigningFieldsInGetter()
            {
                var code = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        private string name;
        private int getCount;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name
        {
            get
            {
                this.getCount++;
                return this.name;
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
            public static void LazyGetter()
            {
                var code = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        private string name;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name
        {
            get
            {
                if (this.name == null)
                {
                    this.name = string.Empty;
                }

                return this.name;
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
            public static void LazyGetterExpressionBody()
            {
                var code = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        private string name;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name => this.name ?? (this.name = string.Empty);

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";

                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void DisposeMethod()
            {
                var code = @"
namespace N
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public sealed class C : INotifyPropertyChanged, IDisposable
    {
        private string name;
        private bool disposed;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name
        {
            get
            {
                this.ThrowIfDisposed();
                return this.name ?? (this.name = string.Empty);
            }
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}";

                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void Recursive()
            {
                var code = @"
namespace N
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        private double h1;

        public event PropertyChangedEventHandler PropertyChanged;

        public double H1
        {
            get => this.h1;
            set
            {
                if (value.Equals(this.h1))
                {
                    return;
                }

                this.h1 = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.Height));
            }
        }

        public double Height
        {
            get
            {
                return Math.Min(this.Height, this.H1);
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }
        }
    }
}
