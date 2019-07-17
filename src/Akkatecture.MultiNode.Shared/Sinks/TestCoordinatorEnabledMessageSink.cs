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

using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.MultiNodeTestRunner.Shared.Reporting;

namespace Akka.MultiNodeTestRunner.Shared.Sinks
{
    /// <summary>
    /// A <see cref="MessageSinkActor"/> implementation that is capable of using a <see cref="TestRunCoordinator"/> for 
    /// test run summaries and other purposes.
    /// </summary>
    public abstract class TestCoordinatorEnabledMessageSink : MessageSinkActor
    {
        protected IActorRef TestCoordinatorActorRef;
        protected bool UseTestCoordinator;

        protected TestCoordinatorEnabledMessageSink(bool useTestCoordinator)
        {
            UseTestCoordinator = useTestCoordinator;
            Receive<SinkCoordinator.RequestExitCode>(code =>
            {
                if (UseTestCoordinator)
                {
                    var sender = Sender;
                    TestCoordinatorActorRef.Ask<TestRunTree>(new TestRunCoordinator.RequestTestRunState())
                        .ContinueWith(task =>
                        {
                            return new SinkCoordinator.RecommendedExitCode(task.Result.Passed.GetValueOrDefault(false)
                                ? 0
                                : 1);
                        }, TaskContinuationOptions.ExecuteSynchronously)
                            .PipeTo(sender, Self);
                }
            });
        }

        protected override void PreStart()
        {
            //Fire up a TestRunCoordinator instance and subscribe to FactData messages when they arrive
            if (UseTestCoordinator)
            {
                TestCoordinatorActorRef = Context.ActorOf<TestRunCoordinator>();
                TestCoordinatorActorRef.Tell(new TestRunCoordinator.SubscribeFactCompletionMessages(Self));
            }
        }

        protected abstract void ReceiveFactData(FactData data);

        protected override void HandleNewSpec(BeginNewSpec newSpec)
        {
            if (UseTestCoordinator)
            {
                TestCoordinatorActorRef.Tell(newSpec);
            }
        }

        protected override void HandleEndSpec(EndSpec endSpec)
        {
            if (UseTestCoordinator)
            {
                TestCoordinatorActorRef.Tell(endSpec);
            }
        }

        protected override void HandleNodeMessageFragment(LogMessageFragmentForNode logMessage)
        {
            if (UseTestCoordinator)
            {
                var nodeMessage = new MultiNodeLogMessageFragment(logMessage.When.Ticks, logMessage.Message,
                   logMessage.NodeIndex, logMessage.NodeRole);

                TestCoordinatorActorRef.Tell(nodeMessage);
            }
        }

        protected override void HandleRunnerMessage(LogMessageForTestRunner node)
        {
            if (UseTestCoordinator)
            {
                var runnerMessage = new MultiNodeTestRunnerMessage(node.When.Ticks, node.Message, node.LogSource,
                    node.Level);

                TestCoordinatorActorRef.Tell(runnerMessage);
            }
        }

        protected override void HandleNodeSpecPass(NodeCompletedSpecWithSuccess nodeSuccess)
        {
            if (UseTestCoordinator)
            {
                var nodeMessage = new MultiNodeResultMessage(DateTime.UtcNow.Ticks, nodeSuccess.Message,
                    nodeSuccess.NodeIndex, nodeSuccess.NodeRole, true);

                TestCoordinatorActorRef.Tell(nodeMessage);
            }
        }

        protected override void HandleNodeSpecFail(NodeCompletedSpecWithFail nodeFail)
        {
            if (UseTestCoordinator)
            {
                var nodeMessage = new MultiNodeResultMessage(DateTime.UtcNow.Ticks, nodeFail.Message,
                    nodeFail.NodeIndex, nodeFail.NodeRole, false);

                TestCoordinatorActorRef.Tell(nodeMessage);
            }
        }

        protected override void HandleTestRunEnd(EndTestRun endTestRun)
        {
            if (UseTestCoordinator)
            {
                var sender = Sender;
                TestCoordinatorActorRef.Ask<TestRunTree>(endTestRun)
                    .ContinueWith(tr =>
                    {
                        var testRunTree = tr.Result;
                        return new BeginSinkTerminate(testRunTree, sender);
                    }, TaskContinuationOptions.ExecuteSynchronously)
                    .PipeTo(Self);
            }
        }

        protected override void HandleSinkTerminate(BeginSinkTerminate terminate)
        {
            HandleTestRunTree(terminate.TestRun);
            base.HandleSinkTerminate(terminate);
        }
    }
}

