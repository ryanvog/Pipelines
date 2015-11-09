
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
using System.Globalization;
using System.Linq;

namespace ClusterOne.Pipelines
{
    public interface IStrategy
    {
        IEnumerable<IStage> Stages { get; }
    }

    public class Strategy : IStrategy
    {
        public static IStrategy Create(Uri uri, IEnumerable<IStrategyMetadata> strategies, IStageResolver stageResolver)
        {
            return new Strategy(uri, strategies, stageResolver);
        }

        public IEnumerable<IStage> Stages
        {
            get
            {
                if (m_stages == null)
                {
                    m_stages = ResolveStages();
                }

                return m_stages;
            }
        }

        protected Strategy(Uri uri, IEnumerable<IStrategyMetadata> strategies, IStageResolver stageResolver)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            m_uri = uri;

            if (strategies == null)
            {
                throw new ArgumentNullException("strategies");
            }

            m_strategies = strategies;

            if (stageResolver == null)
            {
                throw new ArgumentNullException("stageResolver");
            }

            m_stageResolver = stageResolver;
        }

        private IEnumerable<IStage> ResolveStages()
        {
            IStrategyMetadata strategy = m_strategies.FirstOrDefault(sm => sm.Uri.Equals(m_uri));

            if (strategy == null)
            {
                throw new StrategyNotFoundException(String.Format(CultureInfo.InvariantCulture, 
                                                                  Resources.CannotFindStrategyMetadata, 
                                                                  m_uri));
            }

            ICollection<IStage> stages = new List<IStage>();

            foreach (string stageName in strategy.Stages)
            {
                IStage stage = m_stageResolver.GetStage(stageName);

                stages.Add(stage);
            }

            return stages;
        }

        private readonly IEnumerable<IStrategyMetadata> m_strategies;
        private readonly IStageResolver m_stageResolver;
        private readonly Uri m_uri;
        private IEnumerable<IStage> m_stages;
    }
}
