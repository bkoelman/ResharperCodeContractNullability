using System.Collections.Generic;
using System.Xml.Serialization;
using Xunit;
#if !NET452
using CodeContractNullability.Test.TestDataBuilders;

namespace CodeContractNullability.Test.Specs
{
    /// <summary>
    /// Tests for conversion to C# built-in nullability syntax.
    /// </summary>
    public sealed class NullableReferenceTypeConversionSpecs : ConversionNullabilityTest
    {
        [Fact]
        public void When_nullable_reference_types_are_not_enabled_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    class C
                    {
                        [CanBeNull]
                        string f;
                    }
                ")
                .Build();

            // Act and assert
            VerifyDiagnostics(source);
        }

        [Fact]
        public void When_attributes_are_not_defined_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .WithoutNullabilityAttributes()
                .InGlobalScope(@"
                    public class CanBeNullAttribute : Attribute
                    {
                    }

                    class C
                    {
                        [CanBeNull]
                        string f;
                    }
                ")
                .Build();

            // Act and assert
            VerifyDiagnostics(source);
        }

        [Fact]
        public void When_field_is_not_annotated_with_nullability_attributes_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .WithReference(typeof(XmlAttributeAttribute).Assembly)
                .Using(typeof(XmlAttributeAttribute).Namespace)
                .InGlobalScope(@"
                    class C
                    {
                        [XmlAttribute]
                        string f;
                    }
                ")
                .Build();

            // Act and assert
            VerifyDiagnostics(source);
        }

        [Fact]
        public void When_field_is_annotated_with_conflicting_nullability_attributes_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .Using(typeof(IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    class C
                    {
                        [NotNull, CanBeNull]
                        string f1;

                        [ItemNotNull]
                        [ItemCanBeNull]
                        IEnumerable<string> f2;
                    }
                ")
                .Build();

            // Act and assert
            VerifyDiagnostics(source);
        }

        [Fact]
        public void When_field_is_annotated_with_NotNull_it_must_remove_attribute()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .InGlobalScope(@"
                    class C
                    {
                        [-[NotNull]
                        -]string [|f|];
                    }
                ")
                .Build();

            // Act and assert
            VerifyFix(source, CreateMessageForField("f"));
        }

        [Fact]
        public void When_field_is_annotated_with_ItemNotNull_it_must_remove_attribute()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .Using(typeof(IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    class C
                    {
                        [-[ItemNotNull]
                        -]IEnumerable<string> [|f|];
                    }
                ")
                .Build();

            // Act and assert
            VerifyFix(source, CreateMessageForField("f"));
        }

        [Fact]
        public void When_field_is_annotated_with_CanBeNull_it_must_be_reported_and_converted()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .InGlobalScope(@"
                    class C
                    {
                        [-[CanBeNull]
                        -]string[+?+] [|f|];
                    }
                ")
                .Build();

            // Act and assert
            VerifyFix(source, CreateMessageForField("f"));
        }

        [Fact]
        public void When_multi_field_is_annotated_with_CanBeNull_it_must_be_reported_and_converted()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .InGlobalScope(@"
                    class C
                    {
                        [-[CanBeNull]
                        -]string[+?+] [|f|], [|g|];
                    }
                ")
                .Build();

            // Act and assert
            VerifyFix(source,
                CreateMessageForField("f"),
                CreateMessageForField("g"));
        }

        [Fact]
        public void When_field_is_annotated_with_NotNull_and_ItemCanBeNull_it_must_be_reported_and_converted()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .Using(typeof(IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    class C
                    {
                        [-[NotNull, ItemCanBeNull]
                        -]IEnumerable<string[+?+]> [|f|];
                    }
                ")
                .Build();

            // Act and assert
            VerifyFix(source, CreateMessageForField("f"));
        }

        [Fact]
        public void When_field_is_annotated_with_CanBeNull_and_ItemNotNull_it_must_be_reported_and_converted()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .Using(typeof(IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    class C
                    {
                        [-[CanBeNull]
                        [ItemNotNull]
                        -]IEnumerable<string>[+?+] [|f|];
                    }
                ")
                .Build();

            // Act and assert
            VerifyFix(source, CreateMessageForField("f"));
        }

        [Fact]
        public void When_field_is_annotated_with_CanBeNull_and_ItemCanBeNull_it_must_add_question_marks()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .Using(typeof(IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    class C
                    {
                        [-[CanBeNull, ItemCanBeNull]
                        -]IEnumerable<string[+?+]>[+?+] [|f|];
                    }
                ")
                .Build();

            // Act and assert
            VerifyFix(source, CreateMessageForField("f"));
        }

        [Fact]
        public void When_array_field_is_annotated_with_CanBeNull_and_ItemCanBeNull_it_must_add_question_marks()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .Using(typeof(IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    class C
                    {
                        [-[CanBeNull, ItemCanBeNull]
                        -]string[+?+][][+?+] [|f|];
                    }
                ")
                .Build();

            // Act and assert
            VerifyFix(source, CreateMessageForField("f"));
        }

        [Fact]
        public void When_field_is_value_type_it_must_remove_attribute()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .InGlobalScope(@"
                    class C
                    {
                        [-[ItemCanBeNull] [CanBeNull]-] // trailing comment that prevents line removal
                        int [|f|];
                    }
                ")
                .Build();

            // Act and assert
            VerifyFix(source, CreateMessageForField("f"));
        }

        [Fact]
        public void When_field_is_nullable_value_type_it_must_remove_attribute()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .InGlobalScope(@"
                    class C
                    {
                        /* leading comment that prevents line removal */ [-[CanBeNull]
                        -]int? [|f|];
                    }
                ")
                .Build();

            // Act and assert
            VerifyFix(source, CreateMessageForField("f"));
        }

        [Fact]
        public void When_field_is_system_nullable_value_type_it_must_remove_attribute()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .InGlobalScope(@"
                    class C
                    {
                        [-[CanBeNull]
                        -]System.Nullable<int> [|f|];
                    }
                ")
                .Build();

            // Act and assert
            VerifyFix(source, CreateMessageForField("f"));
        }

        [Fact]
        public void When_field_is_lazy_nullable_value_type_it_must_remove_attribute()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .InGlobalScope(@"
                    class C
                    {
                        [-[ItemCanBeNull]
                        -]Lazy<int?> [|f|];
                    }
                ")
                .Build();

            // Act and assert
            VerifyFix(source, CreateMessageForField("f"));
        }

        [Fact]
        public void When_field_is_nullable_reference_type_it_must_remove_attribute()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .InGlobalScope(@"
                    class C
                    {
                        [-[CanBeNull]
                        -]string? [|f|];
                    }
                ")
                .Build();

            // Act and assert
            VerifyFix(source, CreateMessageForField("f"));
        }

        [Fact]
        public void When_field_is_collection_of_value_type_it_must_be_reported_and_converted()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .Using(typeof(IEnumerable<>).Namespace)
                .InGlobalScope(@"
                    class C
                    {
                        [-[ItemCanBeNull] [CanBeNull]
                        -]IEnumerable<int>[+?+] [|f|];
                    }
                ")
                .Build();

            // Act and assert
            VerifyFix(source, CreateMessageForField("f"));
        }

        [Fact]
        public void When_field_is_collection_of_nullable_reference_type_it_must_remove_attribute()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .Using(typeof(IList<>).Namespace)
                .InGlobalScope(@"
                    class C
                    {
                        [-[ItemCanBeNull]
                        -]IList<string?> [|f|];
                    }
                ")
                .Build();

            // Act and assert
            VerifyFix(source, CreateMessageForField("f"));
        }

        [Fact]
        public void When_declaring_partial_method_is_annotated_with_CanBeNull_it_must_be_reported_and_converted()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .InGlobalScope(@"
                    partial class C
                    {
                        partial void M([-[CanBeNull]-] string[+?+] [|p1|]);
                    }

                    partial class C
                    {
                        partial void M(string[+?+] p2)
                        {
                            throw new NotImplementedException();
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyFix(source, CreateMessageForParameter("p1"));
        }

        [Fact]
        public void When_implementing_partial_method_is_annotated_with_CanBeNull_it_must_be_reported_and_converted()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .InGlobalScope(@"
                    partial class C
                    {
                        partial void M(string[+?+] p1);
                    }

                    partial class C
                    {
                        partial void M([-[CanBeNull]-] string[+?+] [|p2|])
                        {
                            throw new NotImplementedException();
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyFix(source, CreateMessageForParameter("p2"));
        }

        [Fact]
        public void When_partial_method_has_conflicting_annotations_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .InGlobalScope(@"
                    partial class C
                    {
                        partial void M([CanBeNull] string p1);
                    }

                    partial class C
                    {
                        partial void M([NotNull] string p2)
                        {
                            throw new NotImplementedException();
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyDiagnostics(source);
        }

        [Fact]
        public void When_method_and_parameters_are_annotated_with_NotNull_it_must_remove_attributes()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .InGlobalScope(@"
                    class C
                    {
                        [-[NotNull]
                        -]string [|M|]([NotNull] string [|p1|], [NotNull] string [|p2|])
                        {
                            throw new NotImplementedException();
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyFix(source,
                CreateMessageForMethod("M"),
                CreateMessageForParameter("p1"),
                CreateMessageForParameter("p2"));
        }

        [Fact]
        public void When_method_and_parameters_are_annotated_with_ItemCanBeNull_it_must_be_reported_and_converted()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .Using(typeof(IList<>).Namespace)
                .InGlobalScope(@"
                    class C
                    {
                        [-[ItemCanBeNull]
                        -]IList<string[+?+]> [|M|]([ItemCanBeNull] IList<string> [|p1|], [ItemCanBeNull] IList<string> [|p2|])
                        {
                            throw new NotImplementedException();
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyFix(source,
                CreateMessageForMethod("M"),
                CreateMessageForParameter("p1"),
                CreateMessageForParameter("p2"));
        }

        [Fact]
        public void When_delegate_and_parameters_are_annotated_it_must_be_reported_and_converted()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .Using(typeof(IList<>).Namespace)
                .InGlobalScope(@"
                    [-[CanBeNull] [ItemCanBeNull]
                    -]public delegate IList<string[+?+]>[+?+] [|D|]([CanBeNull] [ItemCanBeNull] IList<string> [|p1|], [CanBeNull] [ItemCanBeNull] IList<string> [|p2|]);
                ")
                .Build();

            // Act and assert
            VerifyFix(source,
                CreateMessageForDelegate("D"),
                CreateMessageForParameter("p1"),
                CreateMessageForParameter("p2"));
        }

        [Fact]
        public void When_interface_parameter_is_annotated_the_type_hierarchy_must_be_converted()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .InGlobalScope(@"
                    interface I
                    {
                        void M([-[CanBeNull]-] string[+?+] [|p|]);
                    }

                    class B : I
                    {
                        public virtual void M(string[+?+] p)
                        {
                        }
                    }

                    class D : B
                    {
                        public override void M(string[+?+] p)
                        {
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void When_interface_parameter_is_annotated_with_overrides_the_type_hierarchy_must_be_converted()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .Using(typeof(IList<>).Namespace)
                .InGlobalScope(@"
                    public interface IFace
                    {
                        void M([-[NotNull] [ItemCanBeNull]-] List<string[+?+]> [|p1|]);
                    }

                    public class Base : IFace
                    {
                        public virtual void M([-[CanBeNull] [ItemNotNull]-] List<string>[+?+] [|p2|])
                        {
                        }
                    }

                    public class Derived : Base
                    {
                        public override void M([-[NotNull] [ItemCanBeNull]-] List<string[+?+]> [|p3|])
                        {
                        }
                    }

                    public class Top : Derived
                    {
                        public override void M([-[CanBeNull] [ItemCanBeNull]-] List<string[+?+]>[+?+] [|p4|])
                        {
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyFix(source,
                CreateMessageForParameter("p1"),
                CreateMessageForParameter("p2"),
                CreateMessageForParameter("p3"),
                CreateMessageForParameter("p4"));
        }

        [Fact]
        public void When_interface_parameter_is_implemented_implicitly_and_explicitly_it_must_be_reported_and_converted()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .InGlobalScope(@"
                    interface I
                    {
                        void M([-[CanBeNull]-] string[+?+] [|p|]);
                    }

                    class C : I
                    {
                        public void M(string p)
                        {
                        }

                        void I.M(string[+?+] p)
                        {
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void When_base_property_is_annotated_the_type_hierarchy_must_be_converted()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .InGlobalScope(@"
                    class B
                    {
                        [-[CanBeNull]
                        -]public virtual string[+?+] [|P|] { get; set; }
                    }

                    class D : B
                    {
                        public override string[+?+] P
                        {
                            get
                            {
                                throw new NotImplementedException();
                            }
                            set
                            {
                                throw new NotImplementedException();
                            }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyFix(source, CreateMessageForProperty("P"));
        }

        [Fact]
        public void When_base_indexer_parameter_is_annotated_the_type_hierarchy_must_be_converted()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .WithNullableReferenceTypesEnabled()
                .InGlobalScope(@"
                    abstract class B
                    {
                        public abstract int this[[-[CanBeNull]-]string[+?+] [|index|]] { get; set; }
                    }

                    class D : B
                    {
                        public override int this[string[+?+] index]
                        {
                            get
                            {
                                throw new NotImplementedException();
                            }
                            set
                            {
                                throw new NotImplementedException();
                            }
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyFix(source, CreateMessageForParameter("index"));
        }
    }
}
#endif
