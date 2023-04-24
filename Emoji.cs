class Emoji
{
    public string emojiId { get; set; }
    public List<string> shortcuts { get; set; }
    public List<string> searchTerms { get; set; }
    public string iconURL { get; set; }
    public bool isCustomEmoji { get; set; }

    public override string ToString()
    {
        return "Emoji {emojiID=" + emojiId + ", shortcuts=" + shortcuts + ", searchTerms=" + searchTerms + ", iconURL=" + iconURL + ", isCustomEmoji=" + isCustomEmoji + "}";
    }
}