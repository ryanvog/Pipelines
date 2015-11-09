
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
using System.Globalization;
using StructureMap;

namespace ClusterOne.Pipelines
{
    public interface IStageResolver
    {
        IStage GetStage(string fullTypeName);
    }

    public class StageResolver : IStageResolver
    {
        public IStage GetStage(string fullTypeName)
        {
            if (String.IsNullOrWhiteSpace(fullTypeName))
            {
                throw new ArgumentNullException("fullTypeName");
            }

            Type rawType = m_stageDiscovery.GetType(fullTypeName);

            if (rawType == null)
            {
                throw new TypeLoadException(String.Format(CultureInfo.CurrentCulture, Resources.StageTypeNotFound, fullTypeName));
            }

            object obj = m_container.GetInstance(rawType);

            return obj as IStage;
        }

        public StageResolver(IStageDiscovery stageDiscovery,
            IContainer container)
        {
            if (stageDiscovery == null)
            {
                throw new ArgumentNullException("stageDiscovery");
            }

            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            m_stageDiscovery = stageDiscovery;
            m_container = container;
        }

        private readonly IStageDiscovery m_stageDiscovery;
        private readonly IContainer m_container;
    }
}
