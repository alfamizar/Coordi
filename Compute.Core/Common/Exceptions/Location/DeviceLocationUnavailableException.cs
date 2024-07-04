namespace Compute.Core.Common.Exceptions.Location
{
    public class DeviceLocationUnavailableException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Exception"/> class.
        /// </summary>
        public DeviceLocationUnavailableException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Exception"/> class with the specified message.
        /// </summary>
        /// <param name="message">A message that describes this exception in more detail.</param>
        public DeviceLocationUnavailableException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Exception"/> class with the specified message and inner exception.
        /// </summary>
        /// <param name="message">A message that describes this exception in more detail.</param>
        /// <param name="innerException">An inner exception that has relation to this exception.</param>
        public DeviceLocationUnavailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
