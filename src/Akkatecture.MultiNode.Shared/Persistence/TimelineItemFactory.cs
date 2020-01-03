// The MIT License (MIT)
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

namespace Akka.MultiNodeTestRunner.Shared.Persistence
{
    public static class TimelineItemFactory
    {
        private static readonly string[] CssClasses =
        {
            "vis-item-one",
            "vis-item-two",
            "vis-item-three",
            "vis-item-four",
            "vis-item-five",
            "vis-item-six",
            "vis-item-seven",
            "vis-item-eight",
            "vis-item-nine",
            "vis-item-ten",
            "vis-item-eleven",
            "vis-item-twelve",
            "vis-item-thirteen",
            "vis-item-fourteen",
            "vis-item-fifteen"
        };

        private static readonly string passedTestContent = @"<div class=""tick-image"" />";

        public static TimelineItem CreateSpecMessage(string prefix, string title, int groupId, long startTimeStamp)
        {
            var content = title.Replace(prefix, string.Empty);
            return new TimelineItem("timeline-message", content, title, new DateTime(startTimeStamp), groupId);
        }

        public static TimelineItem CreateNodeFact(string prefix, string title, int groupId, long startTimeStamp)
        {
            var content = title.Replace(prefix, string.Empty);
            if (title.EndsWith("PASS") || title.EndsWith("passed."))
            {
                content = passedTestContent;
            }
            return new TimelineItem(CssClasses[startTimeStamp%15], content, title, new DateTime(startTimeStamp), groupId);
        }
    }
}
