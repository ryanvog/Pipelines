
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
using System.Reflection;

namespace ClusterOne.Pipelines
{
    public interface IRequiredKeyProvider
    {
        IDictionary<string, PropertyInfo> GetKeys();
    }

    public class RequiredKeyProvider : IRequiredKeyProvider
    {
        public static IRequiredKeyProvider Create(object obj)
        {
            return new RequiredKeyProvider(obj);
        }

        public IDictionary<string, PropertyInfo> GetKeys()
        {
            IDictionary<string, PropertyInfo> keys = new Dictionary<string, PropertyInfo>();
            BindingFlags propertyFlags = BindingFlags.Instance  |
                                         BindingFlags.NonPublic |
                                         BindingFlags.Public    |
                                         BindingFlags.FlattenHierarchy;

            PropertyInfo[] propInfos = m_object.GetType().GetProperties(propertyFlags);

            foreach (PropertyInfo pi in propInfos)
            {
                RequiredAttribute attr = pi.GetCustomAttribute(typeof(RequiredAttribute)) as RequiredAttribute;

                if (attr != null)
                {
                    keys.Add(String.IsNullOrWhiteSpace(attr.Name) ? pi.Name : attr.Name, pi);
                }
            }

            return keys;
        }

        protected RequiredKeyProvider(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            m_object = obj;
        }

        private object m_object;
    }
}
