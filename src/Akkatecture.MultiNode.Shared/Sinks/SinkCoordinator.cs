// The MIT License (MIT)
//
// Copyright (c) 2009 - 2018 Lightbend Inc.
// Copyright (c) 2013 - 2018 .NET Foundation
// Modified from original source https://github.com/akkadotnet/akka.net
//
// Copyright (c) 2018 - 2019 Lutando Ngqakaza
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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;

namespace Akka.MultiNodeTestRunner.Shared.Sinks
{
    /// <summary>
    /// Top-level actor responsible for managing all <see cref="MessageSink"/> instances.
    /// </summary>
    public class SinkCoordinator : ReceiveActor
    {
        #region Message classes

        /// <summary>
        /// Used to signal that we need to enable a given <see cref="MessageSink"/> instance
        /// </summary>
        public class EnableSink
        {
            public EnableSink(MessageSink sink)
            {
                Sink = sink;
            }

            public MessageSink Sink { get; private set; }
        }

        /// <summary>
        /// Test run is complete. Shut down all sinks.
        /// 
        /// NOTE: Sending this message also means that the <see cref="ActorSystem"/> will be shut down.
        /// </summary>
        public class CloseAllSinks { }

        /// <summary>
        /// Confirms that a <see cref="MessageSink"/> has been closed
        /// </summary>
        public class SinkClosed { }

        /// <summary>
        /// Case class for distinguishing runner messages
        /// </summary>
        public class RunnerMessage
        {
            public RunnerMessage(string message)
            {
                Message = message;
            }

            public string Message { get; private set; }
        }

        /// <summary>
        /// Message that the <see cref="SinkCoordinator"/> will pass onto a <see cref="MessageSinkActor"/>
        /// </summary>
        public class RequestExitCode { }

        /// <summary>
        /// Response sent to <see cref="SinkCoordinator"/>
        /// </summary>
        public class RecommendedExitCode
        {
            public RecommendedExitCode(int code)
            {
                Code = code;
            }

            public int Code { get; private set; }
        }

        #endregion

        protected List<MessageSink> DefaultSinks;
        protected List<MessageSink> Sinks = new List<MessageSink>();

        protected int TotalReceiveClosedConfirmations = 0;
        protected int ReceivedSinkCloseConfirmations = 0;

        /// <summary>
        /// Leave the console message sink enabled by default
        /// </summary>
        public SinkCoordinator()
            : this(new[] { new ConsoleMessageSink() })
        {

        }

        public SinkCoordinator(IEnumerable<MessageSink> defaultSinks)
        {
            DefaultSinks = defaultSinks.ToList();
            InitializeReceives();
        }

        #region Actor lifecycle

        protected override void PreStart()
        {
            foreach(var sink in DefaultSinks)
                Self.Tell(new EnableSink(sink));
        }

        #endregion

        #region Message-handling

        private void InitializeReceives()
        {
            Receive<EnableSink>(sink =>
            {
                Sinks.Add(sink.Sink);
                sink.Sink.Open(Context.System);
            });

            Receive<SinkClosed>(closed =>
            {
                ReceivedSinkCloseConfirmations++;

                //Shut down the ActorSystem if all confirmations have been received
                if (ReceivedSinkCloseConfirmations >= TotalReceiveClosedConfirmations)
                    Context.System.Terminate();
            });

            Receive<RecommendedExitCode>(code =>
            {
                ExitCodeContainer.ExitCode = code.Code;
            });

            Receive<CloseAllSinks>(sinks =>
            {
                //Ignore duplicate CloseAllSinks calls
                if (TotalReceiveClosedConfirmations > 0) return;

                TotalReceiveClosedConfirmations = Sinks.Count;
                ReceivedSinkCloseConfirmations = 0;

                foreach (var sink in Sinks)
                {
                    sink.RequestExitCode(Self);
                    sink.Close(Context.System)
                        .ContinueWith(r => new SinkClosed(),
                        TaskContinuationOptions.ExecuteSynchronously)
                        .PipeTo(Self);
                }
            });
            Receive<string>(s =>
            {
                PublishToChildren(s);
            });
            Receive<NodeCompletedSpecWithSuccess>(s => PublishToChildren(s));
            Receive<IList<NodeTest>>(tests => BeginSpec(tests));
            Receive<EndSpec>(spec => EndSpec(spec.ClassName, spec.MethodName));
            Receive<RunnerMessage>(runner => PublishToChildren(runner));
        }

        private void PublishToChildren(NodeCompletedSpecWithSuccess message)
        {
            foreach(var sink in Sinks)
                sink.Success(message.NodeIndex, message.NodeRole, message.Message);
        }


        private void EndSpec(string testName, string methodName)
        {
            foreach (var sink in Sinks)
                sink.EndTest(testName, methodName);
        }

        private void BeginSpec(IList<NodeTest> tests)
        {
            var test = tests.First();

            foreach (var sink in Sinks)
                sink.BeginTest(test.TestName, test.MethodName, tests);
        }

        private void PublishToChildren(RunnerMessage message)
        {
            foreach (var sink in Sinks)
            {
                sink.LogRunnerMessage(message.Message, Assembly.GetEntryAssembly().GetName().Name, LogLevel.InfoLevel);
            }
        }

        /// <summary>
        /// Publish a message to all <see cref="MessageSink"/> instances.
        /// </summary>
        private void PublishToChildren(string message)
        {
            foreach (var sink in Sinks)
                sink.Offer(message);
        }

        #endregion
    }
}

