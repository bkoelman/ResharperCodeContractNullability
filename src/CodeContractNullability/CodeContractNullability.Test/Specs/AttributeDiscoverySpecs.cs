using CodeContractNullability.Test.TestDataBuilders;
using Xunit;
using JetBrainsNotNullAttribute = JetBrains.Annotations.NotNullAttribute;

namespace CodeContractNullability.Test.Specs
{
    /// <summary>
    /// Tests concerning detection of the nullability attribute definitions at various places.
    /// </summary>
    public sealed class AttributeDiscoverySpecs : NullabilityTest
    {
        [Fact]
        public void When_attributes_are_not_defined_it_must_not_report_any_diagnostics()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithoutNullabilityAttributes()
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

        [Fact]
        public void When_attributes_are_in_global_namespace_they_must_be_found()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .InGlobalNamespace())
                .InGlobalScope(@"
                    namespace N
                    {
                        class C
                        {
                            [+NullabilityAttributePlaceholder+]
                            int? [|f|];
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForField("f"));
        }

        [Fact]
        public void When_attributes_are_in_different_namespace_it_must_add_namespace_import()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .InCodeNamespace("NA"))
                .InGlobalScope(@"
                    class C
                    {
                        [+NullabilityAttributePlaceholder+]
                        int? [|f|];
                    }
                ")
                .ExpectingImportForNamespace("NA")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForField("f"));
        }

        [Fact]
        public void When_some_attributes_are_private_none_must_be_found()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithoutNullabilityAttributes()
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

        [Fact]
        public void When_attributes_are_public_nested_they_must_be_found()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .NestedInTypes(new[]
                    {
                        "public class X",
                        "public class Nested"
                    }))
                .InGlobalScope(@"
                    class C
                    {
                        [+NullabilityAttributePlaceholder+]
                        int? [|f|];
                    }
                ")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForField("f"));
        }

        [Fact]
        public void When_attributes_are_public_nested_in_private_class_they_must_not_be_found()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullabilityAttributes(new NullabilityAttributesBuilder()
                    .NestedInTypes(new[]
                    {
                        "public class X",
                        "private class Nested"
                    }))
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

        [Fact]
        public void When_attributes_are_in_JetBrains_assembly_it_must_add_namespace_import()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithoutNullabilityAttributes()
                .WithReference(typeof(JetBrainsNotNullAttribute).Assembly)
                .InGlobalScope(@"
                    class C
                    {
                        [+NullabilityAttributePlaceholder+]
                        int? [|f|];
                    }
                ")
                .ExpectingImportForNamespace("JetBrains.Annotations")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForField("f"));
        }

        [Fact]
        public void When_attributes_are_public_in_external_assembly_it_must_add_namespace_import()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithoutNullabilityAttributes()
                .WithReferenceToExternalAssemblyFor(@"
                    using System;

                    namespace OtherAssembly
                    {
                        public class NotNullAttribute : Attribute { }
                        public class CanBeNullAttribute : Attribute { }
                        public class ItemNotNullAttribute : Attribute { }
                        public class ItemCanBeNullAttribute : Attribute { }
                    }")
                .InGlobalScope(@"
                    class C
                    {
                        [+NullabilityAttributePlaceholder+]
                        int? [|f|];
                    }
                ")
                .ExpectingImportForNamespace("OtherAssembly")
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForField("f"));
        }

        [Fact]
        public void When_attributes_are_internal_in_external_assembly_they_must_not_be_found()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithoutNullabilityAttributes()
                .WithReferenceToExternalAssemblyFor(@"
                    using System;

                    namespace OtherAssembly
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
