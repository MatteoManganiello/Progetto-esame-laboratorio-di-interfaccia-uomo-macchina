using System.Collections.Generic;
using System.Linq;
using Template.Web.Features.Admin;

namespace Template.Web.Infrastructure
{
    public static class AdminComunicazioniStore
    {
        private const int MaxItems = 20;
        private static readonly object LockObj = new object();
        private static readonly List<NotificaItem> AdminToAdmin = new List<NotificaItem>();
        private static readonly List<NotificaItem> SuperAdminToAdmin = new List<NotificaItem>();

        public static IReadOnlyList<NotificaItem> GetAdminToAdmin()
        {
            lock (LockObj)
            {
                return AdminToAdmin.ToList();
            }
        }

        public static IReadOnlyList<NotificaItem> GetSuperAdminToAdmin()
        {
            lock (LockObj)
            {
                return SuperAdminToAdmin.ToList();
            }
        }

        public static void AddAdminToAdmin(NotificaItem item)
        {
            if (IsEmpty(item))
                return;

            lock (LockObj)
            {
                AdminToAdmin.Insert(0, item);
                Trim(AdminToAdmin);
            }
        }

        public static void AddSuperAdminToAdmin(NotificaItem item)
        {
            if (IsEmpty(item))
                return;

            lock (LockObj)
            {
                SuperAdminToAdmin.Insert(0, item);
                Trim(SuperAdminToAdmin);
            }
        }

        private static void Trim(List<NotificaItem> list)
        {
            if (list.Count <= MaxItems)
                return;

            list.RemoveRange(MaxItems, list.Count - MaxItems);
        }

        private static bool IsEmpty(NotificaItem item)
        {
            return item == null
                || (string.IsNullOrWhiteSpace(item.Data)
                    && string.IsNullOrWhiteSpace(item.Titolo)
                    && string.IsNullOrWhiteSpace(item.Contenuto));
        }
    }
}
