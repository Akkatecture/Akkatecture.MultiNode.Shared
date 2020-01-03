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
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;

namespace Akka.MultiNodeTestRunner.Shared.Sinks
{
    /// <summary>
    /// Interface used to define destinations for MultiNodeTest messages
    /// </summary>
    public interface IMessageSink
    {

        #region Flow Control

        /// <summary>
        /// Make this <see cref="IMessageSink"/> ready for business.
        /// 
        /// Typically called at the beginning of a test run.
        /// </summary>
        void Open(ActorSystem context);

        /// <summary>
        /// Flag that determines if <see cref="Open"/> has been successfully called or not.
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// Flag that determines if <see cref="Close"/> has been successfully called or not.
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Shut down the <see cref="IMessageSink"/> instance. 
        /// 
        /// Typically called at the end of a test run.
        /// 
        /// During instances of when a test run has been successfully started, this method
        /// will wait up to 10 seconds for any <see cref="Actor"/> instances included as part of this
        /// <see cref="IMessageSink"/> to shutdown, via the <see cref="GracefulStopSupport.GracefulStop(IActorRef, TimeSpan)"/> method.
        /// </summary>
        Task<bool> Close(ActorSystem context);

        #endregion

        #region Message Handling

        /// <summary>
        /// Report that the test runner is moving onto the next test in the testsuite.
        /// </summary>
        /// <param name="className">The name of the class containing the spec.</param>
        /// <param name="methodName">The name of the individual test method.</param>
        /// <param name="nodes">The number of nodes who will be participating in this test.</param>
        void BeginTest(string className, string methodName, IList<NodeTest> nodes);

        /// <summary>
        /// Report that the test runner is terminating the current test in the suite.
        /// </summary>
        void EndTest(string className, string methodName);

        /// <summary>
        /// Report that an individual node has passed its test.
        /// </summary>
        /// <param name="nodeIndex">The Id of the node in the 0-N index.</param>
        /// <param name="nodeRole">The Role of the node.</param>
        /// <param name="message">A string message included with the notification.</param>
        void Success(int nodeIndex, string nodeRole, string message);

        /// <summary>
        /// Report a log message from the MultiNodeTestRunner itself.
        /// </summary>
        /// <param name="message">A string message included with the notification.</param>
        /// <param name="logSource">The source of a log message.</param>
        /// <param name="level">The <see cref="LogLevel"/> of this message.</param>
        void LogRunnerMessage(string message, string logSource, LogLevel level);

        /// <summary>
        /// Offer a raw message to the message sink. <see cref="MessageSink"/> will attempt to parse it
        /// and turn it into one of the below parsing calls.
        /// </summary>
        /// <param name="messageStr">A raw log message</param>
        void Offer(string messageStr);

        #endregion
    }
}

