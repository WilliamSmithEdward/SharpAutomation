namespace SharpAutomation.Tests;

public class ToHtmlTests
{
    private static int Count(string text, string part) => text.Split(part).Length - 1;

    [Fact]
    public void The_message_is_HTML_encoded()
    {
        string html = new InvalidOperationException("<img src=x onerror=alert(1)> & \"quoted\"").ToHTML();

        Assert.DoesNotContain("<img", html);
        Assert.Contains("&lt;img src=x onerror=alert(1)&gt; &amp; &quot;quoted&quot;", html);
    }

    [Fact]
    public void The_stack_trace_is_HTML_encoded()
    {
        // A frame inside a lambda is named like "<>c.<Method>b__0_0", with angle brackets.
        Func<Exception> fromLambda = () =>
        {
            try { throw new InvalidOperationException("boom"); }
            catch (Exception caught) { return caught; }
        };
        var thrown = fromLambda();
        Assert.Contains("<", thrown.StackTrace);

        string html = new List<Exception> { thrown }.ToHTML();

        Assert.Contains(System.Net.WebUtility.HtmlEncode(thrown.StackTrace!), html);
        Assert.DoesNotContain(thrown.StackTrace!, html);
    }

    [Fact]
    public void Every_table_row_is_opened_and_closed_once()
    {
        string html = new List<Exception> { new("one"), new("two") }.ToHTML();

        Assert.Equal(Count(html, "<tr"), Count(html, "</tr>"));
        Assert.Equal(Count(html, "<td"), Count(html, "</td>"));
        Assert.Equal(14, Count(html, "<tr"));
    }

    [Fact]
    public void Plain_text_is_unchanged()
    {
        string html = new IOException("The disk is full").ToHTML();

        Assert.Contains(">The disk is full<", html);
        Assert.Contains(">System.IO.IOException<", html);
    }

    [Fact]
    public async Task A_null_list_or_exception_throws_ArgumentNullException_naming_it()
    {
        List<Exception> list = null!;
        Exception exception = null!;

        Assert.Equal("exceptions", Assert.Throws<ArgumentNullException>(() => list.ToHTML()).ParamName);
        Assert.Equal("exception", Assert.Throws<ArgumentNullException>(() => exception.ToHTML()).ParamName);
        Assert.Equal("exception", Assert.Throws<ArgumentNullException>(() => exception.ToJSON()).ParamName);
        Assert.Equal("exception", (await Assert.ThrowsAsync<ArgumentNullException>(() => exception.ToLogAsync("unused.log"))).ParamName);
    }
}
