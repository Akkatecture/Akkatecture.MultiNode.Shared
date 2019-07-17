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
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.MultiNodeTestRunner.Shared.Sinks;

namespace Akka.MultiNodeTestRunner.Shared.Reporting
{
    /// <summary>
    /// Actor responsible for organizing the results of an individual spec
    /// </summary>
    public class SpecRunCoordinator : ReceiveActor
    {
        public SpecRunCoordinator(string className, string methodName, IList<NodeTest> nodes)
        {
            Nodes = nodes;
            MethodName = methodName;
            ClassName = className;
            FactData = new FactData(string.Format("{0}.{1}", className, methodName));
            _nodeActors = new Dictionary<int, IActorRef>();
            SetReceive();
        }

        public string ClassName { get; private set; }

        public string MethodName { get; private set; }

        public IList<NodeTest> Nodes { get; private set; }

        /// <summary>
        /// All of the data for this individual spec
        /// </summary>
        protected FactData FactData;

        /// <summary>
        /// Internal dictionary used to route messages to their discrete nodes
        /// </summary>
        private readonly Dictionary<int, IActorRef> _nodeActors;

        #region Actor Lifecycle

        protected override void PreStart()
        {
            //create all of the NodeFactActor instances
            foreach (var node in Nodes)
            {
                var index = node.Node;
                var role = node.Role;
                _nodeActors.Add(index, Context.ActorOf(Props.Create(() => new NodeDataActor(index, role))));
            }
        }

        #endregion

        #region Message-handling

        private void SetReceive()
        {
            Receive<MultiNodeTestRunnerMessage>(message =>
            {
                FactData.Put(message);
            });
            Receive<MultiNodeMessage>(message => RouteToNodeActor(message));
            Receive<EndSpec>(spec => HandleEndSpec(spec));
            Receive<NodeData[]>(datum => HandleNodeDatum(datum));
        }

        /// <summary>
        /// Send a <see cref="MultiNodeMessage"/> to the correct <see cref="NodeDataActor"/> based on the 
        /// <see cref="MultiNodeMessage.NodeIndex"/> property.
        /// </summary>
        private void RouteToNodeActor(MultiNodeMessage message)
        {
            var actor = _nodeActors[message.NodeIndex];
            actor.Tell(message);
        }

        /// <summary>
        /// Wait for all child <see cref="NodeDataActor"/> instances to finish processing
        /// and report their results
        /// </summary>
        /// <returns>An awaitable task, since this operation uses the <see cref="Futures.Ask"/> pattern</returns>
        private void HandleEndSpec(EndSpec endSpec)
        {
            var futures = new Task<NodeData>[Nodes.Count];

            var i = 0;
            foreach (var node in _nodeActors)
            {
                futures[i] = node.Value.Ask<NodeData>(endSpec, TimeSpan.FromSeconds(1));
                i++;
            }

            var sender = Context.Sender;

            //wait for all Ask operations to complete and pipe the result back to ourselves, including the ref for the original sender
            Task.WhenAll(futures)
                .PipeTo(Self, sender);
        }

        /// <summary>
        /// When the result of a <see cref="HandleEndSpec"/> finally gets finished...
        /// </summary>
        /// <param name="nodeDatum">An envelope with all of the <see cref="NodeData"/> messages we processed from earlier</param>
        private void HandleNodeDatum(NodeData[] nodeDatum)
        {
            FactData.AddNodes(nodeDatum);

            //mark this test as complete
            FactData.Complete();

            //Send our FactData back to the sender
            Sender.Tell(FactData.Copy());

            //Shut ourselves down
            Self.GracefulStop(TimeSpan.FromSeconds(1));
        }

        #endregion

    }
}

