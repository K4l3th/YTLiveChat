class LiveBroadcastDetails {
    public bool isLiveNow { get; set; }
    public string startTimestamp { get; set; }
    public string endTimestamp { get; set; }

    public override string ToString()
    {
        return "LiveBroadcastDetails {isLiveNow=" + isLiveNow + ", startTimestamp=" + startTimestamp + ", endTimestamp=" + endTimestamp + "}";
    }
}