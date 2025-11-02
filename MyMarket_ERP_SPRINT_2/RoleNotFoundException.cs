using System;

namespace MyMarket_ERP
{
    public sealed class RoleNotFoundException : Exception
    {
        public RoleNotFoundException()
            : base("Este rol no existe.")
        {
        }

        public RoleNotFoundException(string? message)
            : base(message ?? "Este rol no existe.")
        {
        }

        public RoleNotFoundException(string? message, Exception? innerException)
            : base(message ?? "Este rol no existe.", innerException)
        {
        }
    }
}
