Console.WriteLine("Hello, World!");

YouTubeLiveChat chat = new YouTubeLiveChat("gFtV4mExi8w", false, IdType.VIDEO);

int liveStatusCheckCycle = 0;
while (true) {
    chat.update();
    foreach (ChatItem item in chat.chatItems) {
        if (item.type == ChatItemType.PAID_MESSAGE) Console.ForegroundColor = ConsoleColor.Red;
        else if(item.type == ChatItemType.NEW_MEMBER_MESSAGE) Console.ForegroundColor = ConsoleColor.Green;
        else if(item.authorType.Contains(AuthorType.MEMBER)) Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(DateTime.Now.ToLongTimeString() + "|" +  item.type + "[" + item.authorName + "]" + String.Join(", ", item.authorType) + " " + item.message);
        Console.ResetColor();
    }
    liveStatusCheckCycle++;
    if(liveStatusCheckCycle >= 10) {
        // Calling this method frequently, cpu usage and network usage become higher because this method requests a http request.
        if(chat.isLive() == false) {
            break;
        }
        liveStatusCheckCycle = 0;
    }
    try {
        Thread.Sleep(1000);
    } catch (Exception e) {
        
    }
}