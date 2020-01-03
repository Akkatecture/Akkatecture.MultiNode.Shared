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
using System.IO;
using Akka.Actor;
using Akka.MultiNodeTestRunner.Shared.Persistence;
using Akka.MultiNodeTestRunner.Shared.Reporting;

namespace Akka.MultiNodeTestRunner.Shared.Sinks
{
    /// <summary>
    /// A file system <see cref="MessageSink"/> implementation
    /// </summary>
    public class FileSystemMessageSink : MessageSink
    {
        public FileSystemMessageSink(string assemblyName, string platform)
            : this(
                Props.Create(
                    () =>
                        new FileSystemMessageSinkActor(new JsonPersistentTestRunStore(), 
                            FileNameGenerator.GenerateFileName(assemblyName, platform, ".json"),
                            true,
                            true)))
        {
            
        }

        public FileSystemMessageSink(Props messageSinkActorProps) : base(messageSinkActorProps)
        {
        }

        protected override void HandleUnknownMessageType(string message)
        {
            //do nothing
        }
    }

    /// <summary>
    /// <see cref="MessageSink"/> responsible for writing to the file system.
    /// </summary>
    public class FileSystemMessageSinkActor : TestCoordinatorEnabledMessageSink
    {
        protected IPersistentTestRunStore FileStore;
        protected string FileName;
        private readonly bool _reportStatus;

        public FileSystemMessageSinkActor(IPersistentTestRunStore store, string fileName, bool reportStatus, bool useTestCoordinator)
            : base(useTestCoordinator)
        {
            FileStore = store;
            FileName = fileName;
            _reportStatus = reportStatus;
        }

        protected override void AdditionalReceives()
        {
            Receive<FactData>(data => ReceiveFactData(data));
        }

        protected override void HandleTestRunTree(TestRunTree tree)
        {
            if (_reportStatus)
                Console.WriteLine("Writing test state to: {0}", Path.GetFullPath(FileName));
            try
            {
                FileStore.SaveTestRun(FileName, tree);
            }
            catch (Exception ex) //avoid throwing exception back to parent - just continue
            {
                if (_reportStatus)
                    Console.WriteLine("Failed to write test state to {0}. Cause: {1}", Path.GetFullPath(FileName), ex);                
            }
            if (_reportStatus)
                Console.WriteLine("Finished.");           
        }

        protected override void ReceiveFactData(FactData data)
        {
            //Ask the TestRunCoordinator to give us the latest state
            Sender.Tell(new TestRunCoordinator.RequestTestRunState());
        }
    }
}

