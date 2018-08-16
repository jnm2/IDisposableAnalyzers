namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    internal partial class DisposableTests
    {
        internal class IsAlreadyAssignedWithCreated
        {
            [Test]
            public void FieldAssignedInCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    internal class Foo
    {
        private Disposable disposable;

        internal Foo()
        {
            this.disposable = new Disposable();
        }
    }

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindAssignmentExpression("this.disposable = new Disposable()").Left;
                Assert.AreEqual(Result.No, Disposable.IsAlreadyAssignedWithCreated(value, semanticModel, CancellationToken.None, out _));
            }

            [Test]
            public void FieldAssignedInLambdaCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private Disposable disposable;

        public Foo()
        {
            Console.CancelKeyPress += (o, e) =>
            {
                this.disposable = new Disposable();
            };
        }
    }

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindAssignmentExpression("disposable = new Disposable()").Left;
                Assert.AreEqual(Result.Yes, Disposable.IsAlreadyAssignedWithCreated(value, semanticModel, CancellationToken.None, out _));
            }

            [Test]
            public void LocalSeparateDeclarationAndAssignment()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    internal class Foo
    {
        internal Foo()
        {
            IDisposable disposable;
            disposable = new Disposable();
        }
    }

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindAssignmentExpression("disposable = new Disposable()").Left;
                Assert.AreEqual(Result.No, Disposable.IsAlreadyAssignedWithCreated(value, semanticModel, CancellationToken.None, out _));
            }

            [Test]
            public void LocalSeparateDeclarationAndAssignmentInLambda()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo()
        {
            Console.CancelKeyPress += (o, e) =>
            {
                IDisposable disposable;
                disposable = new Disposable();
            };
        }
    }

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindAssignmentExpression("disposable = new Disposable()").Left;
                Assert.AreEqual(Result.No, Disposable.IsAlreadyAssignedWithCreated(value, semanticModel, CancellationToken.None, out _));
            }

            [Test]
            public void LocalAssignmentInLambda()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo()
        {
            IDisposable disposable;
            Console.CancelKeyPress += (o, e) =>
            {
                disposable = new Disposable();
            };
        }
    }

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindAssignmentExpression("disposable = new Disposable()").Left;
                Assert.AreEqual(Result.Yes, Disposable.IsAlreadyAssignedWithCreated(value, semanticModel, CancellationToken.None, out _));
            }

            [Test]
            public void Repro()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private Bar bar1;
        private Bar bar2;

        public Bar Bar1
        {
            get
            {
                return this.bar1;
            }

            set
            {
                if (Equals(value, this.bar1))
                {
                    return;
                }

                if (value != null && this.bar2 != null)
                {
                    this.Bar2 = null;
                }

                if (this.bar1 != null)
                {
                    this.bar1.Selected = false;
                }

                this.bar1 = value;
                if (this.bar1 != null)
                {
                    this.bar1.Selected = true;
                }
            }
        }

        public Bar Bar2
        {
            get
            {
                return this.bar2;
            }

            set
            {
                if (Equals(value, this.bar2))
                {
                    return;
                }

                if (value != null && this.bar1 != null)
                {
                    this.Bar1 = null;
                }

                if (this.bar2 != null)
                {
                    this.bar2.Selected = false;
                }

                this.bar2 = value;
                if (this.bar2 != null)
                {
                    this.bar2.Selected = true;
                }
            }
        }
    }

    public class Bar
    {
        public bool Selected { get; set; }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindAssignmentExpression("this.Bar1 = null;").Left;
                Assert.AreEqual(Result.No, Disposable.IsAlreadyAssignedWithCreated(value, semanticModel, CancellationToken.None, out _));
            }
        }
    }
}