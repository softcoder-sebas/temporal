using System;
using System.Collections.Generic;

namespace MyMarket_ERP
{
    public sealed class RoleDefinition
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsSystem { get; set; }
        public HashSet<NavSection> Modules { get; } = new();
        public HashSet<string> Emails { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
