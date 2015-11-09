
#region Copyright(c) 2015 Cluster One
// Copyright(c) 2015 Cluster One

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using ClusterOne.Pipelines;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClusterOne.PipelinesTests
{
    [TestClass]
    public class AssemblyLoaderTests
    {
        [TestMethod]
        [DeploymentItem("SampleStageLibrary.dll")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DiscoverCtorWithNullInput()
        {
            IStageDiscovery discovery = new StageDiscovery(null);
        }

        [TestMethod]
        [DeploymentItem("SampleStageLibrary.dll")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DiscoverThrowsWithNullInput()
        {
            IStageDiscovery discovery = new StageDiscovery(s_log);
            discovery.Discover(null);
        }

        [TestMethod]
        [DeploymentItem("SampleStageLibrary.dll")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DiscoverThrowsWithEmptyInput()
        {
            IStageDiscovery discovery = new StageDiscovery(s_log);
            discovery.Discover(String.Empty);
        }

        [TestMethod]
        [DeploymentItem("SampleStageLibrary.dll")]
        public void ResolvesCorrectNumberOfTypes()
        {
            IStageDiscovery discovery = new StageDiscovery(s_log);
            discovery.Discover(s_validAssembliesToProbe.First());

            Assert.AreEqual(1, discovery.StageTypes.Count);
        }

        [TestMethod]
        [DeploymentItem("SampleStageLibrary.dll")]
        public void ResolvesCorrectNumberOfAssemblies()
        {
            IStageDiscovery discovery = new StageDiscovery(s_log);
            discovery.Discover(s_validAssembliesToProbe.First());

            Assert.AreEqual(1, discovery.StageAssemblies.Count());
        }

        [TestMethod]
        [DeploymentItem("SampleStageLibrary.dll")]
        public void DoesNotResolveInvalidStageAssemblies()
        {
            IStageDiscovery discovery = new StageDiscovery(s_log);

            foreach (string assm in s_invalidAssembliesToProbe)
            {
                discovery.Discover(assm);
                Assert.AreEqual(0, discovery.StageTypes.Count);
            }
        }

        [TestMethod]
        [DeploymentItem("SampleStageLibrary.dll")]
        public void GetValidTypeFromImportedAssembly()
        {
            IStageDiscovery discovery = new StageDiscovery(s_log);

            discovery.Discover(s_validAssembliesToProbe.First());
            
            Type actual = discovery.GetType("SampleStageLibrary.SampleStage1");
            Assert.AreEqual<Type>(typeof(SampleStageLibrary.SampleStage1), actual);
        }

        [TestMethod]
        [DeploymentItem("SampleStageLibrary.dll")]
        public void GetInvalidTypeFromImportedAssembly()
        {
            IStageDiscovery discovery = new StageDiscovery(s_log);

            discovery.Discover(s_validAssembliesToProbe.First());

            Type actual = discovery.GetType("SampleStageLibrary.SomeRandomType");
            Assert.IsNull(actual);
        }

        [TestMethod]
        [DeploymentItem("SampleStageLibrary.dll")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetNullTypeFromImportedAssembly()
        {
            IStageDiscovery discovery = new StageDiscovery(s_log);

            discovery.Discover(s_validAssembliesToProbe.First());

            Type actual = discovery.GetType(null);
        }

        private static readonly IEnumerable<string> s_validAssembliesToProbe = new string[]
        {
            "SampleStageLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
        };

        private static readonly IEnumerable<string> s_invalidAssembliesToProbe = new string[]
        {
            "Foo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            "Bar.dll",
            "Baz"
        };

        private static ILog s_log = LogManager.GetLogger(typeof(AssemblyLoaderTests));
    }
}
