
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
using System.Reflection;
using log4net;


namespace ClusterOne.Pipelines
{
    public interface IStageDiscovery
    {
        void Discover(string stageAssembliesToProbe);

        Type GetType(string fullName);

        IEnumerable<Assembly> StageAssemblies { get; }

        IDictionary<string, Type> StageTypes { get; }
    }

    public class StageDiscovery : IStageDiscovery
    {
        public const string StagesPluginFolder = "";

        public void Discover(string stageAssembliesToProbe)
        {
            if (String.IsNullOrWhiteSpace(stageAssembliesToProbe))
            {
                throw new ArgumentNullException("stageAssembliesToProbe");
            }

            string[] assmembliesToProbe = stageAssembliesToProbe.Split(';');
            ICollection<Assembly> assemblyCache = new List<Assembly>();
            IDictionary<string, Type> typeCache = new Dictionary<string, Type>();

            foreach (string assm in assmembliesToProbe)
            {
                try
                {
                    assemblyCache.Add(Assembly.Load(assm));
                }
                catch (Exception ex)
                {
                    m_log.WarnFormat("Cannot load Stage plug-in {0}: {1}", assm, ex.Message);
                }
            }

            foreach (Assembly a in assemblyCache)
            {
                try
                {
                    IEnumerable<Type> assemblyMatchinTypes = a.ExportedTypes.Where(t => t.GetInterfaces().Contains(typeof(IStage)));
                    foreach (Type t in assemblyMatchinTypes)
                    {
                        m_log.DebugFormat(">>> Found supported type {0}", t.FullName);
                        typeCache.Add(t.FullName, t);
                    }
                }
                catch (Exception ex)
                {
                    m_log.WarnFormat("Cannot load types from plug-in assembly {0}: {1}", a.FullName, ex.Message);
                }
            }

            m_assemblyCache = assemblyCache;
            m_typeCache = typeCache;
        }

        public Type GetType(string fullName)
        {
            if (String.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentNullException("fullName");
            }

            Type t = null;

            m_typeCache.TryGetValue(fullName, out t);

            return t;
        }

        public StageDiscovery(ILog log)
        {

            if (log == null)
            {
                throw new ArgumentNullException("log");
            }

            m_log = log;
        }

        public IEnumerable<Assembly> StageAssemblies
        {
            get
            {
                return m_assemblyCache;
            }
        }

        public IDictionary<string, Type> StageTypes
        {
            get
            {
                return m_typeCache;
            }
        }


        private readonly ILog m_log;
        private IEnumerable<Assembly> m_assemblyCache;
        private IDictionary<string, Type> m_typeCache;
    }
}
