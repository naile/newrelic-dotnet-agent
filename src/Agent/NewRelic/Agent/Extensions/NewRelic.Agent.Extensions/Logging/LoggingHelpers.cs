// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Web;
using NewRelic.Agent.Api;

namespace NewRelic.Agent.Extensions.Logging;

public static class LoggingHelpers
{
    private const string LinkingToken = "NR-LINKING";
    private const string EntityName = "entity.name";
    private const string EntityGuid = "entity.guid";
    private const string Hostname = "hostname";
    private const string TraceId = "trace.id";
    private const string SpanId = "span.id";
    private static EncodedEntityName _encodedEntityName = new(string.Empty, string.Empty);

    public static string GetFormattedLinkingMetadata(IAgent agent)
    {
        var metadata = agent.GetLinkingMetadata();

        string entityName = string.Empty;
        if (metadata.ContainsKey(EntityName))
        {
            entityName = UrlEncodeEntityName(metadata[EntityName]);
        }

        string entityGuid = string.Empty;
        if (metadata.ContainsKey(EntityGuid))
        {
            entityGuid = metadata[EntityGuid];
        }

        string hostname = string.Empty;
        if (metadata.ContainsKey(Hostname))
        {
            hostname = metadata[Hostname];
        }

        string traceId = string.Empty;
        if (metadata.ContainsKey(TraceId))
        {
            traceId = metadata[TraceId];
        }

        string spanId = string.Empty;
        if (metadata.ContainsKey(SpanId))
        {
            spanId = metadata[SpanId];
        }

        // This is a positional blob so we want the delimiters left in when no data is  present.
        // NR-LINKING|{entity.guid}|{hostname}|{trace.id}|{span.id}|
        return LinkingToken + "|" + entityGuid + "|" + hostname + "|" + traceId + "|" + spanId + "|" + entityName + "|";
    }

    public static bool ContainsLinkingToken(string text)
    {
        return text.Contains(LinkingToken);
    }

    private static string UrlEncodeEntityName(string entityName)
    {
        var encodedEntityName = _encodedEntityName;
        if (encodedEntityName.Name != entityName)
        {
            encodedEntityName = new EncodedEntityName(entityName, HttpUtility.UrlEncode(entityName));
            _encodedEntityName = encodedEntityName;
        }

        return encodedEntityName.Encoded;
    }

    private sealed class EncodedEntityName
    {
        public readonly string Name;
        public readonly string Encoded;

        public EncodedEntityName(string name, string encoded)
        {
            Name = name;
            Encoded = encoded;
        }
    }
}