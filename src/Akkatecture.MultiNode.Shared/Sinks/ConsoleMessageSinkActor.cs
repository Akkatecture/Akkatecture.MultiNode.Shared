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
using System.Linq;
using Akka.Actor;
using Akka.Event;
using Akka.MultiNodeTestRunner.Shared.Extensions;
using Akka.MultiNodeTestRunner.Shared.Reporting;

namespace Akka.MultiNodeTestRunner.Shared.Sinks
{
    /// <summary>
    /// <see cref="MessageSinkActor"/> implementation that logs all of its output directly to the <see cref="Console"/>.
    /// 
    /// Has no persistence capabilities. Can optionally use a <see cref="TestRunCoordinator"/> to provide total "end of test" reporting.
    /// </summary>
    public class ConsoleMessageSinkActor : TestCoordinatorEnabledMessageSink
    {
        public ConsoleMessageSinkActor(bool useTestCoordinator) : base(useTestCoordinator)
        {
        }

        #region Message handling

        protected override void AdditionalReceives()
        {
            Receive<FactData>(data => ReceiveFactData(data));
        }

        protected override void ReceiveFactData(FactData data)
        {
            PrintSpecRunResults(data);
        }

        private void PrintSpecRunResults(FactData data)
        {
            WriteSpecMessage(string.Format("Results for {0}", data.FactName));
            WriteSpecMessage(string.Format("Start time: {0}", new DateTime(data.StartTime, DateTimeKind.Utc)));
            foreach (var node in data.NodeFacts)
            {
                WriteSpecMessage(string.Format(" --> Node {0}:{1} : {2} [{3} elapsed]", node.Value.NodeIndex, node.Value.NodeRole,
                    node.Value.Passed.GetValueOrDefault(false) ? "PASS" : "FAIL", node.Value.Elapsed));
            }
            WriteSpecMessage(string.Format("End time: {0}",
                new DateTime(data.EndTime.GetValueOrDefault(DateTime.UtcNow.Ticks), DateTimeKind.Utc)));
            WriteSpecMessage(string.Format("FINAL RESULT: {0} after {1}.",
                data.Passed.GetValueOrDefault(false) ? "PASS" : "FAIL", data.Elapsed));

            //If we had a failure
            if (data.Passed.GetValueOrDefault(false) == false)
            {
                WriteSpecMessage("Failure messages by Node");
                foreach (var node in data.NodeFacts)
                {
                    if (node.Value.Passed.GetValueOrDefault(false) == false)
                    {
                        WriteSpecMessage(string.Format("<----------- BEGIN NODE {0}:{1} ----------->", node.Key, node.Value.NodeRole));
                        foreach (var resultMessage in node.Value.ResultMessages)
                        {
                            WriteSpecMessage(String.Format(" --> {0}", resultMessage.Message));
                        }
                        if (node.Value.ResultMessages == null || node.Value.ResultMessages.Count == 0)
                            WriteSpecMessage("[received no messages - SILENT FAILURE].");
                        WriteSpecMessage(string.Format("<----------- END NODE {0}:{1} ----------->", node.Key, node.Value.NodeRole));
                    }
                }
            }
        }

        protected override void HandleNodeSpecFail(NodeCompletedSpecWithFail nodeFail)
        {
            WriteSpecFail(nodeFail.NodeIndex, nodeFail.NodeRole, nodeFail.Message);

            base.HandleNodeSpecFail(nodeFail);
        }

        protected override void HandleTestRunEnd(EndTestRun endTestRun)
        {
            WriteSpecMessage("Test run complete.");

            base.HandleTestRunEnd(endTestRun);
        }

        protected override void HandleTestRunTree(TestRunTree tree)
        {
            var passedSpecs = tree.Specs.Count(x => x.Passed.GetValueOrDefault(false));
            WriteSpecMessage(string.Format("Test run completed in [{0}] with {1}/{2} specs passed.", tree.Elapsed, passedSpecs, tree.Specs.Count()));
            foreach (var factData in tree.Specs)
            {
                PrintSpecRunResults(factData);
            }
        }

        protected override void HandleNewSpec(BeginNewSpec newSpec)
        {
            WriteSpecMessage(string.Format("Beginning spec {0}.{1} on {2} nodes", newSpec.ClassName, newSpec.MethodName, newSpec.Nodes.Count));

            base.HandleNewSpec(newSpec);
        }

        protected override void HandleEndSpec(EndSpec endSpec)
        {
            WriteSpecMessage("Spec completed.");

            base.HandleEndSpec(endSpec);
        }

        protected override void HandleNodeMessageFragment(LogMessageFragmentForNode logMessage)
        {
            WriteNodeMessage(logMessage);

            base.HandleNodeMessageFragment(logMessage);
        }

        protected override void HandleRunnerMessage(LogMessageForTestRunner node)
        {
            WriteRunnerMessage(node);

            base.HandleRunnerMessage(node);
        }

        protected override void HandleNodeSpecPass(NodeCompletedSpecWithSuccess nodeSuccess)
        {
            WriteSpecPass(nodeSuccess.NodeIndex, nodeSuccess.NodeRole, nodeSuccess.Message);

            base.HandleNodeSpecPass(nodeSuccess);
        }

        #endregion

        #region Console output methods

        /// <summary>
        /// Used to print a spec status message (spec starting, finishing, failed, etc...)
        /// </summary>
        private void WriteSpecMessage(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("[RUNNER][{0}]: {1}", DateTime.UtcNow.ToShortTimeString(), message);
            Console.ResetColor();
        }

        private void WriteSpecPass(int nodeIndex, string nodeRole, string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[NODE{0}:{1}][{2}]: SPEC PASSED: {3}", nodeIndex, nodeRole, DateTime.UtcNow.ToShortTimeString(), message);
            Console.ResetColor();
        }

        private void WriteSpecFail(int nodeIndex, string nodeRole, string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[NODE{0}:{1}][{2}]: SPEC FAILED: {3}", nodeIndex, nodeRole, DateTime.UtcNow.ToShortTimeString(), message);
            Console.ResetColor();
        }

        private void WriteRunnerMessage(LogMessageForTestRunner nodeMessage)
        {
            Console.ForegroundColor = ColorForLogLevel(nodeMessage.Level);
            Console.WriteLine(nodeMessage.ToString());
            Console.ResetColor();
        }

        private void WriteNodeMessage(LogMessageFragmentForNode nodeMessage)
        {
            Console.WriteLine(nodeMessage.ToString());
        }

        private static ConsoleColor ColorForLogLevel(LogLevel level)
        {
            var color = ConsoleColor.DarkGray;
            switch (level)
            {
                case LogLevel.DebugLevel:
                    color = ConsoleColor.Gray;
                    break;
                case LogLevel.InfoLevel:
                    color = ConsoleColor.White;
                    break;
                case LogLevel.WarningLevel:
                    color = ConsoleColor.Yellow;
                    break;
                case LogLevel.ErrorLevel:
                    color = ConsoleColor.Red;
                    break;
            }

            return color;
        }

        #endregion
    }

    /// <summary>
    /// <see cref="IMessageSink"/> implementation that writes directly to the console.
    /// </summary>
    public class ConsoleMessageSink : MessageSink
    {
        public ConsoleMessageSink()
            : base(Props.Create(() => new ConsoleMessageSinkActor(true)))
        {
        }

        protected override void HandleUnknownMessageType(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Unknown message: {0}", message);
            Console.ResetColor();
        }
    }
}
