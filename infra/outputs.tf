output "cloud_run_url" {
  value       = try(google_cloud_run_service.backend[0].status[0].url, null)
  description = "Cloud Run service URL"
}

output "cloud_sql_instance_connection_name" {
  value       = google_sql_database_instance.mysql.connection_name
  description = "Cloud SQL instance connection name"
}
