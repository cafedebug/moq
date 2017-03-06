﻿using System.Linq;
using System.Collections.Generic;
using Moq.Proxy;

namespace Moq.Sdk
{
    /// <summary>
    /// An <see cref="IProxyBehavior"/> that allows configuring <see cref="IMockBehavior"/>s
    /// that can dynamically determine whether they should be applied to the 
    /// current <see cref="IMethodInvocation"/>.
    /// </summary>
    public class MockProxyBehavior : IProxyBehavior, IMock
    {
        public IList<IMethodInvocation> Invocations { get; } = new List<IMethodInvocation>();

        public IList<IMockBehavior> Behaviors { get; } = new List<IMockBehavior>();

        public IMethodReturn Invoke(IMethodInvocation invocation, GetNextBehavior getNext)
        {
            // TODO: this can be optimized with compiled delegates.
            if (invocation.MethodBase.DeclaringType == typeof(IMock))
                return invocation.CreateValueReturn(invocation.MethodBase.Invoke(this, invocation.Arguments.ToArray()));

            if (invocation.MethodBase.DeclaringType == typeof(IMocked))
                return invocation.CreateValueReturn(this);

            CallContext<IMethodInvocation>.SetData(nameof(IMethodInvocation), invocation);

            Invocations.Add(invocation);

            if (Behaviors.Count == 0)
                return getNext().Invoke(invocation, getNext);

            // This is the only added functionality of this behavior, to first match 
            // applicable InvokeBehaviors and execute them in sequence.
            var applicableBehaviors = Behaviors.Where(behavior => behavior.AppliesTo(invocation)).ToArray();
            if (applicableBehaviors.Length == 0)
                return getNext().Invoke(invocation, getNext);

            var index = 0;
            return applicableBehaviors[0].Invoke(invocation, () =>
            {
                ++index;
                return (index < applicableBehaviors.Length) ?
                    applicableBehaviors[index].Invoke :
                    getNext();
            });
        }
    }
}