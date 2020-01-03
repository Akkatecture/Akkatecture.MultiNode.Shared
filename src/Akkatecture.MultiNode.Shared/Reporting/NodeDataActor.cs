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
using Akka.Actor;
using Akka.MultiNodeTestRunner.Shared.Sinks;

namespace Akka.MultiNodeTestRunner.Shared.Reporting
{
    /// <summary>
    /// Actor responsible for processing test messages for an individual node within a multi-node test
    /// </summary>
    public class NodeDataActor : ReceiveActor
    {
        /// <summary>
        /// Data that will be processed and aggregated for an individual node
        /// </summary>
        protected NodeData NodeData;

        /// <summary>
        /// The ID of this node in the 0-N index of all nodes for this test.
        /// </summary>
        protected readonly int NodeIndex;

        /// <summary>
        /// The Role of this node.
        /// </summary>
        protected readonly string NodeRole;

        public NodeDataActor(int nodeIndex, string nodeRole)
        {
            NodeIndex = nodeIndex;
            NodeRole = nodeRole;
            NodeData = new NodeData(nodeIndex, nodeRole);
            SetReceive();
        }

        #region Message-handling

        private void SetReceive()
        {
            Receive<MultiNodeMessage>(message => NodeData.Put(message));


            Receive<EndSpec>(spec =>
            {
                

                //Send NodeData to parent for aggregation purposes
                Sender.Tell(NodeData.Copy());

                //Begin shutdown
                Context.Self.GracefulStop(TimeSpan.FromSeconds(1));
            });
        }

        #endregion
    }
}

