namespace steam_retriever;
internal class UGCFileInfo
{
    internal string FileName { get; private set; }
    internal string Path { get; private set; }

    internal UGCFileInfo(string filenName, string path)
    {
        this.FileName = filenName;
        this.Path = path;
    }
}
