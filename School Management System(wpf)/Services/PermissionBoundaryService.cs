using System;
using System.Collections.Generic;
using System.Linq;
using School_Management_System.Models;

namespace School_Management_System.Services
{
    internal sealed class PermissionBoundaryService
    {
        private static readonly HashSet<PolicyActionKey> SuperAdminAllowList = Enum
            .GetValues<PolicyActionKey>()
            .ToHashSet();

        private static readonly IReadOnlyDictionary<UserRole, HashSet<PolicyActionKey>> RolePolicyMap =
            new Dictionary<UserRole, HashSet<PolicyActionKey>>
            {
                [UserRole.SUPERADMIN] = SuperAdminAllowList,
                [UserRole.TEACHER] = new HashSet<PolicyActionKey>(),
                [UserRole.STUDENT] = new HashSet<PolicyActionKey>()
            };

        public bool IsAllowed(PolicyActionKey actionKey, User? actor = null)
        {
            actor ??= SessionContext.CurrentUser;
            if (actor == null)
            {
                return false;
            }

            if (!RolePolicyMap.TryGetValue(actor.Role, out var allowList))
            {
                return false;
            }

            return allowList.Contains(actionKey);
        }

        public void EnsureAllowed(PolicyActionKey actionKey, User? actor = null)
        {
            actor ??= SessionContext.CurrentUser;
            if (IsAllowed(actionKey, actor))
            {
                return;
            }

            var role = actor?.Role.ToString() ?? "UNAUTHENTICATED";
            throw new DomainValidationException($"Permission denied for '{actionKey}' under role '{role}'.");
        }
    }
}
