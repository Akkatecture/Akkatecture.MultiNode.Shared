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
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.MultiNodeTestRunner.Shared.Sinks;

namespace Akka.MultiNodeTestRunner.Shared.Reporting
{
    /// <summary>
    /// Actor responsible for organizing all of the data for each test run
    /// </summary>
    public class TestRunCoordinator : ReceiveActor
    {
        #region Internal message classes

        /// <summary>
        /// Message used to request the current <see cref="TestRunData"/> value.
        /// </summary>
        public class RequestTestRunState { }

        /// <summary>
        /// Signals that we need to publish all <see cref="FactData"/> messages to the <see cref="Subscriber"/>
        /// </summary>
        public class SubscribeFactCompletionMessages
        {
            public SubscribeFactCompletionMessages(IActorRef subscriber)
            {
                Subscriber = subscriber;
            }

            public IActorRef Subscriber { get; private set; }
        }

        /// <summary>
        /// Signals that <see cref="Subscriber"/> no longer wants to receive <see cref="FactData"/> messages
        /// </summary>
        public class UnsubscribeFactCompletionMessages
        {
            public UnsubscribeFactCompletionMessages(IActorRef subscriber)
            {
                Subscriber = subscriber;
            }


            public IActorRef Subscriber { get; private set; }
        }

        #endregion

        /// <summary>
        /// Default constructor which uses <see cref="DateTime.UtcNow"/> as the time for <see cref="TestRunStarted"/>.
        /// </summary>
        public TestRunCoordinator() : this(DateTime.UtcNow) { }

        public TestRunCoordinator(DateTime testRunStarted)
        {
            TestRunStarted = testRunStarted;
            TestRunData = new TestRunTree(testRunStarted.Ticks);
            Subscribers = new List<IActorRef>();
            SetReceive();
        }

        #region Internal fields and Properties

        protected readonly DateTime TestRunStarted;

        protected IActorRef _currentSpecRunActor;

        /// <summary>
        /// Automatically set when <see cref="EndTestRun"/> is sent to this actor.
        /// </summary>
        protected DateTime? TestRunCompleted { get; private set; }

        /// <summary>
        /// The amount of time elapsed for this test run
        /// </summary>
        protected TimeSpan TestRunElapsed
        {
            get
            {
                return TestRunStarted - (TestRunCompleted.HasValue ? TestRunCompleted.Value : DateTime.UtcNow);
            }
        }

        /// <summary>
        /// Contains the entire tree of information needed to process results of a full test run.
        /// </summary>
        protected TestRunTree TestRunData;

        /// <summary>
        /// All of the subscribers who wish to receive <see cref="FactData"/> notifications
        /// </summary>
        protected List<IActorRef> Subscribers;

        #endregion

        #region Message-handling

        private void SetReceive()
        {
            Receive<MultiNodeMessage>(message =>
            {
                if (_currentSpecRunActor == null) return;
                _currentSpecRunActor.Forward(message);
            });
            Receive<BeginNewSpec>(spec => ReceiveBeginSpecRun(spec));
            ReceiveAsync<EndSpec>(spec => ReceiveEndSpecRun(spec));
            Receive<RequestTestRunState>(state => Sender.Tell(TestRunData.Copy(TestRunPassed(TestRunData))));
            Receive<SubscribeFactCompletionMessages>(messages => AddSubscriber(messages));
            Receive<UnsubscribeFactCompletionMessages>(messages => RemoveSubscriber(messages));
            ReceiveAsync<EndTestRun>(async run =>
            {
                //clean up the current spec, if it hasn't been done already
                if (_currentSpecRunActor != null)
                {
                    await ReceiveEndSpecRun(new EndSpec());
                }

                //Mark the test run as finished
                TestRunData.Complete();

                //Deliver the final copy of the TestRunData
                Sender.Tell(TestRunData.Copy());

                //shutdown
                Context.Stop(Self);
            });
        }

        private void RemoveSubscriber(UnsubscribeFactCompletionMessages unsubscribe)
        {
            Subscribers.Remove(unsubscribe.Subscriber);
        }

        private void AddSubscriber(SubscribeFactCompletionMessages subscription)
        {
            Subscribers.Add(subscription.Subscriber);
        }

        private void ReceiveBeginSpecRun(BeginNewSpec spec)
        {
            if (_currentSpecRunActor != null) throw new InvalidOperationException("EndSpec has not been called for previous run yet. Cannot begin next run.");

            //Create the new spec run actor
            _currentSpecRunActor =
                Context.ActorOf(
                    Props.Create(() => new SpecRunCoordinator(spec.ClassName, spec.MethodName, spec.Nodes)));
        }

        private async Task ReceiveEndSpecRun(EndSpec spec)
        {
            //Should receive a FactData in return
            var factData = await _currentSpecRunActor.Ask<FactData>(spec, TimeSpan.FromSeconds(2));

            TestRunData.AddSpec(factData);

            //Publish the FactData back to any subscribers who wanted it
            foreach (var subscriber in Subscribers)
            {
                subscriber.Tell(factData);
            }

            //Ready to begin the next spec
            _currentSpecRunActor = null;
        }

        private static bool TestRunPassed(TestRunTree tree)
        {
            return tree.Specs.All(x => x.Passed.HasValue && x.Passed.Value);
        }

        #endregion
    }
}

