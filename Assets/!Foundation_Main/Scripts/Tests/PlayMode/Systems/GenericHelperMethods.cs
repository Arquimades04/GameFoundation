using UnityEngine;
using System;
using System.Collections;
using NUnit.Framework;

namespace Foundation.Tests.PlayMode
{
    public class GenericHelperMethods
    {
        public static IEnumerator WaitFor(
           Func<bool> condition,
           string failureMessage,
           int maxFrames = 60)
        {
            for (int frame = 0; frame < maxFrames; frame++)
            {
                if (condition())
                    yield break;

                yield return null;
            }

            Assert.Fail(failureMessage);
        }
    }
}
