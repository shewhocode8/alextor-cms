using Lib.Extractor;
namespace LibExtractor.Test;

public class Test
{
    [Fact]
    public void Test1()
    {
        var path = Path.Join(AppContext.BaseDirectory, "Files", "sample_text.txt");

        var stream = File.OpenRead(path);
        var orig_content = File.ReadAllText(path);

        var content = Parser.Parse(stream);
        Assert.True(content.Content == orig_content, "Content should be equal to file content");
        Assert.True(content.FileType == FileType.Txt, "FileType should be FileType.Txt");
    }

    [Fact]
    public void Test2()
    {
        var path = Path.Join(AppContext.BaseDirectory, "Files", "img1.png");

        var stream = File.OpenRead(path);

        var content = Parser.Parse(stream);
        var expected =
@"HOW TO COMBINE
TEXT AND IMAGE
IN ELEARNING DESIGN";

        Assert.True(content.Content == expected, string.Format("Content is \n\"{0}\"\n\nbut got \n\"{1}\"", expected, content.Content));
        Assert.True(content.FileType == FileType.PNG, "");
    }

    [Fact]
    public void Test3()
    {
        var path = Path.Join(AppContext.BaseDirectory, "Files", "img2.jpeg");

        var stream = File.OpenRead(path);

        var content = Parser.Parse(stream);
        var expected = "Best.\nSummer.\nEver.";

        Assert.True(content.Content == expected, string.Format("Content is \n\"{0}\"\n\nbut got \n\"{1}\"", expected, content.Content));
        Assert.True(content.FileType == FileType.JPG, "");
    }
}
