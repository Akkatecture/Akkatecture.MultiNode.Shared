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
using System.Collections.Generic;
using Akka.Event;

namespace Akka.MultiNodeTestRunner.Shared.Sinks
{
    #region Message types

    /// <summary>
    /// Message type for signaling that a new spec is ready to be run
    /// </summary>
    public class BeginNewSpec
    {
        public BeginNewSpec(string className, string methodName, IList<NodeTest> nodes)
        {
            Nodes = nodes;
            MethodName = methodName;
            ClassName = className;
        }

        public string ClassName { get; private set; }

        public string MethodName { get; private set; }

        public IList<NodeTest> Nodes { get; private set; }
    }

    /// <summary>
    /// Message type for indicating that the current spec has ended.
    /// </summary>
    public class EndSpec
    {
        public EndSpec()
        {
            ClassName = null;
            MethodName = null;
        }

        public EndSpec(string className, string methodName)
        {
            ClassName = className;
            MethodName = methodName;
        }
        
        public string ClassName { get; private set; }
        public string MethodName { get; private set; }
    }

    /// <summary>
    /// Message type for signaling that a node has completed a spec successfully
    /// </summary>
    public class NodeCompletedSpecWithSuccess
    {
        public NodeCompletedSpecWithSuccess(int nodeIndex, string nodeRole, string message)
        {
            Message = message;
            NodeIndex = nodeIndex;
            NodeRole = nodeRole;
        }

        public int NodeIndex { get; private set; }

        public string NodeRole { get; private set; }

        public string Message { get; private set; }
    }

    /// <summary>
    /// Message type for signaling that a node has completed a spec unsuccessfully
    /// </summary>
    public class NodeCompletedSpecWithFail
    {
        public NodeCompletedSpecWithFail(int nodeIndex, string nodeRole, string message)
        {
            Message = message;
            NodeIndex = nodeIndex;
            NodeRole = nodeRole;
        }

        public int NodeIndex { get; private set; }

        public string NodeRole { get; private set; }

        public string Message { get; private set; }
    }

    /// <summary>
    /// Truncated message - cut off from it's parent due to line break in I/O redirection
    /// </summary>
    public class LogMessageFragmentForNode
    {
        public LogMessageFragmentForNode(int nodeIndex, string nodeRole, string message, DateTime when)
        {
            NodeIndex = nodeIndex;
            NodeRole = nodeRole;
            Message = message;
            When = when;
        }

        public int NodeIndex { get; private set; }
        public string NodeRole { get; private set; }

        public DateTime When { get; private set; }

        public string Message { get; private set; }

        public override string ToString()
        {
            return string.Format("[NODE{1}:{2}][{0}]: {3}", When, NodeIndex, NodeRole, Message);
        }
    }

    /// <summary>
    /// Message for an individual node participating in a spec
    /// </summary>
    public class LogMessageForTestRunner
    {
        public LogMessageForTestRunner(string message, LogLevel level, DateTime when, string logSource)
        {
            LogSource = logSource;
            When = when;
            Level = level;
            Message = message;
        }

        public DateTime When { get; private set; }

        public string Message { get; private set; }

        public string LogSource { get; private set; }

        public LogLevel Level { get; private set; }

        public override string ToString()
        {
            return string.Format("[RUNNER][{0}][{1}][{2}]: {3}", When,
                Level.ToString().Replace("Level", "").ToUpperInvariant(), LogSource,
                Message);
        }
    }


    /// <summary>
    /// Message used to signal the end of the test run.
    /// </summary>
    public class EndTestRun
    {
        
    }

    #endregion
}

