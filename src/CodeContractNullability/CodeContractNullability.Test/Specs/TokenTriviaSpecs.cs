using CodeContractNullability.Test.TestDataBuilders;
using NUnit.Framework;

namespace CodeContractNullability.Test.Specs
{
    /// <summary>
    /// Tests concerning whitespace and comment preservation when fixes are applied.
    /// </summary>
    [TestFixture]
    internal class TokenTriviaSpecs : NullabilityNUnitRoslynTest
    {
        [Test]
        public void When_field_has_singleline_comments_they_must_be_preserved()
        {
            // Arrange
            string code = @"public class T
{
    // before
    <annotate/>
    int? [|f|]; // on same line
    // after
}
" + RawSourceCodeBuilder.PublicGlobalNullabilityAttributes;

            ParsedSourceCode source = new RawSourceCodeBuilder()
                .Exactly(RemoveLinesWithAnnotation(code), code)
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Test]
        public void When_field_has_multiline_comments_they_must_be_preserved()
        {
            // Arrange
            string code = @"public class T
{
    /* line before */
    <annotate/>
    int? /* intermediate */ [|f|] /* after */; /* line end */
    /* line after */
}
" + RawSourceCodeBuilder.PublicGlobalNullabilityAttributes;

            ParsedSourceCode source = new RawSourceCodeBuilder()
                .Exactly(RemoveLinesWithAnnotation(code), code)
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Test]
        public void When_property_has_singleline_comments_they_must_be_preserved()
        {
            // Arrange
            string code = @"public class T
{
    // before
    <annotate/>
    public int? [|P|] { get; set; } // on same line
    // after
}
" + RawSourceCodeBuilder.PublicGlobalNullabilityAttributes;

            ParsedSourceCode source = new RawSourceCodeBuilder()
                .Exactly(RemoveLinesWithAnnotation(code), code)
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Test]
        public void When_property_has_multiline_comments_they_must_be_preserved()
        {
            // Arrange
            string code = @"public class T
{
    /* line before */
    <annotate/>
    public int? /* before */ [|P|] /* after */ { get; set; } /* line end */
    /* line after */
}
" + RawSourceCodeBuilder.PublicGlobalNullabilityAttributes;

            ParsedSourceCode source = new RawSourceCodeBuilder()
                .Exactly(RemoveLinesWithAnnotation(code), code)
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Test]
        public void When_indexer_result_has_singleline_comments_they_must_be_preserved()
        {
            // Arrange
            string code = @"public class T
{
    // before
    <annotate/>
    public int? [|this|][byte offset] // on same line
    // after
    {
        get { throw new System.NotImplementedException(); }
        set { throw new System.NotImplementedException(); }
    }
}
" + RawSourceCodeBuilder.PublicGlobalNullabilityAttributes;

            ParsedSourceCode source = new RawSourceCodeBuilder()
                .Exactly(RemoveLinesWithAnnotation(code), code)
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Test]
        public void When_indexer_result_has_multiline_comments_they_must_be_preserved()
        {
            // Arrange
            string code = @"public class T
{
    /* line before */
    <annotate/>
    public int? /* before */ [|this|] /* after */ [byte offset] /* line end */
    /* line after */
    {
        get { throw new System.NotImplementedException(); }
        set { throw new System.NotImplementedException(); }
    }
}
" + RawSourceCodeBuilder.PublicGlobalNullabilityAttributes;

            ParsedSourceCode source = new RawSourceCodeBuilder()
                .Exactly(RemoveLinesWithAnnotation(code), code)
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Test]
        public void When_indexer_parameter_has_singleline_comments_they_must_be_preserved()
        {
            // Arrange
            string code = @"public class T
{
    // before
    public byte this[<annotate/> int? [|offset|]] // on same line
    // after
    {
        get { throw new System.NotImplementedException(); }
        set { throw new System.NotImplementedException(); }
    }
}
" + RawSourceCodeBuilder.PublicGlobalNullabilityAttributes;

            ParsedSourceCode source = new RawSourceCodeBuilder()
                .Exactly(code, code)
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Test]
        public void When_indexer_parameter_has_multiline_comments_they_must_be_preserved()
        {
            // Arrange
            string code = @"public class T
{
    /* line before */
    public byte this[/* before */<annotate/>int? /* intermediate */ [|offset|] /* after */ ] /* line end */
    /* line after */
    {
        get { throw new System.NotImplementedException(); }
        set { throw new System.NotImplementedException(); }
    }
}
" + RawSourceCodeBuilder.PublicGlobalNullabilityAttributes;

            ParsedSourceCode source = new RawSourceCodeBuilder()
                .Exactly(code, code.Replace("<annotate/>", "<annotate/> "))
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Test]
        public void When_return_value_has_singleline_comments_they_must_be_preserved()
        {
            // Arrange
            string code = @"public class T
{
    // before
    <annotate/>
    int? [|M|]() { throw new System.NotImplementedException(); } // on same line
    // after
}
" + RawSourceCodeBuilder.PublicGlobalNullabilityAttributes;

            ParsedSourceCode source = new RawSourceCodeBuilder()
                .Exactly(RemoveLinesWithAnnotation(code), code)
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Test]
        public void When_return_value_has_multiline_comments_they_must_be_preserved()
        {
            // Arrange
            string code = @"public class T
{
    /* line before */
    <annotate/>
    int? /* intermediate */ [|M|]/* after */() { throw new System.NotImplementedException(); } /* line end */
    /* line after */
}
" + RawSourceCodeBuilder.PublicGlobalNullabilityAttributes;

            ParsedSourceCode source = new RawSourceCodeBuilder()
                .Exactly(RemoveLinesWithAnnotation(code), code)
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Test]
        public void When_method_parameter_has_singleline_comments_they_must_be_preserved()
        {
            // Arrange
            string code = @"public class T
{
    // before
    void M(<annotate/> int? [|p|]) { } // on same line
    // after
}
" + RawSourceCodeBuilder.PublicGlobalNullabilityAttributes;

            ParsedSourceCode source = new RawSourceCodeBuilder()
                .Exactly(code, code)
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }

        [Test]
        public void When_method_parameter_has_multiline_comments_they_must_be_preserved()
        {
            // Arrange
            string code = @"public class T
{
    /* line before */
    void M(/* before */<annotate/>int? /* intermediate */ [|p|] /* after */) { } /* line end */
    /* line after */
}
" + RawSourceCodeBuilder.PublicGlobalNullabilityAttributes;

            ParsedSourceCode source = new RawSourceCodeBuilder()
                .Exactly(code, code.Replace("<annotate/>", "<annotate/> "))
                .Build();

            // Act and assert
            VerifyNullabilityFix(source);
        }
    }
}