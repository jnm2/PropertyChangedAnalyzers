namespace PropertyChangedAnalyzers.Test.INPC002MutablePublicPropertyShouldNotifyTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static partial class CodeFix
    {
        public static class Repros
        {
            [Ignore("#70")]
            [Test]
            public static void UglyViewModelBase()
            {
                var c1 = @"
namespace N
{
    public class C1
    {
        public int C1Value;
    }
}";

                var viewModelBaseCode = @"
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace MVVM
{
    /// <summary>
    /// Base class for all ViewModel classes in the application.
    /// It provides support for property change notifications 
    /// and has a DisplayName property.  This class is abstract.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable
    {
        #region Constructor

        protected ViewModelBase()
        {
        }

        #endregion // Constructor

        #region DisplayName

        /// <summary>
        /// Returns the user-friendly name of this object.
        /// Child classes can set this property to a new value,
        /// or override it to determine the value on-demand.
        /// </summary>
        public virtual string DisplayName
        {
            get => this.displayName;
            protected set
            {
                if (value == this.displayName)
                {
                    return;
                }

                this.displayName = value;
                this.NotifyPropertyChanged();
            }
        }

        #endregion // DisplayName

        #region Debugging Aides

        /// <summary>
        /// Warns the developer if this object does not have
        /// a public property with the specified name. This 
        /// method does not exist in a Release build.
        /// </summary>
        [Conditional(""DEBUG"")]
        [DebuggerStepThrough]
        public void VerifyPropertyName(string propertyName)
        {
            // Verify that the property name matches a real,  
            // public, instance property on this object.
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                string msg = ""Invalid property name: "" + propertyName;

                if (this.ThrowOnInvalidPropertyName)
                    throw new Exception(msg);
                else
                    Debug.Fail(msg);
            }
        }

        /// <summary>
        /// Returns whether an exception is thrown, or if a Debug.Fail() is used
        /// when an invalid property name is passed to the VerifyPropertyName method.
        /// The default value is false, but subclasses used by unit tests might 
        /// override this property's getter to return true.
        /// </summary>
        protected virtual bool ThrowOnInvalidPropertyName { get; private set; }

        #endregion // Debugging Aides

        #region INotifyPropertyChanged Members

        /// <summary>
        /// Raised when a property on this object has a new value.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name=""propertyName"">The property that has a new value.</param>
        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            this.VerifyPropertyName(propertyName);

            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }

        protected virtual void NotifyPropertyChangedAll(object inOjbect)
        {
            foreach (PropertyInfo pi in inOjbect.GetType().GetProperties())
            {
                NotifyPropertyChanged(pi.Name);
            }
        }
        public virtual void Refresh()
        {
            NotifyPropertyChangedAll(this);
        }
        #endregion // INotifyPropertyChanged Members

        #region IDisposable Members
        readonly List<IDisposable> _disposables = new List<IDisposable>();
        readonly object _disposeLock = new object();
        bool _isDisposed;

        /// <summary>
        /// Invoked when this object is being removed from the application
        /// and will be subject to garbage collection.
        /// </summary>
        public void Dispose()
        {
            lock (_disposeLock)
            {
                this.OnDispose();

                if (_isDisposed) return;

                foreach (var disposable in _disposables)
                    disposable.Dispose();

                _isDisposed = true;
            }
        }

        /// <summary>
        /// Child classes can override this method to perform 
        /// clean-up logic, such as removing event handlers.
        /// </summary>
        protected virtual void OnDispose()
        {
        }

#if DEBUG
        /// <summary>
        /// Useful for ensuring that ViewModel objects are properly garbage collected.
        /// </summary>
        ~ViewModelBase()
        {
            string msg = string.Format(""{0} ({1}) ({2}) Finalized"", this.GetType().Name, this.DisplayName, this.GetHashCode());
            System.Diagnostics.Debug.WriteLine(msg);
        }
#endif

        #endregion // IDisposable Members
    }
}";
                var before = @"
namespace N
{
    using MVVM;

    public class C : ViewModelBase
    {
        private readonly C1 c1 = new C1();

        public int ↓Value
        {
            get => this.c1.C1Value;
            set => this.c1.C1Value = value;
        }
    }
}";

                var after = @"
namespace N
{
    using MVVM;

    public class C : ViewModelBase
    {
        private readonly C1 c1 = new C1();

        public int Value
        {
            get => this.c1.C1Value;
            set => this.c1.C1Value = value;
        }
    }
}";

                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { viewModelBaseCode, c1, before }, after);
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { viewModelBaseCode, c1, before }, after);
            }
        }
    }
}
