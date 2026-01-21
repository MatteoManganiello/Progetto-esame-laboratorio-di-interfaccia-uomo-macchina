variable "project_id" {
  type        = string
  description = "GCP project ID"
}

variable "region" {
  type        = string
  description = "GCP region"
  default     = "europe-west1"
}

variable "artifact_repo" {
  type        = string
  description = "Artifact Registry repository id"
  default     = "backend"
}

variable "cloud_run_service_name" {
  type        = string
  description = "Cloud Run service name"
  default     = "template-web"
}

variable "container_image" {
  type        = string
  description = "Full container image URL"
  default     = ""
}

variable "cloudsql_connection_name" {
  type        = string
  description = "Cloud SQL instance connection name (project:region:instance)"
  default     = ""
}

variable "sql_instance_name" {
  type        = string
  description = "Cloud SQL instance name"
  default     = "template-mysql"
}

variable "sql_database_name" {
  type        = string
  description = "Cloud SQL database name"
  default     = "template_db"
}

variable "sql_user" {
  type        = string
  description = "Cloud SQL database user"
  default     = "appuser"
}

variable "sql_password" {
  type        = string
  description = "Cloud SQL database password"
  sensitive   = true
}

variable "sql_tier" {
  type        = string
  description = "Cloud SQL machine tier"
  default     = "db-f1-micro"
}

variable "db_connection_string" {
  type        = string
  description = "Connection string for the application"
  sensitive   = true
  default     = ""
}

variable "enable_cloud_run" {
  type        = bool
  description = "Whether to provision Cloud Run resources"
  default     = false
}

variable "vpc_network_self_link" {
  type        = string
  description = "Optional VPC network self link for private IP"
  default     = null
}
