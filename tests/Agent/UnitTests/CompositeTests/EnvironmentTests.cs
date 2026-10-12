// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CompositeTests;

[TestFixture]
public class EnvironmentTests
{
    private NewRelic.Agent.Core.SharedInterfaces.Environment _env;

    [SetUp]
    public void Setup()
    {
        System.Environment.SetEnvironmentVariable("NEWRELIC_HOME", null);
        System.Environment.SetEnvironmentVariable("NEW_RELIC_HOME", null);
        NewRelic.Agent.Core.SharedInterfaces.Environment.ResetCache();

        _env = new NewRelic.Agent.Core.SharedInterfaces.Environment();
    }

    [TearDown]
    public void TearDown()
    {
        System.Environment.SetEnvironmentVariable("NEWRELIC_HOME", null);
        System.Environment.SetEnvironmentVariable("NEW_RELIC_HOME", null);
        NewRelic.Agent.Core.SharedInterfaces.Environment.ResetCache();
    }

    [Test]
    public void GetEnvironmentVariable_Supports_LegacyNaming()
    {
        System.Environment.SetEnvironmentVariable("NEWRELIC_HOME", "legacy");

        var result = _env.GetEnvironmentVariableFromList("NEW_RELIC_HOME", "NEWRELIC_HOME");

        Assert.That(result, Is.EqualTo("legacy"));
    }

    [Test]
    public void GetEnvironmentVariable_Prefers_ModernNaming_IfBothAreSpecified()
    {
        System.Environment.SetEnvironmentVariable("NEWRELIC_HOME", "legacy");
        System.Environment.SetEnvironmentVariable("NEW_RELIC_HOME", "modern");

        var result = _env.GetEnvironmentVariableFromList("NEW_RELIC_HOME", "NEWRELIC_HOME");

        Assert.That(result, Is.EqualTo("modern"));
    }

    [Test]
    public void GetEnvironmentVariableFromList_ReturnsNull_IfNoneIsSet()
    {
        var result = _env.GetEnvironmentVariableFromList("NEW_RELIC_HOME", "NEWRELIC_HOME");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetEnvironmentVariableFromList_ReturnsNull_IfTheFirstSetVariableIsEmpty()
    {
        var env = new EnvironmentWithValues(new Dictionary<string, string> { { "NEW_RELIC_HOME", string.Empty }, { "NEWRELIC_HOME", "legacy" } });

        var result = env.GetEnvironmentVariableFromList("NEW_RELIC_HOME", "NEWRELIC_HOME");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetEnvironmentVariableFromList_Throws_IfNoVariableIsGiven()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => _env.GetEnvironmentVariableFromList(null));
            Assert.Throws<ArgumentNullException>(() => _env.GetEnvironmentVariableFromList());
        });
    }

    private class EnvironmentWithValues : NewRelic.Agent.Core.SharedInterfaces.Environment
    {
        private readonly Dictionary<string, string> _values;

        public EnvironmentWithValues(Dictionary<string, string> values)
        {
            _values = values;
        }

        public override string GetEnvironmentVariable(string variable) => _values.TryGetValue(variable, out var value) ? value : null;
    }
}
