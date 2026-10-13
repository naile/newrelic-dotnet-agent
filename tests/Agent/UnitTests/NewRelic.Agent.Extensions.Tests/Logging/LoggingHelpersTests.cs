// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using NewRelic.Agent.Api;
using NewRelic.Agent.Extensions.Logging;
using NUnit.Framework;
using Telerik.JustMock;

namespace Agent.Extensions.Tests.Logging;

public class LoggingHelpersTests
{
    [Test]
    public void GetFormattedLinkingMetadata_ReturnsPositionalBlob_WhenAllDataIsAvailable()
    {
        var agent = AgentWithLinkingMetadata("My App");

        Assert.That(LoggingHelpers.GetFormattedLinkingMetadata(agent), Is.EqualTo("NR-LINKING|guid|host|trace|span|My+App|"));
    }

    [Test]
    public void GetFormattedLinkingMetadata_KeepsDelimiters_WhenNoDataIsAvailable()
    {
        var agent = Mock.Create<IAgent>();
        Mock.Arrange(() => agent.GetLinkingMetadata()).Returns(new Dictionary<string, string>());

        Assert.That(LoggingHelpers.GetFormattedLinkingMetadata(agent), Is.EqualTo("NR-LINKING||||||"));
    }

    [Test]
    public void GetFormattedLinkingMetadata_EncodesTheNewEntityName_WhenTheEntityNameChanges()
    {
        LoggingHelpers.GetFormattedLinkingMetadata(AgentWithLinkingMetadata("App One"));

        Assert.That(LoggingHelpers.GetFormattedLinkingMetadata(AgentWithLinkingMetadata("App Two")), Does.EndWith("|App+Two|"));
    }

    [Test]
    public void GetFormattedLinkingMetadata_DoesNotEncodeTheSameEntityNameAgain()
    {
        const int extraChars = 999;
        var shortNameAgent = AgentWithLinkingMetadata("a");
        var longNameAgent = AgentWithLinkingMetadata(new string('a', 1 + extraChars));

        var shortNameAllocated = AllocatedBySecondCall(shortNameAgent);
        var longNameAllocated = AllocatedBySecondCall(longNameAgent);

        var returnedStringGrowth = 2 * extraChars;
        Assert.That(longNameAllocated - shortNameAllocated, Is.LessThanOrEqualTo(returnedStringGrowth + 8));
    }

    private static long AllocatedBySecondCall(IAgent agent)
    {
        LoggingHelpers.GetFormattedLinkingMetadata(agent);
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        LoggingHelpers.GetFormattedLinkingMetadata(agent);
        return GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
    }

    private static IAgent AgentWithLinkingMetadata(string entityName)
    {
        var agent = Mock.Create<IAgent>();
        Mock.Arrange(() => agent.GetLinkingMetadata()).Returns(new Dictionary<string, string>
        {
            { "trace.id", "trace" },
            { "span.id", "span" },
            { "entity.name", entityName },
            { "entity.type", "SERVICE" },
            { "entity.guid", "guid" },
            { "hostname", "host" }
        });
        return agent;
    }
}
