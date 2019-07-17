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
using System.IO;
using System.Reflection;
using System.Text;
using Akka.MultiNodeTestRunner.Shared.Reporting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Akka.MultiNodeTestRunner.Shared.Persistence
{
    /// <summary>
    /// JavaScript Object Notation (JSON) implementation of the <see cref="IRetrievableTestRunStore"/>
    /// </summary>
    public class JsonPersistentTestRunStore : IRetrievableTestRunStore
    {
        //Internal version of the contract resolver
        private class AkkaContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var prop = base.CreateProperty(member, memberSerialization);

                if (!prop.Writable)
                {
                    var property = member as PropertyInfo;
                    if (property != null)
                    {
                        var hasPrivateSetter = property.GetSetMethod(true) != null;
                        prop.Writable = hasPrivateSetter;
                    }
                }

                return prop;
            }
        }

        public static JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            //important: if reuse, the serializer will overwrite properties in default references, e.g. Props.DefaultDeploy or Props.noArgs
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            TypeNameHandling = TypeNameHandling.All,
            ContractResolver = new AkkaContractResolver()
            {
                //SerializeCompilerGeneratedMembers = true,
                //IgnoreSerializableAttribute = true,
                //IgnoreSerializableInterface = true,
            }
        };

        public bool SaveTestRun(string filePath, TestRunTree data)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentException("filePath must not be null or empty");


// ReSharper disable once AssignNullToNotNullAttribute //already made this null check with Guard
            var finalPath = Path.GetFullPath(filePath);
            var serializedObj = JsonConvert.SerializeObject(data, Formatting.Indented, Settings);

// ReSharper disable once AssignNullToNotNullAttribute
            File.WriteAllText(finalPath, serializedObj, Encoding.UTF8);

            return true;
        }

        public bool TestRunExists(string filePath)
        {
            return !string.IsNullOrEmpty(filePath) && File.Exists(Path.GetFullPath(filePath));
        }

        public TestRunTree FetchTestRun(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentException("filePath must not be null or empty");
            // ReSharper disable once AssignNullToNotNullAttribute //already made this null check with Guard
            var finalPath = Path.GetFullPath(filePath);
            var fileText = File.ReadAllText(finalPath, Encoding.UTF8);
            if (string.IsNullOrEmpty(fileText)) return null;
            return JsonConvert.DeserializeObject<TestRunTree>(fileText, Settings);
        }
    }
}

