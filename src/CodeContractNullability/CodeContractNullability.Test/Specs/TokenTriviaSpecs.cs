using CodeContractNullability.Test.TestDataBuilders;
using Xunit;

namespace CodeContractNullability.Test.Specs
{
    /// <summary>
    /// Tests concerning whitespace and comment preservation when fixes are applied.
    /// </summary>
    public sealed class TokenTriviaSpecs : NullabilityTest
    {
        [Fact]
        public void When_field_has_single_line_comments_they_must_be_preserved()
        {
            // Arrange
            string code = @"public class T
{
    // before[+
    NullabilityAttributePlaceholder+]
    int? [|f|]; // on same line
    // after
}
" + ExactSourceCodeBuilder.PublicGlobalNullabilityAttributes;

            ParsedSourceCode source = new ExactSourceCodeBuilder()
                .Exactly(code)
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForField("f"));
        }

        [Fact]
        public void When_field_has_multiline_comments_they_must_be_preserved()
        {
            // Arrange
            string code = @"public class T
{
    /* line before */[+
    NullabilityAttributePlaceholder+]
    int? /* intermediate */ [|f|] /* after */; /* line end */
    /* line after */
}
" + ExactSourceCodeBuilder.PublicGlobalNullabilityAttributes;

            ParsedSourceCode source = new ExactSourceCodeBuilder()
                .Exactly(code)
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForField("f"));
        }

        [Fact]
        public void When_property_has_single_line_comments_they_must_be_preserved()
        {
            // Arrange
            string code = @"public class T
{
    // before[+
    NullabilityAttributePlaceholder+]
    public int? [|P|] { get; set; } // on same line
    // after
}
" + ExactSourceCodeBuilder.PublicGlobalNullabilityAttributes;

            ParsedSourceCode source = new ExactSourceCodeBuilder()
                .Exactly(code)
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForProperty("P"));
        }

        [Fact]
        public void When_property_has_multiline_comments_they_must_be_preserved()
        {
            // Arrange
            string code = @"public class T
{
    /* line before */[+
    NullabilityAttributePlaceholder+]
    public int? /* before */ [|P|] /* after */ { get; set; } /* line end */
    /* line after */
}
" + ExactSourceCodeBuilder.PublicGlobalNullabilityAttributes;

            ParsedSourceCode source = new ExactSourceCodeBuilder()
                .Exactly(code)
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForProperty("P"));
        }

        [Fact]
        public void When_indexer_result_has_single_line_comments_they_must_be_preserved()
        {
            // Arrange
            string code = @"public class T
{
    // before[+
    NullabilityAttributePlaceholder+]
    public int? [|this|][byte offset] // on same line
    // after
    {
        get { throw new System.NotImplementedException(); }
        set { throw new System.NotImplementedException(); }
    }
}
" + ExactSourceCodeBuilder.PublicGlobalNullabilityAttributes;

            ParsedSourceCode source = new ExactSourceCodeBuilder()
                .Exactly(code)
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForProperty("this[]"));
        }

        [Fact]
        public void When_indexer_result_has_multiline_comments_they_must_be_preserved()
        {
            // Arrange
            string code = @"public class T
{
    /* line before */[+
    NullabilityAttributePlaceholder+]
    public int? /* before */ [|this|] /* after */ [byte offset] /* line end */
    /* line after */
    {
        get { throw new System.NotImplementedException(); }
        set { throw new System.NotImplementedException(); }
    }
}
" + ExactSourceCodeBuilder.PublicGlobalNullabilityAttributes;

            ParsedSourceCode source = new ExactSourceCodeBuilder()
                .Exactly(code)
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForProperty("this[]"));
        }

        [Fact]
        public void When_indexer_parameter_has_single_line_comments_they_must_be_preserved()
        {
            // Arrange
            string code = @"public class T
{
    // before
    public byte this[[+NullabilityAttributePlaceholder+] int? [|offset|]] // on same line
    // after
    {
        get { throw new System.NotImplementedException(); }
        set { throw new System.NotImplementedException(); }
    }
}
" + ExactSourceCodeBuilder.PublicGlobalNullabilityAttributes;

            ParsedSourceCode source = new ExactSourceCodeBuilder()
                .Exactly(code)
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("offset"));
        }

        [Fact]
        public void When_indexer_parameter_has_multiline_comments_they_must_be_preserved()
        {
            // Arrange
            string code = @"public class T
{
    /* line before */
    public byte this[/* before */[+NullabilityAttributePlaceholder +]int? /* intermediate */ [|offset|] /* after */ ] /* line end */
    /* line after */
    {
        get { throw new System.NotImplementedException(); }
        set { throw new System.NotImplementedException(); }
    }
}
" + ExactSourceCodeBuilder.PublicGlobalNullabilityAttributes;

            ParsedSourceCode source = new ExactSourceCodeBuilder()
                .Exactly(code)
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("offset"));
        }

        [Fact]
        public void When_return_value_has_single_line_comments_they_must_be_preserved()
        {
            // Arrange
            string code = @"public class T
{
    // before[+
    NullabilityAttributePlaceholder+]
    int? [|M|]() { throw new System.NotImplementedException(); } // on same line
    // after
}
" + ExactSourceCodeBuilder.PublicGlobalNullabilityAttributes;

            ParsedSourceCode source = new ExactSourceCodeBuilder()
                .Exactly(code)
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForMethod("M"));
        }

        [Fact]
        public void When_return_value_has_multiline_comments_they_must_be_preserved()
        {
            // Arrange
            string code = @"public class T
{
    /* line before */[+
    NullabilityAttributePlaceholder+]
    int? /* intermediate */ [|M|]/* after */() { throw new System.NotImplementedException(); } /* line end */
    /* line after */
}
" + ExactSourceCodeBuilder.PublicGlobalNullabilityAttributes;

            ParsedSourceCode source = new ExactSourceCodeBuilder()
                .Exactly(code)
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForMethod("M"));
        }

        [Fact]
        public void When_method_parameter_has_single_line_comments_they_must_be_preserved()
        {
            // Arrange
            string code = @"public class T
{
    // before
    void M([+NullabilityAttributePlaceholder+] int? [|p|]) { } // on same line
    // after
}
" + ExactSourceCodeBuilder.PublicGlobalNullabilityAttributes;

            ParsedSourceCode source = new ExactSourceCodeBuilder()
                .Exactly(code)
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }

        [Fact]
        public void When_method_parameter_has_multiline_comments_they_must_be_preserved()
        {
            // Arrange
            string code = @"public class T
{
    /* line before */
    void M(/* before */[+NullabilityAttributePlaceholder +]int? /* intermediate */ [|p|] /* after */) { } /* line end */
    /* line after */
}
" + ExactSourceCodeBuilder.PublicGlobalNullabilityAttributes;

            ParsedSourceCode source = new ExactSourceCodeBuilder()
                .Exactly(code)
                .Build();

            // Act and assert
            VerifyNullabilityFix(source, CreateMessageForParameter("p"));
        }
    }
}
