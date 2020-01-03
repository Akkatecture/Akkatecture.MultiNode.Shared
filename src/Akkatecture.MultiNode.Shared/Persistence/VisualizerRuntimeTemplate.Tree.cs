﻿// The MIT License (MIT)
//
// Copyright (c) 2009 - 2020 Lightbend Inc.
// Copyright (c) 2013 - 2020 .NET Foundation
// Modified from original source https://github.com/akkadotnet/akka.net
//
// Copyright (c) 2018 - 2020 Lutando Ngqakaza
// https://github.com/Lutando/Akkatecture 
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using Akka.MultiNodeTestRunner.Shared.Reporting;

namespace Akka.MultiNodeTestRunner.Shared.Persistence
{
    partial class VisualizerRuntimeTemplate
    {
        private TestRunTree _tree;
        public string Prefix { get; private set; }

        public TestRunTree Tree
        {
            get { return _tree; }
            set
            {
                _tree = value;
                Prefix = LongestCommonPrefix(
                    value.Specs
                        .Select(s => s.FactName)
                        .ToArray());
            }
        }

        public string BuildSpecificationId(FactData spec)
        {
            return spec.FactName.Replace(".", "_");
        }

        public string BuildTimelineItem(FactData spec)
        {
            var messages = spec.RunnerMessages
                .Select(m => TimelineItemFactory.CreateSpecMessage(Prefix, m.Message, m.NodeIndex, m.TimeStamp));

            var facts =
                spec.NodeFacts.SelectMany(
                    nodeFact =>
                        nodeFact.Value.EventStream.Select(
                            nodeMessage =>
                                TimelineItemFactory.CreateNodeFact(
                                    Prefix,
                                    nodeMessage.Message,
                                    nodeMessage.NodeIndex,
                                    nodeMessage.TimeStamp)));

            var itemStrings = messages.Concat(facts)
                .Select(i => i.ToJavascriptString());

            return string.Join(",\r\n", itemStrings);
        }

        public string BuildGroupItems(FactData spec)
        {
            var groups = spec.NodeFacts
                .Select(
                    nf =>
                        string.Format("{{ id:{0}, content:'Node {0}:{1}' }}", nf.Value.NodeIndex, nf.Value.NodeRole))
                .Concat(@"{ id:-1, content:'Misc' }");

            return string.Join(",\r\n", groups);
        }

        public string BuildOptions(FactData spec)
        {
            var events =
                spec.NodeFacts.SelectMany(
                    nodeFact =>
                        nodeFact.Value.EventStream
                            .Select(
                                nodeMessage =>
                                    nodeMessage.TimeStamp))
                    .ToList();

            var startEventTimeParameter = "null";
            var endEventTimeParameter = "null";

            if (events.Count > 0)
            {
                var firstEventTimeStamp = events.Aggregate(
                    (aggregate, nextValue) =>
                        aggregate > nextValue
                            ? nextValue
                            : aggregate);

                var lastEventTimeStamp = events.Aggregate(
                    (aggregate, nextValue) =>
                        aggregate < nextValue
                            ? nextValue
                            : aggregate);


                var startEventTime = new DateTime(firstEventTimeStamp);
                var endDisplayTime = new DateTime(lastEventTimeStamp);

                // TODO: Find a better way of calculating additional time from message length
                // The last message is the 3 second wait. Which is about half the delta from start to end in length.
                var startEndDelta = (endDisplayTime - startEventTime).Ticks / 2;
                endDisplayTime = endDisplayTime.AddTicks(startEndDelta);

                startEventTimeParameter = string.Format("'{0}'", startEventTime.ToString("o"));
                endEventTimeParameter = string.Format("'{0}'", endDisplayTime.ToString("o"));
            }


            return string.Format(
                "{{ start:{0}, end:{1}, align:'left', clickToUse:true }}",
                startEventTimeParameter,
                endEventTimeParameter);
        }

        private static string LongestCommonPrefix(IReadOnlyList<string> strings)
        {
            if (strings == null || strings.Count == 0)
            {
                return string.Empty;
            }

            var commonPrefix = strings[0];

            for (var i = 1; i < strings.Count; i++)
            {
                var j = 0;
                for (; j < commonPrefix.Length && j < strings[i].Length; j++)
                {
                    if (commonPrefix[j] != strings[i][j])
                    {
                        commonPrefix = commonPrefix.Substring(0, j);
                        break;
                    }
                }

                if (j == strings[i].Length)
                {
                    commonPrefix = strings[i];
                }
            }

            return commonPrefix;
        }
    }
}
