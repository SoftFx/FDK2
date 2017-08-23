﻿namespace TickTrader.FDK.Extended
{
    using System;
    using System.Runtime.Serialization;
    using System.Security.Permissions;

    static class HResults
    {
        #region Standard Constants

        public const int E_FAIL =               unchecked((int)0x80000008);
        public const int S_OK =    0;
        public const int S_FALSE = 1;

        #endregion

        #region Standard Error Constants

        public const int E_POINTER =            unchecked((int)0x80000005);
        public const int E_INVALIDARG =         unchecked((int)0x80000003);
        public const int E_OUTOFMEMORY =        unchecked((int)0x8007000E);
        public const int E_NOTIMPL =            unchecked((int)0x80000001);

        #endregion

        #region Constants

        // customer's bit
        public const int FX_CODE_CUSTOMER =     unchecked((int)0x20000000);

        public const int FX_CODE_SUCCESS =      unchecked((int)(0x00000000 | FX_CODE_CUSTOMER));
        public const int FX_CODE_INFORMATION =  unchecked((int)(0x40000000 | FX_CODE_CUSTOMER));
        public const int FX_CODE_WARNING =      unchecked((int)(0x80000000 | FX_CODE_CUSTOMER));
        public const int FX_CODE_ERROR =        unchecked((int)(0xC0000000 | FX_CODE_CUSTOMER));

        public const int FX_CODE_START = 0;

        public const int FX_CODE_ERROR_SEND =           ((FX_CODE_START + 0) | FX_CODE_ERROR);
        public const int FX_CODE_ERROR_TIMEOUT =        ((FX_CODE_START + 1) | FX_CODE_ERROR);
        public const int FX_CODE_ERROR_RECEIVE =        ((FX_CODE_START + 2) | FX_CODE_ERROR);
        public const int FX_CODE_ERROR_EXCEPTION =      ((FX_CODE_START + 3) | FX_CODE_ERROR);
        public const int FX_CODE_ERROR_REJECT =         ((FX_CODE_START + 7) | FX_CODE_ERROR);
        public const int FX_CODE_ERROR_LOGOUT =         ((FX_CODE_START + 8) | FX_CODE_ERROR);
        public const int FX_CODE_ERROR_INVALID_HANDLE = ((FX_CODE_START + 9) | FX_CODE_ERROR);

        #endregion

        public static bool Succeeded(int status)
        {
            return status >= 0;
        }

        public static bool Failed(int status)
        {
            return status < 0;
        }
    }

    /// <summary>
    /// Provides extended data for errors; generated by API.
    /// </summary>
    [Serializable]
    public class RuntimeException : ApplicationException
    {
        internal RuntimeException(string message)
            : base(message)
        {
        }

        internal RuntimeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        internal RuntimeException(int status, string message)
            : base(message)
        {
            this.Status = status;
        }

        internal RuntimeException(int status, string message, Exception innerException)
            : base(message, innerException)
        {
            this.Status = status;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        protected RuntimeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.Status = info.GetInt32("Status");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Status", this.Status);
        }

        /// <summary>
        /// Native code error.
        /// </summary>
        public int Status { get; internal set; }
    }

    /// <summary>
    /// This exception indicates that a synchronous call has been interrupted by logout event.
    /// </summary>
    [Serializable]
    public class LogoutException : RuntimeException
    {
        internal LogoutException(string message)
            : base(HResults.FX_CODE_ERROR_LOGOUT, message)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        protected LogoutException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// This exception indicates that user's request has been rejected by server.
    /// </summary>
    [Serializable]
    public class RejectException : RuntimeException
    {
        internal RejectException(string message)
            : base(HResults.FX_CODE_ERROR_REJECT, message)
        {
        }

        internal RejectException(int code, string message)
            : base(HResults.FX_CODE_ERROR_REJECT, message)
        {
            this.Code = code;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        protected RejectException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.Code = info.GetInt32("Code");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Code", this.Code);
        }

        /// <summary>
        /// Gets business logic code error (if available), otherwise -1.
        /// </summary>
        public int Code { get; internal set; }

        /// <summary>
        /// Returns of reject reason.
        /// </summary>
        public RejectReason Reason
        {
            get
            {
                var result = (RejectReason)this.Code;
                return result;
            }
        }
    }

    /// <summary>
    /// This exception indicates that outgoing request has not been sent.
    /// </summary>
    [Serializable]
    public class SendException : RuntimeException
    {
        internal SendException(string message)
            : base(HResults.FX_CODE_ERROR_SEND, message)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        protected SendException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

/*
    /// <summary>
    /// This is exception indicates that timeout of a synchronous operation has been reached.
    /// </summary>
    [Serializable]
    public class TimeoutException : RuntimeException
    {
        internal TimeoutException(string message)
            : base(HResults.FX_CODE_ERROR_TIMEOUT, message)
        {
        }

        internal TimeoutException(int waitingIntervalInMilliseconds, string operationId, string message)
            : base(HResults.FX_CODE_ERROR_TIMEOUT, message)
        {
            this.WaitingInterval = waitingIntervalInMilliseconds;
            this.OperationId = operationId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        protected TimeoutException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.WaitingInterval = info.GetInt32("WaitingInterval");
            this.OperationId = info.GetString("OperationId");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("WaitingInterval", this.WaitingInterval);
            info.AddValue("OperationId", this.OperationId);
        }

        /// <summary>
        /// Gets used waiting interval in milliseconds.
        /// </summary>
        public int WaitingInterval { get; internal set; }

        /// <summary>
        /// Gets unique id of corresponding synchronous operation; see messages log for detailed information.
        /// </summary>
        public string OperationId { get; internal set; }
    }
*/
    /// <summary>
    /// Generated by API, if a feature is not supported by used protocol version.
    /// </summary>
    [Serializable]
    public class UnsupportedFeatureException : RuntimeException
    {
        /// <summary>
        /// Constructs a new exception instance.
        /// </summary>
        /// <param name="message">Exception message; can not be null.</param>
        internal UnsupportedFeatureException(string message)
            : base(HResults.E_FAIL, message)
        {
            this.Feature = string.Empty;
        }


        /// <summary>
        /// Constructs a new exception instance.
        /// </summary>
        /// <param name="message">Exception message; can not be null.</param>
        /// <param name="feature">Feature name.</param>
        internal UnsupportedFeatureException(string message, string feature)
            : this(message)
        {
            if (feature != null)
                this.Feature = feature;
        }

        /// <summary>
        /// Constructs a new exception instance.
        /// </summary>
        /// <param name="message">Exception message; can not be null.</param>
        /// <param name="innerException">Inner exception.</param>
        internal UnsupportedFeatureException(string message, Exception innerException)
            : base(HResults.E_FAIL, message, innerException)
        {
            this.Feature = string.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        protected UnsupportedFeatureException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.Feature = info.GetString("Feature") ?? string.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Feature", this.Feature);
        }

        /// <summary>
        /// Gets unsupported feature name.
        /// </summary>
        public string Feature { get; internal set; }
    }
}