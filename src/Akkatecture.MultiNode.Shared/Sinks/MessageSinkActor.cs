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

using Akka.Actor;
using Akka.MultiNodeTestRunner.Shared.Reporting;

namespace Akka.MultiNodeTestRunner.Shared.Sinks
{
    /// <summary>
    /// Actor responsible for directing the flow of all messages for each test run.
    /// </summary>
    public abstract class MessageSinkActor : ReceiveActor
    {
        #region Message classes

        /// <summary>
        /// Used to signal that the underlying  <see cref="MessageSinkActor"/> 
        /// must collect and report its final test run results.
        /// 
        /// Shut down process is ready to begin.
        /// </summary>
        public class BeginSinkTerminate
        {
            public BeginSinkTerminate(TestRunTree testRun, IActorRef subscriber)
            {
                Subscriber = subscriber;
                TestRun = testRun;
            }

            public TestRunTree TestRun { get; private set; }
            public IActorRef Subscriber { get; private set; }
        }

        /// <summary>
        /// Signals to <see cref="MessageSink"/> that the <see cref="MessageSinkActor"/> is ready to be
        /// shut down.
        /// </summary>
        public class SinkCanBeTerminated { }

        #endregion

        protected MessageSinkActor()
        {
            SetReceive();
        }

        /// <summary>
        /// Use the template method pattern here to force child actors to fill in
        /// all handlers for these classes
        /// </summary>
        private void SetReceive()
        {
            Receive<BeginNewSpec>(spec => HandleNewSpec(spec));
            Receive<EndSpec>(endspec => HandleEndSpec(endspec));
            Receive<LogMessageFragmentForNode>(node => HandleNodeMessageFragment(node));
            Receive<LogMessageForTestRunner>(node => HandleRunnerMessage(node));
            Receive<NodeCompletedSpecWithSuccess>(success => HandleNodeSpecPass(success));
            Receive<NodeCompletedSpecWithFail>(fail => HandleNodeSpecFail(fail));
            Receive<EndTestRun>(end => HandleTestRunEnd(end));
            Receive<TestRunTree>(tree => HandleTestRunTree(tree));
            Receive<BeginSinkTerminate>(terminate => HandleSinkTerminate(terminate));
            AdditionalReceives();
        }

        #region Abstract message-handling methods

        /// <summary>
        /// Used to hook additional <see cref="Receive"/> methods into the <see cref="MessageSinkActor"/>
        /// </summary>
        protected abstract void AdditionalReceives();

        protected abstract void HandleNewSpec(BeginNewSpec newSpec);

        protected abstract void HandleEndSpec(EndSpec endSpec);

        /// <summary>
        /// Used for truncated messages (happens when there's a line break during standard I/O redirection from child nodes)
        /// </summary>
        protected abstract void HandleNodeMessageFragment(LogMessageFragmentForNode logMessageFragment);

        protected abstract void HandleRunnerMessage(LogMessageForTestRunner node);

        protected abstract void HandleNodeSpecPass(NodeCompletedSpecWithSuccess nodeSuccess);

        protected abstract void HandleNodeSpecFail(NodeCompletedSpecWithFail nodeFail);

        protected virtual void HandleTestRunEnd(EndTestRun endTestRun)
        {
            Self.Tell(new BeginSinkTerminate(null, Sender));
        }

        protected virtual void HandleSinkTerminate(BeginSinkTerminate terminate)
        {
            terminate.Subscriber.Tell(new SinkCanBeTerminated());
        }

        protected abstract void HandleTestRunTree(TestRunTree tree);

        #endregion
    }
}

