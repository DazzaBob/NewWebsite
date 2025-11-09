namespace Website.App.Database.Operations
{
    internal static class Tables
    {
        private const string Schema = App.Database.Schema.Operations.Name;
        internal static System.Data.DataTable GetLinkedJobsForUser(long UserId)
        {
            Npgsql.NpgsqlParameter[] parameters = [new Npgsql.NpgsqlParameter("@UserId", UserId)];
            string sql = @"SELECT DISTINCT CASE WHEN j.user_id = @UserId AND pa.participant_id <> @UserId THEN pa.participant_id WHEN pa.participant_id = @UserId AND j.user_id <> @UserId THEN j.user_id ELSE NULL END AS contact_id 
            FROM ops.job j LEFT JOIN ops.participant_assignment pa ON pa.job_id = j.id WHERE (@UserId IN (j.user_id, pa.participant_id)) AND pa.participant_id IS NOT NULL AND j.allocation_status_id IS NOT NULL;";

            return DataAccessManager.GetDataTable(Schema, sql, parameters); // uses the ops schema.
        }
    }
}
