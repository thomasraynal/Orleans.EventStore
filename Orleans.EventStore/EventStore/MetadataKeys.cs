﻿namespace Orleans.EventStore
{
    public static class MetadataKeys
    {
        public static readonly string EventClrTypeHeader = "EventClrTypeName";
        public static readonly string CommitIdHeader = "CommitId";
        public static readonly string UserIdentityHeader = "UserIdentity";
        public static readonly string ServerNameHeader = "ServerName";
        public static readonly string ServerClockHeader = "ServerClock";
    }
}