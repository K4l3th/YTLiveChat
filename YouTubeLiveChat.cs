using System.Globalization;
using System.Text;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

class YouTubeLiveChat
{
    public static string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.102 Safari/537.36,gzip(gfe)";

    private static string liveChatApi = "https://www.youtube.com/youtubei/v1/live_chat/get_live_chat?key="; // view live chat
    private static string liveStreamInfoApi = "https://www.youtube.com/watch?v="; // stream info


    private string videoId {get; set;}
    private string channelId {get; set;}
    private string continuation {get; set;}
    private bool isTopChatOnly {get; set;}
    private string visitorData {get; set;}
    private ChatItem bannerItem {get; set;}
    public List<ChatItem> chatItems {get; set;}
    private List<ChatItem> chatItemTickerPaidMessages {get; set;}
    private List<ChatItemDelete> chatItemDeletes {get; set;}
    private CultureInfo locale { get; set; }
    private string clientVersion {get; set;}
    private bool isInitDataAvailable {get; set;}
    private string apiKey {get; set;}
    private string datasyncId {get; set;}
    private int commentCounter {get; set;}
    private string clientMessageId {get; set;}
    private string parameters {get; set;}

    

    public YouTubeLiveChat (string id, bool isTopChatOnly, IdType type){
        this.isTopChatOnly = isTopChatOnly;
        this.visitorData = "";
        this.chatItems = new List<ChatItem>();
        this.chatItemTickerPaidMessages = new List<ChatItem>();
        this.chatItemDeletes = new List<ChatItemDelete>();
        this.locale = CultureInfo.GetCultureInfo("en-US");
        this.commentCounter = 0;
        this.clientMessageId = Util.generateClientMessageId();

        try
        {
            this.getInitialData(id, type);
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public void reset(){
        this.visitorData = "";
        this.chatItems.Clear();
        this.chatItemTickerPaidMessages.Clear();
        this.commentCounter = 0;
        this.clientMessageId = Util.generateClientMessageId();

        try
        {
            this.getInitialData(this.videoId, IdType.VIDEO);
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public void update(){
        update(0);
    }

    public void update(long offsetInMs){
        if (this.isInitDataAvailable) {
            this.isInitDataAvailable = false;
            return;
        }
        this.chatItems.Clear();
        this.chatItemTickerPaidMessages.Clear();
        this.chatItemDeletes.Clear();
        try {
            //Get live actions
            if (this.continuation == null) {
                throw new IOException("continuation is null! Please call reset().");
            }
            String pageContent = Util.getPageContentWithJson(liveChatApi + this.apiKey, this.getPayload(offsetInMs), this.getHeader());
            Dictionary<string, Object> json = Util.toJSON(pageContent);
            if (this.visitorData == null || this.visitorData.Length == 0) {
                this.visitorData = Util.getJSONValueString(Util.getJSONMap(json, new string[] {"responseContext"}), "visitorData");
            }
            //Get clientVersion
            List<Object> serviceTrackingParams = Util.getJSONList(json, "serviceTrackingParams", new string[] {"responseContext"});
            if (serviceTrackingParams != null) {
                foreach (Object ser in serviceTrackingParams) {
                    Dictionary<string, Object> service = (Dictionary<string, Object>) ser;
                    string serviceName = Util.getJSONValueString(service, "service");
                    if (serviceName != null && serviceName.Equals("CSI")) {
                        List<Object> parameters = Util.getJSONList(service, "params", new string[] {});
                        if (parameters != null) {
                            foreach (Object par in parameters) {
                                Dictionary<string, Object> param = (Dictionary<string, Object>) par;
                                string key = Util.getJSONValueString(param, "key");
                                if (key != null && key.Equals("cver")) {
                                    this.clientVersion = Util.getJSONValueString(param, "value");
                                }
                            }
                        }
                    }
                }
            }
            Dictionary<string, Object> liveChatContinuation = Util.getJSONMap(json, new string[] {"continuationContents", "liveChatContinuation"});
            if (liveChatContinuation != null) {
                List<Object> actions = Util.getJSONList(liveChatContinuation, "actions", new string[] {});
                if (actions != null) {
                    this.parseActions(actions);
                }
                List<Object> continuations = Util.getJSONList(liveChatContinuation, "continuations", new string[] {});
                if (continuations != null) {
                    foreach (Object co in continuations) {
                        Dictionary<string, Object> continuation = (Dictionary<string, Object>) co;
                        this.continuation = Util.getJSONValueString(Util.getJSONMap(continuation, new string[] {"invalidationContinuationData"}), "continuation");
                        if (this.continuation == null) {
                            this.continuation = Util.getJSONValueString(Util.getJSONMap(continuation, new string[] {"timedContinuationData"}), "continuation");
                        }
                        if (this.continuation == null) {
                            this.continuation = Util.getJSONValueString(Util.getJSONMap(continuation, new string[] {"reloadContinuationData"}), "continuation");
                        }
                    }
                }
            }
        } catch (IOException exception) {
            throw new IOException("Can't get youtube live chat!", exception);
        }
    }
    

    public void parseActions(List<Object> json) {
        foreach (Object i in json) {
            Dictionary<string, Object> actions = (Dictionary<string, Object>) i;
            Dictionary<string, Object> addChatItemAction = Util.getJSONMap(actions, new string[] {"addChatItemAction"});
            //For replay
            if (addChatItemAction == null) {
                Dictionary<string, Object> replayChatItemAction = Util.getJSONMap(actions, new string[] {"replayChatItemAction"});
                if (replayChatItemAction != null) {
                    List<Object> acts = Util.getJSONList(replayChatItemAction, "actions", new string[] {});
                    if (acts != null) {
                        parseActions(acts);
                    }
                }
            }
            if (addChatItemAction != null) {
                ChatItem chatItem = null;
                Dictionary<string, Object> item = Util.getJSONMap(addChatItemAction, new string[] {"item"});
                if (item != null) {
                    chatItem = new ChatItem(this);
                    this.parseChatItem(chatItem, item);
                }
                if (chatItem != null && chatItem.id != null) {
                    this.chatItems.Add(chatItem);
                }
            }
            //Pinned message
            Dictionary<string, Object> contents = Util.getJSONMap(actions, new string[] {"addBannerToLiveChatCommand", "bannerRenderer", "liveChatBannerRenderer", "contents"});
            if (contents != null) {
                ChatItem chatItem = new ChatItem(this);
                this.parseChatItem(chatItem, contents);
                this.bannerItem = chatItem;
            }
            Dictionary<string, Object> markChatItemAsDeletedAction = Util.getJSONMap(actions, new string[] {"markChatItemAsDeletedAction"});
            if (markChatItemAsDeletedAction != null) {
                ChatItemDelete chatItemDelete = new ChatItemDelete();
                chatItemDelete.message = this.parseMessage(Util.getJSONMap(markChatItemAsDeletedAction, new string[] {"deletedStateMessage"}), new List<object>());
                chatItemDelete.targetId = Util.getJSONValueString(markChatItemAsDeletedAction, "targetItemId");
                this.chatItemDeletes.Add(chatItemDelete);
            }
        }
    }

    public void parseChatItem(ChatItem chatItem, Dictionary<string, Object> action) {
        Dictionary<string, Object> liveChatTextMessageRenderer = Util.getJSONMap(action, new string[] {"liveChatTextMessageRenderer"});
        Dictionary<string, Object> liveChatPaidMessageRenderer = Util.getJSONMap(action, new string[] {"liveChatPaidMessageRenderer"});
        Dictionary<string, Object> liveChatPaidStickerRenderer = Util.getJSONMap(action, new string[] {"liveChatPaidStickerRenderer"});
        Dictionary<string, Object> liveChatMembershipItemRenderer = Util.getJSONMap(action, new string[] {"liveChatMembershipItemRenderer"});
        if (liveChatTextMessageRenderer == null && liveChatPaidMessageRenderer != null) {
            liveChatTextMessageRenderer = liveChatPaidMessageRenderer;
        }
        if (liveChatTextMessageRenderer == null && liveChatPaidStickerRenderer != null) {
            liveChatTextMessageRenderer = liveChatPaidStickerRenderer;
        }
        if (liveChatTextMessageRenderer == null && liveChatMembershipItemRenderer != null) {
            liveChatTextMessageRenderer = liveChatMembershipItemRenderer;
        }
        if (liveChatTextMessageRenderer != null) {
            chatItem.authorName = Util.getJSONValueString(Util.getJSONMap(liveChatTextMessageRenderer, new string[] {"authorName"}), "simpleText");
            chatItem.id = Util.getJSONValueString(liveChatTextMessageRenderer, "id");
            chatItem.authorChannelID = Util.getJSONValueString(liveChatTextMessageRenderer, "authorExternalChannelId");
            Dictionary<string, Object> message = Util.getJSONMap(liveChatTextMessageRenderer, new string[] {"message"});
            chatItem.messageExtended = new List<Object>();
            chatItem.message = parseMessage(message, chatItem.messageExtended);
            List<Object> authorPhotoThumbnails = Util.getJSONList(liveChatTextMessageRenderer, "thumbnails", new string[] {"authorPhoto"});
            if (authorPhotoThumbnails != null) {
                chatItem.authorIconURL = this.getJSONThumbnailURL(authorPhotoThumbnails);
            }
            string timestampStr = Util.getJSONValueString(liveChatTextMessageRenderer, "timestampUsec");
            if (timestampStr != null) {
                chatItem.timestamp = long.Parse(timestampStr);
            }
            List<Object> authorBadges = Util.getJSONList(liveChatTextMessageRenderer, "authorBadges", new string[] {});
            if (authorBadges != null) {
                foreach (Object au in authorBadges) {
                    Dictionary<string, Object> authorBadge = (Dictionary<string, Object>) au;
                    Dictionary<string, Object> liveChatAuthorBadgeRenderer = Util.getJSONMap(authorBadge, new string[] {"liveChatAuthorBadgeRenderer"});
                    if (liveChatAuthorBadgeRenderer != null) {
                        String type = Util.getJSONValueString(Util.getJSONMap(liveChatAuthorBadgeRenderer, new string[] {"icon"}), "iconType");
                        if (type != null) {
                            switch (type) {
                                case "VERIFIED":
                                    chatItem.authorType.Add(AuthorType.VERIFIED);
                                    break;
                                case "OWNER":
                                    chatItem.authorType.Add(AuthorType.OWNER);
                                    break;
                                case "MODERATOR":
                                    chatItem.authorType.Add(AuthorType.MODERATOR);
                                    break;
                            }
                        }
                        Dictionary<string, Object> customThumbnail = Util.getJSONMap(liveChatAuthorBadgeRenderer, new string[] {"customThumbnail"});
                        if (customThumbnail != null) {
                            chatItem.authorType.Add(AuthorType.MEMBER);
                            List<Object> thumbnails = (List<Object>) customThumbnail["thumbnails"];
                            chatItem.memberBadgeIconURL = this.getJSONThumbnailURL(thumbnails);
                        }
                    }
                }
            }
        }
        if (action.ContainsKey("liveChatViewerEngagementMessageRenderer")) {
            Dictionary<string, Object> liveChatViewerEngagementMessageRenderer = (Dictionary<string, Object>) action["liveChatViewerEngagementMessageRenderer"];
            chatItem.authorName = "YouTube";
            chatItem.authorChannelID = "user/YouTube";
            chatItem.authorType.Add(AuthorType.YOUTUBE);
            chatItem.id = Util.getJSONValueString(liveChatViewerEngagementMessageRenderer, "id");
            chatItem.messageExtended = new List<object>();
            chatItem.message = this.parseMessage(Util.getJSONMap(liveChatViewerEngagementMessageRenderer, new string[] {"message"}), chatItem.messageExtended);
            string timestampStr = Util.getJSONValueString(liveChatViewerEngagementMessageRenderer, "timestampUsec");
            if (timestampStr != null) {
                chatItem.timestamp = long.Parse(timestampStr);
            }
        }
        if (liveChatPaidMessageRenderer != null) {
            chatItem.bodyBackgroundColor = Util.getJSONValueInt(liveChatPaidMessageRenderer, "bodyBackgroundColor");
            chatItem.bodyTextColor = Util.getJSONValueInt(liveChatPaidMessageRenderer, "bodyBackgroundColor");
            chatItem.headerBackgroundColor = Util.getJSONValueInt(liveChatPaidMessageRenderer, "bodyBackgroundColor");
            chatItem.headerTextColor = Util.getJSONValueInt(liveChatPaidMessageRenderer, "bodyBackgroundColor");
            chatItem.authorNameTextColor = Util.getJSONValueInt(liveChatPaidMessageRenderer, "authorNameTextColor");
            chatItem.purchaseAmount = Util.getJSONValueString(Util.getJSONMap(liveChatPaidMessageRenderer, new string[] {"purchaseAmountText"}), "simpleText");
            chatItem.type = ChatItemType.PAID_MESSAGE;
        }
        if (liveChatPaidStickerRenderer != null) {
            chatItem.backgroundColor = Util.getJSONValueInt(liveChatPaidStickerRenderer, "backgroundColor");
            chatItem.purchaseAmount = Util.getJSONValueString(Util.getJSONMap(liveChatPaidStickerRenderer, new string[] {"purchaseAmountText"}), "simpleText");
            List<Object> thumbnails = Util.getJSONList(liveChatPaidStickerRenderer, "thumbnails", new string[] {"sticker"});
            if (thumbnails != null) {
                chatItem.stickerIconURL = this.getJSONThumbnailURL(thumbnails);
            }
            chatItem.type = ChatItemType.PAID_STICKER;
        }
        Dictionary<string, Object> liveChatTickerPaidMessageItemRenderer = Util.getJSONMap(action, new string[] {"liveChatTickerPaidMessageItemRenderer"});
        if (liveChatTickerPaidMessageItemRenderer != null) {
            Dictionary<string, Object> renderer = Util.getJSONMap(liveChatPaidMessageRenderer, new string[] {"showItemEndpoint", "showLiveChatItemEndpoint", "renderer"});
            this.parseChatItem(chatItem, renderer);
            chatItem.endBackgroundColor = Util.getJSONValueInt(liveChatPaidMessageRenderer, "endBackgroundColor");
            chatItem.durationSec = Util.getJSONValueInt(liveChatPaidMessageRenderer, "durationSec");
            chatItem.fullDurationSec = Util.getJSONValueInt(liveChatPaidMessageRenderer, "fullDurationSec");
            chatItem.type = ChatItemType.TICKER_PAID_MESSAGE;
        }
        if (liveChatMembershipItemRenderer != null) {
            chatItem.messageExtended = new List<object>();
            chatItem.message = this.parseMessage(Util.getJSONMap(liveChatMembershipItemRenderer, new string[] {"headerSubtext"}), chatItem.messageExtended);
            chatItem.type = ChatItemType.NEW_MEMBER_MESSAGE;
        }
    }

    public string getJSONThumbnailURL(List<Object> thumbnails) {
        long size = 0;
        string url = null;
        foreach (Object tObj in thumbnails) {
            Dictionary<string, Object> thumbnail = (Dictionary<string, Object>) tObj;
            long width = Util.getJSONValueLong(thumbnail, "width");
            string u = Util.getJSONValueString(thumbnail, "url");
            if (u != null) {
                if (size <= width) {
                    size = width;
                    url = u;
                }
            }
        }
        return url;
    }

    public string parseMessage(Dictionary<string, Object> message, List<Object> messageExtended) {
        StringBuilder text = new StringBuilder();
        List<Object> runs = Util.getJSONList(message, "runs", new string[] {});
        if (runs != null) {
            text = new StringBuilder();
            foreach (Object runObj in runs) {
                Dictionary<string, Object> run = (Dictionary<string, Object>) runObj;
                if (run.ContainsKey("text")) {
                    text.Append(run["text"].ToString());
                    messageExtended.Add(new Text(run["text"].ToString()));
                }
                Dictionary<string, Object> emojiMap = Util.getJSONMap(run, new string[] {"emoji"});
                if (emojiMap != null) {
                    Emoji emoji = new Emoji();
                    emoji.emojiId = Util.getJSONValueString(emojiMap, "emojiId");
                    List<Object> shortcutsList = Util.getJSONList(emojiMap, "shortcuts", new string[] {});
                    List<string> shortcuts = new List<string>();
                    if (shortcutsList != null) {
                        foreach (Object s in shortcutsList) {
                            shortcuts.Add(s.ToString());
                        }
                    }
                    emoji.shortcuts = shortcuts;
                    if (shortcuts.Count != 0) text.Append(" ").Append(shortcuts[0]).Append(" ");
                    List<Object> searchTermsList = Util.getJSONList(emojiMap, "searchTerms", new string[] {});
                    List<string> searchTerms = new List<string>();
                    if (searchTermsList != null) {
                        foreach (Object s in searchTermsList) {
                            searchTerms.Add(s.ToString());
                        }
                    }
                    emoji.searchTerms = searchTerms;
                    List<Object> thumbnails = Util.getJSONList(emojiMap, "thumbnails", new string[] {"image"});
                    if (thumbnails != null) {
                        emoji.iconURL = this.getJSONThumbnailURL(thumbnails);
                    }
                    emoji.isCustomEmoji = Util.getJSONValueBoolean(emojiMap, "isCustomEmoji");
                    messageExtended.Add(emoji);
                }
            }
        }
        return text.Length == 0 ? null : text.ToString();
    }



    public void getInitialData(string id, IdType type) {
        this.isInitDataAvailable = true;
        string html = "";
        if (type == IdType.VIDEO) {
            this.videoId = id;
            html = Util.getPageContent("https://www.youtube.com/watch?v=" + id, getHeader());
            Match channelIdMatcher = Regex.Match(html, "\"channelId\":\"([^\"]*)\",\"isOwnerViewing\"");
            if (channelIdMatcher.Success) {
                this.channelId = channelIdMatcher.Result("$1");
            }
        } else if (type == IdType.CHANNEL) {
            this.channelId = id;
            html = Util.getPageContent("https://www.youtube.com/channel/" + id + "/live", getHeader());
            Match videoIdMatcher = Regex.Match(html, "\"updatedMetadataEndpoint\":\\{\"videoId\":\"([^\"]*)");
            if (videoIdMatcher.Success) {
                this.videoId = videoIdMatcher.Result("$1");
            } else {
                throw new IOException("The channel (ID:" + this.channelId + ") has not started live streaming!");
            }
        }
        Match topOnlyContinuationMatcher = Regex.Match(html, "\"selected\":true,\"continuation\":\\{\"reloadContinuationData\":\\{\"continuation\":\"([^\"]*)");
        if (topOnlyContinuationMatcher.Success) {
            this.continuation = topOnlyContinuationMatcher.Result("$1");
        }
        if (!this.isTopChatOnly) {
            Match allContinuationMatcher = Regex.Match(html, "\"selected\":false,\"continuation\":\\{\"reloadContinuationData\":\\{\"continuation\":\"([^\"]*)");
            if (allContinuationMatcher.Success) {
                this.continuation = allContinuationMatcher.Result("$1");
            }
        }
        Match innertubeApiKeyMatcher = Regex.Match(html, "\"innertubeApiKey\":\"([^\"]*)\"");
        if (innertubeApiKeyMatcher.Success) {
            this.apiKey = innertubeApiKeyMatcher.Result("$1");
        }
        Match datasyncIdMatcher = Regex.Match(html, "\"datasyncId\":\"([^|]*)\\|\\|.*\"");
        if (datasyncIdMatcher.Success) {
            this.datasyncId = datasyncIdMatcher.Result("$1");
        }
        
        html = Util.getPageContent("https://www.youtube.com/live_chat?continuation=" + this.continuation + "", getHeader());
        String initJson = html.Substring(html.IndexOf("window[\"ytInitialData\"] = ") + "window[\"ytInitialData\"] = ".Length);
        initJson = initJson.Substring(0, initJson.IndexOf(";</script>"));
        Dictionary<String, Object> json = Util.toJSON(initJson);
        Dictionary<String, Object> sendLiveChatMessageEndpoint = Util.getJSONMap(json, new string[] {"continuationContents", "liveChatContinuation", "actionPanel", "liveChatMessageInputRenderer", "sendButton", "buttonRenderer", "serviceEndpoint", "sendLiveChatMessageEndpoint"});
        if (sendLiveChatMessageEndpoint != null) {
            this.parameters = sendLiveChatMessageEndpoint["params"].ToString();
        }
        this.isInitDataAvailable = false;
    }

    public string getClientVersion() {
        if (this.clientVersion != null) {
            return this.clientVersion;
        }
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddMilliseconds(DateTimeOffset.Now.ToUnixTimeMilliseconds() - (24 * 60 * 1000));

        return "2." + dateTime.ToString("yyyyMMdd") + ".06.00";
    }

    public string getPayload(long offsetInMs) {
        if (offsetInMs < 0) {
            offsetInMs = 0;
        }
        Dictionary<string, Object> json = new Dictionary<string, Object>();
        Dictionary<string, Object> context = new Dictionary<string, Object>();
        Dictionary<string, Object> client = new Dictionary<string, Object>();
        json.Add("context", context);
        context.Add("client", client);
        client.Add("visitorData", this.visitorData);
        client.Add("userAgent", userAgent);
        client.Add("clientName", "WEB");
        client.Add("clientVersion", this.getClientVersion());
        client.Add("gl", (new RegionInfo(this.locale.LCID)).Name);
        client.Add("hl", this.locale.TwoLetterISOLanguageName);
        json.Add("continuation", this.continuation);

        return Util.toJSON(json);
    }

    public string getPayloadClient(string parameters) {
        Dictionary<string, Object> json = new Dictionary<string, Object>();
        Dictionary<string, Object> context = new Dictionary<string, Object>();
        Dictionary<string, Object> user = new Dictionary<string, Object>();
        Dictionary<string, Object> client = new Dictionary<string, Object>();
        if (this.commentCounter >= int.MaxValue - 1) {
            this.commentCounter = 0;
        }
        json.Add("context", context);
        context.Add("client", client);
        client.Add("clientName", "WEB");
        client.Add("clientVersion", this.getClientVersion());
        context.Add("user", user);
        user.Add("onBehalfOfUser", datasyncId);
        json.Add("parameters", parameters);

        return Util.toJSON(json);
    }

    public Dictionary<string, string> getHeader() {
        return new Dictionary<string, string>();
    }

    /**
     * Get broadcast info
     *
     * @return LiveBroadcastDetails obj
     */
    public bool isLive() {

        try {
            string url = liveStreamInfoApi + this.videoId + "&hl=en&pbj=1";
            Dictionary<string, string> header = new Dictionary<string, string>();
            header.Add("x-youtube-client-name", "1");
            header.Add("x-youtube-client-version", getClientVersion());
            string response = Util.getPageContent(url, header);
            if(response.Contains("isLiveNow\":true")) return true;
        }
        catch (System.Exception) {
            throw;
        }
        
        return false;
    }
}