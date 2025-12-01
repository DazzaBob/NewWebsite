using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.Mapping;
using System.Data;
using System.Linq.Expressions;
using Website.App.Database;
using Website.Pages.homefolder.content.endpoints.messages;
using static Website.Pages.User.Address.EndPoints.AddFromLocationModel;

namespace Website.App.Messages
{
    internal static class SendMessage
    {
        internal static bool Execute(long SenderId, long JobId, string Message)
        {
            try
            {
                long ThreadId = 0;
                // Check to see if there is an existing thread for this job.
                using DataTable CMTJobIDDT = DataAccessManager.GetDataTable(Schema.Messaging.Name, Schema.Messaging.Tables.MessageThread, $"job_id={JobId}");
                if (CMTJobIDDT.Rows.Count == 0)
                {
                    ThreadId = CreateThreadID(SenderId, JobId);
                }
                else
                {
                    ThreadId = Convert.ToInt64(CMTJobIDDT.Rows[0]["id"]);
                }

                // Insert message
                string typecode = GetTypeCode(SenderId);
                string iconclass = Database.Shared.Sanitize(IconClass(typecode), true, true);

                string safeText = Database.Shared.Sanitize(Message, true, true);
                string fields = "thread_id, job_id, sender_user_id, recipient_user_id, message_text, type_code, icon_class, is_read, ding, created_on_oad, read_on_oad, delivered_on_oad";
                string values = $"{ThreadId}, {JobId}, {SenderId}, {GetRecipientID(JobId)}, {safeText}, '{typecode}', {iconclass}, false, true, {DateTime.UtcNow.ToOADate()}, 0, 0";

                long messageId = DataAccessManager.Insert(Schema.Messaging.Name, Schema.Messaging.Tables.Message, fields, values);
                // Build single bubble HTML
                return true;
            }
            catch (Exception ex)

            {
                Console.WriteLine(ex.ToString());
                return false;
            }
           
        }
        internal static long CreateThreadID(long SenderID, long JobID)
        {
            long recipient = GetRecipientID(JobID);

            string fields = "job_id, subject, is_closed, created_on_oad, updated_on_oad, last_message_id, last_sender_id, last_recipient_id";
            string values = $"{JobID}, 'Job: {GetJobRef(JobID)} - Chat with {GetSenderFullName(SenderID)}', false, {DateTime.UtcNow.ToOADate()}, {DateTime.UtcNow.ToOADate()}, 0, {SenderID}, {recipient}";

            long ThreadID = DataAccessManager.Insert(Schema.Messaging.Name, Schema.Messaging.Tables.MessageThread, fields, values);

            fields = "thread_id, user_id, is_hidden";
            values = $"{ThreadID}, {SenderID}, false";
            _ = DataAccessManager.Insert(Schema.Messaging.Name, Schema.Messaging.Tables.MessageThreadUser, fields, values);
            values = $"{ThreadID}, {recipient}, false";
            _ = DataAccessManager.Insert(Schema.Messaging.Name, Schema.Messaging.Tables.MessageThreadUser, fields, values);

            return ThreadID;
        }
        private static long GetRecipientID(long JobId)
        {
            string sql = $"SELECT user_id FROM {Schema.Operations.Tables.Job} WHERE id={JobId}";
            var id = DataAccessManager.ExecuteScalar(Schema.Operations.Name, sql, []);
            if (id == null) { return 0; }

            return Convert.ToInt64(id);
        }
        private static string GetJobRef(long JobId)
        {
            string sql = $"SELECT job_ref FROM {Schema.Operations.Tables.Job} WHERE id={JobId}";
            var jobref = DataAccessManager.ExecuteScalar(Schema.Operations.Name, sql, []).ToString();
            if (jobref == null) { return ""; }
            return jobref;
        }
        private static string GetSenderFullName(long SenderID)
        {
            string sql = $"SELECT fullname FROM {Schema.Entities.Tables.Users} WHERE id={SenderID}";
            var fullname = DataAccessManager.ExecuteScalar(Schema.Entities.Name, sql, []).ToString();
            if (fullname == null) { return ""; }
            return fullname;
        }
        private static string GetTypeCode(long SenderID)
        {
            using DataTable URDT = DataAccessManager.GetDataTable(Schema.Entities.Name, Schema.Entities.Tables.UserRoles, $"user_id={SenderID}");
            if (URDT.Rows.Count == 0) { return "Customer"; }

            using DataTable RDT = DataAccessManager.GetDataTable(Schema.Entities.Name, Schema.Entities.Tables.Roles, $"id={Convert.ToInt64(URDT.Rows[0]["role_id"])}");
            if (RDT.Rows.Count == 0) { return "Customer"; }

            return RDT.Rows[0]["name"] as string ?? "System";
        }
        private static string IconClass(string typecode)
        {
            using DataTable ICTDT = DataAccessManager.GetDataTable(Schema.Entities.Name, Schema.Entities.Tables.Roles, $"name='{typecode}'");
            if (ICTDT.Rows.Count == 0) { return "fa-comments"; }
            return ICTDT.Rows[0]["iconclass"] as string ?? "fa-comments";
        }
    }
}
