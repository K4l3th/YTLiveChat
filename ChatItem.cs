class ChatItem
{
    public ChatItemType type { get; set; }
    public string authorName { get; set; }
    public string authorChannelID { get; set; }
    public string message{ get; set; }
    public List<Object> messageExtended { get; set; }
    public string authorIconURL { get; set; }
    public string id { get; set; }
    public long timestamp { get; set; }
    public List<AuthorType> authorType { get; set; }
    public string memberBadgeIconURL { get; set; }
    public int bodyBackgroundColor { get; set; }
    public int bodyTextColor { get; set; }
    public int headerBackgroundColor { get; set; }
    public int headerTextColor { get; set; }
    public int authorNameTextColor { get; set; }
    public string purchaseAmount { get; set; }
    public string stickerIconURL;
    public int backgroundColor { get; set; }
    public int endBackgroundColor { get; set; }
    public int durationSec { get; set; }
    public int fullDurationSec { get; set; }

    public YouTubeLiveChat liveChat { get; set; }


    public ChatItem( YouTubeLiveChat liveChat )
    {
        this.authorType = new List<AuthorType>();
        this.authorType.Add( AuthorType.NORMAL );
        this.type = ChatItemType.Message;
        this.liveChat = liveChat;
    }

    public override string ToString()
    {
        return "ChatItem {type=" + type + ", authorName=" + authorName + ", authorChannelID=" + authorChannelID + ", message=" + message + ", messageExtended=" + messageExtended + ", authorIconURL=" + authorIconURL + ", id=" + id + ", timestamp=" + timestamp + ", authorType=" + authorType + ", memberBadgeIconURL=" + memberBadgeIconURL + ", bodyBackgroundColor=" + bodyBackgroundColor + ", bodyTextColor=" + bodyTextColor + ", headerBackgroundColor=" + headerBackgroundColor + ", headerTextColor=" + headerTextColor + ", authorNameTextColor=" + authorNameTextColor + ", purchaseAmount=" + purchaseAmount + ", stickerIconURL=" + stickerIconURL + ", backgroundColor=" + backgroundColor + ", endBackgroundColor=" + endBackgroundColor + ", durationSec=" + durationSec + ", fullDurationSec=" + fullDurationSec + "}";
    }

    

}