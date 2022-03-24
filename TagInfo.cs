internal class TagInfo
{
    public string Tag { get; set; }
    public string Level { get; set; }
    public float Frequency { get; set; }

    public TagInfo(string tag)
    {
        Tag = tag;
    }
}