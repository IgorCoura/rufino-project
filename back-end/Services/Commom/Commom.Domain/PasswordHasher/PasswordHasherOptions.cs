using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Commom.Domain.PasswordHasher
{
    /// <summary>
    /// Specifies options for password hashing.
    /// </summary>
    public class PasswordHasherOptions
    {
        public const string PasswordHash = "PasswordHash";

        private static readonly RandomNumberGenerator _defaultRng = RandomNumberGenerator.Create(); // secure PRNG

        /// <summary>
        /// Gets or sets the number of iterations used when hashing passwords using PBKDF2. Default is 100,000.
        /// </summary>
        /// <value>
        /// The number of iterations used when hashing passwords using PBKDF2.
        /// </value>
        /// <remarks>
        /// This value is only used when the compatibility mode is set to 'V3'.
        /// The value must be a positive integer.
        /// </remarks>
        public int IterationCount { get; set; } = 100000;

        // for unit testing
        internal RandomNumberGenerator Rng { get; set; } = _defaultRng;
    }

}
