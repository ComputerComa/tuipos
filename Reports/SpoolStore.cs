using System.Text.RegularExpressions;

namespace tuipos.Reports;

public record SpooledFile(
    int Number,
    string Name,
    string UserId,
    int Pages,
    DateTime Created,
    string Path);

// Saved report output, like an IBM i output queue. Each spooled file is a
// plain text file in the spool folder named "000001-DLYSLS-0000.txt"
// (number, report, user), ready to send raw to the printer.
public partial class SpoolStore
{
    public SpoolStore(string folder)
    {
        Folder = Path.GetFullPath(folder);
    }

    public string Folder { get; }

    public SpooledFile Save(Report report, string userId)
    {
        Directory.CreateDirectory(Folder);

        int number = List().Select(f => f.Number).DefaultIfEmpty(0).Max() + 1;

        // User IDs can hold characters a file name can't.
        string user = string.Concat(userId.Select(c =>
            Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));

        string path = Path.Combine(Folder, $"{number:D6}-{report.Id}-{user}.txt");
        File.WriteAllText(path, report.Text);

        return Read(path)!;
    }

    // Oldest first, like WRKSPLF.
    public List<SpooledFile> List()
    {
        if (!Directory.Exists(Folder))
        {
            return new List<SpooledFile>();
        }

        return Directory.EnumerateFiles(Folder, "*.txt")
            .Select(Read)
            .OfType<SpooledFile>()
            .OrderBy(f => f.Number)
            .ToList();
    }

    public void Delete(SpooledFile file)
    {
        File.Delete(file.Path);
    }

    private static SpooledFile? Read(string path)
    {
        var match = FileNamePattern().Match(Path.GetFileName(path));

        if (!match.Success)
        {
            return null;
        }

        // Every page ends with a form feed.
        int pages = File.ReadAllText(path).Count(c => c == '\f');

        return new SpooledFile(
            int.Parse(match.Groups[1].Value),
            match.Groups[2].Value,
            match.Groups[3].Value,
            pages,
            File.GetLastWriteTime(path),
            path);
    }

    [GeneratedRegex(@"^(\d{6})-([A-Z0-9]+)-(.+)\.txt$")]
    private static partial Regex FileNamePattern();
}
