using System.Text;
namespace Alextor.RAG.Extractor.Test;

/// <summary>
/// This test does not verify the accuracy of the extraction,
/// but checks whether it can extract any content from the file and if it can recognize the file type.
/// </summary>
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
        stream.Dispose();
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
        stream.Dispose();
        var expected = "Best.\nSummer.\nEver.";

        Assert.True(content.Content == expected, string.Format("Content is \n\"{0}\"\n\nbut got \n\"{1}\"", expected, content.Content));
        Assert.True(content.FileType == FileType.JPG, "");
    }

    [Fact]
    public void Test4()
    {
        var path = Path.Join(AppContext.BaseDirectory, "Files", "pdf1.pdf");

        var stream = File.OpenRead(path);

        var content = Parser.Parse(stream);
        stream.Dispose();
        // var expected = "";

#if DEBUG
        var bdir = Path.Join(AppContext.BaseDirectory, "out");
        Directory.CreateDirectory(bdir);
        var fpath = Path.Join(bdir, "test4.md");
        if (File.Exists(fpath))
        {
            File.Delete(fpath);
        }
        var writer = File.OpenWrite(fpath);

        var bytes = Encoding.UTF8.GetBytes(content.Content);
        writer.Write(bytes);
        writer.Dispose();
#endif

        Console.WriteLine(content);
        // Assert.True(content.Content.Length > 0, string.Format("Content is \n\"{0}\"\n\nbut got \n\"{1}\"", expected, content.Content));
        Assert.True(content.Content.Length > 0, "The content length must be greater than 0");
        Assert.True(content.FileType == FileType.PDF, "");
    }

    [Fact]
    public void Test5()
    {
        var path = Path.Join(AppContext.BaseDirectory, "Files", "doc1.docx");

        var stream = File.OpenRead(path);

        var content = Parser.Parse(stream);
        stream.Dispose();
        // var expected = "";

#if DEBUG
        var bdir = Path.Join(AppContext.BaseDirectory, "out");
        Directory.CreateDirectory(bdir);
        var fpath = Path.Join(bdir, "test5.md");
        if (File.Exists(fpath))
        {
            File.Delete(fpath);
        }
        var writer = File.OpenWrite(fpath);

        var bytes = Encoding.UTF8.GetBytes(content.Content);
        writer.Write(bytes);
        writer.Dispose();
#endif

        Console.WriteLine(content);
        Assert.True(content.Content.Length > 0, "The content length must be greater than 0");
        Assert.True(content.FileType == FileType.DOCX, "");
    }

    [Fact]
    public void Test6()
    {
        var path = Path.Join(AppContext.BaseDirectory, "Files", "xlsx1.xlsx");

        var stream = File.OpenRead(path);

        var content = Parser.Parse(stream);
        stream.Dispose();
        // var expected = "";

#if DEBUG
        var bdir = Path.Join(AppContext.BaseDirectory, "out");
        Directory.CreateDirectory(bdir);
        var fpath = Path.Join(bdir, "test6.md");
        if (File.Exists(fpath))
        {
            File.Delete(fpath);
        }
        var writer = File.OpenWrite(fpath);

        var bytes = Encoding.UTF8.GetBytes(content.Content);
        writer.Write(bytes);
        writer.Dispose();
#endif

        Console.WriteLine(content);
        Assert.True(content.Content.Length > 0, "The content length must be greater than 0");
        Assert.True(content.FileType == FileType.XLSX, "");
    }

    [Fact]
    public void Test7()
    {
        var path = Path.Join(AppContext.BaseDirectory, "Files", "ppt1.pptx");

        var stream = File.OpenRead(path);

        var content = Parser.Parse(stream);
        stream.Dispose();
        // var expected = "";

#if DEBUG
        var bdir = Path.Join(AppContext.BaseDirectory, "out");
        Directory.CreateDirectory(bdir);
        var fpath = Path.Join(bdir, "test7.md");
        if (File.Exists(fpath))
        {
            File.Delete(fpath);
        }
        var writer = File.OpenWrite(fpath);

        var bytes = Encoding.UTF8.GetBytes(content.Content);
        writer.Write(bytes);
        writer.Dispose();
#endif

        Console.WriteLine(content);
        Assert.True(content.Content.Length > 0, "The content length must be greater than 0");
        Assert.True(content.FileType == FileType.PPTX, "");
    }
 
}
