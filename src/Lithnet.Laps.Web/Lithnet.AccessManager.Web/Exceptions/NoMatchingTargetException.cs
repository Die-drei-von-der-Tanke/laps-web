﻿using System;

namespace Lithnet.AccessManager.Web
{
    [Serializable]
    public class NoMatchingTargetException : LapsWebAppException
    {
        public NoMatchingTargetException()
        {
        }

        public NoMatchingTargetException(string message)
            : base(message)
        {
        }

        public NoMatchingTargetException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected NoMatchingTargetException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}