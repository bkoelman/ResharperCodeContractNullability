using CodeContractNullability.Test.TestDataBuilders;
using NUnit.Framework;
using JetBrainsNotNullAttribute = JetBrains.Annotations.NotNullAttribute;

namespace CodeContractNullability.Test.Specs
{
    /// <summary>
    /// Tests concerning detection of the nullability attribute definitions at various places.
    /// </summary>
    [TestFixture]
    internal class AttributeDiscoverySpecs : NullabilityNUnitRoslynTest
    {
        [Test]
        public void When_attributes_are_not_defined_it_must_not_report_any_diagnostics()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    class C
                    {
                        int? f;
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Test]
        public void When_attributes_are_in_global_namespace_they_must_be_found()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .InGlobalNamespace())
                .InGlobalScope(@"
                    class C
                    {
                        <annotate/> int? [|f|];
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Test]
        public void When_attributes_are_in_custom_namespace_they_must_be_found()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .InCodeNamespace("Example.Custom.Space"))
                .InGlobalScope(@"
                    class C
                    {
                        <annotate/> int? [|f|];
                    }
                ")
                .ExpectingImportForNamespace("Example.Custom.Space")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Test]
        public void When_attributes_are_in_different_namespace_it_must_add_namespace_import()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    class C
                    {
                        <annotate/> int? [|f|];
                    }

                    namespace NA
                    {
                        internal class NotNullAttribute : Attribute { }
                        internal class CanBeNullAttribute : Attribute { }
                        internal class ItemNotNullAttribute : Attribute { }
                        internal class ItemCanBeNullAttribute : Attribute { }
                    }
                ")
                .ExpectingImportForNamespace("NA")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Test]
        public void When_some_attributes_are_private_they_must_not_be_found()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    class C
                    {
                        int? f;
                    }

                    public class X
                    {
                        internal class NotNullAttribute : Attribute { }
                        private class CanBeNullAttribute : Attribute { }
                        internal class ItemNotNullAttribute : Attribute { }
                        private class ItemCanBeNullAttribute : Attribute { }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Test]
        public void When_some_attributes_are_public_nested_in_private_class_they_must_not_be_found()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .InGlobalScope(@"
                    class C
                    {
                        int? f;
                    }

                    public class X
                    {
                        private class Nested
                        {
                            private class NotNullAttribute : Attribute { }
                            internal class CanBeNullAttribute : Attribute { }
                            private class ItemNotNullAttribute : Attribute { }
                            internal class ItemCanBeNullAttribute : Attribute { }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }

        [Test]
        public void When_attributes_are_in_JetBrains_assembly_it_must_add_namespace_import()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithReference(typeof (JetBrainsNotNullAttribute).Assembly)
                .InGlobalScope(@"
                    class C
                    {
                        <annotate/> int? [|f|];
                    }
                ")
                .ExpectingImportForNamespace("JetBrains.Annotations")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Test]
        public void When_attributes_are_public_in_external_assembly_it_must_add_namespace_import()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithReferenceToExternalAssemblyFor(@"
                    using System;

                    namespace External
                    {
                        public class NotNullAttribute : Attribute { }
                        public class CanBeNullAttribute : Attribute { }
                        public class ItemNotNullAttribute : Attribute { }
                        public class ItemCanBeNullAttribute : Attribute { }
                    }")
                .InGlobalScope(@"
                    class C
                    {
                        <annotate/> int? [|f|];
                    }
                ")
                .ExpectingImportForNamespace("External")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Test]
        public void When_attributes_are_internal_in_external_assembly_they_must_not_be_found()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .WithReferenceToExternalAssemblyFor(@"
                    using System;

                    namespace External
                    {
                        internal class NotNullAttribute : Attribute { }
                        internal class CanBeNullAttribute : Attribute { }
                        internal class ItemNotNullAttribute : Attribute { }
                        internal class ItemCanBeNullAttribute : Attribute { }
                    }")
                .InGlobalScope(@"
                    class C
                    {
                        int? f;
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityDiagnostic(source);
        }
    }
}