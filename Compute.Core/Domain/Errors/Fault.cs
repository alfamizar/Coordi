namespace Compute.Core.Domain.Errors
{
    public class Fault
    {
        public FaultLevel FaultLevel { get; }
        public FaultCode FaultCode { get; }
        public Exception Exception { get; }

        public Fault(FaultLevel faultLevel, FaultCode faultCode, Exception exc)
        {
            FaultLevel = faultLevel;
            FaultCode = faultCode;
            Exception = exc;
        }
    }
}